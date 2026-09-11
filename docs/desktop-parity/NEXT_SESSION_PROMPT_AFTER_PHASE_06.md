# نص جاهز للمحادثة القادمة — بعد الجزء الثاني من المرحلة 06 (الخزينة)

انسخ هذا الملف كاملاً وأرسله في المحادثة الجديدة. (ملف للنسخ فقط — ليس توثيقاً رسمياً.)

---

المرحلة 05 (المخزون) **مغلقة** بأجزائها السبعة، والمرحلة 06 (الخزينة) أُنجز منها جزآن:
**سند القبض وسند الصرف بوثيقة كاملة + القيد** (§1–§8) و**تعريف الخزن والبنوك** (§9).
راجع `docs/desktop-parity/PHASE_06_TREASURY.md` قبل أي سطر كود.

## الخيار الأول (الموصى به): الجزء الثالث — حركة الصندوق

**الهدف:** `Form_WPF/frmRptKhzna.xaml` (`Title="حركة الصندوق"`) كشفاً حقيقياً برصيد متحرك.

ما يفعله الديسكتوب فعلاً — **وهذا أهم ما في الجزء**: الكشف **لا** يقرأ سندات القبض
والصرف، بل يقرأ **دفتر الحساب** (`.xaml.cs` L156–L260):

1. يجلب حساب الصندوق باسمه لا برقمه —
   `SELECT Accounts_Index.Code FROM Accounts_Index, Stocks
    WHERE Accounts_Index.AName = Stocks.name AND Accounts_Index.acc_branch = Stocks.branch
      AND Accounts_Index.Type = 2 AND Stocks.id = {الصندوق}`؛
   وإن لم يجد: «لم يتم العثور على حساب مرتبط بهذا الصندوق.» عندنا الحساب مربوط فعلياً
   بعمود `cash_locations.account_id` — فلا حاجة لمطابقة الأسماء، وهذا تحسّن لا انحراف.
2. **رصيد سابق** (فقط إن كانت الفترة محدّدة لا «كل الفترة»):
   `SUM(Entry_sub.dept) - SUM(Entry_sub.credit)` لكل القيود التي تاريخها `< من تاريخ`،
   ويضاف له **سطر أول** عنوانه `رصيد سابق` بتاريخ `من تاريخ − يوم`.
3. **الحركات**: `GROUP BY Entry.GlobalID, Entry.type, Entry.date, Entry.notes` مع
   `SUM(dept)`/`SUM(credit)` على حساب الصندوق، مرتّبة بالتاريخ. الرصيد المتحرك
   `runBalance += dept − credit`. و`Entry.IS_Deleted=0 AND Entry.state=1` — المرحَّلة
   فقط، فلا يظهر مسوّدة ولا معتمد ملغى.
4. بطاقتان في الأسفل: `⚖️ الرصيد الإجمالي` = آخر رصيد متحرك، و
   `📅 رصيد الفترة المحددة` = آخر رصيد − الرصيد السابق.

شاشة الديسكتوب (`frmRptKhzna.xaml` L233–L560):

| المنطقة | المحتوى |
|---|---|
| لوحة الترشيح | `🏦 الصندوق` · `📅 الفترة` · ☑ `كل الفترة` (مفعّل افتراضاً) · `من تاريخ:` · `من وقت (HH:mm):` (`00:00`) · `إلى تاريخ:` · `إلى وقت (HH:mm):` (`23:59`) · زر `🏦 عرض حركة الصندوق` |
| الشبكة | `م` · `العملية` · `الرقم` · `📅 التاريخ` · `📥 وارد` · `📤 صادر` · `⚖️ الرصيد` · `📝 البيان` |
| البطاقات | `⚖️ الرصيد الإجمالي` · `📅 رصيد الفترة المحددة` |
| الأزرار | `✖ خروج` · `📊 تصدير Excel` · `👁️ معاينة` · `🖨️ طباعة` (`Reports/RptKhzna.repx`، وإعدادات الطباعة من `SettingPrint WHERE Inv_Id=12`) |

تصدير CSV عنده برأس: `م,العملية,الرقم,التاريخ,وارد,صادر,الرصيد,البيان`، ورسالة
«لا توجد بيانات للتصدير.» إن كانت القائمة فارغة.

**ما عندنا الآن:** تقرير `cash-movement` في `apps/api/src/modules/reporting/report-catalog.ts`
يجمع سندات القبض والصرف لكل صندوق بلا رصيد متحرك ولا ترشيح وقت ولا سطر «رصيد سابق»؛
و`accounting.generalLedger(accountId)` (L310) يقرأ السطور فعلاً لكنه **بلا أي ترشيح**
(لا تاريخ، لا وقت، لا رصيد). وحقل `vouchers.voucher_time` جاهز من الجزء الأول.

**النطاق المقترح:**
1. **بلا ترحيل** — كل شيء موجود: `journal_entries`، `journal_entry_lines`،
   `cash_locations.account_id`، `vouchers.voucher_time`.
2. نهاية `GET /treasury/cash-locations/:id/movements?from=&to=&fromTime=&toTime=&all=1`
   في `apps/api/src/modules/treasury/` بصلاحية `treasury.view`، تعيد:
   `{ openingBalance, rows: [{ seq, processType, number, date, income, outcome, balance, note }], totalAll, totalPeriod }` —
   الحساب من `journal_entry_lines` لا من `vouchers`، والعملة نصّاً عشرياً (`decimal.js`).
   `422 CASH_ACCOUNT_REQUIRED` إن كان الصندوق بلا حساب (رسالة الديسكتوب نفسها).
3. شاشة `/treasury/movements` بالتبويب `🏦 حركة الصندوق`، تقرأ الصناديق من
   `GET /cash-locations?kind=safe`.
4. اختبارات `apps/api/test/treasury-movements.spec.ts` + **قسم ثامن** في
   `scripts/verify-treasury.mjs`.

## الخيار الثاني: الجزء الرابع — إغلاقات اليومية

`Form_WPF/frmCloseShift.xaml` (`📊 إغلاقات اليومية`): `🏦 رصيد الصندوق` · `💵 النقدي` ·
`🌐 الشبكة` · `💰 مجموع الشبكة والنقدي` · `🧾 الضريبة` · `📤 المصاريف` · `🏷️ الخصم` ·
`📉 الفرق` · `💹 الصافي` · `📋 آجل` · `🚗 توصيل` · `☕ الضيافة` · `🛒 المشتريات` ·
`🛡️ تأمين`، وأزرار `📧 إرسال` و`🔄 إعادة`، مع `frmCloseShiftDetails.xaml` و
`frmCloseShiftInv.xaml`. الواجهة الخلفية موجودة (`shift-closes/*`، `cash_count_lines`)؛
الشاشة لا.

## الخيار الثالث: الجزء الخامس — التحويل البنكي والعميل النقدي

`Form_WPF/frmPayBank.xaml` (اختيار البنك لحظة الدفع داخل السند) و
`Form_WPF/frmCashCustomer.xaml` (عميل نقدي سريع بفاتورة من خطوة واحدة).

---

---

## 1) أين نقف (حقائق مقيسة عند كتابة هذا النص)

- الفرع الإلزامي: `arena/01a0889e-cloud-saas-erp`.
- `apps/api`: **83 ملفاً / 488 اختباراً** خضراء. `apps/staff`: **36/36**. `@erp/contracts`: 71/71.
- `pnpm --filter @erp/staff run build`: **100/100** صفحة ثابتة.
- `node scripts/verify-inventory.mjs`: **15 قسماً** كلها ✓.
- `node scripts/verify-treasury.mjs`: **7 أقسام** كلها ✓ وينتهي بـ
  «✔ Phase 06 treasury documents verified».
- **42 ترحيلاً** مطبقاً آخرها `0041_treasury_custody`.

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
