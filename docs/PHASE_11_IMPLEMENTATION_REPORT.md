# Phase 11 Implementation Report

## Current status

`COMPLETE` as of 2026-09-07.

## Delivered

- Purchases module with service, controller, Nest module registration, and tenant-scoped routes.
- Tables `purchase_invoices`, `purchase_invoice_lines`, and `purchase_invoice_costs` with RLS, indexes, foreign keys, and reversible migrations.
- Purchase invoice draft creation/update, read/list, post, void, payment hook, landed-cost preview, and costs management endpoints.
- Supplier-required validation: purchase documents require a `parties.kind` of `supplier` or `both`.
- Shared invoice math extended with a deterministic `allocateLandedCost()` calculator.
- Landed cost allocation supports `qty` and `value`, HALF_UP rounding, and deterministic largest-line remainder assignment.
- Posting integrates with the Phase 09 inventory ledger: purchases feed stock-in `inWithCost` with landed/effective unit cost; returns use stock-out `outAtAvg`.
- Posting integrates with Phase 07 accounting through journal posting inside the same tenant-bound transaction when journal lines are supplied.
- Supplier payment hook records AP allocations through the existing party allocation ledger pending Phase 12 treasury vouchers.
- Purchase permissions added: `purchase.invoice.pay` and `purchase.cost.manage`.
- OpenAPI contracts regenerated.

## Verification

`pnpm run verify` passes with type generation, TypeScript, lint, build, smoke checks, workspace/API tests, and OpenAPI export.

## Notes

Treasury vouchers, supplier portal, approval workflows, reporting, and import/EDI remain intentionally out of scope for Phase 11.
