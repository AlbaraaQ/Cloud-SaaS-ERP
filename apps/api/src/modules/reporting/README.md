# Reporting module

Phase 14 adds a registry-driven report read side. Report keys are centrally registered
in `REPORT_KEYS`; adding a report requires a key, parameter contract, query builder,
row shape, export support, and golden tests.

Reports read immutable journals, inventory ledgers, invoices, vouchers, and shift-close
facts. They do not mutate business state or cache totals on master records.

Async exports return a `reports-export` queue token; later worker/rendering phases can
attach generated CSV/XLSX/PDF artifacts to the files table.

## 📊 حركة المبيعات — `frmRptSalesInPeriod` (phase 10, part one)

`Form_WPF/frmRptSalesInPeriod.xaml` (Title «حركة المبيعات») is one window with two tabs and
one number under each grid. Both are registered here as ordinary catalogue definitions:

| Key | Tab | Columns |
|---|---|---|
| `sales-movement-items` | 📊 إجمالي المبيعات | رقم الصنف · الصنف · الكمية · الإجمالي |
| `sales-movement-invoices` | 🧾 عرض الفواتير | رقم الحركة · رقم الفاتورة · نوع الفاتورة · التاريخ · الوقت · آجل · نقدي · شبكة · الإجمالي · الضريبة · الخصم · الصافي |

Three catalogue features carry what the desktop's `Reports/*.repx` files carried and this
catalogue did not have:

- **`grandTotal: { key, labelAr }`** — 💰 «إجمالي المبيعات», the `txtSumSale`/`txtSumSale2`
  box under the grid. It is summed from the rows on screen, so it can never disagree with
  them. `key` may point at a **hidden** column (`net_signed`), which is how «المبيعات −
  المردودات» is totalled while every row stays positive like the desktop's grid.
- **`emptyAr`** — what an empty report says; `frmRptSalesInPeriod` says
  «لا توجد عمليات بالجدول».
- **`signature: true`** — «أعده · راجعه · المدير», the strip of `RptSalesInPeriod1/2.repx`.

And two filter kinds: **`time`** (⏰ الوقت (HH:mm:ss), glued to the date box by
`BuildDateTime`) and **`invType`** (🧾 نوع الفاتورة: `pos` = cash sale with no عميل,
`sale` = with one — the desktop's `inv_type` 3/2).

`GET /reports/print/:key` answers `{ html }` — the same print-ready page, with «المستخدم»
resolved from the caller. It is declared before `@Get(':key')` for the same reason
`layouts` is.

## 📦 تقارير الأصناف — `frmRptItems*` (phase 10, part two)

Six desktop windows that all read `inv_sub` grouped over `Items`, registered here as seven
ordinary catalogue definitions:

| Key | Window (`Form_WPF`) | Columns |
|---|---|---|
| `items-sales-summary` | `frmRptItemsSalesDetails` — «مبيعات الأصناف تجميعي» | رمز الصنف · الصنف · المجموعة · الكمية · صافي البيع |
| `items-purchases-summary` | the same window at `OperType = 2` — «مشتريات الأصناف تجميعي» | … · صافي الشراء |
| `items-pos-sales-summary` | `frmRptItemsSalesDetailsPOS` — «…- نقطة البيع» | the same five, restricted to فواتير نقطة البيع |
| `items-profit-summary` | `frmRptItemsProfit` — «أرباح المواد تجميعي» | رمز المادة · المادة · الكمية · متوسط التكلفة · صافي البيع · الربح · نسبة الربح |
| `items-profit-details` | `frmRptItemsProfitDetails` — «أرباح المواد تفصيلي» | الرقم · التاريخ · نوع العملية · المستودع · رمز المادة · المادة · الوحدة · الكمية · متوسط التكلفة · إجمالي التكلفة · السعر · المجموع · الإجمالي · الخصم · الربح · نسبة الربح % |
| `items-sales-by-category` | `frmRptSalesByCategory` — «تقرير مبيعات الأصناف حسب المجموعة» | المجموعة · اسم المجموعة / الصنف · الرمز · إجمالي الكمية · الإجمالي · الضريبة · الصافي · الخصم |
| `category-sales-by-day` | `frmRptCategorySaleByDay` — «تقرير المبيعات اليومية للمجموعة» | الرمز · المجموعة · اليوم · التاريخ · الإجمالي |

**`grandTotal` is now one card or many.** Every one of these windows prints several numbers
under its grid, so the catalogue field takes `ReportGrandTotal | ReportGrandTotal[]` and
`run()` answers `grandTotal: Array<{ key, labelAr, amount }>` — each card summed from the
rows on screen, each `key` a column of the report (hidden ones included). The catalogue
listing carries both `grandTotal: string[]` (labels, as before) and
`grandTotalCards: Array<{ key, labelAr }>`, and `reportSheet()` draws the `.totals-strip` as
a wrapping row of `<span>`s, one per card.

Two rules the desktop files state and the SQL keeps:

- **`if (!hasMovement) continue;`** (`frmRptItemsSalesDetails.xaml.cs` L282,
  `…POS` L223, `frmRptItemsProfit` L220) — *any* movement, not a non-zero net, so
  `HAVING sum(line.quantity) <> 0`. An item sold out and fully returned is still a row,
  which is deliberately the opposite of `sales-movement-items` (`if (qty == 0.0) continue;`).
- **The header discount is already on the line.** `frmRptItemsProfit.GetSaleData` divides it
  in the report (`ItemPriceWithoutVAT * minus / NULLIF(InvSum,0)`); the cloud divides it at
  save time (`calculateInvoiceTotals` spreads `invoiceDiscount` pro-rata by gross into every
  `line.net`), so these reports read `line.net` as it stands and «الخصم» is `gross − net`.

Shared scopes: `movementLinesScope(tenantId, f, pos, opts)` and `purchaseLinesScope`
carry «وثيقةٌ مرحَّلة فقط» plus the 🔧 خيارات البحث boxes each window actually owns — the
`opts` switches leave out the فرع and مجموعة boxes a window does not have.

## 🧾 تقارير الفواتير والإشعارات والحركة اليومية — `frmRptInv*` (phase 10, part three)

Seven definitions out of eight desktop windows; all of them read the **invoice header**,
not its lines:

| Key | Window (`Form_WPF`) | Notes |
|---|---|---|
| `sales-invoices-details` | `frmRptInvSalesDetails` — «تقرير فواتير المبيعات» | 21 columns, 10 cards (the «المدفوع» one included) |
| `pos-sales-invoices-details` | `frmRptInvSalesDetailsPos` — «تقرير مبيعات الفواتير» | the same row, `inv.inv_type = 3` hard-coded; 9 cards |
| `sales-notifications` | `frmRptInvNotfic` — «تقرير الإشعارات» | the same row, `inv_type` 21/22; «نوع الإشعار · رقم الإشعار · تاريخ الإشعار» |
| `purchase-invoices-details` | `frmRptInvPurchaseDetails` — «تفاصيل فواتير المشتريات» | 17 columns, «المدفوع · المتبقي», 5 cards |
| `daily-sales` | `frmRptDailySales` — «تقرير مبيعات حسب اليوم» | `dbo.SalesByDay`, day named `ToString("ddd", ar)` |
| `daily-process` | `frmRptDailyProcess` — «تقرير الحركة اليومية» | six `DoProcess` rows in a fixed order |
| `sales-inv-analysis` | `frmRptInvAnalysis` — «تقرير تحليل المبيعات» | eight `ANALYSIS_DIMENSION` radios |

`frmRptInvSalesDetailsPosAndroid` («تقرير مبيعات أندرويد») is **not** a separate definition:
it is the `inv_type = 20` arm of the same «📄 نوع الفاتورة» combo and prints the very same
`rptInvSumtPos.repx`.

**One row shared by three windows.** `invoiceRow` is the SELECT list of all three; the SQL
that differs is `invoiceScope(tenantId, f, pos, { notifications })`, which swaps
`kind IN ('sale','sale_return')` for `kind IN ('credit_note','debit_note')` and applies
`procScope` or `notificationScope` instead.

**Signed cards.** `UpdateSummaryCards()` at the desktop sums with
`Calc(x) = purchases.Sum(x) − returns.Sum(x)`, so the ten cards read the `s_*` mirrored
columns (`s_sum_price` … `s_paid`), each multiplied by
`invoiceSign = CASE WHEN si.kind IN ('sale','debit_note') THEN 1 ELSE -1 END`. A مرتجع
subtracts, and so does an إشعار دائن. The `s_*` columns are `hidden: true` — they are
printed in the 💰 strip, never drawn in the grid.

**`GetPaymentText`** is one CASE over `payment_status` and the three payment legs
(`paymentLegs` is a `LEFT JOIN LATERAL` over `invoice_payments`): «آجل» when nothing was
paid, «نقدي»/«شبكة» when one leg settled the invoice, «متعدد» when several did.

**Purchases settle by سند صرف**, not by a `pay_type` flag: `payment_allocations` →
`vouchers` gives the same three legs for «💳 نوع الدفع».

## Saved layouts — مصمم التقارير

`report_layouts` (migration `0028`) is the whole persistence of the report designer, and it
stores **presentation only**: which of a report's own columns are visible, in what order,
under what heading, plus default filters and an `is_default` flag per report.

| Route | Permission |
|---|---|
| `GET /reports/layouts?report_key=` | `reporting.view` |
| `POST /reports/layouts` | `reporting.layout.manage` |
| `PATCH /reports/layouts/:id` | `reporting.layout.manage` |
| `DELETE /reports/layouts/:id` | `reporting.layout.manage` |

The report's SQL is never user-authored. `normalizeColumns` drops any key the registered
report does not produce and refuses a layout with nothing left visible
(`REPORT_LAYOUT_EMPTY`), and an unknown `reportKey` is a 404 on save. That is what keeps a
"designer" from becoming a query editor pointed at other tenants' data — and it means a
layout keeps working when the report behind it is rewritten.

`GET /reports/:key` accepts `?layout=<id|name>`; omitting it applies the report's default
layout, and `?layout=none` forces the raw column set. Layout filters are merged *under* the
caller's own filters, so a saved default is a starting point rather than a cage.

These layout routes are declared **before** `@Get(':key')` in the controller — otherwise
`/reports/layouts` would be parsed as a report named "layouts".

Both `ReportingService` and `ReportLayoutsService` take the injected `DATABASE_HANDLE`
rather than the module-level `getDatabase()` singleton, so a test (or any second pool) runs
reports against the database it was actually given.

## Printed documents

`PrintTemplatesService` renders the six papers a company hands out: the sales invoice, the
purchase invoice, the receipt/payment voucher, the journal voucher and the daily shift
close. Each route returns `{ html }` — one self-contained A4 page with its own stylesheet,
no external font, image or script — because the admin shows it inside a sandboxed iframe
and the same string is what gets saved to disk or sent to a printer.

| Route | Document |
|---|---|
| `GET /reports/print/invoices/:id` | فاتورة مبيعات / مردود / عرض سعر |
| `GET /reports/print/purchase-invoices/:id` | فاتورة مشتريات / مردود مشتريات |
| `GET /reports/print/vouchers/:id` | سند قبض / سند صرف |
| `GET /reports/print/journal-entries/:id` | سند قيد |
| `GET /reports/print/shifts/:id` | إغلاق اليومية |

All five require `reporting.view` and are tenant-scoped through `withTenantTx`, so a
document id from another tenant is a 404 rather than a leak.

Two details are deliberate:

- **The amount in words** (`tafqeet.ts`) is printed on every money document. It implements
  the conventional accounting form — unit before ten (`واحد وعشرون`), the dual
  (`مائتان`, `ألفان`), the 3–10 plural (`ثلاثة آلاف`) and the accusative singular after
  11–99 (`خمسة عشر ريالاً`) — and names the currency and its fraction from an ISO code.
- **The ZATCA QR** is drawn only when the invoice actually carries a reported TLV payload.
  Until then the slot prints a sentence saying so, because a decorative square that no
  scanner can read is worse than an honest gap.

## Exports

`POST /reports/:key/export` (permission `reporting.export.execute`) re-runs the report
server-side with the query string it was given — the same filters and the same saved layout
as the screen — and returns a finished file:

| `format` | Payload | Notes |
|---|---|---|
| `csv` | `encoding: 'utf-8'` | Leading BOM, so Excel on Windows reads Arabic instead of mojibake. |
| `xlsx` | `encoding: 'base64'` | A real workbook written by `xlsx.ts`: right-to-left sheet, frozen header, auto-filter, `#,##0.00` numeric cells and a bold totals band. |
| `pdf` | `encoding: 'utf-8'` | A print-ready A4 **landscape** page on the company letterhead; the browser's print dialog turns it into a PDF. |

Three decisions worth keeping:

- **The client never builds the file.** It used to serialise the rows already on screen, which
  ignored the layout and silently dropped anything not rendered. The export is now the report,
  not a screenshot of it.
- **Only clean decimals become numeric cells.** An account code (`1101`), a document number
  (`SI-000006`) or anything with leading zeros stays text, so Excel cannot "helpfully" turn it
  into a number or a date.
- **Filters are spelled out in the header band.** `branchId=<uuid>` prints as
  `الفرع: الفرع الرئيسي`; the lookup is best-effort and falls back to the raw value, because a
  caption must never be the reason an export fails.

There is no PDF renderer in the process on purpose: producing Arabic PDF text requires
embedding a font with contextual shaping, and the print dialog already produces a smaller,
selectable, better-typeset document.
