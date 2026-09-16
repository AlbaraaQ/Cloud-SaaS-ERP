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
`http://localhost:3000/api/v1`.

## Structure

- `app/` — overview, tenants, licenses (subscriptions), plans, activation requests,
  users, platform roles, audit, health. Pages moved from the old `apps/admin/app/platform/*`
  tree with their `/platform` prefix flattened to `/`.
- `components/` — session auth gate, platform-only login screen (no signup path),
  tab guard (`PlatformGuard`), and the shared screen kit.
- `lib/` — API client, session provider, `useQuery` helper.
- `tests/` — route/kit coverage checks.

## Security

- No self-service signup: operators are provisioned by hand and granted Family-A
  platform roles (`platform_owner`, `platform_operations`, `platform_billing`,
  `platform_support`, `platform_auditor`) from `/roles`.
- `PlatformGuard` requires effective platform access (`pam` claim); tenant sessions —
  including tenant owners — see `Forbidden`.
- CSP headers are configured in `next.config.mjs`.
