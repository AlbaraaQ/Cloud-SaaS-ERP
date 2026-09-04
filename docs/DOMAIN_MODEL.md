# DOMAIN_MODEL

> Level B — CANONICAL. Ubiquitous language, aggregates, invariants, state machines,
> and the legacy→target domain mapping matrix.

## 1. Ubiquitous Language (frozen vocabulary)

| Term (EN) | Arabic | Meaning |
|---|---|---|
| Tenant | المنشأة/الشركة | SaaS customer; isolation boundary |
| Branch | فرع | Org node under tenant; docs are branch-dated |
| Warehouse | مستودع | Stock location (legacy `Stocks`) |
| Cash Location | خزينة/بنك | Money location, kind=safe|bank (legacy `Safes`/`Banks`) |
| Party | طرف | Customer or Supplier (vertical flags: owner/contractor) |
| Fiscal Year/Period | سنة/فترة مالية | Posting calendar with module locks |
| Journal Entry | قيد يومية | Double-entry document; posted = immutable |
| Voucher | سند | Receipt (قبض) or Payment (صرف) money document |
| Invoice | فاتورة | Sale/Purchase (…Return, Credit/Debit Note) |
| Posting | ترحيل | Turning a draft doc into ledger effects |
| Reversal | قيد عكسي | The only way to undo a posting |
| Allocation | تسوية/ربط | Linking voucher money to open invoices |
| Lot/Serial | تشغيلة/سيريال | Batch/expiry or unit identity tracking |
| Shift Close | إغلاق وردية | Cashier Z-report close |
| Progress Bill | مستخلص | Contracting stage invoice with retention |

`doc_type` codes: `sales_invoice, sales_return, credit_note, debit_note, purchase_invoice,
purchase_return, receipt_voucher, payment_voucher, journal_entry, stock_adjustment,
stock_transfer, cash_transfer, payroll_run, shift_close, progress_bill, rent_invoice,
installment_contract, opening_balance`.

## 2. Domain Map (modules ⇄ capabilities)

| Module | Owns | Consumes (never tables) |
|---|---|---|
| platform | tenants, users, memberships, RBAC, audit, files, notifications, jobs, sequences | — |
| organization | company profile, branches, warehouses, cash locations, currencies, posting profiles | platform |
| catalog | items, categories, units, taxes, price lists, components | organization, files |
| accounting | COA, fiscal years/periods/locks, journals, cost centers, posting engine, opening balances, statements | platform, organization |
| parties | parties, contacts, AR/AP queries, allocations | accounting |
| inventory | ledger, balances, adjustments, transfers, lots/serials | catalog, organization |
| sales | sale invoices/returns/notes, offers, invoice payments | parties, inventory, accounting, catalog |
| purchases | purchase invoices/returns, landed costs | same as sales |
| treasury | vouchers, cash transfers, cheques, shift closes, expense types | parties, accounting, organization |
| einvoicing | credentials, submissions, signing pipeline | sales |
| reporting | read-models, statements, exports | all (read-only) |
| migration | runs, mappings, issues | all (write via public services) |
| hrm / projects / pos / niche / integrations | vertical capabilities | core only |

## 3. Legacy → Target Mapping Matrix (table-level)

| Legacy | Target | Note |
|---|---|---|
| `Foundation` | `company_profiles` | ZATCA address JSON |
| `Branches` | `branches` + `branch_posting_profiles` | *Acc columns → profiles |
| `Stocks` | `warehouses` | |
| `Safes`, `Banks`, `treasury` | `cash_locations` | unified (RC-14 watch) |
| `Accounts_Index` | `accounts` | cached totals dropped (RC-11) |
| `Entry`, `Entry_sub` | `journal_entries`, `journal_entry_lines` | state=1→posted |
| `His_Entry*` | `audit_log` (or archive) | history model change |
| `AccountingPeriods`, `PeriodLocks` | `fiscal_periods`, `period_module_locks` | |
| `Cost_Center`, `costs` | `cost_centers`, `expense_types` | |
| `Customers/Suppliers/Owners/PM_Contractor/VATClients` | `parties` (+flags) | kind split at import |
| `DealPersons` | `party_contacts` | |
| `salesmen` | `employees flag salesman` OR `salesmen` kept in P10 decision | keep `salesmen` table in sales module (ADR-009) |
| `Items`, `ItemsCategory`, `units`, `ItemUnits`, `Itembarcodes`, `ItemAlternativeCodes`, `ItemPrices`, `ItemDetails`, `ItemComponents`, `tax_groups` | catalog §6 tables | units fixed-pk |
| `Inv/Inv_Sub` (sale classes) | `sales_invoices(_lines)`, `invoice_payments` | kind by inv_type/proc_type (RC-01/02) |
| `Inv/Inv_Sub` (purchase classes) | `purchase_invoices(_lines)`, `purchase_invoice_costs` | + `InvoiceCost` |
| `InvContratct(_Sub)` | `progress_bills(_lines)` (P21) + interim mapping table | |
| `RentInvoice` | `rental_invoices` (P22) | |
| `Receipts`, `Sand*` | `vouchers` | unified; RC-19 |
| `CreditDeptNotes` | `sales_adjustment_notes` | |
| `InvoicePayments` | `invoice_payments`/`payment_allocations` | |
| `InvoiceItemDetail`, `ItemSerialNo` | `invoice_item_attributes`, `item_serials`, `item_lots` | |
| `SafesAdjust*`, `SafesTransfer*` | `stock_adjustments*`, `stock_transfers*` | naming fixed! (safes=storage here) |
| `ProductStocks`, stock TVFs | `inventory_transactions`+`stock_balances` | recomputed, not imported (RC-20) |
| `CasherClosed*`, `CahierClosedAndroid`, `Check_Close`, `CloseShiftCustomer` | `shift_closes(_lines)` | |
| `Currency_SafeBalance` | `cash_location_balances` | recomputed/verified |
| `Currency_Lastprice*` | `fx_rates` | latest rows + history |
| `Rekaba` | `cash_count_lines` (+denominations seeded) | |
| `cont`, `cont_installments` | `installment_contracts`, `installment_schedule` | P21 |
| `PM_*` | projects pack tables | P21 |
| HR tables | hrm pack | P20 |
| Restaurant tables + settings | pos pack | P19 |
| `Glasses`, `Other_Column`, `CustomerMeasurements`, `Marine*`, `Booking*`, `Violation`, `Equip*`, `ItemVehicleFitment`, `Offer*` | niche pack / offers | P22 / P10 |
| `Documents` | `files` | object storage |
| `Users`, `User_Permissions`, `Forms`, `OperationPermission`, `OperMaxDiscount` | `users/memberships/roles…` (+`discount_limits` on membership) | passwords force-reset |
| `Employees`, `EmpBranches` | `employees` + membership link | hrm core parts land P03-lite? NO → P20 imports; memberships carry branch scope |
| `SettingGeneral` per inv_type | `tenant_settings` + `branch_posting_profiles` | typed keys |
| Other `Setting*` (print/scale/display/POS/email/mqtt/sync) | `tenant_settings`/`files`/dropped-desktop-only | listed in migrator spec |
| ZATCA/ETA/Salla/Sync/Cloud tables | einvoicing / integrations pack | secrets encrypted |
| `Log4NetLog` | archive only (no import) | |
| `Backup*`, `DbVersions`, `Year_Previews`, `SyncEntities`, `DailyOrderCounter`, `InvCounters`, `OrderNumbers` | platform equivalent / archive | counters → sequences continue values! |
| junk: `[21346]`, `Table_2`, `Paste Errors`, `Switchboard Items*`, `intel*`, `try`, `sett`, `commentsTable` | **archive raw only** | never domain tables |

## 4. Aggregate Roots & Invariants

- **Invoice(AR)** invariant: `total = subtotal − discounts + additions + insurance + taxes
  − withholding` per rounding of currency (BL-5/7; exact formula per type — RC-07
  resolved before P10 goes live). Posted invoice ⇒ balanced journal exists.
- **JournalEntry(AR)**: Σdebit=Σcredit (base currency) ∧ date inside open period ∧
  module unlocked ∧ accounts postable.
- **Inventory posting**: only via `inventory_transactions`; `stock_balances` updated in
  SAME TX; avg cost = (value±mv)/(qty±mv) with zero/negative guards (legacy clamp kept
  as report-level guard, negative stock policy configurable `inventory.allow_negative`).
- **Voucher**: receipt ⇒ cash_location debit, counter/AR credit; payment inverse;
  Σallocations ≤ amount; cheque lifecycle `pending→cleared|bounced`.
- **ShiftClose**: exactly one open shift per (cash_location?,user,branch) at a time;
  close requires cash count; diff recorded (never forced to zero).
- **FiscalPeriod**: close requires all modules locked ∧ no drafts in period;
  reopen = permission `accounting.period.reopen` + reason audit.
- **PayrollRun**: posted ⇒ per-employee net journal aggregated; immutable.
- **Party**: code immutable after first posting; account link must point to
  `subtype receivable|payable` postable account.

## 5. State Machines (normative names)

- Invoice/Voucher: `draft → posted → voided` (voided keeps `reversal_entry_id`).
- Journal: `draft → posted → voided` (void via reversal entry copy with negated amounts).
- StockTransfer: `draft → sent → received | voided`.
- Shift: `open → closed`.
- Submission(e-invoice): `pending → signed → cleared/reported | failed(→retry|manual)`.
- MigrationRun: `prepared → dry_run → importing → reconciled | failed`.

## 6. Engine Contracts (module APIs others may call)

```
PostingEngine.post(docRef, profileKey, payload) -> journal_entry_id   // tx-bound
InventoryLedger.record(docRef, lines[]) -> movements                  // tx-bound
Sequences.next(docType, branchId, fiscalYearId?) -> {number, display}
LedgerQuery.accountStatement(account, range, branch?) -> rows + prevBalance   // BL-9
ArAp.partyBalance(party, asOf?) -> {receivable, payable, allocated, open[]}
EInvoice.submit(invoiceId) -> submission status                        // queued outbox
```

Cross-module writes MUST flow through these contracts (no table poking).

## 7. Domain Events (in-process, P02 emitter; outbox for jobs)

`invoice.posted`, `invoice.voided`, `voucher.posted`, `journal.posted`, `period.closed`,
`shift.closed`, `einvoice.cleared`, `migration.reconciled`. Handlers: notifications,
audit enrichment, future webhooks (documented, not built v1).
