import { Pool, type PoolClient } from 'pg';
import { baselineRoles, env, tenantSettingsRegistry } from '@erp/config';
import { ALL_PERMISSIONS, permissionRegistry } from '@erp/contracts';

import { newId } from './ids.js';

/**
 * Idempotent platform seed (PHASE_03 §5.7 "Idempotent seeds: run twice-safe").
 *
 * Seeding writes tenant-scoped tables, which are protected by RLS with FORCE. The
 * seeding connection therefore binds `app.tenant_id` for the whole session before
 * inserting — the same rule the API follows, so the seed can never write outside the
 * tenant it is seeding.
 */

export type SeedOptions = {
  tenantCode?: string;
  tenantName?: string;
  ownerEmail?: string;
  ownerFullName?: string;
  /** Argon2id PHC string produced by the platform password service. */
  ownerPasswordHash?: string;
  log?: (message: string) => void;
};

export type SeedReport = {
  permissions: number;
  tenantId: string;
  userId: string;
  membershipId: string;
  roles: string[];
  settingsInserted: number;
};

export const DEMO_TENANT_CODE = 'demo';

/**
 * Upserts the code-list permission registry (PHASE_03 §5.6: "permission registry seeded
 * from a code list"). Idempotent, and independent of any tenant, so a freshly migrated
 * database — including an integration-test database — has the codes that
 * `role_permissions.permission_code` references.
 */
export async function seedPermissionRegistry(connectionString?: string): Promise<number> {
  const pool = new Pool({
    connectionString: connectionString ?? env.DATABASE_MIGRATOR_URL ?? env.DATABASE_URL,
  });
  const client = await pool.connect();
  try {
    return await upsertPermissions(client);
  } finally {
    client.release();
    await pool.end();
  }
}

async function upsertPermissions(client: PoolClient): Promise<number> {
  for (const permission of permissionRegistry) {
    await client.query(
      `INSERT INTO permissions (code, module, description)
       VALUES ($1, $2, $3)
       ON CONFLICT (code) DO UPDATE SET module = EXCLUDED.module, description = EXCLUDED.description`,
      [permission.code, permission.module, permission.description],
    );
  }
  return permissionRegistry.length;
}

export async function seedPlatform(
  connectionString?: string,
  options: SeedOptions = {},
): Promise<SeedReport> {
  const log = options.log ?? (() => undefined);
  const tenantCode = options.tenantCode ?? DEMO_TENANT_CODE;
  const tenantName = options.tenantName ?? 'Demo Trading Co.';
  const ownerEmail = options.ownerEmail ?? `owner@${tenantCode}.test`;
  const ownerFullName = options.ownerFullName ?? 'Tenant Owner';

  const pool = new Pool({
    connectionString: connectionString ?? env.DATABASE_MIGRATOR_URL ?? env.DATABASE_URL,
  });

  // A single client for the whole seed: `set_config(..., false)` below binds the tenant
  // for the *session*, and `pool.end()` would hang on a client that was never released.
  const client = await pool.connect();
  try {
    // 1. permission registry (platform table — no RLS)
    const permissionCount = await upsertPermissions(client);
    log(`seed  permissions: ${permissionCount}`);

    // 2. tenant
    const tenantRow = await client.query<{ id: string }>(
      `INSERT INTO tenants (id, code, name)
       VALUES ($1, $2, $3)
       ON CONFLICT (code) DO UPDATE SET name = EXCLUDED.name
       RETURNING id`,
      [newId(), tenantCode, tenantName],
    );
    const tenantId = tenantRow.rows[0]?.id as string;

    // RLS on the tenant-scoped tables below applies to the seeding role too
    // (FORCE ROW LEVEL SECURITY), so bind the tenant for this session.
    await client.query(`SELECT set_config('app.tenant_id', $1, false)`, [tenantId]);

    // 3. owner user (platform table)
    const userRow = await client.query<{ id: string }>(
      `INSERT INTO users (id, email, full_name, status, password_hash, must_change_password, password_changed_at)
       VALUES ($1, $2, $3, $4, $5, $6, CASE WHEN $5::text IS NULL THEN NULL ELSE now() END)
       ON CONFLICT (email) DO UPDATE SET full_name = EXCLUDED.full_name
       RETURNING id`,
      [
        newId(),
        ownerEmail,
        ownerFullName,
        options.ownerPasswordHash ? 'active' : 'invited',
        options.ownerPasswordHash ?? null,
        !options.ownerPasswordHash,
      ],
    );
    const userId = userRow.rows[0]?.id as string;

    // 4. membership (owner)
    const membershipRow = await client.query<{ id: string }>(
      `INSERT INTO memberships (id, tenant_id, user_id, display_name, status, is_owner)
       VALUES ($1, $2, $3, $4, 'active', true)
       ON CONFLICT (tenant_id, user_id) WHERE deleted_at IS NULL
         DO UPDATE SET is_owner = true, status = 'active'
       RETURNING id`,
      [newId(), tenantId, userId, ownerFullName],
    );
    const membershipId = membershipRow.rows[0]?.id as string;

    // 5. baseline roles + their permissions
    const roleNames: string[] = [];
    for (const role of baselineRoles) {
      const roleRow = await client.query<{ id: string }>(
        `INSERT INTO roles (id, tenant_id, name, is_system, description)
         VALUES ($1, $2, $3, $4, $5)
         ON CONFLICT (tenant_id, name) WHERE deleted_at IS NULL
           DO UPDATE SET is_system = EXCLUDED.is_system, description = EXCLUDED.description
         RETURNING id`,
        [newId(), tenantId, role.name, role.isSystem, role.description],
      );
      const roleId = roleRow.rows[0]?.id as string;
      roleNames.push(role.name);

      const codes = role.permissions.includes(ALL_PERMISSIONS)
        ? permissionRegistry.map((permission) => permission.code)
        : role.permissions.filter((code) =>
            permissionRegistry.some((permission) => permission.code === code),
          );

      await client.query(`DELETE FROM role_permissions WHERE role_id = $1`, [roleId]);
      for (const code of codes) {
        await client.query(
          `INSERT INTO role_permissions (role_id, permission_code) VALUES ($1, $2) ON CONFLICT DO NOTHING`,
          [roleId, code],
        );
      }

      if (role.code === 'owner') {
        await client.query(
          `INSERT INTO membership_roles (membership_id, role_id) VALUES ($1, $2) ON CONFLICT DO NOTHING`,
          [membershipId, roleId],
        );
      }
    }
    log(`seed  roles: ${roleNames.join(', ')}`);

    // 6. default tenant settings — inserted only when absent so a re-run never
    //    overwrites an operator change.
    let settingsInserted = 0;
    for (const definition of tenantSettingsRegistry) {
      const inserted = await client.query(
        `INSERT INTO tenant_settings (tenant_id, key, value)
         VALUES ($1, $2, $3::jsonb)
         ON CONFLICT (tenant_id, key) DO NOTHING`,
        [tenantId, definition.key, JSON.stringify(definition.defaultValue)],
      );
      settingsInserted += inserted.rowCount ?? 0;
    }
    log(`seed  tenant_settings: ${settingsInserted} new`);

    return {
      permissions: permissionRegistry.length,
      tenantId,
      userId,
      membershipId,
      roles: roleNames,
      settingsInserted,
    };
  } finally {
    client.release();
    await pool.end();
  }
}
