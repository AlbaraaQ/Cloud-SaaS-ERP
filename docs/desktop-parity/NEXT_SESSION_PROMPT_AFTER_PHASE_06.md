# نص جاهز للمحادثة القادمة — بعد الجزء السابع من المرحلة 06 (الخزينة)

انسخ هذا الملف كاملاً وأرسله في المحادثة الجديدة. (ملف للنسخ فقط — ليس توثيقاً رسمياً.)

---

المرحلة 05 (المخزون) **مغلقة** بأجزائها السبعة، والمرحلة 06 (الخزينة) أُنجز منها **سبعة
أجزاء**: الستة الأصلية ثم **📒 قيد الإغلاق** (المؤجَّل عن قصد من الجزأين 4 و5): **سند القبض وسند الصرف بوثيقة كاملة + القيد** (§1–§8)، و**تعريف الخزن
والبنوك** (§9)، و**حركة الصندوق** (§10)، و**إغلاقات اليومية** (§11)، و**التحويل البنكي
والعميل النقدي** (§12)، و**مناقلة الخزن** (§13) و**قيد الإغلاق** (§14). راجع
`docs/desktop-parity/PHASE_06_TREASURY.md` قبل أي سطر كود.

## الخيار الأول (الموصى به): المرحلة 07 — المحاسبة

حسب برنامج العمل: المحاسبة ثم الموارد البشرية ثم القطاعات الرأسية ثم التقارير ثم
الزكاة/التكاملات. `docs/desktop-parity/README.md` صفّها: قيود يدوية، فترات، ميزان
المراجعة، مراكز التكلفة. تذكر أن كثيراً من أساسها جاهز (`journal_entries`،
`fiscal_periods`، `branch_posting_profiles`) وأن الخزينة كلها أصبحت تُرحّل قيودها من
محرّك واحد.

## الخيار الثاني: تقارير الخزينة المطبوعة

`Reports/rptCloseShift.repx` و`rptCloseday.repx` و`rptClosedayCust.repx` و
`RptKhzna.repx` و`rptSafeTransfer.repx` تنتظر مرحلة التقارير؛ `printShiftData` يهيّئ
بياناتها فقط.

## الخيار الثالث: عهدة الإغلاق والإيداع (مؤجَّل عن قصد من §14)

`1211002` عهدة الإغلاق لم يُنسخ لأنه يحتاج خطوة إيداع تُقفله. حين توجد شاشة الإيداع
يُضاف سطر العهدة إلى قيد الإغلاق معها.

---

---

## 1) أين نقف (حقائق مقيسة عند كتابة هذا النص)

- الفرع الإلزامي: `arena/01a0889e-cloud-saas-erp`.
- `apps/api`: **89 ملفاً / 537 اختباراً** خضراء (منها `treasury-bank-transfer.spec.ts`
  بـ8، و`sales-cash-customer.spec.ts` بـ7، و`treasury-transfers.spec.ts` بـ9،
  و`treasury-shift-entry.spec.ts` بـ11). `apps/staff`: **36/36**.
  `@erp/contracts`: 71/71. و`pnpm -r run lint` **أخضر بالكامل**.
- `node scripts/verify-treasury.mjs`: **12 قسماً** كلها ✓ وينتهي بـ
  «✔ Phase 06 treasury documents verified».
- `node scripts/verify-inventory.mjs`: **15 قسماً** كلها ✓.
- **45 ترحيلاً** مطبقاً آخرها `0044_shift_close_number_scope` (الجزءان الخامس والسادس
  بلا ترحيل، قصداً: `voided` موجود في `0011_treasury.sql` بلفظه).
- شاشات الخزينة الحيّة: `/treasury/vouchers` · `/treasury/cash-locations` ·
  `/treasury/banks` · `/treasury/movements` · `/treasury/day-close` ·
  `/treasury/transfers`، ومعها `/inventory/transfers` (مناقلة الأصناف) و
  `/sales/cash-customers` ونافذتا `🏦 اختر البنك` و`👤 عميل نقدي` في `/sales/pos`
  و`/treasury/vouchers`.

## 2) إعادة بناء البيئة إن كانت جديدة (تعرّضنا لها **مرّتين** فعلاً)

البيئة المحلية **لا تُحفظ**: حزم `node_modules` وقاعدة البيانات تختفي مع إعادة إنشاء
الصندوق، ويعود الفرع محليّاً إلى `d03d14d` (أساس الفرع) فتظهر مئات الملفات «معدَّلة».
لإصلاح ذلك بالترتيب:

```bash
# 1) أعِد الفرع إلى ما على GitHub (لا تلمس الملفات: reset --mixed)
git fetch origin arena/01a0889e-cloud-saas-erp && git reset --mixed FETCH_HEAD
git checkout -- Desktop_ERP/SmartAuditERP/Class/DgvAccount2.cs \
                Desktop_ERP/SmartAuditERP/Class/FormPermission.cs \
                Desktop_ERP/SmartAuditERP/Class/RestrictionData.cs \
                Desktop_ERP/SmartAuditERP/Form_WPF/frmSalePurch.xaml.cs   # ضجيج CRLF

# 2) الحزم والبيئة
corepack pnpm install --no-frozen-lockfile     # --frozen-lockfile يفشل: apps/admin و apps/customer غير متتبَّعين
git checkout -- pnpm-lock.yaml                 # أعِد القفل كما هو في المستودع
mkdir -p ~/.local/bin && printf '#!/bin/sh\nexec corepack pnpm "$@"\n' > ~/.local/bin/pnpm && chmod +x ~/.local/bin/pnpm
export PATH="$HOME/.local/bin:$PATH"
pnpm env:setup                                 # ينشئ .env ويطبع كلمات المرور (احفظها)
pnpm --filter @erp/config --filter @erp/contracts --filter @erp/database --filter @erp/testing run build

# 3) القاعدة
node ./scripts/local-db.mjs &                  # PostgreSQL 16 مضمّن بلا Docker
pnpm db:migrate && pnpm db:seed

# 4) التشغيل
pnpm --filter @erp/api build                   # الخادم يقرأ dist/main.js لا src
pnpm --filter @erp/api dev &                   # :3000
HOST=0.0.0.0 PORT=3001 pnpm --filter @erp/staff dev &   # :3001
```

قاعدة البيانات مُهيَّأة بعد `db:seed`: `tenantCode=demo`، `owner@demo.test` وكلمة المرور
في `.env` تحت `DEMO_OWNER_PASSWORD`.

## 3) قواعد ملزمة (مجرَّبة)

- **البيانات قبل الواجهة**: ترحيل ← خدمة واختبارات ← شاشة. لا شاشة بلا نهاية حقيقية.
- **صلاحية جديدة لا تظهر وحدها**: تُمنح عند التزويد، والمؤسسة القائمة تحتاج
  `pnpm db:seed` لإعادة مزامنة `role_permissions` (وهي فِعلة مقصودة لا خطأ).
  و**إن كان العمود موجوداً فلا تضف عموداً** — الترحيل وسيلة لا غاية.
- كل ترحيل **جمعي** ومع ملف `migrations/down/00XX_*.down.sql` يقابله. لا حذف بيانات.
- بعد أي تعديل على مخطط Drizzle: `pnpm --filter @erp/database run build` ثم
  `npx tsc --noEmit -p apps/api/tsconfig.json`.
- **لا تشغّل `next build` أثناء عمل `next dev`** على نفس التطبيق (كلاهما يستخدم `.next`).
- متغيّر باسم `price|amount|total|balance|cost|rate|count|sum` يُخطّئه ESLint إن لم يكن
  `Decimal`/نصّاً — استخدم `decimal.js` أو أعد التسمية.
- Node's `fetch` لا يحوّل `patch` إلى `PATCH` تلقائياً — اكتبها كبيرة في السكربتات.
- **هيئة الاستجابة ملفوفة بـ`data`**: سكربتات التحقق تفكّها (`parsed.data ?? parsed`)،
  وكذلك استجابة `/auth/login` (`sess.data.accessToken`).
- **أكواد الحسابات فريدة لكل مؤسسة**: كودان متماثلان في `scripts/verify-treasury.mjs`
  يُسفران عن **500** لا 409 — افحص الأكواد المستخدمة قبل إضافة قسم جديد.
- **المسوّدة ليست مالاً**: أي رقم مالي يُقرأ من السندات **المرحَّلة** فقط.
- **فلتر لا يُطبَّق أسوأ من فلتر غائب**: مُعرّف لا يخصّ أحداً يعني قائمة فارغة، لا كل
  الدفتر.
- لا `*` في صلاحيات المنصة، ولا دور إدارة عام. حافظ على توافق الـ API.
- التسميات من `Desktop_ERP` بنصّها العربي، ونسبة تطابق ≥ 90%؛ أي تسمية مخترَعة تُبرَّر
  في الملخّص.
- تنسيق المستودع ليس Prettier (`prettier --check` يُعلّم ملفات لم تُمسّ) — لا تُشغّل
  `prettier --write` على ملفات قائمة.

## 4) تعريف الإنجاز

- اختبارات جديدة خضراء + المجموعة كاملة خضراء.
- قسم جديد في `scripts/verify-treasury.mjs` يمشي المسار ضد ستاك حيّ (القسم الحادي عشر).
- `docs/desktop-parity/PHASE_06_TREASURY.md` و`docs/STATUS.md` مُحدَّثان.
- كومِت ودفع على `arena/01a0889e-cloud-saas-erp`، وتعليق على PR #4 يلخّص ما أُنجز
  (عنوان PR ووصفه لا يمكن تعديلهما بهذا التوكن).
