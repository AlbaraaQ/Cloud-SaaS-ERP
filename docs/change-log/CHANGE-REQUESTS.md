# Change Requests

## 2026-08-23

- No architecture change requests were necessary for Phase 01. The project was created using the already-approved stack and repository layout from the canonical documents.

## 2026-09-04 (Phase 03)

### CR-001 — lockout counters on `users` — APPROVED (applied)

| | |
|---|---|
| Raised by | Phase 03 implementation |
| Affects | `DATABASE_DESIGN.md §1` (`users`), `migrations/0000_platform_identity.sql` |
| Type | Additive schema |

`SECURITY_ARCHITECTURE §2` requires "lockout counters on repeated failures", but the
frozen `users` definition in `DATABASE_DESIGN §1` has no column to hold them. Two
nullable/defaulted columns were added:

```sql
failed_login_attempts integer NOT NULL DEFAULT 0,
locked_until          timestamptz
```

They are reset on a successful login and set by the auth service
(`AUTH_LOGIN_MAX_FAILURES` = 5, 15-minute lock). No existing column, index or relation
changed, and no data migration is required. Alternatives considered and rejected:
a separate `login_attempts` table (an extra join on the hottest path, and it would
itself need a retention job), and keeping the counter in Redis (Phase 23 scope, and it
must not be the only record of a lockout).

### CR-002 — single-resource reads for memberships and roles — APPROVED (applied)

| | |
|---|---|
| Raised by | Phase 03 implementation |
| Affects | `API_CONTRACT.md §2` |
| Type | Additive endpoints |

`API_CONTRACT §2` lists `PATCH/DELETE /memberships/{id}` and `PUT /roles/{id}` but no
`GET` for either. `TESTING_STRATEGY §6` makes "read by id returns 404 for tenant B" the
first mandatory isolation proof, which cannot be exercised without a read-by-id route.
Added, with the same permission as the rest of the resource:

| Path | Perm |
|---|---|
| `GET /api/v1/memberships/{id}` | `platform.membership.manage` |
| `GET /api/v1/roles/{id}` | `platform.role.manage` |

Both are inside the caller's tenant transaction, so a foreign id is indistinguishable
from a missing one (404), as required by `MULTI_TENANCY §7.1`.

### CR-003 — `POST /auth/login` returns only the authenticated tenant's membership — APPROVED (applied)

| | |
|---|---|
| Raised by | Phase 03 implementation |
| Affects | `API_CONTRACT.md §1` |
| Type | Narrowing of an existing response field |

`API_CONTRACT §1` documents the login response as
`{accessToken, refreshToken, user, memberships}` with no qualifier on `memberships`. The
request is tenant-scoped (`tenantCode` is required), so returning every membership the
user holds would disclose which other tenants an e-mail address belongs to — an
enumeration channel that `SECURITY_ARCHITECTURE §2` closes everywhere else (one opaque 401
for wrong password, unknown e-mail *and* unknown tenant).

`memberships[]` therefore contains exactly one element: the membership in the tenant that
was authenticated. The array shape is kept so the wire contract does not change again if a
tenant-picker flow is ever added; such a flow must be a separate, authenticated endpoint.

## Decisions recorded without a change request

These do not contradict any frozen document, but they are load-bearing and later phases
must not silently undo them.

- **Workspace packages emit `dist/`** and are consumed as compiled JavaScript at runtime;
  tests resolve `@erp/*` to TypeScript source through vitest aliases. The API build fails
  with `TS6059` if the packages are consumed as source under `rootDir: src`, and NestJS
  cannot resolve constructor parameters compiled by esbuild/`tsx` because
  `design:paramtypes` is not emitted.
- **Migrations are hand-written, reviewed SQL** with a SHA-256 ledger
  (`erp_migrations`); `drizzle-kit generate` writes to `migrations/generated/` for review
  only and is never applied automatically (`AI_DEVELOPMENT_PROTOCOL §5`: review SQL before
  applying).
- **`erp_api` is pinned `NOBYPASSRLS` on every migration run**, so a later migration
  cannot quietly grant the application role a bypass.
- **Login failures are indistinguishable** (wrong password / unknown e-mail / unknown
  tenant → the same 401 `UNAUTHENTICATED` with the same `detail`). An `mfaCode` on a
  request is rejected with 400 rather than ignored, because MFA logic is out of Phase 03
  scope while the columns already exist.
- **`GET /settings` returns the registry alongside the values** so clients can render the
  typed editor without a second source of truth.
