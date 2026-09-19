# دليل النشر — Cloud SaaS ERP

> **الجمهور**: من ينشر النظام (عرضٌ محلي · عنقود · إنتاج) ولمن يصلح ما بعد النشر.
> **الملفات المرجعية**: `deploy/` (Docker وKubernetes) · `scripts/local-start.sh` (تشغيلٌ محليّ بأمرٍ واحد) ·
> `.github/workflows/release.yml` (CI/CD).
> **التقارير**: [`DEPLOYMENT_REPORT.md`](./DEPLOYMENT_REPORT.md) — ما نُفِّذ فعلاً وأدلّته ·
> [`DEPLOYMENT_SUMMARY.md`](./DEPLOYMENT_SUMMARY.md) — صفحةٌ واحدة.

---

## 1. ما الذي ننشره؟

أربعة أسطح + قاعدة بيانات:

| السطح | المنفذ | الوصف |
|---|---|---|
| **واجهة الـAPI** (`@erp/api`) | 3000 | NestJS — الحيّوية `/health/live` والجهوزية `/health/ready` |
| **تطبيق الموظفين** (`@erp/staff`) | 3001 | المحاسبة والمخزون والمبيعات (RTL) |
| **الموقع التسويقي** (`@erp/marketing`) | 3002 | الموقع العام + `/pricing` و`/signup` |
| **لوحة المنصة** (`@erp/platform-admin`) | 3003 | تحكّم المنصة (مستأجرون · باقات · بريد · محتوى · نسخ) |
| **PostgreSQL 16** | 5432 | الأدوار: تطبيقٌ محدود `erp_api` ومُرحِّل `erp_migrator` (RLS مفروض) |

**اختياريّان**: Redis (الطوابير — بلا خادمٍ تعمل المهام في المسار نفسه) وSMTP (يتحوّل إلى `console` بلا مزوّد).

---

## 2. أسرع طريق: تشغيلٌ محليّ بأمرٍ واحد (بلا Docker)

### المتطلبات
Node ≥ 20 · pnpm 9 · PostgreSQL 16 (أو `pnpm db:local` الذي يُقلع نسخةً مدمجةً بلا Docker).

### الخطوات
```bash
corepack enable && pnpm install --frozen-lockfile
pnpm env:setup                 # يكتب .env: أسرار JWT والتشفير وكلمات مرور العرض
pnpm db:local                  # طرفيةٌ أولى: PostgreSQL محلي (أو استعمل قاعدةً قائمة)
pnpm start:erp:local           # طرفيةٌ ثانية: يبني ثم يُرحّل ويبذر ثم يُقلع الخمسة
```
وإن أردت **أمراً واحداً** بلا طرفيةٍ ثانية:
```bash
pnpm start:erp:local --with-db
```

### ما يفعله الأمر
1. يقرأ `.env` ويُصدّر متغيّراته.
2. يتحقّق من الأدوات والقاعدة والمنافذ — **ويمنع البناء إن كان خادمٌ يعمل على منفذ النظام** (قاعدة المستودع).
3. يبني الحزم المشتركة ثم الـAPI ثم الواجهات الثلاث **بـ`NODE_ENV=production`** (انظر §7).
4. `pnpm db:migrate` ثم `pnpm db:seed` (تُتخطّى بـ`--no-seed`).
5. يُقلع الخمسة (`setsid`؛ السجلّات في `logs/`؛ الـPIDs في `logs/*.pid`).
6. ينتظر الجهوزية ويطبع جدول الروابط والحالات.

### الخيارات
| الخيار | الأثر |
|---|---|
| `--dev` | الواجهات في وضع التطوير (`next dev`) بدل الإنتاج |
| `--no-build` | استعمل آخر مخرجٍ مبنيّ |
| `--no-seed` | تخطّي الترحيل والبذرة |
| `--with-db` | إقلاع PostgreSQL المحلي إن كان 5432 مغلقاً |
| `--force` | إيقاف ما يشغل منافذ النظام قبل البدء |
| `--stop` | إيقاف كل ما شغّله السكربت (القاعدة لا تُمسّ) |
| `--status` | جدول الحالة (منفذ · PID · HTTP) ثم الخروج |

### الروابط بعد النجاح
```
الموقع التسويقي ......... http://localhost:3002
تطبيق الموظفين ......... http://localhost:3001
لوحة المنصة ............ http://localhost:3003
واجهة الـAPI ........... http://localhost:3000   (/health/live · /health/ready)
```
> **عن المنفذ 3000**: هو منفذ **واجهة الـAPI** في خريطة منافذ هذا المستودع (`.env` → `PORT=3000`)؛
> والموقع التسويقي 3002 والتطبيق 3001 واللوحة 3003. وإن أردت منفذاً واحداً للعرض العام فضع
> وكيلاً عكسياً أمام الأسطح الأربعة (انظر §4، مدخل Kubernetes).

### حسابات العرض
من `.env`: `DEMO_OWNER_EMAIL` (منشأة `demo`) و`PLATFORM_ADMIN_EMAIL` (لوحة المنصة) — وكلمات المرور فيه أيضاً
(يولّدها `pnpm env:setup`).

---

## 3. النشر بـDocker Compose

```bash
cp deploy/.env.example deploy/.env      # ثم املأ الأسرار (انظر §6)
docker compose -f deploy/docker-compose.yml up -d --build
docker compose -f deploy/docker-compose.yml logs -f api     # تابع الإقلاع
```
يبني أربع صور (`erp-api` · `erp-marketing` · `erp-staff` · `erp-platform-admin`) ويُقلع:
postgres · redis · mailhog · **migrate** (Jobُ التهيئة) · api · marketing · staff · platform-admin.

الخدمة `migrate` تعمل مرّةً واحدة وتنفّذ بالترتيب:
`pnpm --dir packages/database run roles` ← `migrate` ← `seed` (إن كان `SEED_DEMO=1`)، ثم تنسحب.
و`api` تنتظرها (`service_completed_successfully`) فلا يقلع نظامٌ على قاعدةٍ غير مهيّأة.

| الأمر | الأثر |
|---|---|
| `docker compose ... ps` | حالة الخدمات |
| `docker compose ... exec api node -e "fetch('http://127.0.0.1:3000/health/ready').then(r=>r.json()).then(console.log)"` | فحص الجهوزية من داخل الشبكة |
| `docker compose ... down` | إيقاف (البيانات في `postgres_data` تبقى) |
| `docker compose ... down -v` | إيقاف **ومحو البيانات** |

البريد في هذه الوصفة على MailHog: `http://localhost:8025` يجمع الرسائل الصادرة.

### ما بداخل الصورتين
* `deploy/Dockerfile.api` — بناء متعدّد المراحل: `pnpm install` ← `pnpm --filter "@erp/api..." build`، ثم صورة تشغيل
  بلا اعتماديات تطوير، بمستخدمٍ غير جذري (`uid 10001`)، بـ`HEALTHCHECK` على `/health/live`.
  ونفس الصورة تُشغّل **العامل** بـ`WORKER=1` (في `main.ts` يفحص المتغيّر فيُقلع مسار العامل بلا مستمع HTTP).
* `deploy/Dockerfile.web` — صورةٌ واحدة لأي تطبيق: `--build-arg APP=marketing|staff|platform-admin|customer-portal`.
  تبني `standalone` وتنسخ الشجرة المستقلّة، وتبدأ بـ`node apps/<APP>/server.js`.

---

## 4. النشر على Kubernetes

```bash
kubectl apply -k deploy/k8s            # الاسم · الإعدادات · البيانات · الترحيل · الـAPI · الواجهات · المدخل
kubectl -n erp get pods,svc,ingress
kubectl -n erp logs job/erp-migrate    # نتائج الترحيل والبذرة
```
قبل ذلك: أنشئ السرّ (نموذجه `deploy/k8s/secret.example.yaml`) — أو:
```bash
kubectl -n erp create secret generic erp-secrets \
  --from-literal=POSTGRES_PASSWORD='…' \
  --from-literal=DATABASE_URL='postgres://erp_api:…@erp-postgres:5432/app' \
  --from-literal=DATABASE_MIGRATOR_URL='postgres://erp_migrator:…@erp-postgres:5432/app' \
  --from-literal=JWT_PRIVATE_KEY="$(cat deploy/jwt-private.pem)" \
  --from-literal=JWT_PUBLIC_KEY="$(cat deploy/jwt-public.pem)" \
  --from-literal=DATA_ENC_KEY='…' --from-literal=FILE_URL_SIGNING_SECRET='…'
```
| المورد | الدور |
|---|---|
| `namespace.yaml` | مساحة `erp` + `ResourceQuota` + `LimitRange` |
| `config.yaml` | `ConfigMap erp-config` (غير سرّي: الروابط العامة، CORS، هدف الوكيل، البريد) |
| `data.yaml` | Postgres `StatefulSet` + خدمة رأسية + `volumeClaimTemplates` 20Gi · Redis · MailHog · `NetworkPolicy` تمنع الاتصال بالقاعدة من خارج وحدات المساحة |
| `migrate-job.yaml` | `Job erp-migrate` (أدوار ← ترحيل ← بذرة اختيارياً) بوسم Argo `PreSync` |
| `api.yaml` | `Deployment api` (نسختان، فحوصات `live`/`ready`، `startupProbe`) + `Service` + `Deployment worker` + `HPA` (٢–٦ نسخ على 70٪ معالج) |
| `web.yaml` | ثلاثة `Deployment`/`Service` للواجهات |
| `ingress.yaml` | `erp.example` → الموقع · `app.erp.example` → التطبيق · `platform.erp.example` → اللوحة · `api.erp.example` → الـAPI |
| `kustomization.yaml` | تجميع + تثبيت أسماء الصور ووسومها + عدد النسخ |

**للإنتاج الجدّي**: استبدل Postgres بـRDS/Cloud SQL (نفس مفاتيح السرّ)، واضبط `MAIL_TRANSPORT=smtp` على مزوّدك،
وفعّل `cert-manager` للشهادات، وأبقِ `SEED_DEMO=0`.

---

## 5. CI/CD (`.github/workflows/release.yml`)

* **الزناد**: وسم `v*` أو تشغيلٌ يدوي.
* **المهمّة `images`**: مصفوفةٌ من أربع صور تُبنى بـBuildx وتُدفع إلى GHCR:
  `ghcr.io/<owner>/cloud-saas-erp-{api,marketing,staff,platform-admin}:<tag|latest|sha-…>` مع تخزينٍ مؤقت (GHA cache).
  وبناء الواجهات يستقبل `API_PROXY_TARGET` و`NEXT_PUBLIC_API_BASE_URL` من **متغيّرات المستودع** (Variables)
  لأنهما يُخبزان في `rewrites` وقت البناء (§7).
* **المهمّة `deploy`** (بعد نجاح البناء، وعند التشغيل اليدوي): تُطبّق `deploy/k8s` ثم **تثبّت وسم الإصدار** على
  النشرات (`kubectl set image`) ثم تُعيد تشغيل Job الترحيل ثم تنتظر `rollout status`.
  وإن لم يكن السرّ `KUBE_CONFIG` مضبوطاً تتخطّى بسلامةٍ وتُعلن ذلك.
* **التحقّق قبل الإصدار**: يعمل `ci.yml` (typegen ← tsc ← lint ← build ← اختبارات ← فحص OAS) على كل دفع.

---

## 6. الأسرار والإعدادات

| المفتاح | لماذا |
|---|---|
| `DATABASE_URL` / `DATABASE_MIGRATOR_URL` | دور التطبيق (محدود، RLS) ودور الترحيل |
| `JWT_PRIVATE_KEY` / `JWT_PUBLIC_KEY` / `JWT_KEY_ID` | توقيع الجلسات (غير متماثل) — يولّدها `pnpm env:setup` |
| `DATA_ENC_KEY` · `FILE_URL_SIGNING_SECRET` | تشفير بياناتٍ حسّاسة وتوقيع روابط الملفات |
| `CORS_ALLOWED_ORIGINS` | نطاقات الأسطح الثلاثة (مفصولةً بفواصل) |
| `MAIL_TRANSPORT` + `SMTP_*` | `console` للتطوير · `smtp` للإنتاج |
| `PUBLIC_APP_URL` · `STAFF_PUBLIC_URL` · `MARKETING_PUBLIC_URL` · `CONSOLE_PUBLIC_URL` | الروابط داخل الرسائل والتحويلات |
| `NEXT_PUBLIC_STAFF_URL` … | روابط الأسطح التي يراها المتصفّح (خريطة `apps/marketing/lib/surfaces.ts`) |
| `WORKER` | `1` لتشغيل العامل (نفس الصورة) · `0` لتعطيله |
| `SIGNUP_ENABLED` · `RATE_LIMIT_*` | بوابة التسجيل الذاتي وحدود الطلبات |

**قاعدة**: `deploy/.env` و`.env` **لا يُودَعان** (`.gitignore`)، ويُودَع النموذج `deploy/.env.example` وحده.
وأسرار Kubernetes في `Secret` — لا في `ConfigMap` ولا في `kustomization.yaml`.

---

## 7. مزالق حقيقية (كل واحدةٍ كلّفتنا تشخيصاً)

1. **`NODE_ENV=development` من `.env` يكسر `next build`**: يطبع Next تحذيراً ثم **يسقط توليد `/404`** برسالةٍ
   مضلّلة: `<Html> should not be imported outside of pages/_document`. الحلّ في السكربت: تثبيت
   `NODE_ENV=production` في كل خطوة بناء. (ومن يُشغّل `pnpm --filter @erp/staff build` بعد `export $(cat .env)`
   سيقع فيها — شغّلها ببيئةٍ نظيفة.)
2. **مسار الفحص هو `/health/live` و`/health/ready`** لا `/health` (والمسارات خارج بادئة `api/v1`).
3. **`.next` مشتركٌ بين dev والبناء الإنتاجي** ⇒ امسحه قبل بناء الإنتاج (`rm -rf apps/*/.next`) لتجنّب أخطاءٍ عابرة.
4. **لا `next build` مع خادمٍ يعمل** (تضارب معروف) — والسكربت يرفض ذلك إلا بـ`--force`.
5. **مخرج `standalone` يجمّد `rewrites`** ⇒ `API_PROXY_TARGET` **وسيط بناء** لا متغيّر تشغيل.
6. **لا مجلّد `public/` في التطبيقات اليوم** ⇒ `COPY .../public` في Dockerfile مُعلَّق (أعِده إن أضفت المجلّد).
7. **العامل ليس مُدخلاً منفصلاً**: هو `apps/api/dist/main.js` مع `WORKER=1` (لا `dist/worker.js`).
8. **`CMD` بالصيغة التنفيذية لا تُستبدل فيها المتغيّرات** ⇒ في `Dockerfile.web` كُتب الأمر بصيغة shell ليُقرأ `APP`.
9. **قراءة `.env` بمُحلِّل التطبيق لا بقراءةٍ ساذجة**: مفاتيح PEM مكتوبة في سطرٍ واحد بـ`\n` حرفية،
   وقراءةٌ لا تُحوّلها تُنتج مفتاحاً مشوّهاً ⇒ كل تسجيل دخول يفشل بـ500 برسالة
   `asn1 encoding routines::header too long`. الأمر الجاهز: `eval "$(node scripts/env-exports.mjs)"`.
10. **`setsid` يتفرّع** إذا كان المُشغِّل زعيم مجموعة (`$!` يصير أَباً عابراً) ⇒ لا تُسجّل PID من
   `$!` بعد `setsid`؛ خلِّ الخدمة تكتب PID نفسها، وأبقِ شبكة أمان تُغلق ما يشغل المنفذ.
11. **نافذة الصلاحية في الإنتاج 30 ثانية** (`MARKETING_REVALIDATE_SECONDS`): تعديل المشغّل يظهر في
   الموقع بعدها. وللعرض الفوري (والتحقّق الحيّ) اضبطها `0` — وهو ما يفعله `pnpm start:erp:local`.

---

## 8. الفحص بعد النشر

```bash
curl -s localhost:3000/health/live      # {"status":"ok","service":"api"}
curl -s localhost:3000/health/ready     # checks.database = connected
curl -s localhost:3000/api/v1/public/plans | head -c 200
curl -s -o /dev/null -w '%{http_code}\n' localhost:3002/pricing   # 200
```
وللفحص الوظيفي الكامل (`ls scripts/verify-*.mjs` = **42** سكربتاً حيّاً، وسكربتات الموقع والاشتراك):
```bash
node scripts/verify-marketing-site.mjs   # 37/37
node scripts/verify-pricing.mjs          # 53/53
node scripts/verify-signup.mjs           # 54/54 — الاشتراك والتفعيل (P-M4)
node scripts/verify-platform-console.mjs # 225/225
node scripts/verify-content.mjs          # 62/62
node scripts/verify-weekly-report.mjs    # 35/35
```
> و`verify-signup.mjs` يحتاج خدمة الـAPI وقاعدة البيانات **وخدمة الموقع على `:3002`** (القسم
> الخامس يقرأ HTML صفحة `/onboarding`)، ويقرأ الرمز المُرسل من جدول `email_messages` باتصال
> المشغّل — لأنه لا يُعاد في أي استجابة **عن قصد**.
> هذه السكربتات تعمل بعد النشر بـ`pnpm start:erp:local` (تقرأ `.env` وتضرب `127.0.0.1`).

---

## 9. النسخ الاحتياطي والاستعادة

النسخ والاستعادة جزءٌ من المنتج نفسه (P-C10): شاشة `/backups` في لوحة المنصة، وحصيلةُ النسخ على `BACKUP_STORE`
و`BACKUP_ARTIFACT_DIR`. وللقاعدة على مستوى العنقود: `pg_dump` مجدولٌ من حاويةٍ جانبية أو أداةُ مزوّدك المُدارة،
مع اختبار **استعادةٍ فعليّة** قبل الاعتماد عليه.

---

## 10. ما لم يُنفَّذ في بيئة الإعداد هذه

بيئة العمل التي كُتبت فيها هذه الملفات لا تحتوي Docker (ولا يجوز تثبيته فيها) ولا وصولاً إلى عنقود Kubernetes،
فملفّات `deploy/Dockerfile.*` و`docker-compose.yml` و`k8s/*` **مكتوبةٌ ومراجَعة ولم تُبنَ/لم تُطبَّق فعلياً**؛
وقد فُحصت نحواً وتُرجمت بـ`js-yaml`. **والمُثبَت بالتنفيذ الحيّ هو §2**: `pnpm start:erp:local` شُغّل فعلاً
وأقلع الأسطح الأربعة في وضع الإنتاج (الأدلّة في [`deployment-evidence/`](./deployment-evidence/README.md)).
