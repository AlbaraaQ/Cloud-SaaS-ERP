# Marketing — public site (home, pricing, onboarding, verify, contact)

Arabic-first Next.js 15 App Router app. Served from the apex domain
(`yourdomain.com`) and deployed separately from every other surface, while
calling the same shared API (`apps/api`).

## Run

```bash
pnpm --filter @erp/marketing dev
pnpm --filter @erp/marketing build
```

Set `NEXT_PUBLIC_API_BASE_URL` to the API base URL, for example
`http://localhost:3000/api/v1`. In a split-domain deploy also set
`NEXT_PUBLIC_STAFF_URL`, `NEXT_PUBLIC_PORTAL_URL` and `NEXT_PUBLIC_PLATFORM_URL`.

## Structure

- `app/` — `/` landing, `/pricing` (live plans from `GET /billing/plans`),
  `/onboarding` (real self-service signup via `POST /signup`),
  `/verify` (public invoice verification), `/contact`, `/login` (smart router).
- `components/` — signup panel, smart login (authenticates once, then routes
  staff to the staff app and buyers to the customer portal via a `?token=`
  bridge), KPI cards.
- `lib/` — API client, formatting, `publicRoutes` registry, cross-surface link
  helper (`surfaces.ts`), ZATCA QR decoder.
- `tests/` — route/kit coverage checks.

This surface hosts no authenticated screen: the portal moved to
`apps/customer-portal` and the consoles to `apps/staff` / `apps/platform-admin`.
