# PHASE_09_PROMPT — Inventory (ledger, valuation, adjustments, transfers, lots/serials)

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 09 of 23. Frozen rules apply. Inventory truth is an
APPEND-ONLY `inventory_transactions` ledger + transactional `stock_balances` cache
(qty & moving average cost) — replaces legacy on-the-fly TVFs and the unreliable
`ProductStocks` cache (BL-3, RC-20). Valuation formula MUST match legacy parity:
avg = (ΣcostIn − ΣcostOut ± allocated invoice discounts) / Σqty, discount allocated
pro-rata by line value (BL-3 formula quoted in LEGACY_BUSINESS_LOGIC). Negative stock
policy per tenant setting `inventory.allow_negative` (default false; legacy
`SaleByMinus`). Serial/lot tracking from legacy `InvoiceItemDetail/ItemSerialNo`
(`InvertoryImpact` 1=in/2=out).

## 1. CURRENT PHASE
**#09 — Inventory**: the ledger engine and stock documents; sales/purchases (P10/P11)
record movements ONLY through `InventoryLedger.record()`.

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/DATABASE_DESIGN.md` §9 4. `docs/DOMAIN_MODEL.md` §4–§6
5. `docs/LEGACY_BUSINESS_LOGIC.md` BL-3 (+InventoryCost SPs analysis)
6. `docs/API_CONTRACT.md` §7 7. `docs/ACCOUNTING_ARCHITECTURE.md` §4 (inv journals)
8. `docs/REQUIRES_CONFIRMATION.md` RC-14/15/20. 9. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS (verify; else stop)
Catalog (items/units/flags), accounting engine (for adjustment postings), org
warehouses, isolation harness.

## 4. GOAL & SCOPE
### In scope
- Tables+RLS: `inventory_transactions, stock_balances, stock_adjustments(+lines),
  stock_transfers(+lines), item_lots, item_serials, invoice_item_attributes`.
- `InventoryLedger.record(docRef, lines[])` — tx-bound, validates items/warehouses,
  computes base qty via unit ratio, updates balances with avg-cost math
  (purchase-in increases pool; sale-out maintains avg; returns handled per doc kind
  DON'T hardcode: accept `costing` hints from callers {inWithCost, outAtAvg,
  returnAtOriginalCost?}, document each), guards negative per setting.
- Queries: levels, movements, valuations-as-of (replay-safe: compute at date from ledger).
- Stock adjustments w/ approval → ledger + journal via PostingEngine (shrinkage slots).
- Transfers with send/receive and partial receive (pending qty view), avg cost carries.
- Lots/serials: reserves/validates on record; serial status machine from DOMAIN_MODEL §5;
  `invoice_item_attributes` storage service (attributes write API used by P10).
- Engine property tests: never negative (when disallowed), value conservation on
  transfers, avg-cust parity vs legacy formula fixtures.
### Out of scope (DO NOT DO)
Invoice integration (P10/P11 call you) · stock reports polish (P14 uses your queries) ·
expiry alerts job (P14/notifications wiring later; queries available now) · kit auto
explosion postings (P10 uses catalog explodeKit then calls ledger per leaf).

## 5. EXACT TASKS
1. Migrations+RLS+indexes ((item,warehouse,occurred_at), (lot), serial unique).
2. Ledger service with costing hints + setting guard; same-tx balance updates (prove
   via concurrency test two posts interleave correctly).
3. Rebuild utility `recomputeBalances(warehouse?, item?)` (admin-repair tool; migration
   P15 relies on it) with ledger-replay verification test.
4. Adjustments module + approval workflow + posting link (inventory/shrinkage profile
   slots) + tests.
5. Transfers full lifecycle + tests incl. partial receive, cancel, cost carry.
6. Lots/serials services + expiry query + serial-availability guard tests; attributes
   service API.
7. Endpoints per API_CONTRACT §7; permissions `inventory.view/adjust/transfer/approve/
   receive/negative.override` (seed).
8. Parity fixtures: implement BL-3 formulas against 3 numeric cases extracted from
   legacy SP text (given in prompt appendix below) and assert identity.
9. STATUS.md; ADMIN master §6 alignment.

### Appendix (parity cases — from legacy InventoryCost/ItemAvrgCost)
- In 10 @ 100, in 5 @ 110 → avg 103.3333 (4dp HALF_UP) ; out 3 → pool (12×avg−3×avg)/9.
- Invoice discount allocation: lines L1 8×100, L2 2×100; minus=50 → L1 alloc 40, L2 10.
- Out (return-to-supplier types 1,4,8,9 in legacy list) uses exchange_price pool exit.

## 6. DATABASE IMPACT
+8 tables RLS; no FK from ledger to sales/purchase docs (doc_type/doc_id polymorphic,
service-validated — documented mirroring DOMAIN_MODEL §4; do not add cross FKs).

## 7. API IMPACT
API_CONTRACT §7 endpoints exactly; no contract changes expected.

## 8. SECURITY REQUIREMENTS
Negative-stock override permission; approve restricted; audit approve/receive actions;
ledger immutable at DB level (revoke updates; only deletes for migration role via
run-scoped jobs — guarded).

## 9. TESTING REQUIREMENTS
Property-based invariants (balances == replay; conservation of value on transfer;
avg formula properties) + integration endpoints + isolation + concurrency tests.

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md; `modules/inventory/README.md` documenting the 3 costing hints with numeric
examples; LEGACY_BUSINESS_LOGIC BL-3 marked "implemented parity-tested Phase 09".

## 11. ACCEPTANCE CRITERIA
- Parity fixtures pass exactly (4dp HALF_UP).
- Concurrent postings keep balances correct under test hammer (64 parallel).
- Transfer partial receive + adjustment approval produce correct ledger + journal link.

## 12. DEFINITION OF DONE
verify green · all test classes · docs · protocol §8 report.

## 13. DELIVERABLES
Inventory module complete + ledger service contract + parity tests + docs.

## 14. DO NOT DO
Sales/purchase tables · alternative valuation methods (FIFO wait ADR) · cached totals
outside stock_balances · changing avg formula without CR+ADR.
