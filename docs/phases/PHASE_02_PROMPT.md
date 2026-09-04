# PHASE_02_PROMPT — Backend Platform Core (NestJS bootstrap)

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP (legacy replacement). Phase 02 of 23. Docs in `docs/`
are the Single Source of Truth; `docs/PROJECT_CONTRACT.md` is frozen law: UUID v7 PKs,
money `numeric(20,4)` + decimal.js (never JS number), `timestamptz` UTC + tenant-local
business dates, `tenant_id` + Postgres RLS on every business table, soft delete
(`deleted_at/by`) on master data only, audit columns everywhere, error format
RFC 9457 problem+json with stable codes, API base `/api/v1`.

## 1. CURRENT PHASE
**#02 — Platform Core**: make `apps/api` bootable, observable, configurable, with the
request pipeline that all later domain modules reuse. No business modules yet.

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/TARGET_ARCHITECTURE.md` 4. `docs/API_ARCHITECTURE.md` §1–§2, §6–§7
5. `docs/DATABASE_DESIGN.md` §0 conventions only. 6. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS (assume present; verify, else stop)
- pnpm monorepo with `apps/api` placeholder, `packages/{config,testing}`, verify script.

## 4. GOAL & SCOPE
### In scope
- NestJS 11 app bootstrap (`main.ts`, `app.module.ts`) with config from `packages/config`.
- pino logging (`nestjs-pino`) + AsyncLocalStorage request context (`traceId`, later `tenantId`).
- Global exception filter → problem+json (`@erp/contracts/errors.ts` seed codes from
  API_CONTRACT §0); global validation pipeline setup (zod pipe helper).
- Health endpoints `/health/live`, `/health/ready` (checks DB).
- `packages/database`: Drizzle client factory, tx helper `withTx`, migration runner
  config (drizzle-kit), base column helpers (`id uuid v7`, audit columns, `tenantId`),
  RLS helper `setTenantContext(tx, tenantId)` executing `set_config('app.tenant_id', …, true)`.
- `packages/contracts`: error-code registry, pagination DTOs, id/UUID schemas, request-id types.
- OpenAPI skeleton (`/api/docs` env-gated) + exported `packages/contracts/openapi.json` pipeline stub.
- In-process domain-event emitter utility (typed) — used later by posting engine.
- Rate-limit + helmet + CORS config wired (basics; full security phase is P03).
### Out of scope (DO NOT DO)
Auth/users/tenants tables (P03) · any business module · BullMQ/S3 (P04) ·
frontend work.

## 5. EXACT TASKS
1. Bootstrap Nest app + global prefix `/api/v1`; document port/env wiring.
2. Implement request-id + logging + trace propagation; redact `password|secret|token|key` paths.
3. Implement exception filter + `DomainError` base; map zod errors → `VALIDATION_FAILED` with field list.
4. Implement `packages/database` client + `withTx` + base column helpers + RLS setter
   (unit-tested with a temp table created in test DB only).
5. Drizzle-kit config + first (empty) migration folder convention; `db:migrate` runs.
6. `Idempotency-Key` interceptor skeleton storing to in-memory (table integration in P04) — mark TODO(phase:04).
7. OpenAPI generation from a demo zod DTO; oasdiff note in CI comment.
8. Pagination/sort/filter helper utilities (allow-list enforcement → `FILTER_NOT_ALLOWED`).
9. Tests: unit (context, filter, utils) + one integration test (spin Testcontainers PG,
   migrate, call /health/ready 200; RLS setter smoke).
10. STATUS.md entry + module README (`apps/api/README.md` request-pipeline diagram text).

## 6. DATABASE IMPACT
No business tables. `migrations/` machinery only + test-scoped temp table inside tests.

## 7. API IMPACT
Adds: `/health/live`, `/health/ready`, `/api/docs`. No changes to API_CONTRACT resources;
record these ops endpoints as internal (not part of versioned contract).

## 8. SECURITY REQUIREMENTS
helmet defaults · CORS env allow-list · no stack traces in responses · redaction active ·
env fail-fast validation boots off on missing vars.

## 9. TESTING REQUIREMENTS
Unit suite green; integration suite (testcontainers) green in verify; coverage ≥90% on
new utilities.

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md · note the public `packages/database` API surface in `TARGET_ARCHITECTURE.md`?
No — canonical unchanged; add inline TSDoc + `packages/database/README.md`.
UI masters: none.

## 11. ACCEPTANCE CRITERIA
- `pnpm verify` green incl. integration.
- Boot log shows config validation; /health/ready probes DB; problem+json proven by test.
- RLS setter demonstrably changes `current_setting('app.tenant_id')` inside tx (test).

## 12. DEFINITION OF DONE
All tasks + green verify + docs + report (AI_DEVELOPMENT_PROTOCOL §8 format).

## 13. DELIVERABLES
Runnable `apps/api` skeleton, `packages/database`, `packages/contracts` cores, tests, docs notes.

## 14. DO NOT DO
Identity/business tables · auth · queues · S3 · UI · any divergence from pipeline order
(API_ARCHITECTURE §2) without a change request.
