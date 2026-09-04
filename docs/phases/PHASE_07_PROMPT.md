# PHASE_07_PROMPT — Accounting Core (COA, journals, posting engine, fiscal periods)

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 07 of 23 — the heart of the system. SSOT `docs/`;
frozen: UUID v7, tenant+RLS, numeric money + decimal.js HALF_UP, posted journals
IMMUTABLE (DB-enforced), corrections only by reversal entries, fiscal periods with
per-module locks, posting via config-driven posting profiles. Legacy maps:
`Accounts_Index`→accounts (drop cached totals RC-11), `Entry/Entry_sub`→journal tables
(state=1⇒posted), `AccountingPeriods`+`PeriodLocks`, `Cost_Center`, `SettingGeneral.*Acc`
→ posting profiles; statements must match `GetAccountStatement` Semantics.

## 1. CURRENT PHASE
**#07 — Accounting Core**: chart of accounts, fiscal calendar with module locks,
journal entry model + posting/void/reversal engine, cost centers, opening balances
batch posting, core statements (trial balance / general ledger / account statement),
and the **invariant test suite** T1–T10.

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/ACCOUNTING_ARCHITECTURE.md` (ALL — read twice) 4. `docs/DATABASE_DESIGN.md` §7
5. `docs/DOMAIN_MODEL.md` §4–§6 6. `docs/API_CONTRACT.md` §5
7. `docs/LEGACY_BUSINESS_LOGIC.md` BL-4, BL-8, BL-9
8. `docs/REQUIRES_CONFIRMATION.md` RC-05/06/11/12/13. 9. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS (verify; else stop)
Organization (branches, posting profiles table, `resolvePostProfile`), sequences,
audit, isolation harness.

## 4. GOAL & SCOPE
### In scope
- Tables+RLS: `accounts, fiscal_years, fiscal_periods, period_module_locks,
  journal_entries, journal_entry_lines, cost_centers, opening_balances`; add FK
  `branch_posting_profiles.mapping` account validation now (accounts exist):
  service-level validation + documented check.
- Services: `AccountsService` (tree, reparent guard, postable rules),
  `PeriodsService` (open/close/reopen + module locks),
  **`PostingEngine.post()` / `reverse()`** per ACCOUNTING_CONTRACT rules (guards:
  period open + module unlocked + postable accounts + balance; numbering via sequences;
  idempotency key on posts), `LedgerQuery` statements (TB/GL/account-statement with
  previous-balance row parity to legacy SP but tenant-scoped & N+1-free).
- COA seed template (ar/en SME chart) + `provisionAccountingDefaults(tenantId)`.
- Invariant suite T1–T10 (ACCOUNTING_ARCHITECTURE §7) as automated tests.
- Statement endpoints (read) + journal endpoints per API_CONTRACT §5.
### Out of scope (DO NOT DO)
Invoices/vouchers (P08–P12 produce their journals by calling PostingEngine) ·
period-close UI · VAT return report (P14) · payroll postings (P20) · materialized
views (P14 decision with profiling).

## 5. EXACT TASKS
1. Migrations: all §4 tables incl. `ltree` extension, exclusion constraint for
   fiscal year overlap, immutability trigger on posted entries (UPDATE/DELETE raise),
   CHECK one-sided debit/credit lines.
2. Accounts service + tree endpoints (`?flat=false`), code uniqueness, reparent rejects
   when posted lines exist (guard: only while no posted lines on subtree) — test.
3. Periods: create years w/ periods; close checklist service (drafts in period == 0,
   module locks state); lock/unlock endpoints; reopen with reason→audit.
4. PostingEngine with guards & idempotency; reversal flow mirrors lines; `source`
   links stored; events `journal.posted` emitted.
5. Opening balances: bulk draft import (DTO rows) + `post` producing single balancing
   journal per year with `opening_balance_equity` plug; re-post blocked.
6. Statement endpoints + performance budget smoke (TB on 100k lines fixture ≤ 800 ms
   local — record number).
7. Invariant suite implemented in `packages/testing/accounting-invariants.ts` + run in CI.
8. COA seed + provisioning test.
9. STATUS.md; reconcile API_CONTRACT §5 if drift (change process).

## 6. DATABASE IMPACT
+8 tables, triggers (immutability), exclusion constraint, ltree index, RLS; FK added:
`branch_posting_profiles` validated for account subtypes at service level.

## 7. API IMPACT
API_CONTRACT §5 fully implemented; permissions `accounting.view, account.manage,
journal.create, journal.post, journal.reverse, period.close, period.reopen,
costcenter.manage, opening.manage, reports.view` (seed).

## 8. SECURITY REQUIREMENTS
Close/reopen & reversal audited with reason capture; period guards cannot be bypassed
via crafted dates (server-side period resolution); account deletion soft & blocked
with posted activity.

## 9. TESTING REQUIREMENTS
Invariant suite T1–T10 mandatory + unit (engine math, FX lines, guards) + integration
(all endpoints incl. negative paths) + isolation proofs + immutability DB test.

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md; module README (engine usage for P08–P12 with copy-paste DTO examples);
ADMIN master §4 alignment check.

## 11. ACCEPTANCE CRITERIA
- T1–T10 green; TB on fixture equals hand-computed expected JSON snapshot.
- Post→reverse→attempted-mutation all behave per rules (incl. DB-level denial).
- Period lock mechanically blocks posting of a backdated invoice-like journal.

## 12. DEFINITION OF DONE
verify green · budgets recorded · docs updated · protocol §8 report.

## 13. DELIVERABLES
Accounting module end-to-end + engine + statements + invariants + seeds + docs.

## 14. DO NOT DO
Subledger-payments/party features · invoices · materialized caches · changing
immutability/reversal policies · editing organization internals (public APIs only).
