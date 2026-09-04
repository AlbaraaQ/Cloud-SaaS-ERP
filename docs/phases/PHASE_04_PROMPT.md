# PHASE_04_PROMPT — Platform Services (audit, files, notifications, jobs, sequences, idempotency)

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 04 of 23. `docs/` is SSOT; frozen rules:
UUID v7; tenant_id+RLS everywhere; audit columns; money numeric/decimal.js;
problem+json; permission codes `module.entity.action`; immutability of posted
financial docs (later modules rely on audit service being ready NOW).

## 1. CURRENT PHASE
**#04 — Platform Services**: shared capabilities every later module consumes:
append-only `audit_log` + interceptor, S3-backed `files` with presigned upload,
in-app `notifications`, BullMQ job infra + `outbox_jobs`, transactional
`document_sequences`, `idempotency_keys` real storage, tenant settings write-path.

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/TARGET_ARCHITECTURE.md` §6–§8 4. `docs/DATABASE_DESIGN.md` §3–§4
5. `docs/API_CONTRACT.md` §2 6. `docs/SECURITY_ARCHITECTURE.md` §9–§10
7. `docs/MULTI_TENANCY.md` §3 8. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS (verify; else stop)
Identity module, guards, TenantContext+GUC, isolation harness, settings read.

## 4. GOAL & SCOPE
### In scope
- Tables+migrations+RLS: `audit_log, files, notifications, outbox_jobs,
  idempotency_keys, document_sequences` (+ ensure `tenant_settings` write path).
- AuditInterceptor capturing mutations (entity/action/actor/before/after/meta) +
  service API for explicit domain audits; DB: revoke UPDATE/DELETE on audit_log.
- Files: presigned PUT endpoint, finalize attach (`entity/entity_id` validation hook),
  download via app-signed URL; orphan GC job stub registered.
- Notifications: create/list/mark-read endpoints; event hooks from
  domain-event emitter (subscribe demo: settings updated → notification).
- Jobs: BullMQ module, worker bootstrap in same repo (`apps/api` flag `WORKER=1`),
  outbox table as transactional handoff (service writes outbox row in business tx;
  publisher drains to Redis).
- `Sequences.next(docType, branchId, fiscalYearId?)` transactional allocation
  (INSERT ON CONFLICT / FOR UPDATE), returns number + display with prefix/padding.
- Replace P02 in-memory idempotency with `idempotency_keys` storage (24h expiry,
  replay same response, conflict when key reused with different payload).
### Out of scope (DO NOT DO)
Email/SMS sending adapters beyond an interface + console/mailhog stub · business
modules · file antivirus (interface only) · report exports infra (P14).

## 5. EXACT TASKS
1. Migrations + Drizzle entities + RLS for the 6 tables (audit_log exempt of tenant
   trigger when platform events; policy allows tenant rows only).
2. AuditInterceptor (mutating verbs; skip list for auth endpoints logging CRITICAL
   auth events via explicit service) + tests showing before/after diff persisted.
3. Files service + endpoints per API_CONTRACT §2; S3 env validation; size/mime guards.
4. Notifications endpoints + one demo subscription.
5. BullMQ setup: queues named `einvoice, notifications, reports-export, migration,
   maintenance`; worker health logging; outbox publisher with retry/backoff.
6. Sequences service with concurrency test (parallel allocations produce unique
   monotonic values per scope) + display formatting.
7. Idempotency middleware: full replacement + replay/typo-conflict tests.
8. Settings typed-key write validation (unknown key → 400).
9. Isolation harness applied to files/notifications/sequences probes.
10. STATUS.md + module README updates (how later modules consume sequences/audit/outbox).

## 6. DATABASE IMPACT
+6 tables w/ RLS; `audit_log` privileges hardened; indexes per DATABASE_DESIGN §4.

## 7. API IMPACT
Implements API_CONTRACT §2 files/notifications/settings; internal: none new.
OpenAPI updated; permissions added: `platform.file.upload`, `platform.audit.view`,
`platform.settings.manage`, `platform.notification.view/manage`, `platform.job.view`.

## 8. SECURITY REQUIREMENTS
Presign ACL private; content-type/size allow-lists; audit immutability at DB level;
no PII beyond actor label in audit; queue payloads carry no secrets.

## 9. TESTING REQUIREMENTS
Unit for services incl. concurrency sequence test; integration for endpoints;
idempotency replay tests; isolation 4-proofs on new resources; outbox publisher
failure/retry test.

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md; `packages/database/README` note (sequences usage); ADMIN master §1 gets
"Sequences/numbering admin & files manager" confirmation tick (already listed — verify
phrasing matches implemented capabilities; extend section if gap).

## 11. ACCEPTANCE CRITERIA
- Presign→upload→finalize→download flow works against local MinIO (compose).
- Concurrent `Sequences.next` yields no duplicates in a 64-parallel test.
- Audit row exists for a settings change with redacted sensitive keys.
- Idempotent POST replay returns byte-identical response from storage.

## 12. DEFINITION OF DONE
verify green · per-class tests present · docs updated · protocol §8 report.

## 13. DELIVERABLES
6 tables + services + endpoints + worker + harness coverage + docs.

## 14. DO NOT DO
Real email providers · antivirus impl · any domain module · report engine ·
modifying identity module surfaces (integration only via public APIs).
