# تشغيل النظام محلياً — Run the stack locally

> كل الأوامر تُنفَّذ من جذر المستودع. تحتاج Node ‎22+‎ و pnpm ‎9.15‎ (`corepack enable`).

## 1. التثبيت وتجهيز متغيرات البيئة

```bash
pnpm install
pnpm env:setup     # ينشئ .env في الجذر ويولّد مفاتيح JWT وأسراراً عشوائية
pnpm env:check     # يتحقق أن كل متغير مطلوب موجود ويطبع مصدر كل ملف .env
```

`pnpm env:setup` يكتب ملف `.env` واحداً في جذر المستودع (صلاحيات 600، مستثنى من Git).
كل التطبيقات تقرأ منه: الـ API عبر `@erp/config`، وتطبيقا Next عبر `scripts/dotenv.mjs`
داخل `next.config.mjs`. لا حاجة لنسخ ملفات `.env` داخل كل تطبيق.

## 2. قاعدة البيانات

### أ. مع Docker

```bash
pnpm db:up         # postgres 16 + redis
```

### ب. بدون Docker (موصى به على أجهزة بلا Docker)

```bash
pnpm db:local      # يشغّل PostgreSQL 16 مدمجاً على نفس منفذ DATABASE_URL — اتركه يعمل
```

`pnpm db:local` يستخدم `embedded-postgres` (نفس المحرك الذي تستعمله الاختبارات)، وينشئ
دورَي `erp_api` و`erp_migrator` وقاعدة البيانات تلقائياً. بيانات القاعدة في مجلد مؤقت؛
لجعلها دائمة: `DB_LOCAL_DATA_DIR=/path DB_LOCAL_PERSIST=true pnpm db:local`.

## 3. الترحيلات والبيانات الأولية

```bash
pnpm db:migrate

DEMO_OWNER_PASSWORD='ضع-كلمة-مرور-قوية' \
PLATFORM_ADMIN_EMAIL='admin@platform.test' \
PLATFORM_ADMIN_PASSWORD='ضع-كلمة-مرور-قوية-أخرى' \
pnpm db:seed
```

* `DEMO_OWNER_PASSWORD` → ينشئ منشأة `demo` ومالكها `owner@demo.test`.
* `PLATFORM_ADMIN_EMAIL/PASSWORD` → ينشئ **مدير المنصة** (Super admin) داخل منشأة داخلية
  رمزها `platform`. هذا هو الحساب الوحيد الذي يرى `/platform`.
* سياسة كلمة المرور: 12 حرفاً على الأقل، ثلاث فئات محارف، ولا تحتوي على اسم المستخدم
  أو اسم البريد قبل `@`.

## 4. التشغيل

```bash
pnpm dev           # api :3000 — admin :3001 — customer :3002
```

| الواجهة | العنوان | الدخول |
| --- | --- | --- |
| لوحة التحكم (الموظفون + المنصة) | http://localhost:3001 | رمز المنشأة + البريد + كلمة المرور |
| بوابة العملاء | http://localhost:3002 | نفس البيانات |
| وثائق الـ API | http://localhost:3000/api/docs | — |
| فحص الصحة | http://localhost:3000/health/ready | — |

### تسجيل الدخول

* **مدير المنصة:** رمز المنشأة `platform` + `PLATFORM_ADMIN_EMAIL` + كلمة المرور →
  تظهر له وحدة **لوحة تحكم المنصة** 🛡️ في القائمة الجانبية.
* **مالك منشأة:** رمز المنشأة `demo` + `owner@demo.test` + `DEMO_OWNER_PASSWORD`.
* **عميل جديد:** زر «ليس لديك حساب؟ اشترك الآن» في شاشة الدخول ينشئ منشأة جديدة
  (`POST /api/v1/signup`) مع طلب تفعيل معلّق يعتمده مدير المنصة.

## 5. أول جلسة عمل محاسبية

1. `الإعدادات ← بطاقة فرع` — تأكد من وجود الفرع الرئيسي (يُنشأ تلقائياً).
2. `المحاسبة ← الفترات المحاسبية` — أنشئ السنة المالية؛ تُولَّد 12 فترة شهرية تلقائياً.
3. `المحاسبة ← دليل الحسابات` — أضف حساباتك (أو ابنِ الشجرة من الشاشة نفسها).
4. `المحاسبة ← سند قيد` — سجّل قيداً متوازناً. الفرع والفترة يُشتقّان من التاريخ تلقائياً.
5. `المحاسبة ← ميزان المراجعة / كشف حساب` — تحقق من النتيجة.

## 6. أوامر التحقق

```bash
pnpm lint          # كل الحزم
pnpm build         # كل الحزم
pnpm test          # وحدات + تكامل (تشغّل PostgreSQL مدمجاً تلقائياً)
```

## 7. ملاحظات تشغيلية

* **الـ API في وضع التطوير يُترجم بـ `tsc` وليس `tsx`.** esbuild (المستخدم داخل tsx) لا
  يولّد `emitDecoratorMetadata`، وبدونها يفشل حقن التبعيات في NestJS عند الإقلاع
  (`Nest can't resolve dependencies of the TenantGuard`). المشغّل الجديد
  `apps/api/scripts/dev.mjs` يجمع بين `tsc --watch` و`node --watch`.
* **لا تضبط `NEXT_PUBLIC_API_BASE_URL`** إلا إذا كان الـ API على أصل مختلف؛ القيمة
  الافتراضية (فارغة) تعني نفس الأصل عبر وسيط Next، وهو ما يجعل الواجهة تعمل من أي جهاز
  في الشبكة بلا إعداد CORS.
* `SIGNUP_ENABLED=false` يعطّل نافذة الاشتراك الذاتي في النشرات الخاصة.
