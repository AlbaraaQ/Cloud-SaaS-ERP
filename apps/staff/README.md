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

## Coverage

All core sections are navigable: organization, catalog, accounting,
parties, inventory, sales, purchases, treasury, e-invoicing, reporting, and migration /
compat devices. The platform console moved to `apps/platform-admin`; the customer
portal lives in `apps/customer-portal`.
