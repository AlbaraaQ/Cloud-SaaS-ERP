# 03 — مصفوفة الأدوار والصلاحيات (Roles & Permissions Matrix)

## 1. أدوار المنصة — Family A (نطاق: كل المنشآت، عبر `platform_memberships`)

| الدور | الغرض | أهم الصلاحيات (`console.*`) |
|---|---|---|
| `platform_owner` | مالك المنصة | كل `console.*` + منح/سحب الأدوار |
| `platform_operations` | التشغيل اليومي | `console.tenants.*`، `console.subscriptions.*`، `console.health.*` |
| `platform_billing` | الفوترة والباقات | `console.plans.*`، `console.subscriptions.*`، `console.activation.*` |
| `platform_support` | الدعم الفني | قراءة المنشآت والاشتراكات + `console.activation.review` |
| `platform_auditor` | التدقيق | قراءة فقط: `console.audit.view` + كل شاشات العرض |

السجل المرجعي: `GET /platform/permissions`. الكتالوج: `GET /platform/roles`.

## 2. أدوار المنشأة الإدارية — Family B (نطاق: منشأة واحدة)

| الدور | النطاق | ملاحظة |
|---|---|---|
| `tenant_owner` | المنشأة كلها | `'*'` داخل منشأته فقط — لا يرى المنشآت الأخرى ولا لوحة المنصة |
| `tenant_admin` | المنشأة كلها | إدارة المستخدمين والأدوار والإعدادات |
| `branch_manager` | فرع/فروع (`branchScope`) | نطاق فرعي |
| `device_manager` | الأجهزة | سجل الأجهزة + ربطها |
| `security_admin` | MFA والسياسات | إدارة المصادقة |
| `auditor` | قراءة | `tenant.audit.view` + التقارير |

## 3. الأدوار الوظيفية — ERP (نطاق: الوحدة + الفرع/المستودع/الخزنة)

| الدور | الوحدة |
|---|---|
| `accountant` | المحاسبة |
| `sales` / `purchase` / `inventory` | المبيعات/المشتريات/المخزون |
| `cashier` | نقاط البيع |
| `treasury` | الخزينة |
| `hr` / `project` | الموارد/المشاريع |
| `report_viewer` | التقارير (قراءة) |

## 4. الأذونات القياسية `tenant.*` (والاسم المستعار القديم)

| القياسي | القديم (يعمل للخلف) |
|---|---|
| `tenant.profile.view` / `tenant.profile.manage` | `platform.tenant.view` / `platform.tenant.manage` |
| `tenant.billing.view` / `tenant.subscription.manage` | `platform.billing.view` / `platform.billing.manage` |
| `tenant.users.view` / `tenant.users.manage` | `platform.users.view` / `platform.users.manage` |
| `tenant.roles.view` / `tenant.roles.manage` | `platform.roles.view` / `platform.roles.manage` |
| `tenant.devices.view` / `tenant.devices.manage` | `platform.devices.view` / `platform.devices.manage` |
| `tenant.audit.view` | `platform.audit.view` |

المرجع الكودي: `packages/contracts/src/permission-aliases.ts`. `can()` في `staff` و`platform-admin` يحل الاتجاهين.

## 5. أنواع العضوية `memberships.kind`

| النوع | السطح | المصادقة |
|---|---|---|
| `staff` | `apps/staff` | بريد + كلمة مرور + TOTP اختياري |
| `buyer` | `apps/customer-portal` | حساب يصدره المورد (كلمة لمرة واحدة ثم تغيير إجباري) |
| `api` | تكاملات الأجهزة | رموز API بصلاحيات محدودة |

## 6. قواعد العزل السلوكي (مُختبَرة في `surface-isolation.spec.ts`)

1. مالك المنشأة لا يرى منشأة أخرى (403 على أي `tenantId` أجنبي).
2. المشتري لا يرى إلا مستنداته (`/portal/*` تحل الطرف من الرمز).
3. مشغّل المنصة بلا عضوية منشأة لا يرى بيانات تشغيلية.
4. `tenant_owner` بلا دور منصة يُرفض من `/platform/*`.
5. الجهاز الموقوف تُرفض رموزه.
6. النطاق الفرعي (`branchScope`) يُضيَّق على القراءة والكتابة معاً.
