# أدلّة التشغيل المحلي (لقطاتٌ نصّية)

> تاريخ التنفيذ: 2026-09-18 00:21 UTC · الأمر: `pnpm start:erp:local` (وضع الإنتاج)
> اللقطات في هذا المجلد **HTML حقيقيّ** أُخذ من الخوادم نفسها مع دمج الأنماط داخل الملف،
> فتُفتح في المتصفّح بلا شبكةٍ وتبدو كالصفحة المعروضة (بلا JavaScript/صور).

## جدول الخدمات (الإخراج الحقيقي لـ `pnpm start:erp:local --status`)

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

## استجابات الـAPI (نصّها كما وصل)

```
GET /health/live
{"status":"ok","service":"api"}

GET /health/ready
{"status":"ok","checks":{"database":"connected","process":"running","memory":"ok"},"uptimeSeconds":160}

GET /api/v1/public/plans
{"data":[{"id":"01a0b19f-d890-7398-b02d-d03bed0c9f65","code":"starter-monthly","name":"الباقة الأساسية","interval":"month","amount":"199.00","currency":"SAR","monthlyAmount":"199.00","annualAmount":"2388.00","entitlements":[{"kind":"limit","key":"limits.max_api_calls_per_day","value":5000,"labelAr":"حدّ استدعاءات الـAPI اليومي","labelEn":"Default daily API-call l

```

## صفحات الواجهات (رمز الحالة + العنوان)

```
3002/ → 200 · نظام تخطيط موارد المؤسسات السحابي
3002/pricing → 200 · الباقات والأسعار · Cloud SaaS ERP
3002/pricing?interval=year → 200 · الباقات والأسعار · Cloud SaaS ERP
3001/ → 200 · Cloud ERP — لوحة التحكم
3001/sales/invoices → 200 · Cloud ERP — لوحة التحكم
3003/ → 200 · لوحة تحكم المنصة — Cloud ERP
3003/content → 200 · لوحة تحكم المنصة — Cloud ERP
3003/plans → 200 · لوحة تحكم المنصة — Cloud ERP
```
