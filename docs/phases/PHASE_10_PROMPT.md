# PHASE_10_PROMPT — Sales Invoicing

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 10 of 23. Frozen rules active. Sales invoices are
real financial documents: posting drives journal (PostingEngine) + stock
(InventoryLedger) atomically, one tx; posted ⇒ immutable; void ⇒ reversal + reverse
stock entries. Legacy parity requirements (from `Inv/Inv_Sub`): kinds
sale/sale_return/credit_note/debit_note; multi-tender payments (cash/card/bank/split)
with cash_location; price_includes_vat flag; line discounts + invoice discount;
additions/insurance/additional costs; VAT + extra tax + withholding; walk-in cash
customer (name/mobile per tenant rules — legacy `CashCustomerName/Mobile`,
`CashCustRequire`, `CustMobileRequire`); salesman + sales_emp; per-employee/customer
discount caps; credit-limit enforcement (parties service); ZATCA data columns
present from day one (uuid/hash/qr/status — engine in P13); reference-linked returns;
per-branch/type numbering (BL-1); snapshot InvProfit/InvCost fields kept
(`cost_total`, `profit`).

## 1. CURRENT PHASE
**#10 — Sales Invoicing**: full sales document lifecycle + payments-on-invoice +
adjustment notes + offers (lite).

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/DATABASE_DESIGN.md` §10 4. `docs/DOMAIN_MODEL.md` §4–§6
5. `docs/ACCOUNTING_ARCHITECTURE.md` §4 (sale patterns) 6. `docs/API_CONTRACT.md` §8
7. `docs/LEGACY_BUSINESS_LOGIC.md` BL-5, BL-2 8. `docs/LEGACY_DATABASE_ANALYSIS.md` §1
9. `docs/REQUIRES_CONFIRMATION.md` RC-01/02/03/07 (formula tolerance: see task 3 rule).
10. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS (verify; else stop)
Parties+allocations (`parties.allocate` providers hook), Inventory ledger service,
PostingEngine, posting profiles, catalog, files, seqities.

## 4. GOAL & SCOPE
### In scope
- Tables+RLS: `sales_invoices, sales_invoice_lines, invoice_payments,
  sales_adjustment_notes, offers, offer_items, offer_parties`, `salesmen` (kept per
  ADR-009 open part default).
- Endpoints per API_CONTRACT §8: CRUD(draft), post, void, payments add, return-from,
  print data endpoint (structured JSON; PDF artifact is P14), notes CRUD+post,
  offers CRUD + evaluation service hooks at pricing time.
- Pricing/Totals engine (`SalesTotalsCalculator`): line net = qty×price − discount;
  tax per line (group rate; inclusive/exclusive mode); extras; invoice discount and
  its pro-rata allocation for cost purposes (BL-5/BL-3); rounding HALF_UP to currency
  minor units; explicit tolerance guard: computed total must match Σlines within
  minor unit — else 422 `TOTAL_MISMATCH` (register code).
- Posting: profile resolution (fallback chain), AR/cash/card split lines, VAT out,
  sales/sales_return by kind, COGS optional per profile `post_cogs` (default true),
  stock out/in via ledger hints (outAtAvg / return-in at original line cost),
  credit-limit assertion, salesman capture, payment_status derivation from
  allocations/payments.
- Returns: require reference unless tenant setting allows unlinked; reverse-effects
  verified in tests.
- Offers: target qty/value, per-item %/value, validity window; application at line
  pricing (document precedence: price_list → offer).
### Out of scope (DO NOT DO)
ZATCA submission (P13 — columns + status machine only) · POS tables/order types (P19) ·
installments (P21) · PDF rendering (P14) · customer portal exposure (P18).

## 5. EXACT TASKS
1. Migrations+RLS+indexes; sequences doc types `sales_invoice`,`sales_return`,
   `credit_note`,`debit_note`.
2. TotalsCalculator with exhaustive unit/property tests (inclusive/exclusive VAT,
   mixed discounts, WHT, extra tax, 4dp).
3. Invoice service + endpoints; draft-only PATCH; post flow tx (journal+ledger+
   status+number) idempotent; void with reversal + return-of-stock; payment_status sync.
4. Payments endpoint multi-tender + cash_location validation + credit (`pay_method=
   credit`) creates AR only; allocations register via parties providers.
5. Returns (linked/unlinked per settings) + reference integrity tests.
6. Adjustment notes post→journal per profile slots.
7. Offers engine + precedence tests.
8. `salesmen` CRUD-lite + link columns (commission calc lands P14 reports).
9. Register allocation provider implementations into parties service (real ones now).
10. Status machine + guards tests per DOMAIN_MODEL §5; audit coverage; isolation tests.
11. STATUS.md; ADMIN master §7 alignment; API_CONTRACT reconcile.

## 6. DATABASE IMPACT
+8 tables RLS; FKs to parties/branches/warehouses/journal_entries; polymorphic stock
doc refs only (per DOMAIN rules); ZATCA columns nullable.

## 7. API IMPACT
API_CONTRACT §8 implemented exactly; permissions `sales.view, sales.invoice.create,
post, void, pay, discount.override, return.create, offer.manage, salesman.manage` (seed).

## 8. SECURITY REQUIREMENTS
Discount caps enforced server-side w/ override perm (`sales.discount.override`);
credit-limit override perm; payments audited; idempotency on post/pay; tamper-proof
snapshot fields recomputed server-side (client totals advisory → `TOTAL_MISMATCH`).

## 9. TESTING REQUIREMENTS
Totals property tests; posting integration (journal balanced + subtype correct +
stock deltas); void/reversal; payments/allocations bounds; returns stock-back;
concurrent post numbering uniqueness; isolation proofs.

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md; module README (lifecycle diagram + profile slots used); mark BL-5 parity
notes; ADMIN §7 section alignment.

## 11. ACCEPTANCE CRITERIA
- Golden invoice fixture: totals exactly as hand-calc; post produces balanced journal
  with expected slots + ledger qty/cost effects; profit snapshot equals expected.
- Voided invoice reverses all effects; second void attempt → 409.
- Linked return stock-in at original cost proven; credit-limit block + override path.

## 12. DEFINITION OF DONE
verify green · test classes complete · docs updated · protocol §8 report.

## 13. DELIVERABLES
Sales module end-to-end + totals engine + payments + notes + offers-lite + docs.

## 14. DO NOT DO
E-invoice submission · PDFs · POS tables · purchases · changing posting engine API ·
auto-applying legacy unknown enum guesses (invoice kinds are the new canonical set —
legacy mapping happens in P15).
