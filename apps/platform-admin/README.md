# Platform Admin Console

Arabic-only Next.js 15 App Router console for platform operators. Served from the
`platform.*` domain and deployed separately from every other surface, while calling
the same shared API (`apps/api`).

## Run

```bash
pnpm --filter @erp/platform-admin dev
pnpm --filter @erp/platform-admin build
```

Set `NEXT_PUBLIC_API_BASE_URL` to the API base URL, for example
`http://localhost:3000/api/v1`. Leave it empty to call the same origin — `next.config.mjs`
rewrites `/api/v1/*` (and `/api/health/*`) to `API_PROXY_TARGET`, which is what makes the
console work from any host without touching CORS.

## Structure

- `boot` — `app/layout.tsx` (`dir="rtl"`) → `components/auth-gate.tsx` (login or console)
  → `components/platform-guard.tsx` (**the shell**: sidebar, top bar, omnibox, bell).
- `lib/navigation.ts` — the console's screen tree: four groups (العملاء · المال · التشغيل ·
  المنصة), each item carrying the `console.*` code that opens it. **This file is the single
  source of truth**: `tests/navigation.spec.ts` fails the build when an item claims `ready`
  without a page file, or names a code the registry does not declare.
- `app/` — 14 pages: `overview` (`/`), `tenants`, `tenants/[id]` (customer card), `tenants/new`,
  `subscriptions`, `plans`, `activation-requests`, `users`, `users/[id]` (operator card),
  `roles`, `audit`, `health`, `jobs`, `settings`.
- `components/` — session auth gate, platform-only login screen (no signup path), the shell
  (`PlatformGuard`), and the shared screen kit.
- `lib/` — API client (same origin, refresh-on-401), session provider (`can()` for tenant
  codes, `canConsole()` for `console.*`), `useQuery`, the navigation tree.
- `tests/` — navigation + route/kit coverage checks (20 tests). `tests/routes.spec.ts` owns the
  console's own route list, so a page that exists but is unreachable (or the reverse) fails.

## Screens added by P-C1 (2026-09-17)

| Screen | Route | Console code | Endpoint |
|---|---|---|---|
| إعدادات المنصة | `/settings` | `console.tenants.view` (write: `console.settings.manage`) | `GET/PUT /platform/settings` |
| التدقيق (عابر للمستأجرين) | `/audit` | `console.audit.view` | `GET /platform/audit` |
| المهام والطوابير (عابر للمستأجرين) | `/jobs` | `console.jobs.view` | `GET /platform/jobs/outbox` |

## Screens added by P-C2 (2026-09-17)

| Screen | Route | Console code | Endpoints |
|---|---|---|---|
| بطاقة العميل (8 tabs: نظرة عامة · الاشتراك · المستخدمون · الاستخدام · الرايات · الصحة · التدقيق · الملاحظات) | `/tenants/[id]` | read `console.tenants.view` · write `console.tenants.manage` · settings/flags/branding `console.settings.manage` | 15 routes under `/platform/tenants/:id/*` |

The card is reached from the customers list only (the list row name and its «البطاقة» button);
it is not a sidebar item. Two things in it exist because the console, not the tenant, is the
actor: **suspending a customer requires a written reason** (the API rejects a missing one and
writes it into the customer's own audit trail), and **a limit override is removed by emptying
its field**, which returns the customer to the platform value. Every write lands in the
customer's `audit_log` with `meta.scope = 'platform_console'`.
The settings screen renders entirely from the catalogue the API validates with
(`platformSettingDefinitions` in `@erp/contracts`): labels, help text, kind and default all
come from `GET /platform/settings`, so a key can never exist on one side only. An operator
without `console.settings.manage` sees the form read-only.

## Screens deepened/added by P-C3 (2026-09-17)

| Screen | Route | Console code | Endpoints |
|---|---|---|---|
| المستخدمون (directory across every tenant) | `/users` | read `console.users.view` · invite/write `console.users.manage` | `GET /platform/users` (search) · `POST /platform/operators/invite` |
| بطاقة المشغّل (identification · platform roles · memberships · sessions · 2FA) | `/users/[id]` | read `console.users.view` · the three acts `console.users.manage` | `GET /platform/users/:id` · `POST/DELETE …/roles[/:roleCode]` · `GET /platform/sessions/:id` · `DELETE /platform/sessions/:id?reason=` · `POST …/mfa/reset` |
| مصفوفة الأدوار (the five roles × the 13 `console.*` codes) | `/roles` | read `console.users.view` · write `console.users.manage` | `GET /platform/roles` · `GET /platform/permissions` · `PUT /platform/roles/:code/permissions` |

Three properties of this part are worth knowing before touching it:

1. **The matrix is not decoration.** `PlatformAdminGuard` reads `console.role_permissions`
   overrides (stored in `platform_settings`) on every guarded request, so a *narrowed* role
   stops working immediately — even on a token that was issued seconds earlier — and `/me`
   agrees with the API. A role *grant* still needs a fresh login (the role list itself travels
   in the token).
2. **Every act on a person carries a written reason.** Revoking a session (single or all) and
   resetting 2FA answer `400` without one and write it into `audit_log`
   (`session.revoke`, `operator.mfa_reset`, `operator.invite`, `platform_role.grant|revoke|permissions_update`).
   The revoke reason travels in the **query string**: `apiDelete` sends no body.
3. **Inviting an operator produces a person who can sign in.** The invite grants the platform
   role *and* a role-less membership of the operations tenant (`PLATFORM_TENANT_CODE`,
   default `platform`), because `POST /auth/login` always signs into a tenant; with a temporary
   password the account is `active` and owes a password change, without one it is `invited`
   and waits for the activation e-mail (P-C6).

The user card is reached from the directory (and from the holders table on `/roles`), never
from the sidebar: an operator reading it is already inside the identity area.

## Screens deepened/added by P-C4 (2026-09-17)

| Screen | Route | Console code | Endpoints |
|---|---|---|---|
| الباقات والأسعار (price list + an entitlement per plan: وحدة · حدّ · راية) | `/plans` | read/write `console.plans.manage` | `GET /platform/plans` · `GET /platform/plans/entitlement-keys` · `POST /platform/plans` · `PATCH /platform/plans/:id` (+ reason) · `PUT /platform/plans/:id/entitlements` |
| الاشتراكات والتراخيص (grant with trial · change plan · pause · resume · cancel) | `/subscriptions` | `console.subscriptions.manage` | `GET/POST /platform/subscriptions` · `POST …/:id/change-plan` · `POST …/:id/pause·resume·cancel` (each with a reason) |
| الفواتير والإشعارات الدائنة (**new**) | `/invoices` | `console.billing.manage` | `GET/POST /platform/invoices` · `GET /platform/invoices/:id` · `POST …/:id/issue·pay·void` |
| طباعة الفاتورة (**new**, A4 preview in an isolated frame) | `/invoices/[id]/print` | `console.billing.manage` | `GET /platform/invoices/:id/print` |
| المتابعة والتحصيل (**new**: schedule · attempts · messages) | `/dunning` | `console.billing.manage` | `GET /platform/dunning` · `POST /platform/dunning/:subscription/run` |
| الإيراد (**new**: MRR · ARR · overdue) | `/revenue` | `console.billing.manage` | `GET /platform/revenue` |

What to know before touching the money screens:

1. **The three money codes are the point.** `console.plans.manage`, `console.subscriptions.manage`
   and `console.billing.manage` were declared in P-C1 and unused until here: the price list,
   the licence and the document are three different decisions, and `platform_support` — which
   reads customers all day — holds none of them. The sidebar hides what a session cannot open.
2. **A draft is not a tax document.** It carries no number; issuing allocates the next one from
   the platform series, and voiding keeps it (an auditor reads the sequence). The print preview
   therefore shows the number, the buyer VAT number, the ZATCA QR and the amount in words —
   and nothing at all for a void document.
3. **Money is a two-decimal string end to end.** The API normalises (`platformFormatAmount`) so
   `499.0000` never reaches a screen; the screens format for display only.
4. **The proration preview is the server's.** `/subscriptions` renders the numbers
   `change-plan` returns (credit of the unused part, full charge for the new plan, net) — the
   console never recomputes money, which is how a screen and a document stay in agreement.
5. **Settings are the invoice.** The seller identity, the VAT rate, the payment terms and the
   dunning ladder live in ستة `billing.*` keys on `/settings` (P-C1's catalogue) and are copied
   onto every document as it is created.

## Security

- No self-service signup: operators are provisioned by hand and granted Family-A
  platform roles (`platform_owner`, `platform_operations`, `platform_billing`,
  `platform_support`, `platform_auditor`) from `/roles`.
- `PlatformGuard` requires effective platform access (`pam` claim); tenant sessions —
  including tenant owners — see `Forbidden`.
- **Every `/platform/*` route requires a `console.*` code** (P-C1). The sidebar hides the
  items the session cannot open (`canConsole()`), and the API refuses them regardless —
  the sidebar is a convenience, never a control.
- `console.settings.manage` is held by `platform_owner` alone: the maintenance switch and
  the default limits affect every customer.
- `console.users.view` is likewise `platform_owner`-only **in the catalogue**: none of the
  other four roles can read the directory out of the box, and delegating it is a written,
  audited act on `/roles` (the API suite builds a read-only operator that way and proves the
  read-only operator still cannot revoke a session).
- CSP headers are configured in `next.config.mjs`.
