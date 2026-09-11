# نص جاهز للمحادثة القادمة — بعد الجزء السادس من المرحلة 05

انسخ هذا الملف كاملاً وأرسله في المحادثة الجديدة. (ملف للنسخ فقط — ليس توثيقاً رسمياً.)

---

## الخيار الأول (الموصى به): إغلاق آخر بند مؤجَّل في المرحلة 05 — الرقم التسلسلي والدفعة على سطر المستند

المرحلة 05 منفَّذة بأجزائها الستة (راجع `docs/desktop-parity/PHASE_05_INVENTORY.md` §10 و§11). البند الوحيد المؤجَّل موثَّق في §11.6، وهذا هو نص تنفيذه.

**الهدف:** الرقم التسلسلي ورقم الدفعة يسكنان **سطر المستند**، كما في `Class/InvoiceOper.cs` L1635:

```
INSERT into InvoiceItemDetail(ItemDetailId, ItemIncrId, InvGlobalID, ItemId, InvertoryImpact,
  ItemSerialNo, BatchNo, ItemProductionDate, ItemExpireDate, ItemHeight, ItemWidth,
  ItemColor, ItemSize, ItemProperty, FillValue, FillRatio, ItemQuantity, ItemBarcode) …
```

و`Form_WPF/frmItemSerialNo.xaml.cs` L524 يعرضها: `SELECT SerialNo AS DgvSerialNo FROM ItemSerialNo, inv`.

**لماذا هو مؤجَّل لا منسي:** حالة الرقم (`available/reserved/sold`) تقول أين القطعة، ولا تقول **أي مستند** حرّكها؛ فلا يمكن تتبّع رقم مباع إلى الفاتورة التي باعته. تنفيذه يمسّ كل مستند يحرّك المخزون: `stock_voucher_lines`، `stock_transfer_lines`، `stock_adjustment_lines`، وخطوط فواتير البيع والشراء والمرتجعات — خمس وحدات دفعة واحدة، وهو التغيير الوحيد في هذه المرحلة الذي يُمكن أن يُفسد دفتراً بصمت إن أُخطئ.

**نطاق مقترح (بترتيب الأمان):**
1. ترحيل `0039` جمعي + ملف `down/`: `lot_id` على جداول خطوط المستندات المخزنية الثلاثة، وجدول جديد `stock_document_serials (tenant_id, doc_type, line_id, serial_id)` مع فهرس فريد.
2. السند **الوارد** يُنشئ الأرقام أو يربطها (تصبح `available`)؛ السند **الصادر** يستهلكها (`sold`) ويُصرَف بمعامل الوحدة كما في 0035 (`base_qty = qty × factor`).
3. الكمية يجب أن تساوي عدد الأرقام (`SERIAL_COUNT_MISMATCH` 422)، ولا يجوز تكرار رقم داخل المستند.
4. الشاشات: تبويب «الأرقام» داخل سند الإدخال/الإخراج والمناقلة، مع لصق دفعة من الأرقام (من مولّد `⚙️ توليد`).
5. اختبارات: `apps/api/test/inventory-document-serials.spec.ts`، وقسم خامس عشر في `scripts/verify-inventory.mjs`.

**لا تلمس** فواتير البيع والشراء في هذه الدفعة — تُفتح بعد استقرار المستندات المخزنية.

## الخيار الثاني: بدء المرحلة 06 — الخزينة (Treasury)

«لا تبدأ المرحلة 06 قبل إغلاق نطاق المرحلة 05» — صار مسموحاً الآن إن اخترت هذا الخيار، مع بقاء بند §11.6 موثَّقاً مؤجَّلاً.

---

## 1) أين نقف (حقائق مقيسة عند كتابة هذا النص)

- الفرع الإلزامي: `arena/01a0889e-cloud-saas-erp`.
- `apps/api`: **80 ملفاً / 468 اختباراً** خضراء. `apps/staff`: **36/36**. `@erp/database`: 17/17.
- `pnpm --filter @erp/staff run build`: **100/100** صفحة ثابتة.
- `node scripts/verify-inventory.mjs`: **14 قسماً** كلها ✓ وينتهي بـ «✔ Phase 05 inventory documents verified».
- **39 ترحيلاً** مطبقاً آخرها `0038_production_order_reference`.
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
