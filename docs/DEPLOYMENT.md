# النشر الكامل — Deploy the full ERP stack

> هذا الدليل يشرح كيف تنشر **النظام كاملاً** (Postgres + Redis + MinIO + API + عامل BullMQ
> + واجهتا Next.js) على **سيرفر واحد مع Docker**، لأن Vercel وحدها لا تستطيع تشغيل الباكند
> الدائم وقاعدة البيانات (انظر نهاية الملف: «لماذا ليست Vercel؟»).

## معمارية الاستضافة الموصى بها

```
Internet
   │
   ▼
 Caddy / nginx (منفذ 80/443, TLS)      ← عكسي، يُوجّه بالنطاقات
   │            │            │
 apps/admin   apps/customer   apps/api     ← حاويات
   │            │            │
   └────────────┴────────────┘
        erp-proxy (شبكة docker داخلية)
        │
        ├── postgres  (16)
        ├── redis     (7)
        └── minio     (تخزين الملفات)
```

كلها تُشغَّل بحاوية/أمر واحد من هذا المستودع.

## ما هو موجود في المستودع أصلاً

* `infrastructure/docker-compose.yml` → postgres + redis + minio + mailhog (للاستضافة
  المحلية/التطوير).
* `apps/admin/next.config.mjs` و`apps/customer/next.config.mjs` → `output: 'standalone'`
  (تنتجان حزمة تشغيل مصغّرة سهلة الحاوية).
* الـ API: `apps/api` يُبنى عبر `tsc` إلى `apps/api/dist/main.js` ويُشغَّل بـ
  `node dist/main.js`، وللـ worker: نفس الحاوية مع `WORKER=1` (يرى `apps/api/src/main.ts`).
* متغيرات البيئة كاملة في `.env.example` وتنشأ تلقائياً بـ `pnpm env:setup`.

## خطوات النشر على سيرفر (Ubuntu 22/24 مثالاً)

### 1) جهّز السيرفر

```bash
# ثبّت Docker + Compose plugin ثم اجلب المشروع
git clone https://github.com/AlbaraaQ/Cloud-SaaS-ERP.git && cd Cloud-SaaS-ERP
```

### 2) جهّز متغيرات البيئة والبنية

```bash
pnpm install
pnpm env:setup          # يولّد .env بأمان وJWT + أسرار + كلمات المرور
pnpm env:check
# ابنِ الحزم الداخلية (شرط لأي أمر قاعدة بيانات)
pnpm --filter @erp/config --filter @erp/contracts --filter @erp/database run build
```

### 3) شغّل قواعد البيانات ثم رحّلها وابذرها

```bash
docker compose -f infrastructure/docker-compose.yml up -d postgres redis minio
pnpm db:migrate
pnpm db:seed
```

### 4) ابنِ التطبيقات وشغّلها

النمط المعتمد للنشر الرسمي (ننصح بإضافته للمستودع وتجربته على السيرفر) هو **ملف نشر
`docker-compose.prod.yml`** يضيف حاويات `api` و`worker` و`admin` و`customer` و`proxy`
فوق خدمات البنية التحتية. كل تطبيق يُبنى من مخرجات مستقرة:

* **API/Worker:** نسخ `apps/api/dist` + `packages/*/dist` (مخرجات `tsc`) وتشغيل
  `node dist/main.js`، مع `WORKER=1` لحاوية منفصلة لعامل BullMQ.
* **admin / customer:** نسخ مخرجات `next build` المستقلة (`.next/standalone`) وتشغيل
  `node server.js`، مع تمرير `PORT`/`ADMIN_PORT`/`CUSTOMER_PORT` و`API_PROXY_TARGET`
  إلى داخل الحاوية.
* **proxy:** إمّا Caddy (توليد TLS تلقائي) أو nginx يُوجّه:
  * `admin.example.com` → حاوية admin
  * `customer.example.com` → حاوية customer
  * `api.example.com` → حاوية api

### 5) حدّث `.env` لقيم الإنتاج

فعّل في السيرفر: `ADMIN_PORT`, `CUSTOMER_PORT`, `API_PROXY_TARGET`,
`CORS_ALLOWED_ORIGINS` للنطاقات، `JWT_*` و`DATA_ENC_KEY` (المولّدة من `env:setup`)، وأسرار
Postgres/Redis/MinIO، ثم أعد تشغيل الحاويات.

## لماذا ليست Vercel؟ (إصلاح مشروع Vercel الحالي)

الخطأ `No Output Directory named "public"` يحدث لأن مشروع Vercel مربوط بجذر الـ monorepo
الذي لا يملك مخرجاً واحداً. **حلول ممكنة:**

* **الأفضل:** لا تستخدم Vercel للنشر الكامل. استخدم VPS + Docker أعلاه.
* إن أردت واجهتين أماميتين فقط على Vercel (مع API مستضاف خارجياً):
  1. أنشئ مشروعَين منفصلين على Vercel: Root Directory = `apps/admin` ثم `apps/customer`.
  2. في إعدادات كل مشروع: Build Command = `pnpm build`، Output Directory = `.next`.
  3. وقف/احذف مشروع Vercel المرتبط بجذر المستودع لوقف هذه الأخطاء عند كل push.
* الـ API لا يُنشر على Vercel؛ يحتاج خدمة باكند دائمة (انظر أعلاه).

## ملاحظة أمانة حول هذه الوثيقة

ملفات نشر الحاويات المحدّدة (Dockerfile لكل تطبيق، `docker-compose.prod.yml`) يجب أن
تُكتب وتُختبر على سيرفر فعلي (تثبيت pnpm، بناء workspace، وصياغة `standalone`). لم تُنشأ
بعد في هذا المستودع كملفات «جاهزة ومُختبَرة» — راجع قسم «الخطوة 4» لتكتبها، أو اطلب
توليدها ثم جرّبها على السيرفر قبل ربط النطاق.
