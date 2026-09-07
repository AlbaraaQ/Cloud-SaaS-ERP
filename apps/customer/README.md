# Customer UI / Portal — Phase 18

Next.js 15 mobile-first customer-facing app for public marketing, onboarding, document
verification, and self-service portal flows.

## Run

```bash
pnpm --filter @erp/customer dev
pnpm --filter @erp/customer build
```

Configure `NEXT_PUBLIC_API_BASE_URL`, for example `http://localhost:3000/api/v1`.

## Structure

- `app/` — marketing pages, auth pages, public invoice verification, onboarding, and
  `/portal/*` self-service sections.
- `components/` — lightweight portal shell, tables, KPI cards, badges, forms, QR/print
  friendly layouts.
- `lib/` — API path registry, i18n helpers, formatting, route/permission/flag metadata.
- `tests/` and `lib/*.spec.ts` — route coverage and formatting/privacy checks.

## Security posture

- RTL Arabic is default, with AR/EN helpers ready for a language switch.
- Public verification renders masked/minimal data only and documents strict rate-limit UX.
- Auth pages do not store tokens in localStorage; cookie/in-memory mode is documented.
- Portal routes include permission and tenant-flag metadata for permission-scoped nav.
- CSP and `nosniff` headers are configured in `next.config.mjs`.

## Implemented flows

Marketing home, pricing placeholder, contact, login/forgot/forced-reset, tenant picker,
portal dashboard, invoice list/detail with PDF/QR affordances, statement export, payment
history, notifications, profile change request, quick-sale disabled-state/form, stock
lookup, task inbox, onboarding wizard, and public verification page.
