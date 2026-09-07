# Phase 19 Implementation Report

## Current status

`COMPLETE` as of 2026-09-07.

## Delivered

- Restaurant POS backend pack gated by tenant setting `pack.pos`.
- POS schema and reversible migration `0015_pos_pack.sql` for `table_categories`,
  `dining_tables`, `order_events`, plus additive POS fields on sales invoices and line
  `modifiers` JSON.
- POS permissions: `pos.view`, `pos.operate`, `pos.priceoverride`,
  `pos.tables.manage`, `pos.config.manage`.
- `/pos/*` API for table categories, tables, open table, add items with modifiers,
  void item with reason, merge, split, send-to-invoice, and close.
- `send-to-invoice` composes a normal sales invoice through `SalesService` and allocates
  daily branch-scoped `pos_order:YYYY-MM-DD` sequence numbers.
- Reporting hook: `sales-by-ordertype` now reads POS `order_type` values from posted
  sales invoices.
- Admin POS page `/pos` covering floor map, order flow, kitchen print config, daily
  counters, merge/split, pay/close, and shift-close handoff.
- Module README, CR record, API/DATABASE/ADMIN/LEGACY_COMPAT documentation updates, and
  smoke unit coverage.

## Verification

`pnpm run verify` passes with type generation, TypeScript, lint, admin/customer builds,
API/migrator/package tests, smoke checks, and OpenAPI export.

## Notes

Hardware printers, delivery integrations, loyalty, and a waiter mobile app remain out of
scope. Core posting and totals logic were not forked; POS uses the public sales service.
