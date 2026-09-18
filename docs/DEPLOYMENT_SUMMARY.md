# ملخّص النشر — صفحةٌ واحدة

**الحالة**: ✅ النظام يعمل محلياً في **وضع الإنتاج** (بُني وشُغّل فعلاً، لا وصفاً نظرياً).
**الأمر الواحد**: `pnpm start:erp:local` (والسكربت: `scripts/local-start.sh`).

| السطح | الرابط | الحالة |
|---|---|---|
| واجهة الـAPI | http://localhost:3000 | 200 · `/health/live` = `{"status":"ok"}` · `/health/ready` = قاعدةٌ متّصلة |
| تطبيق الموظفين | http://localhost:3001 | 200 |
| الموقع التسويقي | http://localhost:3002 | 200 · `/pricing` يعرض الباقات من المنصة |
| لوحة المنصة | http://localhost:3003 | 200 · `/content` · `/plans` |
| PostgreSQL | `localhost:5432` | مُرحَّلة (٧٩ ترحيلاً) ومبذورة |

**ملفّات النشر**: `deploy/Dockerfile.api` · `deploy/Dockerfile.web` · `deploy/docker-compose.yml` ·
`deploy/.env.example` · `deploy/k8s/` (٩ ملفات: اسم · إعدادات · سرّ · بيانات · ترحيل · API · واجهات · مدخل ·
تجميع) · `.github/workflows/release.yml` (٤ صور إلى GHCR + نشرٌ اختياري على Kubernetes).

**مزالق أُصلحت (حقيقية)**: `NODE_ENV=development` القادم من `.env` كان يُسقط `next build` برسالةٍ مضلّلة
(`<Html> should not be imported…`) ⇒ تُثبَّت `NODE_ENV=production` في البناء. · مسار الفحص الصحيح
`/health/live` و`/health/ready` لا `/health`. · `standalone` يجمّد `rewrites` ⇒ `API_PROXY_TARGET` وسيطُ بناء.
· لا مجلّد `public/` في التطبيقات ⇒ حُذف `COPY` من صورة الواجهات. · العامل = `dist/main.js` مع `WORKER=1`. · قراءة `.env` بمُحلِّل التطبيق (`scripts/env-exports.mjs`) لأن مفاتيح PEM في سطرٍ واحد
بـ`\n` حرفية (وإلا: فشل تسجيل دخول 500). · `setsid` يتفرّع فلا يُسجَّل PID من `$!` (الخدمة تكتبه بنفسها).
· نافذة صلاحية المحتوى في الإنتاج 30 ثانية (`MARKETING_REVALIDATE_SECONDS`، و`0` في النشر المحلي للعرض الفوري).

**حدّ صريح**: لا متصفّح في بيئة العمل (وشبكة تنزيله محجوبة)، فلم تُنتَج لقطات PNG؛ البديل المستندي:
`docs/deployment-evidence/` — ٤ لقطات HTML مكتفية بذاتها + جدول الحالات ونصوص استجابات الـAPI.
ولا Docker ولا Kubernetes هنا، فملفّاتهما مُتحقَّقٌ من نحوها (`js-yaml`) ولم تُبنَ فعلياً.

**التفصيل**: [`DEPLOYMENT_GUIDE.md`](./DEPLOYMENT_GUIDE.md) · [`DEPLOYMENT_REPORT.md`](./DEPLOYMENT_REPORT.md).
