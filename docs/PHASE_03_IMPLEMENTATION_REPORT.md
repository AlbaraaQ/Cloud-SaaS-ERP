# PHASE 03 IMPLEMENTATION REPORT

Tenancy, Identity & Access — multi-tenant platform tables, RLS, authentication, RBAC,
typed tenant settings, and the guard pipeline.

## Phase Report — PH-03

**Delivered**

- **Schema & migration** — `packages/database/migrations/0000_platform_identity.sql`
  (267 lines) creates the nine platform/tenancy tables from `DATABASE_DESIGN §1–§3`
  (`tenants, users, memberships, roles, permissions, role_permissions, membership_roles,
  refresh_tokens, tenant_settings`) plus the `erp_migrations` ledger, and
  `migrations/down/0000_platform_identity.down.sql` (42 lines) reverses it. Every
  statement is guarded (`IF NOT EXISTS` / `DROP … IF EXISTS` / `DO $$ … $$`), so the file
  is idempotent. Verified against a real PostgreSQL 16 cluster:
  `up → applied`, `up → skipped`, `down → applied`, `up → applied` again, ending at
  **10 tables, 5 policies, 5 RLS-enabled relations**.
- **RLS** — `ENABLE` + `FORCE ROW LEVEL SECURITY` on `memberships, roles,
  role_permissions, membership_roles, tenant_settings`. Parent policies compare
  `tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid`; junction
  policies derive isolation through `EXISTS (SELECT 1 FROM roles/memberships …)`, so a
  child row cannot outlive its parent's visibility. `tenants, users, permissions,
  refresh_tokens` are deliberately platform-wide (no `tenant_id`).
- **Database roles** — `erp_api` (NOBYPASSRLS) and `erp_migrator` (BYPASSRLS) are created
  `NOLOGIN` inside the migration; `pnpm db:roles` grants `LOGIN` and sets the password
  from the environment. `ALTER ROLE erp_api NOBYPASSRLS` is re-asserted on every
  migration run (`PROJECT_CONTRACT §13.4`: only the migration role may bypass RLS).
- **Guards** — `RateLimitGuard → AuthGuard → TenantGuard → BranchScopeGuard →
  PermissionsGuard`, registered as `APP_GUARD` in `AppModule` in exactly that order and
  pinned by `app.module.spec.ts`, which reads the provider metadata rather than trusting
  a comment. `TenantGuard` resolves the membership inside `withTenantTx(db, tid, …)` so
  the RLS GUC is already set when the row is read, then re-asserts
  `membership.tenantId === token.tid` and `membership.userId === token.sub`.
- **Auth** (`API_CONTRACT §1`) — `POST /auth/login|refresh|logout|change-password`,
  `GET /me`, `GET /permissions`. RS256 access tokens, 15 min, `sub/tid/mid/scope/jti`;
  256-bit refresh tokens stored as SHA-256 with family revocation on reuse; Argon2id
  `m=65536,t=3,p=4` (asserted against the emitted PHC string, not just the config);
  lockout after 5 failures; 10/min login bucket with `Retry-After`.
- **Tenancy & access** (`API_CONTRACT §2`) — `GET/PATCH /tenant`,
  `GET/POST /memberships`, `GET/PATCH/DELETE /memberships/{id}`,
  `GET/POST /roles`, `GET/PUT /roles/{id}`, `POST /roles/{id}/permissions`,
  `GET /settings`, `PUT /settings/{key}` — 16 documented paths in total, all present in
  the generated `packages/contracts/openapi.json`.
- **Typed settings** — the 16-key registry in `@erp/config` is the single source of
  truth; values are validated by the declared zod schema before they are written, an
  unknown key is a 404, and `GET /settings` returns both the resolved values and the
  registry itself.
- **Seeds** — `seedPlatform()` upserts the 74-code permission registry, the tenant, the
  owner user/membership, the three baseline system roles and the default settings;
  `seedPermissionRegistry()` seeds just the registry and runs for every integration-test
  database. Re-running never overwrites an operator change.
- **Isolation harness** — `packages/testing/src/isolation-suite.ts` implements the four
  mandatory proofs of `TESTING_STRATEGY §6` and is applied to `memberships` and `roles`
  in `apps/api/test/isolation.spec.ts`, plus a direct-SQL RLS probe over all five
  protected tables.

**Deviations**

- `POST /auth/login` returns **only the authenticated tenant's membership** in
  `memberships[]`, not every membership the user holds. The login request is
  tenant-scoped by `tenantCode`, so returning the others would leak tenant existence.
  Recorded as **CR-003**; `API_CONTRACT §1` annotated.
- `users` gained `failed_login_attempts` and `locked_until`, which `DATABASE_DESIGN §1`
  does not list. Required by the lockout rule in `SECURITY_ARCHITECTURE §2`. Additive
  only. Recorded as **CR-001**.
- `GET /memberships/{id}` and `GET /roles/{id}` are additive to `API_CONTRACT §2`; the
  isolation harness cannot prove "read by id is blocked" without them. Recorded as
  **CR-002**.
- Isolation proof 4 (export/report surface) is reported as `exportChecked: false`:
  Phase 03 exposes no export or report endpoint. The harness skips it explicitly rather
  than passing silently, and Phase 14 must supply the probe.
- `platform-admin.guard.ts` is a stub (no platform-admin plane exists yet) and
  `FeatureFlagGuard` is deferred — both are out of Phase 03 scope per the phase prompt.
- `RateLimiterService` is an in-process token bucket with a `TODO(phase:23)` for Redis.
  Buckets are per replica until then; the frozen limits and `Retry-After` behaviour are
  already implemented and tested.
- Workspace packages are now built to `dist/` and consumed as compiled JavaScript at
  runtime (tests still resolve them to TypeScript source through vitest aliases). This is
  a toolchain change, not a contract change: without it the API build fails with
  `TS6059` and NestJS DI cannot resolve `@erp/*`-provided tokens.

**Files** — versus the Phase-01/02 baseline commit on this branch: 50 created,
39 modified, 1 deleted (`apps/api/package.json.bak`). Notable paths:
`packages/database/{migrations,src}`, `packages/{contracts,config,testing}/src`,
`apps/api/src/modules/platform/{auth,guards,tenancy,identity,rate-limit,context,decorators}`,
`apps/api/test/*`, `apps/api/scripts/{seed.ts,smoke-check.mjs}`,
`packages/contracts/openapi.json`.

**Tests** — 102 passing, `pnpm run verify` exits 0.

| Class | Files | Tests |
|---|---|---|
| Unit — `apps/api/src/**/*.spec.ts` | 11 | 45 |
| Integration — `apps/api/test/*.spec.ts` (real PostgreSQL, `erp_api` role) | 4 | 39 |
| Unit — `packages/{config,contracts,database}` | 4 | 18 |

Verify digest: `typegen` → `.generated-types.json`; `tsc --noEmit` → exit 0;
`pnpm -r run lint` → 8 projects, 0 problems; `pnpm run build` → 5 packages built;
`test:smoke` → `AppModule boots, /health/live is 200, guards emit problem+json 401`;
`pnpm -r run test` → 102 passed; `openapi:export` → 16 paths written to
`packages/contracts/openapi.json`.

Integration coverage highlights: login success/lockout/bucket; opaque 401 for wrong
password, unknown e-mail and unknown tenant (no enumeration); refresh rotation with whole
family revocation on reuse; logout; password change invalidating sessions; suspended
tenant → 423 on login *and* on an authenticated request; forged `tid` → 403; permission
allow/deny with the missing code named in `detail`; membership invite/list/patch/delete
with cross-tenant role rejection (422) and last-owner protection; role create/rename/
re-permission with unknown-code rejection and immutable system names; typed settings
round-trip with wrong-type, out-of-range and unknown-key rejection; `FILTER_NOT_ALLOWED`;
`X-Branch-Id` scope enforcement; log redaction.

**Docs updated** — `docs/STATUS.md`, `docs/PHASE_02_IMPLEMENTATION_REPORT.md` (new),
`docs/PHASE_03_IMPLEMENTATION_REPORT.md` (this file),
`docs/change-log/CHANGE-REQUESTS.md`, `docs/API_CONTRACT.md §1–§2`,
`apps/api/README.md`, `apps/api/src/modules/platform/README.md`,
`packages/database/README.md`.

`SECURITY_ARCHITECTURE §5` was verified **unchanged**: the seven implemented platform
codes (`tenant.view`, `tenant.manage`, `membership.manage`, `role.manage`,
`settings.manage`, `audit.view`, `file.upload`) are exactly the `platform` row of the
frozen matrix, and the full 74-code registry spans the same twelve modules.

**CRs opened** — CR-001, CR-002, CR-003.

**Seeds/Env changes** — two database roles, `erp_api` and `erp_migrator`, named by
`DATABASE_APP_ROLE` / `DATABASE_MIGRATOR_ROLE` and given `LOGIN` + a password from
`DATABASE_APP_PASSWORD` / `DATABASE_MIGRATOR_PASSWORD` by `pnpm db:roles`, which must run
after `db:migrate` on a fresh cluster. `DATABASE_MIGRATIONS_DIR` overrides the migration
directory (read by the runner, not by the env schema). `TEST_DATABASE_URL` /
`TEST_DATABASE_ROLE_PASSWORD` are optional: without them the suite boots an embedded
PostgreSQL 16, so `pnpm verify` needs no Docker.

**Follow-ups**

- Redis-backed rate limiting and idempotency (Phase 23 / Phase 04).
- `GET /audit-log`, `POST /files/presign`, `GET /notifications` remain unimplemented
  (Phases 04+); their permission codes already exist in the registry.
- Branch scope is stored and enforced but the branch module arrives in Phase 05.
- Permission-set caching per membership (currently resolved per request).
- Coverage gate from `TESTING_STRATEGY §3` is not enforced yet.
