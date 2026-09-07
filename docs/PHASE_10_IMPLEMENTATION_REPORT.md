# Phase 10 Implementation Report

## Current status

`COMPLETE` as of 2026-09-07.

## Delivered

- Shared invoice calculation engine with deterministic decimal arithmetic.
- Sales invoice schema and migrations `0007_sales.sql` and `0008_sales_returns.sql` with reversible down migrations.
- Sales module, service, controller, and application registration.
- Draft invoice creation and updates.
- Sequence-backed posting numbers for sales invoices, returns, credit notes, and debit notes.
- Atomic sales posting transaction: inventory ledger movements, accounting journal posting, and invoice status/number updates now share a single tenant-bound transaction.
- Posting and voiding with immutable posted invoices.
- Idempotent invoice payments with positive-amount and balance-overpayment checks.
- Reference-linked return creation with source-status guard, return-of-return guard, positive quantity validation, and remaining source-quantity enforcement across prior returns.
- Print data, adjustment-note lifecycle, offers, offer target/validity evaluation, and salesmen endpoints.
- Tenant-scoped queries and permission guards on all sales routes.

## Verification

`pnpm run verify` passes with type generation, TypeScript, workspace lint, build, API smoke checks, all workspace/API tests, and OpenAPI export.
