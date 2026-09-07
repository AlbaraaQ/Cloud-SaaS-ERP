# Phase 12 Implementation Report

## Current status

`COMPLETE` as of 2026-09-07.

## Delivered

- Treasury module with Nest service/controller/module registration.
- Tenant-scoped tables with RLS: `vouchers`, `cash_transfers`, `expense_types`, `shift_closes`, `shift_close_lines`, and `cash_count_lines`.
- Reversible migration `0011_treasury.sql` and `migrations/down/0011_treasury.down.sql`.
- Unified receipt/payment vouchers for customer, supplier, expense, account, salary, VAT, and other subtypes.
- Draft voucher create/update, post, void, list/read, and cheque transition endpoints.
- Cheque lifecycle with terminal state enforcement for `pending -> cleared|bounced|collected`.
- Cash-transfer create/send/receive lifecycle with same-currency atomic balance updates.
- Expense type create/list endpoints.
- Cashier shift open/current/history/close/print-data endpoints with one-open-shift database invariant per tenant/branch/user.
- Shift-close denomination count storage and `diff = counted - expected` summary calculation.
- Cash-location balance writer integration for voucher posting/voiding, cheque clearance/collection, and cash transfer send/receive.
- `recalcCashBalance()` repair-path endpoint stub for migration/reconciliation consumers.
- Treasury permissions for transfer and expense-type management.
- Module README documenting voucher subtype journal patterns and shift/balance behavior.

## Verification

`pnpm run verify` passes with type generation, TypeScript, lint, build, smoke checks, workspace/API tests, and OpenAPI export.

## Out of scope retained

Bank statement imports, FX cash transfers, payroll-specific UI, POS-specific close UI, and rendered PDF/HTML reports remain later-phase scope.
