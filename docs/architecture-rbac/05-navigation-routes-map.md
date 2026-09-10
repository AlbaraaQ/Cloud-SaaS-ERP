# 05 — خارطة التنقل والمسارات (Navigation & Routes Map)

## marketing — `yourdomain.com` (عام بالكامل)

| المسار | المحتوى | API |
|---|---|---|
| `/` | الصفحة الرئيسية | — |
| `/pricing` | الباقات الحية | `GET /billing/plans` |
| `/onboarding` | الاشتراك الذاتي الحقيقي | `GET /signup/plans` + `POST /signup` |
| `/verify` | التحقق العام من فاتورة | تحقق عام |
| `/contact` | تواصل عبر البريد | `mailto:` قابل للضبط |
| `/login` | الدخول الذكي + جسر الجلسة | `POST /auth/login` |

## staff — `app.*` (موظفو المنشأة، جلسة + `?token=`)

| المسار | المحتوى |
|---|---|
| `/` | لوحة القيادة + رابط خارجي للوحة المنصة (للمشغّلين فقط) |
| `/accounting/*` `/inventory/*` `/purchases/*` `/sales/*` `/hrm/*` `/marina/*` `/projects/*` | وحدات ERP (شجرة `lib/navigation.ts`) |
| `/settings/audit` | **جديد**: سجلات المستخدمين (`tenant.audit.view`) |
| `/settings/change-password` | تغيير كلمة المرور |
| `/support/license` | الاشتراك + **طلب تفعيل** (`POST /billing/activation-requests`) |
| `/s/[...slug]` | هيكل الشاشات غير المنفذة (يعرض الحالة الحقيقية) |

لا `/platform/*` ولا تسجيل ذاتي على هذا السطح.

## platform-admin — `platform.*` (المشغّلون، بلا تسجيل)

| المسار | المحتوى | API |
|---|---|---|
| `/` | نظرة عامة | `GET /platform/overview` |
| `/tenants` `/tenants/new` | العملاء + إنشاء | `GET/POST /platform/tenants` |
| `/subscriptions` | التراخيص | `/platform/subscriptions` |
| `/plans` | الباقات | `/platform/plans` |
| `/activation-requests` | طلبات التفعيل | `/platform/activation-requests` |
| `/users` | المستخدمون + شارات الأدوار | `GET /platform/users` |
| `/roles` | **جديد**: منح/سحب أدوار المنصة | `/platform/roles` + `/platform/users/:id/roles` |
| `/audit` `/jobs` `/health` | التدقيق والمهام والصحة | `/audit-log` `/jobs` `/health` |

## customer-portal — `portal.*` (المشترون، حسابات يصدرها المورد)

| المسار | المحتوى | API |
|---|---|---|
| `/` | تحويل إلى `/portal` | — |
| `/portal` | لوحة الحساب | `GET /portal/me` |
| `/portal/invoices` `[id]` | الفواتير + الطباعة | `GET /portal/invoices/*` |
| `/portal/statement` | كشف الحساب | `GET /portal/statement` |
| `/portal/payments` | المدفوعات | `GET /portal/payments` |
| `/portal/profile` | بيانات التواصل | `GET /portal/me` |
| `/auth/login` `/auth/change-password` `/auth/forgot` | المصادقة | `POST /auth/*` |

لا صفحات تسويقية ولا تسجيل ذاتي على هذا السطح.

## جسور العبور بين الأسطح

- `marketing/login` → `staff/?token=&refresh=&tenant=` أو `portal/...?token=…`.
- `marketing/onboarding` (نجاح) → `staff/?tenant=&email=&joined=1`.
- `staff` (مشغّل منصة) → `NEXT_PUBLIC_PLATFORM_URL`.
- `marketing/pricing` → `staff/support/license` لطلب التفعيل.
