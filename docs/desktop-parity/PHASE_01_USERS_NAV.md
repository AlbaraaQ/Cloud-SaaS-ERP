# Phase 01 — Users/permissions UI + navigation dedup (done 2026-09-10)

## 1A. Staff users & roles — from read-only to real CRUD

Desktop reference: `Form_WPF/frmAddUsers*`, `frmUsersPermissions*`, `frmOperPermission*`,
`Class/User.cs`, `Class/FormPermission.cs` (per-form Editable/Addable/Delete/Search/Print).

Cloud API (already exists, verified): `memberships.controller.ts`
(`GET/GET :id/POST/PATCH :id/DELETE :id/POST :id/scopes`), `roles.controller.ts`
(`GET/GET :id/POST/PUT :id/POST :id/permissions`), `identity.controller.ts`
(`GET me`, `GET permissions`).

Work:
- `apps/staff/app/settings/users/page.tsx`: list + invite (POST), edit status/roles
  (PATCH), remove (DELETE), per-membership scopes editor (`POST :id/scopes`).
- `apps/staff/app/settings/roles/page.tsx`: list + create (POST), rename (PUT),
  permission matrix editor (`POST :id/permissions`), using the registry from
  `GET /permissions` grouped by namespace.
- Gating follows the desktop's 5 verbs: view/create/edit/delete/print map to the
  cloud permission codes on each button; forbidden → `<Forbidden/>`, never a mock.

Accept: owner manages users + roles end-to-end on the staff surface; no `*` grants;
tenant_owner touches only its own tenant (existing API guards, unchanged).

## 1B. Navigation dedup — 18 duplicate hrefs → 0

Rule applied per group (see survey in session notes):
- **True duplicates** (identical label + href, e.g. `purchase-credit-note`/`pn-credit`,
  `vouchers-report`/`purchase-vouchers`/`sales-vouchers`): merged into one entry.
- **Dead links** (`status: 'ready'` with no page: `purchases/notes/credit`,
  `reports/invoice-profit`, `sales/notes/debit`): merged + demoted to `status: 'api'`
  with the endpoint named — the screen says so instead of 404ing.
- **Wrong hrefs** (distinct function sharing a list page: `opening-stock`,
  `item-card`, marina/project cards, contractor card): given their own href
  (existing subpage or `?view=` anchor) or demoted to `'api'` when the screen is
  genuinely a later phase's work.
- **Cross-module shortcuts** (same card reachable from sales + marina + projects):
  kept only where the desktop menu also repeats the entry, with module-qualified
  labels; otherwise removed.

Accept: audit script reports 159 screens, 0 duplicate hrefs (was 183 / 18), and every `'ready'`
href resolves to a real page (checked in CI by `apps/staff/tests/navigation.spec.ts`).
Non-ready entries use the `/s/` scaffold namespace and render as disabled in the
sidebar instead of linking to a 404.
