# PHASE_11_PROMPT — Purchases

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 11 of 23. Frozen rules active. Purchases mirror
sales but increase stock and drive **landed cost**: additional invoice costs
(legacy `InvoiceCost`, freight/insurance etc.) are allocated to lines (qty|value
method) and fed into the moving-average pool on post (BL-3/BL-6). Supplier is
mandatory (`parties kind=supplier`; RC-08 legacy ambiguity resolved by requiring
supplier here). VAT input, withholding support, supplier reference no/date, returns
to supplier with stock-out at pool rules (BL-3 special discount-allocation cases are
covered by the ledger hints from P09).

## 1. CURRENT PHASE
**#11 — Purchases**: purchase invoices/returns, additional costs & landed-cost
allocation, supplier payments hooks.

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/DATABASE_DESIGN.md` §11 4. `docs/LEGACY_BUSINESS_LOGIC.md` BL-3, BL-6
5. `docs/API_CONTRACT.md` §8 (mirror part) 6. `docs/ACCOUNTING_ARCHITECTURE.md` §4
7. `docs/REQUIRES_CONFIRMATION.md` RC-08/15. 8. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS (verify; else stop)
Sales module (shared invoice patterns/services — reuse totals patterns, not tables),
inventory ledger, posting engine, parties.

## 4. GOAL & SCOPE
### In scope
- Tables+RLS: `purchase_invoices, purchase_invoice_lines, purchase_invoice_costs`.
- Endpoints mirror sales: CRUD(draft)/post/void/payments + costs management +
  landed-cost preview endpoint `POST /purchase-invoices/preview-landed-cost`.
- LandedCostCalculator: allocate Σ additional costs per method across lines
  (pro-rata, HALF_UP, remainder to largest line — deterministic rule documented),
  effective unit cost feed for stock-in hints.
- Posting flow: stock-in via `inWithCost(lineCost+allocated)`; journal:
  inventory + vat_input debit / payable (or cash) credit; additions booked to cost or
  expense per profile mapping (`purchase_costs_to_inventory` bool slot).
- Returns: stock-out per BL-3 rules; journal reversal pattern incl. vat_input neg.
- Reuse patterns from sales where possible via a shared internal
  `InvoiceMath` utility (extract into `packages/contracts`/shared service without
  breaking module boundaries: place in inventory? NO — place pure math in
  `packages/contracts/invoice-math.ts` used by both modules).
### Out of scope (DO NOT DO)
Supplier payments vouchers (P12) · reports (P14) · approval workflows beyond status
machine · EDI/import files.

## 5. EXACT TASKS
1. Migrations+RLS; sequences `purchase_invoice/purchase_return`.
2. Extract shared invoice math into contracts package (move, don't duplicate; update
   sales imports — allowed refactor listed here explicitly).
3. Costs CRUD + allocation preview + post integration into ledger hints.
4. Posting + void + payments endpoints incl. payable allocations.
5. Tests: allocation determinism (incl. remainder rule), avg-pool effect parity with
   legacy formula cases, returns pool behavior, journal shape, concurrency numbering,
   isolation.
6. STATUS.md; ADMIN master §8 alignment; BL-6 marked implemented.

## 6. DATABASE IMPACT
+3 tables RLS; FK to parties/warehouses/journals; no sales-table coupling.

## 7. API IMPACT
API_CONTRACT §8 mirror endpoints implemented; permissions `purchase.view,
purchase.invoice.create/post/void/pay, purchase.cost.manage` (seed).

## 8. SECURITY REQUIREMENTS
Cost-manage permission for editing additional costs after draft; audit on costs;
idempotency on post/pay.

## 9. TESTING REQUIREMENTS
Unit (allocator incl. rounding edge cases 3-decimals currencies), property (Σallocated
== Σcosts), integration endpoints, isolation, parity fixture vs BL-3 numbers.

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md; module README (landed cost worked example); API_CONTRACT reconcile.

## 11. ACCEPTANCE CRITERIA
- {2 lines, 3 costs, value-method} fixture: allocation equals hand-calc; stock avg
  matches legacy engine parity numbers; journal balanced with correct subtypes.
- Return at reduced pool verified; concurrent posts unique numbers (per branch).

## 12. DEFINITION OF DONE
verify green · test classes · docs · protocol §8 report.

## 13. DELIVERABLES
Purchases module complete + shared invoice-math extraction + docs.

## 14. DO NOT DO
Treasury vouchers · supplier portal · changing inventory engine hints semantics ·
new allocation methods beyond qty|value.
