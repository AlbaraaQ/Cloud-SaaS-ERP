# PHASE_06_PROMPT — Catalog (items, categories, units, taxes, pricing)

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 06 of 23. SSOT `docs/`; frozen rules apply
(UUID v7, tenant+RLS, money numeric/decimal.js, soft delete master data, sequences,
audit). Legacy maps: `Items`, `ItemsCategory`(dual-id anomaly → single id),
`units`(no PK → fixed), `ItemUnits`(ratio), `Itembarcodes`, `ItemAlternativeCodes`,
`ItemPrices` history, `ItemDetails`, `ItemComponents`, `tax_groups`. Preserve legacy
features: multi-unit with conversion, per-unit barcode/prices, min/max stock,
max discount caps, weight-scale flag, POS visibility, Egypt GS1 fields, withholding
rate, category kitchen-print flags, kit/BOM components.

## 1. CURRENT PHASE
**#06 — Catalog**: sellable/purchasable things and their tax/price model used by
inventory (P09), sales (P10), purchases (P11).

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/DATABASE_DESIGN.md` §6 (+§5 price_lists) 4. `docs/DOMAIN_MODEL.md` §3
5. `docs/API_CONTRACT.md` §4 6. `docs/LEGACY_DATABASE_ANALYSIS.md` §4
7. `docs/REQUIRES_CONFIRMATION.md` RC-15/21/22 awareness. 8. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS (verify; else stop)
Organization module incl. price_lists table, files service, settings.

## 4. GOAL & SCOPE
### In scope
- Tables+RLS: `item_categories, units_of_measure, tax_groups, items, item_units,
  item_barcodes, item_alternative_codes, item_components, item_price_history,
  item_details`; wire `price_list_items.item_id → items`.
- Endpoints per API_CONTRACT §4 incl. nested units/barcodes/components, CSV import job
  (async via outbox→BullMQ `migration` queue? NO — use `notifications`? Use dedicated
  `reports-export`? → define `catalog-import` queue addition allowed; document).
- Domain rules: one default sale/purchase unit per item; ratio > 0; unique barcodes
  per tenant across items+units; kit components cannot contain self-recursive items;
  tax group rate snapshot fields on items for invoice-time defaulting (rate as of
  doc time lives on invoice lines later).
- Price history: append on price change via service (not triggers).
- Search: trigram on names/sku/barcode; `q=` support.
### Out of scope (DO NOT DO)
Stock quantities/valuation (P09) · invoices (P10/11) · serial/lot transactional logic
(P09 — flags `track_lot/track_serial` stored now) · vertical fields (fitment P22).

## 5. EXACT TASKS
1. Migrations + entities + RLS + indexes (trigram, category, barcode uniques partial
   WHERE deleted_at IS NULL).
2. CRUD controllers + nested routes per contract; PATCH via version (optimistic).
3. CSV import: presign file → job → row results report (max 5k rows) with per-row
   errors; uses same DTO validators as API.
4. Price history writer + read endpoint.
5. Components recursive-guard validation + explosion helper `explodeKit(itemId, qty)`
   service fn (unit-tested; consumed by P09/P10).
6. Barcode collision tests across tables; default-unit guard tests.
7. Isolation tests; import permission `catalog.import.execute`.
8. STATUS.md; check ADMIN master §3 vs delivered (extend if feature grew).

## 6. DATABASE IMPACT
+10 tables, FK to price_list_items added; all RLS; no stock columns on items
(balances belong to P09 stock_balances).

## 7. API IMPACT
API_CONTRACT §4 endpoints exactly; permissions `catalog.view`, `catalog.manage`,
`catalog.price.manage`, `catalog.import.execute` (seed migration).

## 8. SECURITY REQUIREMENTS
Import size/type caps; formulas/files sanitized (CSV injection escaping on export
`'=`-prefix rule); category/image file validation via files service.

## 9. TESTING REQUIREMENTS
Unit: ratio validations, kit recursion, barcode uniqueness, price history append,
CSV row mapper. Integration: each endpoint + import happy/error path. Isolation proofs.

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md; module README (unit math examples); reconcile API_CONTRACT if shape drift
(change process); DATABASE_DESIGN untouched unless column-level fix needed.

## 11. ACCEPTANCE CRITERIA
- Contract endpoints pass; imports report row-level errors correctly.
- Multi-unit item with distinct barcodes/prices persists & resolves.
- Exploding a 2-level kit returns leaf components with correct multiplied qty.

## 12. DEFINITION OF DONE
verify green · classes of tests present · docs updated · protocol §8 report.

## 13. DELIVERABLES
Catalog module complete (schema/API/services/import) + docs updates.

## 14. DO NOT DO
Stock/inventory quantities · sales/purchase tables · tax calculation on documents ·
changing price list strategy from contract.
