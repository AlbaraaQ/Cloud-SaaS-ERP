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
  - `pnpm run verify` passes: type generation, TypeScript, lint, builds, smoke, OpenAPI export, 36 test files, and 223 tests.
  - Added database-backed API proofs for tenant isolation, contact lifecycle, allocation-limit rejection, and permission enforcement in `apps/api/test/parties.spec.ts`.
  
  ## Phase hand-off
  - Phase 08 is complete. Invoice and payment-voucher provider integration remains intentionally owned by phases 10 and 12; this phase exposes the allocation service contract required by those providers.
