# نص جاهز للمحادثة القادمة — بعد الجزء الأول من المرحلة 06 (الخزينة)

انسخ هذا الملف كاملاً وأرسله في المحادثة الجديدة. (ملف للنسخ فقط — ليس توثيقاً رسمياً.)

---

المرحلة 05 (المخزون) **مغلقة** بأجزائها السبعة، والمرحلة 06 (الخزينة) بدأت بجزئها الأول:
**سند القبض وسند الصرف بوثيقة كاملة + القيد**. راجع
`docs/desktop-parity/PHASE_06_TREASURY.md` §1–§8 قبل أي سطر كود.

## الخيار الأول (الموصى به): الجزء الثاني — تعريف الخزن والبنوك

**الهدف:** `Form_WPF/frmTreasury.xaml` (`Title="تعريف الخزن"`) و
`Form_WPF/frmBanks.xaml` (`Title="🏦 تعريف البنوك"`) شاشتان حقيقيتان، لا جدول واحد.

ما يفعله الديسكتوب فعلاً:

* `frmTreasury.xaml.cs:87` يعرض الصناديق من `Stocks` موصولة بـ `Branches`، والأعمدة:
  `الرقم` · `🏦 اسم الصندوق` · `🏢 الفرع` · `👤 الموظف المسئول`.
* الحفظ (`.xaml.cs:222`) يشترط: «ادخل اسم الخزينة»، «يجب اختيار الفرع»،
  **«يجب اختيار موظف مسئول»** — ثم يكتب `Stocks` ويستبدل مسئولي الصندوق في
  `Stock_Emps` داخل نفس المعاملة (حذف الكل ثم إعادة الإدراج).
* `📋 حالة الصندوق` قيمتان: `نشط` / `مغلق`. و`⭐ الافتراضي` و`📝 ملاحظات`.
* `frmBanks.xaml` يضيف للبنك: `🌍 الدولة` · `🏙️ المدينة` · `📍 المنطقة` · `تليفون` ·
  `موبايل` · `💰 نسبة الاقتطاع %` · `📝 ملاحظات` · `✅ تغيير في نقطة البيع`.

**ما عندنا الآن:** `cash_locations` فيها `branchId` · `kind (safe|bank)` · `name` ·
`accountId` · `currencyCode` · `isDefault` · `bank jsonb` · `changeInPos` · `isActive`.
الناقص: **ملاحظات**، و**المسئولون** (`Stock_Emps` = مسئولون متعددون لكل صندوق، لا مسئول
واحد)، وحقول البنك.

**نطاق مقترح (بترتيب الأمان):**
1. ترحيل `0041` جمعي + ملف `down/`: `cash_locations.notes text`، وجدول
   `cash_location_custodians (tenant_id, cash_location_id, employee_id)` بفهرس فريد
   وRLS، وحقول البنك داخل `bank jsonb` (لا ترحيل: الخريطة JSON مُصنَّفة في
   `packages/contracts/src/organization/cash-locations.ts`).
2. عقد zod: `notes`، `custodianIds`، وامتداد `bankDetailsSchema`.
3. الخدمة: حفظ المسئولين في معاملة واحدة كما يفعل الديسكتوب، والتحقق أن الموظف من نفس
   المؤسسة (`CUSTODIAN_NOT_FOUND`)، وشرط «يجب اختيار موظف مسئول» للصناديق
   (`CUSTODIAN_REQUIRED`).
4. شاشتان: `/treasury/safes` (🏦 تعريف الخزينة) و`/treasury/banks` (🏦 تعريف البنوك)
   بتبويبات `📋 بيانات الصناديق` · `👤 مسئولي الصندوق` · `📝 ملاحظات`.
5. اختبارات `apps/api/test/treasury-custody.spec.ts` + قسم جديد في
   `scripts/verify-treasury.mjs`.

## الخيار الثاني: الجزء الثالث — حركة الصندوق (كشف)

**الهدف:** `Form_WPF/frmRptKhzna.xaml` (`Title="حركة الصندوق"`): `م` · `الرقم` ·
`📅 التاريخ` · `📝 البيان` · `العملية` · `📥 وارد` · `📤 صادر` · `⚖️ الرصيد` — برصيد
متحرك، وترشيح `من تاريخ / إلى تاريخ` **و`من وقت (HH:mm) / إلى وقت (HH:mm)`**، مع
`⚖️ الرصيد الإجمالي` و`📅 رصيد الفترة المحددة`، واختيار `🏦 الصندوق`.

ما عندنا: تقرير `cash-movement` في `report-catalog.ts` يجمع القبض والصرف لكل صندوق
بلا رصيد متحرك ولا ترشيح وقت. الجزء الثالث يضيف تقريراً جديداً
(`treasury-safe-statement`) ويشغّله من `/reports/[key]`.

## الخيار الثالث: الجزء الرابع — إغلاقات اليومية

`Form_WPF/frmCloseShift.xaml` (`📊 إغلاقات اليومية`): `🏦 رصيد الصندوق` · `💵 النقدي` ·
`🌐 الشبكة` · `💰 مجموع الشبكة والنقدي` · `🧾 الضريبة` · `📤 المصاريف` · `🏷️ الخصم` ·
`📉 الفرق` · `💹 الصافي` · `📋 آجل` · `🚗 توصيل` · `☕ الضيافة` · `🛒 المشتريات` ·
`🛡️ تأمين`، وأزرار `📧 إرسال` و`🔄 إعادة`. الواجهة الخلفية موجودة
(`shift-closes/*`، `cash_count_lines`)؛ الشاشة لا.

---

## 1) أين نقف (حقائق مقيسة عند كتابة هذا النص)

- الفرع الإلزامي: `arena/01a0889e-cloud-saas-erp`.
- `apps/api`: **82 ملفاً / 482 اختباراً** خضراء. `apps/staff`: **36/36**. `@erp/database`: 17/17.
- `pnpm --filter @erp/staff run build`: **100/100** صفحة ثابتة.
- `node scripts/verify-inventory.mjs`: **15 قسماً** كلها ✓.
- `node scripts/verify-treasury.mjs`: **6 أقسام** كلها ✓ وينتهي بـ
  «✔ Phase 06 treasury documents verified».
- **41 ترحيلاً** مطبقاً آخرها `0040_treasury_voucher_detail`.

## 2) إعادة بناء البيئة إن كانت جديدة (تعرّضنا لها فعلاً)

البيئة المحلية **لا تُحفظ** في اللقطات: حزم `node_modules` وقاعدة البيانات تختفي مع
إعادة إنشاء الصندوق. لإعادتها:

```bash
corepack pnpm install --frozen-lockfile      # الحزم
mkdir -p ~/.local/bin && printf '#!/bin/sh\nexec corepack pnpm "$@"\n' > ~/.local/bin/pnpm && chmod +x ~/.local/bin/pnpm
export PATH="$HOME/.local/bin:$PATH"         # لأن السكربتات الداخلية تنادي pnpm مباشرة
pnpm env:setup                               # ينشئ .env ويطبع كلمات المرور (احفظها)
pnpm --filter @erp/config --filter @erp/contracts --filter @erp/database --filter @erp/testing run build
pnpm db:local &                              # PostgreSQL 16 مضمّن بلا Docker
pnpm db:migrate && pnpm db:seed
pnpm --filter @erp/api dev &                 # :3000
HOST=0.0.0.0 PORT=3001 pnpm --filter @erp/staff dev &   # :3001
```

قاعدة البيانات مُهيَّأة بعد `db:seed`: `tenantCode=demo`، `owner@demo.test` وكلمة المرور
في `.env` تحت `DEMO_OWNER_PASSWORD`.

## 3) قواعد ملزمة (مجرَّبة)

- **البيانات قبل الواجهة**: ترحيل ← خدمة واختبارات ← شاشة. لا شاشة بلا نهاية حقيقية.
- كل ترحيل **جمعي** ومع ملف `migrations/down/00XX_*.down.sql` يقابله. لا حذف بيانات.
- بعد أي تعديل على مخطط Drizzle: `pnpm --filter @erp/database run build` ثم
  `npx tsc --noEmit -p apps/api/tsconfig.json`.
- **لا تشغّل `next build` أثناء عمل `next dev`** على نفس التطبيق (كلاهما يستخدم `.next`).
- متغيّر باسم `price|amount|total|balance|cost|rate` يُخطّئه ESLint إن لم يكن
  `Decimal`/نصّاً — استخدم `decimal.js` أو أعد التسمية.
- Node's `fetch` لا يحوّل `patch` إلى `PATCH` تلقائياً — اكتبها كبيرة في السكربتات.
- لا `*` في صلاحيات المنصة، ولا دور إدارة عام. حافظ على توافق الـ API.
- التسميات من `Desktop_ERP` بنصّها العربي، ونسبة تطابق ≥ 90%؛ أي تسمية مخترَعة تُبرَّر
  في الملخّص.
- تنسيق المستودع ليس Prettier (`prettier --check` يُعلّم ملفات لم تُمسّ) — لا تُشغّل
  `prettier --write` على ملفات قائمة.

## 4) تعريف الإنجاز

- اختبارات جديدة خضراء + المجموعة كاملة خضراء.
- قسم جديد في `scripts/verify-treasury.mjs` يمشي المسار ضد ستاك حيّ.
- `docs/desktop-parity/PHASE_06_TREASURY.md` و`docs/STATUS.md` مُحدَّثان.
- كومِت ودفع على `arena/01a0889e-cloud-saas-erp`، وتعليق على PR #4 يلخّص ما أُنجز
  (عنوان PR ووصفه لا يمكن تعديلهما بهذا التوكن).
