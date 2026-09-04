# PHASE 04 IMPLEMENTATION REPORT

Platform Services — audit trail, files, notifications, background jobs with a
transactional outbox, document sequences, and durable idempotency keys.

## Phase Report — PH-04

**Delivered**

- **Schema & migration** — `packages/database/migrations/0001_platform_services.sql`
  (238 lines) creates the six tables of `DATABASE_DESIGN §3–§4` (`audit_log, files,
  notifications, outbox_jobs, idempotency_keys, document_sequences`) with the indexes the
  design lists, and `migrations/down/0001_platform_services.down.sql` (38 lines) reverses
  it. Verified against a real PostgreSQL 16 cluster:
  `up → applied`, `up → skipped`, `down → applied`, `up → applied` again, ending at
  **16 tables, 11 policies, 11 relations with `ENABLE`+`FORCE` RLS**, and
  `information_schema.role_table_grants` showing `erp_api` holding only `SELECT, INSERT`
  on `audit_log`.
- **Audit** (`PHASE_04 §5.2`, `SECURITY_ARCHITECTURE §9–§10`) — a global
  `AuditInterceptor` records every **successful** mutating request
  (`entity/action/actorUserId/actorLabel/after/meta{method,path,status,traceId,ip,userAgent}`)
  and auth events including failures. A service that knows the previous state writes the
  row itself, inside its transaction, with a real `before` — `PUT /settings/{key}` does
  exactly that — and calls `markRequestAudited()` so the interceptor stands down and
  exactly one row is produced per request. `AuditService.record/recordInTx` is the
  explicit API later modules use for domain events. Redaction is structural: any key
  matching `pass|secret|token|credential|authorization|hash|mfa|otp|pin|*key` is replaced
  with `[redacted]` at any depth, in `before`, `after` and `meta`, before the row is
  written.
- **Files** (`PHASE_04 §5.3`, `TARGET_ARCHITECTURE §8`) — `POST /files/presign` → client
  PUT straight to storage → `POST /files/{id}/finalize` → `GET /files/{id}/download`
  (app-signed URL) → `GET /files/{id}/content` (302 to the storage pre-signed GET). The
  SigV4 presigner is implemented in-repo and verified against the AWS reference vector;
  object keys are `tenants/{tid}/{yyyy}/{mm}/{fileId}/{safeName}`, so a leaked key is
  useless in another tenant. MIME allow-list and size ceiling are enforced *before* a URL
  is minted, attachments are refused unless the entity has a registered validator (422),
  and a `pending` row older than `FILES_ORPHAN_GC_HOURS` is collected by the
  `maintenance` job — row soft-deleted first, object removed best-effort second.
- **Notifications** (`PHASE_04 §5.4`) — membership-scoped inbox (`GET /notifications`
  with `meta.unread`, `GET /notifications/{id}`, `POST /notifications/{id}/read`,
  `POST /notifications`). The demo subscription is real: `PUT /settings/{key}` emits
  `settings.updated` on the in-process bus after commit, and the subscriber writes the
  inbox row **and** the `notification.email` outbox job in one transaction.
- **Jobs** (`PHASE_04 §5.5`) — queues `einvoice, notifications, reports-export,
  migration, maintenance` behind a `QueuePort`; BullMQ is imported lazily so nothing
  opens a socket when `REDIS_URL` is absent. Producers never publish from a business
  transaction: they write `outbox_jobs`, and `OutboxPublisher` claims rows
  `FOR UPDATE SKIP LOCKED`, publishes inside the same transaction, retries with
  exponential backoff capped at one hour and dead-letters at `OUTBOX_MAX_ATTEMPTS`.
  `WORKER=1` boots the same image as an application context (no HTTP listener) with the
  consumers, the drain loop, the maintenance jobs and a periodic health log.
  `GET /jobs/outbox` and `GET /jobs/health` expose the state.
- **Sequences** (`PHASE_04 §5.6`, BL-1) — `SequencesService.next({tenantId, docType,
  branchId?, fiscalYearId?}, tx?)` allocates through a single
  `INSERT … ON CONFLICT DO UPDATE … RETURNING`, joining the caller's transaction so a
  rolled-back document never burns a number. Returns `{value, prefix, padding, display}`.
  64 parallel allocations yield 64 distinct, gap-free values.
- **Idempotency** (`PHASE_04 §5.7`) — the Phase-02 in-memory map is gone. Claims are
  written to `idempotency_keys` before the handler runs, so two concurrent requests with
  one key cannot both execute; the stored response is replayed **byte-identically** with
  `Idempotency-Replayed: true`; a reused key with a different payload is
  `409 IDEMPOTENCY_REPLAY`; a failed handler releases the claim so the client can retry;
  expired rows are swept by the `maintenance` job.
- **Isolation** — the `TESTING_STRATEGY §6` harness is applied unchanged to `files` and
  `notifications` (all four proofs, including a raw cross-tenant `INSERT` rejected with
  SQLSTATE `42501`), and the tables with no CRUD surface (`audit_log`, `outbox_jobs`,
  `idempotency_keys`, `document_sequences`) are proved at the RLS layer.

**Deviations**

- `PUT /settings/{key}` with an unknown key now returns **400 `VALIDATION_FAILED`**
  instead of the Phase-02 404. Required by `PHASE_04 §5.8`; recorded as **CR-004**,
  `API_CONTRACT §2` annotated and `settings.spec.ts` updated.
- Endpoints beyond the three the contract lists for platform services (`GET /files`,
  `GET /files/{id}`, `POST /files/{id}/finalize`, `GET /files/{id}/download`,
  `GET /files/{id}/content`, `GET /notifications/{id}`, `POST /notifications`,
  `GET /jobs/outbox`, `GET /jobs/health`) plus the permission codes
  `platform.notification.view|manage` and `platform.job.view`. Recorded as **CR-005**;
  `SECURITY_ARCHITECTURE §5` and `API_CONTRACT §2` updated.
- `GET /files/{id}/content` is the only unauthenticated route in the application. A
  browser download cannot carry a bearer token, so the capability is an HMAC signature
  over `(fileId, tenantId, expiry)`; the tenant is read from the signed payload, never
  from the request. Tampered, expired and cross-tenant signatures are all 401 (tested).
- `idempotency_keys.response` is `text`, not the `jsonb` of `DATABASE_DESIGN §4`. jsonb
  normalises key order, which would break the byte-identical replay the same document's
  `API_CONTRACT §0` promises. Noted inline in `DATABASE_DESIGN §4`.
- `document_sequences` uses a surrogate `id` plus a unique index over
  `(tenant_id, coalesce(branch_id, nil), doc_type, coalesce(fiscal_year_id, nil))` rather
  than the literal PK of `DATABASE_DESIGN §3`: a PostgreSQL primary key containing NULLs
  cannot enforce "one row per scope", which is the property the allocator depends on.
- `PlatformServicesModule` is `@Global()`. `modules/platform` (settings) needs
  `AuditService` and this module's controllers need the guards exported by
  `modules/platform`; an `imports:` edge either way is a module cycle.
- Mailer and virus scanner are **ports with stub adapters** (`ConsoleMailer`,
  `NoopVirusScanner`) — explicitly the phase's out-of-scope instruction (`§14`). The scan
  verdict is still recorded in the file's audit meta, so "was this scanned?" is
  answerable from data.
- `audit_log`'s RLS policy is not the canonical template: `tenant_id` is nullable for
  platform-plane events, so `WITH CHECK` uses `IS NOT DISTINCT FROM <guc>`. Those rows
  are write-only for `erp_api` — no tenant session can read them back.
- **Environment gap:** Docker is unavailable in this build environment, so MinIO, Redis
  and MailHog could not be started. The presign→finalize→download flow was exercised
  end-to-end against an in-memory `OBJECT_STORAGE` adapter and the BullMQ hop against a
  recording `QueuePort`; the SigV4 signature itself is verified against the AWS
  reference vector, and everything touching PostgreSQL (all RLS, concurrency and
  immutability proofs) ran against a real server. Running the acceptance flow against
  live MinIO/Redis remains an open verification item — see Follow-ups.

**Files** — 51 created, 29 modified (80 changed files, +7 272/−58). Notable paths:
`packages/database/migrations/0001_platform_services.sql` (+ `down/`),
`packages/database/src/schema/platform-services.ts`, `packages/database/src/rls.ts`,
`packages/contracts/src/platform/{files,notifications,audit,jobs,sequences}.ts`,
`packages/config/src/env.ts`,
`apps/api/src/modules/platform-services/**` (audit, files, notifications, jobs,
sequences, idempotency + `README.md`), `apps/api/src/events/**`,
`apps/api/src/common/interceptors/idempotency.interceptor.ts`,
`apps/api/src/modules/platform/tenancy/settings.service.ts`,
`apps/api/src/{app.module.ts,main.ts}`, `apps/api/test/*.spec.ts`,
`packages/contracts/openapi.json` (28 paths).

**Tests** — 180 passing, `pnpm run verify` exits 0.

| Class | Files | Tests |
|---|---|---|
| Unit — `apps/api/src/**/*.spec.ts` | 16 | 69 |
| Integration — `apps/api/test/*.spec.ts` (real PostgreSQL, `erp_api` role) | 11 | 79 |
| Unit — `packages/{config,contracts,database}` | 6 | 32 |

Phase-04 contribution: 40 integration tests (`files 6`, `audit 7`, `notifications 5`,
`sequences 5`, `idempotency 6`, `jobs 7`, `isolation-platform-services 4`), 24 API unit
tests (SigV4 vector, download-token HMAC, object keys, audit redaction, route→action
mapping, outbox backoff, queue secret guard), 14 package unit tests (contract schemas,
S3 env validation, audit-log policy + revoke SQL).

Verify digest: `typegen` → `.generated-types.json`; `tsc --project tsconfig.base.json
--noEmit` → exit 0; `pnpm -r run lint` → 8 projects, 0 problems; `pnpm run build` →
5 packages built; `test:smoke` → boots `dist/`, `/health/live` 200, guards emit
problem+json 401; `pnpm -r run test` → 180 passed; `openapi:export` → **28 paths**
written to `packages/contracts/openapi.json`.

Acceptance criteria (`PHASE_04 §12`), evidence by test:

| Criterion | Where |
|---|---|
| presign → upload → finalize → download flow | `test/files.spec.ts` (storage fake; MinIO pending) |
| 64-parallel `Sequences.next`, no duplicates | `test/sequences.spec.ts` |
| audit row for a settings change, sensitive keys redacted | `test/audit.spec.ts` |
| idempotent POST replay is byte-identical | `test/idempotency.spec.ts` |
| audit log immutable for the API role | `test/audit.spec.ts` (SQLSTATE `42501`) |
| outbox retry/backoff and dead-letter | `test/jobs.spec.ts` |
| worker starts and stops without Redis | `test/jobs.spec.ts` |
| isolation harness on the new resources | `test/isolation-platform-services.spec.ts` |

**Docs updated** — `docs/STATUS.md` (Phase-04 row + notes),
`docs/PHASE_04_IMPLEMENTATION_REPORT.md` (this file),
`docs/change-log/CHANGE-REQUESTS.md` (CR-004, CR-005),
`docs/API_CONTRACT.md §2`, `docs/DATABASE_DESIGN.md §4`,
`docs/SECURITY_ARCHITECTURE.md §5`, `apps/api/README.md`,
`apps/api/src/modules/platform-services/README.md` (new),
`packages/database/README.md`, `apps/api/.env.example`.

**CRs opened** — CR-004, CR-005.

**Seeds/Env changes** — no new seed. New environment variables, all defaulted so an
existing `.env` keeps working: `S3_REGION`, `S3_FORCE_PATH_STYLE`,
`S3_PRESIGN_EXPIRY_SECONDS`, `FILES_MAX_UPLOAD_BYTES`, `FILES_ALLOWED_MIME_TYPES`,
`FILES_DOWNLOAD_URL_TTL_SECONDS`, `FILES_ORPHAN_GC_HOURS`, `WORKER`, `JOBS_ENABLED`,
`JOB_QUEUE_PREFIX`, `OUTBOX_POLL_INTERVAL_MS`, `OUTBOX_BATCH_SIZE`,
`OUTBOX_MAX_ATTEMPTS`, `OUTBOX_BACKOFF_BASE_MS`, `WORKER_HEALTH_LOG_INTERVAL_MS`,
`IDEMPOTENCY_TTL_HOURS`, `MAIL_TRANSPORT`, `MAIL_FROM`, `SMTP_HOST`, `SMTP_PORT`,
`FILE_URL_SIGNING_SECRET`. New runtime dependency: `bullmq@^5.34` (loaded lazily).
`pnpm db:migrate` must be re-run for `0001_platform_services.sql`.

**Follow-ups**

- Run the acceptance flow against the docker-compose stack (MinIO + Redis + MailHog) in
  an environment that has Docker; the code paths are complete and stubbed only at the
  network boundary.
- Real SMTP/SES adapter behind `MailerPort` and a ClamAV adapter behind
  `VirusScannerPort` (deferred by `PHASE_04 §14`).
- Register attachment validators as business entities land (`FileAttachmentRegistry` is
  empty by design in this phase, so every `entity` is a 422 today).
- Redis-backed rate limiting still carries its `TODO(phase:23)`.
- BullMQ repeatable schedulers currently enqueue maintenance jobs at worker start with an
  hour-bucketed `jobId`; move to `repeatable` job options once a scheduler owner exists.
- Coverage gate from `TESTING_STRATEGY §3` is still not enforced in CI.
