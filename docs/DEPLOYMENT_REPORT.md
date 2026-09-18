# تقرير التنفيذ — ملفّات النشر وتشغيل النظام محلياً

> **التاريخ**: 2026-09-18 · **الفرع**: `arena/01a0acbb-cloud-saas-erp`
> **الدليل الكامل**: [`DEPLOYMENT_GUIDE.md`](./DEPLOYMENT_GUIDE.md) · **الملخّص**: [`DEPLOYMENT_SUMMARY.md`](./DEPLOYMENT_SUMMARY.md)
> **الأدلّة الحيّة**: [`deployment-evidence/`](./deployment-evidence/README.md)

---

## 1. المطلوب والمُسلَّم

| المطلوب | الحالة | الملف/الدليل |
|---|---|---|
| ملفّات نشر (Dockerfile · Kubernetes · CI/CD) | ✅ مكتوبة ومراجَعة | `deploy/Dockerfile.api` · `deploy/Dockerfile.web` · `deploy/docker-compose.yml` · `deploy/.env.example` · `deploy/k8s/` (٩ ملفات) · `.github/workflows/release.yml` |
| نشر النظام محلياً مع PostgreSQL | ✅ **نُفِّذ فعلاً** | `pnpm start:erp:local` — الأسطح الأربعة تعمل على 3000/3001/3002/3003 |
| رابطٌ مباشر `http://localhost:3000` | ✅ | واجهة الـAPI على 3000 (`/health/live` · `/health/ready`) والموقع على 3002 والتطبيق 3001 واللوحة 3003 |
| تأكيدٌ بلقطات | ⚠️ **بديل** | لا متصفّح في بيئة العمل (§5) — القائم: ٤ لقطات HTML مكتفية بذاتها في `docs/deployment-evidence/` بنصّها ورمز حالتها |
| توثيق عربي (دليل + تقرير + ملخّص) | ✅ | الملفات الثلاثة هذه |
| أمرٌ واحد + `scripts/local-start.sh` | ✅ | `pnpm start:erp:local` ⟵ `bash scripts/local-start.sh` |

---

## 2. ما نُفِّذ خطوةً خطوة (الأوامر ومخرجاتها)

```bash
pnpm start:erp:local                 # المحاولة ١ ⇒ فشلت في بناء staff (سببها الحقيقي في §3)
pnpm start:erp:local                 # المحاولة ٢ (مع إعادة محاولة آلية) ⇒ فشلت أيضاً بالمثل
pnpm start:erp:local                 # المحاولة ٣ (بعد تثبيت NODE_ENV=production) ⇒ نجحت
pnpm start:erp:local --status        # جدول الحالة أدناه
```
مخرج `--status` الأخير (مقتطفٌ حقيقي):

```
──────────────────────────────────────────────────────────────────────────────
الخدمة       المنفذ PID      HTTP
──────────────────────────────────────────────────────────────────────────────
واجهة الـAPI 3000     41102    200
العامل الخلفي —      41119    —
تطبيق الموظفين 3001     41172    200
الموقع التسويقي 3002     41195    200
لوحة المنصة 3003     41214    200
──────────────────────────────────────────────────────────────────────────────
قاعدة البيانات   5432     مفتوحة (localhost)
```
والأدلّة الوظيفية (من [`deployment-evidence/README.md`](./deployment-evidence/README.md)):

```
GET /health/live      → {"status":"ok","service":"api"}
GET /health/ready     → {"status":"ok","checks":{"database":"connected",…},"uptimeSeconds":160}
GET /api/v1/public/plans → {"data":[{"code":"starter-monthly","amount":"199.00","currency":"SAR","monthlyAmount":"199.00","annualAmount":"2388.00","entitlements":[…"labelAr","labelEn"…]}…]}
3002/ 200 · 3002/pricing 200 · 3002/pricing?interval=year 200 · 3001/ 200 · 3001/sales/invoices 200 · 3003/ 200 · 3003/content 200 · 3003/plans 200
```
أي أنّ بناءً إنتاجياً حقيقياً (لا وضع تطوير) يخدم الصفحات، وواجهة الـAPI متّصلةٌ بقاعدةٍ مُرحَّلةٍ ومبذورة.

---

## 3. أخطاءٌ حقيقية اكتُشفت وأُصلحت

| # | العَرَض | السبب الجذري | الإصلاح |
|---|---|---|---|
| 1 | **`next build` لـ`apps/staff` يفشل** بـ`<Html> should not be imported outside of pages/_document` عند توليد `/404` — مرّتين متتاليتين داخل السكربت، وينجح عند تشغيله يدوياً | `.env` في الجذر يضبط `NODE_ENV=development`، والسكربت يُصدّره؛ وNext يرفض ذلك («non-standard NODE_ENV») ثم يسقط التوليد برسالةٍ مضلّلة. **الفرق بين البيئتين كان هو الفرق** | `NODE_ENV=production` في **كل** خطوة بناء (حزم · API · واجهات)، وطباعة تحذيرٍ صريح في السكربت وتعليقٌ يشرح القاعدة |
| 2 | فحص الجهوزية يُرجع **404** للـAPI | لا مسار `/health` في هذا المستودع: الصحيحان `/health/live` (حيوية) و`/health/ready` (يفحص قاعدة البيانات) | صُحّح في `scripts/local-start.sh` و`Dockerfile.api` و`docker-compose.yml` و`k8s/api.yaml` (والجهوزية صارت على `ready` فلا يتحوّل خطأ القاعدة إلى بريقٍ أخضر) |
| 3 | «تعثّرٌ عابر» في البناء لا يُفسَّر، وإعادة المحاولة وحدها لا تكفي | `.next` مشتركٌ بين dev والبناء الإنتاجي + ذاكرةٌ محدودة (معالجان) | مسح `.next` قبل كل بناء + محاولةٌ ثانية مُعلنة بسببٍ معلوم + تثبيت `NODE_ENV` (البند ١ كان السبب الفعلي) |
| 4 | `CMD ["node","apps/${APP}/server.js"]` في `Dockerfile.web` كان **لن يعمل** | الصيغة التنفيذية لا تُستبدل فيها متغيّرات البيئة | صيغة shell عن قصد + تعليقٌ يشرح، وحُذف `COPY public` لأن التطبيقات **بلا مجلّد `public/`** (كان سيفشل البناء) |
| 5 | الواجهات في الحاويات كانت ستحاول الاتصال بـ`127.0.0.1:3000` | مخرج `standalone` يجمّد `rewrites` وقت البناء | `API_PROXY_TARGET` **وسيط بناء** في `docker-compose.yml` و`Dockerfile.web` و`release.yml` (وهدفٌ داخلي `http://api:3000` و`http://erp-api:3000`) |
| 6 | `k8s/api.yaml` كان يُشغّل العامل بأمرٍ لوحدةٍ غير موجودة | لا `apps/api/dist/worker.js`: العامل هو `main.ts` نفسه مع `WORKER=1` | `command: ['node','apps/api/dist/main.js']` + `WORKER=1` |
| 7 | `deploy/.env.example` كان **مُتجاهَلاً** من Git | قاعدة `.env.*` تلتقطه | استثناءٌ صريح `!deploy/.env.example` في `.gitignore` (مُتحقَّقٌ منه بـ`git check-ignore`) |
| 8 | **تسجيل الدخول يفشل بـ500 على النشر المحلي**: `asn1 encoding routines::header too long` عند توقيع الجلسة | `.env` يكتب مفتاح `JWT_PRIVATE_KEY` في **سطرٍ واحد** وبـ`\n` **حرفية**، وقراءتي الساذجة لـ`KEY=VALUE` في السكربت لم تُحوّلها إلى أسطر ⇒ مفتاحٌ مشوّه | قراءة `.env` بمُحلِّل التطبيق نفسه عبر `scripts/env-exports.mjs` (يُصدّر `export KEY=$'…'`) — مصدرٌ واحد لا نسختان من قواعد التحليل |
| 9 | `--stop` لم يُوقف إلا خدمةً واحدة، وبقيت المنافذ مشغولة، وملفّات الـPID مفقودة | `setsid` **يتفرّع** إن كان المُشغِّل زعيم مجموعة، فيصير `$!` أَباً عابراً يموت فوراً | القشرة التي تُقلع الخدمة تكتب `$$` ثم تُستبدل بها (`exec`) ⇒ الملفّ يحمل PID الخدمة الحقيقي، ومعه شبكة أمان: إغلاقُ ما يشغل المنفذ فعلاً (`kill_port`) في `--stop` |
| 10 | بعد النشر الإنتاجي سقطت 4 نقاط تحقّق (نشرٌ لا يظهر في الصفحة/الخريطة) | ذاكرة بيانات Next بنافذة **30 ثانية** في الإنتاج (`REVALIDATE_SECONDS`) — وهي سلوكٌ مقصود، لكن السكربتات الحيّة تقيس «فوراً» كما في التطوير | مدّة الصلاحية صارت مفتاحاً (`MARKETING_REVALIDATE_SECONDS`)، وتُضبط `0` في النشر المحلي (بناءً وتشغيلاً)، وخريطة الموقع تأخذ المدّة نفسها من مصدرٍ واحد |

---

## 4. ما بُني في هذه الجولة

**جديدة (١٧ ملفاً)**:
`deploy/Dockerfile.api` · `deploy/Dockerfile.web` · `deploy/docker-compose.yml` · `deploy/.env.example` ·
`deploy/k8s/{kustomization,namespace,config,secret.example,data,migrate-job,api,web,ingress}.yaml` ·
`scripts/local-start.sh` · `.github/workflows/release.yml` ·
`docs/{DEPLOYMENT_GUIDE,DEPLOYMENT_REPORT,DEPLOYMENT_SUMMARY}.md` · `docs/deployment-evidence/` (يُدرج مع الأدلّة).

**معدَّلة**: `package.json` (سطر واحد: `start:erp:local`) · `.gitignore` (٣ أسطر).

**قرارات**:
* **`setsid` لا `nohup`** في إقلاع الخدمات: يُعطي كل خدمة مجموعةً خاصّة، فينظّف `--stop` المجموعة كاملةً
  (المُشغِّل وما أطلقه) ولا يبقى يتيم.
* **البناء بلا Docker هو المسار المُثبَت**: بيئة العمل بلا Docker (وبلا إذن تثبيته)، فصُنع مسارٌ محليّ
  حقيقيّ يُنفَّذ ويُقاس، وبقيت ملفّات الحاويات والعنقود للمراجعة والتسليم.
* **الأسرار لا تدخل Git**: النموذج يُودَع، والقيم في `.env`/`Secret` فقط — والسرّ في Kubernetes لا في
  `ConfigMap`.
* **السبب يُكتب في الكود**: كل مزلقٍ في §3 صار تعليقاً في مكانه، فلا يعود أحدٌ إلى التشخيص نفسه.

---

## 5. ما لم يُنفَّذ — وبصراحة

1. **لقطات PNG**: لا متصفّح في بيئة العمل (لا Chromium/Firefox)، وشبكة التنزيل محجوبة
   (`storage.googleapis.com` و`deb.debian.org` بلا استجابة، فلا `chrome-headless-shell` ولا حزم أنظمة).
   الحاصل بدلاً منها: `docs/deployment-evidence/*.html` — **HTML حقيقيّ** من الخوادم نفسها مع الأنماط
   مدموجةً داخل الملف، تُفتح بلا شبكة وتبدو كالصفحة (بلا JavaScript/صور)، ومعه جدول الحالات ونصوص الاستجابات.
2. **Docker/Kubernetes لم تُبنَ ولم تُطبَّق**: لا Docker daemon ولا عنقود هنا، والملفّات مُتحقَّقٌ من نحوها
   (`js-yaml` لكل YAML) ومراجَعةٌ يدوياً. وحين تتوفّر البيئة: `docker compose -f deploy/docker-compose.yml up -d --build`.
3. **الواجهة عبر منفذٍ واحد**: الأسطح على منافذها الأربعة (خريطة المستودع). جمعُها على 3000 يحتاج وكيلاً
   عكسياً (Ingress/nginx) — والنطاقات المقترحة في `deploy/k8s/ingress.yaml` وموثّقةٌ في الدليل §4.
4. **`customer-portal`** (3004) خارج النطاق الحالي كما في قيود المشروع (لا مسّ `apps/customer*`).

---

## 6. العمل التالي

* **P-M4** — الاشتراك والتفعيل (معالج ٤ خطوات + `POST /signup/verify|resend` + `GET /signup/status/:email`)،
  ثم P-M6 · P-M7 · P-M8/P-M9 · P-M10 بترتيب `docs/roadmap/MARKETING_SITE_PLAN.md`.
