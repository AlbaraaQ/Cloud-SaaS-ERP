# Roadmap — phases 02–11 (specs for future sessions)

How to run a phase: read its desktop sources below + the matching cloud module,
port behaviours rule-by-rule behind real endpoints with specs, update the screen(s),
then flip this file's checkbox and `README.md`. Every phase keeps API compatibility.

## Phase 02 — Sales invoice engine ✅ (2026-09, `PHASE_02_SALES_ENGINE.md`)

- [x] Desktop: `Class/InvoiceOper.cs` (`SaveInvoice` L1310, `BindToEntry` L2252,
  `InvoiceCalc` L5009, `InvoicePayments`, `DeleteInvoice`, `CheckForOffer`),
  `Form_WPF/frmSalesInvoice.xaml.cs`, `frmInvSale*`, `frmInvoice*`, `frmCreditNote*`,
  `frmDebtNote*`, `frmOffers*`, `Class/Number2Arabic.cs` (تفقيط on print).
- [ ] Cloud: `apps/api/src/modules/sales/**`, staff `/sales/invoices`.
- [ ] Behaviours: line-level discount then header discount then VAT (confirm order in
  `InvoiceCalc`); credit-limit + TaxType-2 requires VAT number (`IsTaxCustomer`,
  `isCreditCust`); suspended (معلقة) vs quotation (عرض سعر) vs temporary (مؤقتة);
  return-must-reference-original (`isReturned`, `IsPreviousReturned`); payments split
  across safe/bank (`PayInvoice*`); posting via `BindToEntry` → journal (reuse
  `branch_posting_profiles`).
- [ ] Accept: create/post/return/credit-note flows pass specs; printed invoice shows
  amount-in-words; Zatca-ready fields populated (send itself is phase 11).

## Phase 03 — Purchase engine ✅ (2026-09, `PHASE_03_PURCHASE_ENGINE.md`)

- [x] Desktop: `InvoiceOper` invType 1 paths, `frmInvPurch*`, `frmPurchInv*`,
  `frmAdditionalCost*`, `frmSuppliers*`, `frmInvReturnTypes*`.
- [ ] Cloud: `modules/purchases/**`, staff `/purchases/invoices`.
- [ ] Behaviours: goods-receipt vs invoice separation; additional costs distributed
  over lines (confirm rule); supplier returns; debit notes; average-cost update on
  post (confirm in `ItemOper`).
- [ ] Accept: purchase → post → stock+average-cost updated; return + debit note specs green.

## Phase 04 — POS + shifts + cashier close ✅ (2026-09, `PHASE_04_POS_SHIFTS.md`)

- [x] Desktop: `frmPOS*`, `frmInvPOS*`, `frmCloseShift*`, `frmCasherSetting*`,
  `frmHoldM*`, `frmPOSBill*`, `frmPOSPay*`, `frmShortCutInv*`, `frmQueueM*`,
  `frmTables*`, `Class/CasherClosed*.cs`, `EntryOper.BindCloseShiftToEntry` (L402).
- [ ] Cloud: `modules/pos/**`, staff `/pos`, `/sales/shifts`.
- [ ] Behaviours: hold/recall, shortcuts, table mode, multi-payment tender,
  shift open/close with expected-vs-actual + variance entry, cashier permissions.
- [ ] Accept: full shift lifecycle posts balanced entries; variance account configurable.

## Phase 05 — Inventory ✅ (2026-09, `PHASE_05_INVENTORY.md`)

- [x] Desktop: `frmInvInOutput*` (invType 4 إدخال / 5 إخراج / 9 بضاعة أول المدة — all
  saved with `entry = null`, ledger repaired later by `frmReGenerateEntries`),
  `frmInventoryTransfer*`, `Class/Inventory.UpdateItemStock`, `Class/ItemOper*`.
- [x] Cloud: `modules/inventory/**` (vouchers, adjustments, transfers, below-minimum),
  staff `/inventory/vouchers`, `/inventory/adjustments`, `/inventory/transfers`,
  `/inventory/below-minimum`, `/inventory/items`.
- [x] Behaviours: stock in/out + opening as numbered postable documents with balanced
  journals; transfer both legs through بضاعة تحت التحويل (value-neutral); multi-line
  count with approval; serial/lot enforcement; reorder limits; negative-stock override.
- [x] Behaviours (part two): multi-unit conversion — `item_units` repaired by migration
  0035 (it was created unusable: `FORCE RLS`, no policy, no `tenant_id`), every document
  line can be counted in any unit the card defines and the ledger moves
  `base_qty = qty × factor`; multi-barcode with `GET /inventory/barcode/:code` resolving
  `items.barcode` → `item_barcodes` → `item_units.barcode`; expiry report
  `GET /inventory/expiry?days=…` and the `/inventory/expiry` screen; staff
  `/inventory/item-units` and unit columns on vouchers, counts and transfers.
- [x] Behaviours (part three): بضاعة في الطريق — a transfer sent but not fully received
  is listed with its value and age, and closed either as `return` (the remainder goes
  home, value conserved) or `shortage` (written off, no stock moves); the settled
  quantity lives in a new `closed_qty`, never in `received_qty`. بطاقة الصنف —
  `GET /inventory/item-card` with opening, running balance, totals and a closing that is
  asserted against `stock_balances`; `GET /inventory/movements` gained a period and
  joined names. Screens `/inventory/in-transit`, `/inventory/item-card`, and the
  updated `/inventory/movements`.
- [ ] Still open: unit-aware price lists (phase 08), printing the stock documents and
  barcode labels (phase 10). Production/assembly keeps its own service.

## Phase 06 — Treasury

- [ ] Desktop: `Class/ReceiptOper.cs`, `Treasury.cs`, `Bond.cs`, `Bank.cs`,
  `frmSand*` (vouchers), `frmTreasury*`, `frmSafes*`, `frmPay*`, `frmReseved*`
  (cheques), `frmNkd*` (daily cash), `frmSafeAdjust*`, `EntryOper.BindReceiptToEntry`.
- [ ] Cloud: `modules/treasury/**`, staff `/treasury/**`.
- [ ] Behaviours: receipt/payment voucher numbering per safe, safe↔bank transfers,
  cheque lifecycle (under-collection → cleared/bounced), daily cash close,
  customer/supplier allocation (`frmCustLastPay`, `frmPayMultiCredit`).
- [ ] Accept: voucher posts correct journal; cheque states transition with entries.

## Phase 07 — Accounting core

- [ ] Desktop: `Class/EntryOper.cs` (`SaveEntry`, `ShowEntrySource`, `BindEntryByID`),
  `frmNewEntry*`, `frmIntialRestraiction*` (opening), `frmAccounts*` (tree, directory,
  statement, balances), `frmMezan*` (trial balance), `IncomeStatementForm*`,
  `FrmAccountingPeriods*`, `frmCostCenter*`, `frmTax*`/`frmVAT*` (returns),
  `frmReGenerateEntries*`.
- [ ] Cloud: `modules/accounting/**`, staff `/accounting/**`.
- [ ] Behaviours: manual entry balance gate + period-open check; opening entry;
  source-drill (entry → invoice/voucher); regenerate-postings job; VAT return
  aggregation; cost-centre dimensions on lines.
- [ ] Accept: trial balance = f(posted entries); period close blocks back-dating.

## Phase 08 — HRM

- [ ] Desktop: `frmEmployees*`, `frmEmpSalaryAddSub*`, `frmAttendM*`, `frmSalaryPay*`,
  `frmJobs*`, `frmDepartments*`, `frmEmpAccountGet*`, `frmCustody*` (find exact form),
  `Class/AddSubEmployee.cs`, `Class_WPF/SalaryRow.cs`.
- [ ] Cloud: `modules/hrm/**`, staff `/hrm/**`.
- [ ] Behaviours: salary items (+/-), attendance import + summary, payroll run →
  approve → post → pay → reverse, employee custody, end-of-service (check desktop).
- [ ] Accept: payroll posts accrual + payment entries; reversal restores balances.

## Phase 09 — Verticals

- [ ] Contracting/projects: `InvoiceOperContract.cs`, `frmProject*`, `frmContract*`,
  `frmStage*`, `frmTerms*`, `frmProjCyclePM*`, `frmRequirementPM*` → `modules/projects|contracting`.
- [ ] Marina: `frmBookingM*`, `frmViolationM*`, `frmOwners*`, `frmGroupM*`,
  rental invoices `RptRentInvData.cs` → `modules/marina`.
- [ ] Optics: `frmGlasses*`, `glassOtions` (InvoiceOper L3904) → `modules/optics`.
- [ ] Tailoring: `frmMeasurement*` → `modules/tailoring`.
- [ ] Orders/offers: `frmOrders*`, `OrdersManager.cs`, `ProductsManager.cs`.
- [ ] Salesmen/commission: `frmSalesMen*`, `frmInvBySalesMen*`, `Class_WPF/SalesmanRow.cs`.
- [ ] Salla/online: `SallaAPI.cs`, `ManagerOnline.cs`, `ClListManagerOnline.cs`.
- [ ] Accept per vertical: desktop flow reproducible end-to-end on the cloud screen.

## Phase 10 — Reports (95 `.repx`)

- [ ] Desktop: `Reports/*.repx` + `Class/Report.cs` + `frmRpt*` (35 viewer/filter forms).
- [ ] Cloud: report engine TBD (first task of the phase: pick renderer — e.g. stored
  query + React print templates — then port in batches: sales → inventory → accounting
  → treasury → HRM → verticals).
- [ ] Accept: every ported report matches desktop columns/filters; print-ready Arabic RTL.

## Phase 11 — Zatca / ETA / integrations

- [ ] Desktop: `ZatcaService.cs`, `EtaService.cs`, `EtaReciptService.cs`,
  `frmZatcaSetting*`, `frmEtaSetting*`, `frmSentEinvoice*`, `frmInvsSyncStatusZatca*`,
  `Geidea.cs`, `NeoleapService.cs` (payments), `WhatsAppSender.cs`.
- [ ] Cloud: `modules/zatca|integrations/**`.
- [ ] Accept: onboarding → sign → send → poll → credit/debit-note flow certified
  against the Fatoora simulator; ETA sale receipts; payment-gateway tender in POS.
