# ADMIN_PANEL_MASTER_REQUIREMENTS

> Level C — aggregated from all module requirements (single canonical file per §17/§19
> consolidation decision; phases extend their module section here, not separate files).
> App: `apps/admin` (Next.js 15, Tailwind + shadcn/ui, TanStack Query, RHF+zod via
> `@erp/contracts`). RTL-first (ar default, ar/en switch), tenant timezone rendering.

## 0. Global Shell (all modules inherit)

- Shell: sidebar (module groupings below), topbar (tenant + branch switcher
  `X-Branch-Id`, user menu, notifications bell, global search `Ctrl+K`),
  breadcrumbs, RTL layout.
- Table kit: server pagination/sort/filter toolbar, column chooser (persist per user —
  legacy CustomizedDGV equivalent), CSV export where allowed, empty/loading/error states
  mandatory (`EmptyState`, `TableSkeleton`, `ErrorState` components).
- Form kit: zod-validated, dirty-guard, sticky action bar, inline field errors, disable
  on immutable docs (posted), optimistic-lock conflict dialog (`VERSION_CONFLICT`).
- Feedback: toast on success; problem+json `detail` surfaced; `traceId` copy button.
- Permission-driven rendering (hide, not disable, when no view perm).
- Dashboard home: KPIs (today sales, cash, AR overdue, low stock count), charts
  (sales trend, top categories/items — legacy MonthlySales/GetSalesByCategory parity),
  quick actions, pending items (drafts, failed e-invoices, unlocked periods).
- Print: browser print CSS + PDF artifacts from API where listed.

## 1. Platform & Settings module

Screens: Tenant profile/settings editor (typed keys w/ descriptions) · Users &
memberships (invite, branch scope, discount limits — legacy OperMaxDiscount UI) ·
Roles & permissions matrix (module×action grid) · Audit log explorer (entity filter,
diff viewer) · Files manager · Notifications center · Sequences/numbering admin ·
Feature flags (vertical packs).
Tables: users(email,status,last_login), audit(time,actor,entity,action,diff).
Perms: platform.*. Actions: invite/reset-password/suspend/resend.

> Backend readiness after PHASE_04 (the screens themselves land in PHASE_17):
> - Audit log explorer — `GET /audit-log` with `entity/entityId/action/actorUserId/from/to`
>   filters; each row already carries `before`/`after`, so the diff viewer needs no extra
>   endpoint.
> - Files manager — `GET /files` (filter `status`/`entity`, search by name),
>   `GET /files/{id}`, presign/finalize, and a short-lived signed download link.
> - Notifications center — `GET /notifications` (+ `meta.unread`), mark-read,
>   `POST /notifications`.
> - Sequences/numbering admin — **service only** (`SequencesService.peek/configure`); no
>   HTTP surface yet, because PHASE_04 §5.6 scopes sequences to the allocation service.
>   The screen needs read/update endpoints for `document_sequences`; whichever phase
>   builds it must add them (prefix/padding are editable, `current_value` must not be
>   rewindable).
> - Feature flags — already covered by the typed settings editor (`feature.*` keys).

## 2. Organization module

Screens: Company profile form (+ ZATCA national address sub-form, logo upload to S3) ·
Branches CRUD (+ posting profile tab per doc type) · Warehouses CRUD · Cash locations
CRUD (safe/bank, bank JSON block, balances view) · Currencies & FX rates grid ·
Price lists (items side-panel pricing editor) · Fiscal calendar viewer (link to
accounting). Empty states guide first-time setup (onboarding checklist widget).

Backend capabilities available to these screens beyond the list above (PHASE_05, aligned
2026-09-05): a **rate preview** (`GET /fx-rates/resolve`) that reports whether a number is
direct, inverse or triangulated and through which pivot — the grid should show the source,
not just the number; a **posting-profile preview** (`GET /branch-posting-profiles/resolve`)
that shows which rung of the branch→tenant fallback chain a document would actually use;
IBANs arrive **masked in lists** and unmasked only on the detail read, so a grid must not
be built to expect the full value; "activate" and "make default" are PATCH fields on the
row (with an optimistic-concurrency `version`), not separate actions; and delete is a soft
delete, so the UI should say "archive" and expect the code to become re-usable.


## 3. Catalog module

Screens: Categories tree (drag-reorder, POS flags, kitchen printer) · Items list
(advanced filters: category/kind/tax/barcoded/stock state; bulk ops: import CSV,
export) · Item editor (tabs: general, units & barcodes, pricing incl. price lists,
components/BOM, tax & codes (Egy GS1 fields), images, alternative codes, details
(importer/weight/lead time), history (price changes)) · Units CRUD · Tax groups CRUD.
Filters: q, category, active, low-stock. Actions: duplicate, print barcode labels
(config sizes from legacy SettingBarcode concept), merge caution dialog.

## 4. Accounting module

Screens: COA tree (collapsible, badges: type/subtype/postable; drag re-parent
validated) · Account drawer (statement shortcut) · Journal entries list (status/branch
/date filters) · Journal editor (lines grid with running debit/credit totals, balance
indicator, attachments) · Post/Reverse dialogs (reason required) · Fiscal years &
periods manager (locks toggle per module, close checklist wizard, reopen with reason) ·
Cost centers tree · Opening balances wizard (CSV import preview, post batch) ·
Statements: Trial Balance (expand tree, drill to GL), General Ledger (account/date/
branch), Account Statement party view link. Perms + accounting.guardrail banners
(period closed strips).

## 5. Parties (customers & suppliers)

Screens: Parties list (kind tabs, tax_no search, balance column, credit-limit flag) ·
Party editor (tabs: profile + ZATCA address, contacts (DealPersons), bank details if
contractor, credit & price list, linked account picker, attachments) · Party 360 view:
balance card, open invoices table, statement (export PDF), allocations timeline,
notes. Actions: quick receive/pay voucher prefilled. Empty states for no-open-items.

## 6. Inventory module

Screens: Stock levels (warehouse filter, item search, qty/avg cost/value, negative
highlight) · Item movement (ledger timeline incl. doc links) · Stock adjustments
(list + editor with counted vs system diff columns, approve action w/ perm) · Stock
transfers (send/receive workflow badges, partial receive grid) · Lots & expiry report
(legacy ItemsExpirationStock parity: remaining years/months/days) · Serials tracker
(status filter). Widgets: low-stock, expiring-soon.

## 7. Sales module

Screens: Sales invoices list (kind/status/payment filters, daily total footer) ·
Invoice editor: party picker or cash-customer inline (name/mobile per tenant rules),
price-includes-VAT toggle, lines grid (unit dropdown with ratios, price from price
list, discount with employee-cap hint, tax auto, stock availability badge), totals
panel (subtotal/discounts/additions/insurance/VAT/extra/WHT/total/paid/balance),
payments panel (multi-tender cash/card/bank), reference linking for returns, ZATCA
status chip, print & PDF. Actions: post/void/pay/duplicate/return-from-invoice ·
Credit/Debit notes editor · Offers manager (targets/validity editor) ·
Customer quick-statement link. Widgets: today sales, unpaid > 30 d.

## 8. Purchases module

Mirror of sales + supplier-required validation, additional-costs tab with allocation
method preview (qty/value) and landed-cost effect per line, supplier reference/date
fields. Receiving note shortcut from invoice (creates stock-in when draft policy off).

## 9. Treasury module

Screens: Vouchers list (kind tabs receipt/payment, method filter) · Voucher editor
(party or counter account, amount, VAT split auto, cheque sub-form with lifecycle
actions clear/bounce, allocations table to open invoices with auto-suggest oldest-first)
· Cash transfers (send/receive badges) · Expense types CRUD · Cash location balances
board (+ per-currency chips) · Shift closes: open/current screen (live counters),
close wizard (counts by denomination ← Rekaba grid, diff explanation, print report),
history list with PDF. Widgets: cash on hand, pending cheques.

## 10. E-invoicing console

Credentials wizard (CSR upload, environment toggle, masked secrets) · Submissions
monitor (status, UUID, hash, error, retry) · Failed queue bulk retry · ZATCA health
panel. Egypt ETA tab hidden unless authority enabled.

## 11. Reporting center

Reports index (categories: financial/inventory/sales/purchases/parties/treasury/HR) ·
Parametric runner (date presets, branch/warehouse/party pickers) · Tables + charts ·
Async export (CSV/XLSX/PDF) to files with notification on ready. Reports keys per
`API_CONTRACT` §11 (incl. legacy-parity: SalesByDay, category/items/payment-method
breakdowns, expiry, stock limits, aging).

## 12. Migration console (P15/P16)

New run wizard (source profile, waves selection, mode pick) · Analyze/dry-run results
(counts, issue severities drill-down payloads) · Import progress (per-wave bars,
pause/resume) · Reconciliation report viewer (R1–R7 pass/waive/fail + PDF download,
owner waiver upload) · Legacy ID lookup tool (search old GlobalID → new record) ·
Compat devices manager (API keys, cursor resets) + sync status.

## 13. HR (P20)

Employees directory (profile + salary components + bank) · Attendance import + log ·
Adjustments (additions/deductions incl. SubFromSalary) · Payroll runs wizard (month/
year, preview lines, post) · Payslip print. Perms hrm.*.

## 14. Vertical packs (enabled per tenant flags)

POS (P19): tables floor map grid, open/new order flow, order-type chips, kitchen print
config, daily order numbers reset view. Projects (P21): projects kanban by stage,
progress-bill editor (retention, previous payments auto), contractor parties view,
installment contracts schedule grid + collect action. Niche (P22): prescription form
(optics), measurements card (tailoring), marina booking calendar + rent invoice +
violations, fitment compatibility picker (make/model/year), Salla sync monitor
(export queue, diffs view from legacy vw_Items_Salla_Status parity).

## 15. Cross-cutting UI states (mandatory everywhere)

loading skeleton · empty (with CTA) · error (retry + traceId) · forbidden (403 page) ·
offline banner (query retries) · posted/immutable read-only mode with explain tooltip ·
RTL numerals option (western digits default) · decimal input masks per currency digits.
