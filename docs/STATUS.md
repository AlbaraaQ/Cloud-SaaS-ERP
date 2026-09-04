# Status Ledger

| Phase | Date | State | Notes |
|---|---|---|---|
| PHASE_01 | 2026-08-23 | COMPLETE | Repository and engineering standards bootstrap. Re-audited on 2026-09-04: the phase was **present but over-claimed** — `.prettierrc.json`, `.prettierignore`, `.lintstagedrc.json`, `.gitleaksignore`, `.github/workflows/ci.yml` and every `.env.example` were missing from the commit and have since been created. |
| PHASE_02 | 2026-09-04 | COMPLETE | Backend platform core (NestJS bootstrap, request pipeline, contracts/config/database packages, health probes). Originally reported `IN_PROGRESS` on 2026-09-01; the phase **failed its own acceptance bar** (`npm run verify` never reached lint/unit/integration/build) and was completed during the Phase 03 pass. See `PHASE_02_IMPLEMENTATION_REPORT.md`. |
| PHASE_03 | 2026-09-04 | COMPLETE | Tenancy, Identity & Access: 9-table platform/tenancy schema with reversible idempotent migration, RLS on all tenant-scoped tables, `api`/`migrator` database roles, auth (login/refresh/logout/change-password), RBAC, typed tenant settings, guard pipeline, isolation harness. `pnpm run verify` exits 0. See `PHASE_03_IMPLEMENTATION_REPORT.md`. |
| PHASE_04 | 2026-09-04 | COMPLETE | Platform Services: audit trail (interceptor + service API, append-only at the privilege level), files (presign/finalize/app-signed download, MIME+size allow-lists, orphan GC), notifications (membership inbox + settings-updated demo subscription), BullMQ queues with a transactional outbox and a `WORKER=1` bootstrap, `Sequences.next` (64-parallel, no duplicates), DB-backed idempotency keys replacing the Phase-02 in-memory map. 6 new tables, all under FORCE RLS. `pnpm run verify` exits 0 (148 tests). See `PHASE_04_IMPLEMENTATION_REPORT.md`. |

## Phase-01 Notes

- Monorepo skeleton created with `apps/{api,admin,customer,migrator}` and `packages/{database,contracts,config,testing}`.
- Shared TypeScript, ESLint, Prettier, and workspace configuration established.
- Money guard and env/config skeleton added in the shared config package.
- Docker compose skeleton for postgres, redis, minio, and mailhog added.
- Verify script is wired to run the project bootstrap checks and smoke test.
- No runtime application modules or database schema were created, in line with Phase 01 scope.

## Phase-02 Notes

- NestJS platform bootstrap implemented at the API app boundary: `RequestIdMiddleware` →
  helmet/CORS → global `api/v1` prefix → zod pipes → RFC 9457 `application/problem+json`
  exception filter → request-context and idempotency interceptors.
- `packages/contracts` (error codes, problem shape, pagination/filter/sort helpers,
  permission registry, request-id), `packages/config` (env schema, tenant-settings
  registry) and `packages/database` (Drizzle client, migration runner, CLI) established.
- `/health/live` and `/health/ready` outside the versioned prefix.
- Completed items that Phase 02 had left open: ESLint flat config that actually runs,
  coverage/tooling config, the generated `packages/contracts/openapi.json` artifact, and
  `docs/PHASE_02_IMPLEMENTATION_REPORT.md`.

## Phase-03 Notes

- Schema: `tenants, users, memberships, roles, permissions, role_permissions,
  membership_roles, refresh_tokens, tenant_settings` + `erp_migrations`, applied by
  `packages/database/migrations/0000_platform_identity.sql` (267 lines) with a reversible
  counterpart in `migrations/down/`. Verified idempotent (apply → skip) and reversible
  (down → re-apply) against a real PostgreSQL 16 cluster.
- RLS `ENABLE` + `FORCE` on `memberships, roles, role_permissions, membership_roles,
  tenant_settings`; `refresh_tokens`, `tenants`, `users`, `permissions` stay platform-wide.
- Roles: `erp_api` (NOBYPASSRLS, pinned on every migration run) and `erp_migrator`
  (BYPASSRLS, migration-only), created `NOLOGIN` in SQL; `LOGIN` + password only from
  `pnpm db:roles` so no credential is ever written into a migration.
- Guard pipeline frozen by `API_ARCHITECTURE §2` and asserted by `app.module.spec.ts`:
  `RateLimitGuard → AuthGuard → TenantGuard (RLS GUC) → BranchScopeGuard → PermissionsGuard`.
- Auth: RS256 access tokens (15 min, `sub/tid/mid/scope/jti`), 256-bit rotating refresh
  tokens stored as SHA-256, family revocation on reuse, Argon2id `m=65536,t=3,p=4`,
  lockout after 5 failures, 10/min login bucket, 423 for a suspended tenant, 403 for a
  forged `tid`.
- Testing: `packages/testing` provides the `TESTING_STRATEGY §6` isolation harness; it is
  applied to `memberships` and `roles` with all four proofs plus a direct-SQL RLS probe.
  Integration tests run against an embedded PostgreSQL with no Docker dependency.
- Toolchain: workspace packages now emit `dist/` and are consumed as compiled JavaScript at
  runtime (tests still resolve them to TypeScript source through vitest aliases). This is
  what makes `pnpm run build`, `openapi:export` and the entry-point smoke check pass.

## Phase-04 Notes

- Schema: `audit_log, files, notifications, outbox_jobs, idempotency_keys,
  document_sequences` applied by `packages/database/migrations/0001_platform_services.sql`
  with a reversible counterpart in `migrations/down/`. All six carry `ENABLE`+`FORCE`
  RLS; `audit_log` additionally has `UPDATE, DELETE, TRUNCATE` revoked from `erp_api`,
  so immutability is a privilege, not a convention (proved in `test/audit.spec.ts` by
  asserting SQLSTATE `42501`).
- Audit: a global `AuditInterceptor` records every successful mutating request
  (entity/action/actor/after/meta) and auth events including failures; a service that
  knows the previous state writes the row itself inside its transaction — with a real
  `before` — and marks the request audited so exactly one row is produced. Sensitive
  keys are redacted structurally at any depth before the row is written.
- Files: `POST /files/presign` → client PUT → `POST /files/{id}/finalize` →
  `GET /files/{id}/download` → `GET /files/{id}/content` (302). SigV4 is implemented
  in-repo and verified against the AWS reference vector; object keys are
  `tenants/{tid}/{yyyy}/{mm}/{fileId}/{name}`. `VirusScanner` and the SMTP mailer are
  ports with no-op/console adapters, per the phase's out-of-scope list.
- Jobs: queues `einvoice, notifications, reports-export, migration, maintenance`; nothing
  publishes from inside a business transaction — services write `outbox_jobs` rows and
  `OutboxPublisher` drains them per tenant (`FOR UPDATE SKIP LOCKED`, exponential backoff
  capped at 1 h, dead-letter at `OUTBOX_MAX_ATTEMPTS`). No component uses BYPASSRLS.
- `WORKER=1` boots the same image as an application context with no HTTP listener; it
  starts cleanly without Redis (inert driver, outbox rows simply stay `pending`).
- Idempotency: `idempotency_keys` replaces the Phase-02 in-memory map. The stored
  response is **text**, so a replay is byte-identical; a reused key with a different
  payload is a 409 `IDEMPOTENCY_REPLAY`; a failed handler releases the claim.
- Deviations recorded as CR-004 (unknown setting key on write: 404 → 400) and CR-005
  (additional file/notification/job endpoints + `platform.notification.view|manage`,
  `platform.job.view`).
- Known gap: no Docker in the build environment, so the presign→upload→finalize→download
  flow was verified against an in-memory storage fake rather than live MinIO, and the
  BullMQ hop was verified against a recording queue fake. Everything that touches
  PostgreSQL — including all RLS and concurrency proofs — ran against a real server.

## Conventions

- `docs/` remains the authoritative documentation source.
- Phase outputs must be self-verifying and must not contradict `PROJECT_CONTRACT.md` or `TARGET_ARCHITECTURE.md`.
- Implementation for later phases starts from this foundation only.
- A phase is `COMPLETE` only when `pnpm run verify` exits 0 on a clean checkout.
