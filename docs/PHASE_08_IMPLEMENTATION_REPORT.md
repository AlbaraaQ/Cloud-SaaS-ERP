# Phase 08 Implementation Report

## Scope
Phase 08 introduces the tenant-scoped parties and subledger foundation for customers, suppliers, contacts, credit controls, balances, statements, and payment allocations.

## Implemented
- Added `parties`, `party_contacts`, and `payment_allocations` database schema.
- Added reversible migration `0005_parties.sql` and down migration.
- Enabled and forced tenant RLS for all phase tables.
- Added API module, service, and controller.
- Added party listing, lookup, creation, update, soft deletion, contacts with soft deletion, balances, statements, credit-limit checks, and allocation endpoints.
- Added open-balance deletion protection and allocation-overrun validation.
- Integrated `PartiesModule` into `AppModule` and exported schema through the database index.

## Verification
`pnpm run verify` passed: type generation, TypeScript, workspace lint, builds, API smoke test, workspace tests, and OpenAPI export.

## Verification update
- Credit-limit, balance, and allocation comparisons now use `decimal.js` string arithmetic; no financial comparison relies on JavaScript `number` precision.
- `pnpm run verify` passes: type generation, TypeScript, lint, builds, smoke, OpenAPI export, 35 test files, and 219 tests.

## Remaining production hardening
- Add database-backed integration tests for party RLS, allocation uniqueness/concurrency, and posted journal party foreign keys.
- Complete invoice/payment voucher provider integration in phases 10 and 12; this phase exposes the allocation service contract only.

Status remains `IN_PROGRESS` until the database-backed isolation/concurrency proofs are added and the phase hand-off is accepted.
