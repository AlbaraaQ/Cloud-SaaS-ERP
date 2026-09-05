import { Injectable, Logger } from '@nestjs/common';
import { assertObjectStorageEnv, env, readObjectStorageEnv, type ObjectStorageEnv } from '@erp/config';

import { presignS3Url } from './s3-signer.js';

/**
 * Object-storage port (TARGET_ARCHITECTURE §8: "Files: S3 pre-signed upload → `files`
 * row → entity attach").
 *
 * The application never streams bytes: it hands out short-lived, private, pre-signed
 * URLs and stores metadata. That keeps large uploads off the API process and means the
 * only thing that must be secret is the storage credential, which lives in env
 * (SECURITY_ARCHITECTURE §9).
 */

export type PresignedUrl = {
  url: string;
  expiresAt: Date;
  requiredHeaders: Record<string, string>;
};

export interface ObjectStoragePort {
  readonly bucket: string;
  /** True when the endpoint/bucket/credentials are all configured. */
  isConfigured(): boolean;
  presignUpload(objectKey: string, contentType: string, expiresInSeconds?: number): PresignedUrl;
  presignDownload(objectKey: string, fileName: string, expiresInSeconds?: number): PresignedUrl;
  /** Best-effort removal used by the orphan collector; never throws. */
  deleteObject(objectKey: string): Promise<boolean>;
}

export const OBJECT_STORAGE = 'ERP_OBJECT_STORAGE';

@Injectable()
export class S3ObjectStorage implements ObjectStoragePort {
  private readonly logger = new Logger(S3ObjectStorage.name);
  private readonly config: ObjectStorageEnv | undefined;

  constructor(config?: ObjectStorageEnv) {
    this.config = config ?? readObjectStorageEnv();
  }

  get bucket(): string {
    return this.config?.bucket ?? '';
  }

  isConfigured(): boolean {
    return this.config !== undefined;
  }

  private requireConfig(): ObjectStorageEnv {
    // Throws a single aggregated "missing S3_*" message (PHASE_04 §5.3 env validation).
    return this.config ?? assertObjectStorageEnv(env);
  }

  presignUpload(objectKey: string, contentType: string, expiresInSeconds?: number): PresignedUrl {
    const config = this.requireConfig();
    return presignS3Url({
      method: 'PUT',
      endpoint: config.endpoint,
      bucket: config.bucket,
      objectKey,
      region: config.region,
      accessKeyId: config.accessKeyId,
      secretAccessKey: config.secretAccessKey,
      expiresInSeconds: expiresInSeconds ?? config.presignExpirySeconds,
      forcePathStyle: config.forcePathStyle,
      // Binding the content type into the signature stops a client from presigning a
      // harmless `text/csv` and then uploading an executable under that key.
      headers: { 'Content-Type': contentType },
    });
  }

  presignDownload(objectKey: string, fileName: string, expiresInSeconds?: number): PresignedUrl {
    const config = this.requireConfig();
    return presignS3Url({
      method: 'GET',
      endpoint: config.endpoint,
      bucket: config.bucket,
      objectKey,
      region: config.region,
      accessKeyId: config.accessKeyId,
      secretAccessKey: config.secretAccessKey,
      expiresInSeconds: expiresInSeconds ?? config.presignExpirySeconds,
      forcePathStyle: config.forcePathStyle,
    });
  }

  async deleteObject(objectKey: string): Promise<boolean> {
    if (!this.isConfigured()) return false;
    const config = this.requireConfig();
    const { url } = presignS3Url({
      method: 'DELETE',
      endpoint: config.endpoint,
      bucket: config.bucket,
      objectKey,
      region: config.region,
      accessKeyId: config.accessKeyId,
      secretAccessKey: config.secretAccessKey,
      expiresInSeconds: 60,
      forcePathStyle: config.forcePathStyle,
    });

    try {
      const response = await fetch(url, { method: 'DELETE' });
      return response.ok || response.status === 404;
    } catch (error) {
      this.logger.warn(
        { err: error instanceof Error ? error.message : String(error), objectKey },
        'object delete failed; the row stays marked deleted and will be retried',
      );
      return false;
    }
  }
}
