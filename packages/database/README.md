# @erp/database

Drizzle schema, client factory, transaction/RLS helpers, migration runner and the
idempotent platform seed. Canonical shapes live in `docs/DATABASE_DESIGN.md`; tenancy
rules in `docs/MULTI_TENANCY.md`.

## Public API

| Export                                                                                         | Purpose                                                                                  |
| ---------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| `createDatabase(url?, max?)`                                                                   | `{ db, pool, close() }` — one pool per process                                           |
| `getDb()` / `getDatabase()` / `closeDb()`                                                      | process-wide singleton handle                                                            |
| `withTx(db, work)`                                                                             | single transaction, **platform scope only** (no tenant GUC)                              |
| `withTenantTx(db, tenantId, work)`                                                             | transaction with `app.tenant_id` bound — **required for every tenant-scoped read/write** |
| `setTenantContext(tx, tenantId)`                                                               | raw GUC setter (`is_local = true`)                                                       |
| `readTenantContext(tx)`                                                                        | reads the GUC — used by the isolation harness                                            |
| `newId()`                                                                                      | UUID **v7**, application-side (`PROJECT_CONTRACT §2`)                                    |
| `baseAuditColumns()`, `baseSoftDeleteColumns()`, `baseTenantIdColumn()`, `baseLegacyColumns()` | shared column groups                                                                     |
| `citext`, `bytea`, `inet`                                                                      | custom column types                                                                      |
| `runMigrations(url?, opts?)`                                                                   | applies `migrations/*.sql` once each, checksum-guarded                                   |
| `runMigrationsDown(url?, opts?)`                                                               | applies `migrations/down/*.down.sql`                                                     |
| `configureDatabaseRoles(url?, opts?)`                                                          | grants LOGIN + password from env                                                         |
| `seedPlatform(url?, opts?)`                                                                    | idempotent permissions / demo tenant / roles / settings seed                             |
| `rlsProtectedTables`, `createTenantIsolationPolicySql()`, `createParentIsolationPolicySql()`   | RLS helpers                                                                              |
| `schema` (`tenants`, `users`, `memberships`, `roles`, …)                                       | Drizzle table objects                                                                    |

## Why `withTenantTx` and not `withTx`

`app.tenant_id` is set with `is_local = true`, so it only lives for the enclosing
transaction. Every tenant-scoped table also has `FORCE ROW LEVEL SECURITY`, which means a
query executed **outside** such a transaction returns zero rows even for the table owner.
That is the intended failure mode: forgetting the helper produces empty results, never
another tenant's data.

`refresh_tokens` and the other `DATABASE_DESIGN §1` platform tables have no RLS policy;
they are reachable through `withTx`.

## Migrations

```bash
pnpm db:generate     # drizzle-kit draft → migrations/generated/ (review before folding in)
pnpm db:migrate      # applies migrations/*.sql in filename order, records checksums
pnpm db:migrate:down # reverts the most recent migration
pnpm db:roles        # grants LOGIN to erp_api / erp_migrator using env passwords
pnpm db:seed         # (run from apps/api) idempotent platform seed
```

Migrations are hand-written, idempotent SQL — not generated artefacts — because
`PROJECT_CONTRACT §13.4` and `MULTI_TENANCY §3` require the RLS/role statements to be
reviewed on every change. Editing an already-applied file is a hard error; add a new one.

## Roles

| Role           | RLS                                                 | Used by                         |
| -------------- | --------------------------------------------------- | ------------------------------- |
| `erp_api`      | enforced (`NOBYPASSRLS`, pinned on every migration) | the API at runtime              |
| `erp_migrator` | bypassed (`BYPASSRLS`)                              | `pnpm db:migrate`, offline only |
