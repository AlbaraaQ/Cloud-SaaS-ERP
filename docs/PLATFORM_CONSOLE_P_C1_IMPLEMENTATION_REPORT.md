# P-C1 — الأساس والقشرة، وترميم الصلاحيات

> الجزء الأول من خطة لوحة تحكم المنصة ([`docs/roadmap/PLATFORM_CONSOLE_PLAN.md`](./roadmap/PLATFORM_CONSOLE_PLAN.md) §4).
> نُفِّذ وحده ولم يتجاوزه إلى P-C2 · تاريخ التنفيذ: 2026-09-17 · الفرع
> `arena/01a0acbb-cloud-saas-erp`.

---

## 0. بوابة «المصدر بالسطر» — كيف أُرضيت في لوحة لا مقابل لها في الديسكتوب

`Desktop_ERP` نسخة مكتبية لشركة واحدة: لا مشتركين، ولا اشتراكات، ولا مشغّلين، ولا شاشة
واحدة تُدير منشآت. لذلك لا يوجد لمسارات `/platform/*` مصدرٌ بالسطر في الديسكتوب. الخطة
نصّت على البديل (§2 من ملف الخطة): **«المصدر من هذا المستودع + مواصفة الخدمة المنشورة»**،
وهذا ما اعتمدته هذه الوثيقة: كل بند يسمّي ملفه ونقطته القائمة.

| ما بُني | المصدر بالسطر / بالمسار |
|---|---|
| الجرد الأولي للفجوة | `docs/roadmap/INCOMPLETE_INVENTORY.md` §4.2 (11 رمزاً معلَناً بلا استخدام، و`GET /platform/audit-log` = 404) |
| الحماية التي رُمِّمت | `apps/api/src/modules/platform/guards/platform-admin.guard.ts` (كانت: `pam` وحده يكفي) + `decorators/requires-platform-role.decorator.ts` |
| جدول المسارات السبعة عشر | `apps/api/src/modules/platform/admin/platform-admin.controller.ts` (قبل التعديل: 2 من 17 مساراً برمز) |
| سجل رموز اللوحة | `packages/contracts/src/permissions.ts` → `platformPermissionRegistry` |
| الأدوار الخمسة وحقوقها | `packages/contracts/src/rbac.ts` → `platformRoleCatalog` (السطور 70–135) |
| `/me` ودعاوى الجلسة | `apps/api/src/modules/platform/identity/identity.service.ts` + `auth/token.service.ts` (`proles`) |
| سياسة مستوى المنصة | `packages/database/migrations/0020_platform_admin_plane.sql` (النمط المتَّبع في 0066) |
| `audit_log` وسياسته | `packages/database/migrations/0001_platform_services.sql` L21–L37 و L174–L192 |
| إدراج الرموز في الترحيلات | `packages/database/migrations/0032_platform_roles_devices_scopes.sql` L199–L225 |
| شجرة التنقّل والقشرة | `apps/platform-admin/components/platform-guard.tsx` (9 تبويبات قبل P-C1 — تسمياتها أُعيدت بالحرف) |
| صفحات اللوحة | `apps/platform-admin/app/**` (11 صفحة قائمة) |

> ملاحظة منهجية: هذا الجزء **لا ينقل شاشة من الديسكتوب**، فلا تنطبق نسبة التطابق ≥ 90٪
> على نصوصه؛ ما ينطبق هو ما تفرضه الخطة: إعادة التسميات القائمة بالحرف، وتبرير كل تسمية
> مخترَعة (الجدول في §6).

---

## 1. ما كان مكسوراً — بالقياس لا بالرواية

| الفجوة | القياس قبل P-C1 | الدليل |
|---|---:|---|
| مسارات `/platform/*` بلا رمز `console.*` | **15 من 17** | `grep -rn "RequiresPlatformRole" apps/api/src` → سطرا `console.users.manage` فقط |
| رموز `console.*` معلَنة ومستخدَمة | **1 من 12** | `platformPermissionRegistry` مقابل نتائج البحث نفسه |
| تدقيق عابر للمستأجرين | **لا يوجد** | `GET /platform/audit-log` → 404، وصفحة `/audit` تقرأ `/audit-log` (سجل مستأجر المنصة وحده) |
| إعدادات المنصة | **`.env` على المضيف** | لا جدول ولا شاشة ولا نقطة نهاية |
| صفحة `/jobs` | **مستأجر واحد** | `GET /jobs/outbox` → `data: []` دائماً لمشغّل المنصة |
| بحث شامل (`Ctrl+K`) | **لا يوجد** | لا نقطة نهاية بحث، ولا لوحة أوامر |

---

## 2. ما بُني

### 2.1 نقطة النهاية — أربع نقاط جديدة (كلها بصلاحية `console.*`)

| الطريقة والمسار | الرمز | الوظيفة |
|---|---|---|
| `GET /platform/audit` | `console.audit.view` | سجلّ تدقيق عابر للمستأجرين، كل سطر بجانبه عميله (`tenantCode`/`tenantName`)، وبمرشّحات: `tenantId` · `actorUserId` · `action` · `entity` · `entityId` · `from` · `to` |
| `GET /platform/settings` | `console.tenants.view` | ثمانية إعدادات: الكاتالوج + القيم المكتوبة + اسم البيئة ووسمها العربي |
| `PUT /platform/settings` | `console.settings.manage` | كتابة مجموعة مفاتيح، تُتحقَّق مقابل الكاتالوج المشترك، وتُدوَّن سطراً لكل مفتاح بـ`before`/`after` |
| `GET /platform/tenants/search?q=` | `console.tenants.view` | بحث `Ctrl+K` بالرمز أو الاسم (عشرة نتائج كحد أقصى) |
| `GET /platform/jobs/outbox` | `console.jobs.view` | صندوق الأحداث الصادرة عبر كل العملاء (قراءة فقط) — **إضافة على قائمة الخطة**، انظر §7 |

### 2.2 ترميم الصلاحيات — كل مسار برمزه

`platform-admin.controller.ts`: 17 مساراً، كلٌّ منها الآن يحمل `@RequiresPlatformRole`:

| المسار | الرمز | المالك | العمليات | الفوترة | الدعم | المدقّق |
|---|---|:--:|:--:|:--:|:--:|:--:|
| `GET overview` · `GET tenants` | `console.tenants.view` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `POST tenants` · `PATCH tenants/:id/status` | `console.tenants.manage` | ✓ | | | | |
| `GET plans` · `POST plans` · `PATCH plans/:id/active` | `console.plans.manage` | ✓ | | ✓ | | |
| `GET subscriptions` · `POST subscriptions` · `POST subscriptions/:id/cancel` | `console.subscriptions.manage` | ✓ | | ✓ | | |
| `GET activation-requests` · `POST activation-requests/:id/review` | `console.activation.review` | ✓ | | ✓ | | |
| `GET users` · `GET roles` · `GET permissions` | `console.users.view` | ✓ | | | | |
| `POST users/:id/roles` · `DELETE users/:id/roles/:roleCode` | `console.users.manage` | ✓ | | | | |
| `GET/PUT settings` (فيهما قراءة فقط للرموز) | `console.tenants.view` / `console.settings.manage` | ✓ | | | | |
| `GET audit` · `GET jobs/outbox` | `console.audit.view` / `console.jobs.view` | ✓ | ✓ | | | ✓ |
| `GET tenants/search` | `console.tenants.view` | ✓ | ✓ | ✓ | ✓ | ✓ |

**قاعدة القراءة:** مسارات القراءة أُسندت إلى رمز *المنطقة* الذي يملكها (الباقات، التراخيص،
طابور التفعيل، الهوية) لا إلى رمز قراءةٍ مزدوج، لأن السجل لا يحمل توأماً للقراءة في هذه
المناطق، واختراع أربعة رموز لقراءة أربع قوائم يوسّع السطح لا يضيّقه. القشرة تُخفي ما يمنعه
الرمز (`apps/platform-admin/lib/navigation.ts`)، فلا يرى المشغّل رابطاً ميتاً.

**حالة استثنائية واحدة مقصودة:** مسار `POST /platform/signup` (في `signup.controller.ts`)
عامٌّ عن قصد — إنه نافذة الاشتراك الذاتي المُعدَّل بالحدّ (`@Public`)، ولا يدخل في العدّ أعلاه.

### 2.3 الترحيل `0066_platform_settings.sql`

| العنصر | التفصيل |
|---|---|
| الجدول | `platform_settings(id, tenant_id NULL→tenants, key, value jsonb, created_at/by, updated_at/by, version)` + تحقّق على طول المفتاح |
| الفرادة | `platform_settings_scope_key ON (key, tenant_id) NULLS NOT DISTINCT` (PostgreSQL 15+) — مفتاح المنصة وحيد مرةً واحدة، ولكل منشأة صفُّها |
| RLS | `ENABLE` + `FORCE` + سياسة `platform_settings_tenant_isolation` على `tenant_id` + سياسة `platform_admin_plane` (نمط 0020) |
| التدقيق | سياسة `platform_admin_plane` **للقراءة فقط** على `audit_log` (بلا `WITH CHECK`، فالكتابة تبقى على مسارها القديم) |
| الرمز الجديد | `INSERT INTO permissions ('console.settings.manage', 'console', …) ON CONFLICT (code) DO UPDATE` — إدراج idempotent |
| المنح | `erp_api`: SELECT/INSERT/UPDATE/DELETE · `erp_migrator`: ALL · وإعادة تأكيد `NOBYPASSRLS` |
| التراجع | `packages/database/migrations/down/0066_platform_settings.down.sql` |

`tenant_id` **إضافة على نصّ الخطة** (التي كتبت `key, value jsonb, updated_by, updated_at`):
P-C2 يحتاج تجاوز المستأجر بمفتاحه، والحقل نفسه يُدخل الجدول في قاعدة RLS بدل استثنائه منها.
مسجَّل في §7.

### 2.4 مخطط Drizzle

`packages/database/src/schema/platform.ts` → `platformSettings` (+ `PlatformSetting` /
`NewPlatformSetting`). الفهرس الفريد مُعلَن في الترحيل لأن `NULLS NOT DISTINCT` خارج
تعبير Drizzle، وهذا مذكور في تعليق الجدول صراحةً.

### 2.5 القشرة (شاشة/تنقّل)

| المطلوب في الخطة | المنفَّذ |
|---|---|
| شريط جانبي بمجموعات: العملاء · المال · التشغيل · المنصة | `apps/platform-admin/lib/navigation.ts` (12 بنداً) + `<aside className="side">` في `components/platform-guard.tsx` |
| بحث شامل `Ctrl+K` عن مستأجر/مستخدم | `Omnibox` → `GET /platform/tenants/search` و`GET /platform/users?search=` (والثاني لا يُعرض إلا لمن يملك `console.users.view`) |
| جرس التنبيهات | `Bell` → عدّاد مركّب من `GET /platform/overview` (طلبات تفعيل معلّقة + تراخيص متأخرة + عملاء موقوفون) |
| شارة البيئة | من `GET /platform/settings` → `environment` (من `NODE_ENV` الحقيقي للـAPI) |
| فتات الخبز | `Screen` + `groupForPath()` (المنصة ← المجموعة ← الصفحة) |
| لوحة RTL | `layout.tsx` على `dir="rtl"`، والقشرة بخصائص منطقية (`inset-inline-start`) لا بأصفاد LTR |

الشاشة الجديدة: **إعدادات المنصة** على المسار `/settings` (`app/settings/page.tsx`)،
تُصيَّر بالكامل من الكاتالوج القادم من الـAPI (تسميات وشرح ونوع وقيمة افتراضية)، وتكون
للقراءة فقط لمن لا يملك `console.settings.manage`.

الشاشتان المُعاد توصيلهما: **التدقيق** `/audit` (عابرة للمستأجرين بمرشّحاتها) و**المهام**
`/jobs` (عابرة للمستأجرين، مع عمود العميل).

### 2.6 الصلاحيات — الرمز الجديد والأشياء الثلاثة معاً

| المطلوب | المنفَّذ |
|---|---|
| إعلان في `packages/contracts/src/permissions.ts` | `console.settings.manage` في `platformPermissionRegistry` (13 رمزاً الآن) |
| إدراج idempotent في الترحيل | `0066_platform_settings.sql` §4 (`ON CONFLICT … DO UPDATE`) |
| اختبار يثبتهما | `apps/api/src/permission-codes.spec.ts` — فحص `RequiresPlatformRole(` مقابل السجل، وفحص وجود كل رمز في ترحيل، وفحص أن لا مسار `/platform/*` بلا رمز |

والمنح: `platform_owner` فقط (13/13 رمزاً). العمليات تقرأ إعدادات المنصة بـ
`console.tenants.view` ولا تكتبها.

### 2.7 الاختبار والتحقّق الحيّ

| الملف | المحتوى |
|---|---|
| `apps/api/test/platform-console-rbac.spec.ts` | **20 اختباراً**: مصفوفة الأدوار الخمسة، رفض رمز `pam` وحده، التدقيق العابر ومرشّحاته، دورة الإعدادات، رفض الباتش كاملاً عند مفتاح تالف، RLS على المستأجر، البحث بالعربية، صندوق الأحداث، وتطابق جدول الرموز |
| `apps/api/src/permission-codes.spec.ts` | 5 اختبارات (منها 3 جديدة لـP-C1) — بوابة قبول «صفر مسار بلا رمز» |
| `apps/platform-admin/tests/navigation.spec.ts` | 8 اختبارات — كل بند `ready` له ملف صفحة، وكل بند يحمل رمزاً معلَناً، والمجموعات الأربع، وإخفاء ما يُمنع |
| `apps/platform-admin/tests/routes.spec.ts` | 4 اختبارات (منها `/settings` و`/jobs` في القائمة، وفحص ألا رابط صفحةٍ تحت `/platform`) |
| `scripts/verify-platform-console.mjs` | **70 نقطة في 10 أقسام**، قابلة لإعادة التشغيل، تُصوّر الإعدادات وتعيدها، ولا ترسل بريداً ولا رسالة. أُعيد تشغيله **ثلاث مرات متتالية**: **70/70 في كل مرة** (آخرها بعد تثبيت كل الأرقام) |

---

## 3. الأرقام قبل/بعد (مقيسة)

| البند | قبل | بعد |
|---|---:|---:|
| اختبارات API | 951 (126 ملفاً) | **975** (127 ملفاً) — +24 |
| اختبارات staff | 36 | 36 (لم تُمسّ) |
| اختبارات contracts | 71 | 71 (لم تُمسّ) |
| اختبارات platform-admin | 3 | **12** |
| رموز `console.*` | 12 معلَناً · 1 مستخدَم في نقطة نهاية | **13 معلَناً · 10 مستخدَمة في نقاط نهاية** (الباقي: `health` لـP-C9، `billing` لـP-C4، `support` لـP-C8) |
| مسارات `/platform/*` بلا رمز | 15 | **0** |
| نقاط نهاية اللوحة | 17 | **22** (+5: تدقيق · إعدادات GET/PUT · بحث المستأجرين · صندوق الأحداث) |
| شاشات اللوحة | 11 | **12** |
| الترحيلات | حتى 0065 | **حتى 0066** |
| سكربتات التحقّق الحيّ | 27 | **28** |

### 3.1 أرقام الاختبارات الفعلية بعد التنفيذ

قياس فعلي على هذا المستودع بعد التنفيذ (`npx vitest run` داخل كل حزمة، 2026-09-17):

| الحزمة | قبل | بعد | التغيّر |
|---|---:|---:|---:|
| API | 951 اختباراً في 126 ملفاً | **975** في **127** ملفاً | **+24** (20 في `platform-console-rbac.spec.ts` — ملف جديد — و4 في `permission-codes.spec.ts` من 1 إلى 5) |
| platform-admin | 3 | **12** | +9 (8 في `navigation.spec.ts` الجديد، و1 في `routes.spec.ts`) |
| staff | 36 | 36 | 0 (لم تُمسّ) |
| contracts | 71 | 71 | 0 (لم تُمسّ) |

`951 + 24 = 975` بالضبط؛ لم يُحذف اختبار ولم يُتخطَّ ملف.


---

## 4. نقاط النهاية والشاشات — الأثر على الواجهة

| الشاشة | المسار | الرمز | ما تغيّر |
|---|---|---|---|
| إعدادات المنصة | `/settings` | `console.tenants.view` (+`console.settings.manage` للكتابة) | **جديدة** |
| التدقيق | `/audit` | `console.audit.view` | صارت عابرة للمستأجرين بمرشّحات (كانت سجل مستأجر المنصة) |
| المهام والطوابير | `/jobs` | `console.jobs.view` | صارت عبر كل العملاء (كانت `data: []`) |
| بقية الشاشات (10) | — | كما في §2.2 | القشرة الجديدة (شريط جانبي بأربع مجموعات، شريط علوي، فتات خبز) |

كل صفحة من الاثنتي عشرة لها ملف في `apps/platform-admin/app/**`، ويثبته
`tests/navigation.spec.ts` — فلا رابط بلا صفحة.

---

## 5. الثغرة التي كُشفت أثناء التنفيذ وأُغلقت (RLS على «الإعدادات»)

قيمة نصية مثل `920000000` كانت **تعود من الـAPI عدداً**، و`true` تعود **بوليان**،
و`{"x":1}` تعود **كائناً**. السبب: `jsonb` يصل من السائق مُفكَّكاً، ثم يفكّه مغلّف
Drizzle مرة ثانية إن كان نصاً يشبه JSON. الرقم النصي واسم النطاق والسابقة هي بالضبط هذا
النوع من القيم، أي أن الشاشة كانت ستعرض نوعاً غير الذي كُتب.

الإصلاح: قراءة `value::text` وفكّها مرة واحدة في الخدمة
(`platform-console.service.ts` → `listSettings` و`updateSettings`)، واختبار انحدار في
`platform-console-rbac.spec.ts` يكتب أربع قيم (`'920000000'` · `'true'` · `'{"x":1}'` ·
`'007'`) ويثبت أنها تعود نصوصاً كما كُتبت.

---

## 6. ما اخترعناه (جدول التسميات والقرارات)

### 6.1 تسميات أُعيدت بالحرف من اللوحة القائمة (لا اختراع)

نظرة عامة · العملاء · عميل جديد · التراخيص · الباقات · طلبات التفعيل · المستخدمون ·
أدوار المنصة · التدقيق · الصحة · المهام والطوابير · الجهات الأربع للمجموعات
(العملاء · المال · التشغيل · المنصة) — الأخيرة مأخوذة من نصّ الخطة §4 حرفياً.

### 6.2 تسميات وقرارات مخترَعة — ولكلٍّ سبب

| التسمية / القرار | لماذا اخترناها |
|---|---|
| **إعدادات المنصة** | من الخطة نفسها («شاشة إعدادات المنصة»)؛ ولم نسمّها «الإعدادات» فقط لأن لوحة القائمة فيها إعدادات المنشأة على سطح staff |
| **بريد الدعم** · **هاتف الدعم** | «بريد الدعم» من الخطة؛ و«هاتف الدعم» توأمه الضروري، وإلا لخرج المشغّل إلى ملف البيئة لكتابة رقم |
| **نطاقات الخدمة** | الخطة قالت «النطاقات»؛ وأضفنا «الخدمة» لأن «نطاق» وحدها تلبس على نطاق العميل (P-C2) ونطاق المنصة |
| **حدّ المستخدمين/الفروع/الفواتير الشهرية الافتراضي** | الخطة قالت «الحدود الافتراضية»؛ وأخذنا أسماء المقاييس الثلاثة من جدول مقاييس P-C5 نفسه (مستخدمون · فروع · فواتير/شهر) مع كلمة «الافتراضي» لتمييزها عن حدّ المنشأة الخاص |
| **رسالة الصيانة** · **مُطفأ / مُفعَّل** | الخطة ذكرت «مفتاح صيانة» بلا نصّ مرافق؛ ولا يصلح مفتاحٌ يُطفئ الأسطح بلا رسالة تُبلّغ العميل السبب |
| **التنبيهات المفتوحة** · **لا شيء مفتوح الآن** · **جارٍ البحث…** · **لا نتائج** | نصٌّ واجهيّ بحت لا مقابل له في أي مصدر؛ عربية فصيحة قصيرة كما في بقية الشاشات |
| **عمود «العميل» في التدقيق** | الشاشة القديمة كان لها الوقت/الإجراء/الكيان/المعرّف/المستخدم — وهي أعمدة مستأجرٍ واحد؛ سجلٌّ عابر للمستأجرين بلا عمود عميل لا يُقرأ |
| **عمود «الطابور» في المهام** | مأخوذ من اسم العمود `queue` في `outbox_jobs`؛ والعميل أُضيف لنفس سبب التدقيق |
| **رمز `console.settings.manage`** | كتابة إعدادات المنصة ليست «إدارة مستأجرين» (`console.tenants.manage`)، ولا «مراجعة تفعيل»؛ فرُمزها الخاص يمنح المالك وحده الكتابة ويُبقي القراءة للعمليات. الإعلان والإدراج والاختبار في §2.6 |
| **قراءة الباقات/التراخيص/الطابور برمز الإدارة** | لا توأم قراءةٍ لهذه الرموز في السجل، واختراع أربعة رموز جديدة لقراءة أربع قوائم يوسّع السطح. القرار قابل للنقض في P-C3/P-C4 إن طلب المشغّل فصل القراءة |
| **`tenant_id` في `platform_settings`** | انحراف مقصود عن نصّ الخطة: P-C2 يحتاج تجاوز المستأجر، والحقل يُدخل الجدول في قاعدة RLS العامة بدل استثنائه. صفٌّ بـ`tenant_id IS NULL` = إعداد منصة |
| **`GET /platform/jobs/outbox`** | إضافة على قائمة الخطة: P-C1 اسمه «ترميم الصلاحيات»، وبند «المهام» في الشريط الجانبي كان يفتح صفحة تكذب (تقرأ صندوق مستأجر المنصة). إما رمز بلا شاشة، أو شاشة بنقطة نهاية حقيقية — اخترنا الثانية. إعادة المحاولة والإفراغ من عمل P-C9 |
| **`platformPermissions` في `/me`** | القشرة تحتاج أن تعرف ما تعرضه قبل أن تكلّف الخادم بطلبٍ سيُرفض؛ وهي قائمة **منفصلة** عن `permissions` لأن `*` (الذي يحمله مالك المستأجر) لا يجوز أن يمسّ رمز `console.*` |

---

## 7. ما لم يُبنَ في هذا الجزء (والبقايا المؤجَّلة)

| البند | لماذا |
|---|---|
| P-C2 … P-C12 | خارج نطاق الجزء بقرار جلسةٍ واحدة لكل جزء |
| `console.health.view` في نقطة نهاية | الشاشة تفحص `/api/health/ready\|live` مباشرةً (مجسّات علنية). تحويل الصحة إلى مسار `/platform/health` عملُ P-C9 |
| `console.billing.manage` · `console.support.manage` | بلا مستهلك حتى P-C4 وP-C8 — أُبقيت معلَنة كما هي، ولم تُحذف |
| إعادة المحاولة/الإفراغ في صندوق الأحداث | للقراءة فقط هنا؛ الكتابة في P-C9 |
| 🇪🇬 مصر (`frmEtaSetting` · `EtaService` · `EtaReciptService`) ونداءا 🚫 إلغاء الفاتورة و❌ رفض الفاتورة | **مؤجَّلة/خارج النطاق** بقرار صاحب المشروع (النظام موجَّه للسعودية — زاتكا)، ولا علاقة لها بهذا الجزء، وتُسجَّل هنا لئلا تُنسى |

---

## 8. إعادة الإنتاج

```bash
pnpm -r --filter "./packages/*" build
pnpm db:local && pnpm db:migrate && pnpm db:seed      # الترحيل 0066
pnpm dev                                             # API :3000 · platform :3003
node scripts/verify-platform-console.mjs             # 70 نقطة في 10 أقسام
```

آخر تشغيل: **70/70 خضراء**، ثلاث مرات متتالية، والحالة تعود كما كانت (الإعدادات مستعادة،
والدور المؤقّت مسحوب).

### 8.1 ما شُغِّل فعلاً قبل التسليم (2026-09-17)

| الأمر | النتيجة |
|---|---|
| `npx vitest run` في `apps/api` | **975/975** في 127 ملفاً · EXIT=0 |
| `npx vitest run` في `apps/platform-admin` | **12/12** · EXIT=0 |
| `npx vitest run` في `apps/staff` | 36/36 · EXIT=0 |
| `npx vitest run` في `packages/contracts` | 71/71 · EXIT=0 |
| `pnpm --filter @erp/api build` | EXIT=0 |
| `pnpm --filter @erp/platform-admin build` | **نجح** — 12 مساراً + `/_not-found` · EXIT=0 |
| `pnpm -r run build` (كل الحزم والتطبيقات) | EXIT=0 |
| `pnpm -r run lint` | EXIT=0 (لا تحذير إلا تحذير `jsboundaries` القائم في المستودع كله) |
| `node scripts/verify-platform-console.mjs` | **70/70** · EXIT=0 (ثلاث مرات) |

**ملاحظة أمانة عن `tsc`:** `pnpm exec tsc -p tsconfig.base.json --noEmit` (الفحص التجميعي
على كل المستودع) يُخرج **245 خطأً في 69 ملفاً** — كلها **قائمة قبل هذا الجزء**، ونمطها
واحد: ملفات واجهات (`apps/staff` · `apps/marketing` · `apps/platform-admin`) تُفحَص بلا
`lib: dom`، و`ActorOptions` في `apps/api/test/fixtures.ts` لا يُصرّح بـ`kind`/`platformRoles`
مع أن `apps/api/test/surface-isolation.spec.ts` **غير المعدَّل** يستخدمهما. أي أن الخطأ
نفسه يظهر في ملفات لم نمسّها قبل تعديلنا وبعده. البوابة الفعلية هي `tsc` لكل تطبيق
(`pnpm --filter … build`) وقد نجحت كلها.
