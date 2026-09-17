# الموقع التسويقي ونظام إدارة المحتوى — P-M1 + P-M2 + P-M5

> تاريخ التنفيذ: 2026-09-17/18 · الفرع: `arena/01a0acbb-cloud-saas-erp`
> المرجع الحاكم: [`docs/roadmap/MARKETING_SITE_PLAN.md`](roadmap/MARKETING_SITE_PLAN.md) (§3 المبادئ · §5 الأجزاء · §6 تفصيل CMS · §11 بوابة القبول)

## الأرقام أولاً

| | قبل هذه الجلسة | بعدها |
|---|---|---|
| **API** | 1165 اختباراً في 138 ملفاً | **1191 في 140 ملفاً** (`public-content.spec.ts` 10 · `platform-content.spec.ts` **16**) |
| **contracts** | 167 في 19 ملفاً | **167 في 19 ملفاً** (بلا اختبار جديد — والعقود نمت: `platform/content.ts` 486 سطراً) |
| **platform-admin** | 24 (مسارات 31) | **24** (مسارات **32** — أُضيف `/content`) |
| **staff** | 37 | **37** (بلا مسّ) |
| **`apps/marketing`** | 3 مسارات تشغيلية بلا اختبارٍ للعرض | **18 اختباراً في 3 ملفات** · 27 ملفّ صفحة · 12 مساراً عربياً و10 إنجليزية |
| **OAS** | 546 مساراً | **564 مساراً · 738 عملية** (`/platform/*` 112 · `/public/*` 7) |
| **سكربتات التحقّق** | 37 | **39**: `verify-content.mjs` (جديد · **62/62** في 7 أقسام) · `verify-marketing-site.mjs` (جديد · **37/37** في 4 أقسام) |
| **`verify-platform-console.mjs`** | 218/218 | **224/224** (‏§2 صار يقرأ أربعة مسارات محتوى، و§4 يقيس 23 رمزاً) |
| **الترحيلات** | 77 | **79** (`0077_content.sql` · `0078_content_permissions.sql` + تراجعاهما) |
| **أدوار المنصة (رموز اللوحة)** | 21 | **23** (`console.content.view` · `console.content.manage`) |

كل البوابات خضراء: `tsc` للحزم الثلاث ✅ · `eslint` لـapi/contracts/marketing/platform-admin ✅ ·
بناء packages + api + platform-admin + **marketing الإنتاجي** ✅ (‏`NEXT_DIST_DIR=.next-build` فلا
يتعارض مع خادم التطوير).

## 1. ما طُلب وما سُلِّم

الطلب: **«ابدأ بالموقع التسويقي مع نظام إدارة المحتوى. ثم التقرير الأسبوعي بالبريد»**. نُفِّذ
الجزءان الأولان بنطاقهما من الخطة: **P-M1** (الأساس والتصميم وSEO) و**P-M2** (الرئيسية والوحدات)
و**P-M5** (نظام إدارة المحتوى كاملاً). والباقي (P-M3 · P-M4 · P-M6 … P-M10) والتقرير الأسبوعي
موضعُ تفصيلٍ في §8.

## 2. الموقع التسويقي — P-M1 وP-M2

**القشرة** (`components/site/shell.tsx`): رأسٌ بشعارٍ من `GET /public/site` وقائمةٍ تُقرأ من
`content_menus` مع بديلٍ افتراضي، ومبدّل لغة، وتذييلٌ بأربع مجموعات، **ولافتةٌ** إن كانت هناك
لافتةٌ معروضة. والشعار والنصوص من إعدادات المنصة (`site.*` — ثمانية مفاتيح أُضيفت إلى كتالوج
`console.ts`)، فلا نصّ مطبوع في الكود لِما يجب أن يُدار من اللوحة.

**اللغتان**: العربية بلا بادئة والإنجليزية بـ`/en`، و`<html lang dir>` يُضبطان من المسار نفسه،
و`hreflang` (ar · en · x-default) و`canonical` يُبنى من `contentPathOf` — دالّة واحدة يستعملها
الموقع وخريطة الموقع، فلا يختلف رابطٌ عن رابط. واللغة تُقرأ من ترويسة `x-pathname` التي يضعها
`middleware.ts`، لأن ميتاداتا الصفحة تُحسب في مسارٍ لا يرى فيه `params` الجذر.

**الصفحات**: الرئيسية · الوحدات `/features` (+ `/features/einvoicing`) · المدوّنة `/blog` و`/blog/[slug]` ·
دراسات الحالة `/cases` و`/cases/[slug]` · مركز المساعدة `/help` و`/help/[slug]` (ببحثٍ `?q`) ·
`/legal/[slug]` · والأسطح التشغيلية القائمة (`/pricing` · `/onboarding` · `/login` · `/verify` ·
`/contact` · `/maintenance`).

**SEO**: `generateMetadata` لكل صفحة (عنوان · وصف · Open Graph · Twitter) · `sitemap.ts` و`robots.ts`
مبنيّان من `GET /public/sitemap` و`robotsRules` · **JSON-LD** لأربعة أنواع (`Organization` ·
`SoftwareApplication` · `Article` · `FAQPage`) — **بلا `aggregateRating` مُخترع** (يقيسه السبيك).

**404 الصحيحة**: Next 15 لا يُصدر حالة 404 لصفحةٍ ديناميكية تبدأ تصييرها قبل قرار الصفحة (أُثبت
بمجسّ: `notFound()` ⇒ 200). الحلّ المعتمد: `middleware.ts` يسأل `GET /public/content/:slug` لمسارات
المحتوى وحدها، وغير المنشورة تُعاد كتابتها إلى `/not-found-view/{ar|en}` **بحالة 404**، وصفحة
`app/not-found.tsx` ترسم النصّ بلغة المسار وتُوسم `noindex`. النتيجة الحيّة: `/blog/zzz` و
`/en/blog/zzz` و`/safha-la-tujad` = **404**، والعنوان الإنجليزي في المسار الإنجليزي.

**نقاط النهاية العامّة سبع**: `/public/{site,sitemap,posts,help,faq,banners,content/:slug}`.

## 3. نظام إدارة المحتوى — P-M5

**النموذج** (`0077_content.sql`): `content_pages` · `content_blocks` · `content_menus` ·
`content_banners` · `content_versions` — بخمسة جداول وRLS مُفعَّل ومفروض، وسياسة `platform_admin_plane`،
وقيودٍ مكتوبة (‏`content_banners_window_check`: النافذة تنتهي بعد أن تبدأ · `text` 3..200 · slug
بنمط ASCII).

**الأنواع الستّة**: `page` · `post` · `case_study` · `faq` · `help` · `legal` — ولكلٍّ مسارٌ عام
تشتقّه `contentPathOf` (`/blog/:slug` · `/cases/:slug` · `/help/:slug` · `/legal/:slug` · `/:slug`).

**الكتل الأحد عشر**: heading · text · list · cards · table · faq · quote · image · video · code ·
cta — **لا HTML حرّاً**، ولكلّ نوعٍ مخطط Zod يتحقّق عند الكتابة، ويُعاد التحقّق نفسه في شاشة
اللوحة قبل الإرسال. والتعديل **بديلٌ كامل** للكتل (`blocks` إن أُرسلت) لا دمج، لأن الدمج يجعل حذف
كتلةٍ مستحيلاً من الواجهة.

**نقاط اللوحة أربعة عشر**: `GET /platform/content/{pages,categories,menus,banners}` ·
`POST/PATCH /platform/content/banners` · `PUT /platform/content/menus/:position` ·
`POST /platform/content/pages` · `GET/PATCH /platform/content/pages/:id` ·
`POST …/publish` · `POST …/retract` · `GET …/versions` · `POST …/versions/:version/restore`.

**الصلاحيتان**: `console.content.view` (يقرأ) و`console.content.manage` (يكتب وينشر ويسحب) —
للمالك الرمزان · للتشغيل الرمزان · للدعم `view` وحده · وللمدقّق `view` وحده.

**النشر المجدول**: الجدولة تكتب مهمّة `content.publish` على طابور `maintenance` بوقت التنفيذ،
**ومسح `publishDue` idempotent** يُنادى قبل كل قراءةٍ عامّة فينشر ما استحقّ وقته حتى بلا عامل
(إذ لا Redis في كل بيئة). و`publishNow` يكتب فاعلاً `actorUserId = null` واسم «نظام الجدولة» في
التدقيق — فلا يُنسب فعلُ الآلة إلى إنسان.

**السحب لا الحذف**: لا مسار `DELETE` أصلاً؛ الصفحة المنشورة تعود مسوّدةً **بسببٍ مكتوب (3..300)**
يبقى في التاريخ، والرابط يبقى محجوزاً كي لا يشير رابطٌ قديم إلى محتوى آخر. والاستعادة تُنتج نسخةً
جديدة قبل الكتابة، فلا يُمحى شيء.

**إصلاحٌ لازم كشفه التحقّق**: `publicPosts` كان يقيّد `kind = 'post'` ثابتاً ⇒ `/cases` فارغة أبداً.
صار `kind` **معاملاً** (افتراضيه `post` من المخطّط) — وهو نفس مسار القراءة الذي تبني به الرئيسية
«قالوا عن النظام».

## 4. شاشة `/content` في اللوحة

أربعة تبويبات على أربعة أسئلة، وكلها تنادي نقاطاً حقيقية:

1. **الصفحات** — ترشيحٌ (النوع · الحالة · بحث · ترتيب)، وإنشاء مسوّدة، وتحريرٌ بأحد عشر حقلاً
   (عنوانان · ملخّصان · تصنيف · كاتب · ثلاثة حقول SEO بلغتين · صورة مشاركة)، ونشرٌ فوريّ،
   **وجدولةٌ بوقتٍ يُرسل بإزاحة صريحة** (`toIsoWithOffset` — `datetime-local` بلا منطقة زمنية)،
   وسحبٌ بسبب، وتاريخٌ بالنسخ والاستعادة.
2. **الكتل** — اختر صفحة ⇒ كتلها مرتّبة، ولكل كتلةٍ محرّران (عربية/إنجليزية) يُحقَّقان محلياً
   بمخطّط النوع نفسه، مع ترتيبٍ وحذفٍ وإضافةٍ من أحد عشر قالباً ابتدائياً صالحاً، والحفظ يرسل
   البديل الكامل.
3. **القوائم** — المواضع الخمسة (رأس · تذييل · جانبي · قانوني · تواصل): إضافةٌ وترتيبٌ وتسميتان
   ووسم، والحفظ `PUT`.
4. **اللافتات** — إنشاءٌ بنافذةٍ وجمهورٍ ولون، وإيقاف/تفعيل، وتحرير، ووسم «معروضة الآن» يأتي
   **من الخدمة** لا من الشاشة.

## 5. ما اخترعناه (تسميات)

| ما اخترعناه | لماذا |
|---|---|
| **المحتوى** (بند القائمة) | الخطة تسمّي الجزء «نظام إدارة المحتوى»، والبند في قائمة قصيرة يحتاج مفردةً واحدة |
| **الصفحات · الكتل · القوائم · اللافتات** | أسماء الخطة نفسها للشاشات (`pages` · `blocks` · `menus` · `banners`) |
| **النسخ** (بدل «الإصدارات») | «نسخة» هي ما يقوله التدقيق والاستعادة في المنتج كله |
| **سحبٌ من النشر** | الفعل في الخطة `retract`؛ و«سحب» تفرّقه عن «حذف» الذي لا وجود له |
| **معروضة الآن** | ترجمة `live` — وسمٌ يقول الحقيقة المحسوبة لا نيّة التشغيل |

## 6. أخطاء حقيقية كشفها التشغيل والسبك

| # | الخطأ | الإصلاح |
|---|---|---|
| 1 | `generateMetadata` في صفحة الوجهة لا يُطبَّق (ميتاداتا 404 خرجت بلغةٍ واحدة) | صفحة `app/not-found.tsx` بعنوانٍ محايد ووسم `noindex`، واللغة للرسم من `x-pathname` |
| 2 | ‏`matchAll` يرفض نمطاً بلا علم `g` ⇒ سكربت التحقّق يسقط | دالّة `all()` تلفّ النمط |
| 3 | **`PATCH` لافتة بـ`endsAt` وحده ⇒ 500 INTERNAL** (قيد `content_banners_window_check` يرفض الصفّ بعد الكتابة) | مدقّقة في العقد (`refine`) + فحص النافذة **الفعّالة** في الخدمة (`starts_at` المخزّن) ⇒ 400/422 بوصفٍ عربي |
| 4 | `content.service.ts` يستورد `../platform/auth/auth-context.js` غير الموجود | المسار الصحيح `../../request-context/request-context.js` |
| 5 | نوع `rows[0].next` بلا فهرسة ⇒ خطأ بناء | `rows[0]?.next ?? 1` |
| 6 | عدّاد السبيك مثبَّت على 19 مفتاحاً بعد إضافة `site.*` | 27 مع تعليقٍ يسمّي المفاتيح الثمانية |
| 7 | عدّاد سكربت اللوحة مثبَّت على 21 رمزاً و19 مفتاحاً | 23 رمزاً و27 مفتاحاً + فحصان للرمزين الجديدين |
| 8 | قاعدة المال في eslint اصطادت عدّادات بريئة (`total` · `tenantCount`) | **إعادة تسمية** (`pageCount` · `matched` · `tenantCount`) لا تعطيل القاعدة |
| 9 | استيرادات ميتّة في المتحكّمين (`ContentRestore` · `_Keep`) | أُزيلت والأنواع صارت في توقيعات الدوال |

## 7. التحقّق الحيّ

| السكربت | النتيجة | ما يقيسه |
|---|---|---|
| `scripts/verify-content.mjs` (جديد) | **62/62 في 7 أقسام** | الرمزان ومن دونهما · القراءة والترشيح · دورة حياة كاملة (مسوّدة ← مجدولة ← منشورة ← مسحوبة) + مهمّة الطابور بوقتها + النسخ والاستعادة · الكتل (حمولة مكسورة تُرفض والبديل الكامل) · القوائم ووصولها إلى `/public/site` · اللافتات (النافذة · الجمهور · الإيقاف · النافذة المعكوسة) · التنظيف. **يعيد التشغيل بلا أثرٍ متراكم** (تشغيلٌ ثانٍ: 62/62) |
| `scripts/verify-marketing-site.mjs` (جديد) | **37/37 في 4 أقسام** | القشرة واللغتان (12 مساراً + 5 إنجليزية + 404) · SEO (عنوان · وصف · canonical · hreflang · JSON-LD) · الخريطة وrobots · **من اللوحة إلى الموقع**: كتابةٌ بالـAPI ⇒ 404 قبل النشر ⇒ ظهورٌ في المدوّنة وصفحتها و`sitemap.xml` ⇒ اختفاءٌ بعد السحب |
| `verify-platform-console.mjs` | **224/224** | +4 مسارات محتوى في §2، و§4 يقيس 23 رمزاً ويقيس الرمزين الجديدين |

## 8. مؤجَّلٌ صراحةً

- **P-M3** (الباقات والأسعار بصفحةٍ كاملة) · **P-M4** (الاشتراك والتفعيل) · **P-M6** (العملاء
  المتوقّعون) · **P-M7** (الحملات) · **P-M8** (التحقّق والثقة والقطاعات) · **P-M9** (مركز
  المساعدة والوثائق وحالة الخدمة) · **P-M10** (القياس والتحسين).
- **`GET /public/testimonials`**: لم يُنشأ — «قالوا عن النظام» تُقرأ من `/public/posts?kind=case_study`
  (مصدرٌ واحد للشهادات وقصص العملاء، وقرارٌ مصرَّح به).
- **التقرير الأسبوعي بالبريد** (المؤجَّل من P-C12، وهو الشقّ الثاني من طلب المستخدم): يُنفَّذ بعد
  هذه الدفعة — البنية جاهزة (قالب البريد في P-C6 + ماسح المهامّ في P-C9).

## 9. الملفات

**جديد**: `packages/database/migrations/0077_content.sql` (+down) · `0078_content_permissions.sql`
(+down) · `packages/contracts/src/platform/content.ts` · `apps/api/src/modules/content/{content.module,content.service,public-content.controller,platform-content.controller}.ts` ·
`apps/api/test/{public-content,platform-content}.spec.ts` · `apps/platform-admin/app/content/page.tsx` ·
`apps/marketing/**` (27 ملفّ صفحة + `lib/{i18n,site,content,navigation,meta,modules}.ts` +
`components/site/*` + `middleware.ts` + `app/not-found.tsx` + `tests/site.spec.ts`) ·
`scripts/{verify-content,verify-marketing-site}.mjs` · هذا التقرير.

**معدَّل**: `app.module.ts` · `permissions.ts` · `rbac.ts` · `platform/console.ts` · `platform/index.ts` ·
`errors.ts` · `platform/jobs.ts` · `apps/platform-admin/{lib/navigation.ts,tests/routes.spec.ts}` ·
`apps/api/test/{platform-console-rbac,platform-analytics}.spec.ts` · `scripts/verify-platform-console.mjs` ·
`docs/{STATUS.md,roadmap/README.md,roadmap/MARKETING_SITE_PLAN.md}`.
