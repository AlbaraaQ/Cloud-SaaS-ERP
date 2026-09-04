import { describe, expect, it } from 'vitest';

import {
  createParentIsolationPolicySql,
  createTenantIsolationPolicySql,
  enableRowLevelSecuritySql,
  quoteIdent,
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

  it('lists exactly the tenant-scoped tables created by PHASE_03', () => {
    expect([...rlsProtectedTables]).toEqual([
      'memberships',
      'roles',
      'role_permissions',
      'membership_roles',
      'tenant_settings',
    ]);
  });
});

describe('quoteIdent', () => {
  it('quotes plain identifiers and refuses anything else', () => {
    expect(quoteIdent('erp_api')).toBe('"erp_api"');
    expect(() => quoteIdent('erp_api"; DROP TABLE users; --')).toThrow(/not a plain SQL identifier/);
  });
});
