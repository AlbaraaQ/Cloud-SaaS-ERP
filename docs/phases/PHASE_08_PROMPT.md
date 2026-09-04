# PHASE_08_PROMPT — Parties & AR/AP

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 08 of 23. SSOT rules frozen (UUID v7, tenant+RLS,
numeric money/decimal.js, soft delete, audit, sequences). Legacy maps: `Customers`,
`Suppliers`, `Owners`, `PM_Contractor`, `VATClients` → unified `parties` (ADR-009;
flags is_owner/is_contractor), `DealPersons`→`party_contacts`, `Customers.AccountCode`
party↔COA link (receivable/payable subtype enforced), `maxdepit`→credit_limit,
`Pricing`→price_list; payment settlement becomes explicit `payment_allocations`
(legacy had none — RC-09). ZATCA national-address fields preserved in `address jsonb`.

## 1. CURRENT PHASE
**#08 — Parties & AR/AP**: who we trade with, their balances, settlement links.

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/DATABASE_DESIGN.md` §8 4. `docs/DOMAIN_MODEL.md` §3–§5
5. `docs/ACCOUNTING_ARCHITECTURE.md` §6 (subledger reads)
6. `docs/API_CONTRACT.md` §6 7. `docs/LEGACY_DATABASE_ANALYSIS.md` §5
8. `docs/REQUIRES_CONFIRMATION.md` RC-08/09/22/23. 9. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS (verify; else stop)
Accounting core (accounts, journal lines with party_id tag, statements), catalog not
required, organization+settings present.

## 4. GOAL & SCOPE
### In scope
- Tables+RLS: `parties, party_contacts, payment_allocations`.
- Endpoints per API_CONTRACT §6: CRUD (kind filter), contacts, balance, statement,
  allocations create (voucher-side full wiring arrives P12 — here allocation service +
  invoice-kind generic hooks validated by DTO contract tests with fake invoice ids
  guarded behind service interfaces).
- Balance computations from posted journal lines tagged party_id on receivable/payable
  subtype accounts; open-items via allocations; credit-limit check service fn
  `assertCreditAvailable(partyId, newAmount)` consumed later (P10).
- Party code from sequence `party` scope; national address jsonb schema typed.
- Merge guard: cannot soft-delete/merge parties with nonzero open balance (assert + test).
### Out of scope (DO NOT DO)
Vouchers/invoices themselves · aging buckets report (P14 reports via these primitives
— you DO provide `ArAp.partyBalance` + open items query used by P14) · portal views.

## 5. EXACT TASKS
1. Migrations+RLS; unique tax_no partial (warn-only policy at service: flag but allow
   per RC — test both messages) — implement as non-blocking duplicate warning.
2. Party CRUD with zod DTOs incl. address schema; kind flags validation
   (customer/supplier mutually coherent booleans).
3. Contacts CRUD.
4. `ArAp` read service: partyBalance(partyId, asOf?) {receivable, payable, open[]}
   using posted lines subtype join — parity test against handcrafted journal fixtures.
5. `payment_allocations` service: allocate(voucherId?, invoiceRef{kind,id}, amount)
   with Σ-bounds checks (invoice/voucher totals provider interfaces injected; fake
   providers in tests — P10/P12 implement real ones).
6. Statement endpoint → lines + prev-balance + running balance (same shape as
   LedgerQuery account statement but party-scoped via linked account or party tag).
7. Credit-limit service + unit property tests (never exceed; boundary equality).
8. Isolation + soft-delete + audit coverage tests.
9. STATUS.md; ADMIN master §5 alignment.

## 6. DATABASE IMPACT
+3 tables RLS; FKs: parties→accounts (nullable), contacts→parties, allocations→
voucher nullable + invoice polymorphic (service-validated; no cross-module FK
physically — documented decision ties to TARGET_ARCHITECTURE §4 rule 3; note here).

## 7. API IMPACT
API_CONTRACT §6 implemented; permissions `parties.view, parties.manage,
parties.allocate, parties.creditlimit.override` (seed).

## 8. SECURITY REQUIREMENTS
Tax numbers & national ids masked in logs; export of statements respects branch scope;
credit override requires dedicated permission.

## 9. TESTING REQUIREMENTS
Unit (balance math, allocation bounds, address schema) · integration endpoints ·
isolation · determinism: fixture journals→expected open items snapshot.

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md; module README (how P10/P12 register allocation providers); API_CONTRACT
reconcile if drift.

## 11. ACCEPTANCE CRITERIA
- Fixture: posted AR lines + two allocations → balance/open-items exactly as hand-calc.
- Credit-limit enforcement & override-permission path proven in tests.
- Cannot delete party with open balance (422 documented code).

## 12. DEFINITION OF DONE
verify green · tests per classes · docs updated · protocol §8 report.

## 13. DELIVERABLES
Parties module complete + ArAp service contract used by later phases + docs.

## 14. DO NOT DO
Invoices/vouchers · PDFs · reports UI · direct FKs into sales/treasury tables ·
legacy data import.
