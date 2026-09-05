import { sql } from 'drizzle-orm';

import type { DrizzleDb, DrizzleTx } from './client.js';

/**
 * Row-Level Security helpers — MULTI_TENANCY §3.
 *
 * Every tenant-scoped table gets the same policy. The GUC is transaction-local, so it
 * MUST be set inside the same transaction as the queries that rely on it
 * (`withTenantTx`). `nullif(..., '')` hardens the canonical template: an unset
 * `app.tenant_id` yields SQL NULL, so the policy matches zero rows instead of raising
 * `invalid input syntax for type uuid`.
 */
export const TENANT_GUC = 'app.tenant_id';
export const PLATFORM_ADMIN_GUC = 'app.is_platform_admin';

/** The `USING` / `WITH CHECK` predicate of the canonical tenant policy. */
export const tenantIsolationExpression = `tenant_id = nullif(current_setting('${TENANT_GUC}', true), '')::uuid`;

export function enableRowLevelSecuritySql(table: string): string {
  return `ALTER TABLE ${table} ENABLE ROW LEVEL SECURITY;\nALTER TABLE ${table} FORCE ROW LEVEL SECURITY;`;
}

/**
 * Idempotent by construction: the policy is dropped and recreated, so re-applying the
 * migration never fails with "policy already exists".
 */
export function createTenantIsolationPolicySql(table: string, policyName = 'tenant_isolation'): string {
  return [
    `DROP POLICY IF EXISTS ${policyName} ON ${table};`,
    `CREATE POLICY ${policyName} ON ${table}`,
    `  USING (${tenantIsolationExpression})`,
    `  WITH CHECK (${tenantIsolationExpression});`,
  ].join('\n');
}

/**
 * RLS for a junction table that has no `tenant_id` of its own: isolation is derived
 * through the owning parent row (`roles` / `memberships`).
 */
export function createParentIsolationPolicySql(
  table: string,
  parentTable: string,
  parentKeyColumn: string,
  policyName = 'tenant_isolation',
): string {
  const expression = `EXISTS (SELECT 1 FROM ${parentTable} AS parent WHERE parent.id = ${table}.${parentKeyColumn} AND parent.${tenantIsolationExpression})`;
  return [
    `DROP POLICY IF EXISTS ${policyName} ON ${table};`,
    `CREATE POLICY ${policyName} ON ${table}`,
    `  USING (${expression})`,
    `  WITH CHECK (${expression});`,
  ].join('\n');
}

/**
 * `audit_log` policy (PHASE_04 §5.1). Reads are strictly tenant-scoped, but writes also
 * have to accept the **platform** rows whose `tenant_id` is NULL: a failed login happens
 * before any tenant context exists (DATABASE_DESIGN §4, "tenant_id NULL (platform
 * events)"). `IS NOT DISTINCT FROM` expresses exactly that — with the GUC unset only a
 * NULL-tenant row may be written, with the GUC set only that tenant's rows may be.
 * Platform rows are therefore write-only for the API role: nothing can read them back
 * through a tenant session.
 */
export function createAuditLogPolicySql(table = 'audit_log', policyName = 'tenant_isolation'): string {
  const guc = `nullif(current_setting('${TENANT_GUC}', true), '')::uuid`;
  return [
    `DROP POLICY IF EXISTS ${policyName} ON ${table};`,
    `CREATE POLICY ${policyName} ON ${table}`,
    `  USING (tenant_id = ${guc})`,
    `  WITH CHECK (tenant_id IS NOT DISTINCT FROM ${guc});`,
  ].join('\n');
}

/**
 * Append-only hardening (SECURITY_ARCHITECTURE §9: "audit log … immutable (no update/
 * delete grants)"). Enforced by privileges rather than a trigger so that even a SQL
 * injection through the API role cannot rewrite history.
 */
export function revokeMutationsSql(table: string, role: string): string {
  return `REVOKE UPDATE, DELETE, TRUNCATE ON ${table} FROM ${role};`;
}

/**
 * Binds the tenant to the current transaction (MULTI_TENANCY §3.3).
 * `is_local = true` → the setting disappears at COMMIT/ROLLBACK, so a pooled connection
 * can never leak a tenant into the next request.
 */
export function setTenantContext(tx: DrizzleDb | DrizzleTx, tenantId: string): Promise<unknown> {
  return tx.execute(sql`SELECT set_config(${TENANT_GUC}, ${tenantId}, true)`);
}

export function clearTenantContext(tx: DrizzleDb | DrizzleTx): Promise<unknown> {
  return tx.execute(sql`SELECT set_config(${TENANT_GUC}, '', true)`);
}

type QueryRows<T> = { rows: T[] } | T[];

function rowsOf<T>(result: unknown): T[] {
  return Array.isArray(result) ? (result as T[]) : ((result as { rows: T[] }).rows ?? []);
}

/** Reads the GUC of the current transaction — used by the isolation harness probe. */
export async function readTenantContext(tx: DrizzleDb | DrizzleTx): Promise<string | null> {
  const result = await tx.execute(sql`SELECT nullif(current_setting(${TENANT_GUC}, true), '') AS tenant_id`);
  const rows = rowsOf<{ tenant_id: string | null }>(result);
  return rows[0]?.tenant_id ?? null;
}

/** Tables that carry RLS in this phase (kept next to the schema for review). */
export const rlsProtectedTables = [
  'memberships',
  'roles',
  'role_permissions',
  'membership_roles',
  'tenant_settings',
  // PHASE_04 — platform services (DATABASE_DESIGN §3–§4).
  'audit_log',
  'files',
  'notifications',
  'outbox_jobs',
  'idempotency_keys',
  'document_sequences',
  // PHASE_05 — organization (DATABASE_DESIGN §5 + §3 currencies).
  'company_profiles',
  'branches',
  'warehouses',
  'cash_locations',
  'cash_location_balances',
  'currencies',
  'fx_rates',
  'price_lists',
  'price_list_items',
  'branch_posting_profiles',
] as const;

export type QueryRowsOf<T> = QueryRows<T>;
