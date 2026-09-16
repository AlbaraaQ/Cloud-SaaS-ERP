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
- [x] Screens (part four): a shared component layer (`PageHeader`/`FilterBar`,
  `StatTiles`, an advanced `DataTable`, `StatusTrack`, `Empty/Loading/ErrorState`,
  `ActionBar`, `Tabs`) plus ~320 lines of design-system CSS (tiles, bars, steppers,
  tabs, split layout, totals, print styles), applied to the 21 `/inventory/*`
  screens — **designed from `Desktop_ERP`**: tabs of `frmItems.xaml`
  (عام / وحدات / بضاعة أول المدة / المكونات), document headers of
  `frmInvInOutput.xaml` and `frmInventoryTransfer.xaml`, grids of
  `frmProductionOrder.xaml`, `frmItemsExpire.xaml`, `frmItemsLimit.xaml` and
  `frmMultiBarcode.xaml`, the layouts of `Reports/*.repx` (`rptItemDetails`,
  `RptInvInOutput`, `rptInventoryTransfer`, `rptItemsExpire`, `rptItemsDirectory`,
  `rptProductionOrder`, `Barcode`), and the behaviour of `Class/Inventory.cs`,
  `Class/ItemOper.cs` (س 744-760), `Class/InvoiceOper.cs` (س 5031) and
  `Class/ItemComponent.cs`. Vouchers, transfers, adjustments, the item card and the
  in-transit report became split master/detail screens with tiles, status tracks and
  totals; `items` became a four-tab card; the expiry and in-transit reports gained
  ageing buckets; and a new `/inventory/overview` dashboard closes the module.
  Presentation layer only — no endpoint changes. Measured before: `.kpi`/`.state`/
  `.skeleton` had **0** uses in `app/inventory/`, 8 screens hand-wrote `<table>`.
  Spec: `PHASE_05_INVENTORY.md` §8. Still open inside it: the المكونات tab (waits for
  `item_components` to be wired into production orders) and barcode-label printing
  (phase 10).
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

## Phase 10 — Reports (94 `.repx`) 🟡 parts 1–3 done (2026-09, `PHASE_10_REPORTS.md`)

- [x] Desktop: `Reports/*.repx` (94) + `Class/Report.cs` (610) + `Class/Print.cs` +
  `frmRpt*` (32 viewer/filter forms) + `Reports/header.repx`/`footer.repx` +
  `SettingPrint` (`CrystalLiteDB.txt` L2260).
- [x] Cloud engine (already there before this phase): `modules/reporting/**` —
  `report-catalog.ts` (81 definitions), `reporting.service.ts`, `print-templates.service.ts`,
  `report-layouts.service.ts` (مصمّم التقارير), `xlsx.ts`, `tafqeet.ts`; staff
  `/reports` centre + `/reports/[key]` runner.
- [x] Part one — 📊 حركة المبيعات: `frmRptSalesInPeriod` + `RptSalesInPeriod1/2.repx`
  (both tabs, both «💰 إجمالي المبيعات», ⏰ الوقت, 🧾 نوع الفاتورة, 🖨️ طباعة).
- [x] Part two — 📦 reports of الأصناف: `frmRptItemsSalesDetails` ·
  `frmRptItemsSalesDetailsPOS` · `frmRptItemsProfit(Details)` · `frmRptSalesByCategory` ·
  `frmRptCategorySaleByDay` — seven cloud reports (مبيعات · نقطة البيع · أرباح تجميعي ·
  أرباح تفصيلي · حسب المجموعة · اليومية للمجموعة · مشتريات), 💰 summary cards now come in
  lists, 14 API tests + `scripts/verify-reports-items.mjs` (126 checks).
- [x] Part three — 🧾 reports of الفواتير والإشعارات والحركة اليومية:
  `frmRptInvSalesDetails` · `frmRptInvSalesDetailsPos` (+`PosAndroid` merged into the
  «أندرويد» arm of نوع الفاتورة) · `frmRptInvNotfic` · `frmRptInvPurchaseDetails` ·
  `frmRptDailySales` · `frmRptDailyProcess` · `frmRptInvAnalysis` — seven cloud reports
  (one invoice row shared by three windows, signed 📊 ملخص النتائج cards where a مرتجع
  and an إشعار دائن subtract), 15 API tests + `scripts/verify-reports-invoices.mjs`
  (159 live checks).
- [ ] Part four — 📚 inventory + serials: `frmRptInventory` ·
  `frmRptItemsActivity(Detailed)` · `frmRptItemsExpiration` · `frmRptSerialNo(Summary)` ·
  `frmRptProducedItems`.
- [ ] Part five — 📒 accounting: `frmRptBalances` · `frmRptEntries` ·
  `frmRptIncomeStatement` · `frmRptCostCenter` · `frmTaxRptPeriod`.
- [ ] Part six — 💰 treasury · payroll · verticals: `frmRptKhzna` · `frmRptSalary` ·
  `frmRptReseved` · `frmrptUsersRecords` · `frmRptRentInvoices` · `frmInvRptType`.
- [ ] Part seven — 🖨️ `SettingPrint`: print header/footer/stamp, copies and the default
  printer per report; `HeaderImage` · `FooterImage` · `StampImage`.
- [ ] Accept (each part): every ported report matches desktop columns/filters; print-ready
  Arabic RTL; new tests + a re-runnable live script; a real route in the staff tree.

## Phase 11 — Zatca / ETA / integrations 🟡 parts one–three, five and six done (2026-09, `PHASE_11_EINVOICING.md`)

- [x] Desktop (part one): `frmZatcaSetting.xaml` (472) + `.xaml.cs` (1160),
  `Class/ZatcaService.cs` (546), `Class/ZatcaCredential.cs`,
  `Class/CustZatcaEndDate.cs`, and the three tables `SettingZatca` · `CSRProperties` ·
  `ZatcaCredential`.
- [x] Cloud (part one): `modules/einvoicing/**` — `einvoice_settings` (migration `0062`)
  plus `request_id` and the `P_*` pair on `einvoice_credentials`;
  `zatca/csr.ts` (a real PKCS#10 request), `zatca/gateway.ts` (🧪 simulation · 🔵 sandbox ·
  🔴 core), `zatca/compliance-check.ts` (the six documents),
  `zatca-onboarding.service.ts`; staff `/settings/zatca`.
- [x] Part one — ⚙️ إعدادات الربط الضريبي: 🔄 تعبئة تلقائي · ⚡ توليد · 🔵 Compliance
  CSID · 🔐 Get PCSID · 🧪 اختبار الربط · 🔄 Renews CSID · ⏸ إيقاف الربط (18 tests +
  64 live checks).
- [x] Part two — 🧾 send, sign and chain: `frmSentEinvoice.xaml` (358) + `.xaml.cs` (304)
  and the grid of `frmInvsSyncStatusZatca.xaml` (559), behind
  `ZatcaService.IntegrateInvoice` (L78-L410) and `InvoiceOper.SendZatca` (L2209).
  Migration `0063` adds `chain_index` · `authority_status` · `cleared_invoice` to
  `einvoice_submissions` (`ZatcaResponse` in the desktop); `zatca/filing.ts` reports a
  `0200000` to `/invoices/reporting/single` and clears a `0100000` through
  `/invoices/clearance/single`, then reads the QR out of the **returned** document with the
  desktop's own XPath (L474-L477). Three endpoints: `GET /einvoice/filings` (paged, joined
  to the invoice), `GET /einvoice/filings/:id` (both documents, the eight tags decoded,
  the chain slot) and `GET /einvoice/chain`; `POST /einvoice/submissions/:id/retry`
  re-files a stored document without moving its hash. Staff `/settings/zatca/sent`.
  (16 tests + 53 live checks.)
- [x] Part three — 📊 `frmInvsSyncStatusZatca.xaml` (559) + `.xaml.cs` (1165): the window is
  a registered report (`einvoice-sync-status`) — the grid's eleven columns, 🔄 حالة
  المزامنة, 📋 نوع الفاتورة (مبيعات · نقطة بيع · إشعار · مقاولات · أندرويد), 📅 الفترة
  الزمنية, 💰 «الصافي» = المبيعات − المردودات — so 🖨️ طباعة, 👁️ معاينة and 📊 تصدير Excel
  go through the report engine the way the desktop's `PrintReport` goes through
  `rptInvSumByClient.repx`; and `POST /einvoice/sync` (permission `einvoice.submit`) is
  🔄 مزامنة ZATCA, which files the ticked rows and answers `sent` / `failed` / `skipped`
  per row. (16 tests + 53 live checks.)
- [x] Part five — 💳 بوابات الدفع: `frmSettings.xaml` L1726-L1831
  («إعدادات جيديا» + GroupBox «NeoLeap») with `Class/Geidea.cs` (57) and
  `Class/NeoleapService.cs` (165), and the two POS save paths that charge a card
  (`frmPOSBill.xaml.cs` L460-L492, `frmPOSPay.xaml.cs` L428-L441). Migration `0064` adds
  `payment_gateway_settings` (the desktop's `GediaSetting` and `SettingNeoleap` rows, one
  per tenant and provider) and `payment_gateway_transactions` — the log the desktop never
  kept. `gateways/geidea.ts` speaks the provider's published API (KSA host
  `https://api.ksamerchant.geidea.net`: create session, query the order by
  `MerchantReferenceId`, HMAC-SHA256 signature) and `gateways/neoleap.ts` speaks the
  connector's own `SALE` request and reads `00` / `01` / `02` back. Six endpoints:
  `GET /payment-gateways`, `PUT /payment-gateways/:provider` (💾 حفظ),
  `POST …/:provider/test` (🧪 TEST · 🧪 Test), `POST …/:provider/sale` (💳, settling an
  approved amount on the invoice once), `GET /payment-gateways/transactions` (📜) and
  `POST /payment-gateways/transactions/:id/refresh` (🔄). Staff
  `/settings/payment-gateways`. (16 tests + 64 live checks.)
- [⛔] Part four — 🇪🇬 `frmEtaSetting` + `EtaService` + `EtaReciptService`: **out of
  scope by decision** — the product targets Saudi Arabia (ZATCA) today; the sources stay
  listed in `PHASE_11_EINVOICING.md` §1 and the reason is recorded in §11. The two
  ETA-only calls (🚫 إلغاء الفاتورة · ❌ رفض الفاتورة) go with it.
- [x] Part six — 📱 واتساب: `frmInvSale.xaml` L1190 «💬 واتساب» and its handler
  (`frmInvSale.xaml.cs` L3124-L3199 `printwhatsapp`), behind `Class/WhatsAppSender.cs`
  (267 — Selenium ChromeDriver over a persistent `chrome-profile`) and
  `Class/Session.cs` (L12-L31). Migration `0065` adds `whatsapp_settings` — the number,
  the sealed token, the country code, 📎 and 🧪 — and `whatsapp_messages`, the log the
  desktop never kept. `modules/integrations/whatsapp/**` speaks the Cloud API as
  published (Graph `v21.0`: text message, `POST /{phone-number-id}/media` upload, document
  message, and `GET /{phone-number-id}` for 🧪 اختبار), builds the desktop's own greeting
  («🧾 مرحباً … هذه فاتورتك رقم … من …») and dials the number the desktop's own rule
  produces (`if (!text.StartsWith("966")) text = "966" + text.TrimStart('0');`). Five
  endpoints: `GET`/`PUT /whatsapp/settings` (💾 حفظ), `POST /whatsapp/test` (🧪 اختبار),
  `POST /whatsapp/send` (💬 واتساب, `sales.view` — whoever may open the invoice window)
  and `GET /whatsapp/messages` (📜). Staff `/settings/whatsapp`, plus a «💬 واتساب» card
  with its own 📜 سجل الإرسال on the invoice window itself. (17 tests + 71 live checks.)
- [ ] Accept: onboarding → sign → send → poll → credit/debit-note flow certified
  against the Fatoora simulator; payment-gateway tender in POS; WhatsApp delivery against
  a live number (🧪 محاكاة is what the tests and the live script exercise today).
