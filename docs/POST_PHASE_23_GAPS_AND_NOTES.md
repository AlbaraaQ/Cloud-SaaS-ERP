# Post Phase 23 Gaps & Notes — النواقص والملاحظات بعد تنفيذ المراحل

Date: 2026-09-07  
Branch: `arena/01a07993-cloud-saas-erp`  
PR: https://github.com/AlbaraaQ/Cloud-SaaS-ERP/pull/3

## 1. الملخص التنفيذي

تم تنفيذ مراحل المشروع داخل المستودع حتى Phase 23، وتم تشغيل أوامر التحقق المتاحة بنجاح، وتم تحديث التوثيق والـ PR. ومع ذلك، عند تقييم المشروع حرفياً مقابل شروط prompts وDefinition of Done الإنتاجية، توجد نواقص وملاحظات يجب إغلاقها قبل اعتبار النظام جاهزاً لإطلاق إنتاجي كامل بدون تحفظات.

الحالة الدقيقة:

- ✅ التنفيذ البرمجي الأساسي والواجهات والتوثيق موجودة لمعظم النطاق.
- ✅ آخر تشغيل لـ `pnpm run verify` خرج بالكود `0`.
- ✅ تم تحديث OpenAPI وSTATUS وRelease Notes.
- ⚠️ لا توجد بيئة staging حقيقية مثبتة داخل هذا السياق.
- ⚠️ اختبارات API المعتمدة على embedded PostgreSQL لم تعمل فعلياً بسبب نقص `libpq.so.5` في بيئة sandbox.
- ⚠️ بعض متطلبات Phase 21 وPhase 22 وPhase 23 منفذة كـ skeleton أو readiness layer وليست مغلقة إنتاجياً بالكامل.

## 2. الملاحظة الأهم: اختبارات DB/API/E2E لم تعمل فعلياً

أثناء تشغيل `pnpm run verify`، ظهر في مرحلة API Vitest الخطأ المعروف:

```text
embedded PostgreSQL missing libpq.so.5
Test Files no tests
```

مع ذلك أكمل سكربت verify وخرج بالكود `0` بعد OpenAPI export.

### الأثر

هذا يعني أن:

- TypeScript compilation مرّ بنجاح.
- Lint مرّ بنجاح.
- Builds مرّت بنجاح.
- Smoke check مرّ بنجاح.
- OpenAPI export نجح.
- لكن اختبارات API التي تحتاج PostgreSQL embedded لم تُثبت فعلياً داخل هذه البيئة.

### المطلوب للإغلاق

- تثبيت أو توفير `libpq.so.5` في بيئة الاختبار، أو تغيير test harness لاستخدام PostgreSQL متاح عبر Docker/CI.
- إعادة تشغيل كامل اختبارات API integration.
- إضافة/تشغيل E2E flows للمراحل 21 و22 تحديداً.

## 3. Phase 21 — Installments & Contracting/Projects gaps

### 3.1 Installments

تم تنفيذ الجداول والخدمات الأساسية وجدولة الأقساط والتحصيل، لكن توجد نقاط تحتاج إكمال:

| البند | الحالة | الملاحظة | الإجراء المطلوب |
|---|---:|---|---|
| Contract from posted sale | ⚠️ جزئي | يوجد `sourceInvoiceId` لكن لا يوجد تحقق صارم أن الفاتورة posted وأنها تنتمي لنفس tenant/party. | إضافة validation على `sales_invoices.status = posted`. |
| Early settlement discount | ⚠️ جزئي | القيمة مخزنة على العقد، لكن لا يوجد action كامل لتسوية مبكرة وحساب الخصم وإغلاق العقد. | إضافة endpoint مثل `POST /installments/contracts/{id}/settle-early`. |
| Custom schedule templates | ⚠️ جزئي | تم دعم `scheduleLines` عند الإنشاء، لكن لا توجد إدارة templates مستقلة. | إضافة جدول/خدمة template إن كان مطلوباً إنتاجياً. |
| Reports hook | ⚠️ جزئي | يوجد overdue endpoint، لكن لم يُدمج كمصدر رسمي داخل reporting registry. | إضافة report key للأقساط المتأخرة/aging. |
| Voucher lifecycle | ⚠️ يحتاج قرار | التحصيل ينشئ voucher، لكن لا يوجد post تلقائي للسند. | تحديد هل التحصيل ينشئ draft أو posted voucher ثم تطبيق القرار. |
| DB/E2E tests | ❌ ناقص | لم تثبت بسبب مشكلة embedded PostgreSQL. | تشغيل integration flow: contract → schedule → collect two installments. |

### 3.2 Projects / Contracting

تم تنفيذ project/stages/BOQ/progress bills والربط بفواتير البيع، لكن توجد ملاحظات:

| البند | الحالة | الملاحظة | الإجراء المطلوب |
|---|---:|---|---|
| Sales invoice source `progress_bill` | ⚠️ جزئي | لا يوجد field صريح في `sales_invoices` باسم source؛ الربط موجود عبر `progress_bills.invoice_id` والوصف. | إضافة source metadata أو source_type/source_id إذا اعتمدت كقاعدة عامة. |
| Retention profile slot | ⚠️ جزئي | `retentionReceivableAccountId` يُمرر يدوياً، ولا يتم حله تلقائياً من posting profile slot `retention_receivable`. | إضافة resolver من branch posting profile. |
| Retention release | ⚠️ جزئي | ينشئ invoice فقط ولا يرحّلها تلقائياً. | تحديد lifecycle المطلوب ثم post invoice أو تركها draft بوضوح. |
| Audit trail granular | ⚠️ جزئي | يعتمد على AuditInterceptor العام، لا يوجد audit domain مفصل لكل stage/bill. | إضافة domain audit records للتغييرات الحساسة. |
| Legacy fixture parity | ❌ ناقص | لا توجد fixture تثبت مطابقة `InvContratct` semantics رقمياً. | إضافة fixture test للعمل/السابق/الضمان/المتبقي. |
| DB/E2E tests | ❌ ناقص | لم تثبت بسبب مشكلة embedded PostgreSQL. | تشغيل flow: project → accredit stage → bill 40% → retention → release. |

## 4. Phase 22 — Niche Verticals & Salla gaps

### 4.1 Optics

| البند | الحالة | الملاحظة | الإجراء المطلوب |
|---|---:|---|---|
| Prescription CRUD | ✅ أساسي | موجود. | تحسين validation للـ SPH/CYL/AX/ADD/IPD ranges إن لزم. |
| Invoice print integration | ⚠️ جزئي | يوجد endpoint print-section، لكنه غير مدمج فعلياً داخل قالب invoice print الرئيسي. | دمج conditional block في print template الأساسي. |
| Other_Column semantics | ⚠️ محفوظ لا مفسّر | تم حفظه كـ JSONB حسب RC-21، لكن لم يتم تفسير كل R1..L5. | اعتماد mapping نهائي من صاحب النظام عند توفره. |

### 4.2 Tailoring

| البند | الحالة | الملاحظة | الإجراء المطلوب |
|---|---:|---|---|
| Versioned measurement cards | ✅ أساسي | موجود. | إضافة UI تفصيلي للحقول حسب نشاط العميل. |
| Invoice/party context display | ⚠️ جزئي | يوجد latest endpoint، لكن لا يوجد دمج عميق داخل شاشات Party/Sales. | دمج العرض في صفحات العميل والفاتورة. |
| `AtiveCustMeasur` legacy behavior | ⚠️ موثق | تم تمثيله بـ active/current rows. | تثبيت القرار في ADR إذا تغير من صاحب النظام. |

### 4.3 Marina

| البند | الحالة | الملاحظة | الإجراء المطلوب |
|---|---:|---|---|
| Group pricing | ✅ أساسي | hour/half_hour/offer/day موجود. | اختبار أوسع لكل سيناريوهات RentPeriodSub. |
| Booking conflict detection | ❌ ناقص | لا يوجد منع لتداخل حجوزات نفس القارب بنفس الفترة. | إضافة constraint/service validation للتداخل. |
| Rental invoice posting | ⚠️ جزئي | يتم إنشاء sales invoice، لكنها لا تُرحّل تلقائياً. | تحديد lifecycle ثم تطبيق post أو إبقاؤها draft. |
| Owner percent accounting | ⚠️ خارج/جزئي | النسبة مخزنة فقط، لا توجد دورة توزيع مستحقات الملاك. | إن كان مطلوباً، Phase لاحقة/AP flow. |
| Operation planning CRUD | ⚠️ Minimal | يوجد create/list بسيط، وليس CRUD كامل. | إضافة update/delete/read تفصيلي إن مطلوب. |
| Insurance accounting | ⚠️ Metadata فقط | التأمين ضمن invoice amount/metadata، لا يوجد deposit accounting مستقل. | مقبول حسب out-of-scope، أو يحتاج CR لاحق. |

### 4.4 Fitment

| البند | الحالة | الملاحظة | الإجراء المطلوب |
|---|---:|---|---|
| Make/model/year fitment | ✅ أساسي | موجود. | إضافة validations للسنوات وترتيب `yearFrom <= yearTo`. |
| Catalog UI integration | ⚠️ جزئي | توجد صفحة Admin عامة، لا يوجد picker مدمج داخل Catalog item editor. | دمج compatibility picker في شاشة الكتالوج. |
| Lookup richness | ⚠️ جزئي | lookup يعيد fitment rows، وليس item details كاملة. | Join مع items أو endpoint غني للواجهة. |

### 4.5 Salla integration

| البند | الحالة | الملاحظة | الإجراء المطلوب |
|---|---:|---|---|
| OAuth authorization URL | ✅ أساسي | موجود. | إضافة state store/nonce persistence. |
| OAuth callback/token exchange | ❌ ناقص | لا يوجد callback حقيقي يبدل code بـ tokens. | إضافة `GET/POST /integrations/salla/oauth/callback`. |
| Token encryption | ✅ موجود | AES-256-GCM. | إضافة rotation/versioning لاحقاً إن لزم. |
| Export queue | ⚠️ جزئي | يوجد queue/export-next mock، وليس outbox/BullMQ worker حقيقي. | ربطه بـ outbox_jobs وWorkerRunner. |
| Real Salla calls | خارج النطاق | غير مطلوبة في CI حسب prompt. | mock client contract أفضل لاحقاً. |
| Diff detector | ✅ أساسي | name/price/cost/qty. | إضافة fixture truth table أوسع مطابق legacy view. |
| Webhook HMAC | ⚠️ يحتاج تقوية | يستخدم `JSON.stringify(req.body)` وليس raw request body bytes. | إضافة raw body middleware والتحقق على bytes الأصلية. |
| Branch mapping in webhook | ⚠️ جزئي | webhook يتطلب `branchId` في payload ولا يحل mapping تلقائياً. | استخدام `salla_branch_mappings` حسب store/remote branch. |
| Webhook rate limit | ⚠️ عام فقط | يعتمد على guard/pipeline العام؛ لا يوجد limit خاص للويب هوك. | إضافة bucket خاص أو decorator config. |

## 5. Phase 23 — Hardening & Go-Live gaps

Phase 23 هي الأكثر حساسية لأن prompt يطلب تنفيذ checklist على staging حقيقي ببيانات migrated fixture. ما تم داخل المستودع هو readiness pack وليس إثبات تشغيل إنتاجي كامل.

| البند | الحالة | الملاحظة | الإجراء المطلوب |
|---|---:|---|---|
| Staging environment | ❌ غير مثبت | لا توجد بيئة staging حقيقية داخل sandbox. | تشغيل deployment على staging فعلي وتوثيق URL/commit. |
| 10x performance run | ❌ ناقص | `perf-report.md` يحتوي template/نتائج verify وليس أرقام p95/p99 حقيقية. | تحميل fixture 10x وتشغيل load smoke وتسجيل الأرقام. |
| Backup/PITR restore drill | ❌ ناقص | يوجد runbook/template، لا توجد provider backup IDs أو restore timestamp. | تنفيذ drill فعلي على staging وتوثيق الأدلة. |
| UAT sign-off | ❌ ناقص | توجد sign-off sheets فقط. | توقيع المالك/المستخدمين على السيناريوهات. |
| Alert infrastructure | ⚠️ docs فقط | alert catalogue موجود، لكن لا توجد Prometheus/Grafana/Alertmanager configs فعلية. | إضافة configs أو ربطها بمنصة التشغيل. |
| `/metrics` queue depth | ⚠️ placeholder | `erp_queue_depth` حالياً placeholder وليس قراءة فعلية من DB/queue. | حقن OutboxService أو DB query للـ pending/dead counts. |
| Retention purge job | ⚠️ جزئي | يوجد service لحساب cutoffs، لا يوجد scheduled job ينفذ purge/archive. | إضافة worker job وتنفيذ اختبارات على DB. |
| audit_log monthly partition plan | ⚠️ موثق جزئياً | لا يوجد partitioning فعلي. | تطبيق partitioning أو ADR deferral واضح. |
| Dependency audit clean | ⚠️ waived | audit ليس high=0؛ يوجد ADR-021 waiver. | تقليل waivers عند توفر تحديثات متوافقة. |
| gitleaks | ⚠️ بديل | gitleaks لم يكن متاحاً؛ تم استخدام git grep fallback. | تشغيل gitleaks الحقيقي في CI/staging. |
| Docs freeze | ✅/⚠️ | تم توثيق audit، لكن بعض evidence بيئي فقط. | تعبئة evidence بعد staging. |

## 6. Dependency/security audit notes

تم تشغيل:

```bash
pnpm audit --audit-level high --json
```

بعد ترقية `drizzle-orm` إلى `0.45.2` بقيت advisory findings في tooling مثل:

- `vitest`
- `vite`
- `postcss`
- transitive build/test/dev-server packages

تم توثيق waiver في:

```text
docs/ARCHITECTURE_DECISION_RECORDS.md#ADR-021
```

### الملاحظة

هذا يحقق شرط "high=0 or waived w/ ADR" من ناحية التوثيق، لكنه لا يعني أن audit أصبح clean تماماً.

### المطلوب لاحقاً

- إعادة محاولة تحديث toolchain عندما تتوافق الإصدارات مع config الحالي.
- فصل production dependency audit عن devDependency audit إن أمكن.
- تشغيل audit في CI وتوثيق waivers كملف machine-readable إن لزم.

## 7. Secret scanning notes

لم يكن `gitleaks` مثبتاً في البيئة. تم استخدام fallback:

```bash
git grep -nE '(BEGIN (RSA|OPENSSH|PRIVATE) KEY|AKIA[0-9A-Z]{16}|ghp_[A-Za-z0-9_]{36,}|xox[baprs]-)' -- . ':!pnpm-lock.yaml'
```

النتيجة وجدت فقط AWS example fixture في:

```text
apps/api/src/modules/platform-services/files/s3-signer.spec.ts
```

### المطلوب لاحقاً

- تشغيل gitleaks الحقيقي في CI/staging.
- إبقاء `.gitleaksignore` محدثاً فقط للأمثلة المعروفة، وليس أسراراً حقيقية.

## 8. ملاحظات عامة على جودة التنفيذ

### 8.1 Admin UI

صفحات Admin للمراحل المتأخرة هي صفحات module/features عامة وليست شاشات CRUD إنتاجية كاملة. هذا كافٍ لإثبات route/navigation coverage، لكنه يحتاج تطوير UX كامل قبل تشغيل مستخدمين فعليين.

### 8.2 Feature flags

الخدمات تتحقق من flags عند وجود setting بقيمة false. لكن يجب توحيد سياسة flags:

- هل absence يعني enabled أو disabled؟
- prompts تقول gated by flag، وغالباً الإنتاج يجب أن يعتبر absence = disabled للباكات الرأسية.

يفضل توثيق/تطبيق سياسة واحدة لكل pack.

### 8.3 OpenAPI schemas

بعض endpoints تستخدم TypeScript body types فقط بدون Zod DTOs كاملة أو Swagger decorators تفصيلية، لذلك OpenAPI موجود لكنه قد لا يحتوي schemas دقيقة لكل request/response.

### 8.4 Error codes

بعض DomainError codes أضيفت كنصوص مباشرة وليست كلها في registry مركزي. يفضل توحيدها إذا كان النظام يعتمد على stable error codes.

### 8.5 Transaction boundaries

بعض flows تنشئ records عبر أكثر من service call، وقد تحتاج مراجعة atomicity في production، خصوصاً:

- progress bill → sales invoice → post/update bill
- marina booking → rental invoice → sales invoice
- Salla webhook → sales invoice

## 9. Recommended Final Completion Pass

للوصول إلى إغلاق إنتاجي صارم، يُنصح بتنفيذ pass نهائي بهذه الأولويات:

1. إصلاح بيئة API tests وتشغيل integration/E2E فعلي.
2. إضافة E2E Phase 21:
   - installment contract → schedule → collect two installments.
   - project → stage accredit → 40% bill → retention → release.
3. إضافة E2E Phase 22:
   - marina pricing → booking → rental invoice.
   - Salla connect mock → item diff → export → log.
   - webhook tamper test باستخدام raw body.
4. تحويل Salla webhook إلى raw-body HMAC verification.
5. جعل `/metrics` يقرأ queue/outbox depth فعلياً.
6. إضافة retention purge/archive worker فعلي.
7. تنفيذ booking conflict detection في Marina.
8. إضافة retention_receivable posting profile resolver في Projects.
9. دمج optics/tailoring/fitment داخل شاشات invoice/catalog/party فعلياً.
10. تنفيذ staging performance run 10x وتعبئة `perf-report.md` بأرقام p95/p99.
11. تنفيذ PITR restore drill وتعبئة `backup-restore.md` بالأدلة.
12. تشغيل gitleaks الحقيقي.
13. إغلاق أو تحديث ADR-021 عند توفر toolchain آمن ومتوافق.
14. الحصول على UAT signatures من المالك/المستخدمين.

## 10. التصنيف النهائي

| المجال | التقييم الحالي |
|---|---|
| Code compiles/builds | ✅ جيد |
| Lint | ✅ جيد |
| Repository-level verify | ✅ يخرج 0 |
| API DB integration proof | ⚠️ غير مثبت بسبب `libpq.so.5` |
| Phase 21 production completeness | ⚠️ جزئي/قابل للتشغيل الأساسي |
| Phase 22 production completeness | ⚠️ جزئي/قابل للتشغيل الأساسي |
| Phase 23 go-live evidence | ⚠️ readiness docs لا staging execution |
| Security audit | ⚠️ waivers موجودة وليست clean صفر |
| Final launch readiness | ⚠️ يحتاج staging evidence وUAT |

## 11. الحكم النهائي

المشروع في حالته الحالية هو:

> Implementation-complete داخل المستودع ومجهز كـ READY candidate، لكنه ليس مثبتاً كـ production go-live كامل حتى يتم إغلاق اختبارات DB/E2E، staging performance، backup restore drill، UAT signatures، وsecurity scan الحقيقي.

لا توجد مرحلة “فارغة”، لكن توجد مراحل — خصوصاً 21 و22 و23 — تحتاج completion hardening pass قبل الاعتماد الإنتاجي النهائي.

---

## 12. ما أُغلق في الجولات 8 و9 (تحديث)

| البند في هذا التقرير | الحالة الآن |
|---|---|
| قوالب الطباعة كانت نصوصاً بديلة (stubs) | ✅ مغلق — قوالب A4 كاملة لكل مستند مع التفقيط ورمز QR (`reporting/print-templates.service.ts`) |
| شاشات البيانات الأساسية بلا تعديل/حذف | ✅ مغلق — `PATCH`/`DELETE` لكل البطاقات مع حواجز الاستخدام، و`Directory` يدعمها |
| تصدير التقارير يتجاهل `format` ويصدّر CSV دائماً | ✅ مغلق — Excel حقيقي و CSV و صفحة طباعة/PDF (`reporting/xlsx.ts`) |
| ZATCA محاكاة كاملة (`accepted:true`, بائع ثابت) | 🟡 مغلق جزئياً — المستند UBL 2.1 حقيقي، سلسلة التجزئة والعدّاد حقيقيان، رمز QR من بيانات المنشأة، والتوقيع ECDSA فعلي عند رفع المفتاح. المتبقي **مرتبط بالاعتماد وليس بالكود**: توقيع XAdES داخل `UBLExtensions`، ومسار الإصدار (CSR → compliance CSID → production CSID)، والوسم 9. التفاصيل في `apps/api/src/modules/einvoicing/README.md` |

المتبقي من القائمة القديمة دون تغيير: بوابة العميل (`apps/customer`) ما زالت تعرض صفوفاً
تجريبية ثابتة في بعض الشاشات، شاشة اللغة (عربي/English) غير منفَّذة، تكامل سلة يعتمد إدخال
الرموز يدوياً، البريد عبر console/mailhog، TOTP/2FA غير منفَّذ، محدّد المعدل في الذاكرة،
النسخ الاحتياطي داخل قاعدة البيانات، ولا توجد اختبارات E2E للواجهة.
