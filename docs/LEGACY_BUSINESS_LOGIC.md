# LEGACY_BUSINESS_LOGIC

> Level C. Business rules extracted from the SQL code (SPs/TVFs/view) + column evidence.
> Badge legend as before. Rule IDs (`BL-n`) are referenced by `DATABASE_DESIGN.md`,
> `ACCOUNTING_ARCHITECTURE.md` and phase prompts.

---

## BL-1 Document numbering & identity (CONFIRMED)

- Numbers are **per branch + doc-kind**: `InvCounters(Branch, InvType, ProcType) → LastID`
  guarded by unique `(branch, inv_type, proc_type, id)` on `Inv`.
- Global uniqueness is achieved via the string `InvGlobalID` (format unknown → flag).
- Restaurant order tickets use a **per-day counter** (`DailyOrderCounter`, unique
  `(OrderDate, OrderNo)` in `OrderNumbers`).
- Target: `document_sequences` (scope tenant+branch+type[+fiscal year]) single service.

## BL-2 Document classification (PARTIALLY CONFIRMED — enumeration open)

Evidence from read-path SQL:
- `proc_type=1` vs `proc_type=2` split "original" vs "return" in **sales** aggregates
  (`SalesByDay`, `GetSalesByOrderType`, `GetSalesByPaymentMethods`) where
  `inv_type IN (2,3)`.
- In **stock** aggregates, `inv_sub.proc_type=1` sums as stock-IN and `=2` as stock-OUT,
  globally, regardless of `inv_type`.
- Cost procedures treat `proc_type=2 AND inv_type IN (1,4,8[,9])` as stock/cost-OUT;
  `proc_type=1` of purchasing types as IN.
- `inv_type=9` is excluded from balance-sheet costing (`inv_type<>9`) → non-stock-value
  movement (INFERRED: internal usage/services).
- `funGetInvoiceItemDetails` excludes `proc_type=4` → at least one more proc_type
  (INFERRED: quotation/order, non-posting).

**Contradiction note:** the same `(proc_type=1)` simultaneously means "sale" (stock should
decrease) and "stock-IN" in these functions → direction is de-facto **type-dependent and
data-driven** via `InvTypes.IsInput`, and the TVFs are correct only for the type subsets
they were written for. `InvTypes.DATA` (not exported) holds the truth.

`REQUIRES_CONFIRMATION` (RC-01..RC-04): full `InvTypes` rows; `proc_type` value list;
`pay_type` map vs `SettingPayMethods`; whether sales actually post `proc_type=2` rows for
returns with stock-IN effect (expected) and how the TVFs then stay correct (they don't for
sales types → they are only *named* generic).

**Target rule (frozen):** in the new system, stock direction is an explicit, validated
property of the document type (`inventory_transactions.qty` signed + posting profile),
never inferred from a shared integer.

## BL-3 Stock balance & valuation (CONFIRMED mechanics, formula variants)

- Balances are **computed on demand** from `Inv`×`Inv_Sub` (no ledger table), with
  `IS_Deleted=0` rows only; several variants: per branch/store/date/category/POS-only.
- A **cache** `ProductStocks(quantity, AvrgCost)` exists but its writer is not in the
  export (application-maintained) → treat as stale-risk; engine re-derives truth.
- **Valuation = weighted/moving average**: `ItemAvrgCost` =
  `(Σ(costIn) − Σ(costOut) + Σ(discountAllocIn) − Σ(discountAllocOut)) / Σ(qtyIn − qtyOut)`,
  where invoice discount `minus` is allocated to lines pro-rata by line value
  (`((val1*exchange_price) − discount)/(InvTotal + minus) * minus + lineDiscount`).
- Returns-to-supplier of types (1,4,8) reduce average-cost pool with special discount math.
- `ItemBalanceWithAvrgCost` newer variant uses `COALESCE(AvrgCost, exchange_price)*val1`.
- Negative & zero guards clamp totals to ≥ 0 in reporting SPs.

**Target rule (frozen):** append-only `inventory_transactions` + transactional
`stock_balances(item, warehouse)` cache holding `qty` and `avg_cost`; same formulas as
legacy for reconciliation; moving average is the v1 valuation method (FIFO is a noted ADR
candidate, not v1).

## BL-4 Journal posting linkage (CONFIRMED pattern, mapping open)

- Journals live in `Entry`(`state=1` = posted)+`Entry_sub`; business docs reference them:
  `Receipts.EntryGlobalID`, `SalaryPay.EntryGlobalID`, `Salary_Res.EntryGlobalID`,
  `EmpSalaryAddSub.EntryGlobalId`, `Inv.EntryID` (likely `Entry.id` — RC-05).
- `SettingGeneral.HasEntry` per invoice type toggles auto-journal; `ItemsAcc/ReturnItemsAcc/
  DiscountAcc/InsureAcc/DeliveryAcc/StoreAcc/VATCode` provide the **COA mapping**;
  branches carry default account codes (`CustomersAcc/SupliersAcc/TreasuriesAcc/BanksAcc/
  InventoryAcc/CostCenter/EmployeeAcc`).
- `Entry.IsVAT` marks VAT-related journal; VAT accounts presumably via `VATCode`.
- Reversal/unpost: no explicit reversal table — corrections likely re-entries or state
  flips (RC-06).

**Target rule:** posting engine builds journals from **posting profiles** (doc type +
branch + party + item class) into immutable posted entries; void = reversal entry
(`reversal_of` link); `state` machine: `draft → posted → voided`.

## BL-5 Sales flow (CONFIRMED shape)

- Multi-tender on one invoice: `cash`, `visa`, `bank` amounts + `pay_type` (1 cash-net,
  2 card, 4 split, -1/5 deferred classes). Deferred sales excluded from daily cash sales
  (`pay_type<>5` in `SalesByDay`) → credit-sale class CONFIRMED.
- Discounts: line `discount` + invoice `minus` + `ItemsDiscount`; per-employee caps via
  `OperMaxDiscount`; item-level caps `MaxDicountParcent/MaxDiscountAmount`.
- Prices may embed VAT (`PriceIncVAT`); extras: `ExtraVAT`, `AdditionalTax`, `Insurance`,
  delivery (`AdditionsTot`?) — exact total formula per type RC-07.
- `InvProfit` and `InvCost/tot_purch` snapshots recorded on the invoice.
- Walk-in cash customer: `CashCustomerName/Mobile` + default customer setting; mobile
  optional/required flag.
- Promotions: `Offer/OfferItems/OfferForClient` (targets by qty/value, grouped items,
  item %/value, validity dates, account for posting).
- Returns: linked vs unlinked returns toggles (`LinkedReturn/UnLinkedReturn`),
  `Reff_No/Reff_date` on returns.

## BL-6 Purchasing flow (INFERRED from cost logic + fields)

- Purchase docs (likely `inv_type=1`, `proc_type=1` in / `=2` return) carry supplier
  reference, `tot_purch`, `AdditionalCost`, `Insurance`, VAT lines, WHT fields.
- Additional costs feed item cost (`ItemAddedCost`) → landed cost into average pool.
- Free-of-charge/exempt sales buckets (`FreeVATSales`) tracked.
- RC-08: where supplier identity lives on purchases (`cust_id` vs `Suppliers`).

## BL-7 Treasury & receipts (CONFIRMED shapes)

- Two generations: `Sand*` family (typed vouchers with cheque block, `safe_bank_id`,
  `Cccode`) and newer unified `Receipts` (Explicit Debit/Credit accounts, types, cheque
  lifecycle, journal link). Cheques have states (`CheckState` int — clearance workflow
  INFERRED).
- Shift close: cash counted vs computed (`diff`), network/returns/expenses/purchases
  buckets, per-customer deferred amounts, Android variant w/ HTML report print.
- Multi-currency safe balances + denomination counting sheets (`Rekaba`).

## BL-8 Period governance (CONFIRMED, recently introduced)

- `AccountingPeriods` (closed by/at, active) + **`PeriodLocks` per module** → phased
  closing: lock sales first, then accounting, etc. `Inv.PeriodID` binds invoices to periods.
- Target: `fiscal_years` + `fiscal_periods` + `period_module_locks`; posting rejected when
  locked; close = permission-gated action with checklist (mirrors `SettingCloseShift`
  philosophy at period scale).

## BL-9 Sub-ledgers & statements (CONFIRMED)

- Party balances ride on COA accounts (`Customers.AccountCode`), branch-scoped statements
  (`GetAccountStatement` with recursive children + "Previous Balance" row).
- No dedicated open-item/allocation table discovered → invoice settlement inferred via
  vouchers + balances (RC-09). Target adds explicit `payment_allocations`.

## BL-10 HR/Payroll (CONFIRMED)

- Salary = components on employee + monthly `Salary_Res` runs + `SalaryPay` payouts with
  journal link; advances/adjustments via `EmpSalaryAddSub` (`SubFromSalary` deducts).
- Attendance = raw biometric punches only (no evaluation logic in DB) → basic import
  in target P20; evaluation rules RC-10.

## BL-11 E-invoicing (CONFIRMED pipeline)

- ZATCA: CSR properties → credentials (CSID/secret/prod-request) → per-invoice
  `UUID/InvoiceHash/QRCode/ZatcaSent` → responses log. Simplified/standard types via
  config; simulation & production flags; finalization dates window.
- ETA Egypt: client id/2 secrets, signer pin, doc-type version, branch/activity codes +
  GS1/EGS item coding on items.

## BL-12 Reservations/orders vs invoices (CONFIRMED existence)

- Restaurant flow: table open (`Tables.InvGlobalID`, `Status`, `Balance`), items per
  order (`Table_Order`), merged/final invoices (`InvCombinedId`), order type workflows.
- Marina: bookings → rent invoices with periods, additions, insurance, companions.
- Contracting: requirement → contract invoice (progress bill) w/ retention & previous
  payments, stages accreditation workflow.
- Installments: contract w/ down payment & schedule rows (`cont_installments` paid tracking).

## BL-13 Deletion & edit model (CONFIRMED behavior)

- Soft-delete everywhere (`IS_Deleted`); business math ignores deleted rows.
- Documents editable post-factum (history clones `His_Entry*` for entries).
- Target replaces edit-posting with **reversal discipline**; draft-only edits.

## BL-14 Multi-branch data scoping (CONFIRMED)

- Most docs carry `branch`; employee↔branch ACL via `EmpBranches`; reports branch-param.
- Target: branch as first-class org node + membership branch-scope; consolidated vs
  per-branch reporting is a filter, not a security boundary (tenancy is).

## BL-15 Currencies (CONFIRMED partial)

- Default SAR; `CurrencyCode` on invoices; FX journals & bands in `Currency_Lastprice*`;
  per-safe currency balances. Target: ISO 4217 + per-tenant base currency + fx_rates.

---

### Consolidated open register

All RC-xx above are consolidated with context + required evidence in
`REQUIRES_CONFIRMATION.md` (RC-01 … RC-31). Nothing here is silently adopted without
either `CONFIRMED` evidence or an owner answer.
