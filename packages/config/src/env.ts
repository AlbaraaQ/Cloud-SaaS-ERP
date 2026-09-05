import { z } from 'zod';

/**
 * Environment schema — SECURITY_ARCHITECTURE §9 ("Secrets via env only"),
 * TARGET_ARCHITECTURE §5 ("packages/config zod-validates at boot (fail-fast)").
 *
 * Every variable is optional at *import* time so that type-checking, linting and unit
 * tests never depend on a populated environment. `assertRuntimeEnv()` performs the
 * fail-fast validation that `apps/api` runs during bootstrap.
 */

const booleanish = z
  .union([z.boolean(), z.enum(['true', 'false', '1', '0'])])
  .transform((value) => (typeof value === 'boolean' ? value : value === 'true' || value === '1'));

const csv = z.string().transform((value) =>
  value
    .split(',')
    .map((entry) => entry.trim())
    .filter((entry) => entry.length > 0),
);

const envSchema = z.object({
  NODE_ENV: z.enum(['development', 'test', 'production']).default('development'),
  PORT: z.coerce.number().int().positive().default(3000),
  API_HOST: z.string().default('0.0.0.0'),

  DATABASE_URL: z.string().optional(),
  DATABASE_POOL_MAX: z.coerce.number().int().positive().max(100).default(10),
  DATABASE_APP_ROLE: z.string().default('erp_api'),
  DATABASE_MIGRATOR_ROLE: z.string().default('erp_migrator'),
  DATABASE_MIGRATOR_URL: z.string().optional(),
  /** Used only by `pnpm db:roles`; never read at runtime by the API. */
  DATABASE_APP_PASSWORD: z.string().optional(),
  DATABASE_MIGRATOR_PASSWORD: z.string().optional(),

  REDIS_URL: z.string().optional(),

  /** PEM encoded RS256 key pair. Literal `\n` sequences are normalised to newlines. */
  JWT_PRIVATE_KEY: z.string().optional(),
  JWT_PUBLIC_KEY: z.string().optional(),
  JWT_KEY_ID: z.string().default('dev-key-1'),
  /** Frozen: PROJECT_CONTRACT §9 — access 15 min, refresh 30 d. */
  JWT_ACCESS_TTL_SECONDS: z.coerce.number().int().positive().default(900),
  JWT_REFRESH_TTL_SECONDS: z.coerce.number().int().positive().default(2_592_000),

  /** Frozen: PROJECT_CONTRACT §9 / SECURITY_ARCHITECTURE §2 — Argon2id m=64MiB t=3 p=4. */
  AUTH_ARGON2_MEMORY_KIB: z.coerce.number().int().positive().default(65_536),
  AUTH_ARGON2_TIME_COST: z.coerce.number().int().positive().default(3),
  AUTH_ARGON2_PARALLELISM: z.coerce.number().int().positive().default(4),
  AUTH_ARGON2_OUTPUT_LENGTH: z.coerce.number().int().positive().default(32),
  /** SECURITY_ARCHITECTURE §2 — exponential lockout on repeated failures. */
  AUTH_LOGIN_MAX_FAILURES: z.coerce.number().int().positive().default(5),
  AUTH_LOCKOUT_MINUTES: z.coerce.number().int().positive().default(15),

  /** SECURITY_ARCHITECTURE §8 — token buckets. */
  RATE_LIMIT_DEFAULT_PER_MINUTE: z.coerce.number().int().positive().default(600),
  RATE_LIMIT_LOGIN_PER_MINUTE: z.coerce.number().int().positive().default(10),
  RATE_LIMIT_REGISTER_PER_MINUTE: z.coerce.number().int().positive().default(5),

  CORS_ALLOWED_ORIGINS: csv.default(''),
  OPENAPI_ENABLED: booleanish.default(true),

  S3_ENDPOINT: z.string().optional(),
  S3_BUCKET: z.string().optional(),
  S3_ACCESS_KEY_ID: z.string().optional(),
  S3_SECRET_ACCESS_KEY: z.string().optional(),
  /** SigV4 scope. MinIO ignores it but still signs with it, so it must match on both ends. */
  S3_REGION: z.string().default('us-east-1'),
  /** MinIO and most self-hosted gateways only serve path-style (`/bucket/key`) URLs. */
  S3_FORCE_PATH_STYLE: booleanish.default(true),
  /** Lifetime of a pre-signed PUT/GET URL (SECURITY: keep it short). */
  S3_PRESIGN_EXPIRY_SECONDS: z.coerce.number().int().positive().max(604_800).default(900),

  /** PHASE_04 files — size/mime allow-lists are enforced before a presign is issued. */
  FILES_MAX_UPLOAD_BYTES: z.coerce
    .number()
    .int()
    .positive()
    .default(25 * 1024 * 1024),
  FILES_ALLOWED_MIME_TYPES: csv.default(
    'image/png,image/jpeg,image/webp,image/gif,application/pdf,text/csv,text/plain,' +
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,' +
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document,' +
      'application/vnd.ms-excel,application/zip,application/xml,text/xml',
  ),
  /** Lifetime of the app-signed download URL handed to a browser. */
  FILES_DOWNLOAD_URL_TTL_SECONDS: z.coerce.number().int().positive().max(86_400).default(300),
  /** A `pending` file older than this is an abandoned upload and is collected. */
  FILES_ORPHAN_GC_HOURS: z.coerce.number().int().positive().default(24),

  /** PHASE_04 jobs — BullMQ is only wired up when a Redis URL is present. */
  WORKER: booleanish.default(false),
  JOBS_ENABLED: booleanish.default(true),
  JOB_QUEUE_PREFIX: z.string().default('erp'),
  OUTBOX_POLL_INTERVAL_MS: z.coerce.number().int().positive().default(5_000),
  OUTBOX_BATCH_SIZE: z.coerce.number().int().positive().max(1_000).default(50),
  OUTBOX_MAX_ATTEMPTS: z.coerce.number().int().positive().default(8),
  OUTBOX_BACKOFF_BASE_MS: z.coerce.number().int().positive().default(2_000),
  WORKER_HEALTH_LOG_INTERVAL_MS: z.coerce.number().int().positive().default(60_000),

  /** PHASE_04 idempotency — DATABASE_DESIGN §4 ("expires 24h"). */
  IDEMPOTENCY_TTL_HOURS: z.coerce.number().int().positive().default(24),

  /** PHASE_04 mail — `console` writes to the log, `smtp` targets MailHog/SES later. */
  MAIL_TRANSPORT: z.enum(['console', 'smtp']).default('console'),
  MAIL_FROM: z.string().default('no-reply@erp.local'),
  SMTP_HOST: z.string().optional(),
  SMTP_PORT: z.coerce.number().int().positive().default(1025),

  /** AES-256-GCM data-encryption key, base64 (SECURITY_ARCHITECTURE §9). */
  DATA_ENC_KEY: z.string().optional(),
  /** HMAC secret for app-signed file download URLs; derived from DATA_ENC_KEY when unset. */
  FILE_URL_SIGNING_SECRET: z.string().optional(),
});

const parsed = envSchema.safeParse(process.env);

export const env: AppEnv = parsed.success ? parsed.data : envSchema.parse({});

export type AppEnv = z.infer<typeof envSchema>;

/** Normalises PEM material that arrived through an environment variable. */
export function normalisePem(value: string | undefined): string | undefined {
  if (!value || value.trim().length === 0) return undefined;
  if (value.includes('-----BEGIN')) return value;
  const withNewlines = value.replace(/\\n/g, '\n').trim();
  return withNewlines.includes('-----BEGIN') ? withNewlines : undefined;
}

/**
 * Fail-fast boot validation (PHASE_02 §8). Call once during application bootstrap.
 * Missing values throw with a single aggregated message and never leak the value.
 */
export function assertRuntimeEnv(candidate: AppEnv = env): AppEnv {
  const missing: string[] = [];
  if (!candidate.DATABASE_URL) missing.push('DATABASE_URL');
  if (!normalisePem(candidate.JWT_PRIVATE_KEY)) missing.push('JWT_PRIVATE_KEY');
  if (!normalisePem(candidate.JWT_PUBLIC_KEY)) missing.push('JWT_PUBLIC_KEY');
  if (candidate.NODE_ENV === 'production' && !candidate.DATA_ENC_KEY) missing.push('DATA_ENC_KEY');

  if (missing.length > 0) {
    throw new Error(`Invalid environment: missing required variable(s) ${missing.join(', ')}`);
  }
  return candidate;
}

export type ObjectStorageEnv = {
  endpoint: string;
  bucket: string;
  accessKeyId: string;
  secretAccessKey: string;
  region: string;
  forcePathStyle: boolean;
  presignExpirySeconds: number;
};

/**
 * PHASE_04 §5.3 — "S3 env validation". Object storage is optional at boot (the API runs
 * without it; every other endpoint keeps working) but a file endpoint must fail loudly
 * rather than hand out an unusable URL, so the check happens where the value is used.
 */
export function readObjectStorageEnv(candidate: AppEnv = env): ObjectStorageEnv | undefined {
  const missing = objectStorageGaps(candidate);
  if (missing.length > 0) return undefined;
  return {
    endpoint: (candidate.S3_ENDPOINT as string).replace(/\/+$/, ''),
    bucket: candidate.S3_BUCKET as string,
    accessKeyId: candidate.S3_ACCESS_KEY_ID as string,
    secretAccessKey: candidate.S3_SECRET_ACCESS_KEY as string,
    region: candidate.S3_REGION,
    forcePathStyle: candidate.S3_FORCE_PATH_STYLE,
    presignExpirySeconds: candidate.S3_PRESIGN_EXPIRY_SECONDS,
  };
}

export function objectStorageGaps(candidate: AppEnv = env): string[] {
  const missing: string[] = [];
  if (!candidate.S3_ENDPOINT) missing.push('S3_ENDPOINT');
  if (!candidate.S3_BUCKET) missing.push('S3_BUCKET');
  if (!candidate.S3_ACCESS_KEY_ID) missing.push('S3_ACCESS_KEY_ID');
  if (!candidate.S3_SECRET_ACCESS_KEY) missing.push('S3_SECRET_ACCESS_KEY');
  return missing;
}

export function assertObjectStorageEnv(candidate: AppEnv = env): ObjectStorageEnv {
  const storage = readObjectStorageEnv(candidate);
  if (!storage) {
    throw new Error(
      `Object storage is not configured: missing ${objectStorageGaps(candidate).join(', ')}. ` +
        'See infrastructure/docker-compose.yml for the local MinIO defaults.',
    );
  }
  return storage;
}

/**
 * Values that must never reach a log line (PROJECT_CONTRACT §10, PHASE_02 §5.2:
 * "redact password|secret|token|key paths"). Both the bare and the nested form are
 * listed because pino treats `password` and `*.password` as different paths.
 */
export const REDACTED_LOG_PATHS = [
  'req.headers.authorization',
  'req.headers.cookie',
  'req.headers["x-branch-id"]',
  'password',
  '*.password',
  'passwordHash',
  '*.passwordHash',
  'current',
  '*.current',
  'new',
  '*.new',
  'accessToken',
  '*.accessToken',
  'refreshToken',
  '*.refreshToken',
  'token',
  '*.token',
  'tokenHash',
  '*.tokenHash',
  'mfaCode',
  '*.mfaCode',
  'mfaSecretEnc',
  '*.mfaSecretEnc',
  'secret',
  '*.secret',
  'key',
  '*.key',
] as const;
