# P-C11 — بوابة المطوّر

> الجزء الثاني عشر من [`roadmap/PLATFORM_CONSOLE_PLAN.md`](./roadmap/PLATFORM_CONSOLE_PLAN.md) §4.
> **الحالة: ✅ مُنجَز** (2026-09-17) على الفرع `arena/01a0acbb-cloud-saas-erp`.
> يعتمد على **P-C1** وحده (القشرة · الرموز · التدقيق العابر · `PlatformAdminGuard`) — كما قالت الخطة.
>
> **الأرقام بعد التنفيذ:** API **1155** اختباراً (137 ملفاً؛ كان 1139/136) · contracts **151**
> (18 ملفاً؛ كان 139/17 — منها **12** لعقود المطوّر) · platform-admin **24** · staff **37** ·
> `platform-developer.spec.ts` **16** (الخطة طلبت ≥ 10) · `scripts/verify-platform-developer.mjs`
> **63/63** في **9** أقسام (سكربتٌ جديد) · `verify-platform-console.mjs` **218/218** في **22**
> قسماً (كان 200/21) · والمجموع الحيّ للوحة **861** نقطة في **97** قسماً (كان 780/87) · ترحيل واحد
> (`0075_developer_platform.sql` + ملف تراجع، والتراكمي **76**) · **12** مساراً جديداً تحت
> `/platform/*` (التراكمي **119**) و**3** مسارات تحت `/integration/v1/*` · **3** شاشات في اللوحة
> (التراكمي **30**) · رمزان `console.*` جديدان (`console.apikeys.manage` · `console.webhooks.manage`،
> المجموع **20**) · **4** جداول جديدة · **5** مُنتِجين للأحداث من مساراتٍ حقيقية.

---

## 0. بوابة «المصدر بالسطر»

| الشاشة/السلوك | المصدر | السطر |
|---|---|---|
| «الهدف: تكامل رسمي بدل الأبواب الخلفية» | `docs/roadmap/PLATFORM_CONSOLE_PLAN.md` §4 «P-C11» | 276 |
| «`/api-keys` (لكل مستأجر: نطاقات · تدوير · آخر استخدام · إبطال) · `/webhooks` (الأحداث · العنوان · سرّ التوقيع · إعادة الإرسال · سجلّ التسليم بأكواد الاستجابة) · مستكشف OpenAPI · مستأجر تجريبي (sandbox)» | المرجع نفسه | 277 |
| «`GET/POST/DELETE /platform/tenants/:id/api-keys` · `GET/POST/PATCH/DELETE /platform/tenants/:id/webhooks` · `POST /platform/webhooks/:id/test` · `GET /platform/webhooks/:id/deliveries`» | المرجع نفسه | 278 |
| «الأحداث الأولى: `invoice.posted` · `invoice.paid` · `invoice.voided` · `stock.below_reorder` · `einvoice.submission_failed` · `shift.closed` · `subscription.*`» | المرجع نفسه | 279 |
| «صلاحيات: **`console.apikeys.manage`** · **`console.webhooks.manage`** (جديدان)» | المرجع نفسه | 280 |
| «ترحيل: `0073_developer_platform.sql` — `api_keys` (المفتاح مُجزَّأ، لا يُخزَّن نصّاً) · `webhook_endpoints` · `webhook_deliveries`» (والرقم صار **0075** لأن 0072/0073/0074 أخذها P-C8/P-C9/P-C10) | المرجع نفسه | 281 |
| «اختبار: `platform-developer.spec.ts` (≥ 10): توقيع صالح، إعادة إرسال، إبطال مفتاح، عزل» | المرجع نفسه | 282 |
| الجدول التراكمي وعدّاد الجزء (10 اختبارات · +25 نقطة) | المرجع نفسه | 314-315 |
| ترتيب الأجزاء: «12 المطوّرون (مفاتيح · ويب هوكس) P-C11» | المرجع نفسه | 65-66 |
| جدول الترحيلات: `0075_developer_platform.sql` | المرجع نفسه | 349 |
| 📌 «حذف/تصدير البيانات الشخصية (طلبات)» والرموز المستعملة | `docs/roadmap/INCOMPLETE_INVENTORY.md` | 132 |
| ترتيب الحُرّاس: `AuthGuard → TenantGuard → PermissionsGuard` وسياسة `@Public()` بالاسم | `apps/api/test/permission-codes.spec.ts` | 80-170 |
| الحارس الذي تُبنى عليه البوابة (`@RequiresPlatformRole` + الأدوار من الرمز) | `apps/api/src/modules/platform/guards/platform-admin.guard.ts` | 1-90 |
| نقش الرموز في الرمز نفسه (`TokenService.sign/verify`) | `apps/api/src/modules/platform/identity/token.service.ts` | 40-140 |
| منح التسلسلات الافتراضي (سبب خطأ الـ500 المشخَّص) | `packages/database/migrations/0000_platform_identity.sql` | 258-262 |
| حارس المال في eslint (تسمية `amount`/`total` في موضع القيمة) | `eslint.config.mjs` | 21-27 |
| نمط شاشات اللوحة (`Screen` · `Empty` · `Forbidden` · `useQuery` بحالاته الأربع) | `apps/platform-admin/components/screen.tsx` · `lib/use-query.ts` | 1-80 · 1-40 |

---

## 1. ما كان مكسوراً — بالقياس لا بالرواية

### 1.1 التكامل كان **باباً خلفياً**: جلسةُ إنسانٍ تُشارَك، أو لا تكامل

قبل هذا الجزء لم يكن في المنصّة **أي مفهومٍ لمفتاح API**: لا جدول، لا حارس، لا نطاق، ولا رمز
صلاحية يحكمه. فمن أراد أن يربط نظامه بالـERP كان أمامه واحدٌ من طريقين: مشاركةُ بريد وكلمة مرور
موظّف (فلا تُعرَف مَن استعمل ماذا، ولا يُسحَب الوصول إلا بتغيير كلمة المرور)، أو كتابةُ سكربتٍ
يُسجّل الدخول بالجلسة نفسها. وهذا ليس «تكاملاً» — إنه جلسةُ إنسانٍ تتنكّر في هيئة نظام.

### 1.2 الأحداث كانت **تصمت**: لا عقد يخرج، ولا قياس يبقى

وُجد في المنصّة ويب هوك **داخلٌ** (سلة، P-C… قائم) وطابورُ مهام (P-C9)، لكن لم يكن لأحداث
العميل **مخرَج** بعقدٍ ثابت: من أراد أن يعرف أن فاتورةً رُحّلت أو أُلغيت كان يستعلم دورياً. ولذلك
حين يُبنى المخرَج بلا قياس (رمز استجابة، محاولات، زمن، خطأ) يصير «الويب هوك» وعداً لا يُعرف
هل وصل — وهي الكذبة نفسها التي رفضها P-C7 في الإعلانات: خانةٌ تُشترى ولا تأتي.

### 1.3 لا وثيقة للعقد: المسارات موجودة والمواصفة غائبة

`/api/docs` كان يخدم الوثيقة، لكن لا شاشة في اللوحة تُعطي المطوّر ما ينشره الخادم فعلاً، ولا
مساراً يقول له «هذه نطاقات مفتاحك، وهذه صيغة التوقيع، وهذا ما نعطّله بعد فشلٍ متكرّر». فالمعرفة
كانت تُنقل بالمراسلة، لا تُقرأ من الخادم.

---

## 2. ما بُني

### 2.1 العقود — `packages/contracts/src/platform/developer.ts` (جديد)

**8 نطاقات** (`invoices:read` · `invoices:write` · `inventory:read` · `inventory:write` ·
`parties:read` · `parties:write` · `reporting:read` · `webhooks:manage`) — قدراتٌ على موارد
المستأجر مكتوبةً `resource:action`، و**لا تشمل `console.*` بحال**: مفتاحُ عميلٍ يُعرّف نفسه
لمنشأته ولا يفتح سطح المنصة. و**9 أحداث** قابلة للاشتراك، و**حدثُ اختبار** `webhook.test`
**ليس منها** (يُرسل من الزرّ ولا يُشترَك به). وثوابت التوقيع
(`WEBHOOK_SIGNATURE_HEADER = 'x-erp-signature'` · `WEBHOOK_SIGNATURE_TOLERANCE_SECONDS = 300`)
والأوراق (`apiKeyRowSchema` · `apiKeyCreatedSchema` · `webhookEndpointRowSchema` · `webhookDeliveryRowSchema`
· `webhookAttemptSchema` · `developerCatalogueSchema`) و`developerAuditActions` الثمانية.
و`webhookUrlSchema` تشترط `https` وتستثني `http` على `localhost`/`127.0.0.1` صراحةً — لأن الويب
هوك يحمل بيانات عميل، والسرّ يمنع التزوير لا التنصّت.

### 2.2 الترحيل `0075_developer_platform.sql` (+ملف تراجع)

**4 جداول**: `api_keys` (البادئة + بصمة SHA-256 + النطاقات + `last_used_at/ip` + `expires_at` +
`revoked_at/revoked_reason` — **ولا عمود للنصّ الصريح**) · `webhook_endpoints` (العنوان ·
الأحداث · الحالة · **السرّ مشفَّراً** `secret_encrypted` · `secret_prefix`) · `webhook_deliveries`
(الحدث · الحمولة · الحالة · `attempts`/`max_attempts` · `response_code`/`body` · `duration_ms` ·
`error` · `next_attempt_at` · `delivered_at`) · `api_key_uses` (سجلّ الطلبات المقبولة).
RLS مُفعَّل ومفروض على الأربعة بسياسة `platform_admin_plane`، ومنح `erp_api` — **بلا `DELETE` على
`api_keys`** (الإبطال وسمٌ لا حذف) وبـ`DELETE` على `webhook_endpoints` وحدها، و**منحٌ صريح على
تسلسل `api_key_uses_id_seq`** (تفصيله في §6.3)، وصفّان في `permissions` للرمزين الجديدين،
و`ALTER ROLE erp_api NOBYPASSRLS`.

### 2.3 المسارات — 12 في اللوحة و3 في سطح التكامل

`platform-developer.controller.ts`: بأسماء الخطة حرفاً بحرف
(`GET/POST/DELETE /platform/tenants/:id/api-keys` · `GET/POST/PATCH/DELETE /platform/tenants/:id/webhooks`
· `POST /platform/webhooks/:id/test` · `GET /platform/webhooks/:id/deliveries`) **زائد ثلاثة**
لكلٍّ سببٌ لا يُستغنى عنه: `POST /platform/tenants/:id/api-keys/:keyId/rotate` (التدوير ليس
إنشاءً ثانياً: القديم يُبطَل والجديد يرث الاسم والنطاقات في معاملةٍ واحدة)،
`POST /platform/webhooks/:id/deliveries/:deliveryId/retry` (الإعادة إجراءٌ يُنفَّذ الآن لا صفٌّ
يُعاد جدولته)، و`GET /platform/developer/catalogue` (ما يقول الخادم عن نفسه: النطاقات والأحداث
وصيغة التوقيع ومهل الإعادة).

و`integration.controller.ts` — **سطحٌ ثالث** جديد `@Controller('integration')` بحارسٍ واحد
`ApiKeyGuard` و`@Public()` على الصنف (فيمرّ من الحُرّاس العامّة ولا يعني «مفتوحاً»): `GET /integration/v1/me`
(هوية المفتاح ونطاقاته وصلاحياته) · `GET /integration/v1/invoices` (بنطاق `invoices:read` — عيّنةٌ
تُثبت أن النطاق يعمل على بياناتٍ حقيقية) · `GET /integration/v1/signature`.

### 2.4 القرارات الحاكمة في الخدمة

- **المفتاح لا يُخزَّن نصّاً.** النصّ `erp_live_<24B base64url>` يُعاد مرّةً واحدة؛ والمحفوظ
  `prefix` + `hash` (SHA-256) ويُقارَن بـ`timingSafeEqual`. ومن نسي مفتاحه يُدوّره — لا «أرسل
  لي المفتاح مرّة أخرى».
- **السرّ يُشفَّر لأننا نقرؤه.** سرّ الويب هوك (`whsec_<uuid>`) يُخزَّن مشفَّراً (`sealSecret`/`openSecret`)
  لا مُجزَّأً: المُرسِل يحتاج قراءته ليوقّع، بخلاف مفتاح الـAPI الذي يُقارَن ولا يُقرأ. الفرقان
  مكتوبان في المخطّط لا في تعليق.
- **التوقيع بنافذة.** `x-erp-signature: t=<ثواني>,v1=<hex>` حيث `v1 = HMAC-SHA256('<t>.<body>')`
  بسرّ العنوان، ونافذة القبول **300** ثانية. الطابع الزمني **داخل** المُوقَّع: بدونه يصحّ التوقيع
  إلى الأبد، ومن التقط حمولةً مرّةً يعيد إرسالها كل يوم.
- **التسليم يسبق الطلب.** يُكتب صفّ `pending` **قبل** الـPOST، فانقطاعُ الخدمة في منتصف الإرسال
  لا يمحو الواقعة؛ والفشل يُجدوَل بتراجع `60 / 300 / 1800` ثانية، ويُنفَّذ من نفس ماسح المهامّ
  (`scanPending` ≤ 20)، والإرسال الاختباري `maxAttempts = 1` لأنه إجراءٌ ينتظر جواباً الآن.
- **زرّ الاختبار يمرّ من مسار الإرسال نفسه**: يوقّع، ويُرسل POST، ويقيس — ثم يعيد للمشغّل رمز
  الاستجابة والزمن. ولا يوجد «زرّ يوهم بالسلامة».
- **الإيقاف يمنع التلقائي لا اليدوي.** الأحداث الحقيقية لا يُنشأ لها صفّ تسليم لعنوانٍ موقوف (يُقاس
  في السبيك: صفر صفّ)، ويبقى «اختبار» متاحاً لأن من أصلح مستقبِلَه للتوّ يقيسه قبل أن يُعيد التشغيل.
- **الصلاحيات نطاقاتٌ تُترجم**: `invoices:read → sales.view` · `invoices:write → sales.invoice.create/post/void/pay`
  + `sales.return.create` + `sales.adjustment.create` · `inventory:write → inventory.adjust` + `inventory.transfer`
  · إلخ. فالحارس ينشر سياقاً حقيقياً (`userId = keyId`، `membershipId/tokenId = api-key:<id>`)
  وتقرأ الخدمات تحت العزل نفسه الذي يقرأ به الموظّف — لا مسارٌ موازٍ بلا RLS.

### 2.5 المُنتِجون الخمسة (أحداثٌ من مساراتٍ حقيقية لا من زرّ)

`WebhookPublisher.emit(event, tenantId, payload)` — **لا يرمي أبداً** ويعيد عدد العناوين التي
وُجد لها صفّ، فلا يسقط فعلٌ تجاري لأن ويب هوك عميلٍ تعطّل. ومُنتِجوه:

| الحدث | المصدر | الحمولة |
|---|---|---|
| `invoice.posted` / `invoice.voided` / `invoice.paid` | `sales/sales.service.ts` | `invoiceId` · `number` · `kind` · `status` · `total` · `currency` · `partyId` · `branchId` (+`reason` في الإلغاء، و`paymentId/method/amount/paidTotal` في الدفع) |
| `shift.closed` | `treasury/treasury.service.ts` | `shiftId` · `number` · `countedCash` · `diff` · `closedAt` |
| `einvoice.submission_failed` | `einvoicing/einvoicing.service.ts` | `invoiceId` · `submissionId` · `authority` · `environment` · `status` · `error` |
| `stock.below_reorder` | `inventory/inventory.service.ts` | `itemId` · `warehouseId` · `sku` · `nameAr` · `quantity` · `minQty` (مرّةً لكل صنفٍ ومخزن، ويتجاهل `minQty ≤ 0`) |
| `subscription.activated` / `plan_changed` / `suspended` | `platform/admin/platform-billing.service.ts` | `subscriptionId` · `status` · `plan` · `reason` · `pausedAt` |

### 2.6 ثلاث شاشات في اللوحة

- **`/api-keys`** — اختيار المنشأة · إصدار مفتاح (اسم · نطاقاتٍ من الكتالوج الحيّ · انتهاء) ·
  بطاقة «النصّ يظهر مرّة واحدة» · جدولٌ يقول البادئة والنطاقات والحالة وآخر استخدام وعدد الطلبات
  (الإجراءات: **تدوير** و**إبطال** بسببٍ ≥ 5 يُكتب في التدقيق).
- **`/webhooks`** — إضافة عنوان (الأحداث المُشترَك بها) · بطاقة السرّ مرّةً واحدة بصيغة التوقيع ·
  جدولٌ بحصيلة التسليمات وآخر ردّ · سجلّ تسليمٍ بالرمز والزمن ومفاتيح الحمولة والخطأ مع «إعادة
  الإرسال» على الفاشل.
- **`/api-explorer`** — الوثيقة **كما ينشرها الخادم**: يقرأ `/api/docs/openapi.json` عبر أصل
  اللوحة (لا CORS ولا منفذٌ مفتوح في المتصفّح) ويعرض المسارات مجموعةً بحسب سطح العقد (تكامل ·
  بوابة المطوّر · المنصة · العميل) مع بحث، ورابط «واجهة Swagger الكاملة». **لا نسخة ثانية من
  الوثيقة تُكتب في الشاشة** — تُقرأ من الخادم فلا تتخلّف عنه.

وبندٌ ثلاثة في شريط اللوحة (`lib/navigation.ts` — مجموعة «المنصة»)، و`routes.spec.ts` يشترط
وجود ملف صفحةٍ لكل مسار.

### 2.7 التحقّق الحيّ: سكربتٌ جديد وقسمٌ جديد

- `scripts/verify-platform-developer.mjs` — **9 أقسام / 63 نقطة**: الأبواب · الإصدار والنصّ
  مرّةً واحدة · سطح التكامل والنطاق المنتهي · النطاق سقفاً (403 يقول أيّ نطاقٍ ينقص) · التدوير ·
  الإبطال · التسليم بتوقيعٍ يُتحقَّق منه في السكربت نفسه على مستقبِل HTTP حقيقي، ثم تعطيل
  المستقبِل (500) فيُسجَّل الفشل، ثم إعادة الإرسال فتنجح · العزل · التدقيق بالأسماء الثمانية.
- `scripts/verify-platform-console.mjs` §21 (جديد) — **18 نقطة**، ومنه فحصٌ كشف حقيقةً سلوكية:
  **الأدوار تُقرأ من الرمز لا من القاعدة**، فجلسةُ عميلٍ أُصدرت قبل منحه دور «التشغيل» تُردّ 403
  وإن كان `/me` يقول إنه يحمل الرمز — والجلسة التي أُصدرت بعده تمرّ 200.
- `packages/contracts/src/platform/developer.spec.ts` — **12 اختباراً** تقيس الحدود من الجهتين
  (اسمٌ قصير · نطاقٌ مجهول · انتهاءٌ 0 و3651 · `http` عامّ · `webhook.test` غير قابل للاشتراك ·
  معرّف منشأةٍ في الجسم).

---

## 3. القرارات المصمَّمة (بأسبابها)

1. **سطح ثالث للتكامل بدل تمرير المفتاح على مسارات العميل.** مسارات العميل عقدُ واجهة مستخدم
   (جلسة، رمزٌ ينتهي، شكلُ شاشة)، والتكامل عقدٌ يعيش سنوات ويُقاس بسقفٍ اشتراه العميل. الفصل
   صيانةٌ لا تفخيم — ولذلك سُمّي `v1` صراحةً.
2. **مفتاحٌ بنطاقٍ لا مفتاحٌ بكل شيء.** سقفٌ صريح يجعل الرفض قابلاً للشرح (403 يقول النطاق
   الناقص)، ويجعل التسريب محصوراً بموردٍ واحد.
3. **السرّ مشفَّر والمفتاح مُجزَّأ** — لأن الأول يُقرأ عند التوقيع والثاني يُقارَن عند المصادقة.
   تشفيرُ المفتاح بلا حاجة يوسّع سطح الأسرار، وتجزئةُ السرّ تجعل التوقيع مستحيلاً.
4. **الطابع الزمني داخل المُوقَّع** + نافذة 5 دقائق: يمنع إعادة تشغيل حمولةٍ مُلتقطة، ويجعل
   الساعة جزءاً من العقد (ومعروضاً في الشاشة).
5. **الحكم يكتبه المُرسِل، والسجلّ يحكي لا يُجمّل**: `delivered/failed/pending` من نتيجة الـPOST
   الفعلية، و`responseCode` قد يكون `null` إن لم يُجب العنوان أصلاً — والفرق بين «ردّ 500» و«لم
   يجب» هو قرارُ مشغّلٍ لا تفصيل.
6. **حمولة التسليم يُعرض مفاتيحُها لا قيمُها** — نفس قرار مهامّ P-C9: بيانات العميل تُقرأ عنده،
   والسجلّ يجيب «هل وصل» لا «ماذا كان».
7. **الإبطال لا الحذف**، والصفّ يبقى بسببه؛ ومن أراد إخفاء المعرفة فليقرأ سجلّ التدقيق لا ليحذف
   سطراً منه.
8. **الكتالوج من الخادم** (`GET /platform/developer/catalogue`) والشاشة تقرأ منه: نطاقٌ يُضاف في
   العقد يظهر في الواجهة بلا نشر واجهة.

---

## 4. ما لم يُنفَّذ (بصراحة)

| البند | الحالة | السبب |
|---|---|---|
| **مستأجر تجريبي (sandbox)** | **مؤجَّل** | الخطة ذكرته بلا نقاط نهاية، وهو نطاقٌ قائم بذاته (إنشاء مستأجرٍ للتجربة + بيانات نموذجية + تنظيف دوري). لم يُنفَّذ هنا بدل تنفيذ «شبه» يكسر بيانات حقيقية. |
| **مستكشف OpenAPI «قابل للتجربة»** | لم يُنفَّذ | الشاشة تعرض العقود وتربط بواجهة Swagger؛ إرسال الطلبات من اللوحة بجلسة المنصّة يخالف فكرة أن المُختبَر مفتاحُ عميلٍ بنطاقٍ محدَّد. |
| **حصص/حدود معدّل للمفاتيح** | لم يُنفَّذ | تحتاج عدّاداً في مسارٍ حقيقي؛ P-C5 يقيس الاستخدام ويحسب تجاوزات العقد، وحصص المفاتيح تحتاج قرار تسعيرٍ قبل الكود. |
| **تبليغ العميل انتهاء مفتاحه** | لم يُنفَّذ (P-C6 تبني) | يحتاج قالباً وسياسة تكرار؛ سبق أن رُفض إدخال الإشعارات في أجزاءٍ سابقة بلا قالبٍ معتمد. |
| **توقيع داخلي لسلة** | خارج النطاق | موجودٌ من قبل (`integrations/salla`) ولم نمسّ عقده؛ الويب هوك الداخل يعتمد سرّ المتجر لا سرّ المنصة. |

---

## 5. «ما اخترعناه» — التسميات المخترعة وأسبابها

| المُصطلح | النصّ العربي | لماذا |
|---|---|---|
| `/api-keys` | «مفاتيح الـAPI» | الخطة قالت «api-keys» بلا عنوانٍ عربي؛ والبند في الشريط يحتاج اسماً يُقرأ. |
| `/webhooks` | «الويب هوك» | المصطلح مستعملٌ في المستودع نفسه (سلة)، ولذلك لم يُترجم إلى «إشعارات خارجية» فيُوهم أنهما شيءٌ آخر. |
| `/api-explorer` | «مستكشف الـAPI» | الخطة قالت «مستكشف OpenAPI»؛ والمعروض **ليس** الوثيقة الخام بل مستكشفٌ لها، ولم يكن في الديسكتوب ما يقابله. |
| `erp_live_…` | بادئة المفتاح | تمييزه في السجلّات عن غيره من الرموز، و`prefix` هو ما يُعرض ليتعرّف عليه صاحبه. |
| `whsec_…` | بادئة سرّ التوقيع | اصطلاحٌ مألوف (Stripe) يجعل السرّ معروفاً بنوعه في أي نصّ يظهر فيه — وأُعلن في الشاشة. |
| «تدوير» | rotate | فعلٌ واحد يفعل اثنين (إصدارٌ جديد + إبطال القديم)، و«تجديد» يوهم أن القديم يبقى صالحاً. |
| «يعمل / موقوف» | active/paused | «نشط» توحي بحركةٍ تحدث الآن؛ العنوان الساكن قد يكون سليماً. |
| «مفاتيح حمولة التسليم» | payloadKeys | ليست تسميةً مخترعة بل قرارٌ (يُعرض الشكل لا المحتوى) — تُكتب في الواجهة كي لا يُظنّ السجلّ ناقصاً. |

---

## 6. التحقّق

### 6.1 بوابات ثابتة (بعد آخر تعديل)

| البوابة | النتيجة | قبل هذا الجزء |
|---|---|---|
| `pnpm --filter @erp/api test` | **1155 / 137 ملفاً** ✅ | 1139 / 136 |
| `pnpm --filter @erp/contracts test` | **151 / 18 ملفاً** ✅ | 139 / 17 |
| `pnpm --filter @erp/platform-admin test` | **24** ✅ | 24 |
| `pnpm --filter @erp/staff test` | **37** ✅ | 37 |
| `platform-developer.spec.ts` | **16** (طُلب ≥ 10) ✅ | — |
| `developer.spec.ts` (contracts) | **12** ✅ | — |
| `tsc --noEmit` (api · platform-admin) | نظيف ✅ | نظيف |
| eslint (api · platform-admin · contracts) | **Exit 0** ✅ | api فيه 28 خطأً قائماً |
| بناء (packages · api · platform-admin) | **Exit 0** ✅ (29/29 صفحة) | — |

### 6.2 تحقّق حيّ على الخادم المحلّي

| السكربت | النتيجة | الملاحظة |
|---|---|---|
| `scripts/verify-platform-developer.mjs` (جديد) | **63/63** في 9 أقسام ✅ | مستقبِل HTTP حقيقي يُتحقّق من التوقيع داخل السكربت، ويتعطّل (500) فيُقاس الفشل ثم الإعادة |
| `scripts/verify-platform-console.mjs` | **218/218** في 22 قسماً ✅ | كان 200/21؛ §21 جديد (18 نقطة) وتصحيح عدّادَي الرموز 18 → 20 |
| المجموع الحيّ للوحة | **861** نقطة في **97** قسماً ✅ | كان 780/87 |
| شاشات `:3003` | `/api-keys` · `/webhooks` · `/api-explorer` → **200** ✅ | عبر الخادم الحقيقي |
| `/integration/v1/me` بمفتاح حقيقي | **200** مع `prefix` و`scopes` و`lastUsedAt` ✅ | بعد إصلاح منح التسلسل |

### 6.3 أخطاء حقيقية كشفها التشغيل الحيّ (وأُصلحت)

1. **500 على كل طلبٍ بمفتاح.** الجذر: `insert into api_key_uses` يسقط بـ`permission denied for
   sequence api_key_uses_id_seq` — لأن `0000_platform_identity.sql` منح «كل التسلسلات القائمة»
   يومها (س258) وضبط الصلاحيات الافتراضية **للجداول فقط** (س260)، فالتسلسل الذي وُلد في 0075 لم
   يُمنَح. الإصلاح: `GRANT USAGE, SELECT ON SEQUENCE api_key_uses_id_seq TO erp_api, erp_migrator`
   **داخل 0075 نفسه** (والترحيل غير مدفوع فجاز تعديله)، مع تعليقٍ يشرح السبب. ثم أُعيد تطبيقه
   حيّاً (إسقاط الجداول الأربعة + حذف صفّه من `erp_migrations` + `pnpm db:migrate`) وصار
   `/integration/v1/me` **200**. وهذا مثالٌ على خطأٍ لا يظهر إلا في تشغيلٍ حقيقي: اختبارات
   السبيك تُشغّل الترحيلات بترتيبها وبصلاحياتٍ أوسع فلا تراه.
2. **حدث الاختبار وصل لكن باسمٍ ضائع.** المستقبِل استقبل POST صحيحاً وموقّعاً، لكن الحمولة لم تكن
   تحمل اسم الحدث فسقط الفحص على `undefined`. الإصلاح: `payload` يحمل
   `{ event, tenantId, endpointId, at, via }` — كما تفعل حمولات الأحداث الحقيقية، فلا يقود
   المستقبِلَ منطقُه من ترويسةٍ قد تُهملها وسائط.
3. **أخطاء تهيئة في السبيك (لا في المنتج)**: `/platform/audit` يردّ `{items,…}` لا مصفوفة ·
   صلاحيات الأدوار في السبيك يجب أن تكون **كل** `permissionRegistry` وإلا سقطت خطواتٌ بـ403 ·
   ترحيل فاتورة يحتاج `OrgProvisioningService.provisionOrgDefaults` لكل مستأجر (وإلا
   `422 ACCOUNT_PROFILE_MISSING`) · وكل خطوة تجهيز صارت تُقاس بـ`checked()` فما ينجح صامتاً لا
   يظهر لاحقاً كخطأٍ مضلِّل.
4. **28 خطأً في eslint الـAPI**، أُصلحت كلها: 16 `import/order` بـ`--fix` (الاستيراد الجديد
   `../developer/…` موضعه قبل المتحكّمات) · 11 إخباراً من **حارس المال** (`amount`/`total` في موضع
   القيمة — عدّادُ صفوفٍ ومُعاملٌ نصّيّ) أُصلحت **بإعادة تسمية** (`matched` · `collected` · `billed`)
   لا بتعطيل القاعدة · ومتغيّرٌ غير مستعمل في السبيك (`operations`) **صار مستخدماً**: أُضيف فحصٌ
   حقيقي بأن دور «التشغيل» يقرأ مفاتيح المنشأة والكتالوج (200) — والرمز إنما وُجد ليُقاس.
5. **7 مخالفاتٍ قائمة في HEAD** (يتيمةٌ من أجزاءٍ سابقة) فُحصت بنسخ `git show HEAD:` مؤقتة
   وأُصلحت: 5 في `platform-billing.service.ts` + 2 في `platform-billing.spec.ts` (نفس تسمية
   المال) + سطرٌ فارغ في `platform-admin.guard.spec.ts` + 2 في `packages/contracts/src/platform/billing.ts`
   (معاملٌ اسمه `amount` نصّيُّ القيمة) مع تعليقَي تعطيل/تمكين صارا غير مستعملين. النتيجة: **eslint
   نظيف في الحزم الثلاث**، والعملية كلها إعادة تسميةٍ واحدة وثلاثة أسطر.

### 6.4 تصحيحٌ متقاطع (بلا مسّ منطق إنتاجي)

عدّادا الرموز في `verify-platform-console.mjs` §4 كانا **18**، وهما الآن **20**؛ والمقارنة صارت
تشترط أن الرمزين الجديدين **عند المالك والتشغيل وحدهما** — أي أن العدّاد صار يقيس السياسة لا
الرقم. وفحص «جلسة عميل» في §21 كان سيقيس **متغيّراً** لا قاعدة، فحُوّل إلى زوجٍ يقيس الحقيقة
السلوكية: جلسةٌ أُصدرت قبل المنح 403، وجلسةٌ أُصدرت بعده 200.

---

## 7. الملفات التي لمسها هذا الجزء

**جديد:** `packages/contracts/src/platform/developer.ts` + `developer.spec.ts` ·
`packages/database/migrations/0075_developer_platform.sql` + `.down.sql` ·
`packages/database/src/schema/developer.ts` ·
`apps/api/src/modules/developer/{developer.module,developer.service,webhook-publisher.service,api-key.ts,api-key.guard.ts,requires-api-key-scope.decorator.ts,platform-developer.controller,integration.controller}.ts` ·
`apps/api/test/platform-developer.spec.ts` · `apps/platform-admin/app/{api-keys,webhooks,api-explorer}/page.tsx` ·
`scripts/verify-platform-developer.mjs` · هذا التقرير.

**معدَّل:** `packages/contracts/src/{permissions.ts,errors.ts,rbac.ts,platform/index.ts}` ·
`packages/database/src/schema/index.ts` · `apps/api/src/app.module.ts` ·
`apps/api/test/test-app.ts` ·
`apps/api/src/modules/{sales,treasury,einvoicing,inventory}/…` (المُنتِجون) ·
`apps/api/src/modules/platform/admin/{platform-admin.module,platform-billing.service}.ts` ·
`apps/api/src/modules/platform/guards/platform-admin.guard.spec.ts` ·
`apps/api/test/platform-billing.spec.ts` · `packages/contracts/src/platform/billing.ts` ·
`apps/platform-admin/{lib/navigation.ts,next.config.mjs,tests/routes.spec.ts}` ·
`scripts/verify-platform-console.mjs` · `docs/STATUS.md` ·
`docs/roadmap/{README.md,PLATFORM_CONSOLE_PLAN.md}`.
