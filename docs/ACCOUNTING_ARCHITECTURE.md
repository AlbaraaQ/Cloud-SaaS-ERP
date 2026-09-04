# ACCOUNTING_ARCHITECTURE

> Level B — CANONICAL. The accounting core is a real subledger-driven engine,
> not CRUD on invoices.

## 1. Principles (non-negotiable)

1. **Double-entry, always**: every financial fact = balanced journal entry.
2. **Immutability of history**: posted entries/documents never change; reversals form
   audit-visible chains (`reversal_of`).
3. **Subledger ≠ afterthought**: invoices/vouchers carry `journal_entry_id`; AR/AP,
   inventory and VAT reports must reconcile to the GL by construction.
4. **Config-driven posting**: mappings live in posting profiles (legacy `SettingGeneral`
   proved the need), never hard-coded account numbers in services.
5. **Period governance**: fiscal periods + per-module locks; posting into closed/locked
   periods is impossible at DB+service level.
6. **Deterministic money math**: `numeric(20,4)`, `decimal.js`, HALF_UP per currency
   minor units; one rounding policy per module documented in code.

## 2. Chart of Accounts

- Hierarchical: `parent_id` + `ltree path` (fast subtree ops → trial balance rollups).
- `type ∈ {asset, liability, equity, revenue, expense}`; `subtype` drives engine routing
  (`cash, bank, receivable, payable, inventory, vat_input, vat_output, sales,
  sales_return, purchase, purchase_return, discounts, cogs, expense, other_income…`).
- `normal_balance` matches type; statements compute signed balances by `normal_balance`.
- Only `is_postable=true` leaves accept lines; parents are for rollup.
- Tenant seeds: standard Arabic/English SME COA template (assets/…), customized freely.
- Legacy import: `Accounts_Index` rows → accounts; `ParentCode` cast + orphan repair
  report (RC-12); cached `Total_*` columns dropped (RC-11); `Code` → `code text`.

## 3. Journal Model

`journal_entries(header) + journal_entry_lines`:
- line CHECK: exactly one of debit/credit > 0; both ≥ 0.
- multi-currency: line stores `currency_code, currency_amount, fx_rate` when the posting
  source is foreign; base amounts are the debit/credit columns.
- subledger tags: `party_id` for AR/AP lines (receivable/payable subtype REQUIRED then),
  `cost_center_id` optional, `branch_id` inherited default from header.
- kinds: `manual | auto | reversal`; `source_type/source_id` polymorphic link enforced
  at service layer (`sales_invoice`, `purchase_invoice`, `voucher`, `payroll_run`,
  `stock_adjustment`, `opening_balance`, `migration_adjustment`…).

Posting flow (service): validate → resolve period (tenant-local date) → guards
(period open, module unlocked, accounts postable/active, party/branch scope) →
balance check → assign number (sequence) → mark posted (`posted_at/by`) → emit
`journal.posted`. DB trigger forbids UPDATE/DELETE where status='posted'.

**Reversal**: `reverse(entryId, date, reason)` creates kind=reversal entry with mirrored
lines, links `reversal_of`, marks original `void` ONLY via pair (original keeps posted
fact; voided flag logical). Original can be reversed once.

## 4. Posting Profiles & the Posting Engine

`branch_posting_profiles(branch_id, doc_type, mapping jsonb)` — resolved doc type →
account slots: `sales, sales_return, vat_output, receivable, cash, card_clearing,
discount_allowed, inventory, cogs, purchase, vat_input, payable, discount_received,
delivery_income, insurance_income, rounding, retention_receivable, advances…` plus
`cost_center`. Resolution order: branch profile → tenant default profile → hard fail
(misconfiguration must block posting loudly).

Engine input DTO (per doc) describes **lines of intent**: `{accountSlot|accountId,
amount, drcrHint?, party?, costCenter?, memo}`; the engine materializes balanced
entries (auto-plugs rounding diff to `rounding` slot if ≤ tolerance).

### Canonical event → journal patterns (defaults; profiles override)

| Event | Debit | Credit |
|---|---|---|
| Sale (cash) | cash_location | sales (net), vat_output (tax) |
| Sale (credit) | receivable(party) | sales, vat_output |
| Sale (split) | cash + card_clearing | sales, vat_output |
| Sale return | sales_return, vat_output(neg) | receivable/cash |
| Purchase | inventory(net), vat_input | payable(party)/cash |
| Purchase return | payable | inventory, vat_input(neg) |
| Receipt voucher | cash/bank | receivable(party) or counter account |
| Payment voucher | payable(party)/expense | cash/bank |
| COGS on sale post | cogs | inventory (avg cost snapshot) [optional profile `post_cogs=true`] |
| Stock adjustment | inventory / shrinkage expense | inverse by diff sign |
| Payroll run | salaries expense (+ component lines) | payroll payable / cash |
| Opening balances | assets/expenses… | opening_balance_equity plug |

WHT & extra taxes post to dedicated slots when configured (legacy `TotalWithholdingTax`,
`ExtraVAT` preserved as columns now).

## 5. Fiscal Calendar

- `fiscal_years` (non-overlap exclusion), `fiscal_periods` (≥1; usually 12).
- `period_module_locks(period, module)`: close sales before accounting etc. (BL-8).
- Close period: checklist service (drafts=0, pending e-invoices resolved, shift closes
  done, trial TB balanced (by construction asserts)); `accounting.period.close`.
- Year close: P&L roll into `retained_earnings` via generated closing entry;
  opening entry generated for next year (balances carry) — linked pair, idempotent.
- Legacy multi-DB years merge: each legacy DB year maps to a `fiscal_year`; migration
  imports oldest-first and posts per-period (RC-27).

## 6. Statements & Reports (read side, P14; logic contract here)

- Trial Balance: rollup posted lines over account tree between dates (opening column
  uses < start date), branch/tenant scoping. (legacy `AccountTotalBalance` generalized)
- General Ledger / Account statement: hierarchical party/account statement with
  previous-balance row (port of `GetAccountStatement`, minus its N+1).
- AR/AP aging: from subledger lines + `payment_allocations`; buckets 0-30/31-60/…
  per due_date else doc date.
- VAT report: vat_output − vat_input by tax group; ties to invoice tax sums.
- Balance Sheet / P&L: tree rollups; inventory valuation line ties to stock ledger
  value (engine asserts equality in tests).

## 7. Invariants Test Suite (runs in accounting phase & regression)

T1 any posted entry balances · T2 posted rows immutable (DB throws) · T3 reversal
mirrors exactly · T4 closed period rejects post (service+DB) · T5 TB(assets−liabilities
−equity→P&L) identity on fixture data · T6 subledger AR sum = Σ receivable-account
party lines per party · T7 every posted invoice/voucher has exactly one journal link ·
T8 FX conversions recompute deterministically · T9 COGS value = inventory ledger value
delta for fixture docs · T10 reopen requires reason + audit.

## 8. Migration hooks

- Opening balances arrive as `opening_balances` drafts → posted as one entry/year.
- Legacy journals import as auto entries preserving numbers in `legacy_id`,
  date, branches; unbalanced legacy rows → quarantined into `migration_issues` with a
  proposed balancing line when provable (else manual queue). Float drift fixed by
  rounding to currency minor units with variance report (RC-31).
