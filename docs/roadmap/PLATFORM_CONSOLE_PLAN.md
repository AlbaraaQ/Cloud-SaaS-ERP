# خطة لوحة تحكم المنصة — الاحترافية والشمول

> تاريخ الكتابة: 2026-09-17 · الفرع `arena/01a0889e-cloud-saas-erp` · حتى `a5c998d`.
> يُقرأ مع: [`INCOMPLETE_INVENTORY.md`](./INCOMPLETE_INVENTORY.md) §4.2 و
> [`MARKETING_SITE_PLAN.md`](./MARKETING_SITE_PLAN.md) (الموقع يُدار من هذه اللوحة).
>
> **قاعدة التوثيق في هذا الملف:** لوحة المنصة **لا مقابل لها في `Desktop_ERP`** (النسخة
> المكتبية لشركة واحدة، بلا مشتركين ولا اشتراكات). لذلك بوابة «المصدر بالسطر» تُستبدل
> هنا ببوابة «**المصدر من هذا المستودع + مواصفة الخدمة المنشورة**»: كل بند يسمّي الملف
> أو نقطة النهاية القائمة التي سيبني عليها، أو المواصفة الخارجية التي سيُنفَّذ عليها.
> وكل تسمية عربية جديدة تُبرَّر في §9.

---

## 1. الرؤية

لوحة واحدة يمسك بها مُشغّل الخدمة بكل ما يجري: من لحظة دخول العميل المتوقع من الموقع،
إلى اشتراكه وترخيصه، إلى استخدامه اليومي وفوترته، إلى بريده وإشعاراته، إلى دعمه حين
يتعثّر، إلى نسخه الاحتياطية حين تقع الكارثة. **لا شيء يُدار بـ SQL ولا بملف `.env`
بعد اليوم**: كل ما يُضبط اليوم بتحرير البيئة أو بقاعدة البيانات يصير شاشةً موثّقة
ومقنّنة ومُدوَّنة في سجل التدقيق.

ثلاثة مبادئ تحكم الخطة:

1. **الخلفية قبل الشاشة.** لا شاشة بلا نقطة نهاية حقيقية، ولا نقطة نهاية بلا اختبار
   وسكربت تحقّق حيّ (بوابة 7 في `README.md` §4).
2. **المنصة لا تتجاوز المستأجر.** أي وصول إلى بيانات مستأجر من اللوحة (عرض، تعديل،
   دخول مؤقّت) يجب أن يكون مرخَّصاً برمز `console.*`، ومُدوَّناً في سجل التدقيق العابر
   للمستأجرين، ومقيداً بسببه ووقته.
3. **العربية أولاً، والاصطلاح من اللوحة القائمة.** التسميات التي أقرّتها اللوحة اليوم
   (§2) تُعاد كما هي؛ الجديد يُقاس عليها ويُبرَّر.

---

## 2. ما هو قائم اليوم (قياس، لا رواية)

| البند | القيمة |
|---|---|
| الشاشات | 11: `/` · `/tenants` · `/tenants/new` · `/subscriptions` · `/plans` · `/activation-requests` · `/users` · `/roles` · `/audit` · `/health` · `/jobs` |
| التبويب واللسان | `platform-guard.tsx`: نظرة عامة · العملاء · التراخيص · الباقات · طلبات التفعيل · المستخدمون · أدوار المنصة · التدقيق · الصحة |
| نقاط النهاية | 17 تحت `/platform/*` (overview · tenants · plans · subscriptions · activation-requests · users · roles · permissions · منح/سحب الأدوار) + `/audit-log` و`/jobs/outbox` (مستأجر واحد) |
| الرموز الصلاحية | 12 رمز `console.*` معلَناً في `packages/contracts/src/permissions.ts`؛ **المستخدم فعلياً: `console.users.manage` فقط** (مرّتان) |
| الحماية | `PlatformAdminGuard` بادّعاء `pam`؛ جلسات المستأجرين (حتى المالك) ترى `Forbidden` |
| الخدمات المساندة الجاهزة | بريد SMTP فعلي (`platform-services/notifications/mailer.ts`) · إشعارات داخل التطبيق · تدقيق · ملفات (تخزين كائني + توقيع S3 + فحص فيروسات) · مهام + outbox + طابور Redis · مفاتيح تكرار · تسلسل مستندات |

**الفجوة الكبرى:** 11 رمزاً من 12 معلَّقة بلا استخدام، أي أن **كل عملية حساسة في اللوحة
تعمل بادّعاء `pam` وحده** — لا فرق بين من يقرأ ومن يوقف منشأة. إصلاح ذلك هو الجزء الأول.

---

## 3. خريطة الوحدات (12 وحدة)

```
المنصة
├─ 1  الأساس والقشرة            P-C1   (RBAC حقيقي + إعدادات المنصة + تدقيق عابر)
├─ 2  العملاء (المستأجرون)      P-C2
├─ 3  الهوية والوصول            P-C3
├─ 4  الباقات والتراخيص         P-C4
├─ 5  الفوترة والتحصيل          P-C4
├─ 6  الاستخدام والحصص          P-C5
├─ 7  البريد                    P-C6   ← خدمة احترافية مقترحة (§7)
├─ 8  الإعلانات والإشعارات      P-C7
├─ 9  مكتب الدعم + دخول مؤقّت   P-C8
├─ 10 العمليات (مهام · صحة · رايات · ملفات) P-C9
├─ 11 البيانات والاسترجاع       P-C10
├─ 12 المطوّرون (مفاتيح · ويب هوكس) P-C11
└─ 13 التحليلات                 P-C12
```

---

## 4. الأجزاء (كل جزء = جلسة عمل، ببوابات `README.md` §4)

### P-C1 — الأساس والقشرة، وترميم الصلاحيات 🔴 (يبدأ به كل ما بعده)

> ✅ **مُنجَز** (2026-09-17) — [`../PLATFORM_CONSOLE_P_C1_IMPLEMENTATION_REPORT.md`](../PLATFORM_CONSOLE_P_C1_IMPLEMENTATION_REPORT.md).
> الأرقام بعد التنفيذ: API 975 اختباراً · platform-admin 12 · الترحيل 0066 · `scripts/verify-platform-console.mjs` = 70 نقطة في 10 أقسام (70/70 مرتين).
> ملاحظتان للجلسة التالية (P-C2): قراءة الباقات/التراخيص/الطابور مسندة إلى رموز الإدارة (لا توأم قراءة في السجل)،
> و`platform_settings` يحمل `tenant_id` قابلاً للعدم — فهو جاهز لتجاوز المستأجر مباشرةً.

| | |
|---|---|
| **الهدف** | ربط كل نقطة نهاية `/platform/*` برمز `console.*`، وإضافة تدقيق عابر للمستأجرين، وإعدادات منصة مكتوبة |
| **الشاشات** | قشرة جديدة: شريط جانبي بمجموعات (العملاء · المال · التشغيل · المنصة)، شريط علوي (بحث شامل `Ctrl+K` عن مستأجر/مستخدم، جرس التنبيهات، شارة البيئة)، فتات الخبز، ولوحة RTL؛ + شاشة **إعدادات المنصة** (بريد الدعم، النطاقات، الحدود الافتراضية، مفتاح صيانة) |
| **نقاط نهاية جديدة** | `GET /platform/audit` (سجلّ عابر للمستأجرين: `tenantId/actor/action/entity/from/to`) · `GET/PUT /platform/settings` (إعدادات منصّة مكتوبة) · `GET /platform/tenants/search?q=` (لبحث `Ctrl+K`) |
| **صلاحيات** | `console.audit.view` · `console.tenants.view` (قائمان) على كل مسار؛ ولا مسار بلا `@RequiresPlatformRole` بعد هذا الجزء |
| **ترحيل** | `0066_platform_settings.sql` — `platform_settings(key, value jsonb, updated_by, updated_at)` |
| **اختبار** | `apps/api/test/platform-console-rbac.spec.ts` (≥ 14): كل مسار `/platform/*` يُرفض بلا رمزه، ومشغّل «العمليات» لا يوقف منشأة، ومدقّق يقرأ فقط، وعزل المستأجرين |
| **تحقّق حيّ** | `scripts/verify-platform-console.mjs` (≈ 45 نقطة في 8 أقسام) |
| **القبول** | صفر مسار `/platform/*` بلا رمز `console.*` (يُثبته اختبار يمسح الكود) |

### P-C2 — العملاء في العمق 🔴

| | |
|---|---|
| **الهدف** | بطاقة مستأجر واحدة تُجيب عن كل سؤال: من هو؟ ماذا يستهلك؟ هل دفع؟ ماذا حدث له؟ |
| **الشاشات** | `/tenants/[id]` بتبويبات: نظرة عامة (الحالة · الباقة · المستخدمون · الفروع · فواتير آخر 30 يوماً · آخر نشاط) · الاشتراك · المستخدمون · الاستخدام · الرايات · الصحة · التدقيق · الملاحظات |
| **نقاط نهاية** | `GET /platform/tenants/:id` · `GET /platform/tenants/:id/usage` · `PATCH /platform/tenants/:id` (الاسم · الرمز · المنطقة الزمنية · العملة) · `POST /platform/tenants/:id/status` (مع سبب) · `POST /platform/tenants/:id/owner/transfer` · `GET/POST /platform/tenants/:id/notes` · `GET /platform/tenants/:id/settings` + `PUT /platform/tenants/:id/settings/:key` · `GET/PUT /platform/tenants/:id/flags` · `GET/PUT /platform/tenants/:id/branding` (شعار · ألوان · اسم المُرسِل) |
| **صلاحيات** | `console.tenants.view` للقراءة · `console.tenants.manage` للكتابة · **`console.settings.manage`** (جديد) للإعدادات والرايات |
| **ترحيل** | يُكتفى بـ`0066` (الإعدادات) + `tenant_settings` القائم للرايات (`feature.*`) |
| **اختبار** | `platform-tenants.spec.ts` (≥ 12): تعليق/إعادة تنشيط بسبب، نقل الملكية، راية تُقفل وحدة، إعداد يُكتب ويُقرأ، منع تعديل مستأجر آخر، والتدقيق يسجّل الفعل |
| **تحقّق حيّ** | توسيع `verify-platform-console.mjs` |

### P-C3 — الهوية والوصول على المنصة 🟠

| | |
|---|---|
| **الهدف** | إدارة مَن يدير المنصة نفسها، وإدارة جلساته وأجهزته |
| **الشاشات** | `/users` بمطوّر (بحث عبر كل المستأجرين + عمود المنشأة + الأدوار + آخر دخول + حالة 2FA) · بطاقة مستخدم (عضوياته · أدواره · جلساته · إعادة تعيين 2FA · إبطال الجلسات) · `/roles` مصفوفة (الأدوار الخمسة × رموز `console.*`) · دعوة مشغّل |
| **نقاط نهاية** | `GET /platform/users/:id` · `POST /platform/operators/invite` · `POST /platform/users/:id/mfa/reset` · `GET/DELETE /platform/sessions/:id` · `PUT /platform/roles/:code/permissions` |
| **صلاحيات** | `console.users.view` · `console.users.manage` |
| **ترحيل** | — (الجداول قائمة) |
| **اختبار** | `platform-identity.spec.ts` (≥ 10) |
| **امتداد لاحق** | SSO/SAML لكل مستأجر + فرض 2FA على مستأجر بعينه (يبني على `apps/api/src/modules/platform/auth/mfa`) |

### P-C4 — الباقات والتراخيص والفوترة 🟠

| | |
|---|---|
| **الهدف** | من «باقة وسعر» إلى منظومة اشتراك كاملة: حقوق، فواتير، تحصيل، متابعة |
| **الشاشات** | `/plans` بجدول حقوق (وحدة · حدّ · راية) · `/subscriptions` بدورة حياة (تجربة · تفعيل · ترقية/تخفيض · إيقاف مؤقّت · إلغاء) · **`/invoices`** (فواتير الاشتراك + إيصالات + أشعار دائن) · شاشة **المتابعة** (dunning: محاولات، جدولها، رسائلها) · لوحة الإيراد (MRR · ARR · المتأخّر) |
| **نقاط نهاية** | `GET/POST/PATCH /platform/plans/:id` (+ `entitlements`) · `POST /platform/subscriptions/:id/change-plan` (مع proration) · `GET /platform/invoices` · `POST /platform/invoices/:id/issue` · `POST /platform/invoices/:id/pay` · `POST /platform/invoices/:id/void` · `GET /platform/invoices/:id/print` (صفحة HTML جاهزة للطباعة كما في `reporting`) · `POST /platform/dunning/:subscription/run` |
| **الدفع** | يبدأ **يدوياً** (تحويل بنكي + إيصال مرفوع) ثم يُوسَّع: بطاقة عبر **المهايئات القائمة أصلاً** (`modules/payments/gateways/geidea.ts` · `neoleap.ts` — بوابتان سعوديتان مُنفَّذتان ومُختبَرتان) بدل إضافة بوابة أجنبية |
| **الضريبة** | ضريبة قيمة مضافة 15٪ على الاشتراك، وحقل الرقم الضريبي، ورقم الفاتورة المتسلسل (بناءً على `SequencesService`) |
| **صلاحيات** | `console.plans.manage` · `console.subscriptions.manage` · `console.billing.manage` |
| **ترحيل** | `0067_platform_billing.sql` — `billing_plan_entitlements` · `platform_invoices` · `platform_invoice_lines` · `platform_payments` · `dunning_attempts` (كلها RLS) |
| **اختبار** | `platform-billing.spec.ts` (≥ 14): proration، فاتورة ضريبية متوازنة، إلغاء اشتراك، متابعة، منع تكرار التحصيل |
| **تحقّق حيّ** | `scripts/verify-platform-billing.mjs` (≈ 50 نقطة) |

### P-C5 — الاستخدام والحصص 🟠

| | |
|---|---|
| **الهدف** | قياس ما يستهلكه كل عميل، ومنعه عند الحدّ، ومحاسبته على التجاوز |
| **المقاييس** | مستخدمون · فروع · فواتير/شهر · أصناف · تخزين (MB) · استدعاءات API/يوم · رسائل واتساب/شهر · إرسالات بريد/شهر |
| **الشاشات** | تاب «الاستخدام» في بطاقة العميل · شاشة `/usage` (شبكة + رسوم + تصدير) · شاشة للمستأجر في staff (`/settings/usage`) |
| **نقاط نهاية** | `GET /platform/usage?tenantId=&period=` · `GET /platform/usage/export.csv` · `GET /usage` (للمستأجر) |
| **السلوك** | ناعم عند 80٪ (إشعار + راية)، صلب عند 100٪ (رفض برمز خطأ صريح) — ويُبلَّغ عنه في التدقيق |
| **صلاحيات** | `console.tenants.view` + `console.billing.manage` |
| **ترحيل** | `0068_usage_metering.sql` — `usage_counters(tenant_id, metric, period, value)` فريد `(tenant, metric, period)` |
| **اختبار** | `platform-usage.spec.ts` (≥ 8): العدّاد يزيد، الحدّ يمنع، التصدير، عزل |

### P-C6 — خدمة البريد 🔴 (أكبر خدمة احترافية مقترحة — تفصيلها في §7)

### P-C7 — الإعلانات والإشعارات 🟡

| | |
|---|---|
| **الهدف** | أن تصل رسالة المنصة إلى كل مستخدم، في التطبيق وبالبريد |
| **الشاشات** | `/announcements` (إنشاء · استهداف بالباقة أو الحالة · جدولة · معاينة عربية/إنجليزية · قراءات) · **مركز الإشعارات في staff** (جرس + شاشة؛ الخلفية قائمة: `GET/POST /notifications` · `POST /:id/read`) |
| **نقاط نهاية** | `GET/POST/PATCH /platform/announcements` · `POST /platform/announcements/:id/publish` · `GET /platform/announcements/:id/reads` · (staff) لا جديد: الاستهلاك من `/notifications` |
| **صلاحيات** | **`console.notifications.manage`** (جديد) · `tenant.notification.view` (قائم) |
| **ترحيل** | `0070_announcements.sql` — `announcements` · `announcement_reads` |
| **اختبار** | `platform-announcements.spec.ts` (≥ 8) |

### P-C8 — مكتب الدعم والدخول المؤقّت 🟡

| | |
|---|---|
| **الهدف** | تذكرة واحدة لكل مشكلة، ودخول مؤقّت مضبوط حين يلزم النظر بعين العميل |
| **الشاشات** | `/tickets` (صندوق وارد · حالات · أولوية وSLA · إسناد · رودود جاهزة · ملاحظات داخلية · مرفقات عبر وحدة `files`) · `/tickets/[id]` · شاشة **الدخول المؤقّت** (سبب + مدّة + سجلّ) ولافتة حمراء ظاهرة أثناء الجلسة |
| **نقاط نهاية** | `GET/POST/PATCH /platform/tickets` · `POST /platform/tickets/:id/reply` · `POST /platform/impersonate` (سبب + مدّة) · `GET /platform/impersonate/sessions` · `DELETE /platform/impersonate/:id` |
| **الضوابط** | الدخول المؤقّت: سبب إلزامي، مدّة قصوى 60 دقيقة، رمز قصير العمر، يُسجَّل في التدقيق العابر، ويظهر للمستأجر في سجله؛ لا قدرة على تغيير كلمة مرور ولا على حذف |
| **صلاحيات** | `console.support.manage` |
| **ترحيل** | `0071_support_desk.sql` — `support_tickets` · `ticket_messages` · `support_sessions` |
| **اختبار** | `platform-support.spec.ts` (≥ 10): الدخول المؤقّت ينتهي بوقته، ويُسجَّل، ولا يسمح بالحذف |

### P-C9 — العمليات 🟠

| | |
|---|---|
| **الهدف** | تشغيل الخدمة يومياً من شاشة واحدة |
| **الشاشات** | `/jobs` (شبكة outbox: النوع · الحالة · المحاولات · الخطأ · إعادة المحاولة · الإلغاء · الرسائل الميتة + نبض العامل) · `/health` بمجسات (قاعدة · Redis · التخزين · البريد · الطابور) وزمن الاستجابة p95 ونسبة الخطأ وتاريخ التشغيل ولافتة حادث · رايات الميزات وإعدادات المستأجر (واجهتها هنا، نهاياتها في P-C2) · **مدير ملفات** (بحث · حجر · فحص فيروسات · حذف) · **مستكشف التدقيق العابر** مع عارض فرق (`before`/`after` موجودان في كل صف) |
| **نقاط نهاية** | `POST /platform/jobs/:id/retry` · `POST /platform/jobs/:id/cancel` · `GET /platform/health/detailed` · `GET/DELETE /platform/files` · (التدقيق من P-C1) |
| **صلاحيات** | `console.jobs.view` + **`console.jobs.manage`** (جديد) · `console.health.view` |
| **ترحيل** | — |
| **اختبار** | `platform-operations.spec.ts` (≥ 10) |

### P-C10 — البيانات والاسترجاع 🟠

| | |
|---|---|
| **الهدف** | نسخة تُغادر القاعدة فعلاً، وسياسة احتفاظ، وحقّ نسيان |
| **الشاشات** | `/backups` (جدولة · النسخ · الحجم · الحالة · التنزيل · التحقّق من السلامة · استعادة تجريبية) · سياسات الاحتفاظ · طلبات تصدير/حذف البيانات الشخصية |
| **نقاط نهاية** | `POST /platform/backups/run` · `GET /platform/backups` · `GET /platform/backups/:id/download` (رابط موقّت قصير العمر) · `POST /platform/backups/:id/verify` · `GET/PUT /platform/retention` |
| **التخزين** | MinIO/S3 عبر `platform-services/files/object-storage.ts` (قائم) + تشفير + سياسة احتفاظ + سجلّ |
| **صلاحيات** | **`console.backups.manage`** (جديد) |
| **ترحيل** | `0072_platform_backups.sql` — `backup_jobs` · `backup_artifacts` |
| **اختبار** | `platform-backups.spec.ts` (≥ 8)؛ والتحقّق الحيّ بـ`scripts/verify-platform-backups.mjs` |

### P-C11 — بوابة المطوّر 🟢

| | |
|---|---|
| **الهدف** | تكامل رسمي بدل الأبواب الخلفية |
| **الشاشات** | `/api-keys` (لكل مستأجر: نطاقات · تدوير · آخر استخدام · إبطال) · `/webhooks` (الأحداث · العنوان · سرّ التوقيع · إعادة الإرسال · سجلّ التسليم بأكواد الاستجابة) · مستكشف OpenAPI · مستأجر تجريبي (sandbox) |
| **نقاط نهاية** | `GET/POST/DELETE /platform/tenants/:id/api-keys` · `GET/POST/PATCH/DELETE /platform/tenants/:id/webhooks` · `POST /platform/webhooks/:id/test` · `GET /platform/webhooks/:id/deliveries` |
| **الأحداث الأولى** | `invoice.posted` · `invoice.paid` · `invoice.voided` · `stock.below_reorder` · `einvoice.submission_failed` · `shift.closed` · `subscription.*` |
| **صلاحيات** | **`console.apikeys.manage`** · **`console.webhooks.manage`** (جديدان) |
| **ترحيل** | `0073_developer_platform.sql` — `api_keys` (المفتاح مُجزَّأ، لا يُخزَّن نصّاً) · `webhook_endpoints` · `webhook_deliveries` |
| **اختبار** | `platform-developer.spec.ts` (≥ 10): توقيع صالح، إعادة إرسال، إبطال مفتاح، عزل |

### P-C12 — التحليلات 🟢

| | |
|---|---|
| **الهدف** | أن ترى المنصة نفسها كما ترى عملاءها |
| **الشاشات** | `/analytics` (MRR · ARR · التسرب «شعاراتي ومالي» · قمع التفعيل: تسجيل → تفعيل → أول فاتورة → أول إرسال زاتكا · أفواج الاحتفاظ · التحويل من التجربة · الاستخدام لكل باقة · تنبيهات) + تصدير CSV + تقرير أسبوعي بالبريد (يبني على P-C6) |
| **نقاط نهاية** | `GET /platform/analytics/overview` · `GET /platform/analytics/funnel` · `GET /platform/analytics/cohorts` · `GET /platform/analytics/export.csv` |
| **صلاحيات** | **`console.analytics.view`** (جديد) |
| **ترحيل** | — (يقرأ القائم + عدّادات P-C5) |
| **اختبار** | `platform-analytics.spec.ts` (≥ 8) |

---

## 5. جدول الأجزاء

| الجزء | الأولوية | يعتمد على | اختبارات | نقاط التحقّق |
|---|:--:|---|---:|---:|
| P-C1 الأساس والصلاحيات ✅ | 🔴 | — | 20 منفَّذ | 70 منفَّذ |
| P-C2 العملاء في العمق | 🔴 | P-C1 | 12 | +25 |
| P-C3 الهوية والوصول | 🟠 | P-C1 | 10 | +20 |
| P-C4 الباقات والفوترة | 🟠 | P-C2 | 14 | 50 |
| P-C5 الاستخدام والحصص | 🟠 | P-C4 | 8 | +20 |
| P-C6 البريد | 🔴 | P-C1 | 16 | 55 |
| P-C7 الإعلانات والإشعارات | 🟡 | P-C6 | 8 | +20 |
| P-C8 الدعم والدخول المؤقّت | 🟡 | P-C1 | 10 | +25 |
| P-C9 العمليات | 🟠 | P-C1 | 10 | +25 |
| P-C10 البيانات والاسترجاع | 🟠 | P-C1 | 8 | 30 |
| P-C11 بوابة المطوّر | 🟢 | P-C1 | 10 | +25 |
| P-C12 التحليلات | 🟢 | P-C4 · P-C5 | 8 | +20 |

**المجموع المقدَّر:** ≈ 128 اختباراً و≈ 360 نقطة تحقّق حيّة.

---

## 6. الترحيلات المقترحة (من 0066)

| الترحيل | الجداول | الجزء |
|---|---|---|
| `0066_platform_settings.sql` | `platform_settings` | P-C1 |
| `0067_platform_billing.sql` | `billing_plan_entitlements` · `platform_invoices` · `platform_invoice_lines` · `platform_payments` · `dunning_attempts` | P-C4 |
| `0068_usage_metering.sql` | `usage_counters` | P-C5 |
| `0069_email_service.sql` | `email_templates` · `email_messages` · `email_suppressions` · `email_settings` | P-C6 |
| `0070_announcements.sql` | `announcements` · `announcement_reads` | P-C7 |
| `0071_support_desk.sql` | `support_tickets` · `ticket_messages` · `support_sessions` | P-C8 |
| `0072_platform_backups.sql` | `backup_jobs` · `backup_artifacts` | P-C10 |
| `0073_developer_platform.sql` | `api_keys` · `webhook_endpoints` · `webhook_deliveries` | P-C11 |

كل جدول: `tenant_id` حيث يلزم + `RLS` بـ`ENABLE` و`FORCE` وسياسة `tenant_id` + فهارس
`(tenant_id, created_at DESC)` + ملف `down/` مقابل (اتّباعاً لمنهج المراحل 1–23).

---

## 7. خدمة البريد (P-C6) تفصيلاً

### 7.1 لماذا هي أول الخدمات

لأن **كل خدمة أخرى تحتاجها**: تفعيل الحساب، استعادة كلمة المرور، طلبات التفعيل،
الفواتير والمتابعة، الإعلانات، تقارير التحليلات، منح الوصول للبوابة (وهو الوحيد
المُنفَّذ اليوم عبر `MAILER` في `modules/portal/portal.service.ts`). والبنية موجودة:
`MAIL_TRANSPORT=smtp` يسلّم فعلياً عبر عميل SMTP مكتوب على `node:net`
(`platform-services/notifications/mailer.ts`)، و`mailhog` في `docker-compose` للاختبار.
ما ينقص: القوالب، السجلّ، الطابور، الإشراف.

### 7.2 المكوّنات

| المكوّن | الوصف | الجدول |
|---|---|---|
| **القوالب** | قالب لكل حدث، بلغتين (ar/en)، متغيّرات `{{name}}` · `{{invoice_no}}` · `{{amount}}` · `{{link}}`، إصدار، معاينة RTL، **تجاوز لكل مستأجر** (نصّه فقط، لا كود) | `email_templates` |
| **الأحداث** | فهرس ثابت: دعوة مستخدم · استعادة كلمة المرور · رموز استرداد 2FA · منح وصول البوابة · فاتورة جديدة · دفع مستلم · كشف حساب جاهز · رفض/فشل زاتكا · مخزون تحت الحد · فرق إغلاق وردية · اشتراك جديد/متجدد/قارب الانتهاء/فشل دفعه · تفعيل مقبول/مرفوض · إعلان | لا جدول (ثابت في الكود، ويُفعَّل/يُطفأ لكل مستأجر) |
| **السجلّ** | إلى · الموضوع · القالب · الحالة (مُدرَج/مُرسَل/فاشل/مُرجَع) · معرّف المزوّد · الخطأ · المستأجر · المحاولات · فتح/نقرة (اختياري) | `email_messages` |
| **المزوّدون** | `console` (تطوير) · `smtp` (قائم) · مزوّد ويب (SendGrid/Resend/SES) خلف المهايئ نفسه · اختبار اتصال من الشاشة · تبديل بلا إعادة نشر | `email_settings` |
| **الحجر** | قائمة موقوفة (ارتداد/إلغاء اشتراك) تمنع الإرسال قبل الطابور | `email_suppressions` |
| **الحصص** | سقف إرسال لكل مستأجر (يومي/شهري)، ورفض برمز خطأ صريح عند تجاوزه | `usage_counters` (P-C5) |
| **الطابور** | كل إرسال = مهمة outbox + طابور، مع تراجع أُسّي (1د · 5د · 30د) ورسائل ميتة وإعادة محاولة يدوية من الشاشة | `outbox_jobs` (قائم) |
| **التسليم** | شاشة تشرح SPF/DKIM/DMARC لنطاق العميل، ونطاق إرسال مُتحقَّق منه، واسم مُرسِل عربي | `email_settings` |

### 7.3 نقاط النهاية

منصة: `GET/POST/PUT /platform/email/templates` · `POST /platform/email/templates/:id/test` ·
`GET /platform/email/messages` (مرشّحات: المستأجر · الحدث · الحالة · من/إلى) ·
`POST /platform/email/messages/:id/retry` · `GET/PUT /platform/email/settings` ·
`POST /platform/email/settings/test` · `GET/POST/DELETE /platform/email/suppressions`.

مستأجر: `GET /email/templates` · `PUT /email/templates/:event` (تجاوز النصّ فقط) ·
`GET /email/messages` (سجلّه) · `GET/PUT /email/settings` (اسم المُرسِل والعنوان).

### 7.4 الصلاحيات

| الرمز | جديد؟ | السبب |
|---|---|---|
| `console.email.manage` | ✅ | القوالب والإعدادات وإعادة الإرسال تخصّ المنصة |
| `console.email.view` | ✅ | قراءة السجلّ بلا قدرة على الإرسال |
| `tenant.email.template.manage` | ✅ | تجاوز نصّ قالب داخل المستأجر |
| `tenant.email.log.view` | ✅ | أن يرى العميل ما أُرسل باسمه |

(الآلية: إعلان في `packages/contracts/src/permissions.ts` + `INSERT … ON CONFLICT DO NOTHING`
في الترحيل + اختبار `apps/api/src/permission-codes.spec.ts`.)

### 7.5 الاختبار والتحقّق

`apps/api/test/platform-email.spec.ts` (≥ 16): القالب يُصيَّر بمتغيّراته، ومتغيّر ناقص
خطؤه صريح، والحجر يمنع قبل الطابور، والفشل يُسجَّل ويُعاد، والحصة تمنع، وتجاوز المستأجر
يلغي نصّ المنصة، والسجلّ معزول بين المستأجرين.
`scripts/verify-platform-email.mjs` (≈ 55 نقطة في 9 أقسام) — **ولا يُرسل بريداً حقيقياً
أبداً**: كل التشغيل على `console` أو MailHog، والإعدادات تُصوَّر وتُعاد.

---

## 8. خدمات احترافية مقترحة أخرى (خارج الأجزاء الاثني عشر)

| الخدمة | لماذا | الأولوية | يعتمد على |
|---|---|:--:|---|
| SSO/SAML + فرض 2FA لكل مستأجر | عملاء الشركات يطلبونه؛ والبنية (`mfa`) قائمة | 🟡 | P-C3 |
| نطاق مخصّص + هوية بصرية لكل مستأجر (white-label) | بيع الخدمة بعلامة العميل | 🟡 | P-C2 |
| صفحة حالة الخدمة العلنية (status page) | الثقة؛ وتُغذّى من مجسات الصحة | 🟢 | P-C9 |
| تقارير مجدولة تُرسل بالبريد | قيمة يومية للعميل | 🟢 | P-C6 · P-C12 |
| إدارة الشهادات الضريبية (زاتكا) كخدمة مُدارة | حين يتوفّر الاعتماد | 🟢 | P-C2 |
| فحص الفيروسات على المرفقات (إظهار النتائج) | `virus-scanner.ts` قائم بلا شاشة | 🟢 | P-C9 |
| حذف/تصدير البيانات الشخصية (طلبات) | امتثال | 🟡 | P-C10 |
| مخزن أسرار لكل مستأجر (مثل بوابات الدفع) | قائم أصلاً (`secret-box`)؛ واجهة موحّدة | 🟢 | P-C2 |

---

## 9. ما اخترعناه (تبرير كل تسمية وكل مكوّن جديد)

| التسمية / المكوّن | السبب |
|---|---|
| **العملاء** بدل «المستأجرون» في الواجهة | هكذا تسمّيهم اللوحة القائمة (`platform-guard.tsx` و`/tenants`)؛ و«مستأجر» مصطلح تقني |
| **التراخيص** بدل «الاشتراكات» | مذكور في شاشة `subscriptions` القائمة، وهو أقرب للبيع بالباقة |
| **الباقات** | مذكور في `/plans` القائمة |
| **الدخول المؤقّت** | لا يقابله مصطلح في الديسكتوب (لا وجود له)؛ وهو الأصدق لـ *impersonation* المقيَّد بسبب ووقت |
| **المتابعة** (dunning) | مصطلح الفوترة المتعارف عليه؛ ويُستعمل بدل «تحصيل متأخرات» |
| **الحجر** (suppressions) | مقابل *suppression list*؛ وهو أوضح من «قائمة سوداء» |
| **الاستخدام / الحصص** | مقابل *usage / quota*؛ «الحصة» عربية وفصيحة |
| **رايات الميزات** (feature flags) | المفاتيح في الكود تُكتب `feature.*`، فسُمّيت باسمها |
| **الرسائل الميتة** (dead-letter) | مقابل *dead-letter queue* |
| **النظرة العامة / الصحة / التدقيق** | مذكورة في التبويب القائم |
| شاشات **البريد · الإعلانات · الدعم · البيانات · المطوّرون · التحليلات** | لا مقابل لها في اللوحة؛ وهي الوحدات الست التي يطلبها سؤالك صراحةً |

---

## 10. ترتيب التنفيذ المقترح

```
P-C1 ──┬─ P-C2 ── P-C4 ── P-C5 ──┐
       ├─ P-C3                    ├─ P-C12
       ├─ P-C6 ── P-C7            │
       ├─ P-C8                    │
       ├─ P-C9 ───────────────────┤
       ├─ P-C10                   │
       └─ P-C11 ──────────────────┘
```

الجلسة الأولى: **P-C1 كاملاً** (لا شيء بعده آمن بدونه). الثانية: **P-C6** (البريد) لأن
التسويق والمتابعة والتنبيهات كلها تتفرّع عنه. الثالثة: **P-C2 + P-C4** (العملاء
والمال). ثم الباقي بالأولوية.
