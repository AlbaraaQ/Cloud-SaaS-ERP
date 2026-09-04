import { describe, expect, it } from 'vitest';

import {
  createAuditLogPolicySql,
  createParentIsolationPolicySql,
  createTenantIsolationPolicySql,
  enableRowLevelSecuritySql,
  quoteIdent,
  revokeMutationsSql,
  rlsProtectedTables,
  tenantIsolationExpression,
} from './index.js';

describe('RLS SQL builders (MULTI_TENANCY §3.5)', () => {
  it('emits ENABLE + FORCE so even the table owner is subject to the policy', () => {
    expect(enableRowLevelSecuritySql('memberships')).toContain(
      'ALTER TABLE memberships ENABLE ROW LEVEL SECURITY',
    );
    expect(enableRowLevelSecuritySql('memberships')).toContain(
      'ALTER TABLE memberships FORCE ROW LEVEL SECURITY',
    );
  });

  it('hardens the canonical policy with nullif so an unset GUC hides rows instead of erroring', () => {
    expect(tenantIsolationExpression).toBe(
      "tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid",
    );
    const sql = createTenantIsolationPolicySql('roles');
    expect(sql).toContain('DROP POLICY IF EXISTS tenant_isolation ON roles;');
    expect(sql).toContain('CREATE POLICY tenant_isolation ON roles');
    expect(sql).toContain('USING (tenant_id = nullif(');
    expect(sql).toContain('WITH CHECK (tenant_id = nullif(');
  });

  it('derives isolation for junction tables through the owning parent', () => {
    const sql = createParentIsolationPolicySql('role_permissions', 'roles', 'role_id');
    expect(sql).toContain('FROM roles AS parent');
    expect(sql).toContain('parent.id = role_permissions.role_id');
    expect(sql).toContain('parent.tenant_id = nullif(');
  });

  it('lists exactly the tenant-scoped tables created by PHASE_03 and PHASE_04', () => {
    expect([...rlsProtectedTables]).toEqual([
      'memberships',
      'roles',
      'role_permissions',
      'membership_roles',
      'tenant_settings',
      'audit_log',
      'files',
      'notifications',
      'outbox_jobs',
      'idempotency_keys',
      'document_sequences',
    ]);
  });

  /**
   * PHASE_04 §5.1: `audit_log.tenant_id` is nullable (platform-plane events), so its
   * policy cannot be the canonical one — a NULL tenant would fail `tenant_id = <guc>`
   * on INSERT and the login trail would silently vanish.
   */
  it('lets audit_log write platform rows while keeping reads tenant-scoped', () => {
    const sql = createAuditLogPolicySql();
    expect(sql).toContain('CREATE POLICY tenant_isolation ON audit_log');
    // Reads: strict equality, so a NULL-tenant row is never readable from a tenant session.
    expect(sql).toContain("USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)");
    // Writes: NULL is allowed when (and only when) the GUC is unset.
    expect(sql).toContain('WITH CHECK (tenant_id IS NOT DISTINCT FROM');
  });

  it('revokes every mutation on the append-only table from the API role', () => {
    expect(revokeMutationsSql('audit_log', 'erp_api')).toBe(
      'REVOKE UPDATE, DELETE, TRUNCATE ON audit_log FROM erp_api;',
    );
  });
});

describe('quoteIdent', () => {
  it('quotes plain identifiers and refuses anything else', () => {
    expect(quoteIdent('erp_api')).toBe('"erp_api"');
    expect(() => quoteIdent('erp_api"; DROP TABLE users; --')).toThrow(/not a plain SQL identifier/);
  });
});
