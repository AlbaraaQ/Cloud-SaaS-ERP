# P-C10 — البيانات والاسترجاع

> الجزء الحادي عشر من [`roadmap/PLATFORM_CONSOLE_PLAN.md`](./roadmap/PLATFORM_CONSOLE_PLAN.md) §4.
> **الحالة: ✅ مُنجَز** (2026-09-17) على الفرع `arena/01a0acbb-cloud-saas-erp`.
> يعتمد على **P-C1** وحده (القشرة · الرموز · التدقيق العابر · `PlatformAdminGuard`) — كما قالت الخطة.
>
> **الأرقام بعد التنفيذ:** API **1139** اختباراً (136 ملفاً) · contracts **139** (17 ملفاً) ·
> platform-admin **24** · staff **37** · `platform-backups.spec.ts` **25** (الخطة طلبت ≥ 8) ·
> `scripts/verify-platform-backups.mjs` **87/87** في **9** أقسام (سكربتٌ جديد) ·
> `verify-platform-console.mjs` **200/200** في **21** قسماً (كان 187/20) · والمجموع الحيّ للوحة
> **780** نقطة في **87** قسماً (كان 679/76) · ترحيل واحد
> (`0074_platform_backups.sql` + ملف تراجع، والتراكمي **75**) · **13** مساراً تحت `/platform/*`
> (التراكمي **107**) · **شاشتان** في اللوحة (التراكمي **27**) · رمز `console.*` جديد واحد
> (`console.backups.manage`، المجموع **18**، وصفر رمز غير مستعمل) · **3** جداول جديدة.

---

## 0. بوابة «المصدر بالسطر»

| الشاشة/السلوك | المصدر | السطر |
|---|---|---|
| «الهدف: نسخة تُغادر القاعدة فعلاً، وسياسة احتفاظ، وحقّ نسيان» | `docs/roadmap/PLATFORM_CONSOLE_PLAN.md` §4 «P-C10» | 266 |
| «`/backups` (جدولة · النسخ · الحجم · الحالة · التنزيل · التحقّق من السلامة · استعادة تجريبية) · سياسات الاحتفاظ · طلبات تصدير/حذف البيانات الشخصية» | المرجع نفسه | 267 |
| «نقاط نهاية: `POST /platform/backups/run` · `GET /platform/backups` · `GET /platform/backups/:id/download` (رابط موقّت قصير العمر) · `POST /platform/backups/:id/verify` · `GET/PUT /platform/retention`» | المرجع نفسه | 268 |
| «التخزين: MinIO/S3 عبر `platform-services/files/object-storage.ts` (قائم) + تشفير + سياسة احتفاظ + سجلّ» | المرجع نفسه | 269 |
| «صلاحيات: **`console.backups.manage`** (جديد)» | المرجع نفسه | 270 |
| «ترحيل: `0072_platform_backups.sql` — `backup_jobs` · `backup_artifacts`» (والرقم صار **0074** لأن 0072/0073 أخذهما P-C8/P-C9) | المرجع نفسه | 271 |
| «اختبار: `platform-backups.spec.ts` (≥ 8)؛ والتحقّق الحيّ بـ`scripts/verify-platform-backups.mjs`» | المرجع نفسه | 272 |
| الجدول التراكمي وعدّاد الجزء (8 اختبارات · +30 نقطة) | المرجع نفسه | 313-314 |
| «النسخ الاحتياطي داخل القاعدة» 🔴 كنقصٍ قائم | `docs/roadmap/INCOMPLETE_INVENTORY.md` | 85 · 166 |
| «نسخ احتياطي خارج القاعدة» و«حذف/تصدير البيانات الشخصية (طلبات)» في قائمة المفتوح | المرجع نفسه + PLAN §5 | 132 · 421 |
| منفذ التخزين و`ArtifactStore`-المكافئ: توقيع الروابط و`isConfigured()` | `apps/api/src/modules/platform-services/files/object-storage.ts` | 1-80 |
| رمز التنزيل الموقّع (`signDownloadToken` · `verifyDownloadToken` · `isExpired`) وTTL | `apps/api/src/modules/platform-services/files/download-token.ts` · `packages/config/src/env.ts` (`FILES_DOWNLOAD_URL_TTL_SECONDS`) | 1-70 · 96 |
| قرار `@Public()` على نهاية المحتوى الموقّعة | `apps/api/src/modules/platform-services/files/files.controller.ts` | 114-119 |
| محرّك الاحتفاظ القائم (`computeRetentionPlan` · `auditHardDeleteAllowed:false`) | `apps/api/src/modules/ops/retention.service.ts` | 1-120 |
| كتابة التدقيق (`record` يبتلع الفشل ولا يُسقط الفعل · redact · ذاكرة 60 ثانية) | `apps/api/src/modules/platform-services/audit/audit.service.ts` | 40-140 |
| تسمية الفاعل في التدقيق (`platformActorLabel`) | `apps/api/src/modules/email/actor-label.ts` | 1-40 |
| أعمدة `users` التي تُخفى عند المحو + `memberships.status` + `refresh_tokens` | `packages/database/migrations/0000_init.sql` | 86 · 121 |
| أنماط الشاشات (بطاقات · جداول · `Notice` · `Forbidden`) | `apps/platform-admin/app/files/page.tsx` · `app/announcements/page.tsx` | 1-200 |
| تسمية البندين في الشريط ومجموعاته الأربع | `apps/platform-admin/lib/navigation.ts` (المجموعة الرابعة «التشغيل») | 158-171 |

---

## 1. ما كان مكسوراً — بالقياس لا بالرواية

### 1.1 «النسخ الاحتياطي» كان نسخةً **داخل** القاعدة

`INCOMPLETE_INVENTORY` يقولها بصراحة: «النسخ الاحتياطي | 🟡 داخل قاعدة البيانات نفسها |
`/settings/backup` و`/settings/restore` (لا تصمد لفقدان القرص)». والشاشة التي وُجدت في staff
تعمل بـ`pg_dump`-بديل أو بلقطةٍ داخلية؛ ومن فقد القرص فقد النسخة والبيانات معاً. **وسؤال
الجزء ليس «هل عندنا نسخة؟» بل «أين النسخة، وكيف أعرف أنها سليمة؟»** — ولا واحدٌ من الثلاثة كان
مُجاباً: لا مخزن خارجي، ولا بصمة، ولا تحقّق، ولا حجمٌ مقيس.

### 1.2 سياسة الاحتفاظ كانت **وصفاً بلا أثر**

`ops/retention.service.ts` كان يحسب `auditArchiveDays`/`idempotencyPurgeDays`/`outboxPurgeDays`/
`fileOrphanPurgeDays` ويعيد خطةً (`computeRetentionPlan`) — لكن **لا شيء يُنفّذها من سطح
المنصّة**: لا نافذة قابلة للضبط، ولا نهاية تُطبّق، ولا عدّاد يقول كم سيطالها التنفيذ. وسياسةُ
احتفاظٍ تُقرأ ولا تُنفَّذ هي مستندٌ لا نظام.

### 1.3 «حقّ النسيان» كان بلا مسار

لا نهاية تقبل طلب محوٍ لشخصٍ بعينه، ولا صفٌّ يسجّل (من طلب · من قرّر · بماذا · متى نُفِّذ)، ولا
طريقة لإخفاء الهوية مع **إبقاء الإيصال**: محوُ `users` مباشرةً يمحو معه إن كان المستخدم قد
كتب سطور تدقيق، أو يترك طلباً بلا أثر. ولم يكن للسؤال «ماذا بقي بعد المحو؟» جوابٌ رقميّ.

### 1.4 ولا بصمة، ولا تحقّق، ولا رابط موقّع

لم يكن في اللوحة مسار تنزيلٍ بنسخة، ولا `verify`: أي أن نسخةً تعفّنت في المخزن (بايتٌ تغيّر)
تُنزَّل وتُظنّ سليمة. وهذا هو **بالضبط** الفرق بين نسخةٍ ووعدٍ بنسخة.

---

## 2. ما بُني

### 2.1 العقود — `packages/contracts/src/platform/backups.ts` (جديد)

| الشيء | التفصيل |
|---|---|
| النطاقات | `platform` · `tenant` |
| الحالات | `running` · `succeeded` · `failed` |
| المخازن | `object-storage` (S3/MinIO) · `filesystem` (بديل مُعلَن) |
| التشفير | `aes-256-gcm` دائماً · صيغة `erp-platform-dump/1` |
| الاحتفاظ | `platformRetentionPolicySchema` بخمس نوافذ (`auditArchiveDays` 90..3650 · `artifactRetentionDays` 1..365 · `idempotencyPurgeDays` 7..365 · `outboxPurgeDays` 7..365 · `fileOrphanPurgeDays` 1..90) وافتراضياتها `{365,30,90,2,30}` |
| التنفيذ | `platformRetentionApplySchema`: `mode` (`dry_run` افتراضاً · `apply`) · `targets` (`artifacts` · `idempotency` · `outbox` · `files`) · `reason` ≥ 5 — **و`audit` مرفوض في المخطّط نفسه** |
| طلبات البيانات | `platformDataRequestCreateSchema` (`export`/`erase` + المستأجر + بريد صاحب البيانات) · `…DecideSchema` (`approve`/`reject` + سبب ≥ 5) · `…ExecuteSchema` (`confirm` للحيازة) |
| الحالات | `pending` · `approved` · `rejected` · `completed` · `cancelled` |
| نتيجة المحو | `anonymisedFields` · `anonymisedUsers` · **`suspendedMemberships`** · `revokedSessions` · **`retainedAuditRows`** · `subjectRef` |
| نتيجة التحقّق | `checksum{expected,actual,matches}` · `truncated` · `restore.mode='dry_run'` بـ`missingTables` و`verdict: ready | drifted | unreadable` |
| الأفعال المُدقَّقة | تسعة: `platform.backup.{run,verify,download}` · `platform.retention.{update,apply}` · `platform.data-request.{create,decide,export,erase}` |

و19 تصديراً من `platform/index.ts` (كان 19 قبل هذا الجزء بالمجموع العام، والملف أضاف عقوده إلى
`backups.spec.ts` بـ**11** اختباراً).

### 2.2 الصلاحية والترحيل `0074_platform_backups.sql`

- **`backup_jobs`** — *المحاولة*: النطاق (`scope` + `tenant_id` بقيد `backup_jobs_tenant_scope_check`)،
  الحالة، عدّادات (جداول/صفوف/مستأجرون)، المُدّة، `note` (3..500)، `failure_reason`،
  `verified_at`/`verified_checksum`، ومن طلبها.
- **`backup_artifacts`** — *الملف*: المخزن · `object_key` · الصيغة · الخوارزمية · `iv` · الحجم ·
  **بصمة النصّ الصريح** · و`pruned_at` (حُذفت البايتات بالاحتفاظ وبقي الصفّ إيصالاً).
- **`data_requests`** — *الطلب*: النوع · الحالة · المستأجر · `subject_email` (يبقى بعد التنفيذ:
  هو الإيصال) · `subject_user_id` · `decision_note` (5..500) · `decided_by/at` · `executed_at` ·
  `result jsonb` · قيدان: «التنفيذ لا يسبق القرار» و«لا تنفيذ إلا بحالة `completed`».
  وعليه RLS بسياسة `platform_admin_plane` وحدها.
- `REVOKE UPDATE, DELETE ON audit_log FROM erp_api` (تأكيد المنع) + صفّ `console.backups.manage`
  في `permissions` + `ALTER ROLE erp_api NOBYPASSRLS`.

**وفصلُ المحاولة عن الملف مقصود**: محاولةٌ فشلت قبل كتابة بايتات صفٌّ مشروع (`failed` بلا ملف)،
وملفٌّ واحد قد يُقرأ مرّاتٍ (تحقّق · تنزيل · عبث) فتتغيّر تواقيته بلا أن تتغيّر النسخة.

### 2.3 ثلاثة عشر مساراً (`platform-backups.controller.ts`)

| المسار | الرمز | ما يفعله |
|---|---|---|
| `GET /platform/backups` | `console.backups.manage` | الفهرس بترشيح (`status` · `scope` · `tenantId` · `store`) وفرز (`startedAt` · `bytes` · `durationMs`) وصفحة (`limit` ≤ 200) |
| `POST /platform/backups/run` | `console.backups.manage` | نسخةٌ الآن: قراءة الكتالوج الحيّ → NDJSON → تشفير → كتابة في المخزن → قياس → صفّ |
| `GET /platform/backups/:id/download` | `console.backups.manage` | **رابطٌ موقّع قصير العمر** (TTL من `FILES_DOWNLOAD_URL_TTL_SECONDS`) |
| `GET /platform/backups/:id/content` | **`@Public()`** | وجهة الرابط: الرمز الموقّع هو التصريح (نمط `/files/:id/content`) |
| `POST /platform/backups/:id/verify` | `console.backups.manage` | إعادة قراءة الملف وفكّه ومسحه سطراً سطراً ومقارنته بالكتالوج الحيّ |
| `GET /platform/retention` | `console.backups.manage` | السياسة + الافتراضيات + التجاهيز الزمنية + **عدّادات ما سيطاله التنفيذ** |
| `PUT /platform/retention` | `console.backups.manage` | تعديل النوافذ (كل تعديل صفُّ تدقيق، و`version` يزيد) |
| `POST /platform/retention/apply` | `console.backups.manage` | `dry_run` يقيس · `apply` ينفّذ فعلاً (حذف بايتات النسخ المنتهية · مفاتيح `idempotency` · مهامّ الطابور · الملفات اليتيمة) |
| `GET /platform/data-requests` | `console.backups.manage` | فهرس الطلبات بحالاتها ونتائجها |
| `POST /platform/data-requests` | `console.backups.manage` | فتح طلب تصدير/محو لشخصٍ بعينه |
| `POST /platform/data-requests/:id/decide` | `console.backups.manage` | قرارٌ بسببٍ مكتوب، ومرّة واحدة (422 في الثانية) |
| `POST /platform/data-requests/:id/execute` | `console.backups.manage` | تنفيذ: تصديرٌ يُنتج ملفاً، أو محوٌ يُخفي الهوية |
| `GET /platform/data-requests/exports/:artifactId` | **`@Public()`** | وجهة رابط التصدير الموقّع |

### 2.4 الخدمة — القرارات الثلاثة الحاكمة

1. **النسخة تُقاس من الملف، لا من الطلب.** بعد الكتابة تُقرأ البايتات من المخزن ويُقاس حجمها
   ويُحسب `sha256` **للنصّ الصريح**؛ و`verify` يعيد القراءة والفكّ والمسح. فبايتٌ واحد يتغيّر
   في المخزن ⇒ `verified:false` بسببٍ مكتوب — لا ابتسامة.
2. **الاحتفاظ يُنفَّذ لا يُوصف.** النوافذ في `platform_settings` تحت `retention.policy`، ولها آثارٌ
   حقيقية، **وسجلّ التدقيق لا يُمحى أبداً**: يُعدّ القابلُ للأرشفة ويُترك مكانه.
3. **حقّ النسيان يُنفَّذ على الهوية الحيّة.** طلب `erase` يُخفي حقول `users`، ويسحب العضويات
   (`status='suspended'` — الجدول لا يعرف `revoked`)، ويُبطل الجلسات (`refresh_tokens.revoked_at`),
   **ويُبقي صفوف التدقيق ويقول كم بقي**.

**ولا مقابل لهاتين الشاشتين في `Desktop_ERP`**: سطح اللوحة كلّه (منذ P-C1) سطحٌ **لا نظير له في
المنتج المكتبي** — المكتبي يخدم شركةً واحدة ولا يعرف مستأجرين ولا نسخاً منصّية، فلا سطرَ يُقتبس
منه. ولذلك كل تسميةٍ عربية هنا **مخترعة** ومُدرجة في §5.

### 2.5 الشاشتان في اللوحة

| الشاشة | ما تعرضه |
|---|---|
| `/backups` — «البيانات والنسخ» | نموذج التشغيل (نطاق + مستأجر + ملاحظة) · جدول النسخ: بدأت · النطاق · الحالة · **الحجم** · المحتوى (جداول/صفوف) · **الوجهة** · **البصمة** · فحصٌ سابق؟ · زرّا «تحقّق» و«تنزيل» · بطاقة **نتيجة التحقّق** (الصيغة · الأسطر · الجداول · الصفوف · الحكم · جداول منحرفة) · بطاقة **سياسة الاحتفاظ** بخمس نوافذ وعدّاداتها وزرّي «قِس بلا تنفيذ» و«طبّق الآن» بسببٍ ≥ 5 |
| `/data-requests` — «طلبات البيانات» | فتح طلب (نوع · مستأجر · بريد · ملاحظة) · جدول الطلبات: فُتح · النوع · العميل · صاحب البيانات (+`subjectRef`) · الحالة · **النتيجة** (كم مستخدماً أُخفي · كم عضوية سُحبت · كم جلسة أُبطلت · كم صفّ تدقيق بقي / أو حجم الملف وبصمته) · «اقبل»/«ارفض» بسبب · «نفّذ» مع **حقل تأكيد بكتابة البريد** للمحو |

وكلتاهما تُخفي الفعل بلا `console.backups.manage`، وتعرض `Forbidden` بدل أن تُحاول.

### 2.6 سكربت التحقّق الجديد `scripts/verify-platform-backups.mjs` — 87/87 في 9 أقسام

يشغّل الـAPI الحيّ ولا يوكل شيئاً:

1. **الأبواب** — المالك يمرّ، والعميل 403 على الثلاثة، والمجهول 401.
2. **نسخةٌ حقيقية** — تتشغّل، ويُقاس **حجم الملف من القرص** ويُقارن بالحقل المسجَّل (مِلي بايت
   بمِلي بايت)، والبصمة 64 حرفاً سداسياً، والوجهة مُعلَنة.
3. **التشفير** — الملف على القرص يبدأ بـ`ERP-BACKUP/1` **ولا يحتوي نصّ NDJSON الصريح**.
4. **التحقّق** — يعيد القراءة، والعدّادات من داخل الملف، والاستعادة التجريبية `ready` بلا جدول مفقود.
5. **العبث** — يُقلب بايتٌ في الملف على القرص ⇒ `verified:false` و`unreadable`، ثم يُعاد البايت فينجح.
6. **الرابط الموقّع** — يُنزَّل بلا رمز حامل، ويُطابق عدّاداته، والمزوَّر 403 والمنتهي 410.
7. **الاحتفاظ** — يُقرأ، ونافذة خارج حدّها 400 ولا تُكتب، ونافذة صالحة تُحفظ وتُقرأ ثم **تُعاد**
   لقيمتها، والقياس التجريبي يغطّي الأهداف الأربعة **ولا يحذف شيئاً**.
8. **طلبات البيانات** — يُفتح طلبٌ لصاحب بيانات **يُنشئه السكربت نفسه عبر `POST /memberships`**
   (فلا تُمسّ هوية `owner@demo.test` التي تسجّل بها كل السكربتات الأخرى)، والتنفيذ قبل القرار 422،
   والقرار مرّة واحدة، والتصدير يُنزَّل بمحتواه ويُرفض بتوقيعٍ مزوَّر، والمحو يُرفض بلا تأكيد
   وبخطأ، ثم يُخفّي الهوية (9 حقول) ويسحب العضوية ويُبقي الإيصال — **ويُقاس أن بريده الأصلي لم
   يبقَ في دليل المستخدمين**.
9. **التدقيق** — الأفعال التسعة كلها مقيَّدة في `audit_log` بأسمائها.

### 2.7 قسمٌ جديد في `verify-platform-console.mjs` (§20 — 200/200 في 21 قسماً)

قراءةٌ فقط بقصد: النسخ تُقرأ بحقولها، وسياسة الاحتفاظ بنوافذها وعدّاداتها و`auditHardDeleteAllowed:false`،
والطلبات بأنواعها وحالاتها، والعميل 403 على الثلاثة، وسجلّ الرموز صار **18** والرمز الجديد فيه.
**والقراءة هنا لا تكتب** — لأن §21 «التنظيف» يقارن إعدادات المنصّة بما كانت عليه.

---

## 3. القرارات المصمَّمة (بأسبابها)

1. **لا `pg_dump` في البيئة ⇒ النسخة تُبنى من الكتالوج الحيّ.**
   البيئة لا تحمل `pg_dump` (Postgres مُدمَج للتطوير)، والقرار أن تُقرأ **قائمة الجداول من
   `pg_class`/`pg_policies` نفسها** لا من قائمةٍ في الكود: نسخةٌ من قائمةٍ مكتوبة يدوياً تنسى
   الجدول الذي أضافه ترحيلُ الأمس — وهو بالضبط الجدول الذي ستحتاجه غداً. الوحدة في
   `backup-dump.ts` مصمَّمة لتقبل مخرجات `pg_dump` الخام أيضاً (`parseDump`)، فالترقية إلى
   `pg_dump` الحقيقي لاحقاً لا تكسر الشكل.

2. **نطاق المنصّة = مروران لا مرورٌ واحد.**
   جداول المنصّة تُقرأ **مرّة** في سياق المنصّة (`withPlatformAdminTx`)، ثم يمرّ المشغّل على كل
   مستأجر في سياقه (`withTenantTx` + `app.tenant_id`) — لأن `RLS` هنا هو من يعرف ما يخصّ مَن،
   وقراءة جدولٍ مستأجريّ من سياق المنصّة تُخرج «صفراً صامتاً» أو تكسر العزل. وصفُّ `tenants` في
   الملف يقيس: نسخةُ المنصّة الحيّة تحمل أكثر من مستأجرين.

3. **ونطاق المستأجر يُعلن ما أسقطه.**
   نسخةُ مستأجرٍ واحد تُسقط جداول المنصّة (billing_plans · permissions · …) وتقرأ ما يحمل
   `tenant_id` من جداول المنصّة (`audit_log` · `email_messages` · `files` …) + `users` عبر
   `memberships`، ثم **تكتب أسماء ما أسقطته في ذيل الملف** (`skippedTables`). نسخةٌ تبدو كاملةً
   وهي ناقصة أخطر من نسخةٍ تقول ما أسقطته.

4. **التشفير دائم ولا يُختار.** لا «نسخةٌ بلا تشفير في التطوير»: المفتاح من `DATA_ENC_KEY` وإلا
   من `FILE_URL_SIGNING_SECRET`، والملف يبدأ بترويسة `ERP-BACKUP/1 aes-256-gcm <iv> <bytes>`
   ويُفكّ بـGCM (فبايتٌ محوَّل يُرفض بوسم التأكيد لا بخطأ قراءة). ونتيجة ذلك أن **الملف على القرص
   ليس NDJSON** — والسكربت الحيّ يقيس هذه الحقيقة نصّاً.

5. **الوجهة تُعلَن ولا تُخفى.** `S3ArtifactStore` عند `isConfigured()`، وإلا `FileArtifactStore`
   بديلٌ **مُعلَن** يظهر في `store` لكل صفّ. وأُضيف `BACKUP_STORE` (`auto` الافتراضي · `s3` ·
   `filesystem`) لتثبيت الوجهة صراحةً في بيئةٍ تُدار، ولأن **الصمت** هو المشكلة: بيئةٌ تظنّ أنها
   تنسخ إلى S3 وهي تكتب على قرص الخادم هي نفسها التي لا تصمد لفقدان القرص. و`.env` في بيئة
   التطوير يثبّتها على `filesystem` (لا MinIO هنا) فيصير سلوك التشغيل الحيّ معلوماً لا مفترضاً.

6. **التحميل بقدرةٍ موقّعة على نهايتين عامّتين.** `expires` + `signature` على `(id|scope|expires)`
   هو التصريح؛ لا رمز حامل ولا قراءة مستأجر من الطلب (النمط نفسه في `/files/:id/content`).
   والقرار مسجَّل في بوابتين: اختبار `permission-codes.spec.ts` صار يقبل مساراً عامّاً **إن كان
   مُعلَناً في قائمةٍ بالاسم ومُتحقَّقاً من توقيعه** (`verifyDownloadToken`) — و`@Public()` عارية
   تُسقط البوابة.

7. **الاستعادة التجريبية تقارن ولا تكتب.** الكتابة فوق بياناتٍ حيّة تحتاج نافذة صيانة؛ والظاهر
   هنا أقوى ما يمكن إثباته بلا كتابة: يُفكّ الملف ويُمسح سطراً سطراً وتُقارن جداوله بكتالوج
   القاعدة الحيّ ⇒ `ready` أو `drifted` (بأسماء الجداول المفقودة) أو `unreadable`.

8. **المحو إخفاءُ هويةٍ لا حذف صفوف.** `email` → `erased+<sha12>@erased.invalid` · `phone` ·
   `password_hash` · `mfa_secret_enc` ⇒ NULL · `full_name` ⇒ «Erased user» · `status` ⇒ `suspended`
   (9 حقول)؛ والعضويات `suspended` والجلسات مُبطلة. ثم **يُقال الرقم**: كم صفّ تدقيقٍ بقي يذكر
   صاحبه. ومحوُ سجلّ «من فعل ماذا» يمحو الحقوق نفسها — فالتدقيق يبقى، والقاعدة تمنع UPDATE/DELETE
   عليه أصلاً.

9. **التأكيد يكلّف شيئاً.** المحو لا يُنفَّذ بضغطة: يُكتب **بريد صاحب البيانات نفسه** في الحقل،
   وإلا 422 — والسبب مكتوبٌ في الفعل نفسه لا في إرشادٍ بالشاشة.

10. **`audit` مستثنى من التنفيذ في المخطّط لا في الشاشة.** `targets` لا يقبل `audit` (400)، و
    `auditHardDeleteAllowed:false` مُعلَن في الاستجابة، والعدّاد `auditArchivable` يُعرض ليعرف
    المشغّل الحجم الذي **لن** يُحذف. الاستثناء في العقد أوضح من تعطيل زرّ.

---

## 4. ما لم يُنفَّذ (بصراحة)

| البند | لماذا |
|---|---|
| **جدولة النسخ** (وردت في سرد الشاشة) | النسخة تُشغَّل الآن بطلبٍ من اللوحة أو من الـAPI، ولا مُجدولٌ يعيدها كل ليلة. المُجدول الحقيقي يحتاج عاملَ طابورٍ دائماً (`WORKER=0` في هذه البيئة)، والبناء هنا أن **النسخة تُقاس وتُتحقَّق**؛ الجدولة سطرُ تشغيلٍ فوقها. |
| **استعادةٌ تكتب فعلاً** | تحتاج نافذة صيانة وقراراً تشغيلياً (أي جداول؟ بأيّ ترتيب؟ وماذا عن الصفوف التي تغيّرت بعد النسخة؟). المعروض هو التخطيط والانحراف — لا الكتابة. |
| **صيغة `pg_dump` الخارجية** | تُقرأ مخرجاته (`parseDump`) لكن لا تُستدعى الأداة (غير موجودة في البيئة). الشكل جاهزٌ للتبديل. |
| **محو صفوف التدقيق** | **قرارٌ لا نقص**: `REVOKE UPDATE, DELETE ON audit_log FROM erp_api` قائم، والعدّاد يُعرض ولا يُنفَّذ. |
| **سياسة احتفاظ لكل مستأجر** | النوافذ منصّية واحدة اليوم — وهذا ما تنصّ عليه الخطة («سياسة احتفاظ» بلا تخصيص). |
| **SSO/SAML و2FA** | ملحقٌ مؤجَّل صراحةً من المستخدم، لا علاقة له بهذا الجزء. |

---

## 5. «ما اخترعناه» — التسميات المخترعة وأسبابها

| النصّ المعروض | أين | لماذا اخترناه |
|---|---|---|
| «**البيانات والنسخ**» | بند الشريط `/backups` | الخطة تقول «`/backups`» بالإنجليزية؛ والترجمة الحرفية «النسخ الاحتياطي» لا تشمل الاحتفاظ وطلبات البيانات، وهذا البند صار سطح **كل** ما يتعلّق بالبيانات والاسترجاع |
| «**طلبات البيانات**» | بند الشريط `/data-requests` | الخطة تقول «طلبات تصدير/حذف البيانات الشخصية» داخل سرد شاشة `/backups`؛ ورُفعت شاشةً مستقلة لأن للمحو فعلاً لا رجعة فيه يستحق صفّه وحقل تأكيده |
| «**قِس بلا تنفيذ**» / «**طبّق الآن**» | بطاقة الاحتفاظ | الخطة تقول `dry_run`؛ والصياغة العربية في SHIFT من واجهة المطوّر، فالعربية الطبيعية هنا للمشغّل |
| «**الحكم**» (جاهزة للاستعادة · منحرفة · غير قابلة للقراءة) | نتيجة التحقّق | مقابل `verdict: ready/drifted/unreadable` — الكلمة العربية «حكم» مستعملة في شاشة `/files` لنفس المعنى (حكم الفحص) |
| «**بقي … صفّ تدقيق**» | عمود النتيجة | مقابل `retainedAuditRows` |
| «**نظام ملفات الخادم**» | عمود الوجهة | مقابل `filesystem`؛ كُتب هكذا ليقول المشغّل مكان الكتابة لا اسم التقنية |
| «**يُنشئه السكربت نفسه**» (في ذيل التحقّق) | سكربت التحقّق | ليست تسمية واجهة بل **إعلان قرار**: المحو يُقاس على شخصٍ صنعه السكربت، لا على حساب القياس |

---

## 6. التحقّق

### 6.1 بوابات ثابتة

| البوابة | النتيجة |
|---|---|
| `pnpm --filter @erp/api test` | **1139/1139** في 136 ملفاً (كان 1114/135) |
| `pnpm --filter @erp/contracts test` | **139/139** في 17 ملفاً (كان 128/16) |
| `pnpm --filter @erp/staff test` | **37/37** (لم يُلمس staff في هذا الجزء) |
| `pnpm --filter @erp/platform-admin test` | **24/24** (المسارات 26 بعد إضافة شاشتين) |
| `tsc -p apps/{api,platform-admin,staff}/tsconfig.json --noEmit` | **Exit 0** للثلاثة |
| `pnpm --filter @erp/{api,platform-admin,contracts} lint` | api **8** خطايا قائمة في HEAD (لا واحدة منها في ملفات P-C10) · platform-admin **0** في الملفات الجديدة · contracts **3** قائمة في `billing.ts` |
| `prettier --check` | كل ملفات الجزء نظيفة (`.env.example` بلا مُحلِّل في prettier — لا يخضع للبوابة في HEAD أيضاً) |
| `pnpm -r --filter "./packages/*" build` · `pnpm --filter @erp/api build` · `pnpm --filter @erp/platform-admin build` | Exit 0 |
| `pnpm db:migrate` | «apply 0074 · applied 1 · skipped 74» ⇒ **75** ترحيماً |

### 6.2 تحقّق حيّ على الخادم المحلي

| السكربت | النتيجة |
|---|---|
| `scripts/verify-platform-backups.mjs` (جديد) | **87/87** في **9** أقسام |
| `scripts/verify-platform-console.mjs` | **200/200** في **21** قسماً (كان 187/20؛ §20 جديد + عدّاد الرموز 17 → 18) |
| `scripts/verify-platform-operations.mjs` (P-C9) | **76/76** (لم يُمسّ) |
| `scripts/verify-platform-billing.mjs` · `verify-platform-usage.mjs` · `verify-platform-email.mjs` · `verify-platform-announcements.mjs` · `verify-platform-support.mjs` | **148/148** · **85/85** · **74/74** · **47/47** · **63/63** — أُعيد تشغيلها كلها بعد التغيير: لا كسر |

**فالمجموع الحيّ للوحة: 780 نقطة في 87 قسماً** (كان 679/76):
200 (اللوحة) + 87 (النسخ، جديد) + 76 (العمليات) + 148 (الفوترة) + 85 (الاستخدام) + 74 (البريد)
+ 47 (الإعلانات) + 63 (الدعم).

**واهتزازٌ واحد مسجَّل للأمانة**: تشغيلُ البريد الأول داخل حلقة من ستة سكربتات متتالية أعطى
**73/74** ثم أعطت ثلاث تشغيلاتٍ متتالية بعده **74/74** — الفشل في دخولٍ متكرّر (الحدّ على
`/auth/login`) لا في منطقٍ تغيّر؛ والجزء لم يمسّ البريد.

### 6.3 أخطاء حقيقية كشفها التشغيل الحيّ (وأُصلحت)

1. **`sealArtifact` قسّم الترويسة إلى خمس كلمات وهي أربع.** كتبتُ `const [prefix, version, algorithm, iv, bytes]`
   بينما الترويسة `ERP-BACKUP/1 aes-256-gcm <iv> <bytes>` ⇒ `format` و`algorithm` يقارنان بـ`undefined`
   و**كل تحقّق** يفشل («The stored artifact has no readable header»). كشفه الاختبار الحيّ أولاً، ثم
   ثبّته الاختبار الشبكي: الترويسة أربع كلمات لا خمس.
2. **عمودٌ غير موجود في جدول البريد.** كان `collectDump` يقرأ `email_messages.template` والعمود
   الصحيح `event` (0070) ⇒ نسخةُ المنصّة كانت تفشل كاملةً بـ`column does not exist` (500). لم
   يظهر إلا في التشغيل الحيّ على قاعدةٍ حقيقية — وهذا تحديداً ما تمنعه اختباراتُ القاعدة الوهمية.
3. **`run.tables = 13` في نسخة المنصّة.** المُسبِّب: `const names = …` داخل الحلقتين كان يحجب
   عدّاد الجداول في العدّ الأخير، فالعقد كان يقول 13 بينما الملف يحمل 130+. أُعيد العدّ من
   تجميع المشغّل (`lines` ← الخريطة) ⇒ **العدّاد يُشتقّ من القراءة** لا من متغيّرٍ عابر.
4. **`skippedTables` كانت مُرشَّحة بقائمةٍ خفيّة.** كانت تُسقط `tenants` و`users` و`permissions`
   من قائمة «ما أُسقط» في نسخة المستأجر، فيبدو الملف كاملاً وهو ينقصه جداول المنصّة كلها.
   القائمة أُزيلت: **كل** ما سقط يُسمّى.
5. **مستأجرٌ بمعرّفٍ غير موجود أعطى 500 بدل 404.** كان التحقّق يقع **بعد** إدراج صفّ `running`،
   و`backup_jobs.tenant_id` مفتاحٌ أجنبي ⇒ الإدراج يفشل بخطأ قاعدة. نُقل التحقّق قبل الإدراج ⇒
   `404 No such tenant`.
6. **حقن التبعيات: `FileArtifactStore` لا يُحلّ.** `Nest can't resolve dependencies of the
   FileArtifactStore (?)` لأن الوحدة كانت تُمرّر الصنف مع `useExisting`؛ صار المصنع صريحاً
   (`useFactory: () => new FileArtifactStore()`) مع `ARTIFACT_STORE_S3` بمصنعٍ يستقبل `OBJECT_STORAGE`
   — وهذا ما جعل اختبار الترتيب يحقن مخزناً في مجلد `mkdtemp` بدل قرص الخادم.
7. **خطأ ترحيلٍ حقيقي: `check constraint "backup_jobs_scope_check" already exists`.**
   التسمية كانت تصطدم بالاسم الذي تُولّده PostgreSQL تلقائياً لقيد العمود `scope` **داخل نفس
   الأمر**؛ أُعيد التسمية إلى `backup_jobs_tenant_scope_check` مع تعليقٍ يشرح الفخّ في الملف نفسه
   كي لا يعود. (شُخِّص بـ`/tmp` probe ينفّذ كل جملةٍ وحدها، وبعد 4 صيغ تبيّن أن الاسم هو السبب.)
8. **بوابة P-C1 أسقطت نهايتي المحتوى العامّتين.** `permission-codes.spec.ts` تشترط رمزاً على كل
   مسار `/platform/*`؛ وأُضيف الاستثناء **بالاسم** (مساران مُعلَنان + التحقّق من `verifyDownloadToken`
   في الملف) لا باستثناءٍ أعمى لكل `@Public()`. وبالمناسبة: ترتيب `@Public()` نُقل إلى ما بعد
   مُزيِّن المسار (نمط `files.controller.ts`) فصار يُقرأ في كتلة المسار نفسها.

### 6.4 تصحيحٌ متقاطع (بلا مسّ منطق)

`verify-platform-console.mjs` كان يعدّ رموز اللوحة **17** ويسأل عن `console.jobs.manage`؛ أُضيف
الرمز الجديد إلى القائمة والعدّاد (18) وسؤالٌ عنه في السجل — فالبوابة تُعلن الرمز بدل أن تسقط.

---

## 7. الملفات التي لمسها هذا الجزء

**جديد (11):**

| الملف | الدور |
|---|---|
| `packages/contracts/src/platform/backups.ts` | عقود الجزء كلها (نسخ · احتفاظ · طلبات بيانات) |
| `packages/contracts/src/platform/backups.spec.ts` | **11** اختباراً للعقود |
| `packages/database/migrations/0074_platform_backups.sql` + `down/0074_platform_backups.down.sql` | الجداول الثلاثة + RLS + المنع + الصلاحية |
| `packages/database/src/schema/backups.ts` | وصف drizzle للجداول |
| `apps/api/src/modules/backups/artifact-store.ts` | منفذ `ArtifactStore` + `sealArtifact`/`openArtifact` + المخزنان + `selectArtifactStore` |
| `apps/api/src/modules/backups/backup-dump.ts` | قراءة الكتالوج الحيّ وبناء NDJSON و`parseDump` |
| `apps/api/src/modules/backups/platform-backups.service.ts` | الخدمة (النسخة · التحقّق · الاحتفاظ · طلبات البيانات) |
| `apps/api/src/modules/backups/platform-backups.controller.ts` | **13** مساراً |
| `apps/api/src/modules/backups/platform-backups.module.ts` | الوحدة (مخزنان بمصنعَين صريحَين) |
| `apps/api/test/platform-backups.spec.ts` | **25** اختباراً في 6 مجموعات |
| `apps/platform-admin/app/backups/page.tsx` · `app/data-requests/page.tsx` | الشاشتان |
| `scripts/verify-platform-backups.mjs` | **87/87** في 9 أقسام |

**معدَّل:** `packages/contracts/src/platform/index.ts` · `src/permissions.ts` · `src/rbac.ts` ·
`packages/config/src/env.ts` · `.env.example` · `packages/database/src/schema/index.ts` ·
`apps/api/src/app.module.ts` · `apps/api/src/permission-codes.spec.ts` ·
`apps/platform-admin/lib/navigation.ts` · `tests/routes.spec.ts` · `scripts/verify-platform-console.mjs` ·
`docs/STATUS.md` · `docs/roadmap/README.md` · `docs/roadmap/PLATFORM_CONSOLE_PLAN.md` ·
`docs/roadmap/INCOMPLETE_INVENTORY.md` · `docs/PLATFORM_CONSOLE_P_C10_IMPLEMENTATION_REPORT.md` (هذا الملف).
