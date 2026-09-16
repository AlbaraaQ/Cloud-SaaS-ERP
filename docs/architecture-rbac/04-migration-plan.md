# 04 — خطة النقل (Migration Plan)

نُفّذت على فرع `arena/01a0889e-cloud-saas-erp` بدفعتين: الخلفية أولاً ثم الأسطح.

## الدفعة 1 — الخلفية (مُسلَّمة `6680a3e`)

- `platform_roles` + `platform_memberships` + كتالوج Family A في `@erp/contracts`.
- `memberships.kind` + نطاق `tenant.*` + الأسماء المستعارة + `can()` المزدوج.
- سجل الأجهزة `/devices` + `tenant.devices.*`.
- `surface-isolation.spec.ts`: 15 اختباراً يغطي القواعد الست.
- كل اختبارات الحزم خضراء (contracts 58، database 17، api 22+15+11+12+12+12+9…).

## الدفعة 2 — الأسطح (هذه الدفعة)

### النقل مع الحفاظ على التاريخ (`git mv`)

| من | إلى | السبب |
|---|---|---|
| `apps/admin` | `apps/staff` | السطح هو الموظفين لا "الإدارة" العامة |
| `apps/customer` | `apps/marketing` | الغالبية صفحات عامة |
| `apps/admin/app/platform/*` | `apps/platform-admin/app/*` | سطح مستقل، البادئة تُسطَّح إلى `/` |
| `apps/admin/components/platform-guard.tsx` | `apps/platform-admin/components/` | حارس اللوحة يتبعها |
| `apps/admin/components/signup-panel.tsx` | `apps/marketing/components/` | التسجيل وظيفة الموقع العام |
| `apps/customer/app/portal/*` | `apps/customer-portal/app/portal/*` | سطح مستقل |
| `apps/customer/app/auth/*` | `apps/customer-portal/app/auth/*` | مصادقة المشترين تتبع بوابتهم |
| `apps/customer/components/{portal-shell,forms}.tsx` | `apps/customer-portal/components/` | إغلاق البوابة |
| `apps/customer/lib/portal.spec.ts` | `apps/customer-portal/lib/` | عقد البوابة يتبعها |

### النسخ الصغيرة الموثقة (ملفات مشتركة لا تستحق حزمة)

- `platform-admin`: نسخ `globals.css` و`screen.tsx` (حُذف منه `ScreenScaffold`) و`lib/{api,session,use-query}` من `staff`.
- `customer-portal`: نسخ `globals.css` و`layout` (أُعيدت كتابته) و`lib/{api,format,use-async}` و`components/card` من `marketing`.
- `staff/app/settings/audit`: نسخة من صفحة تدقيق اللوحة بمحاذاة نطاق المنشأة.

### شاشات جديدة حقيقية (فوق APIs موجودة)

| الشاشة | الـ API |
|---|---|
| `platform-admin/app/roles` منح/سحب الأدوار | `GET /platform/roles` + `POST/DELETE /platform/users/:id/roles` |
| `staff/app/support/license` لوحة طلب التفعيل | `GET /billing/plans` + `POST /billing/activation-requests` |
| `staff/app/settings/audit` سجلات المستخدمين | `GET /audit-log?limit=100` |
| `marketing/app/login` الدخول الذكي | `POST /auth/login` + كشف `/me` / `/portal/me` |

### وظائف نُقلت لا حُذفت

| الوظيفة | من | إلى |
|---|---|---|
| التسجيل الذاتي | شاشة دخول الإدارة | `marketing/onboarding` (نفس `POST /signup`) |
| طلب التفعيل | زر مكسور في الأسعار العامة | شاشة الترخيص المصادق عليها |
| سجلات المستخدمين | رابط ميت `/platform/audit` | `/settings/audit` بنطاق المنشأة |
| رابط لوحة المنصة | توجيه داخلي `/platform` | رابط خارجي `NEXT_PUBLIC_PLATFORM_URL` |

### صفحات أُعيدت كتابتها لصدق المحتوى

- `marketing/pricing`: عرض الباقات الحية + اشترك/اطلب من لوحة الإدارة (حُذف POST المكسور).
- `marketing/contact`: نموذج يُرسل عبر `mailto` لعنوان قابل للضبط (كان زراً ميتاً).
- `marketing/onboarding`: دُمجت لوحة الاشتراك الحقيقية تحت خطوات التهيئة (كان زراً ميتاً).

### ملفات البنية المتقاطعة

`scripts/next-run.mjs` (4 تطبيقات + منافذ)، `eslint.config.mjs` (مطابقة الأسماء الجديدة)،
`.env.example` (المنافذ + روابط الأسطح)، `scripts/check-env.mjs`، `docs/PROJECT_OVERVIEW.md`.

## التشغيل بعد النقل

```bash
pnpm install
pnpm --filter @erp/api build        # الخلفية
pnpm -r --parallel run dev          # الكل، أو كل سطح على حدة:
pnpm --filter @erp/staff dev        # 3001
pnpm --filter @erp/marketing dev    # 3002
pnpm --filter @erp/platform-admin dev
pnpm --filter @erp/customer-portal dev
```
