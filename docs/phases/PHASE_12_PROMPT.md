# PHASE_12_PROMPT — Treasury (vouchers, cheques, transfers, shift close)

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 12 of 23. Frozen rules active. Treasury unifies
legacy families into ONE document `vouchers` (ADR-010): `Receipts` +
`SandQ/SandD/SandQD/SandSD/SandVAT` (kinds: receipt/payment; subtypes party/account/
expense/salary/vat/other). Cheques have lifecycle states. Shift close unifies
`CasherClosed/_Sub`, `CahierClosedAndroid`, `CloseShiftCustomer`, `Check_Close`,
denomination counting from `Rekaba`. Cash movement posts journals through the engine;
AR/AP settlement via parties allocations (P08).

## 1. CURRENT PHASE
**#12 — Treasury & Banking**: money in/out documents, cheque lifecycle, inter-location
transfers, cashier shift close, expense types.

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/DATABASE_DESIGN.md` §12 (+§5 balances) 4. `docs/DOMAIN_MODEL.md` §4–§5
5. `docs/LEGACY_BUSINESS_LOGIC.md` BL-7 6. `docs/LEGACY_DATABASE_ANALYSIS.md` §3
7. `docs/API_CONTRACT.md` §9 8. `docs/ACCOUNTING_ARCHITECTURE.md` §4
9. `docs/REQUIRES_CONFIRMATION.md` RC-18/19. 10. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS (verify; else stop)
Parties allocations, sales/purchases invoices (open items), posting engine, org cash
locations + balances table, sequences.

## 4. GOAL & SCOPE
### In scope
- Tables+RLS: `vouchers, cash_transfers, expense_types, shift_closes,
  shift_close_lines, cash_count_lines` (allocations reused from P08).
- Endpoints per API_CONTRACT §9: voucher CRUD(draft)/post/void, cheque transitions,
  transfers create/send/receive, expense types CRUD, shift open/current/close/history,
  cash location balances (writer now wired: balances updated in same tx on post).
- Voucher post: journal per profile (cash/bank debit | credit counter: party AR/AP
  line tagged party_id when party present; account/expense per counter account;
  VAT split when present); allocations auto/manual with bounds from P08.
- Cheque lifecycle: pending→cleared|bounced|(collected) with journal movements via
  clearing account slot when configured (default: same-location memo only —
  profile-driven; document).
- Shift close: compute expected totals per method from posted sales/vouchers between
  open→close timestamps (branch/user scope), counted input by denominations,
  `diff = counted − expected` recorded (never auto-zero), report JSON + stored summary
  (HTML render is P14 artifact), per-party deferred totals lines, checks linking
  invoices closed within shift.
### Out of scope (DO NOT DO)
Bank reconciliation import/files · cash-transfer FX conversions (same-currency only;
multi-currency vouchers allowed with fx_rate on base currency) · payroll-specific
voucher UI (P20 uses vouchers subtype salary) · POS close integration (P19 reuses
shift close).

## 5. EXACT TASKS
1. Migrations+RLS; sequences `receipt_voucher/payment_voucher/cash_transfer`.
2. Voucher services+endpoints incl. allocations calls; enforce Σ-bounds; VAT split.
3. Cheque state machine + transition endpoints + tests (invalid transitions 422).
4. Cash transfers with send/receive + journal pair + balances updates; receive-partial forbidden (atomic).
5. Shift close engine: totals query implementation + counted/diff logic + per-party
   lines + print-data endpoint (structured); only one open shift per (branch,user) DB
   partial unique index on open state.
6. Balances writer integration + reconcile function `recalcCashBalance(locationId)`
   (repair path; used by P15).
7. Tests: posting patterns per subtype, allocations bounds, cheque transitions,
   transfer atomicity, shift diff math with fixture docs, one-open-shift invariant,
   isolation proofs, audit coverage.
8. STATUS.md; ADMIN master §9 alignment; BL-7 mark implemented.

## 6. DATABASE IMPACT
+6 tables RLS; FK cash_locations/journals; partial unique for single-open-shift;
balances wires.

## 7. API IMPACT
API_CONTRACT §9 implemented exactly; permissions `treasury.view, voucher.create/post/
void, cheque.clear, transfer.manage, shift.close, expensetype.manage` (seed).

## 8. SECURITY REQUIREMENTS
Cheque clear restricted; shift close restricted + audited; vouchers idempotency on
post; amounts server-validated vs allocations; no negative balances unless configured.

## 9. TESTING REQUIREMENTS
Unit (diff math, allocation bounds, state machines) · integration all endpoints ·
concurrency (double close attempt) · isolation · golden fixture shift totals.

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md; module README (voucher subtype → journal pattern table); API_CONTRACT
reconcile; LEGACY mark for BL-7.

## 11. ACCEPTANCE CRITERIA
- Fixture day (3 sales cash/card/credit + 2 vouchers + transfer) closes shift with
  exact expected JSON totals incl. per-party deferred; journal links complete.
- Bounced cheque reverses its effect per profile; second transition blocked.
- Cross-tenant/foreign-branch FK attempts rejected (isolation suite green).

## 12. DEFINITION OF DONE
verify green · test classes · docs · protocol §8 report.

## 13. DELIVERABLES
Treasury module complete + shift engine + balances wiring + docs.

## 14. DO NOT DO
Invoices changes · notification templates · PDF/HTML artifacts (P14) · bank file
imports · legacy Sand* table imports (P15).
