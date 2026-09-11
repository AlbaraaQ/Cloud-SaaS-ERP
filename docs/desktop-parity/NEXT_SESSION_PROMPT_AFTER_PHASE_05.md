# نص جاهز للمحادثة القادمة — بعد الجزء السابع من المرحلة 05

انسخ هذا الملف كاملاً وأرسله في المحادثة الجديدة. (ملف للنسخ فقط — ليس توثيقاً رسمياً.)

---

## الخيار الأول (الموصى به): بدء المرحلة 06 — الخزينة (Treasury)

المرحلة 05 **مغلقة** بأجزائها السبعة (راجع `docs/desktop-parity/PHASE_05_INVENTORY.md`
§10 و§11 و§12)، بما فيها آخر بند مؤجَّل: الرقم التسلسلي على سطر المستند. الباقي المؤجَّل
موثَّق في §13 وهو مقصود لا منسي:

- **الدفعة وتاريخ الإنتاج والانتهاء على سطر المستند** (`BatchNo`, `ItemProductionDate`,
  `ItemExpireDate` — `Class/InvoiceOper.cs` L1635): نصف الرقم التسلسلي نُفِّذ في §12؛ نصف
  الدفعة ينتظر المستندات التي تستهلك الدفعات، حيث `lot_id` موجود أصلاً على السطر.
- **وحدات الفواتير الخمس** (البيع والشراء ومرتجعاتها وعرض السعر): ما زالت تكتب خطوطها بلا
  أرقام. المخزون يتحرّك عبر المستندات المخزنية فقط الآن، وتُعامل الفواتير بنفس الجدول
  `stock_document_serials` عند حلول مراحلها.

## الخيار الثاني: إغلاق الباقي المؤجَّل من المرحلة 05 (§13)

**الهدف:** `BatchNo` و`ItemProductionDate` و`ItemExpireDate` على سطر المستند — نفس السطر في
`Class/InvoiceOper.cs` L1635 الذي حمل `ItemSerialNo`، ونفس القاعدة: المستند يقول **أي دفعة**
تحرّكت، لا كم فقط.

**نطاق مقترح (بترتيب الأمان):**
1. ترحيل `0040` جمعي + ملف `down/`: `lot_id` على جداول خطوط المستندات المخزنية الثلاثة
   (المناقلة والجرد لا يملكانه بعد)، وامتداد `stock_document_serials` أو جدول مقابل للدفعات
   إن لزم تتبّعها بنفس الطريقة.
2. السطر الذي يخرج من دفعة يجب أن تكون دفعته موجودة وفي المستودع نفسه
   (`LOT_NOT_FOUND` / `LOT_WRONG_WAREHOUSE` 422)، والسطر الوارد ينشئها إن لم تكن
   (`received_at` = تاريخ الإنتاج، `expiry_date` = تاريخ الانتهاء).
3. إلغاء المستند يعكس ذلك: دفعة أنشأها المستند تُمحى إن لم تتحرك، ودفعته تُعاد كما كانت.
4. اختبارات: `apps/api/test/inventory-document-lots.spec.ts`، وقسم سادس عشر في
   `scripts/verify-inventory.mjs`.
5. **ثم** — اختيارياً وبعد استقرار المستندات المخزنية — نقل `serialNos` إلى خطوط فواتير
   البيع والشراء والمرتجعات (خمس وحدات، دفعة واحدة، وبنفس جدول الربط).

## 1) أين نقف (حقائق مقيسة عند كتابة هذا النص)

- الفرع الإلزامي: `arena/01a0889e-cloud-saas-erp`.
- `apps/api`: **81 ملفاً / 475 اختباراً** خضراء. `apps/staff`: **36/36**. `@erp/database`: 17/17.
- `pnpm --filter @erp/staff run build`: **100/100** صفحة ثابتة.
- `node scripts/verify-inventory.mjs`: **15 قسماً** كلها ✓ وينتهي بـ «✔ Phase 05 inventory documents verified».
- **40 ترحيلاً** مطبقاً آخرها `0039_document_line_serials`.
- مسارات `/inventory/*` (21) و`/reports/*` للمخزون ترجع 200.

## 2) إعادة بناء البيئة إن كانت جديدة (تعرّضنا لها فعلاً)

البيئة المحلية **لا تُحفظ** في اللقطات: حزم `node_modules` وقاعدة البيانات تختفي مع إعادة إنشاء الصندوق. لإعادتها:

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

قاعدة البيانات مُهيَّأة بعد `db:seed`: `tenantCode=demo`، `owner@demo.test` وكلمة المرور في `.env` تحت `DEMO_OWNER_PASSWORD`.

## 3) قواعد ملزمة (مجرَّبة)

- **البيانات قبل الواجهة**: ترحيل ← خدمة واختبارات ← شاشة. لا شاشة بلا نهاية حقيقية.
- كل ترحيل **جمعي** ومع ملف `migrations/down/00XX_*.down.sql` يقابله. لا حذف بيانات.
- بعد أي تعديل على مخطط Drizzle: `pnpm --filter @erp/database run build` ثم `npx tsc --noEmit -p apps/api/tsconfig.json`.
- **لا تشغّل `next build` أثناء عمل `next dev`** على نفس التطبيق (كلاهما يستخدم `.next`) — أوقف `dev` أولاً.
- لا `*` في صلاحيات المنصة، ولا دور إدارة عام. حافظ على توافق الـ API.
- التسميات من `Desktop_ERP` بنصّها العربي، ونسبة تطابق ≥ 90%؛ أي تسمية مخترَعة تُبرَّر في الملخّص.
- تنسيق المستودع ليس Prettier (`prettier --check` يُعلّم ملفات لم تُمسّ) — لا تُشغّل `prettier --write` على ملفات قائمة.

## 4) تعريف الإنجاز

- اختبارات جديدة خضراء + المجموعة كاملة خضراء.
- قسم جديد في `scripts/verify-inventory.mjs` يمشي المسار ضد ستاك حيّ.
- `docs/desktop-parity/PHASE_05_INVENTORY.md` و`docs/STATUS.md` مُحدَّثان.
- كومِت ودفع على `arena/01a0889e-cloud-saas-erp`، وتعليق على PR #4 يلخّص ما أُنجز (عنوان PR ووصفه لا يمكن تعديلهما بهذا التوكن).
