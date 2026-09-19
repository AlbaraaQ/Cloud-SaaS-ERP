# Staff — tenant admin + ERP back office

Arabic-first Next.js 15 App Router back-office for the core ERP modules. Served
from the `app.*` domain and deployed separately from every other surface, while
calling the same shared API (`apps/api`).

## Run

```bash
pnpm --filter @erp/staff dev
pnpm --filter @erp/staff build
```

Set `NEXT_PUBLIC_API_BASE_URL` to the API base URL, for example
`http://localhost:3000/api/v1`. In a split-domain deploy also set
`NEXT_PUBLIC_MARKETING_URL` and `NEXT_PUBLIC_PLATFORM_URL`.

## Structure

- `app/` — routes for dashboard and sections 1-12 from `ADMIN_PANEL_MASTER_REQUIREMENTS`.
- `components/` — shared UI kit: data table, filter bar, status badge, KPI card,
  money/quantity inputs, confirmation dialog, loading/empty/error/forbidden states, and
  report runner.
- `lib/` — API client, i18n helpers, formatting, and navigation registry.
- `tests/` — route/kit coverage checks.

## Security and UX

- RTL Arabic is the default; LTR is represented by the AR/EN switch and i18n helpers.
- Navigation entries carry required permissions so callers can hide links by scope.
- Browser print CSS is included for printable sales, shift, and report artifacts.
- Column chooser state is persisted in `localStorage` and can later sync to server
  settings when exposed by the API.
- CSP headers are configured in `next.config.mjs`.
- No signup on this surface: new tenants register on the marketing site, which
  hands them back to the login screen with `?tenant=` prefilled. Sessions handed
  over from the marketing smart login arrive via a `?token=` bridge that is
  consumed and stripped from the address bar.

## Screens added by P-C5 (2026-09-17)

| Screen | Route | Permission | Endpoints |
|---|---|---|---|
| الاستخدام والحصص | `/settings/usage` | `tenant.view` (read-only; `Forbidden` otherwise) | `GET /usage` |

The screen is the tenant's own view of the same engine the platform console reads: the eight
metrics with their limit, source and state, plus a 30-day chart for the metered counters. It is
read-only on purpose — limits are the platform's to set (`/platform/tenants/:id/settings` on the
operator side, or the `limits.*` defaults), and a tenant that could raise its own ceiling would
make the gate decorative.

## Screens added by P-C6 (2026-09-17)

| Screen | Route | Permission | Endpoints |
|---|---|---|---|
| البريد — القوالب والسجلّ | `/settings/email` | `tenant.email.log.view` (read) · `tenant.email.template.manage` (edit) | `GET /email/templates` · `PUT /email/templates/:event` · `GET /email/messages` · `GET/PUT /email/settings` |

Three tabs: **قوالبي** (edit the tenant's own text over the platform's, with «أعِد نصّ المنصة»
to drop the override) · **سجلّي** (the tenant's outbound log with event/status filters and
counts) · **هويّة المُرسِل** (sender name, reply-to and sending domain, plus the provider —
read-only). The provider and the caps belong to the platform console on purpose: a tenant that
could switch the transport or raise its own ceiling would make the platform's gate decorative.
`{{variables}}` are the event's declared ones only — an unknown variable is refused at save
time, and a missing one at delivery time, so neither reaches a customer's inbox.

## Screens added by P-C7 (2026-09-17)

| Screen | Route | Permission | Endpoints |
|---|---|---|---|
| مركز الإشعارات | `/notifications` | `tenant.notification.view` | `GET /notifications` · `POST /notifications/:id/read` |

The inbox lists what was addressed to the calling membership — platform announcements
(`type=announcement`, text read from the notification `payload` in the UI language) and system
notices — newest first, with «تحديد كمقروء» per row and «قراءة الكل». The **bell in the top
bar** shows the same `meta.unread` this screen shows (no second counter), polling once a minute
and staying silent when the call fails. Marking a read **also stamps the platform's delivery
ledger** (`announcement_reads`), which is what makes the console's «القراءات» column truthful.

## Screens added by P-C8 (2026-09-17)

| Screen | Route | Permission | Endpoints |
|---|---|---|---|
| (لا شاشة جديدة) لافتة الدخول المؤقّت | كل شاشة | — | `GET /me` (حقل `impersonation`) |

When the console hands a **temporary access token** over (the token travels in the URL
*fragment*: `#support=…`, so it is never sent to a server), the workspace adopts it as a
**view-only session**: `readSession()` returns it without a refresh token — a break-glass token
is never renewed — and a red banner (`components/impersonation-banner.tsx`) sits above every
screen saying who is inside the tenant, why, and for how long. The banner is driven by
`me.impersonation` alone, so it cannot be dismissed while the token still carries `imp`; its
button leaves the view locally, and the session itself is ended from the console.

## What P-C9 (2026-09-17) changed here

Nothing. The part is a console part: it turned the operator's daily service questions into
screens (`/jobs` with retry and cancel, `/health` with six server-measured probes, `/files`
with a real quarantine, `/audit` with a `before`/`after` diff viewer) and it added the
`console.jobs.manage` code that separates *reading* the queue from *running* it. The customer
surface — and this app's screens, navigation and session handling — did not change, which is
itself a result: the quarantine the console writes is enforced by the API, so the customer's
`GET /files/:id` answers 404 without a single line changing in the workspace.

## Coverage

All core sections are navigable: organization, catalog, accounting,
parties, inventory, sales, purchases, treasury, e-invoicing, reporting, and migration /
compat devices. The platform console moved to `apps/platform-admin`; the customer
portal lives in `apps/customer-portal`.
