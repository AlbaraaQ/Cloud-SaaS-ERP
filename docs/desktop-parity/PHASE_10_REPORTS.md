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
| 2 | 📦 تقارير الأصناف — `frmRptItemsSalesDetails` · `frmRptItemsSalesDetailsPOS` · `frmRptItemsProfit` · `frmRptItemsProfitDetails` · `frmRptSalesByCategory` · `frmRptCategorySaleByDay` | ⬜ |
| 3 | 🧾 تقارير الفواتير والإشعارات والحركة اليومية — `frmRptInvSalesDetails` · `frmRptInvSalesDetailsPos(Android)` · `frmRptInvNotfic` · `frmRptInvPurchaseDetails` · `frmRptDailySales` · `frmRptDailyProcess` · `frmRptInvAnalysis` · `FrmRptSalesChart` | ⬜ |
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

## 5. معايير القبول لكل جزء

1. كل تقريرٍ منقول يُسمّي ملفه من `Desktop_ERP` (`Form_WPF/frmRpt*.xaml` و
   `Reports/*.repx`) نصّاً في الوثيقة وفي تعليق تعريفه.
2. الأعمدة والفلاتر مأخوذة من تلك الملفات بالعربية، ونسبة المطابقة ≥ 90٪، وكل تسمية
   مخترَعة مبرَّرة في ملخّص الجزء.
3. نقطة نهاية حقيقية (لا شاشة بلا خدمة)، وعزل مؤسسات، وإذن `reporting.view`.
4. اختبارات API جديدة + سكربت تحقّق حيّ قابل لإعادة التشغيل غير مُتلف.
5. الشاشة تصل من طريقٍ حقيقي في الشجرة (`apps/staff/lib/navigation.ts`).
6. تحديث هذه الوثيقة و`docs/STATUS.md` و`docs/desktop-parity/README.md`.

## 6. مؤجَّل عن قصد

- 🖨️ `SettingPrint` (رأس · تذييل · ختم · عدد النسخ · الطابعة الافتراضية) — جزءٌ سابع.
- صور الرأس والتذييل والختم (`HeaderImage` · `FooterImage` · `StampImage`).
- التقارير التي ترسم بيانياً (`FrmRptSalesChart`) حتى يُبتَ في مكتبة الرسوم.
- تصدير PDF من الخادم (الطباعة تمرّ بطابعة المتصفّح اليوم).
- تقارير أُعيد بناؤها في مراحل سابقة بعناوين من اختيار السحابة (`sales-invoices` ·
  `net-sales` · …) — تُطابق مع الديسكتوب في الجزء الثالث.
