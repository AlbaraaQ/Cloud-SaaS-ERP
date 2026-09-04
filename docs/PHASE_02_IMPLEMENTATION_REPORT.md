# PHASE 02 IMPLEMENTATION REPORT

Backend platform core — NestJS bootstrap, request pipeline, shared packages, health
probes, verification harness.

## Verification verdict (2026-09-04)

Phase 02 was reported `IN_PROGRESS` in `docs/STATUS.md` and never satisfied its own
acceptance bar. Auditing the committed state before starting Phase 03:

- `pnpm run verify` could not progress past the bootstrap step: the ESLint flat config was
  not runnable, and the API build (`tsc -p apps/api/tsconfig.json`) failed with `TS6059`
  because the workspace packages were consumed as TypeScript **source**, which lands them
  outside `rootDir`.
- `packages/contracts/openapi.json` — a declared CI artifact — did not exist and no script
  generated it.
- No unit or integration test existed anywhere in the repository; `pnpm -r run test`
  resolved to echo statements.
- `docs/PHASE_02_IMPLEMENTATION_REPORT.md` (required by `AI_DEVELOPMENT_PROTOCOL §8`)
  was missing.

Everything below is what Phase 02 should have delivered, completed during the Phase 03
pass and verified end to end.

## Delivered

- **Request pipeline** (`apps/api/src`), in the order frozen by `API_ARCHITECTURE §2`:
  `RequestIdMiddleware` (UUIDv7, gateway id normalised, echoed on every response) →
  helmet + CORS allow-list → global `api/v1` prefix with `/health/*` excluded → global
  guards → zod pipes → service layer → `RequestContextInterceptor` /
  `IdempotencyInterceptor` → `AllExceptionsFilter`.
- **RFC 9457 errors**: every failure is `application/problem+json` with
  `{type,title,status,code,detail,traceId,errors?[]}`; a stable `code` comes from
  `@erp/contracts/errorCodes`, and an unexpected exception is reported as `INTERNAL`
  without leaking the message (asserted in `all-exceptions.filter.spec.ts`).
- **Request context**: one `AsyncLocalStorage` store per request carrying `traceId`,
  `startTime`, and (from Phase 03) `auth`/`tenant`/`branchId`.
- **`packages/contracts`**: error codes, problem builder, pagination
  (`limit 1..200`, default 50, `offset`), allow-listed `parseFilters`
  (unknown filter → 400 `FILTER_NOT_ALLOWED`) and `parseSort` (`-column` syntax),
  the 74-entry permission registry, request-id helpers.
- **`packages/config`**: zod-parsed `env` (fails fast with the list of missing variables),
  the typed 16-key tenant-settings registry with defaults, baseline role seed.
- **`packages/database`**: Drizzle client factory, migration runner with SHA-256
  checksums and hard failure on drift, `migrate` / `migrate:down` / `roles` CLIs,
  `drizzle.config.ts` for draft generation only.
- **Health**: `GET /health/live` (no I/O) and `GET /health/ready` (pool ping), both
  `@Public()` so an orchestrator can reach them without a token.
- **Logging**: `nestjs-pino` with `REDACTED_LOG_PATHS` covering
  `password|passwordHash|current|new|token|accessToken|refreshToken|tokenHash|mfaCode|
  secret|key` plus `Authorization`/`Cookie` headers; verified by
  `logging-redaction.spec.ts` driving the real pino configuration.

## Deviations

- The Phase-02 `test:smoke` script only imported `@erp/config` and printed a message. It
  now boots the real `AppModule` from `dist/` and asserts `/health/live` = 200 and
  `/api/v1/me` without a token = 401 `UNAUTHENTICATED` as `application/problem+json`.
  That is what caught `HealthController` missing `@Public()`.
- The smoke check runs against compiled output, not `tsx`: NestJS constructor injection
  needs `design:paramtypes`, which only `tsc` emits (esbuild/`--experimental-strip-types`
  do not), so a source-level boot reports `Nest can't resolve dependencies`.

## Files

- Created: `apps/api/src/{main,bootstrap,app.module}.ts`, `common/{middleware,filters,
  interceptors,pipes}/*`, `database/*`, `health/health.controller.ts`, `config/*`,
  `request-context/*`, `openapi/export-openapi*.ts`; all of `packages/contracts/src`,
  `packages/config/src`, `packages/database/src`; the four `packages/*/tsconfig.json`.
- Modified: root `package.json` (`verify`, `build`, `openapi:export`),
  `tsconfig.base.json`, `eslint.config.mjs`, `apps/api/{package.json,tsconfig.json,
  vitest.config.ts}`, every `packages/*/package.json` (`main`/`types`/`exports`/`build`).

## Tests

- Unit: `all-exceptions.filter` (5), `request-id.middleware` (3),
  `zod-validation.pipe` (3), `logging-redaction` (3), `pagination` (6), `errors` (1),
  `tenant-settings` (6), `rls` (5) — 32.
- Entry point: `test:smoke` boots `AppModule` and asserts 2 routes.
- `pnpm run verify` → exit 0 (typegen, `tsc --noEmit`, `pnpm -r run lint`, build,
  smoke, `pnpm -r run test`, `openapi:export`).

## Follow-ups carried into later phases

- Coverage thresholds (`TESTING_STRATEGY §3`: changed lines ≥ 80%) are not enforced yet —
  the gate needs the coverage provider wired into CI.
- `IdempotencyInterceptor` stores keys in a process-local `Map` with a 24 h TTL
  (`TODO(phase:04)` in the source); it must move to the `idempotency_keys` table
  (`DATABASE_DESIGN §4`) so replays survive a restart and are shared across replicas.
