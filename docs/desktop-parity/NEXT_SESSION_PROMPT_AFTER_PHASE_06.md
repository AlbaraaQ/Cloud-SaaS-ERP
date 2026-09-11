# نص جاهز للمحادثة القادمة — بعد الجزء الرابع من المرحلة 06 (الخزينة)

انسخ هذا الملف كاملاً وأرسله في المحادثة الجديدة. (ملف للنسخ فقط — ليس توثيقاً رسمياً.)

---

المرحلة 05 (المخزون) **مغلقة** بأجزائها السبعة، والمرحلة 06 (الخزينة) أُنجز منها أربعة
أجزاء: **سند القبض وسند الصرف بوثيقة كاملة + القيد** (§1–§8)، و**تعريف الخزن والبنوك**
(§9)، و**حركة الصندوق** (§10)، و**إغلاقات اليومية** (§11). راجع
`docs/desktop-parity/PHASE_06_TREASURY.md` قبل أي سطر كود.

## الخيار الأول (الموصى به): الجزء الخامس — التحويل البنكي والعميل النقدي

* `Form_WPF/frmPayBank.xaml` — `🏦 اختر طريقة الدفع (تحويل بنكي)` ولوحة
  `🏦 البنوك المتاحة` (`wrapBanks`) ثم `✔ موافق` / `✖ خروج`: البنك يُختار **لحظة الدفع**
  داخل السند، لا في بطاقة البنك فقط. الواجهة الخلفية تعرف `method: bank_transfer` وقائمة
  البنوك من الجزء الثاني (`GET /cash-locations?kind=bank`)؛ **ما ينقص هو اختيار البنك
  على السند نفسه وربطه بالقيد**.
* `Form_WPF/frmCashCustomer.xaml` — `👤 عميل نقدي` «إضافة أو اختيار عميل نقدي للفاتورة»:
  `🏷️ الاسم:` · `📱 رقم الجوال:` · `✅ إدراج` / `🚪 خروج`، وشبكة بحث
  (`🔍 البحث` بالجوال أو بالاسم) بأعمدة `👤 الاسم` · `📱 الجوال` · `✔ اختار`.
  أي: إنشاء طرف سريع من نقطة البيع بلا فتح شاشة الأطراف.

## الخيار الثاني: الجزء السادس — مناقلة الخزن

`Form_WPF/frmSafesTransfer.xaml` والتقرير `Reports/rptSafeTransfer.repx`: إرسال /
استلام / إقفال بين الخزن. الواجهة الخلفية موجودة (`/cash-transfers`،
`/cash-transfers/:id/send`، `/:id/receive`)؛ الشاشة لا.

## الخيار الثالث: قيد الإغلاق (مؤجَّل عن قصد من الجزء الرابع)

`BindCloseShiftToEntry1` في `Form_WPF/ClosShiftAndroid.xaml.cs` يبني قيداً حول رقم
الإغلاق. النهايات تحسب الأرقام وتجمّدها ولا تكتب القيد بعد — يُبنى مع جزء المحاسبة في
هذه المرحلة حتى تأتي القيود من محرّك واحد لا اثنين.

---

---

## 1) أين نقف (حقائق مقيسة عند كتابة هذا النص)

- الفرع الإلزامي: `arena/01a0889e-cloud-saas-erp`.
- `apps/api`: **85 ملفاً / 502 اختباراً** خضراء (منها `treasury-dayclose.spec.ts` بـ8).
  `apps/staff`: **36/36**. `@erp/contracts`: 71/71.
- `node scripts/verify-treasury.mjs`: **9 أقسام** كلها ✓ وينتهي بـ
  «✔ Phase 06 treasury documents verified» — أول قسم منه يقفل درجاً مفتوحاً من تشغيل
  سابق حتى يبقى السكربت قابلاً للتكرار.
- `node scripts/verify-inventory.mjs`: **15 قسماً** كلها ✓.
- **43 ترحيلاً** مطبقاً آخرها `0042_shift_close_number`.
- الشاشات الحيّة في الخزينة: `/treasury/vouchers` · `/treasury/cash-locations` ·
  `/treasury/banks` · `/treasury/movements` · `/treasury/day-close`.

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

> **درس من الجزء الرابع:** خادم الـ API الشغّال يقرأ `apps/api/dist/main.js`
> (`node --watch`)، فتعديل `src` لا يظهر حتى `pnpm --filter @erp/api build`.

## 3) قواعد ملزمة (مجرَّبة)

- **البيانات قبل الواجهة**: ترحيل ← خدمة واختبارات ← شاشة. لا شاشة بلا نهاية حقيقية.
- كل ترحيل **جمعي** ومع ملف `migrations/down/00XX_*.down.sql` يقابله. لا حذف بيانات.
- بعد أي تعديل على مخطط Drizzle: `pnpm --filter @erp/database run build` ثم
  `npx tsc --noEmit -p apps/api/tsconfig.json`.
- **لا تشغّل `next build` أثناء عمل `next dev`** على نفس التطبيق (كلاهما يستخدم `.next`).
- متغيّر باسم `price|amount|total|balance|cost|rate|count|sum` يُخطّئه ESLint إن لم يكن
  `Decimal`/نصّاً — استخدم `decimal.js` أو أعد التسمية.
- Node's `fetch` لا يحوّل `patch` إلى `PATCH` تلقائياً — اكتبها كبيرة في السكربتات.
- **هيئة الاستجابة ملفوفة بـ`data`**: سكربتات التحقق تفكّها (`parsed.data ?? parsed`)،
  فلا تفترض شكلاً مختلفاً.
- **المسوّدة ليست مالاً**: أي رقم مالي يُقرأ من السندات **المرحَّلة** فقط.
- **فلتر لا يُطبَّق أسوأ من فلتر غائب**: مُعرّف لا يخصّ أحداً يعني قائمة فارغة، لا كل
  الدفتر — وهو خطأ صحيحه الجزء الرابع في `dayCloses`.
- لا `*` في صلاحيات المنصة، ولا دور إدارة عام. حافظ على توافق الـ API.
- التسميات من `Desktop_ERP` بنصّها العربي، ونسبة تطابق ≥ 90%؛ أي تسمية مخترَعة تُبرَّر
  في الملخّص.
- تنسيق المستودع ليس Prettier (`prettier --check` يُعلّم ملفات لم تُمسّ) — لا تُشغّل
  `prettier --write` على ملفات قائمة.

## 4) تعريف الإنجاز

- اختبارات جديدة خضراء + المجموعة كاملة خضراء.
- قسم جديد في `scripts/verify-treasury.mjs` يمشي المسار ضد ستاك حيّ (القسم العاشر).
- `docs/desktop-parity/PHASE_06_TREASURY.md` و`docs/STATUS.md` مُحدَّثان.
- كومِت ودفع على `arena/01a0889e-cloud-saas-erp`، وتعليق على PR #4 يلخّص ما أُنجز
  (عنوان PR ووصفه لا يمكن تعديلهما بهذا التوكن).
