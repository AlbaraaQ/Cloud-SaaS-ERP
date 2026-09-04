import { Inject, Injectable } from '@nestjs/common';
import { eq } from 'drizzle-orm';
import { ZodError } from 'zod';
import { DomainError, errorCodes } from '@erp/contracts';
import {
  findTenantSetting,
  isTenantSettingKey,
  parseTenantSettingValue,
  resolveTenantSettings,
  tenantSettingsRegistry,
  type TenantSettingsMap,
} from '@erp/config';
import { tenantSettings, withTenantTx, type DatabaseHandle } from '@erp/database';

import { DATABASE_HANDLE } from '../../../database/database.module.js';

export type SettingsListResponse = {
  settings: TenantSettingsMap;
  registry: Array<{
    key: string;
    module: string;
    description: string;
    default: string | boolean | number | null;
  }>;
};

/**
 * `GET /settings`, `PUT /settings/{key}` — API_CONTRACT §2, `platform.settings.manage`.
 * Every write is validated against the typed registry in `packages/config`
 * (MULTI_TENANCY §5); an unknown key or a value of the wrong shape is a 400.
 */
@Injectable()
export class SettingsService {
  constructor(@Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle) {}

  async list(tenantId: string): Promise<SettingsListResponse> {
    const stored = await withTenantTx(this.database.db, tenantId, async (tx) => {
      const rows = await tx
        .select({ key: tenantSettings.key, value: tenantSettings.value })
        .from(tenantSettings)
        .where(eq(tenantSettings.tenantId, tenantId));
      return new Map(rows.map((row) => [row.key, row.value as string | boolean | number | null]));
    });

    return {
      settings: resolveTenantSettings(stored),
      registry: tenantSettingsRegistry.map((entry) => ({
        key: entry.key,
        module: entry.module,
        description: entry.description,
        default: entry.defaultValue,
      })),
    };
  }

  async put(
    tenantId: string,
    key: string,
    value: unknown,
  ): Promise<{ key: string; value: string | boolean | number | null }> {
    if (!isTenantSettingKey(key)) {
      throw new DomainError(errorCodes.NOT_FOUND, `Unknown tenant setting '${key}'`, 404, {
        field: 'key',
      });
    }

    let parsed: string | boolean | number | null;
    try {
      parsed = parseTenantSettingValue(key, value);
    } catch (error) {
      if (error instanceof ZodError) {
        throw new DomainError(
          errorCodes.VALIDATION_FAILED,
          `Value for '${key}' failed validation`,
          400,
          error.issues.map((issue) => ({ field: 'value', message: issue.message })),
        );
      }
      throw error;
    }

    await withTenantTx(this.database.db, tenantId, async (tx) => {
      await tx
        .insert(tenantSettings)
        .values({ tenantId, key, value: parsed, updatedAt: new Date() })
        .onConflictDoUpdate({
          target: [tenantSettings.tenantId, tenantSettings.key],
          set: { value: parsed, updatedAt: new Date() },
        });
    });

    return { key, value: parsed };
  }

  describe(key: string) {
    const definition = findTenantSetting(key);
    if (!definition) return undefined;
    return {
      key: definition.key,
      module: definition.module,
      description: definition.description,
      default: definition.defaultValue,
    };
  }
}
