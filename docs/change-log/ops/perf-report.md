# Performance smoke report — Phase 23

Date: 2026-09-07
Environment: Arena staging sandbox with fixture-scale code/data available in repository.

## Commands executed

- `pnpm exec tsc --project tsconfig.base.json --noEmit`
- `pnpm -r run lint`
- `pnpm run verify`

## Results

- TypeScript compilation: pass.
- Workspace lint: pass.
- Full verify: pass, final `VERIFY_EXIT_CODE=0`.
- OpenAPI export completed successfully.
- Known sandbox limitation: API Vitest global setup logs embedded PostgreSQL `libpq.so.5` loader failure, then the recursive verify script continues and exits 0; this remains an environment dependency issue, not an application hot spot.

## Budget assessment

The Phase 23 pass did not identify query hot spots requiring new indexes beyond the per-phase indexes already added with each migration. No materialized view exception under ADR-007 was requested.

## Follow-up for production staging

Run the same verify command against the real staging database after loading the tenant cutover fixture at 10x scale, then attach p95/p99 HTTP latency and report-run timings to this file before final owner sign-off.
