# PHASE_14_PROMPT — Reporting & Analytics

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 14 of 23. Frozen rules. Reports read from
immutable ledgers/journals — NO cached totals (ADR-007). Legacy parity list comes
from legacy SPs/TVFs: SalesByDay, GetSalesByCategory/Items/OrderType/PaymentMethods,
MonthlySales, ItemStockLimits, ItemsExpirationStock, serial/batch summaries,
InventoryCost(-ForBalanceSheet), AccountTotalBalance, GetAccountStatement, cashier
shift reports, AR/AP aging, VAT return. Async export (CSV/XLSX/PDF) via jobs.
Performance budgets per TESTING §5.

## 1. CURRENT PHASE
**#14 — Reporting**: parametrized read-side report catalog + async exports + print
artifacts (invoice/shift HTML→PDF).

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/API_CONTRACT.md` §11 4. `docs/LEGACY_BUSINESS_LOGIC.md` BL-3/5/7/9 (+ SP list)
5. `docs/ACCOUNTING_ARCHITECTURE.md` §6 6. `docs/DATABASE_DESIGN.md` §16
7. `docs/TESTING_STRATEGY.md` §5 8. `docs/TARGET_ARCHITECTURE.md` §7
9. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS (verify; else stop)
Core modules complete (accounting/inventory/sales/purchases/treasury/einvoice).

## 4. GOAL & SCOPE
### In scope
- Reporting module with registry: each report = key, param zod schema, SQL builder
  via Drizzle, row types, CSV/XLSX renderer, chart config hint for UI.
- Report keys v1 (exact contract list): sales-by-day, sales-by-category, sales-by-item,
  sales-by-payment, sales-by-ordertype (ordertype column empty until P19 — document),
  monthly-sales, inventory-valuation, item-movement, stock-limits, expiry-report,
  serial-tracking, batch-tracking, ar-aging, ap-aging, party-statement, vat-return,
  trial-balance, general-ledger, profit-loss, balance-sheet, cashier-shift.
- Async export jobs (`reports-export` queue) → files artifact + notification; PDF via
  HTML templates (invoice print, shift close print) rendered server-side.
- Statement parity mode: account-statement output shape compatible with legacy
  `GetAccountStatement` columns for migration comparison tooling (flag `legacyShape=1`).
- Optional materialized views ONLY IF budget fails: opening ADR-007 requires profiling
  note + CR first — default: computed queries with proper indexes; add composite
  indexes as needed (allowed without ADR).
### Out of scope (DO NOT DO)
Dashboards/charts rendering (P17 UI consumes endpoints) · BI/warehouse · scheduled
subscriptions · report-designer user UI.

## 5. EXACT TASKS
1. Report registry + param validation + error codes (`FILTER_NOT_ALLOWED` reuse).
2. Implement all v1 keys with pagination where applicable; SQL review for N+1.
3. Export pipeline: enqueue → render → S3 → notification; idempotent (params hash).
4. Invoice print HTML template (ar RTL default) + PDF generator; shift report HTML.
5. Budget harness: time TB & inventory-valuation on fixture volumes; record numbers.
6. Golden tests: fixture books → expected rows for TB, sales-by-day (with returns
   subtraction — legacy behavior `SaleTotal − ReturnSaleTotal`), aging buckets.
7. Isolation tests incl. exports listing (user sees own tenant artifacts only).
8. STATUS.md; ADMIN master §11 alignment (registry keys documented).

## 6. DATABASE IMPACT
Read-only + optional indexes (list them in report); artifacts use `files` table.

## 7. API IMPACT
Implements API_CONTRACT §11 exactly; permissions `reporting.<key>.view` seeded per key;
`reporting.export.execute`.

## 8. SECURITY REQUIREMENTS
Per-key permissions; exports access-checked; no cross-tenant artifacts; HTML sanitize
inputs used in templates.

## 9. TESTING REQUIREMENTS
Golden datasets per financial key; param validation tests; export job tests; budget
measurements recorded; isolation.

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md; module README (how to add a report key recipe); CUSTOMER/ADMIN masters §11
reconciled.

## 11. ACCEPTANCE CRITERIA
- Every key in registry returns contract rows on fixtures and matches hand-calcs.
- Export flow delivers file→notification end-to-end.
- Budgets measured & recorded (not necessarily all green at scale — note exceptions).

## 12. DEFINITION OF DONE
verify green · golden tests green · docs · protocol §8 report.

## 13. DELIVERABLES
Reporting module + export jobs + print templates + registry docs + measurements.

## 14. DO NOT DO
New business facts/tables · modifying ledgers · cached balances on masters · UI pages.
