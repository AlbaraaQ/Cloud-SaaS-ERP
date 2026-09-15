# المرحلة 10 — التقارير (94 `.repx` → محرّك التقارير في السحابة)

**الحالة: الجزء الأول مُنجز** — 📊 حركة المبيعات (`Form_WPF/frmRptSalesInPeriod` +
`Reports/RptSalesInPeriod1.repx` + `RptSalesInPeriod2.repx`؛ **10** اختبارات API و**59**
نقطة تحقّق حيّة، **777** اختبار API). ما بقي من المرحلة 10 ستة أجزاء مبيّنة في §3،
و`SettingPrint` (إعدادات الطباعة) مؤجَّل إلى جزءٍ لاحق (§6).

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
| إعدادات الطباعة | جدول `SettingPrint` — `CrystalLiteDB.txt` L2260 (`Inv_Id` · `PrintHeader` · `PrintFooter` · `PrintStamp` · `printNo` · `printType` · `CasherPrinter` · `RptUrl` · `RptName`)؛ `frmRptSalesInPeriod` يقرأ `Inv_Id=12` |

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
| 4 | 📚 تقارير المخزون والأرقام التسلسلية — `frmRptInventory` · `frmRptItemsActivity(Detailed)` · `frmRptItemsExpiration` · `frmRptSerialNo` · `frmRptSerialNoSummary` · `frmRptProducedItems` | ⬜ |
| 5 | 📒 تقارير المحاسبة — `frmRptBalances` · `frmRptEntries` · `frmRptIncomeStatement` · `frmRptCostCenter` · `frmTaxRptPeriod` | ⬜ |
| 6 | 💰 تقارير الخزينة والرواتب والمستخدمين — `frmRptKhzna` · `frmRptSalary` · `frmRptReseved` · `frmrptUsersRecords` · `frmRptRentInvoices` · `frmInvRptType` | ⬜ |
| 7 | 🖨️ إعدادات الطباعة — `SettingPrint` (رأس · تذييل · ختم · عدد النسخ · الطابعة) و`Reports/header.repx`/`footer.repx` لكل تقرير | ⬜ |

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

## 7. معايير القبول لكل جزء

1. كل تقريرٍ منقول يُسمّي ملفه من `Desktop_ERP` (`Form_WPF/frmRpt*.xaml` و
   `Reports/*.repx`) نصّاً في الوثيقة وفي تعليق تعريفه.
2. الأعمدة والفلاتر مأخوذة من تلك الملفات بالعربية، ونسبة المطابقة ≥ 90٪، وكل تسمية
   مخترَعة مبرَّرة في ملخّص الجزء.
3. نقطة نهاية حقيقية (لا شاشة بلا خدمة)، وعزل مؤسسات، وإذن `reporting.view`.
4. اختبارات API جديدة + سكربت تحقّق حيّ قابل لإعادة التشغيل غير مُتلف.
5. الشاشة تصل من طريقٍ حقيقي في الشجرة (`apps/staff/lib/navigation.ts`).
6. تحديث هذه الوثيقة و`docs/STATUS.md` و`docs/desktop-parity/README.md`.

## 8. مؤجَّل عن قصد

- 🖨️ `SettingPrint` (رأس · تذييل · ختم · عدد النسخ · الطابعة الافتراضية) — جزءٌ سابع.
- 🔄 أنواع `DoProcess2(5 … 9)` من «تقرير الحركة اليومية» — تقرأ جدول `receipts`
  (سندات القبض) ولا مقابل له في السحابة (§6.5/8).
- 🧑‍💼 «👤 المندوب» في «تفاصيل فواتير المشتريات» — لا عمود `salesman_id` على
  `purchase_invoices` (§6.5/5).
- 📊 `FrmRptSalesChart` «تقرير بياني للمبيعات» — أربع رسوم LiveCharts، حتى يُبتَّ في
  مكتبة الرسوم (§6.1).
- 🔗 «رقم المرجع» نصّاً حرّاً (`inv.Reff_No`) — السحابة تحفظ الرابط لا الصندوق (§6.5/1).
- 👤 المستخدم و🧑‍💼 المندوب في `frmRptItemsSalesDetailsPOS` و`frmRptSalesByCategory`
  (§5.5/6) — حتى يوجد مرشِّح `kind: 'membership'` في السجل.
- «الأصناف الخاضعة للضريبة الإضافية» وعمود `DgvItemAdditionalTax` (§5.5/5).
- زرّا «تفاصيل» و«📄 الفاتورة» اللذان ينقلان من تقريرٍ إلى نافذةٍ أخرى (§5.5/4).
- صور الرأس والتذييل والختم (`HeaderImage` · `FooterImage` · `StampImage`).
- التقارير التي ترسم بيانياً (`FrmRptSalesChart`) حتى يُبتَ في مكتبة الرسوم.
- تصدير PDF من الخادم (الطباعة تمرّ بطابعة المتصفّح اليوم).
- تقارير أُعيد بناؤها في مراحل سابقة بعناوين من اختيار السحابة (`sales-invoices` ·
  `net-sales` · …) — تُطابق مع الديسكتوب في الجزء الثالث.
