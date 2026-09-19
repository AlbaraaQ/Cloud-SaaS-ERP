# P-C4 — الباقات والتراخيص والفوترة

> الجزء الرابع من [`docs/roadmap/PLATFORM_CONSOLE_PLAN.md`](roadmap/PLATFORM_CONSOLE_PLAN.md) §4.
> يبدأ من حيث انتهى P-C3 (`60b2120` · PR #6) ولا يعيد بناء شيء منه.
> **الحالة: مُنجَز** — 2026-09-17.

| | |
|---|---|
| **الشاشات** | `/plans` (جدول حقوق: وحدة · حدّ · راية) · `/subscriptions` (دورة الحياة كاملة) · **`/invoices`** جديدة (فواتير + إشعارات دائن + تحصيل + شطب) · **`/invoices/[id]/print`** جديدة (معاينة A4) · **`/dunning`** جديدة (الجدول · المحاولات · الرسائل) · **`/revenue`** جديدة (MRR · ARR · المتأخّر) |
| **نقاط النهاية** | `PlatformBillingController` — **21** مساراً تحت `/platform/*`: 5 منقولة **بمساراتها** من `PlatformAdminController` و16 جديدة |
| **الترحيل** | `0068_platform_billing.sql` — 6 جداول جديدة + توسعة دورة حياة على `tenant_subscriptions` (الخطة قالت `0067`؛ الرقم صار لبطاقة العميل في P-C2 — §6 من الخطة يسمح بإعادة الترقيم) |
| **الرموز** | `console.plans.manage` · `console.subscriptions.manage` · `console.billing.manage` — **مُعلنة من P-C1 وأول استخدام لها هنا**: ثلاثة أبواب، لا رمز جديد |
| **الاختبار** | `apps/api/test/platform-billing.spec.ts` — **23** اختباراً (الخطة طلبت ≥ 14) · لوحة المنصة **24** (كان 20) · contracts **81** (كان 71) · staff 36 |
| **التحقّق الحيّ** | `scripts/verify-platform-billing.mjs` — **147** نقطة في **12** قسماً (الخطة طلبت ≈ 50)، أُعيد تشغيله مرّتين: 147/147 وExit 0 |
| **الأرقام** | مسارات `/platform/*` **57** (كان 40) · شاشات اللوحة **18** (كانت 14) |

---

## 0. بوابة «المصدر بالسطر»

اللوحة كلها بلا مقابل في `Desktop_ERP`: لا مستأجرين في الديسكتوب ولا مشغّلين ولا تراخيص
تُباع. لذلك ينقل هذا الجزء **مفردات** الشاشات القائمة في اللوحة نفسها، ويُدرج كل تسمية
مخترعة في §5 أدناه بسببها.

**التسميات المنقولة بالحرف:**

| التسمية | المصدر |
|---|---|
| الباقات · الرمز · الاسم · السعر · الدورة · العملة | `apps/platform-admin/app/plans/page.tsx` (نسخة ما قبل P-C4) |
| الاشتراكات والتراخيص · العميل · الباقة · القيمة · المصدر · الحالة · يبدأ · ينتهي · إلغاء | `apps/platform-admin/app/subscriptions/page.tsx` (نسخة ما قبل P-C4) |
| مسودّة · صادرة · مدفوعة · ملغاة | `packages/contracts/src/platform/billing.ts` (`platformInvoiceStatusSchema`) ومفردات الفاتورة في `apps/api/src/modules/reporting/print-templates.service.ts` |
| الإجمالي قبل الضريبة · ضريبة القيمة المضافة · الإجمالي · المدفوع · المتبقّي · الإجمالي بالحروف | `print-templates.service.ts` (ورقة الفاتورة الضريبية في سطح العميل — نفس الورقة بمفرداتها) |
| الرقم الضريبي · رقم الفاتورة · تاريخ الإصدار · تاريخ الاستحقاق | `apps/staff/app/invoices/*` وشاشة `settings` القائمة |
| المتابعة · التحصيل · التجربة · إيقاف مؤقّت · استئناف · ترقية/تخفيض | مفردات خطة §4 حرفياً |
| MRR · ARR · المتأخّر | مفردات لوحة الإيراد في خطة §4 (وهي مصطلحات المجال لا اختراع) |

**التسميات المخترعة** — «وحدة · حدّ · راية» (أنواع الحقوق) · «الفواتير والإشعارات
الدائنة» · «المتابعة والتحصيل» · «الإيراد» · «الإيراد المتكرّر» · «قبل الضريبة» ·
«السلّم» · «سقف المحاولات» · «مجدولة» · «أُرسلت» · «اتصال يدوي» · «إشعار دائن» ·
«الرمز الضريبي (ZATCA)» — ولكلٍّ سبب في §5.

---

## 1. ما كان مكسوراً — بالقياس لا بالرواية

### 1.1 «الباقة» كانت سطراً في جدول أسعار، لا عقداً

`billing_plans` كان يحمل `code, name, amount, interval, currency, active` فقط: لا حقوق
تُقاس، ولا ترخيص يمتد، ولا فاتورة تتبعه. والدليل أن `GET /platform/plans` قبل هذا الجزء
لم يكن يعرف كلمة `entitlements`، وأن `tenant_subscriptions` لم يكن فيه من دورة الحياة إلا
`status` و`current_period_end` — فلا إيقاف مؤقت، ولا إلغاء بنهاية المدة، ولا سبب مكتوب.

### 1.2 ولا فاتورة واحدة في المنصة على عملائها

`grep -rn "platform_invoices\|platform_invoice_lines" packages/database/migrations` قبل
هذا الجزء = صفر. المنصة كانت **تحصّل** (Stripe في `modules/platform/billing`) ولا
**تفوتر**: لا رقم متسلسل، ولا ضريبة قيمة مضافة، ولا مرجع ضريبي، ولا ورقة تُطبع.

### 1.3 ثلاثة رموز معلَّقة بلا استخدام

`console.plans.manage` · `console.subscriptions.manage` · `console.billing.manage` —
مُعلنة في فهرس P-C1 و**لا مسار واحد يحملها**: كانت `GET/POST /platform/plans` و
`GET/POST /platform/subscriptions` و`PATCH /platform/plans/:id/active` في
`PlatformAdminController` تعمل بـ`console.tenants.manage`. أي أن الفصل بين «من يدير
العملاء» و«من يدير المال» كان موجوداً في الفهرس وغير موجود في الحرس.

---

## 2. ما بُني

### 2.1 الترحيل `0068_platform_billing.sql`

| الجدول | الدور | القيود التي تمنع الخطأ |
|---|---|---|
| `billing_plan_entitlements` | حقوق كل باقة (`feature.*` · `limits.*`) | فريد `(plan_id, key)` · القيمة `jsonb` |
| `platform_invoice_sequences` | تسلسل أرقام المنصة (`PINV-` · `PCN-`) | فريد `doc_type` · التخصيص `INSERT … ON CONFLICT … RETURNING` داخل معاملة المستند |
| `platform_invoices` | فاتورة/إشعار دائن بضريبة | `total = subtotal + tax_amount` · `paid_amount >= 0` · الرقم مطلوب إلا في مسودّة أو ملغاة · رقم فريد · حالة من أربع |
| `platform_invoice_lines` | السطور (اشتراك · تناسب · خصم · تسوية) | فريد `(invoice_id, line_no)` |
| `platform_payments` | الدفعات (تحويل بنكي · نقداً · بطاقة · أخرى) | `amount > 0` · `bank_transfer\|cash\|card\|other` · `geidea\|neoleap` للحقل الاحتياطي |
| `dunning_attempts` | محاولات المتابعة | فريد `(subscription_id, invoice_id, attempt_no)` · 4 حالات · 3 قنوات |

وتوسعة `tenant_subscriptions`: `paused_at` · `resumed_at` · `cancel_at_period_end` ·
`canceled_reason` + قيد `CHECK` على ثماني حالات + **فهرس فريد جزئي**
(`tenant_subscriptions_live_tenant_key`) يجعل «ترخيصين حيّين لعميل واحد» مستحيلاً على
مستوى القاعدة لا التطبيق.

الجداول الستة كلها `ENABLE`+`FORCE ROW LEVEL SECURITY` مع سياستين: `tenant_isolation`
(`app.tenant_id`) و`platform_admin_plane` (`app.is_platform_admin`)، ومُنحت
`erp_api` صلاحيات القراءة/الإدراج/التحديث **بلا حذف** على المستندات.

### 2.2 المسارات (21 تحت `/platform/*`)

| الطريقة | المسار | الرمز | جديدة؟ |
|---|---|---|:--:|
| `GET` | `/platform/plans/entitlement-keys` | `console.plans.manage` | ✔ |
| `GET` `POST` | `/platform/plans` | `console.plans.manage` | منقولة بمسارها |
| `PATCH` | `/platform/plans/:id` | `console.plans.manage` | بديل `PATCH /platform/plans/:id/active` |
| `PUT` | `/platform/plans/:id/entitlements` | `console.plans.manage` | ✔ |
| `GET` `POST` | `/platform/subscriptions` | `console.subscriptions.manage` | منقولة بمسارها |
| `POST` | `/platform/subscriptions/:id/change-plan` | `console.subscriptions.manage` | ✔ |
| `POST` | `/platform/subscriptions/:id/pause` \| `resume` | `console.subscriptions.manage` | ✔ |
| `POST` | `/platform/subscriptions/:id/cancel` | `console.subscriptions.manage` | صار له سبب و`atPeriodEnd` |
| `GET` `POST` | `/platform/invoices` | `console.billing.manage` | ✔ |
| `GET` | `/platform/invoices/:id` | `console.billing.manage` | ✔ (شاشة التفاصيل) |
| `POST` | `/platform/invoices/:id/issue` \| `pay` \| `void` | `console.billing.manage` | ✔ |
| `GET` | `/platform/invoices/:id/print` | `console.billing.manage` | ✔ (ورقة A4 مكتفية بذاتها) |
| `GET` | `/platform/dunning` | `console.billing.manage` | ✔ |
| `POST` | `/platform/dunning/:subscription/run` | `console.billing.manage` | ✔ |
| `GET` | `/platform/revenue` | `console.billing.manage` | ✔ |

`platform-admin.service.ts` فقد `listPlans/createPlan/setPlanActive/listSubscriptions/
grantSubscription/cancelSubscription` (نُقلت أو استُبدلت)، وبقيت «طلبات التفعيل» في
`PlatformAdminController` لأنها قرارُ تنشيط لا مستندُ مال.

### 2.3 الشاشات

* **`/plans`** — بطاقة لكل باقة، وجدول حقوق بثلاثة أنواع: **وحدة** (راية تُفتح) · **حدّ**
  (رقم مشروط) · **راية** (سلوك). المحرّر يقرأ الفهرس من `GET /platform/plans/entitlement-keys`
  (لا قائمة مكتوبة في الشاشة)، والكتابة `PUT …/entitlements` **مجموعةً كاملة** + سبب.
* **`/subscriptions`** — دورة الحياة كاملة على صفّ واحد: إصدار (بتجربة أو بلا)، ترقية/تخفيض
  بمعاينة التناسب من الخادم، إيقاف مؤقّت، استئناف، إلغاء (فوري أو بنهاية المدة) — كلها بسبب.
* **`/invoices`** — مستندات الاشتراك: مسودّة ← إصدار (رقم متسلسل) ← تحصيل (كامل أو جزئي)
  ← مدفوعة، أو ملغاة بسبب. والطباعة من الصفّ مباشرة.
* **`/invoices/[id]/print`** — إطار معزول يحمل ورقة الخادم، مع زرّ طباعة وزرّ حفظ نسخة.
* **`/dunning`** — الجدول (من سيتّصل به غداً) والمحاولات (ما جرى) والرسائل (ما سيُقال).
* **`/revenue`** — MRR و ARR والمتأخّر والمحصَّل وعدّادات التراخيص وفواتير قادمة.

---

## 3. القرارات المصمَّمة (بأسبابها)

| القرار | السبب |
|---|---|
| **التناسب**: الرصيد من الباقة القديمة عن الأيام غير المستهلكة (`HALF_UP`, منزلتان)، والمقابل **سعر الباقة الجديدة كاملاً**، والصافي `charge − credit` | المدة الجديدة تبدأ اليوم: العميل يشتري شهراً كاملاً من الجديدة، ويُخصم منه ما دفع لغير المستهلك منها. اجتماعُهما في معادلةٍ واحدة هو ما يجعل الفاتورة قابلة للمراجعة |
| **التجربة لا رصيد فيها** | الرصيد ثمن مدةٍ دُفعت ولم تُستهلك؛ ومن يُجرّب لم يدفع. فتغيير الباقة أثناء التجربة يبدأ الفترة المدفوعة اليوم بكامل سعرها |
| **المسودّة بلا رقم** | الرقم الضريبي يُخصَّص عند الإصدار فقط، فلا يحترق رقمٌ في خطأ مشغّل. والعدد يبقى محفوظاً عند الإلغاء: المدقق يقرأ التسلسل |
| **إشعار دائن بسلسلة خاصة (`PCN-`)** | خلطُ البيعة بعكسها في تسلسل واحد يجعل الإقرار الضريبي غير مقروء |
| **مدفوعة لا تُلغى (422)** | الإلغاء بعد التحصيل يمحو إيراداً وقع فعلاً؛ والتصحيح المقروء هو ردّ الدفعات |
| **تحصيل زائد يُرفض (422) ويُعلن المتبقّي في الردّ** | منع التحصيل المزدوج: نفس التحويل البنكي مرتين يضاعف المدفوع فيدفع العميل ضريبةً على مبلغ لم يتحصّل |
| **فاتورة واحدة لكل مدة لكل ترخيص** | المطالبة مرتين عن شهر واحد أشهرُ خطأ في أنظمة الفوترة |
| **الضريبة والمهلة وهوية البائع من `platform_settings`** (`billing.*`، ستة مفاتيح) لا من الشيفرة | الورقة التي صدرت أمس لا يتغيّر بائعها بتغيير إعدادٍ اليوم — القيم **تُنسخ على المستند** لحظة إنشائه |
| **الضريبة تُحسب سطراً سطراً ثم تُجمع** (`calculateInvoiceTotals`) | مجموع سطورٍ لا يساوي إجمالي مستنده لا يُقبل في مراجعة، والصافي السالب (إشعار دائن) يحمل ضريبةً سالبةً بإشارته |
| **المتابعة تُسجَّل `مجدولة` لا `مرسلة`** | لا خدمة بريد بعد (P-C6): لا ندّعي إرسالاً لم يقع. والاتصال اليدوي وحده يُسجَّل `sent` لأن إنساناً أجراه. والسقف ثلاث محاولات، والرابعة **تُتجاوَز بسببها** لا بصمت |
| **حقوق الباقة تشمل `feature.*` و`limits.*` فقط** | إعدادات الهوية والشعار تُسكن `platform_settings` ولا تُباع كبند في قائمة أسعار |
| **نطاق المال منزلتان دائماً** (`platformFormatAmount`) | `numeric` في القاعدة بأربع منازل: `499.0000` في الشاشة رقمٌ يقرأ خطأً. التطبيع عند حدّ القراءة مرة واحدة |
| **رمز ZATCA والتفقيط على الورقة** | فاتورة سعودية بلا رمز QR تُعاد إلى البائع، وإجماليها بالحروف شرطٌ عرفيّ. الرمز مبنيّ بـ`buildQrPayload` نفسه الذي يستخدمه سطح العميل — لا مُنشئ ثانياً |

---

## 4. ما لم يُنفَّذ (بصراحة)

* **سداد البطاقة للاشتراك** — الدفع يدوي أولاً كما قالت الخطة (`bank_transfer` + مرجع +
  إيصال مرفوع، والحقلان `gateway` و`receipt_file_id` جاهزان). بوابتا `geidea` و`neoleap`
  القائمتان تخصّان **دفع عملاء العميل**، لا اشتراك العميل في المنصة؛ وربطهما بجلسة
  الدفع يحتاج قراراً تجارياً (استرداد، تسوية، فاتورة تلقائية) لم يكن في هذه الخطة.
* **إرسال رسائل المتابعة** — P-C6. الصفوف تُجدول والرسائل تُكتب، والمرسل يقرأها.
* **لوحة العميل «فواتيري»** — سياسة `tenant_isolation` على الجداول الأربعة جاهزة لتقرأها
  لوحة العميل بلا تغيير، لكن الشاشة ليست في هذه الخطة.
* **السقف الضريبي والتحويلات الأجنبية** — سعر صرف غير مدعوم؛ العملات تُعرض ولا تُجمع
  (`mixedCurrency` في لوحة الإيراد).

---

## 5. «ما اخترعناه» — التسميات المخترعة وأسبابها

| التسمية | السبب |
|---|---|
| وحدة · حدّ · راية | أنواع الحقوق الثلاثة في العقد (`module` · `limit` · `flag`). العربية صيغةٌ لفهرس P-C1، و«راية» مفردة اللوحة القائمة (`feature.*` = «راية ميزة») |
| الفواتير والإشعارات الدائنة | اسم الشاشة يذكر المستندين لأن أحدهما لا يفهم بلا الآخر (خطة §4 تسمّي «إيصالات» و«أشعار دائن») |
| المتابعة والتحصيل | ترجمة `dunning` بلا كلمة أعجمية؛ «المتابعة» فعل المشغّل و«التحصيل» نتيجته |
| الإيراد · الإيراد المتكرّر | خطة §4 تسمّي `revenue dashboard` و`MRR · ARR`؛ MRR وARR تُركتا لاتينيتين لأنهما مصطلحان مستعملان باللاتينية في التقارير المالية العربية |
| قبل الضريبة | تمييزٌ لصافي السطور عن الإجمالي بعد الضريبة (شائع في الفواتير المحلية) |
| السلّم · سقف المحاولات | `ladder` و`max attempts`: وصفٌ لما يحدث فعلاً (0 · 3 · 7 ثم توقّف) |
| مجدولة · أُرسلت | تمييز حالة المحاولة عن فعل الإرسال — وهو جوهر §3 أعلاه |
| اتصال يدوي | قناة `manual`: المشغّل اتّصل بنفسه وسجّل النتيجة |
| إشعار دائن | مرادف `credit_note` المستعمل في الفاتورة الضريبية السعودية |
| الرمز الضريبي (ZATCA) | تسمية الرمز على الورقة؛ أُضيفت العربية بين قوسين لأن الماسح يقرأ الاسم بالرمز |

---

## 6. التحقّق

* `platform-billing.spec.ts` — 23 اختباراً على قاعدة حقيقية (RLS مُشتغل، `erp_api` بلا
  `BYPASSRLS`): الصافي والضريبة والتوازن، رقم متسلسل، منع التحصيل المزدوج، الإلغاء،
  المتابعة وسقفها، الإيراد، الأبواب الثلاثة، والعزل بالصفوف.
* **مجموعة API كاملة**: `pnpm vitest run` في `apps/api` → **1043** اختباراً في **130** ملفاً كلها
  خضراء (كانت 1020 في 129)، بما فيها اختبارات النطاق والفوترة التي تمسّ الجداول الجديدة.
* `scripts/verify-platform-console.mjs` — أُعيد تشغيله بعد تحديث سجلّ الإعدادات: **160/160** في
  **17** قسماً (كان 159)؛ مجموع التحقّق الحيّ للوحة صار **307** نقطة في **29** قسماً.
* `scripts/verify-platform-billing.mjs` — 147 نقطة على مسار HTTP حقيقي: 12 قسماً من
  الجلسة إلى التنظيف، مرّتان متتاليتان 147/147 مع Exit 0. الأثر المعلَن الوحيد: مستندات
  تحقّق لا تُحذف (وهو قصدٌ: التسلسل الضريبي لا يُفرَّغ) وعميل تحقّق واحد يُعاد استخدامه.
* أُصلح خللان اكتشفهما الاختبار لا المراجعة: (1) قيد `platform_invoices_number_check`
  كان يمنع إلغاء **مسودّة** (لا رقم لها) — فصُحِّح إلى `status IN ('draft','void') OR
  number IS NOT NULL` وأُعيد الترحيل صعوداً ونزولاً؛ (2) `ANY(${keys}::text[])` في
  drizzle يوسّع مصفوفة JavaScript إلى قائمة وسائط فيفشل الاستعلام — استُبدل بـ`inArray`.

---

## 7. الملفات التي لمسها هذا الجزء

**ترحيل**
`packages/database/migrations/0068_platform_billing.sql` ·
`packages/database/migrations/down/0068_platform_billing.down.sql`

**عقود**
`packages/contracts/src/platform/billing.ts` (+ `.spec.ts`) ·
`packages/contracts/src/platform/console.ts` (6 مفاتيح `billing.*`) ·
`packages/contracts/src/platform/index.ts`

**API**
`apps/api/src/modules/platform/admin/platform-billing.{controller,service}.ts` (جديدان) ·
`platform-admin.{controller,service,module}.ts` ·
`apps/api/src/modules/platform/billing/billing.service.ts` ·
`apps/api/test/platform-billing.spec.ts` ·
`apps/api/test/platform-console-rbac.spec.ts` (تثبيت عدد الإعدادات 8 → 14)

**اللوحة**
`apps/platform-admin/app/plans/page.tsx` ·
`app/subscriptions/page.tsx` ·
`app/invoices/page.tsx` (جديد) ·
`app/invoices/[id]/print/page.tsx` (جديد) ·
`app/dunning/page.tsx` (جديد) ·
`app/revenue/page.tsx` (جديد) ·
`app/settings/page.tsx` · `app/globals.css` · `lib/navigation.ts` ·
`tests/routes.spec.ts` · `tests/navigation.spec.ts`

**سكربتات وتوثيق**
`scripts/verify-platform-billing.mjs` (جديد) ·
`scripts/verify-platform-console.mjs` (سجلّ الإعدادات صار 14 + مفاتيح الفوترة) ·
`docs/STATUS.md` · `docs/roadmap/{README,PLATFORM_CONSOLE_PLAN,INCOMPLETE_INVENTORY}.md` ·
`apps/platform-admin/README.md`
