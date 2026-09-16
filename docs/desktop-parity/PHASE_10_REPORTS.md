# المرحلة 10 — التقارير (94 `.repx` → محرّك التقارير في السحابة)

**الحالة: الأجزاء السبعة مُنجزة** — من 📊 حركة المبيعات (`Form_WPF/frmRptSalesInPeriod`
+ `Reports/RptSalesInPeriod1.repx` + `RptSalesInPeriod2.repx`) إلى آخرها 🖨️ إعدادات
الطباعة: `frmSettings.xaml` «خيارات الطباعة» + `frmInvRptType.xaml` «🖨️ افتراضي طباعة
الفواتير» + `Class/Print.cs` + `Reports/header.repx`/`footer.repx`، وجدول
`print_settings` (migration `0061`) بسبعة نطاقات ونطاقٍ لكل تقرير؛ **15** اختباراً
و**34** نقطة تحقّق حيّة في الجزء السابع، و**852** اختبار API في المجموع. الأجزاء
مبيّنة في §3، ومعايير القبول في §12، وما أُجِّل عن قصد في §13.

الغرض: نقل **التقارير** كما يبنيها الديسكتوب — أعمدتها وفلاترها ومجاميعها وصفحة
طباعتها — لا اختراع تقارير جديدة. المحرّك موجود في السحابة منذ الإصدار الأول
(`modules/reporting` بسجلّ تعريفات ومصمّم تخطيط وتصدير XLSX وتفقيط)؛ وهذه المرحلة
تُتمّ **كل تقريرٍ على حدة**: تعريفٌ بأعمدة نافذته وفلاترها، وشاشة، واختبارات، وتحقّق
حيّ، وطريقٌ في الشجرة.

كل قسم في هذه الوثيقة يُسمّي ملفه من `Desktop_ERP` نصّاً، وتسمياته مأخوذة من تلك
الملفات بالعربية. أي تسمية مخترَعة تُبرَّر صراحةً في ملخّص الجزء.

## 1. المصادر (ملفات `Desktop_ERP`)

| المجال | الملفات |
|---|---|
| ملفات التخطيط | `Reports/*.repx` — **94** ملفاً (DevExpress 18.2) |
| نوافذ العرض والترشيح | `Form_WPF/frmRpt*.xaml` + `.xaml.cs` — **32** نافذة (الجدول أدناه) |
| حشو البيانات وطباعتها | `Class/Report.cs` (610 سطراً: `BindToData` L380 وL533 · `Printing` L463 · `CreateDgv` L19 · `LoadDGvSetting` L248) · `Class/Print.cs` · `Class/InvPrinter.cs` |
| الرأس والتذييل | `Reports/header.repx` (المنشأة · النشاط · الرقم الضريبي · السجل التجاري · هاتف · جوال) · `Reports/footer.repx` (العنوان · هاتف) · `Common.FoundationInfoDT` |
| إعدادات الطباعة | جدول `SettingPrint` — `CrystalLiteDB.txt` L2260-L2280 (`Inv_Id` · `PrintTotItem` · `PrintTotGroup` · `PrintHeader` · `PrintFooter` · `PrintStamp` · `printNo` · `printType` · `CasherPrinter` · `Kitchenprinter` · `RptName` · `RptUrl` · `note` · `PrintItemType` · `PrintComponentsItemsIndividually` · `printmakpay` والصور الثلاث)؛ تكتبه `frmSettings.xaml.cs` L2125-L2156 وتقرأه `Class/Print.cs`؛ و`frmRptSalesInPeriod` يقرأ `Inv_Id=12` و`frmRptKhzna` يقرأ 12 و`frmRptEntries` يقرأ 9 و`frmRptRentInvoices` يقرأ 14 — نُقل في الجزء السابع (§10) |

### النوافذ الاثنتان والثلاثون (`Form_WPF/frmRpt*`)

| النافذة | العنوان | ملفات التخطيط |
|---|---|---|
| `frmRptSalesInPeriod` | حركة المبيعات | `RptSalesInPeriod1` · `RptSalesInPeriod2` · header · footer |
| `frmRptSalesByCategory` | تقرير مبيعات الأصناف حسب المجموعة | `RptItemSalesByCategory` |
| `frmRptCategorySaleByDay` | تقرير المبيعات اليومية للمجموعة | `RptCategorySaleByDay` · header · footer |
| `frmRptItemsSalesDetails` | مبيعات الأصناف تجميعي | `RptItemsSalesDetails` · `RptItemsPurchDetails` · header · footer |
| `frmRptItemsSalesDetailsPOS` | مبيعات الأصناف تجميعي - نقطة البيع | `RptItemsSalesDetailsPos` · `RptItemsPurchDetailsPos` |
| `frmRptItemsProfit` | أرباح المواد تجميعي | header · `rptItemsProfit` |
| `frmRptItemsProfitDetails` | أرباح المواد تفصيلي | `RptItemsProfitDetails` · header · footer |
| `frmRptInvSalesDetails` | تقرير فواتير المبيعات | `rptInvSumByClient` |
| `frmRptInvSalesDetailsPos` | تقرير مبيعات الفواتير | `rptInvSumtPos` |
| `frmRptInvSalesDetailsPosAndroid` | تقرير مبيعات أندرويد | `rptInvSumtPos` |
| `frmRptInvNotfic` | تقرير الإشعارات | `rptInvSumByClient` |
| `frmRptInvPurchaseDetails` | تفاصيل فواتير المشتريات | `rptInvSumBySupplier` |
| `frmRptDailySales` | تقرير مبيعات حسب اليوم | `daysalesPos` · header · footer |
| `frmRptDailyProcess` | تقرير الحركة اليومية | `rptDailyProcess` · header · footer |
| `frmRptInvAnalysis` | تقرير تحليل المبيعات | header · footer |
| `FrmRptSalesChart` | تقرير بياني للمبيعات | — (رسم بياني) |
| `frmRptInventory` | تفاصيل فواتير المشتريات | `rptInventoryReport` |
| `frmRptItemsActivity` | مادة باجمالي الحركات | `rptItemsTotalGrd` · header · footer |
| `frmRptItemsActivityDetailed` | حركة صنف تفصيلي | `rptItemDetails` · header · footer |
| `frmRptItemsExpiration` | صلاحية المواد | `RptItemsExpiration` |
| `frmRptSerialNo` | حركة الأرقام التسلسلية | `rptSerialNo` · header · footer |
| `frmRptSerialNoSummary` | أرصدة الأرقام التسلسلية | `rptSerialNoSummary` · header · footer |
| `frmRptProducedItems` | تقرير مواد المنتجة | — |
| `frmRptBalances` | أرصدة الحسابات | `rptAccountBalance` · header · footer |
| `frmRptEntries` | القيود اليومية | `Entry` · header · footer |
| `frmRptIncomeStatement` | أرباح وخسائر حسابات رئيسية | `RptIncomeStatement` · header · footer |
| `frmRptCostCenter` | تقرير مراكز التكلفة | `RptCostCenter` · `RptCostCenterDetails` · header · footer |
| `frmTaxRptPeriod` | الإقرار الضريبي | `TaxRptPeriod` · `TaxRptPeriodNew` |
| `frmRptKhzna` | حركة الصندوق | `RptKhzna` · header · footer |
| `frmRptSalary` | تقرير الرواتب | — |
| `frmRptReseved` | تقرير الرواتب المستحقة | — |
| `frmRptRentInvoices` | تقرير فواتير التأجير | `Statement` |
| `frmrptUsersRecords` | سجلات المستخدمين | — |
| `frmInvRptType` | أنواع الفواتير (تصنيف للتقارير) | — |

## 2. ما هو موجود في السحابة قبل الجزء الأول (قياس)

| السطح | ما كان موجوداً |
|---|---|
| `apps/api/src/modules/reporting/report-catalog.ts` | **65** تعريفاً: `key` · `titleAr` · `group` · `hintAr` · `params` · `columns` · `totals` · `chart` · `build()`؛ وفلاتر `from` · `to` · `branchId` · `warehouseId` · `partyId` · `itemId` · `categoryId` · `salesmanId` · `costCenterId` |
| `reporting.service.ts` | `GET /reports` (السجل) · `GET /reports/:key` (التشغيل) · `POST /reports/:key/export` (`csv` · `xlsx` · `pdf`) · تسطيح الفلاتر بـzod · مجاميع الأعمدة · أسماء الفلاتر للطباعة |
| `print-templates.service.ts` (723) | `reportSheet()` — صفحة A4 بالرأس (المنشأة · الرقم الضريبي · السجل التجاري) والشبكة والمجاميع؛ وطباعة الفاتورة والسند والقيد والإغلاق |
| `report-layouts.service.ts` | مصمّم التقارير (تخطيط محفوظ لكل مؤسسة: ترتيب الأعمدة وتسميتها وفلاترها) |
| `xlsx.ts` · `tafqeet.ts` | مصنف حقيقي (ورقة RTL ورأس مثبّت) · تفقيط المبالغ |
| `apps/staff/app/reports/` | «مركز التقارير» `/reports` ومشغّل واحد `/reports/[key]` لكل تقرير، وصفوف «التقارير» في الشجرة |

**ما كان ناقصاً** — وهو ما يبدأ به الجزء الأول: لا تقريرٌ في السجل مأخوذ من ملفّات
الديسكتوب (العناوين والأعمدة من اختيار السحابة)، ولا صندوق ⏰ وقت بجانب صندوق التاريخ،
ولا عمود «💰 إجمالي المبيعات» تحت الشبكة، ولا «المستخدم» ولا شريط «أعده · راجعه ·
المدير» في صفحة الطباعة، ولا جملة التقرير الفارغ.

## 3. الأجزاء

| الجزء | النوافذ | الحالة |
|---|---|---|
| 1 | 📊 تقارير المبيعات — `frmRptSalesInPeriod` (`RptSalesInPeriod1/2`) | ✅ مُنجز (§4) |
| 2 | 📦 تقارير الأصناف — `frmRptItemsSalesDetails` · `frmRptItemsSalesDetailsPOS` · `frmRptItemsProfit` · `frmRptItemsProfitDetails` · `frmRptSalesByCategory` · `frmRptCategorySaleByDay` | ✅ مُنجز (§5) |
| 3 | 🧾 تقارير الفواتير والإشعارات والحركة اليومية — `frmRptInvSalesDetails` · `frmRptInvSalesDetailsPos(Android)` · `frmRptInvNotfic` · `frmRptInvPurchaseDetails` · `frmRptDailySales` · `frmRptDailyProcess` · `frmRptInvAnalysis` · `FrmRptSalesChart` | ✅ مُنجز (§6) |
| 4 | 📚 تقارير المخزون والأرقام التسلسلية — `frmRptInventory` · `frmRptItemsActivity(Detailed)` · `frmRptItemsExpiration` · `frmRptSerialNo` · `frmRptSerialNoSummary` · `frmRptProducedItems` | ✅ مُنجز (§7) |
| 5 | 📒 تقارير المحاسبة — `frmRptBalances` · `frmRptEntries` · `frmRptIncomeStatement` · `frmRptCostCenter` · `frmTaxRptPeriod` | ✅ مُنجز (§8) |
| 6 | 💰 تقارير الخزينة والرواتب والمستخدمين — `frmRptKhzna` · `frmRptSalary` · `frmRptReseved` · `frmrptUsersRecords` · `frmRptRentInvoices` · `frmInvRptType` | ✅ مُنجز (§9) — و`frmInvRptType` إلى الجزء السابع |
| 7 | 🖨️ إعدادات الطباعة — `SettingPrint` (رأس · تذييل · ختم · عدد النسخ · الطابعة) و`Reports/header.repx`/`footer.repx` لكل تقرير | ✅ مُنجز (§10) |
| 8 | 📑 كشوف الحساب — **خارج جدول `frmRpt*`**: `frmCustAccount` · `frmCustAccountGet` · `frmCustLastPay` (+ ⏰ الوقت و📋 نوع القيد على `frmAccountBalance` · `frmAccountsStatement` · `frmCostCenterBalance`) | ✅ مُنجز (§11) |

## 4. الجزء الأول — 📊 حركة المبيعات (`frmRptSalesInPeriod`)

### 4.1 النافذة وما تقرأه

`Form_WPF/frmRptSalesInPeriod.xaml` (565 سطراً، العنوان «حركة المبيعات») + `.xaml.cs`
(587) نافذةٌ ذات تبوبيْن يقرآن الوثائق نفسها:

- ⚙️ لوحة «إدخال التاريخ»: 🧾 نوع الفاتورة `cmbInvType` (**«مبيعات نقطة البيع»** ·
  **«مبيعات عادية»**) · 📅 التواريخ: «من التاريخ» + «الوقت (HH:mm:ss)» و«إلى التاريخ» +
  «الوقت (HH:mm:ss)».
- 📊 التبويب الأول «إجمالي المبيعات» (من `btnShow` «📊 إجمالي حركة المواد») — شبكة
  **رقم الصنف · الصنف · الكمية · الإجمالي**، وتحته **«💰 إجمالي المبيعات:»**
  (`txtSumSale`)؛ و`if (qty == 0.0) continue;` يُسقط صنفاً لم يُبع أصلاً.
- 🧾 التبويب الثاني «عرض الفواتير» — شبكة **رقم الحركة · رقم الفاتورة · نوع الفاتورة ·
  التاريخ · الوقت · آجل · نقدي · شبكة · الإجمالي · الضريبة · الخصم · الصافي**، وتحته
  **«💰 إجمالي المبيعات:»** (`txtSumSale2` = المبيعات − المردودات).
- الأسفل: «✖ خروج» · «📊 تصدير Excel» · «👁️ معاينة» · «🖨️ طباعة».

القواعد في `.xaml.cs`: `GetSaleData` تجمع `proc_type=1` (بيع) و`proc_type=2` (مرتجع)
لكل صنف بـ`SUM(val)` و`SUM(val * exchange_price)`؛ و`ShowResults` تشترط
`Proc_Type<>3 AND Proc_Type<>4 AND IS_Buy=0 AND IS_Deleted=0` وتسمّي «بيع»/«مرتجع»؛
و`IsPostpone` علامة `pay_type = -1`؛ و`PrintDevexpress` يرفض الطباعة بـ**«لا توجد
عمليات بالجدول»** إذا خلا الجدولان، ويقرأ `SettingPrint WHERE Inv_Id=12`، ويلصق
`header.repx` و`footer.repx`، ويمرّر `InventoryType = this.Title` (عنوان النافذة) إلى
كلا التقريرين.

### 4.2 السطح (نقاط النهاية)

| الطريقة | المسار | الإذن |
|---|---|---|
| `GET` | `/api/v1/reports` | `reporting.view` — السجل، وفيه التقريران بأعمدتهما وفلاترهما |
| `GET` | `/api/v1/reports/sales-movement-items` | `reporting.view` |
| `GET` | `/api/v1/reports/sales-movement-invoices` | `reporting.view` |
| `GET` | `/api/v1/reports/print/:key` | `reporting.view` — 🖨️ صفحة الطباعة (HTML جاهز للطباعة) |
| `POST` | `/api/v1/reports/:key/export` | `reporting.export.execute` — `csv` · `xlsx` · `pdf` |

الفلاتر: `from` · `to` · **`fromTime`** · **`toTime`** · **`invType`** (`pos` · `sale`) ·
`branchId`. وقتٌ بصيغة خاطئة يُرفض `422 VALIDATION_FAILED`.

### 4.3 مطابقة الأعمدة والتسميات

| عمود التقرير (الأصناف) | حقل الديسكتوب | الحقل في السحابة |
|---|---|---|
| رقم الصنف | `Items.id` | `items.sku` (`—` عند غيابه) |
| الصنف | `Items.name` | `items.name_ar` |
| الكمية | `SUM(val)` صافياً من المرتجع | `sum(CASE kind WHEN 'sale' THEN +qty ELSE −qty)` |
| الإجمالي | `SUM(val * exchange_price)` صافياً | `sum(CASE kind WHEN 'sale' THEN +total ELSE −total)` |
| 💰 إجمالي المبيعات | `txtSumSale` | `grandTotal` من صفوف التقرير |

| عمود التقرير (الفواتير) | حقل الديسكتوب | الحقل في السحابة |
|---|---|---|
| رقم الحركة | `InvGlobalID` | `sales_invoices.id` |
| رقم الفاتورة | `Inv.id` | `sales_invoices.number` |
| نوع الفاتورة | `proc_type` 1/2 | `kind` → «بيع» / «مرتجع» |
| التاريخ · الوقت | `Inv.date` | `posted_at::date` · `to_char(posted_at,'HH24:MI:SS')` |
| آجل | `pay_type = -1` | `payment_status = 'unpaid'` |
| نقدي · شبكة | `Inv.cash` · `Inv.visa` | `invoice_payments` بـ`method='cash'` / `method='card'` |
| الإجمالي · الضريبة · الخصم · الصافي | `InvTotal` · `tax` · `minus` · `tot_net` | `subtotal` · `tax_total` · `invoice_discount` · `total` |
| 💰 إجمالي المبيعات | `txtSumSale2` | `grandTotal` من `net_signed` (عمودٌ مخفيّ يحمل الإشارة) |

### 4.4 🖨️ الطباعة

`GET /reports/print/:key` يعيد `{ html }` — صفحة A4 عربية RTL تكمل `reportSheet()` بما
كان ناقصاً من `RptSalesInPeriod1/2.repx`:

- 🏢 رأس المنشأة من `company_profiles` (الاسم · الرقم الضريبي · السجل التجاري · الهاتف) —
  `header.repx`.
- 📄 عنوان التقرير (`InventoryType` = «حركة المبيعات») والفترة ونوع الفاتورة وعدد
  السجلات.
- 👤 **المستخدم** — `Common.GetEmpName(MainClass.EmpNo)` عند الديسكتوب، واسم المستخدم
  من سياق الطلب هنا — مع ⏰ «طُبع في».
- 💰 **إجمالي المبيعات** — سطرٌ موسومٌ تحت الشبكة كما تحت شبكة الديسكتوب.
- ✍️ شريط **أعده · راجعه · المدير** — `footer.repx`.
- «**لا توجد عمليات بالجدول**» بدل «لا توجد بيانات ضمن معايير البحث المحددة.» حين يخلو
  التقرير (جملة الديسكتوب نفسها، وكل تقريرٍ يقدر أن يختار جملته بـ`emptyAr`).

الشاشة تعرض «💰 إجمالي المبيعات» شريحةً مجاورةً لشرائح المجاميع، وصندوق وقت
(`<input type="time" step="1">`) لكل مرشِّح `kind: 'time'`؛ والشجرة اكتسبت صفّي
**«إجمالي حركة المواد»** و**«عرض الفواتير»** تحت «التقارير» في وحدة المبيعات.

### 4.5 التحويلات عن الديسكتوب (مبرَّرة)

1. **🧾 نوع الفاتورة** — الديسكتوب يقرأ `inv.inv_type` (3 نقطة بيع / 2 مبيعات)؛ ولا
   عمودَ مماثل في السحابة، فصار الشرط `party_id IS NULL` / `IS NOT NULL`، وهو ما تقرأ
   به نقطةُ البيع في سائر تقارير السجل أصلاً.
2. **⏰ الوقت** — `BuildDateTime` يجمع التاريخ والوقت في نصٍّ ويقارن به؛ والسحابة تربط
   `date + time` محوَّلاً إلى `timestamptz`، فتُفسَّر الحدود بمنطقة الخادم. والصيغة
   المرفوضة (`99:99`) تُردّ `422` بدل خطأٍ من قاعدة البيانات.
3. **💵 نقدي و💳 شبكة** — من `invoice_payments` لا من عمودي `Inv.cash`/`Inv.visa`؛
   والتحويل البنكي (عمود `Inv.bank` عند الديسكتوب) ليس في هذين العمودين أصلاً.
4. **آجل** — `pay_type = -1` صار `payment_status = 'unpaid'`؛ وقيمتا العمود «نعم»/«—»
   مخترَعتان لأن العمود عند الديسكتوب مربّع اختيار (✔) لا نصّ.
5. **💰 إجمالي المبيعات** — يُجمع من الصفوف المعروضة لا بتمريرة SQL ثانية، حتى يطابق
   ما على الشاشة حرفياً؛ وعمود `net_signed` المخفيّ يحمل إشارة المردود بينما تبقى
   قيم الشبكة موجبة كما عند الديسكتوب.
6. **«لا توجد عمليات بالجدول»** — الديسكتوب يرفض الطباعة بصندوق رسالة؛ والسحابة تطبع
   الصفحة وفيها الجملة نفسها سطراً وحيداً (لا مقابل لصندوق الرسالة في نقطة نهاية).
7. **🖨️ إعدادات الطباعة** — لا طابعة ولا عدد نسخ (`SettingPrint`)؛ الطباعة تمرّ
   بطابعة المتصفّح. مؤجَّل إلى الجزء السابع.
8. **العملة** — `SUM(val * exchange_price)` لا مقابل له: المؤسسة في السحابة تعمل بعملة
   واحدة.
9. **رقم الصنف ورقم الحركة** — `Items.id` الرقمي صار `items.sku`، و`InvGlobalID`
   (معرّف فريد عند الديسكتوب) صار `sales_invoices.id`.
10. **ملف CSV** — الديسكتوب يكتب ترويسته يدوياً فيقول «النوع» ويُسقط «آجل»؛ والسحابة
    تُصدّر بعناوين الشبكة نفسها (فتقول «نوع الفاتورة» وتضمّ «آجل»).
11. **`#` في الصف المطبوع** — `DataSource.CurrentRowIndex + 1` عند الديسكتوب، وترقيم
    الصفوف في `reportSheet` يقابله.
12. **أثر التشغيل الحيّ** — فاتورةٌ مسدَّدة لا يُلغيها الإبطال
    (`SALES_VOID_HAS_PAYMENTS`)، فهي الأثر الوحيد الذي يتركه `verify-reports-sales`
    بعد تنظيفه؛ وما عداها (المرتجع · نقطة البيع · الأصناف · الخزينة) يُمحى.

### 4.6 الاختبارات والتحقّق الحيّ

- `apps/api/test/report-sales-movement.spec.ts` — **10** اختبارات: السجل بالأعمدة
  والفلاتر · 📊 الإجمالي الصافي وصنفٌ لم يُبع · 🧾 عرض الفواتير (النوع · الوقت ·
  نقدي · شبكة · آجل · الصافي) · 💰 إجمالي المبيعات (المبيعات − المردودات) · 🧾 نوع
  الفاتورة · ⏰ الوقت (تضييق ورفض) · 🖨️ الطباعة (الرأس · المستخدم · أعده · راجعه ·
  المدير) · «لا توجد عمليات بالجدول» · العمود المخفيّ · صلاحية `reporting.view`
  وعزل المؤسسات.
- `scripts/verify-reports-sales.mjs` — **59** نقطة تحقّق حيّة (أربع تشغلات خضراء
  متتالية)، كل رقمٍ فيها **فارقٌ عن خطّ أساس** يُؤخذ قبل الكتابة، والتنظيف في
  `finally`.

## 5. الجزء الثاني — 📦 تقارير الأصناف (`frmRptItems*`)

سبع نوافذ تُقرأ كلها من `inv_sub` المجموعة على `Items`، وأربع نقاط نهاية جديدة:

### 5.1 النوافذ وما تقرأه

| الملف (السطور) | العنوان | الشبكة | 💰 البطاقات | 🔧 خيارات البحث |
|---|---|---|---|---|
| `frmRptItemsSalesDetails.xaml` (688) + `.cs` (730) | «مبيعات الأصناف تجميعي» و«مشتريات الأصناف تجميعي» (`OperType = 2`) | م · رمز الصنف · الصنف · الكمية · صافي البيع · نسبة البيع · تفاصيل | 🔢 عدد الأصناف · 💵 إجمالي صافي البيع · 📦 إجمالي الكميات | 🏪 المستودع · 🗂️ المجموعة · 📦 الصنف · 🏢 الفرع · 📅 الفترة الزمنية (من تاريخ · وقت البدء · إلى تاريخ · وقت الانتهاء) |
| `frmRptItemsSalesDetailsPOS.xaml` (586) + `.cs` (496) | «مبيعات الأصناف تجميعي - نقطة البيع» | الشبكة نفسها + `DgvItemAdditionalTax` | البطاقات نفسها | 👤 المستخدم · 📅 الفترة الزمنية · ⚙️ الأصناف الخاضعة للضريبة الإضافية |
| `frmRptItemsProfit.xaml` (643) + `.cs` (714) | «أرباح المواد تجميعي» | م · رمز المادة · المادة · الكمية · متوسط التكلفة · صافي البيع · الربح · نسبة الربح | 💵 إجمالي صافي البيع · 💰 إجمالي الربح · 🔢 عدد الأصناف | 🏪 المستودع · 🗂️ المجموعة · 📦 الصنف · 📅 الفترة الزمنية (من · حتى) |
| `frmRptItemsProfitDetails.xaml` (768) + `.cs` (861) | «أرباح المواد تفصيلي» | م · المستودع · نوع العملية · التاريخ · الرقم · رمز المادة · المادة · الوحدة · الكمية · متوسط التكلفة · إجمالي التكلفة · السعر · المجموع · الإجمالي · الخصم · الربح · نسبة الربح % · الفاتورة | 📊 ملخص الأرباح: عدد السجلات · الكمية الإجمالية · إجمالي التكلفة · المجموع · الإجمالي · الخصم · 💹 إجمالي الربح | 🏭 المستودع · 📦 الصنف · 📅 الفترة (من تاريخ · من وقت · إلى تاريخ · إلى وقت) |
| `frmRptSalesByCategory.xaml` (677) + `.cs` (528) | «تقرير مبيعات الأصناف حسب المجموعة» | شجرة: المجموعة ← أصنافها (اسم المجموعة / الصنف · الرمز · إجمالي الكمية · الإجمالي · الضريبة · الصافي · الخصم) | إجمالي الكمية · الإجمالي · الضريبة · الصافي | 📄 نوع الفاتورة (مبيعات · نقطة بيع) · 👤 المستخدم · 📅 الفترة · 📂 المجموعة · 🏬 الفرع · 🧑‍💼 المندوب |
| `frmRptCategorySaleByDay.xaml` (444) + `.cs` (393) | «تقرير المبيعات اليومية للمجموعة» | # · الرمز · المجموعة · اليوم · التاريخ · الإجمالي | عدد السجلات · إجمالي المبيعات | 📄 نوع الفاتورة (فاتورة مبيعات · فاتورة نقطة بيع) · 🏢 الفرع · 📦 المجموعة · 📅 من · إلى |

القواعد المشتركة في الملفات:

- `ShowResults()` — `netQty = saleVal − retSaleVal + posVal − posRetVal` و
  `netValue = sumSale + sumPos − sumRetSale − sumPosRet` للمبيعات، و
  `netQty = purchVal − rePurchVal` و`netValue = sumPurch − sumRePurch` للمشتريات
  (`frmRptItemsSalesDetails.xaml.cs` L285 … L295).
- `if (!hasMovement) continue;` — **أيّ** حركة (L282 · L223 · L220) لا صافٍ غير صفر:
  صنفٌ بيع ثم أُعيد كلّه يبقى سطراً. هذا يخالف الجزء الأول الذي يسقط ما صافيه صفر
  (`if (qty == 0.0) continue;`)، فالتقريران لا يتصرفان بالطريقة نفسها عن قصد.
- `GetSaleData` في `frmRptItemsProfit.xaml.cs` يوزّع خصم رأس الفاتورة على السطور:
  `SUM(ROUND((ItemPriceWithoutVAT * minus / NULLIF(InvSum,0)),2))`، ومثله
  `((val1*exchange_price)/InvSum)*minus` في التفصيلي.
- «نسبة الربح» = (صافي البيع − التكلفة) ÷ التكلفة × 100، وصفر عند تكلفةٍ صفر.
- `frmRptCategorySaleByDay` يستدعي الإجراء المخزَّن `proGetCategorySaleByDay`
  (`@branch` · `@inv_type` · `@StartDate` · `@EndDate` · `@CategoryID`) ويسمّي اليوم بـ
  `parsedDate.ToString("ddd", culture ar)`.

### 5.2 السطح (نقاط النهاية)

| الطريقة | المسار | الإذن |
|---|---|---|
| `GET` | `/api/v1/reports/items-sales-summary` | `reporting.view` — مبيعات الأصناف تجميعي |
| `GET` | `/api/v1/reports/items-pos-sales-summary` | `reporting.view` — … نقطة البيع |
| `GET` | `/api/v1/reports/items-profit-summary` | `reporting.view` — أرباح المواد تجميعي |
| `GET` | `/api/v1/reports/items-profit-details` | `reporting.view` — أرباح المواد تفصيلي |
| `GET` | `/api/v1/reports/items-sales-by-category` | `reporting.view` — مبيعات الأصناف حسب المجموعة |
| `GET` | `/api/v1/reports/category-sales-by-day` | `reporting.view` — المبيعات اليومية للمجموعة |
| `GET` | `/api/v1/reports/items-purchases-summary` | `reporting.view` — مشتريات الأصناف تجميعي |
| `GET` | `/api/v1/reports/print/:key` | `reporting.view` — 🖨️ الطباعة |
| `POST` | `/api/v1/reports/:key/export` | `reporting.export.execute` |

الفلاتر: `from` · `to` · `fromTime` · `toTime` · `warehouseId` · `categoryId` · `itemId` ·
`branchId` · `invType` — لكل نافذةٍ منها ما تملكه عند الديسكتوب فقط (§5.1).

### 5.3 مطابقة الأعمدة والتسميات

| التقرير | العمود | حقل الديسكتوب | الحقل في السحابة |
|---|---|---|---|
| التجميعية الثلاثة | رمز الصنف · الصنف · المجموعة | `Items.code` · `Items.name` · `ItemsCategory.name` | `items.sku` · `items.name_ar` · `item_categories.name_ar` |
| | الكمية · صافي البيع / الشراء | `saleVal − retSaleVal + posVal − posRetVal` · `sumSale + sumPos − …` | `sum(signed line.quantity)` · `sum(signed line.total)` |
| أرباح المواد تجميعي | رمز المادة · المادة · الكمية | `Items.code` · `Items.name` · `salesQty` | `items.sku` · `items.name_ar` · `sum(signed quantity)` |
| | متوسط التكلفة · صافي البيع · الربح · نسبة الربح | `SUM(val1*AvrgCost)` · `salesTotal − salesDiscount − salesInvDisc` · `netSale − salesTotalCost` · `(netSale − cost)/cost*100` | `sum(signed cost_total)` · `sum(signed line.net)` · `net − cost` · نفسه |
| أرباح المواد تفصيلي | الرقم · التاريخ · نوع العملية · المستودع | `Inv.id` · `date` · `proc_type` · `Inv_sub.store` | `sales_invoices.number` · `posted_at::date` · `kind` (بيع/مرتجع) · `warehouses.name` |
| | الوحدة · السعر · المجموع · الإجمالي · الخصم · الربح | `inv_sub.unit` · `exchange_price` · `val1*exchange_price` · `netSum` · `discount + InvDiscount` · `netSum − totAvg` | `units_of_measure.name_ar` · `unit_price` · `quantity × unit_price` · `line.net` · `gross − net` · `net − cost` |
| حسب المجموعة | إجمالي الكمية · الإجمالي · الضريبة · الصافي · الخصم | `stockin − stockout` · `ItemTotal − InvoiceDiscount` · `× 0.15` · `× 1.15` · `InvDiscount` | `signed quantity` · `signed line.net` · `signed line.tax` · `signed line.total` · `gross − net` |
| اليومية للمجموعة | الرمز · المجموعة · اليوم · التاريخ · الإجمالي | `CtgyCode` · `CategoryName` · `ToString("ddd", ar)` · `InvDate` · `SaleNet` | `item_categories.code` · `name_ar` · `CASE extract(dow …)` · `posted_at::date` · `sum(signed line.total)` |

### 5.4 💰 البطاقات — تغييرٌ في المحرّك

كل نافذةٍ هنا تضع **أكثر من رقم** تحت الشبكة (إجمالي صافي البيع + إجمالي الكميات، أو
خمس بطاقات في التفصيلي). فصار `grandTotal` يقبل **بطاقةً أو قائمة بطاقات**:

- `ReportDefinition.grandTotal: ReportGrandTotal | ReportGrandTotal[]` —
  `{ key, labelAr }`، والـ`key` عمودٌ من أعمدة التقرير (قد يكون مخفياً).
- `run()` يعيد `grandTotal: Array<{ key, labelAr, amount }>` محسوباً من الصفوف
  المعروضة نفسها (لا تمريرة SQL ثانية)، فالبطاقة لا تخالف الشبكة أبداً.
- سجل التقارير يعرض `grandTotal: string[]` (التسميات، كما كانت) **و**
  `grandTotalCards: Array<{ key, labelAr }>` للشاشة.
- `reportSheet()` يرسم الشريط `.totals-strip` بصفٍّ من `<span>`: بطاقةٌ لكل رقم،
  تتلفّف عند ضيق الصفحة.

هذا تغييرٌ في شكل الاستجابة، و`apps/staff/lib/reports.ts` و
`apps/staff/app/reports/[key]/page.tsx` و`test/report-sales-movement.spec.ts` و
`scripts/verify-reports-sales.mjs` تبعته (الجزء الأول كان بطاقةً واحدة).

### 5.5 التحويلات عن الديسكتوب (مبرَّرة)

1. **✂️ خصم رأس الفاتورة** — الديسكتوب يوزّعه داخل التقرير
   (`ItemPriceWithoutVAT * minus / NULLIF(InvSum,0)`)؛ والسحابة توزّعه عند **الحفظ**
   (`calculateInvoiceTotals` في `packages/contracts/src/invoice-math.ts`، بنسبة إجمالي
   السطر من إجمالي الفاتورة) فصار «صافي البيع» يقرأ `line.net` كما هو، و«الخصم» هو
   `gross − net`. النتيجة واحدة والضريبة أصحّ (وهو ما تشترطه الزكاة والضريبة).
2. **📚 متوسط التكلفة** — عمود `AvrgCost` لكل سطر عند الديسكتوب؛ ولا مقابل له في
   `sales_invoice_lines`، فالتكلفة تُختم على السطر عند الترحيل من متوسط حركة المخزون
   (`sales.service.ts@recordAutoStock`)؛ واسم العمود بقي كما هو عند الديسكتوب لأنه
   يعبّر عن المعنى: «متوسط التكلفة» = التكلفة ÷ الكمية، و«إجمالي التكلفة» = المجموع.
3. **نسبة البيع** — عمودٌ في شبكة `frmRptItemsSalesDetails` قيمته صفر دائماً في الكود
   وغائب عن `RptItemsSalesDetails.repx`، فأُسقط (لا معنى لعمودٍ فارغ).
4. **تفاصيل** و**📄 الفاتورة** — زرّان يفتحان نافذةً أخرى؛ ولا مقابل لزرٍّ في صفّ
   تقرير، وهما مؤجَّلان إلى جزءٍ يبني مسارات التنقّل بين التقارير.
5. **`DgvItemAdditionalTax` و`chkOnlyItemAdditionalTax`** («الأصناف الخاضعة للضريبة
   الإضافية») — لا عمود `additional_tax` على سطور الفواتير في السحابة؛ مؤجَّل.
6. **👤 المستخدم و🧑‍💼 المندوب** (`frmRptItemsSalesDetailsPOS` · `frmRptSalesByCategory`)
   — مستخدمو السحابة يُقرأون من `GET /api/v1/memberships` (عرض · حالة · أدوار) لا من
   جدول `Employees`، ولا مرشِّح `kind: 'membership'` في السجل بعد؛ مؤجَّل.
7. **المجموعة ← أصنافها** — شجرة Master-Detail عند الديسكتوب، وشبكةٌ مسطّحة في
   السحابة (عمود «المجموعة» مضاف، والصفوف مرتّبة بها)، لأن لا شجرة في محرّك التقارير.
8. **الضريبة 15%** — الديسكتوب يحسبها `total * 0.15` و`total * 1.15`؛ والسحابة تقرأ
   `line.tax` و`line.total` كما رُحِّلت، فتصحّ مع نسبةٍ أخرى أو سطرٍ معفى.
9. **🔢 عدد الأصناف / عدد السجلات** — بطاقةٌ ثالثة عند الديسكتوب؛ ورأس الصفحة المطبوعة
   في السحابة يقول «عدد السجلات:» أصلاً، فالبطاقات تحمل الأرقام الماليّة وحدها.
10. **📅 نوع الفاتورة** — `inv.inv_type` (2 مبيعات / 3 نقطة بيع) صار
    `party_id IS NOT NULL` / `IS NULL` كما في الجزء الأول؛ وترتيب الخيارات وصيغها من
    كل نافذة: «مبيعات · نقطة بيع» في `frmRptSalesByCategory`،
    و«فاتورة مبيعات · فاتورة نقطة بيع» في `frmRptCategorySaleByDay`،
    و«مبيعات نقطة البيع · مبيعات عادية» في الجزء الأول. والديسكتوب يبدأ بأول خيار،
    والسحابة تبدأ بالكل حتى يختار المستخدم.
11. **⏰ الوقت** — صناديق الديسكتوب بالدقائق (`HH:mm`) وصيغتها محفوظة في التسميات؛
    والمرشِّح `kind: 'time'` نفسه يقبل الثواني (كما في الجزء الأول).
12. **الرموز التعبيرية في العناوين** (🏭 📦 💹 🗂️) أُسقطت من أسماء الأعمدة كما في
    الجزء الأول: الشبكة والملف المصدَّر يستعملان العربية وحدها.
13. **`if (!hasMovement)` في المشتريات** — الديسكتوب يشترك في الحلقة نفسها فيختبر
    أعمدة **البيع** حتى في وضع `OperType = 2` (أي لا يستبعد شيئاً)؛ والسحابة تختبر
    حركة الشراء نفسها (`HAVING sum(line.quantity) <> 0`).

### 5.6 الاختبارات والتحقّق الحيّ

- `apps/api/test/report-items-summary.spec.ts` — **14** اختباراً: السجل (سبعة تقارير
  بأعمدتها وفلاترها وبطاقاتها) · 📦 التجميعي صافياً من المردود و«حبل» الذي لم يتحرك ·
  🏪🏗️📦 الفلاتر الثلاثة · 🧾 نقطة البيع وحدها · 💰 الأرباح (التكلفة · الربح · النسبة ·
  خصم الرأس) · 📥 المشتريات · 💰 التفصيلي (ستة أسطر · التكلفة · السعر · الخصم · الربح) ·
  🗂️ حسب المجموعة (المجاميع الأربعة · نوع الفاتورة) · 📅 اليومية للمجموعة (اسم اليوم ·
  الفلاتر) · 📅 فترةٌ فارغة · 🖨️ الطباعة · 📊 التصدير · `reporting.view` · عزل المؤسسات.
- `scripts/verify-reports-items.mjs` — **126** نقطة تحقّق حيّة (أربع تشغلات خضراء
  متتالية): كل رقمٍ **فارقٌ عن خطّ أساس**، وكل ما كُتب أُلغي في `finally` (البيع ·
  المرتجع · نقطة البيع · بيع المستودع الثاني · فاتورة خصم الرأس · الشراء · مردود
  الشراء · الأصناف · المستودع · الوحدة · المجموعتان)، فلا أثر للتشغيل بعده.

## 6. الجزء الثالث — 🧾 تقارير الفواتير والإشعارات والحركة اليومية (`frmRptInv*`)

ثماني نوافذ (سابعتها مؤجَّلة) تقرأ رأس الفاتورة `inv` لا سطورها، وسبعة تقارير جديدة في
السجل (صار **81** تعريفاً). ثلاث نوافذ منها تتقاسم **صفّاً واحداً** لأنها تقرأ السجل نفسه
بشروطٍ مختلفة: `inv_type` 2/3/20 في نافذة الفواتير، و21/22 في نافذة الإشعارات.

### 6.1 النوافذ وما تقرأه

| الملف (السطور) | العنوان | التقرير المطبوع | الشبكة | 💰 البطاقات | 🔧 خيارات البحث |
|---|---|---|---|---|---|
| `frmRptInvSalesDetails.xaml` (1143) + `.cs` (1513) | «تقرير فواتير المبيعات» | `Reports/rptInvSumByClient.repx` | **28** عموداً: م · `InvGlobalID` · `ProcType` · `InvoiceType` · `PayType` · نوع الفاتورة · رقم الفاتورة · رقم المرجع · 📅 تاريخ الفاتورة · ⏰ الوقت · نوع الدفع · 💰 المدفوع · 👥 العميل · 💵 نقدي · 💳 شبكة · المجموع · الخصم · الإجمالي · الضريبة · ضريبة إضافية · إجمالي الضريبة · 💰 الصافي · 🏭 المستودع · 🏬 الفرع · المندوب · 👤 المستخدم · 📄 تفاصيل · 👁️ عرض | 📊 ملخص النتائج — عدد الفواتير · 💵 المجموع · 🏷️ الخصم · 📦 الإجمالي · 🧾 الضريبة · ➕ ضريبة إضافية · 🧾 إجمالي الضريبة · 💰 الصافي · 💵 نقدي · 💳 شبكة · 💰 المدفوع | 📄 نوع الفاتورة · 💳 نوع الدفع · 🏭 المستودع · 🧑‍💼 المندوب · 👥 العميل · 🏬 الفرع · 📅 الفترة (من تاريخ · من وقت · إلى تاريخ · إلى وقت) · 🧾 الضريبة · 💵 حالة الدفع · 🔄 نوع العملية |
| `frmRptInvSalesDetailsPos.xaml` (942) + `.cs` (895) | «تقرير مبيعات الفواتير» (`inv.inv_type = 3`) | `Reports/rptInvSumtPos.repx` | **27** عموداً بالشبكة نفسها بلا «💰 المدفوع» | البطاقات نفسها بلا «المدفوع» | 🔄 نوع العملية · 💵 حالة الدفع · 🏭 المستودع · 👤 المستخدم · 📅 الفترة |
| `frmRptInvSalesDetailsPosAndroid.xaml` (1068) + `.cs` (985) | «تقرير مبيعات أندرويد» (`inv_type = 20`) | `Reports/rptInvSumtPos.repx` | الشبكة نفسها | البطاقات نفسها | الفترة وحدها |
| `frmRptInvNotfic.xaml` (701) + `.cs` (812) | «تقرير الإشعارات» (`inv_type` 21 مدين · 22 دائن) | `Reports/rptInvSumByClient.repx` | **25** عموداً: «نوع الإشعار» و«رقم الإشعار» و«تاريخ الإشعار» مكان أخواتها، وبلا «نوع الدفع» ولا «المدفوع» | البطاقات نفسها | 📄 نوع الإشعار · 🔄 نوع العملية · 👤 المستخدم · 🧑‍💼 المندوب · 🏢 العميل · 📅 من / إلى |
| `frmRptInvPurchaseDetails.xaml` (921) + `.cs` (1170) | «تفاصيل فواتير المشتريات» | `Reports/rptInvSumBySupplier.repx` | **31** عموداً (16 ظاهراً): # · 📄 نوع الفاتورة · 🔢 رقم الفاتورة · 🔗 رقم المرجع · 📅 التاريخ · 💳 نوع الدفع · 🏢 المورد · 💰 المجموع · 🏷️ الخصم · 💵 الإجمالي · 🧾 الضريبة · ✅ الصافي · 🏪 المستودع · 🏬 الفرع · 👤 المندوب · 👤 المستخدم، ومخفيّة: النقدية · 💰 المدفوع · 💰 المتبقي · الوقت | المجموع · الخصم · الإجمالي · الضريبة · الصافي | 🧾 الضريبة · 🔄 نوع العملية · 🏬 الفرع · 🏢 المورد · 🏪 المستودع · 📅 الفترة (من تاريخ · وقت البدء · إلى تاريخ · وقت النهاية) |
| `frmRptDailySales.xaml` (404) + `.cs` (377) | «تقرير مبيعات حسب اليوم» | `Reports/daysalesPos.repx` + `header/footer.repx` | الرقم · التاريخ · اليوم · الإجمالي قبل الضريبة · الضريبة · الإجمالي | إجمالي قبل الضريبة · إجمالي الضريبة · الإجمالي الكلي | 📄 نوع الفاتورة · 🏢 الفرع · 📅 من / إلى |
| `frmRptDailyProcess.xaml` (418) + `.cs` (545) | «تقرير الحركة اليومية» | `Reports/rptDailyProcess.repx` + `header/footer.repx` | (بلا عنوان) · عدد الفواتير · الإجمالي · نقدي · آجل | — | 🏢 الفرع · 📅 من / إلى + وقت |
| `frmRptInvAnalysis.xaml` (811) + `.cs` (2146) | «تقرير تحليل المبيعات» | `header/footer.repx` (الشبكة تُطبع من النافذة) | المستودع · فواتير · صافي الكمية · صافي الإجمالي · صافي الخصم · صافي التكلفة · صافي الربح · نسبة الربح للتكلفة · نسبة الاجمالي لإجمالي البيع · نسبة الربح لإجمالي الربح | صافي الكمية · صافي الإجمالي · صافي الخصم · صافي التكلفة · صافي الربح | 📋 نوع التقرير (8 أزرار راديو) · 👤 المستخدم · 🧑‍💼 العميل · 📦 مجموعة الصنف · 🤝 مندوب البيع · 🏭 المستودع · 📅 الفترة |
| `FrmRptSalesChart.xaml` (605) + `.cs` (492) | «تقرير بياني للمبيعات» | — (أربع رسوم LiveCharts) | — | — | — |

### 6.2 السطح (نقاط النهاية)

| المفتاح | النافذة | المجموعة | المسار في `apps/staff` |
|---|---|---|---|
| `sales-invoices-details` | `frmRptInvSalesDetails` | `sales` | `/reports/sales-invoices-details` |
| `pos-sales-invoices-details` | `frmRptInvSalesDetailsPos` | `sales` | `/reports/pos-sales-invoices-details` |
| `sales-notifications` | `frmRptInvNotfic` | `sales` | `/reports/sales-notifications` |
| `purchase-invoices-details` | `frmRptInvPurchaseDetails` | `purchases` | `/reports/purchase-invoices-details` |
| `daily-sales` | `frmRptDailySales` | `sales` | `/reports/daily-sales` |
| `daily-process` | `frmRptDailyProcess` | `sales` | `/reports/daily-process` |
| `sales-inv-analysis` | `frmRptInvAnalysis` | `sales` | `/reports/sales-inv-analysis` |

كل مفتاحٍ يعمل على المسارات العامة نفسها: `GET /api/v1/reports/:key`،
`GET /api/v1/reports/print/:key`، `POST /api/v1/reports/:key/export`. والاسم
`sales-inv-analysis` لا `sales-analysis` لأن السحابة كانت تملك `sales-analysis`
(«أفضل الأصناف مبيعاً») من مرحلةٍ سابقة.

### 6.3 مطابقة الأعمدة والتسميات

| التقرير | أعمدة الديسكتوب | المنقول | النسبة | ما أُسقط أو أُضيف |
|---|---|---|---|---|
| 🧾 فواتير المبيعات | 28 | **21** | **100٪** من 21 عموداً قابلاً للنقل | أُسقطت: «م» و4 أعمدة تقنية (`InvGlobalID` · `ProcType` · `InvoiceType` · `PayType`) وزرّا «📄 تفاصيل» و«👁️ عرض» |
| 🧾 مبيعات الفواتير | 27 | **21** | **100٪** من 20 | عمود «💰 المدفوع» مضاف من النافذة الأخت (الصفّ واحد) |
| 📄 الإشعارات | 25 | **21** | **100٪** من 19 | عمودا «نوع الدفع» و«المدفوع» مضافان من النافذة الأخت |
| 📥 فواتير المشتريات | 31 (16 ظاهراً) | **17** | **94٪** من 17 عمود بيانات | أُسقط «#» و6 أعمدة تقنية و«النقدية»، و«👤 المندوب» مؤجَّل (لا `salesman_id` على `purchase_invoices`) |
| 📅 مبيعات حسب اليوم | 6 | **6** | **100٪** | — |
| 🔄 الحركة اليومية | 5 | **5** | **100٪** | العمود الأول بلا عنوان عند الديسكتوب فسُمّي «نوع العملية» من `DoProcess` |
| 📊 تحليل المبيعات | 10 | **10** | **90٪** | العمود الأول «المستودع» صار «البعد» (§6.5/7) |

### 6.4 القواعد المنقولة كما هي

1. **`UpdateSummaryCards()` — `Calc(x) = purchases.Sum(x) − returns.Sum(x)`**
   (`frmRptInvSalesDetails.xaml.cs` L680 … L712). المرتجع يخصم من البطاقات العشر كلها؛
   ولأن السجل يقرأ الأعمدة نفسها في «الإشعارات»، فالإشعار الدائن يخصم كذلك
   (`credit_note` إشارة −1 و`debit_note` إشارة +1).
2. **`GetPaymentText`** (L650) — «آجل · نقدي · شبكة · متعدد · ضيافة». فاتورةٌ لم يُسدَّد
   منها شيء «آجل»، ومسدَّدةٌ بطريقٍ واحد تُسمّى به، وبأكثر من طريق «متعدد»؛ «ضيافة» لا
   مقابل لها في السحابة.
3. **«المجموع» و«الخصم»** — «المجموع» هو Σ(quantity × unit_price) و«الخصم» ما نقص منه
   حتى «الإجمالي»؛ «إجمالي الضريبة» = الضريبة + ضريبة إضافية.
4. **`dbo.SalesByDay(@branch, @InvType, @StartDate, @EndDate)`** (`.cs` L122) —
   `invType = cmbInvType.SelectedIndex == 0 ? 2 : 3` (L119)، واسم اليوم
   `ToString("ddd", culture ar)` → الأحد … السبت، ورسالة الفراغ «لا توجد بيانات، أدخل
   الفترة الزمنية الصحيحة».
5. **`DoProcess(name, type1, type2)`** (`frmRptDailyProcess.xaml.cs` L227 … L232) —
   مبيعات (2, 1) · مرتجع مبيعات (2, 2) · نقطة البيع (3, 1) · مرتجع نقطة البيع (3, 2) ·
   مشتريات (1, 1) · مرتجع مشتريات (1, 2)، و«نقدي» ما كان `pay_type > 0` و«آجل» ما كان
   `pay_type = -1`.
6. **`frmRptInvAnalysis`** — الصافي = بيع − مرتجع في كل بعد، و«نسبة الربح للتكلفة»
   = (الإجمالي − التكلفة) ÷ التكلفة × 100، والنسبتان الأخريان حصّة الصفّ من إجمالي
   التقرير نفسه.

### 6.5 التحويلات عن الديسكتوب (مبرَّرة)

1. **🔗 رقم المرجع** — `inv.Reff_No` عند الديسكتوب صندوقٌ نصّي حرّ، ولا مقابل له في
   `sales_invoices`؛ والسحابة تحفظ **الرابط** نفسه (`reference_invoice_id`)، فيُطبع رقم
   الفاتورة المُشار إليها (رجيعٌ أو إشعار). وفي المشتريات `supplier_reference_no` هو
   المقابل الحقيقي، ويليه رقم فاتورة الشراء المُشار إليها.
2. **📊 ملخص النتائج — «عدد الفواتير»** — بطاقةٌ حادية عشرة عند الديسكتوب؛ ورأس
   الصفحة المطبوعة في السحابة يقول «عدد السجلات:» أصلاً (كما في الجزء الثاني)، فالبطاقات
   تحمل الأرقام الماليّة وحدها.
3. **💳 نوع الدفع في المشتريات** — `inv.pay_type` عَلَمٌ واحد على الفاتورة؛ والسحابة
   تسدّد الشراء بـ**سند صرف** عبر `payment_allocations`، فتُجمع أرجل السند (نقد · شبكة ·
   بنك) بالمنطق نفسه.
4. **👤 المستخدم** — `Employees` عند الديسكتوب و`users` في السحابة؛ والاسم يُقرأ من
   `created_by` (كما في كل تقريرٍ سابق).
5. **🧑‍💼 المندوب في المشتريات** — شبكة الديسكتوب تحمله، ولا عمود `salesman_id` على
   `purchase_invoices`؛ مؤجَّل إلى أن يُنقل الحقل.
6. **📄 نوع الفاتورة (مبيعات / نقطة بيع / أندرويد)** — `inv_type` 2 · 3 · 20؛ والسحابة
   تميّز نقطة البيع بـ`party_id IS NULL` (كما في الجزأين الأول والثاني)، و«أندرويد» لا
   مقابل له فلم يُبنَ له تقريرٌ ثالث: نافذة `frmRptInvSalesDetailsPosAndroid` تقرأ
   `rptInvSumtPos.repx` نفسه وتختلف في شرطٍ واحد.
7. **«البعد» مكان «المستودع»** — شبكة `frmRptInvAnalysis` تسمّي عمودها الأول «المستودع»
   ويبقى الاسم ثابتاً ولو تغيّر البعد بالراديو؛ والسحابة تطبع اسم البعد المختار في كل
   صفّ، فصار العنوان «البعد».
8. **🔄 الحركة اليومية — `DoProcess2(5 … 9)`** تقرأ جدول `receipts` («سندات القبض») via
   `Common.ResrirectionType`؛ ولا جدول مقابل في السحابة، فالستة أنواع الماليّة وحدها
   منقولة (§8).
9. **⏰ الوقت** — `HH:mm` في الصناديق و`HH:mm:ss` في الشبكة كما عند الديسكتوب
   (`InvoiceTime`)؛ والمرشِّح `kind: 'time'` يقبل الاثنين.
10. **📅 «من وقت / إلى وقت»** يقع على **التاريخ والوقت معاً** لا على الساعة وحدها: فترةٌ
    من ثلاثة أيام مع «من وقت 23:59» لا تُسقط فواتير اليوم التالي (وهو سلوك
    `inv.Inv_Date` عند الديسكتوب كذلك).
11. **الرموز التعبيرية في العناوين** (📅 ⏰ 💰 👥 💵 💳 🏭 🏬 👤 ➕ 🧾) أُسقطت من أسماء
    الأعمدة كما في الجزأين السابقين.
12. **📄 تفاصيل و👁️ عرض** — زرّان يفتحان نافذةً أخرى؛ مؤجَّلان مع زرّي الجزء الثاني.

### 6.6 الاختبارات والتحقّق الحيّ

- `apps/api/test/report-invoices.spec.ts` — **15** اختباراً: السجل (سبعة تقارير بأعمدتها
  وفلاترها وبطاقاتها) · 🧾 فواتير المبيعات (النوع · طريقة الدفع · نقدي · شبكة · المدفوع ·
  رقم المرجع · المندوب · العشر بطاقات) · 🔍 خيارات البحث الاثنا عشر · ⏰ الوقت · 🧾 نقطة
  البيع · 📄 الإشعارات (الدائن يخصم) · 📥 المشتريات (المدفوع · المتبقي · رقم المرجع) ·
  📅 حسب اليوم (اسم اليوم) · 🔄 الحركة اليومية (ست حركات) · 📊 التحليل (المخزن · الصنف ·
  المندوب · اليوم · المجموعة) · 📆 فترةٌ فارغة · 🖨️ الطباعة · 📊 التصدير ·
  `reporting.view` · عزل المؤسسات.
- `scripts/verify-reports-invoices.mjs` — **159** نقطة تحقّق حيّة: كل رقمٍ **فارقٌ عن خطّ
  أساس** يُؤخذ قبل الكتابة، وكل ما كُتب أُلغي في `finally` (المرتجع · نقطة البيع ·
  الإشعار الدائن · الشراء · مردود الشراء · الأصناف · المستودع · الوحدة · المجموعتان ·
  المندوب · الخزينة)، إلا **الفاتورة المسدَّدة**: قاعدة الدفاتر تمنع إلغاءها
  (`SALES_VOID_HAS_PAYMENTS`) — وهو بندٌ مفحوص لا مُهمَل، وهو عينُ ما يفعله
  `scripts/verify-reports-sales.mjs` في الجزء الأول.

## 7. الجزء الرابع — 📚 تقارير المخزون والأرقام التسلسلية (`frmRptInventory` · `frmRptItems*` · `frmRptSerial*` · `frmRptProducedItems`)

### 7.1 النوافذ وما تقرأه

| النافذة | العنوان | الملف | الأسطر | التقرير في السجل |
|---|---|---|---|---|
| `frmRptInventory.xaml` | «📋 الفواتير» (مستندات المخزون) | `Form_WPF/frmRptInventory.xaml.cs` | 769 | `inventory-documents` |
| `frmRptItemsActivity.xaml` | «مادة باجمالي الحركات» | `Form_WPF/frmRptItemsActivity.xaml.cs` | 964 | `item-movement-totals` |
| `frmRptItemsActivityDetailed.xaml` | «حركة صنف تفصيلي» | `Form_WPF/frmRptItemsActivityDetailed.xaml.cs` | 1195 | `item-movement-details` |
| `frmRptItemsExpiration.xaml` | «صلاحية المواد» | `Form_WPF/frmRptItemsExpiration.xaml.cs` | 448 | `item-expiry` |
| `frmRptSerialNo.xaml` | «حركة الأرقام التسلسلية» | `Form_WPF/frmRptSerialNo.xaml.cs` | 491 | `serial-movements` |
| `frmRptSerialNoSummary.xaml` | «أرصدة الأرقام التسلسلية» | `Form_WPF/frmRptSerialNoSummary.xaml.cs` | 324 | `serial-balances` |
| `frmRptProducedItems.xaml` | «تقرير مواد المنتجة» | `Form_WPF/frmRptProducedItems.xaml.cs` | 376 | `produced-items` + `produced-components` |

- `frmRptInventory` يقرأ `Inv` بثمانية أنواع عملية (L260-267): «مناقلة مرسلة» 8/2 ·
  «مناقلة مستلمة» 8/1 · «بضاعة أول مدة» 9/1 · «أمر توريد» 4/1 · «أمر صرف» 5/1 ·
  «أمر إنتاج» 6/1 · «طلب بضاعة» 14/1 · «تسوية جردية» 7/1، ويطبعها في
  `Reports/rptInventoryReport.repx`. **عنوان النافذة في الديسكتوب نفسه يقول «تفاصيل
  فواتير المشتريات»** (نسخٌ ولصقٌ من نافذة المشتريات) — والعنوان المعتمد هنا هو عنوان
  شبكتها «📋 الفواتير» مع اسم تقريرها `rptInventoryReport`.
- `frmRptItemsActivity` يستدعي `GetSumVal(itemId, inv_type, proc_type, …)` ثلاث عشرة مرة
  (L232-244) ويحسب الرصيد يدويّاً (L248)، ومتوسط التكلفة من `CalcAvgCost` (L363-415).
- `frmRptItemsActivityDetailed` يقرأ `Inv, Inv_Sub` مرتّباً بالتاريخ ويُراكم الرصيد سطراً
  سطراً (L355-365)، واسم النوع من جدول `InvTypes` (L334-345) مع إسقاط `IsInput = 0`.
- `frmRptItemsExpiration` يستدعي `Inventory.ItemsExpirationStock(cond)`
  (`Class/Inventory.cs:352`) وهي `dbo.ItemsExpirationStock()` في `AlterDb.txt:2157`.
- `frmRptSerialNoSummary` يستدعي `dbo.funCalculateSerialNoSummary` (`AlterDb.txt:3615`)
  من جدول `InvoiceItemDetail` بشرطي `InvertoryImpact` 1 و2.
- `frmRptProducedItems` شبكتان في نافذة واحدة: «🔧 المكونات» و«🏭 المواد المنتجة».

### 7.2 السطح (نقاط النهاية)

| التقرير | نقطة النهاية | الفلاتر |
|---|---|---|
| مستندات المخزون | `GET /api/v1/reports/inventory-documents` | 📄 نوع العملية (ثمانية) · 🏭 المستودع · 🏢 الفرع · 📅 من/إلى + ⏰ وقت |
| مادة باجمالي الحركات | `GET /api/v1/reports/item-movement-totals` | 🏢 الفرع · 📦 الصنف · 📅 من/إلى · 🏪 المستودع · 👥 العميل/المورد · 🗂️ المجموعة |
| حركة صنف تفصيلي | `GET /api/v1/reports/item-movement-details` | 🏬 الفرع · 🏭 المستودع · 📦 الصنف · 📅 من/إلى · 👥 عميل/مورد · 🔄 نوع العملية (أحد عشر) · 📋 أنماط الفواتير |
| صلاحية المواد | `GET /api/v1/reports/item-expiry` | 🏪 المستودع · 🗂️ المجموعة · 📦 الصنف · 🏢 الفرع |
| حركة الأرقام التسلسلية | `GET /api/v1/reports/serial-movements` | 🏬 الفرع · 📦 الصنف · 🔢 الرقم التسلسلي · 📅 من/إلى |
| أرصدة الأرقام التسلسلية | `GET /api/v1/reports/serial-balances` | 🏬 الفرع · 📦 الصنف · 🔢 الرقم التسلسلي |
| تقرير مواد المنتجة | `GET /api/v1/reports/produced-items` | 📅 من/إلى |
| مكونات المواد المنتجة | `GET /api/v1/reports/produced-components` | 📅 من/إلى |

كلها بـ `reporting.view`، والطباعة `/api/v1/reports/print/:key`، والتصدير
`POST /api/v1/reports/:key/export` (CSV · XLSX · PDF).

### 7.3 مطابقة الأعمدة والتسميات

| النافذة | الأعمدة المنقولة | التسميات |
|---|---|---|
| `frmRptInventory` | 10 من 12 (المستخدم · الخصم · الضريبة · الصافي لا مقابل لها — §7.5) | 100٪ من القابلة للنقل |
| `frmRptItemsActivity` | 18 من 18 | 100٪ |
| `frmRptItemsActivityDetailed` | 18 من 19 (زر «📄 الفاتورة» شبكةٌ فرعية) | 100٪ |
| `frmRptItemsExpiration` | 9 من 9 | 100٪ |
| `frmRptSerialNo` | 9 من 12 (ثلاثة أعمدة معرّفات + زر «👁️ عرض») | 100٪ |
| `frmRptSerialNoSummary` | 5 من 5 | 100٪ |
| `frmRptProducedItems` | 10 من 6 (الشبكة + رقم الأمر + المرجع + الحالة) | 100٪ |

### 7.4 القواعد المنقولة كما هي

1. **صيغة الرصيد** (`frmRptItemsActivity.xaml.cs` L248) — الرصيد = أول مدة + مشتريات
   − مرتجع مشتريات − مبيعات + مرتجع مبيعات − نقطة البيع + مرتجع POS + مناقلة − مناقلة
   + فاتورة إدخال − فاتورة إخراج + تسوية إدخال − تسوية إخراج. كل عمودٍ «حجم حركة»
   والإشارة تأتي من اسم العمود لا من قيمته، وعلى ذلك جاء التحقّق: 100 + 20 − 5 − 10 + 2
   − 1 − 25 + 3 − 2 + 7 − 4 − 8 = **77**.
2. **إجمالي التكلفة = الرصيد × متوسط التكلفة** (L271) ومتوسط التكلفة من
   `CalcAvgCost` (L363-415) — والسحابة تحفظه على صف الرصيد (`stock_balances`).
3. **الرصيد المتحرك** في «حركة صنف تفصيلي» (L355-365) يُراكم داخل الفترة فقط.
4. **إسقاط `IsInput = 0`** (L344) — لا يظهر في التقرير إلا ما حرّك المخزون.
5. **`dbo.ItemsExpirationStock`** — الأصناف المراقَبة بصلاحية وحدها (`ItemProperty = 8` →
   `track_lot`)، و«باقي سنوات/أشهر/أيام» بـ `DATEDIFF` نفسه.
6. **`funCalculateSerialNoSummary`** — العدد رصيدٌ لا عددُ صفوف: ما خرج من الرفّ ليس رصيداً
   (مُختبَر: استهلاك رقمٍ يُخرجه ثم إرجاعه يُعيده).

### 7.5 التحويلات عن الديسكتوب (مبرَّرة)

1. **المعرّفات والأزرار** (`InvGlobalID` · `ProcType` · `InvTypeNo` · «👁️ عرض») — لا تُعرض؛
   وهي محفوظةٌ أعمدةً مخفيّة (`doc_id` · `doc_type`) ليُبنى عليها رابط المستند لاحقاً.
2. **👤 المستخدم** في `frmRptInventory` — ليس على كل رؤوس مستندات المخزون عمود `created_by`
   موحّد؛ مؤجَّل (§9).
3. **المجموع · الخصم · الإجمالي · الضريبة · الصافي** في `frmRptInventory` — مستندات
   المخزون في السحابة تحمل **تكلفة** و**كمية** لا سعراً وضريبة، فانضغطت البطاقات الخمس في
   «إجمالي الكمية · إجمالي التكلفة · عدد المستندات».
4. **أمر توريد وطلب بضاعة فارغان** — السحابة تحفظهما وثيقتين (`stock_deliveries` ·
   `goods_requests`) لا تُحرّكان رصيد المخزون، فلا سطر لهما في دفتر الحركات؛ الخياران
   باقيان في القائمة كما في الديسكتوب، والفرق مُختبَر ومُوثَّق.
5. **«نوع الفاتورة» من جدول `InvTypes`** — لا جدول مقابل في السحابة، فاشتُقّ الاسم من
   `doc_type` بأسماء النوافذ نفسها: «فاتورة إدخال/إخراج» و«تسوية إدخال/إخراج» و«نقطة
   البيع» (بلا عميل) و«إذن مخزني» للمستند الذي يحمل الاتجاهين.
6. **الصلاحية بالدُفعات** — الديسكتوب يجمّع حركات الصنف على تاريخ انتهاء، والسحابة تحفظ
   الصلاحية على الدفعة (`item_lots.expiry_date`)؛ ورُصيد الدفعة من دفتر الحركات، مع
   `HAVING … <> 0` لأن دفعةً استُنفدت لا صلاحية لها.
7. **أعمدة الاتجاه** — `proc_type` الديسكتوب صار `direction` (`in`/`out`)، وعمود
   «الإتجاه» في حركة الأرقام التسلسلية يقرأه نصّاً («داخل»/«خارج»).
8. **«🔄 نوع العملية» على فلتر `kind`** — لأن `procType` محجوزٌ لتقارير الفواتير
   («مبيعات»/«مرتجع»)، و«أنماط الفواتير» على فلتر `status`.
9. **🔢 الرقم التسلسلي مدخلٌ نصّي** — نوعٌ جديد `kind: 'serial'` في المحرّك والواجهة،
   لأن صندوق الديسكتوب حرّ (`@ItemSerialNo`) لا قائمة منسدلة.
10. **🔧 المكونات تقريرٌ ثانٍ** — شبكتان في نافذة واحدة عند الديسكتوب، وتقريران في
    السجل هنا، كلٌّ بنقطة نهايته وفلاتره.

### 7.6 الاختبارات والتحقّق الحيّ

- `apps/api/test/report-inventory.spec.ts` — **10** اختبارات: السجل (ثمانية تقارير
  بأعمدتها) · مستندات المخزون (سطرٌ لكل مستند وثمانية أنواع) · مادة باجمالي الحركات
  (ثلاثة عشر عموداً وصيغة الرصيد ومتوسط التكلفة) · حركة صنف تفصيلي (رصيدٌ متحرك · نوع
  الفاتورة · أنماط الفواتير) · صلاحية المواد (باقي سنوات/أشهر/أيام) · حركة الأرقام
  التسلسلية · أرصدة الأرقام التسلسلية (استهلاكٌ وإرجاع) · مواد مُنتَجة ومكوّناتها ·
  فترةٌ فارغة · عزل المؤسسات و`reporting.view`.
- `scripts/verify-reports-inventory.mjs` — **150** نقطة تحقّق حيّة: أحدَ عشرَ مستنداً
  ودُفعات وثلاثة أرقام تسلسلية وأمر إنتاج، وكل رقمٍ مستأجَرٍ واسع **فارقٌ عن خطّ أساس**
  يُؤخذ قبل الكتابة، وكل ما يُلغى أُلغي في `finally` (السندات · الفواتير · الدفعات ·
  الأرقام التسلسلية)، وما لا يُلغى **مفحوصٌ أنه يُرفض**: أمرُ إنتاجٍ مكتمل
  (`PRODUCTION_ORDER_INVALID_STATUS`) ومناقلةٌ مستلمة (`TRANSFER_INVALID_STATE`).

## 8. الجزء الخامس — 📒 تقارير المحاسبة (`frmRptBalances` · `frmRptEntries` · `frmRptIncomeStatement` · `frmRptCostCenter` · `frmTaxRptPeriod`)

### 8.1 النوافذ وما تقرأه

| النافذة | العنوان | الملف | الأسطر | التقرير في السجل |
|---|---|---|---|---|
| `frmRptBalances.xaml` | «أرصدة الحسابات» (📊 أرصدة الحسابات خلال فترة) | `Form_WPF/frmRptBalances.xaml.cs` | 662 | `account-balances` |
| `frmRptEntries.xaml` | «القيود اليومية» (📋 بيانات القيد · 🔍 البحث) | `Form_WPF/frmRptEntries.xaml.cs` | 788 | `journal-entries` |
| `frmRptEntries.xaml` | «🧾 تفاصيل القيد» | `Form_WPF/frmRptEntries.xaml.cs` | 788 | `journal-entry-lines` |
| `frmRptIncomeStatement.xaml` | «أرباح وخسائر حسابات رئيسية» | `Form_WPF/frmRptIncomeStatement.xaml.cs` | 637 | `income-statement-accounts` |
| `frmRptCostCenter.xaml` | «تقرير مراكز التكلفة» | `Form_WPF/frmRptCostCenter.xaml.cs` | 842 | `cost-center-statement` |
| `frmTaxRptPeriod.xaml` | «إقرار ضريبي» | `Form_WPF/frmTaxRptPeriod.xaml.cs` | 952 | `vat-return-period` |

التقارير المطبوعة: `Reports/rptAccountBalance.repx` · `Reports/Entry.repx` ·
`Reports/RptIncomeStatement.repx` (+ `RptIncomeStatementNew.repx` باسمٍ يبدأ بـU+200F) ·
`Reports/RptCostCenter.repx` و`RptCostCenterDetails.repx` و`RptCostCenterStatement.repx` ·
`Reports/TaxRptPeriod.repx` و`TaxRptPeriodNew.repx` (وهذان **XML صريح** لا حزمة zip،
خلافاً لسائر ملفات `Reports`)، وإعدادات الطباعة `SettingPrint` بأرقام `Inv_Id` 12 · 9 · 14.

### 8.2 السطح (نقاط النهاية)

| التقرير | نقطة النهاية | الفلاتر |
|---|---|---|
| أرصدة الحسابات | `GET /api/v1/reports/account-balances` | 📊 الحساب الرئيسي · 🏢 الفرع · 🧑‍💼 المندوب · 📅 من/إلى + ⏰ من وقت/إلى وقت |
| القيود اليومية | `GET /api/v1/reports/journal-entries` | 📅 من/إلى · 🏢 الفرع · 🧾 نوع القيد (ستة عشر) · 📋 حالة القيد · 🔢 رقم القيد · 📄 رقم المستند |
| تفاصيل القيد | `GET /api/v1/reports/journal-entry-lines` | 📅 من/إلى · 🏢 الفرع · 🧾 نوع القيد · 📂 مركز التكلفة · 📒 الحساب |
| أرباح وخسائر حسابات رئيسية | `GET /api/v1/reports/income-statement-accounts` | 📅 من/إلى · 🏢 الفرع |
| تقرير مراكز التكلفة | `GET /api/v1/reports/cost-center-statement` | 📂 مركز التكلفة · 📒 الحساب · 📋 نوع التقرير (تجميعي · تفصيلي) · 🏢 الفرع · 📅 من/إلى + ⏰ وقت البداية/النهاية |
| الإقرار الضريبي | `GET /api/v1/reports/vat-return-period` | 📅 من تاريخ/إلى تاريخ · 🏢 الفرع · 📆 ربع سنة · 📆 شهري |

### 8.3 مطابقة الأعمدة والتسميات

| النافذة | الأعمدة المنقولة | التسميات |
|---|---|---|
| `frmRptBalances` | 10 من 11 (زر «تفاصيل» شبكةٌ فرعية) | 100٪ من القابلة للنقل |
| `frmRptEntries` «🔍 البحث» | 7 من 7 | 100٪ |
| `frmRptEntries` «🧾 تفاصيل القيد» | 7 من 7 | 100٪ |
| `frmRptIncomeStatement` | 4 من 4 + سطر المخزون | 100٪ |
| `frmRptCostCenter` | 14 من 15 (زر «تفاصيل») | 100٪ |
| `frmTaxRptPeriod` | 13 بنداً من 13 (+ عمود «القسم» — §8.5/6) | 100٪ |

### 8.4 القواعد المنقولة كما هي

1. **`Entry.type = 0` قيدٌ إفتتاحي (L285) وما عداه حركة (L270)** — كل نافذةٍ في الجزء
   تقسم أرقامها على هذا الخط، فصار «رصيد افتتاحي» استعلاماً فرعياً بـ
   `source_type = 'opening'` و«حركة» استعلاماً بـ`IS DISTINCT FROM` في التقرير نفسه.
2. **صيغة الرصيد من وجهين (L373-L430)** — `حركة مدين = max(مدين − دائن، 0)` و
   `حركة دائن = max(دائن − مدين، 0)`، ثم `ختامي = افتتاحي + حركة`، ثم **تصفيةٌ ثانية**
   تُبقي وجهاً واحداً غير صفر: `ختامي مدين = max(مدين − دائن، 0)`.
3. **`GetParent` (L206-L233)** — الحساب يظهر إذا كان «الحساب الرئيسي» أحد آبائه، والسير
   يبدأ من **أب الصف** لا منها؛ أي أنّ المختار نفسه لا يظهر. `accounts.path` شجرة ltree
   من الجذر، فصار الشرط `acc.path <@ <المختار> AND acc.id <> <المختار>`.
4. **`Accounts_Index.FinalAcc = 2` (L229)** — المصروفات والإيرادات وحدهما
   (`CrystalLiteDB.txt` L2515-L2516؛ الأصول والخصوم بقيمة 1)، والتجميع على الأب
   (`_Type == 1`, L248-L298) هو `coalesce(parent.id, acc.id)` مع دمج المكرَّر.
5. **الإيرادات الأخرى من `RentInvoice` (`tot_net` و`tax`, L304-L343)** —
   `proc_type` 1 و3 زائداً و2 ناقصاً، ويقابلها في السحابة `rental_invoices.net_amount`
   و`tax_amount` بحالة `posted`.
6. **`CalcInvoicePart` (L345-L427)** — السطر «خاضع» إذا `taxval <> 0` و«معفى» إذا
   `taxval = 0`؛ والسحابة تجد التمييز نفسه في `sales_invoice_lines.tax` و
   `purchase_invoice_lines.tax`.
7. **«سندات الصرف» = سندات الصرف + قيود الضرائب اليدوية (`Entry.type = 10 AND IsVAT = 1`)**
   (L565-L585) — والصافي ما عدا حساب الضريبة `2222001` والضريبة ما عليه؛ والسحابة تقرأ
   `journal_entries.is_vat` وتفرّق الحسابين بـ`tax_groups.vat_account_id`.
8. **`SetDate` (L219-L266)** — «ربع سنة» و«شهري» يكتبان الفترة فوق صندوقي التاريخ،
   والربع يسبق الشهر إذا عُدّا معاً.

### 8.5 التحويلات عن الديسكتوب (مبرَّرة)

1. **«رصيد افتتاحي» من قيدٍ لا من حقل.** الديسكتوب يقرأ قيداً إفتتاحياً
   (`Entry.type = 0`)؛ والسحابة تُسمّيه `source_type = 'opening'` — المفتاح الذي يطبعه
   بيان الحساب أصلاً باسم «قيد إفتتاحي» (`ENTRY_TYPE_LABELS`) — فلم يُضف التقرير حقل
   `accounts.opening_balance` إلى الرصيد كي لا يُحسب مرّتين.
2. **«سند قبض من عميل» و«سند صرف لمورد» (Id 5 · 6) يندمجان في «سند قبض» و«سند صرف»
   (Id 7 · 8)** — الديسكتوب يفرّقهما بالطرف، و`entryTypeOf()` في السحابة يدمجهما أصلاً،
   وسطر السند يحمل نوعه بنفسه.
3. **«لاغي» هو ما عُكس.** لا حالة `void` على القيد في السحابة: الإلغاء قيدٌ عكسيّ
   (`kind = 'reversal'`) يشير إلى أصله، ف«حالة القيد = لاغي» تعني «له قيدٌ عكسيّ»،
   و«مسودة» هي الحالة الوحيدة غير المُرحَّلة.
4. **أربعة بنود بلا مقابل** — «المبيعات المحلية الخاضعة للنسبة الصفرية» (Label30/31) و
   «الاستيرادات الخاضعة للقيمة المضافة بالنسبة الأساسية» (Label51/52) و«الاستيرادات …
   آلية الاحتساب العكسي» (Label47/48) لا تُحسب في الديسكتوب نفسه (تُترك أصفاراً)، فهي
   في السحابة أصفارٌ أيضاً ومُعلَنة في §10.
5. **قيمة المخزون** — `Inventory.InventoryCost(branch, toDate)` (L379) دالةٌ في قاعدة
   الديسكتوب؛ والسحابة تحسب الرصيد المتحرك من `inventory_transactions` حتى «إلى تاريخ»،
   وهو الرقم نفسه بزيادة `w.branch_id` بدل رقم الفرع.
6. **عمود «القسم» في الإقرار الضريبي** — التقرير المطبوع يرسم المبيعات والمشتريات
   كتلتين متجاورتين بعنوانَي «تفاصيل المبيعات» و«تفاصيل المشتريات»؛ وشبكة السحابة عمودٌ
   واحد، فحمل عمود «القسم» العنوانين («ضريبة القيمة المضافة على المبيعات» · «…
   على المشتريات») بدل أن تُرسم الكتلتان فوق بعضهما، و«الوصف» هو تسمية عمود الصفوف
   في `.repx` نفسه.
7. **بطاقتا «الرصيد (مدين)» و«الرصيد (دائن)»** بدل `txtBalance` و`lblStatus`
   (L466-L480): الديسكتوب يطبع رقماً واحداً ويكتب وجهه بجانبه؛ والبطاقة تحمل الرقم،
   فصارت بطاقتان إحداهما صفر دائماً.
8. **«عدد القيود» و«عدد السطور» و«الفرق»** بطاقاتٌ مضافة: الأوليان نظيرُ «عدد
   السجلات:» (`lblCount` في `frmRptCostCenter.xaml` L498) في سائر نوافذ الجزء،
   والثالثة تحمل الرقم الذي يوازن به الديسكتوب «✅ قيد متوازن» / «❌ قيد غير متوازن»
   (L291 · L298).
9. **«📆 ربع سنة» و«📆 شهري» فلاتر «select»** بدل زرّي اختيار ومربّعين — المحرّك لا
   يعرف «زر اختيار»، والسلوك واحد: الفترة تُكتب فوق صندوقي التاريخ.

### 8.6 الاختبارات والتحقّق الحيّ

- `apps/api/test/report-accounting.spec.ts` — 12 اختباراً: السجل بأعمدة الديسكتوب،
  صيغة الرصيد من وجهين، «الحساب الرئيسي» شجرةً، المندوب، نوع القيد وحالته ورقم
  المستند، سطور القيد وإجمالياه، تجميع الأرباح والخسائر على الآباء، «تجميعي» و«تفصيلي»
  في مراكز التكلفة (والورقة بلا أبناء تُبلّغ عن نفسها في «تفصيلي» وحده)، البنود
  الثلاثة عشر للإقرار وصافي الضريبة، «ربع سنة» و«شهري»، فترةٌ بلا حركة، وعزلُ
  مؤسساتٍ وإذنُ `reporting.view`.
- `scripts/verify-reports-accounting.mjs` — 77 فحصاً على المستأجر `demo` الحيّ:
  خطّ أساس قبل الكتابة وكل رقمٍ بعده **فرقٌ عن الخط**، ستة حسابات وثلاثة مراكز تكلفة
  وأربعة قيود وسندان موسومة بختم، القيود تُعكس في النهاية (لا تُلغى: السحابة تعكس
  والديسكتوب كذلك)، والحسابات والمراكز التي حملت حركة تُرفض بحذفها رفضاً من قاعدة
  الدفاتر (409) لا بخطأ. أُجري ست مراتٍ متتالية بصفر فشل.

## 9. الجزء السادس — 💰 تقارير الخزينة والرواتب والمستخدمين (`frmRptKhzna` · `frmRptSalary` · `frmRptReseved` · `frmrptUsersRecords` · `frmRptRentInvoices`)

### 9.1 النوافذ وما تقرأه

| النافذة | العنوان | الملف | الأسطر | التقرير في السجل |
|---|---|---|---|---|
| `frmRptKhzna.xaml` | «حركة الصندوق» (🏦 حركة الصندوق) | `Form_WPF/frmRptKhzna.xaml.cs` | 538 | `cash-statement` |
| `frmRptSalary.xaml` | «تقرير الرواتب» (💼 تقرير الرواتب) | `Form_WPF/frmRptSalary.xaml.cs` | 190 | `salary-statement` |
| `frmRptReseved.xaml` | «تقرير الرواتب المستحقة» | `Form_WPF/frmRptReseved.xaml.cs` | 225 | `salary-reserved` |
| `frmrptUsersRecords.xaml` | «سجلات المستخدمين» | `Form_WPF/frmrptUsersRecords.xaml.cs` | 168 | `user-records` |
| `frmRptRentInvoices.xaml` | «تقرير فواتير التأجير» | `Form_WPF/frmRptRentInvoices.xaml.cs` | 716 | `rent-invoices` |

ونافذةٌ سادسة، `frmInvRptType.xaml` «🖨️ افتراضي طباعة الفواتير» (122/72)، نافذةُ إعدادٍ
راديوان («📄 ورقة A4» · «🧾 ورق صغير») تكتب `UPDATE sett SET val = 1|2 WHERE id = 1`
ولا تعرض شبكةً واحدة — فهي من إعدادات الطباعة، وقد نُقلت في الجزء السابع (§10.1 و§10.4).

### 9.2 السطح (نقاط النهاية)

| التقرير | نقطة النهاية | الفلاتر |
|---|---|---|
| حركة الصندوق | `GET /api/v1/reports/cash-statement` | 🏦 الصندوق · 📅 من تاريخ · ⏰ من وقت (HH:mm) · إلى تاريخ · إلى وقت (HH:mm) |
| تقرير الرواتب | `GET /api/v1/reports/salary-statement` | 📅 الشهر · السنة · 🏢 الفرع |
| تقرير الرواتب المستحقة | `GET /api/v1/reports/salary-reserved` | 📅 من · إلى |
| سجلات المستخدمين | `GET /api/v1/reports/user-records` | 👤 المستخدم · 📅 من/إلى |
| تقرير فواتير التأجير | `GET /api/v1/reports/rent-invoices` | 📅 من/إلى · 🏢 الفرع · 👥 العميل · 👤 المستخدم · 📁 الفئة · 🔢 رقم الفاتورة · ⚙️ نوع العملية · 📋 الخطة |

### 9.3 مطابقة الأعمدة والتسميات

| النافذة | الأعمدة المنقولة | التسميات |
|---|---|---|
| `frmRptKhzna` | 8 من 8 | 100٪ |
| `frmRptSalary` | 10 من 11 (زر «👁️ عرض») | 100٪ |
| `frmRptReseved` | 7 من 7 (+ أربعة من شبكة التفاصيل) | 100٪ |
| `frmrptUsersRecords` | 5 من 5 | 100٪ |
| `frmRptRentInvoices` | 9 من 9 (+ خمسة أعمدة سحابية) | 100٪ |

### 9.4 القواعد المنقولة كما هي

1. **«رصيد سابق» سطرٌ يفتح الكشف (L198-L233)** — يظهر كلما اختيرت فترة، ويحمل واردَها
   وصادرَها قبل «من تاريخ»؛ ويليه سطرٌ لكل قيد بـ`runBalance += dept − credit`.
2. **البطاقتان (L299-L300)** — ⚖️ الرصيد الإجمالي هو الرصيد في نهاية النافذة،
   و📅 رصيد الفترة المحددة هو ما حرّكته النافذة وحدها.
3. **`gross = tot_salary + Houses + Travel + salary_add` و`net = gross − salary_sub`
   (L104-L113)**، و«💰 إجمالي الرواتب» مجموع الصوافي لا الإجماليات (L113).
4. **`CalcIncome` (L258-L274)** — `proc_type` 2 مرتجع، و4 خارج الحساب، وما عداهما إيراد؛
   و«الصافي» = الإيرادات − المرتجع.
5. **«ضمن الخطة» (L319-L333)** — ثلاثة راديوات: الكل · ضمن الخطة · خارج الخطة.

### 9.5 التحويلات عن الديسكتوب (مبرَّرة)

1. **حساب الصندوق على الصندوق لا بالاسم** — الديسكتوب يبحث عن الحساب بمطابقة
   `Accounts_Index.AName = Stocks.name AND Accounts_Index.Type = 2` (L159-L164)؛
   والسحابة تعلّقه على الخزينة (`cash_locations.account_id`) منذ المرحلة 06، وترفض
   بالجملة نفسها حين يغيب («لم يتم العثور على حساب مرتبط بهذا الصندوق»).
2. **«💰 الإجمالي» = الصافي + الخصومات** — الإذن السحابي يحمل «بدلات أخرى» لا عمودَ لها
   في شبكة الديسكتوب، فالإجمالي يُقرأ من الصافي والخصومات (وهو ما يفعله
   `GET /hrm/reports/salary` منذ المرحلة 08).
3. **المسيّر بديلُ `Salary_Res`** — «رقم السند» هو رقم القيد الذي يحمل الاستحقاق،
   و«📝 ملاحظات» سببُ العكس (`reversal_reason`)، وعدّاد الموظفين والإجمالي والخصومات
   وصافي المستحق مأخوذةٌ من شبكة التفاصيل التي يفتحها الديسكتوب بزر «👁️ عرض»
   (`Salary_Res_Details`: Basic · Houses · travel · sal_add · Total · Sal_Sub · Net_Sal).
4. **`Log4NetLog` هو سجلّ التدقيق** — «🖥️ الجهاز» يقرأ عنوانَ المنادِي (`meta.ip` ثم
   `ua` ثم `entity`) بدل اسم الصنف الذي يسجّله log4net؛ و«👤 المستخدم» فلترٌ على الموظف
   لأنّ الديسكتوب يسمّي الموظف والسحابة تسمّي العضوية، والطريق بينهما
   `employees.membership_id → memberships.user_id`. وأُضيف فلتر الفترة: الديسكتوب يقرأ
   السجلّ كله، وسجلٌّ بلا حدٍّ لا يُطبع.
5. **`proc_type` لا عمودَ له في السحابة** — «تأجير» فاتورةٌ مُرحَّلة فاتورةُ مبيعاتها،
   و«مرتجع» مبيعاتُها مردودة، و«معلق» لم تُرحَّل بعد، و«حجوزات» حجزٌ بلا فاتورة.
6. **«ضمن الخطة» باليوم لا بالمركب** — الديسكتوب يحمل علماً ثابتاً على المركب
   (`Marine.IS_InPlan`)؛ والسحابة تعرف الخطة يوماً بيوم (`marina_operation_plan_lines`
   على تاريخ الفاتورة).
7. **أعمدةٌ سحابيةٌ زائدة** — «قيمة الفترة · الإضافات · التأمين · الإجمالي · المرافقون»
   في «فواتير التأجير» (كما أُضيف «رقم الأمر والمرجع والحالة» في الجزء الرابع).
8. **«إجمالي الفترة:» لم يُنقل** — تسميةٌ في `frmRptRentInvoices` (L500) بلا صندوقِ
   قيمةٍ في النافذة نفسها: لا رقمَ لها عند الديسكتوب فلا رقمَ لها عندنا (§13).
9. **📱 الجوال عمودٌ لا فلتر** — النافذة تعرّض مربّعاً نصّياً للجوال، والبحثُ النصّيُّ
   على رقم هاتف ليس فلتراً في محرّك التقارير (§13).

### 9.6 الاختبارات والتحقّق الحيّ

- `apps/api/test/report-treasury-hrm.spec.ts` — 9 اختبارات: السجل بأعمدة الديسكتوب،
  «رصيد سابق» والرصيد المتحرك والبطاقتان، بلا صندوق وبلا فترة، الإجمالي والصافي
  وإجمالي الرواتب و«كل الفترة»، المسيّر وعداده ومستحقه وملاحظاته، سجلّ المستخدمين
  كاملاً وسجلَّ موظفٍ واحد، تأجير · معلق · حجوزات والإيرادات والمرتجع والصافي، فترةٌ
  بلا حركة، وعزلُ مؤسساتٍ وإذنُ `reporting.view`.
- `scripts/verify-reports-treasury-hrm.mjs` — 62 فحصاً على المستأجر `demo` الحيّ:
  خطّ أساس قبل الكتابة وكل رقمٍ بعده **فرقٌ عن الخط**، موظفان وإذنا صرف ومسيّران
  مُرحَّلٌ ومعكوس (بسنواتٍ لا يستخدمها مسيّرٌ حقيقي)، وثلاثة حجوزات، والسندات تُلغى
  والقيود تُعكس في النهاية، وما لا رجعة فيه يُرفض بحذفه:
  `SALARY_PAYMENT_POSTED` · `EMPLOYEE_ON_PAYROLL` · `ACCOUNT_POSTED`. أُجري أربع
  مراتٍ متتالية بصفر فشل؛ ويُنهِي التشغيلُ خدمةَ من استخدمهم من الموظفين ليبدأ
  التشغيلُ التالي بعدّادٍ من اثنين.

## 10. الجزء السابع — 🖨️ إعدادات الطباعة (`frmSettings.xaml` «خيارات الطباعة» · `frmInvRptType.xaml` · `Class/Print.cs`)

### 10.1 النوافذ وما تقرأه

| الملف | ما هو | ما يقرأه/يكتبه |
|---|---|---|
| `Form_WPF/frmSettings.xaml` (2364 سطراً) | نافذة الإعدادات، وتبويب «خيارات الطباعة» فيها (الأسطر 889-1312) | يقرأ `SettingPrint WHERE Inv_Id=@id` (L1237) و`PrinterSettings` (L1293)، ويكتب بـ`delete` ثم `insert` (L2125-L2156) |
| `Form_WPF/frmInvRptType.xaml` (122 سطراً) | «🖨️ افتراضي طباعة الفواتير» | راديوان: «📄 ورقة A4» · «🧾 ورق صغير» → `UPDATE sett SET val = 1|2 WHERE id = 1` |
| `Class/Print.cs` (1237 سطراً) | محرّك الطباعة لكل نافذة | `Print(int RptId)` يقرأ الصف، و`Printing()` يُصدِر الورقة |
| `Reports/header.repx` · `Reports/footer.repx` | الترويسة والتذييل | يربطان `Common.FoundationInfoDT`: الاسم بالعربية والإنجليزية · النشاط · الرقم الضريبي · الجوال · الهاتف · السجل التجاري |

`SettingPrint` عموداً عموداً (`CrystalLiteDB.txt` L2260-L2280)، ومقابلُه في السحابة:

| العمود | الشاشة | في السحابة |
|---|---|---|
| `Inv_Id` | راديوات «🧩 تفعيل إعدادات الطباعة» | **`scope` نصّي**: `default` · `purchases` · `sales` · `pos` · `rental` · `contracts` · `reports` · `report:<key>` |
| `printType` | راديوا `frmInvRptType` | `print_type` 1 = A4 · 2 = ورق صغير |
| `PrintHeader` | «طباعة ترويسة الفاتورة» (`chkInvHeader`) | `print_header` |
| `PrintFooter` | «طباعة تذييل الفاتورة» (`chkInvFooter`) | `print_footer` |
| `PrintStamp` | «طباعة الختم» (`ckPrintStamp`) | `print_stamp` |
| `PrintTotItem` | «طباعة تفاصيل الأصناف» (`ckPrintItems`) | `print_item_details` |
| `PrintTotGroup` | «طباعة مجموعات الأصناف مع إغلاق اليومية» (`ckPrintGroups`) | `print_item_groups` |
| `PrintComponentsItemsIndividually` | «طباعة مكونات الأصناف المركبة بشكل منفرد» | `print_components_individually` |
| `printmakpay` | «طباعة make pay» | `print_make_pay` |
| `printNo` | «عدد النسخ» (`cmbPrintNo`) | `print_no` 1..50 |
| `PrintItemType` | — | `print_item_type` |
| `CasherPrinter` · `kitchenprinter` | «طابعة الكاشير» · «طابعة المطبخ» | `casher_printer` · `kitchen_printer` |
| `RptName` · `RptURl` | «اسم التقرير» · «مسار التقرير» | `rpt_name` · `rpt_url` |
| `note` | «ملاحظات التقرير» (`txtNote`) | `note` |
| `HeaderImage` · `FooterImage` · `StampImage` | «الترويـسة» · «التـذيـيـل» · «الخـتـم» (📂 رفع · 🗑️ حذف) | `header_image_url` · `footer_image_url` · `stamp_image_url` |

### 10.2 السطح (نقاط النهاية)

| الطريقة | المسار | الإذن |
|---|---|---|
| `GET` | `/reports/print-settings` | `reporting.view` |
| `GET` | `/reports/print-settings/:scope` | `reporting.view` |
| `PUT` | `/reports/print-settings/:scope` | `reporting.layout.manage` |
| `DELETE` | `/reports/print-settings/:scope` | `reporting.layout.manage` |
| `GET` | `/reports/print/:key` (قائم) | `reporting.view` — يقرأ الإعدادات الآن |
| `POST` | `/reports/:key/export?format=pdf` (قائم) | `reporting.export.execute` — الورقة نفسها |
| `GET` | `/reports/print/invoices/:id` · `/purchase-invoices/:id` · `/vouchers/:id` · `/journal-entries/:id` · `/shifts/:id` (قائمة) | `reporting.view` — تقرأ الإعدادات الآن |

والوثائق الخمس تقرأ نطاقها كما يقرأها الديسكتوب: `frmPurchInv` يطبع بـ`new Print(1)`
«مشتريات»، و`frmSalesInvoice` بـ`new Print(InvType)` = 2 «مبيعات»، و`frmCloseShift` يقرأ
`SettingPrint WHERE Inv_Id = 6` «تقارير»، و`frmSandQD` بـ`new Print(11)` و`FrmNewEntry`
بـ`Inv_Id=9` — عددان لا راديوَ لهما في `frmSettings` فيرجعان إلى «الإفتراضي».

والشاشة `/settings/printing` في `apps/staff` (ضمن وحدة «الإعدادات»)، ومن كل تقريرٍ زرّ
«إعدادات الطباعة» بجانب «طباعة / PDF».

### 10.3 مطابقة التسميات

كل تسمية في الشاشة مأخوذة نصّاً من `frmSettings.xaml`: «🧩 تفعيل إعدادات الطباعة» ·
«الإفتراضي» · «مشتريات» · «مبيعات» · «نقطة بيع» · «تأجير» · «عقود» · «تقارير» ·
«طابعة الكاشير» · «طابعة المطبخ» · «عدد النسخ» · «اسم التقرير» · «مسار التقرير» ·
«ملاحظات التقرير» · «طباعة ترويسة الفاتورة» · «طباعة تذييل الفاتورة» · «طباعة الختم» ·
«طباعة تفاصيل الأصناف» · «طباعة مجموعات الأصناف مع إغلاق اليومية» · «طباعة مكونات الأصناف
المركبة بشكل منفرد» · «طباعة make pay» · «الترويـسة» · «التـذيـيـل» · «الخـتـم» ·
«📄 ورقة A4» · «🧾 ورق صغير». نسبة المطابقة **100٪**، والتسميتان المضافتان — «💾 حفظ» و
«🗑️ إرجاع إلى الإفتراضي» — على النمط نفسه الذي استخدمه الديسكتوب في تبويباته الأخرى
(«💾 حفظ إعدادات المزامنة» · «💾 حفظ إعدادات الإغلاق»).

### 10.4 القواعد المنقولة كما هي

1. **نطاقٌ واحد لكل قسم، والإفتراضي يسري على ما لم يُحدَّد** — `frmSettings.xaml.cs`
   L2095-L2116 تحفظ 0 للإفتراضي و1..6 للأقسام؛ والسحابة تحفظ الاسم، وتبحث في
   `report:<key>` أولاً ثم «تقارير» ثم «الإفتراضي» (`PrintSettingsService.effective`).
2. **🔢 عدد النسخ حلقةُ طباعة** — `Printing()` (L201-L206) يكرّر `Print()` بقدر `PrintNo`؛
   والورقة السحابية تتكرّر بقدرها، كل نسخة في صفحة (`page-break-before`)، ويُطبع
   «عدد النسخ: N» في رأس الورقة حين يزيد عن واحدة.
3. **📄 نوع الورقة راديوان لا ثلاثة** — `frmInvRptType` يكتب 1 أو 2 في `sett`؛
   و`print_type` هنا 1 = A4 (عرضي كما أوراق التقارير) و2 = 80mm.
4. **🏛️ الترويسة والتذييل تقريران فرعيّان** — `Printing()` يُدخِل `header.repx` و
   `footer.repx` في `headerRpt` و`footerRpt`؛ وعندنا كتلة المنشأة (الاسم · الرقم الضريبي ·
   السجل التجاري · الهاتف) فوق الجدول، وسطرُ الاتصال (`footer.repx`: الهاتف · الجوال ·
   العنوان) تحته.
5. **🔖 الختم تحت التواقيع** — «أعده · راجعه · المدير» ثم صورة الختم.
6. **📝 ملاحظات التقرير تحت الجدول** — `txtNote`، لا في الرأس.
7. **الإعدادات الافتراضية بلا صف** — `Print.cs` L54-L64: `PrintType=2` و
   `PrintHeader=true` و`PrintFooter=false` و`PrintNo=1` و`PrintItemType=1`.
8. **🖨️ زرّا الطباعة والمعاينة ورقةٌ واحدة** — «👁️ معاينة» و«🖨️ طباعة» عند الديسكتوب
   يمرّان بـ`Print.cs` نفسه؛ وعندنا `/reports/print/:key` و`export?format=pdf` يمرّان
   بـ`ReportingService.printOptionsFor()` نفسه.
9. **🧾 الوثائق تقرأ الإعدادات مثل التقارير** — الفاتورة والسند والقيد والإغلاق تمرّ
   بـ`PrintTemplatesService.documentPage()`: «عدد النسخ» نسخاً، ونوع الورق، والترويسة
   (`header.repx`)، والتذييل (`footer.repx`)، والختم — لأنّ `Print.cs` هو محرّك الوثائق
   قبل أن يكون محرّك التقارير.

### 10.5 التحويلات عن الديسكتوب (مبرَّرة)

1. **النطاق اسمٌ لا عدد (`Inv_Id`)** — نافذتان في الديسكتوب لا تتفقان على معنى الأعداد:
   `frmSettings` يحفظ 0..6، و`frmRptKhzna` (L106) يقرأ 12، و`frmRptEntries` يقرأ 9،
   و`frmRptRentInvoices` (L541) يقرأ 14. حفظنا المعنى واسمينا النطاق، وأضفنا
   `report:<key>` ليحمل تقريرٌ بعينه إعداداته — وهو ما أرادته النوافذ بالأعداد المختلفة.
2. **الصور روابط لا بايتات** — `HeaderImage` · `FooterImage` · `StampImage` أعمدةُ
   `[image]` في SQL Server؛ والسحابة لا تخزّن ملفات (سَبقَتها `vessel_groups.image_url`)،
   فهي روابط `https://` تُدرج في الورقة. والتحقّق يرفض ما ليس رابطاً (400).
3. **🖨️ الطابعتان تُحفظان ولا تُستخدمان** — `Printing()` يُرسل العمل إلى `CasherPrinter`
   ونسخةً إلى `kitchenprinter`؛ والخادم لا يرى طابعات المحل، فالاسمان يُعرضان في شريط
   الطباعة (`.no-print`) ويبقى اختيارُ الطابعة في نافذة المتصفّح — وهو مقامُ «طابعة
   الكاشير» عند من يقف أمام الصندوق.
4. **`PrinterSettings` («📑 ربط الطابعات بالتقارير») لم يُنقل** — شبكة
   (`Inv_Id, PrintName, Printer, RptUrl, RptName`) يقرأها `Print.cs` لتوجيه كل تقرير إلى
   طابعة؛ ولا سبيل إلى ذلك من المتصفّح، فالجدول مؤجَّل (§13).
5. **`PrinterItemType` و`PrintComponentsItemsIndividually` حقولٌ لا أثر** — تُحفظ وتُقرأ
   ولا تغيّر الورقة بعد: ترتيبُ بنود الفاتورة والمكوّنات لم يُنقل إلى هذه الورقة بعد
   (§13).
6. **مسار التقرير حقلٌ تذكاري** — `RptURl` مجلدٌ على قرص الديسكتوب؛ ويُحفظ نصّاً كما هو
   لأنّ من ينقل ملفاته بين جهازٍ وخادمٍ يبحث عنه، ولا أثر له في الطباعة هنا.
7. **الإعداد الافتراضي A4 لا «ورق صغير»** — `Print.cs` L56 يبدأ بـ`PrintType=2`؛
   وأوراقُنا تُطبع PDF، و`frmInvRptType` يفتح على «📄 ورقة A4»، فالافتراضي 1.

### 10.6 الاختبارات والتحقّق الحيّ

- `apps/api/test/print-settings.spec.ts` — 15 اختباراً: النطاقات السبعة بإعدادات
  `Print.cs` قبل أي حفظ، الحفظ حقلٌ بحقل والقراءة بعده، حفظٌ ثانٍ يبقي ما لم يُرسل،
  `report:<key>` لتقريرٍ مسجّل ورفضُ تقريرٍ غير مسجّل، نطاقٌ مجهول 404، التحقّق
  (نسخ · ورق · روابط)، الحذف يعيد الافتراضي، **العزل بين المستأجرين**، الورقة
  (النسخ · 80mm · الترويسة · التذييل · الختم · الملاحظة · الطابعتان)، ورقةٌ بلا ترويسة
  ولا ختم، تجاوزٌ من سطر الاستعلام، سلسلة الرجوع (تقرير → تقارير → إفتراضي)،
  «طباعة / PDF» يطبع الورقة نفسها، و403 لعارضٍ بلا `reporting.layout.manage` ولمستخدمٍ
  بلا `reporting.view`.
- `scripts/verify-print-settings.mjs` — **50** فحصاً على المستأجر `demo` الحيّ: خطّ أساس
  لما كان محفوظاً، والسبعة نطاقات، والحفظ والقراءة، والمرفوضات الستّ، والورقة بكل
  أجزائها، والتجاوز لمرة واحدة، وسلسلة الرجوع، وصلاحيات الأدوار، **والوثائق** (فاتورة
  مبيعات بنطاق «مبيعات»، وسندٌ وقيدٌ بنطاق «الإفتراضي»)، ثم تنظيفٌ يعيد كل نطاقٍ إلى ما
  كان (حذفاً إن لم يكن محفوظاً، وإعادةً لقيمه إن كان). أُجري ثلاث مراتٍ متتالية بصفر
  فشل وبلا أثرٍ بعد التشغيل.

## 11. الجزء الثامن — 📑 كشوف الحساب (سبع نوافذ خارج جدول `frmRpt*`)

### 11.1 لماذا جزءٌ ثامن خارج الجدول

الجدول في §1 يعدّ **32** نافذةً اسمها `frmRpt*`، وقد أُنجزت كلها في الأجزاء 1–7. والديسكتوب
يملك عائلةً أخرى لا تدخل في ذلك العدّ: نوافذ «كشف حساب»، وهي سبعٌ منتشرة في
`Form_WPF/` بأسماء لا تبدأ بـ`frmRpt`. أربعٌ منها كانت حيّة في السحابة من مراحل سابقة
(المرحلة 07 والرواتب)، وثلاثٌ لم تكن — فالجزء الثامن هو الثلاث الباقية، واستكمال فلاتر الأربع القائمة:

| النافذة | العنوان | ملف التخطيط | حالها في السحابة قبل الجزء الثامن |
|---|---|---|---|
| `frmCustAccount` | أرصدة حساب العملاء | `rptCustAccount` | ❌ تقريرٌ بدائي `customer-balances` (ثلاثة أعمدة، بلا فلاتر) |
| `frmCustAccountGet` | 📋 كشف حساب عميل | `rptCustAccountGet` | ❌ تقريرٌ بدائي `party-statement` (بلا بطاقات ولا نوع حساب) |
| `frmCustLastPay` | 📋 حركة آخر سداد للعملاء | `rptCustLastPay` | ❌ لا مقابل له أصلاً |
| `frmEmpAccountGet` | 📋 كشف حساب موظف | `rptCustAccountGet` | ✅ `GET /hrm/employee-statement` (المرحلة 08) — كامل |
| `frmAccountBalance` | كشف حساب تفصيلي | `Statement` | ✅ `GET /statements/general-ledger/:id` (المرحلة 07 ج2) — ناقص ⏰ الوقت و📋 نوع القيد |
| `frmAccountsStatement` | كشف حساب رئيسي | `Statement` | ✅ النهاية نفسها مع `with_descendants=1` — ناقص الفلاتر نفسها |
| `frmCostCenterBalance` | تقرير مركز كلفة | `RptCostCenterStatement` | ✅ `GET /statements/cost-center/:id` — ناقص 📋 نوع القيد |

### 11.2 النوافذ الثلاث وما تقرأه

**`Form_WPF/frmCustAccount.xaml` (556) + `.xaml.cs` (683) — «أرصدة حساب العملاء».**
شبكةٌ من سبعة أعمدة: `#` · `🔢 رقم الحساب` · `👤 اسم العميل` · `💸 حركة مدين` ·
`💰 حركة دائن` · `⚖️ الرصيد` · `📌 الحالة`؛ وفلاتر 👤 اسم العميل · 🔵 الكل · 🤝 كل
المندوبين · 📅 فترة كاملة · من/إلى. القواعد: حركة العميل هي حركة **حسابه هو**
(`Entry_sub.acc_no = Customers.AccountCode`, L221-L231)، والرصيد `Abs(debit − credit)`
مقرّباً إلى ثلاثة أرقام، والحالة «مدين» إن زاد المدين و«دائن» إن زاد الدائن
(L258-L288)، ولا يُطبع من لم يتحرّك حسابه. ومجاميعه ليست بطاقاتٍ بل صفُّ مجموعٍ في
الشبكة نفسها (`GridSummaryItem`: «مدين: {0:n2}» · «دائن: {0:n2}» · «الرصيد: {0:n2}»).

**`Form_WPF/frmCustAccountGet.xaml` (747) + `.xaml.cs` (1017) — «📋 كشف حساب عميل».**
شبكةٌ من ثمانية: `م` · `مدين` · `دائن` · `العميل / المورد` · `رقم القيد` · `تاريخ القيد` ·
`البيان` · `تفاصيل`؛ وفلاتر 🏷️ نوع الحساب (🔵 الكل · 👤 عملاء · 🏭 موردين) · 👤 اسم العميل ·
كل الفروع · 📅 فترة كاملة · من/إلى. القواعد: سطرٌ لكل **قيد**
(`GROUP BY Entry.GlobalID, Entry.date, notes, acc_no`, L336-L343)، والبيان هو بيان القيد
(`Entry.notes`)، وأربع بطاقات: `💳 إجمالي المدين` · `💵 إجمالي الدائن` ·
`⚖️ الرصيد المدين` · `⚖️ الرصيد الدائن`، و`UpdateSummary` (L583-L609) يضع الرصيد على
**جانبٍ واحد**، فلا تجتمع البطاقتان أبداً.

**`Form_WPF/frmCustLastPay.xaml` (628) + `.xaml.cs` (630) — «📋 حركة آخر سداد للعملاء».**
شبكةٌ من أحد عشر: `رقم الحساب` · `اسم العميل` · `الهاتف` · `الجوال` · `قيمة آخر سداد` ·
`تاريخ آخر سداد` · `نوع السند` · `الرصيد` · `الحالة` · `رقم القيد` · `عرض`؛ وفلاتر 👤 اسم
العميل و✅ الكل — **ولا مربّع تاريخ فيها البتّة**. القواعد: آخر قيدٍ حرّك الحساب
(`SELECT TOP 1 … ORDER BY id DESC`, L222-L231)، وقيمته `dept == 0 ? credit : dept`
(L258)، والرصيد رصيد الحساب كله لا رصيد الفترة (L258-L275)، وثلاث بطاقات:
`💳 الإجمالي` · `⚖️ الرصيد` · `📌 السجلات` (L430-L435).

### 11.3 السطح (نقاط النهاية)

| الطريقة | المسار | الإذن |
|---|---|---|
| `GET` | `/api/v1/reports/customer-balances?partyId=&partyKind=&salesmanId=&from=&to=` | `reporting.view` |
| `GET` | `/api/v1/reports/party-statement?partyId=&partyKind=&branchId=&from=&to=` | `reporting.view` |
| `GET` | `/api/v1/reports/customer-last-payment?partyId=&partyKind=` | `reporting.view` |
| `GET` | `/api/v1/statements/general-ledger/:accountId?…&from_time=&to_time=&kind=` | `accounting.reports.view` |
| `GET` | `/api/v1/statements/cost-center/:costCenterId?…&kind=` | `accounting.reports.view` |
| `GET` | `/api/v1/reports/print/:key` · `POST /api/v1/reports/:key/export` | `reporting.view` · `reporting.export.execute` |

🏷️ `partyKind` فلترٌ جديد في السجل (`all` · `customer` · `supplier`)، و`kind` على
كشفَي الحساب يقبل مفردات `source_type` نفسها التي يطبعها عمود «النوع»، فلا يفترق
المختار عن المطبوع. والشاشة: صفّان جديدان في شجرة «التقارير» — «أرصدة حساب العملاء»
(مُسمّىً باسم نافذته) و«حركة آخر سداد للعملاء» و«كشف حساب عميل».

### 11.4 القواعد المنقولة

1. **👤 حركة الطرف هي حركة حسابه.** `frmCustAccount` L221 و`frmCustAccountGet` L336
   يقرآن `Entry_sub.acc_no = Customers.AccountCode` وحده. أبقينا ذلك
   (`parties.receivable_account_id` · `parties.payable_account_id`)، وجعلنا الأسطر
   التي تحمل الطرف (`journal_entry_lines.party_id`) **بديلاً** لمن لا حساب له في الدليل —
   فعميلٌ فُتح على عجل لا تضيع سداداته. أما عدُّ الأسطر الحاملة للطرف مع حسابه فمزدوج:
   سندُ القبض يمدين الصندوق ويُدين العميل، والسطران كلاهما باسمه (§11.5).
2. **⚖️ الرصيد مطلقٌ والحالة بزيادة الجانب** (L258-L288): `Abs(debit − credit)` و«مدين»
   إن زاد المدين. وهو خلافُ «كشف حساب تفصيلي» الذي يوقّع الرصيد بطبيعة الحساب
   (`frmAccountBalance` L216-L222: أول رقم الحساب 1 أصول و3 مصروفات تجمع
   `dept − credit` و2 خصوم و4 إيرادات تجمع `credit − dept`) — فكل نافذةٍ حسابها.
3. **💳 الرصيد على جانبٍ واحد** (`UpdateSummary` L583-L609): الرصيد المدين أو الرصيد
   الدائن، ولا يجتمعان. البطاقتان في السحابة مجموعان من الصفوف، فاتفاقهما مع الجدول
   مضمونٌ ببنائهما لا بحسابٍ ثانٍ.
4. **💵 آخر سداد** (L222-L231 · L258): أحدث قيدٍ حرّك الحساب — بالتاريخ ثم الوقت ثم
   ترتيب السطر، لأن `ORDER BY id DESC` عند الديسكتوب هو «آخر ما كُتب».
5. **📅 الفترة بالساعة** (`frmAccountBalance` L458-L463): للنافذة صندوقا وقتٍ مع
   صندوقي تاريخ، فتُبنى الفترة من تاريخٍ ووقتٍ عند كل طرف؛ و`journal_entries` يحفظ
   التاريخ والوقت في عمودين (`date` · `entry_time`)، فأُلصقا هنا. والرصيد السابق يقرأ
   الساعة نفسها: قيدُ التاسعة **قبل** فترةٍ تفتح في الثانية عشرة من اليوم نفسه.
6. **📋 نوع القيد** (`cmbEntryType` L108-L127): قائمة الديسكتوب بالفهرس (0…16) وتخالف
   `GetEntryTypeName` في معنى الأرقام، فصرنا إلى مفردات `source_type` — وهي الأسماء
   ذاتها التي يطبعها عمود «النوع» في الكشفين.

### 11.5 التحقّق

- **14** اختباراً جديداً في `apps/api/test/report-party-statements.spec.ts` (الأطراف
  الثلاثة · البطاقات · نوع الحساب · الفرع والفترة · العزل · الصلاحيات).
- **2** اختبارَيْن ملحقين بـ`apps/api/test/accounting-statement.spec.ts` (13: ⏰ الوقت ·
  14: 📋 نوع القيد) على النهايات القائمة من المرحلة 07.
- `scripts/verify-party-statements.mjs` — **67** نقطة تحقّق حيّة في **11** قسماً
  (السجل · خطّ الأساس · الوثائق · الأرصدة · الفلاتر · الكشف · آخر سداد · الوقت ونوع
  القيد · فترةٌ بلا حركة · الطباعة والتصدير · الصلاحيات والتنظيف)، يعكس القيود ولا
  يحذفها، ويُحصي كل رقمٍ فرقاً عن خطّ أساس.
- الحزمة كلها: **121** ملفاً و**868** اختباراً (كانت 120 و852) و**36** اختبار شاشة،
  و`tsc` وlint وbuild نظيفة.

### 11.6 المؤجَّل عن قصد

- 📱 **عمود «الجوال»** في `frmCustLastPay` — لا عمود `mobile` على `parties`
  (الهاتف وحده)، كما أُجِّل في «تقرير فواتير التأجير» (الجزء السادس).
- 👁️ **زرّا «تفاصيل» و«عرض»** في `frmCustAccountGet` و`frmCustLastPay`
  (`EntryOper.ShowEntrySource`) — نقلٌ من تقريرٍ إلى نافذة قيد.
- ⚖️ **«نوع الرصيد» في `frmAccountBalance` و`frmCostCenterBalance`** — النافذة
  **تُخفي** العمود الآخر وبطاقتيه ولا تُسقط الصفوف (`ApplyColumnVisibilityFilters`
  L376-L404)؛ وإخفاءُ عمودٍ في نهايةِ برمجةٍ يعني حذفَ حقلٍ من الردّ، فتُرك حتى
  يُبتَّ في شكل الشاشة.
- 🧮 `frmCostCenterBalance` L236 يقرأ طبيعة الرصيد من **أول رقم رمز مركز الكلفة**
  (نقلٌ عن نافذة كشف الحساب)؛ والسحابة تقرأها من طبيعة الحساب نفسه لأنّ مركز الكلفة
  لا طبيعة له.

## 12. معايير القبول لكل جزء

1. كل تقريرٍ منقول يُسمّي ملفه من `Desktop_ERP` (`Form_WPF/frmRpt*.xaml` و
   `Reports/*.repx`) نصّاً في الوثيقة وفي تعليق تعريفه.
2. الأعمدة والفلاتر مأخوذة من تلك الملفات بالعربية، ونسبة المطابقة ≥ 90٪، وكل تسمية
   مخترَعة مبرَّرة في ملخّص الجزء.
3. نقطة نهاية حقيقية (لا شاشة بلا خدمة)، وعزل مؤسسات، وإذن `reporting.view`.
4. اختبارات API جديدة + سكربت تحقّق حيّ قابل لإعادة التشغيل غير مُتلف.
5. الشاشة تصل من طريقٍ حقيقي في الشجرة (`apps/staff/lib/navigation.ts`).
6. تحديث هذه الوثيقة و`docs/STATUS.md` و`docs/desktop-parity/README.md`.

## 13. مؤجَّل عن قصد

- 📑 `PrinterSettings` — شبكة «ربط الطابعات بالتقارير» في `frmSettings` (الجدول
  `Inv_Id, PrintName, Printer, RptUrl, RptName`، L2163-L2180، §10.5/4)؛ الخادم لا يرى
  طابعات المحل، ولا سبيل إلى توجيه ورقةٍ إلى طابعةٍ بعينها من المتصفّح.
- 🔢 `PrintItemType` و`PrintComponentsItemsIndividually` أثراً على الورقة (§10.5/5) —
  يُحفظان ويُقرآن، وترتيبُ بنود الفاتورة ومكوّناتها لم يُنقل بعد.
- 📊 «إجمالي الفترة:» في `frmRptRentInvoices` (L500) — تسميةٌ بلا صندوق قيمة (§9.5/8).
- 📱 مربّع «الجوال» فلتراً في `frmRptRentInvoices` (§9.5/9).
- ⚓ `Marine.IS_InPlan` علماً ثابتاً على المركب بدل خطة اليوم (§9.5/6).
- 👁️ أزرار «عرض» في `frmRptSalary` و`frmRptReseved` — تنقل من تقريرٍ إلى نافذةٍ أخرى.
- 🧾 «المبيعات المحلية الخاضعة للنسبة الصفرية» في `frmTaxRptPeriod` (Label30 · Label31،
  §8.5/4) — النافذة لا تملؤهما أصلاً، ولا معدّل ضريبةٍ صفريّاً في السحابة يميّز السطر.
- 🚢 بندا «الاستيرادات» في `frmTaxRptPeriod` (Label51 · Label52 · Label47 · Label48،
  §8.5/4) — لا استيراد في نموذج السحابة.
- 📋 `cost_center.type = 2` في `frmRptCostCenter` (L262) — «المركز الورقة يُبلّغ عن
  نفسه»؛ لا عمود `type` على `cost_centers`، وسلوك النافذة بعد `GetParent` أنّه لا يُبقي
  شيئاً على أيّ حال.
- 🖼️ أعمدة `DgvDelete` و`DgvEdit` و«تفاصيل» في `frmRptBalances` و`frmRptCostCenter`
  (§8.3) — أزرارٌ تنقل من تقريرٍ إلى نافذةٍ أخرى.
- 🔄 أنواع `DoProcess2(5 … 9)` من «تقرير الحركة اليومية» — تقرأ جدول `receipts`
  (سندات القبض) ولا مقابل له في السحابة (§6.5/8).
- 🧑‍💼 «👤 المندوب» في «تفاصيل فواتير المشتريات» — لا عمود `salesman_id` على
  `purchase_invoices` (§6.5/5).
- 📊 `FrmRptSalesChart` «تقرير بياني للمبيعات» — أربع رسوم LiveCharts، حتى يُبتَّ في
  مكتبة الرسوم (§6.1).
- 🔗 «رقم المرجع» نصّاً حرّاً (`inv.Reff_No`) — السحابة تحفظ الرابط لا الصندوق (§6.5/1).
- 👤 المستخدم و🧑‍💼 المندوب في `frmRptItemsSalesDetailsPOS` و`frmRptSalesByCategory`
  (§5.5/6) — حتى يوجد مرشِّح `kind: 'membership'` في السجل.
- 👤 «المستخدم» في «تقرير مستندات المخزون» `frmRptInventory` (§7.5/2) — حتى يُوحَّد عمود
  `created_by` على رؤوس مستندات المخزون.
- 🔧 «المكونات» شبكةً داخل «تقرير مواد المنتجة» بدل تقريرٍ ثانٍ (§7.5/10).
- 🚚 «مرتجع مناقلة» و«إلغاء مناقلة» عمودين مستقلّين في «مادة باجمالي الحركات» (§7.4/1)
  — الديسكتوب يحسب المناقلة بعمودين اثنين لا أربعة.
- «الأصناف الخاضعة للضريبة الإضافية» وعمود `DgvItemAdditionalTax` (§5.5/5).
- زرّا «تفاصيل» و«📄 الفاتورة» اللذان ينقلان من تقريرٍ إلى نافذةٍ أخرى (§5.5/4).
- التقارير التي ترسم بيانياً (`FrmRptSalesChart`) حتى يُبتَ في مكتبة الرسوم.
- تصدير PDF من الخادم (الطباعة تمرّ بطابعة المتصفّح اليوم).
- تقارير أُعيد بناؤها في مراحل سابقة بعناوين من اختيار السحابة (`sales-invoices` ·
  `net-sales` · …) — تُطابق مع الديسكتوب في الجزء الثالث.
