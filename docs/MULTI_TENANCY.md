# MULTI_TENANCY

> Level B — CANONICAL.

## 1. Strategy: Shared Database + Shared Schema + Row-Level Security

Chosen: **one Postgres database, one schema, `tenant_id` on every business table,
isolation enforced twice** — (a) application tenant guard binding every query, and
(b) **PostgreSQL RLS** policies as a DB-enforced backstop.

Rejected: DB-per-tenant (ops explosion at N tenants; the legacy "DB per fiscal year"
pain proves the cost) · Schema-per-tenant (migration fan-out, pooler pain) ·
Silo-per-tenant (only viable for regulated whales; revisit via ADR if such a tenant signs).

## 2. Tenant Model

`tenants(id, code, name, status, base_currency, timezone, locale, country_code, …)`
- `status`: `active | suspended | archived` (middleware rejects non-active).
- One tenant = one company group; branches inside tenant. Legacy multi-company
  customers map to multiple tenants (linked in `tenant_groups` only if needed — deferred).

`memberships(tenant_id, user_id, display_name, branch_scope, status, …)`
- `branch_scope jsonb`: `null` = all branches; else array of branch ids → enforced by
  guard + query filters (branch is NOT a security boundary for tenant data, only scoping).

## 3. Runtime Binding

1. `AuthGuard` verifies JWT → `{ sub, tid, mid }`.
2. `TenantGuard` asserts membership active for `tid`, attaches `TenantContext`
   (`AsyncLocalStorage`).
3. `DataSource` wrapper: every transaction starts with
   `SELECT set_config('app.tenant_id', $1, true)` (+ `app.is_platform_admin` when applicable).
4. Drizzle queries: tenant filter auto-injected by module repositories
   (lint rule bans raw queries missing `tenant_id` on scoped tables).
5. RLS policy template (applied to every scoped table):

```sql
ALTER TABLE sales_invoices ENABLE ROW LEVEL SECURITY, FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON sales_invoices
  USING (tenant_id = current_setting('app.tenant_id')::uuid)
  WITH CHECK (tenant_id = current_setting('app.tenant_id')::uuid);
```

- Migration role (`migrator`) and platform-admin plane use `BYPASSRLS` roles **only** —
  never the API role.
- Indexes: prefixed `(tenant_id, …)` on all hot paths; PKs remain `uuid` plain.

## 4. Cross-Tenant Rules

| Plane | Allowed? | Mechanism |
|---|---|---|
| Tenant user → own tenant | ✅ | guards + RLS |
| Tenant user → other tenant | ❌ hard-block | RLS + no tenant switch for standard users |
| Platform admin (ops) → any tenant | ⚠️ audited | separate `platform` module, `platform_admin` flag on user, dedicated audit channel, break-glass reason logged |
| Migrator → target tenant | ✅ | bypass role inside worker, run-scoped, tenant pre-created |
| Aggregated analytics across tenants | deferred | read replica + dask later — not v1 |

## 5. Tenant-Level Configuration

`tenant_settings(tenant_id, key, value jsonb)` with typed keys in
`packages/config/tenant-settings.ts`: invoice numbering prefixes, VAT defaults,
rounding digits (from legacy `DigitsNo`), price-includes-VAT default, posting-profile
bindings, feature flags for vertical packs (pos / projects / hrm / niche…).

Feature flags gate UI + API module routes; disabled module routes return `404`
(not `403`) to avoid capability enumeration.

## 6. Provisioning & Lifecycle

- Sign-up (or ops console) → tenant row + admin user + **seed**: default COA template,
  currencies, units, tax groups, payment methods, posting profiles, number sequences,
  default branch/warehouse/safe.
- Suspension: middleware denies write traffic; read-only 7 days export window.
- Deletion: soft → purge job after retention (30 d) with PII crypto-shredding note in
  `SECURITY_ARCHITECTURE.md` §11.

## 7. Tenant Isolation Test Contract (mandatory per phase)

For every new tenant-scoped resource the phase MUST add tests proving:
1. Tenant A cannot READ tenant B row by id (404), list (absent), search (absent).
2. Tenant A cannot WRITE to tenant B id (404/422), cannot create with foreign
   `branch_id/warehouse_id/party_id/account_id` (422), cannot guess sequences.
3. RLS layer: direct SQL under API role without GUC returns zero rows.
4. Export/report endpoints obey the same isolation.
(See `TESTING_STRATEGY.md` §6 — copy-paste harness provided in P03.)
