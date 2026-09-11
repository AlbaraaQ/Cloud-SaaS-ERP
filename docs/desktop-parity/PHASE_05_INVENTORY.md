# Phase 05 — المخزون: المستندات، التتبع، والتشغيل

> **الحالة:** قيد التنفيذ — أُنجزت slice مستندات المخزون وبطاقة الصنف المتقدمة، لكن ذلك لا
> يجعل المرحلة كاملة أو التصميم البصري للنظام كاملاً.
>
> **آخر تحقق:** 2026-09-11 — `@erp/contracts` و`@erp/database` و`@erp/testing` و`@erp/api`
> builds نجحت؛ `@erp/api lint` نجح؛ اختبار catalog المتكامل نجح (**7 tests**)؛ suite الـAPI
> الكاملة نجحت (**79 files / 440 tests**)؛ و`@erp/staff lint` و`@erp/staff build` نجحا.

## 1. مرجع الديسكتوب

هذه المرحلة تستلهم قواعد سير العمل من الملفات الموجودة فعلياً في:

- `Desktop_ERP/SmartAuditERP/Form_WPF/frmInputs.xaml(.cs)`؛
- `Desktop_ERP/SmartAuditERP/Form_WPF/frmInvInputOutput.xaml(.cs)`؛
- `Desktop_ERP/SmartAuditERP/Form_WPF/frmInventoryTransfer.xaml(.cs)`؛
- `Desktop_ERP/SmartAuditERP/Form_WPF/frmItems.xaml(.cs)` و`frmEditItems.xaml(.cs)` و`frmItemUnits.xaml(.cs)`؛
- `Desktop_ERP/SmartAuditERP/Form_WPF/frmItemsBarcode.xaml(.cs)` و`frmItemsLimit.xaml(.cs)`؛
- `Desktop_ERP/SmartAuditERP/Form_WPF/frmItemSerialNo.xaml(.cs)`، `frmSerial.xaml(.cs)`،
  و`frmItemsExpire.xaml(.cs)`؛
- `Desktop_ERP/SmartAuditERP/Reports/` عند استكمال مجموعة تقارير المخزون المطابقة.

النقل يلتزم بالمنطق: مستند محفوظ أولاً، ثم أثر مخزني ومحاسبي ذري عند الترحيل، ثم عكس
موثق بدلاً من حذف الأثر. لا ينسخ WPF أو SQL Server أو state العام الخاص بالديسكتوب.

## 2. ما تم تسليمه في هذه الـslice

### 2.1 مستندات المخزون الحقيقية

أضيف `InventoryDocumentsService` و`InventoryDocumentsController` في
`apps/api/src/modules/inventory/` لمستندات:

| النوع | البادئة | المسار في staff | الأثر عند الترحيل |
|---|---:|---|---|
| رصيد أول المدة | `OS` | `/inventory/opening-stock` | إضافة مخزون + قيد افتتاحي |
| استلام بضاعة | `GR` | `/inventory/receipts` | إضافة مخزون + قيد مقابل |
| صرف بضاعة | `GI` | `/inventory/issues` | إخراج بسعر المتوسط + قيد مقابل |
| تسوية مخزنية | `SA` | `/inventory/adjustments` | أسطر إضافة/صرف مختلطة + قيد متوازن |

العقد الحالي:

- `GET /inventory/documents?kind=&status=&warehouse_id=` — السجل مع الفلاتر؛
- `GET /inventory/documents/:id` — المستند وأسطره؛
- `POST /inventory/documents` — حفظ مسودة؛
- `PATCH /inventory/documents/:id` — تعديل **مسودة فقط**؛
- `POST /inventory/documents/:id/post` — الترحيل؛
- `POST /inventory/documents/:id/cancel` — إلغاء المسودة؛
- `POST /inventory/documents/:id/void` — عكس مستند مرحّل، مع سبب إلزامي.

كل read محمي بـ`inventory.view`، وتعديل المسودة/الإلغاء بـ`inventory.adjust`، والترحيل
والعكس بـ`inventory.adjust.approve`. جميع المراجع (فرع، مستودع، صنف، وحدة، حساب مقابل)
تتحقق داخل `withTenantTx`، ولا يعتمد الخادم على الكمية الأساسية أو التكلفة المحولة القادمة
من المتصفح.

`PATCH` لا يغير `kind` أو الرقم المخصص للمستند. يعيد الخادم resolve لوحدة المادة ونسبة
تحويلها ويخزن `baseQuantity` بنفسه. لذلك يبقى مستند الكرتون قابلاً للتدقيق حتى لو كان
المحرر قد أرسل كمية بوحدة بديلة.

### 2.2 الذرية والعكس وحماية القيد

- ترحيل المسودة يسجل حركات ledger ثم قيداً متوازناً ويغير الحالة داخل معاملة واحدة؛ ففشل
  الرصيد أو الفترة أو الحساب يترك المستند `draft` بلا أثر جزئي.
- قيمة الصرف لا تؤخذ من حقل الواجهة: `InventoryService` يستخدم متوسط التكلفة المتحرك.
- العكس يولد حركات معكوسة وقيد `kind: reversal` مرتبطاً بـ`reversalOf`، ثم يوسم القيد
  الأصلي `void` والمستند `voided`.
- `0038_journal_void_immutability.sql` يصلح trigger قديم كان يعيد `OLD` ويجعل تحويل
  `posted → void` صامتاً بلا أثر. يسمح الإصلاح فقط بتحويل الحالة مع حقول audit/version؛
  لا يسمح في العملية نفسها بتغيير التاريخ أو المصدر أو الحسابات أو المبالغ.
- أصبح مسار down/up لـ`0038` مختبراً على PostgreSQL disposable في
  `journal-void-immutability.spec.ts`: يثبت السلوك القديم عند down، ثم نجاح التحويل
  الصحيح بعد up، ثم رفض `status=void` مع تعديل تاريخ القيد بـSQLSTATE `42501`.

### 2.3 واجهة موظفين عربية RTL

`apps/staff/components/inventory-document-workspace.tsx` هو workspace موحد وليس نموذج
CRUD منفرداً. الصفحات الأربع تغلفه حسب نوع المستند، مع عنوان وسياق مختلفين. وهو يوفر:

- سجل قابل للبحث والفلاتر حسب الحالة/الفرع/المستودع؛
- بطاقة تفاصيل وحالة مرئية وخطوات سير العمل؛
- محرر مسودة بأسطر قابلة للإضافة والحذف؛
- اختيار صنف، وحدة بديلة، كمية، تكلفة، lot وserials، واتجاه مستقل لكل سطر تسوية؛
- مسح barcode أو SKU أو alternative code لإضافة السطر؛
- حفظ/تعديل مسودة، ترحيل، إلغاء، وعكس بسبب واضح؛
- حالات loading / empty / error / forbidden حقيقية، وتعطيل الأزرار وفق RBAC؛
- تصحيح navigation: `opening-stock` مستقل عن `adjustments`، مع redirect متوافق مؤقتاً من
  `/s/inventory/opening-stock`.

### 2.4 طباعة المستند

أضيفت صفحة A4 RTL ذاتية الاكتفاء لكل مستند مخزون:

- `GET /reports/print/inventory-documents/:id`، بصلاحية `inventory.view`؛
- `PrintTemplatesService.inventoryDocument()` يطبع رأس المنشأة، النوع والرقم والحالة،
  الفرع والمستودع والحساب المقابل والقيد المرتبط، الأسطر والوحدة والكمية الأساسية،
  lot/serial، والسبب/الملاحظات؛
- عند الترحيل، قيمة الداخل/الخارج وصافي التغير تأتي من **دفتر المخزون غير القابل للتعديل**
  لا من قيمة واجهة المستخدم؛
- workspace يفتح `/print/inventory-document/:id`، وهو viewer مستقل يدعم الطباعة وحفظ نسخة.

هذا print تشغيلي للمستند، وليس بديلاً عن تقارير Phase 10 الكاملة المستلهمة من ملفات
`.repx`.

### 2.5 بطاقة الصنف المتقدمة وربط scanner namespace

تحولت `/inventory/items` من directory/CRUD أولي إلى workspace عربي RTL واحد في
`apps/staff/components/inventory-item-workspace.tsx`، مستلهم من تبويبات
`frmItems` و`frmEditItems` و`frmItemUnits` و`frmItemsBarcode` في الديسكتوب. لا يفتح
تبويب تشغيلي قبل حفظ بيانات الصنف ووحدته الأساسية، حتى لا تنشأ أكواد أو وحدات يتيمة.

- **دليل وبطاقة:** بحث بالاسم العربي أو الإنجليزي وSKU والباركود الأساسي، جدول نتائج
  مختصر، بطاقة جديدة/تحرير/أرشفة مع رسالة تأكيد، وحالات loading/empty/error/forbidden.
- **البيانات الأساسية:** التصنيف، الوحدة الأساسية غير القابلة لإعادة التفسير، النوع، الاسم
  العربي/الإنجليزي، SKU، الباركود الأساسي، الضريبة والسعران الافتراضيان. الحقل الفارغ للسعر
  الاختياري يرسل `null` لمسحه ولا يتحول إلى صفر.
- **المخزون والتشغيل:** حد الطلب الأدنى والأقصى، سقفا الخصم، الظهور في POS، والـweighted
  scale وlot وserial. الواجهة تعكس قيود الخادم على الحدود وعلى ثبات SKU ونسبة الوحدة بعد
  الحركة ولا تتجاوزها.
- **الوحدات:** جدول للوحدة الأساسية والوحدات البديلة، نسبة التحويل، الباركود، البيع/الشراء
  والوحدة الافتراضية للبيع/الشراء. تعديل النسبة أو الحذف بعد حركة مخزنية يبقى خاضعاً لقيد
  API ولا يخفيه العميل.
- **الباركود والأكواد البديلة:** registry فعلي لكل باركود حسب الوحدة، وإدارة رمز مورد/رمز
  قديم مع ملاحظة. endpoints الجديدة هي `GET/POST/DELETE
  /organization/catalog/items/:id/alternative-codes`، ورمز بديل يذهب إلى scanner lookup
  على الوحدة الأساسية. SKU والباركود الأساسي وباركود الوحدة والـregistry والرمز البديل
  مساحة scan-code موحدة لكل tenant؛ لا يمكن أن يتعارض أي منها مع الآخر.
- **RBAC قابل للاستعمال:** بطاقة الصنف تمنع الكتابة من دون `catalog.item.manage`، وتبقى
  endpoints القراءة/الكتابة محمية على الخادم. أضيف `catalog.taxgroup.view` إلى قالب دور
  `inventory_manager` حتى لا تظهر قائمة الضريبة فارغة لهذا الدور أثناء إدارة بطاقة الصنف.

الاختبار `catalog-inventory-integrity.spec.ts` يغطي lifecycle للرمز البديل
`SUPPLIER-05` (upsert idempotent، lookup، تعارض مع SKU، وsoft-delete) ويغطي كذلك
بحث directory بالاسم وSKU والباركود ومسح السعرين الاختياريين للصنف والوحدة.

## 3. migrations والبيانات ذات الصلة

توجد migrations additive متتابعة يجب تطبيقها بالترتيب ثم الاحتفاظ بملفات down المقابلة:

| Migration | الغرض |
|---|---|
| `0034_desktop_parity_followups_and_inventory.sql` | بنية المستندات وامتدادات inventory/POS الأولى |
| `0035_inventory_document_line_traceability.sql` | تثبيت وحدة/lot/serial في سطور البيع والشراء |
| `0036_shift_cash_variance_journal.sql` | دعم قيد فرق الصندوق |
| `0037_catalog_scan_code_integrity.sql` | عدم تعارض أكواد المسح في catalog |
| `0038_journal_void_immutability.sql` | إصلاح immutability للـvoid المحاسبي |

يوجد الآن اختبار disposable مستقل `desktop-parity-migrations-roundtrip.spec.ts` يعيد
`0038` ثم `0037`–`0034` بالترتيب العكسي، ويتأكد من اختفاء الكيانات/الأعمدة الإضافية فقط ثم
إعادة تطبيق `0034`–`0038`، وبقاء tenant/branch/warehouse السابقين، وإمكان كتابة barcode
registry ومستند inventory draft بعد الإعادة. لا يعني ذلك أن rollback لبيانات production
الممتلئة بمستندات جديدة آمن بلا تحضير: down لـ`0034` يسقط جداول المستندات عمداً، لذا تُزال
المستندات التابعة أو تؤرشف خطة rollback قبل تشغيله على بيانات حقيقية.

## 4. الاختبارات المثبتة

- `apps/api/test/inventory-documents.spec.ts` (**7 tests**):
  - opening carton وتحويله إلى base quantity؛
  - تعديل مسودة receipt بوحدة carton مع ثبات الرقم ورفض تغيير النوع؛
  - void وعكس مخزون وقيد؛
  - issue بمتوسط التكلفة ورفض الرصيد غير الكافي دون تلويث المسودة؛
  - mixed adjustment واتجاهي in/out ومنع double post؛
  - طباعة مستند مرحّل بقيمة ledger؛
  - tenant isolation وRBAC.
- `apps/api/test/journal-void-immutability.spec.ts` (**1 test**): down/up trigger وحماية
  الحقول المحاسبية أثناء void.
- `apps/api/test/desktop-parity-migrations-roundtrip.spec.ts` (**1 test**): down ثم up
  لـ`0034`–`0037` (ومعها `0038` لأنها أعلى stack)، فحص الجداول/الأعمدة/الفهارس/RLS، بقاء
  بيانات tenant المرجعية، وإعادة كتابة barcode registry ومستند draft بعد الإعادة.
- `apps/api/test/catalog-inventory-integrity.spec.ts` (**7 tests**) يغطي barcode والوحدات
  والتتبع، رمز المورد/الرمز القديم البديل، scanner lookup، تعارض namespace مع SKU،
  soft-delete، بحث دليل الأصناف بالاسم/SKU/الباركود، ومسح سعر الصنف أو الوحدة بـ`null`.
- الاختبارات الكاملة للـAPI الآن: **79 ملفاً، 440 اختباراً ناجحاً**.

## 5. ما لم يكتمل — لا تضع Phase 05 كـdone

1. **امتدادات بطاقة الصنف المؤجلة:** الصورة وخصائص القياس/التعبئة والمكوّنات المركبة
   وسجل أسعار أكثر تفصيلاً موجودة كأفكار أو حقول/شاشات في الديسكتوب، لكنها ليست contract
   كاملاً في هذه الـslice؛ لا تحاكِها محلياً قبل تصميم API وworkflow واختبار واضحين.
2. **المناقلات والطلبات والتوصيل والإنتاج:** المحركات والمسارات موجودة، لكن يلزم تدقيق
   واجهاتها كسير عمل مكتبي كامل (partial receive، تسلسل الحالات، وحدات/serials) واختبارها.
3. **تقارير تشغيلية:** أرصدة، حركة وبطاقة صنف، حد إعادة الطلب، انتهاء صلاحية وسيريالات
   تحتاج فلاتر/drill-down وطباعة/export مخصصة؛ لا تكفي الجداول الحالية.
4. **rollback production:** اختبار round trip يغطي بنية `0034`–`0037` وبيانات أساس
   قبل/بعد migration، لكن لا يُشغّل down لـ`0034` على production فيه مستندات جديدة من دون
   خطة أرشفة/نسخة احتياطية واختبار بيانات مماثلة.
5. **تحقق browser/runtime:** component بني ومر lint، لكن يبقى اختبار يدوي مع حساب حقيقي
   وبيانات lot/serial متعددة في preview قبل قبول UX النهائي.
6. **إعادة التصميم الشاملة:** نجاح هذه الـslice لا يجعل بقية `apps/staff` أو
   `apps/platform-admin` إعادة تصميم مكتملة.

## 6. الخطوة التالية الآمنة

بطاقة الصنف المتقدمة أصبحت slice مكتملة مبدئياً من `frmItems` و`frmEditItems` و
`frmItemUnits`: API موجود + tabs حقيقية + RBAC + tests + navigation. اختبار round-trip
لـ`0034`–`0037` موجود الآن. الخطوة الآمنة التالية هي تدقيق workflow
المناقلات/الطلبات/التوصيل/الإنتاج من مراجع WPF قبل فتح شاشات CRUD جديدة، مع إبقاء اختبار
browser/runtime وPOS/treasury المتبقيين ضمن التحصين. لا تُحذف سياسة `journalLines` اليدوية
من API المبيعات قبل فصل DTO العام عن caller الداخلي في `projects.service.ts` الذي يحتاجها
لـprogress bills.
