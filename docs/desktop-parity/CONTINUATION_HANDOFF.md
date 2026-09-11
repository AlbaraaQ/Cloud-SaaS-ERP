# سجل الاستمرارية وخطة التنفيذ — Cloud SaaS ERP

> **الغرض من هذا الملف:** هذه هي نقطة البدء الإلزامية لأي محادثة أو جلسة تنفيذ جديدة.
> يشرح ما الذي يبنيه المشروع، ما الذي تم إنجازه فعلاً، ما الذي ما زال قيد العمل، وأول خطوات آمنة للاستئناف دون فقدان سياق أو حذف عمل غير مُرحّل.
>
> **آخر تحديث:** 2026-09-11
> **الفرع المعتمد للجلسة:** `arena/01a08d86-cloud-saas-erp`
> **الحالة العامة:** نتابع ما بعد المراحل 01–04، مع معالجة الفجوات الحرجة أولاً ثم تنفيذ المرحلة 05 (المخزون) بصورة مكتملة وظيفياً وبصرياً.

---

## 1) الفكرة والنتيجة المطلوبة

المشروع ليس تحويل صفحات CRUD أو إضافة API فقط. الهدف هو إعادة بناء نظام
`Desktop_ERP/SmartAuditERP` المكتبي (WPF + SQL Server) كنظام ERP SaaS حديث:

- **واجهة موظفين عربية RTL احترافية** مستلهمة من سير عمل الديسكتوب أو أفضل منه، وليست لوحة إدارية أولية؛
- **API محاسبي ومخزني موثوق** مبني على NestJS/Postgres، حيث يترتب على ترحيل المستند قيد يومية وحركة مخزون صحيحة داخل معاملة واحدة؛
- **عزل متعدد المنشآت** (`tenant`) على مستوى الخدمة والـRLS، مع RBAC فعلي لكل عملية وزر؛
- **استمرارية محاسبية**: دليل الحسابات والبيانات المرجعية للديسكتوب تُزرع لكل منشأة جديدة؛
- **شاشات وتقارير قابلة للتشغيل والطباعة** باللغة العربية، مع بحث وفلاتر وحالات مستندات وإجراءات واضحة ورسائل أخطاء مفهومة؛
- **لوحة منصة SaaS** في `apps/platform-admin` تتحكم في المنشآت والاشتراكات والوحدات المفعّلة لكل منشأة.

الديسكتوب هو **المواصفة الوظيفية** لا القالب المعماري: تُنقل القواعد، الحالات، الصلاحيات، وتسلسل العمل من `Form_WPF` و`Class`، لكن لا تُنسخ بنية `SqlClient` أو `MessageBox` أو الـstatic globals إلى السحابة.

---

## 2) معيار قبول لا يجوز تجاوزه

لا تُوسم أي مرحلة بأنها مكتملة لمجرد أن TypeScript يمر أو أن endpoint أو صفحة موجودة. لا تصبح الوظيفة «منجزة» إلا عند اكتمال البنود التالية معاً:

1. **قاعدة الديسكتوب:** تم الرجوع إلى شاشة/كلاس الديسكتوب المعني وتوثيق السلوك الذي نُقل.
2. **منطق تشغيلي ومحاسبي صحيح:** حالات المستند، القيود، الترقيم، التكلفة، العكس/الإلغاء، وعدم تكرار الأثر المالي أو المخزني.
3. **معاملة ذرية:** لا يُحفظ مستند مُرحّل دون حركته المخزنية وقيده، ولا العكس.
4. **أمان وعزل:** تحقق tenant-scoped في كل مرجع، RLS حيث يلزم، وصلاحية RBAC مطلوبة في الـAPI والواجهة.
5. **واجهة SaaS عربية حقيقية:** شاشة RTL متقدمة فيها قائمة/بحث/فلاتر/بطاقة أو محرر مستند/حالات/إجراءات مسودة–ترحيل–إلغاء–عكس–طباعة حسب المجال، وليست نموذجاً بسيطاً أو mock.
6. **تنقل صحيح:** كل مدخل `ready` يقود إلى الشاشة الصحيحة؛ لا يجوز أن تفتح «بضاعة أول المدة» شاشة «تسوية مخزنية» أو صفحة غير ذات صلة.
7. **اختبارات وتوثيق:** اختبارات وحدة وتكامل وtenant/RBAC وحالات الفشل المهمة، ثم تحديث وثيقة المرحلة وسجل الاستمرارية.

لذلك عبارة «تمت المراحل 01–04» تعني أن مسارات المحركات والوظائف الأساسية الموثقة فيها نُفذت في المحادثة السابقة؛ **ولا تعني** أن إعادة التصميم البصري الشامل أو جميع التحسينات المؤجلة لتلك المراحل قد اكتملت.

---

## 3) مصادر الحقيقة التي يجب قراءتها قبل تعديل أي مجال

| الأولوية | المسار | الاستخدام |
|---|---|---|
| 1 | `docs/desktop-parity/CONTINUATION_HANDOFF.md` | هذا الملف: الحالة الحية، الأولوية، وخطوات الاستئناف. |
| 2 | `docs/desktop-parity/README.md` | فهرس برنامج التماثل وحالة المراحل. |
| 3 | `docs/desktop-parity/PHASE_00_SURVEY.md` | خريطة الديسكتوب، مصفوفة أنواع الفواتير، وأهم classes. |
| 4 | `docs/desktop-parity/PHASE_01_USERS_NAV.md` إلى `PHASE_04_POS_SHIFTS.md` | ما نُقل في المراحل السابقة، الاختبارات، والمتابعات المؤجلة. |
| 5 | `docs/desktop-parity/ROADMAP_PHASES_02_11.md` | نطاق المراحل 05–11 ومراجع الديسكتوب لكل منها. |
| 6 | `Desktop_ERP/SmartAuditERP/Form_WPF/` و`Class/` | المرجع الفعلي للـUX وقواعد العمل والحالات. اقرأ `.xaml` و`.xaml.cs` والكلاس قبل تصميم/تغيير أي شاشة. |
| 7 | `Desktop_ERP/SmartAuditERP/Reports/` | مرجع تقارير DevExpress (`.repx`) عند تنفيذ التقارير والطباعة. |
| 8 | `Desktop_ERP/CrystalLiteDB.txt` و`Desktop_ERP/AlterDb.txt` | مصدر البيانات المرجعية والدليل المحاسبي الافتراضي؛ لا تُخترع بدائل قبل فحصهما. |
| 9 | `packages/database/src/schema/` و`packages/database/migrations/` | العقد الفعلي للبيانات وmigrations الحالية. |

### قواعد ثابتة

- حافظ على توافق الـAPI: أضف حقولاً ومسارات بشكل additive، ولا تغيّر مساراً قائماً أو معناه دون ترحيل وتوثيق.
- لا تتجاوز `withTenantTx` أو تحقق الملكية/المنشأة في أي service.
- لا تثق بقيم حسابات أو مخازن أو أصناف يرسلها المتصفح دون التحقق داخل المعاملة.
- جميع العروض العربية يجب أن تكون RTL، وتستخدم حالات loading / empty / error / forbidden حقيقية.
- لا تعيد الطلب من المستخدم رفع `.xaml.cs` أو business classes؛ الملفات موجودة داخل المستودع.
- لا تلمس أو تعِد تصميم `apps/marketing` حالياً؛ هذا مؤجل بقرار المالك.
- لا تُنشأ وحدة مستقلة لمبيعات الجوالات والإكسسوارات في الوقت الحالي.

---

## 4) ما تم إنجازه في خط الأساس (المراحل 00–04)

### المرحلة 00 — المسح والتحليل

- فُهرست بنية الديسكتوب: business logic، شاشات WPF، code-behind، وتقارير `.repx`.
- وُثقت مصفوفة `invType × ProcType` ومراجع المحركات الأساسية مثل `InvoiceOper`, `ItemOper`, `EntryOper`, و`ReceiptOper`.
- التفاصيل: `PHASE_00_SURVEY.md`.

### المرحلة 01 — المستخدمون والصلاحيات والتنقل

- شاشات المستخدمين والأدوار ومصفوفة الصلاحيات في staff مرتبطة بالـAPI الحقيقي.
- تم تدقيق navigation وإزالة التكرار والمسارات الخاطئة/الميتة بحسب الوثيقة.
- التفاصيل: `PHASE_01_USERS_NAV.md`.
- **يبقى:** تدقيق بصري وتشغيلي للشاشات، والتأكد المستمر أن جميع روابط `ready` تقود إلى واجهة مخصصة صحيحة لا مجرد scaffold.

### المرحلة 02 — محرك المبيعات

- توحيد حساب الفاتورة: خصم السطر ثم خصم الرأس ثم VAT، مع `calculateInvoiceTotals` مشترك.
- ترحيل المبيعات/المرتجع/الإشعارات يولّد المخزون والتكلفة والقيد تلقائياً داخل معاملة واحدة، مع تسوية آجل/نقد/بنك.
- الحماية من إلغاء مبيع مدفوع أو فاتورة مختومة، وعكس القيد والحركة عند الإلغاء ضمن القواعد الموثقة.
- التفاصيل والاختبارات التاريخية: `PHASE_02_SALES_ENGINE.md`.
- تم تحصين بوابة التسوية للعميل النقدي في هذه الجلسة وإبقاء اختبار delivery صريحاً عن تسوية cash؛ **يبقى** تدقيق واجهات sales كوحدات إدخال احترافية لا CRUD.

### المرحلة 03 — محرك المشتريات

- ترحيل الشراء/المردود وتكلفة الهبوط والمتوسط المتحرك وقيد الشراء تلقائياً؛ مع تسويات المورد والتعامل مع الخدمات والأصناف المخزنية.
- عكس القيد وحركات المخزون عند الإلغاء وفق قيود الحالة.
- التفاصيل: `PHASE_03_PURCHASE_ENGINE.md`.
- **يبقى:** واجهات مستندات مشتريات متقدمة وتحقق تكاملي جديد عند إدخال وحدة/دفعة/سيريال في المرحلة 05.

### المرحلة 04 — نقطة البيع والورديات

- `POST /pos/checkout` يجعل البيع النقدي عملية ذرية (فاتورة + مخزون + قيد + دفعة).
- دعم طرق الدفع، الباقي، ربط الفاتورة بالوردية، وإقفال الوردية مع الملخص ومنع تعديل مبيعات وردية مقفلة.
- التفاصيل: `PHASE_04_POS_SHIFTS.md`.
- **متابعات لا تزال نشطة:** hold/recall، shortcut items، multi-tender، صلاحية override السعر، ومراجعة الربط المحاسبي لفروقات الصندوق. توجد تعديلات أولية غير مُتحقق منها لهذه النقاط في شجرة العمل الحالية؛ لا تُعلنها مكتملة قبل الاختبارات والواجهة.

### بيانات المنشأة الجديدة

- نُفذت مسارات زرع دليل حسابات الديسكتوب والبيانات المرجعية للمنشآت الجديدة، مع أداة للمنشآت القديمة الخالية:
  - `scripts/desktop-seed/extract-coa.mjs`
  - `packages/database/src/desktop-coa.ts`
  - `packages/database/src/cli/seed-coa.ts`
  - `apps/api/src/modules/organization/provisioning/org-provisioning.service.ts`
  - الأمر: `pnpm db:seed:coa`
- يجب **التحقق من الملفات والاختبارات الحالية قبل إعادة العمل أو تغييرها**؛ لا يعتمد هذا السجل وحده كبديل عن الكود.

---

## 5) حالة شجرة العمل الآن — مهم قبل أي استئناف

الفرع يحتوي تغييرات غير مُرحّلة من العمل الجاري. **لا تستخدم `git reset --hard`، ولا
تنظف الملفات غير المتتبعة، ولا تفترض شجرة نظيفة.** افحص أولاً `git status --short` و`git
diff`؛ migrations والمستندات الجديدة جزء مقصود من هذه الشجرة.

### 5.1 ما أصبح منجزاً ومتحققاً

| المجال | النتيجة الحالية | الدليل الرئيسي |
|---|---|---|
| تكامل barcode والوحدات | سياسة scan-code موحدة وتحويل server-side إلى base quantity مع اختبارات catalog/inventory. | `catalog-inventory-integrity.spec.ts`، `0037_*` |
| بوابة تسوية المبيعات | لم يعد ترحيل العميل النقدي يتجاوز تحديد وسيلة/حساب التسوية؛ عدّل regression في delivery لاستعمال cash settlement صريحاً. | `sales.service.ts`، `warehouse-documents.spec.ts` |
| مستندات المخزون | opening/receipt/issue/adjustment بنمط draft → post → void/cancel، رقم ثابت، تحقق tenant/RBAC، ledger + journal ذري، وعكس موثق. | `inventory-documents.service.ts`، `inventory-documents.spec.ts` |
| واجهة inventory | workspace عربي RTL موحد للسجل والفلاتر والمحرر والأسطر والوحدات/lots/serials والـbarcode والإجراءات الفعلية؛ routes الأربعة مستقلة. | `inventory-document-workspace.tsx` |
| بطاقة الصنف | `/inventory/items` أصبح directory + card بتبويبات أساسية/تشغيل/وحدات/أكواد، مع إدارة فعلية للوحدات والأسعار والنسب وbarcode registry والأكواد البديلة وPOS/reorder/lot/serial/scale. | `inventory-item-workspace.tsx`، `catalog.service.ts`، `catalog-inventory-integrity.spec.ts` |
| التنقل | `/inventory/opening-stock` لم يعد يشير إلى التسوية، ويوجد redirect مؤقت للـbookmark القديم `/s/inventory/opening-stock`. | `apps/staff/lib/navigation.ts`، `next.config.mjs` |
| الطباعة | A4 RTL لمستند المخزون مع قيمة ledger، ومشاهد print مستقل وحفظ نسخة. | `PrintTemplatesService.inventoryDocument()`، `/print/inventory-document/:id` |
| immutability للقيد | migration `0038` أصلحت trigger `posted → void` واختُبر down ثم up على PostgreSQL disposable، مع رفض تعديل الحقول أثناء void. | `journal-void-immutability.spec.ts` |
| round-trip migrations | أعيدت `0034`–`0037` (ومعها `0038` أعلى stack) down ثم up على PostgreSQL disposable؛ الفحص يثبت اختفاء/عودة structures، بقاء بيانات tenant الأساس، وكتابة barcode registry ومستند draft بعد الإعادة. | `desktop-parity-migrations-roundtrip.spec.ts` |

التفاصيل الدقيقة لهذه الـslice في [`PHASE_05_INVENTORY.md`](./PHASE_05_INVENTORY.md).

### 5.2 آخر تحقق معروف

نفذت النتائج التالية في 2026-09-11 على هذا الفرع:

```text
corepack pnpm --filter @erp/contracts lint   # success
corepack pnpm --filter @erp/contracts build  # success
corepack pnpm --filter @erp/contracts test   # success — 10 files / 71 tests
corepack pnpm --filter @erp/database build   # success
corepack pnpm --filter @erp/testing build    # success
corepack pnpm --filter @erp/api lint         # success
corepack pnpm --filter @erp/api build        # success
corepack pnpm --filter @erp/api test         # success — 79 files / 440 tests
node apps/api/dist/openapi/export-openapi-cli.js  # success — packages/contracts/openapi.json refreshed
corepack pnpm --filter @erp/staff lint       # success
corepack pnpm --filter @erp/staff build      # success

git diff --check                             # success
```

رسائل PostgreSQL التي تظهر أثناء suite الـAPI عن duplicate/RLS/immutability هي assertions
سلبية متوقعة في الاختبارات، وليست فشلاً. ما زال تحذير Next غير الحاجب عن عدم اكتشاف ESLint
plugin وتحذير `boundaries` الخاص بأنماط descriptors موجودين؛ لا يمنعان build أو الاختبارات.

### 5.3 حدود صادقة باقية

- الـmigrations `0034` إلى `0038` وملفات down الآن مرّت باختبار round-trip disposable:
  يعيد الاختبار `0034`–`0037` ومعها `0038` بسبب تسلسل runner. لا تدّع مع ذلك أن rollback
  production مكتمل أو غير فاقد للبيانات؛ down لـ`0034` يسقط مستندات المخزون الجديدة عمداً
  ويتطلب backup/archival plan واختبار بيانات مماثلة.
- بطاقة الصنف `/inventory/items` أصبحت tabs حقيقية ومتحققة، لكن لا تدّع اكتمال امتدادات
  الديسكتوب المؤجلة (الصورة، خصائص القياس/التعبئة، المكوّنات المركبة، وسجل الأسعار الموسع)
  قبل عقد API وworkflow واختبارات لها.
- print لمستندات المخزون مكتمل تشغيلياً، لكن تقارير المخزون المتخصصة ومحرك تقارير `.repx`
  الأوسع من نطاق Phase 10 ما زالت غير مكتملة.
- لا تحذف `journalLines` من API مبيعات عام دون فصل DTO؛ `projects.service.ts` caller داخلي
  يعتمد عليه لـprogress bills.

---

## 6) الأولوية التنفيذية الفورية

الترتيب التالي مُلزم ما لم يطلب المالك خلافه:

### A. التحصين المتبقي قبل التوسع

1. تم اختبار round-trip للمigrations `0034`–`0037` (ومعها `0038` أعلى stack) على قاعدة
   disposable. قبل أي rollback production، أضف/راجع خطة backup وarchival لأن down لـ0034
   يحذف جداول المستندات الجديدة عمداً.
2. راجع regression للـPOS وtreasury المضافين في الشجرة: multi-tender، hold/recall,
   shortcuts، قيد فرق الصندوق، وتاريخ الإقفال؛ لا تعلنها مكتملة قبل tests وواجهة.
3. نفذ اختبار browser/runtime يدوي للـworkspace مع lot وserials متعددة وحساب يملك/لا يملك
   صلاحيات الترحيل، ثم عالج أي UX ظاهر في preview.

### B. slice بطاقة الصنف — منجزة مبدئياً، لا تعاد من الصفر

تمت إعادة بناء `/inventory/items` من `frmItems.xaml(.cs)` و`frmEditItems.xaml(.cs)` و
`frmItemUnits.xaml(.cs)` و`frmItemsBarcode.xaml(.cs)` كـdirectory + card ذي tabs حقيقية:

1. البيانات الأساسية والتصنيف والضريبة والأسعار؛
2. الوحدات البديلة ونسب التحويل وسياسة lock بعد الحركة؛
3. primary/multi barcode والأكواد البديلة، مع visibility للـlookup؛
4. POS وreorder level وtrack lot/serial/scale؛
5. RBAC، loading/error/empty، وحماية أرشفة سليمة؛
6. tests API + staff lint/build + توثيق.

لا تعِد هذه الـslice كـCRUD منفصل. بعد تحصين القسم A انتقل إلى workflows
المناقلات/الطلبات/التوصيل/الإنتاج والتقارير المتخصصة، مع تمديد بطاقة الصنف فقط عندما يوجد
contract/API واختبار لسجل السعر أو الصورة أو الخصائص/المكوّنات.

---

## 7) خريطة شاشات Phase 05 المطلوبة

الحالة هنا صادقة عن مستوى التشغـيل، وليست حكماً على جودة واجهة النظام كاملة.

| عملية الديسكتوب | مرجع WPF رئيسي | وجهة السحابة | الحالة الحالية |
|---|---|---|---|
| بطاقة/دليل الأصناف | `frmItems*`, `frmEditItems*`, `frmItemProperties*` | `/inventory/items` | **slice بطاقة متقدمة منجزة مبدئياً:** directory + tabs أساسية/تشغيل/وحدات/أكواد. تبقى الصورة/خصائص القياس/المكوّنات/سجل السعر الموسع. |
| الوحدات والباركود | `frmItemUnits*`, `frmMultiBarcode*`, `frmItemsBarcode*`, `frmPrintBarcode*` | `/inventory/units`, `/inventory/barcodes`، داخل بطاقة الصنف | APIs وbarcode registry والأكواد البديلة وUI داخل البطاقة متاحة. تبقى label designer/print settings من نطاق التقارير/الطباعة الأوسع. |
| أرصدة/حركة الصنف | `frmItemsBalances*`, `frmItemInvertory*`, `frmRptItemsActivity*` | `/inventory/levels`, `/inventory/movements` | صفحات قائمة؛ تحتاج drill-down وتقارير/print مخصصة. |
| أول المدة | `frmInputs*`, `frmInvInputOutput*` | `/inventory/opening-stock` | **slice تشغيلية جاهزة**: مسودة/ترحيل/عكس/طباعة. |
| إدخال/إخراج | `frmInvInOutput*`, `frmInvInputOutput*` | `/inventory/receipts`, `/inventory/issues` | **slice تشغيلية جاهزة**: مسودة/ترحيل/عكس/طباعة. |
| التسوية والجرد | `frmInvInputOutput*`, `frmRptInventory*` | `/inventory/adjustments` | تسوية mixed in/out جاهزة؛ شاشة الجرد/العد الموجهة ما زالت لاحقة. |
| المناقلات | `frmInventoryTransfer*` | `/inventory/transfers` | route ومحرك مبدئيان؛ يلزم UX كامل وpartial receive/serial tests. |
| الدفعات/الصلاحية | `frmItemsExpire*` | `/inventory/lots` | route موجود؛ lifecycle والتنبيهات والتقرير تحتاج تدقيقاً. |
| السيريالات | `frmItemSerialNo*`, `frmSerial*` | `/inventory/serials` | route موجود؛ lifecycle/report وUX الوثائقي يحتاجان تحققاً يدوياً. |
| الإنتاج | `frmProductionOrder*`, `frmInputs*` | `/inventory/production` | محرك/route موجودان؛ يلزم تدقيق UI والمواد/الناتج والتكلفة. |
| طلب/توصيل المخزون | `frmInvItemsDeliveries*` وما يرتبط بالطلبات | `/inventory/requests`, `/inventory/deliveries` | routes ومحرك موجودان؛ يلزم تدقيق workflow والصلاحيات وواجهة احترافية. |

---

## 8) إعادة التصميم والتدقيق عبر المراحل 01–04

يُستكمل هذا بالتوازي التدريجي مع المراحل الجديدة؛ لا يُعاد الإعلان عن تلك المراحل بصرياً كمكتملة قبل هذه المراجعة:

- **Users & roles:** مصفوفة صلاحيات قابلة للاستخدام، scopes واضحة، حالات منع مفهومة، وواجهة عربية منظمة.
- **Sales:** محرر فاتورة يعكس desktop (عميل، مستودع، أصناف/وحدات، خصم وضريبة، حالة، تسوية، ترحيل/عكس/طباعة)، مع بوابة الآجل الصحيحة.
- **Purchases:** محرر شراء/مرتجع وتكلفة إضافية وحالة استلام/ترحيل واضحة.
- **POS & shifts:** واجهة تشغيل سريعة للمس، بحث/باركود، وردية، hold/recall، shortcuts، multi-tender، وإقفال قابل للطباعة.
- **Reports:** لا تعتبر صفحات report generic بديلاً عن تقارير الديسكتوب؛ Phase 10 يبني محرك التقرير والطباعة باستخدام `Reports/*.repx` مرجعاً.

---

## 9) المراحل اللاحقة بعد Phase 05

| المرحلة | النطاق | الأولوية بعد المخزون |
|---|---|---|
| 06 | الخزينة: سندات قبض/صرف، الصناديق والبنوك، تحويلات، شيكات، تسويات | التالية |
| 07 | المحاسبة: القيود اليدوية والافتتاحية، الفترات، الأستاذ والميزان، مراكز التكلفة والضريبة | التالية |
| 08 | الموارد البشرية والرواتب | لاحقة |
| 09 | الوحدات الرأسية: مقاولات/مشاريع، مراسي/تأجير، بصريات، خياطة، تكاملات متجر | لاحقة |
| 10 | التقارير والطباعة من 95 تقرير `.repx` | لاحقة؛ يُنفذ على دفعات بحسب المجالات |
| 11 | ZATCA / ETA / التكاملات والتحصين النهائي | لاحقة |

`apps/platform-admin` يظل مسار عمل مطلوباً: تطوير لوحة منصة متقدمة وتمكين/تعطيل الوحدات لكل منشأة. أما `apps/marketing` فمؤجل، ولا يزال ضمن الخارطة المستقبلية.

---

## 10) طريقة الاستئناف العملية في محادثة جديدة

نفّذ بالترتيب، ولا تبدأ شاشة/endpoint جديداً قبل الخطوة 6:

```bash
cd /home/user/Cloud-SaaS-ERP
git branch --show-current
git status --short
git diff --check
git diff --stat
```

1. تأكد أن الفرع هو `arena/01a08d86-cloud-saas-erp` وأن تغييرات شجرة العمل ظاهرة؛ لا تبدّل الفرع ولا تنظف العمل.
2. اقرأ هذا الملف، ثم `README.md` و`PHASE_00_SURVEY.md` والوثيقة الخاصة بالمجال المستهدف.
3. افحص migrations غير المتتبعة `0034`–`0038` وملفات down قبل تعديل schema أو توليد migration جديد. اختبار `desktop-parity-migrations-roundtrip.spec.ts` مرّ بالـdown/up للـ0034–0038 على قاعدة disposable، لكنه ليس بديلاً عن خطة rollback للـproduction الممتلئ بمستندات جديدة.
4. اقرأ مرجع الديسكتوب الخاص بالعملية قبل كتابة المنطق أو الواجهة. بطاقة الصنف موجودة الآن؛ لأي امتداد لها ابدأ بـ`frmItems*` و`frmEditItems*` و`frmItemUnits*` و`frmItemsBarcode*`. للمناقلات/المستندات استخدم `frmInventoryTransfer*` و`frmInvInputOutput*` و`frmInputs*`.
5. نفّذ التحقق أولاً (حسب البيئة المتاحة):

```bash
corepack pnpm --filter @erp/contracts build
corepack pnpm --filter @erp/database build
corepack pnpm --filter @erp/api build
corepack pnpm --filter @erp/api test
corepack pnpm --filter @erp/staff build
```

6. ابدأ بالفجوات الموثقة في القسم 6-A (خطة rollback production وPOS/treasury runtime)، ثم حدّث هذا الملف بحالة التحقق الفعلية.
7. بعد ذلك نفّذ slice واحدة مكتملة من Phase 05: بطاقة الصنف المتقدمة موجودة بالفعل؛ الأولوية الآن لـworkflows المناقلات/الطلبات/التوصيل/الإنتاج، بدلاً من فتح عدة شاشات CRUD ناقصة.
8. عند نهاية كل جلسة: حدّث أقسام «حالة شجرة العمل»، «آخر تحقق معروف»، و«الأولوية الفورية»، ثم حدّث `PHASE_05_INVENTORY.md` و`README.md` بحالة صادقة.

### أوامر مفيدة

```bash
# قواعد محلية عند الحاجة
pnpm db:up
pnpm db:migrate

# تحقق دليل الحسابات لمنشأة قديمة فارغة
pnpm db:seed:coa

# فحص جميع الاختبارات قبل التسليم النهائي (قد يكون طويلاً)
pnpm verify
```

إذا فشل بناء أو migration، لا تغيّر نطاق المرحلة لتجاوز الفشل: وثّق الخطأ، أصلح سببه، وأعد التحقق. ولا تضع بيانات DB أو مفاتيح سرية داخل التوثيق أو Git.

---

## 11) قائمة تسليم Slice/Phase قبل وضع علامة الإنجاز

- [ ] مرجع desktop محدد في وثيقة المرحلة.
- [ ] schema/migration additive ومجربة up/down عند الحاجة.
- [ ] API محمي بصلاحية دقيقة ومعزول tenant/RLS.
- [ ] تحقق جميع المراجع tenant-scoped داخل transaction.
- [ ] حالات document واضحة ومحمية (draft/post/cancel/void/reverse حسب المجال).
- [ ] الحركة المخزنية والقيد متوازنان وذريان، مع idempotency أو حماية التكرار المناسبة.
- [ ] اختبارات success + validation + tenant violation + RBAC + regression + reversal.
- [ ] staff UI عربي RTL بمسار navigation صحيح، وحالات loading/error/empty/forbidden، وأزرار الإجراءات الحقيقية.
- [ ] print/report أو رابط واضح لما يؤجل إلى Phase 10، من دون ادعاء اكتمال غير موجود.
- [ ] تحديث `CONTINUATION_HANDOFF.md` ووثيقة المرحلة و`README.md` بحالة صادقة.

---

## 12) ملخص قصير جداً للمحادثة التالية

> استأنف من الفرع `arena/01a08d86-cloud-saas-erp` مع الاحتفاظ بكل التغييرات غير المُرحّلة. تم التحقق من builds وlint وsuite API كاملة (79/440) في 2026-09-11. Phase 05 ليست مكتملة، لكن slice مستندات أول المدة/الاستلام/الصرف/التسوية أصبحت تشغيلية: draft/edit/post/cancel/void، ledger+journal ذري، barcode/unit/lot/serial، routes صحيحة، وطباعة A4. وبطاقة `/inventory/items` أصبحت directory + tabs متقدمة (أساسية/تشغيل/وحدات/أكواد) مع API حقيقية للوحدات والباركود والرموز البديلة وPOS/reorder؛ مرجعها `frmItems` و`frmEditItems` و`frmItemUnits`. اختبار migration 0038 مر down/up ويثبت immutability، كما مر round-trip للـ0034–0037 (ومعها 0038 أعلى stack) ويثبت structures وبيانات الأساس وكتابة fixtures بعد الإعادة. ابدأ بمراجعة خطة rollback production وPOS/treasury المتبقيين، ثم workflows المناقلات/الطلبات/التوصيل/الإنتاج. لا تدّع اكتمال إعادة التصميم الشاملة أو تقارير المخزون أو platform-admin قبل تنفيذها.
