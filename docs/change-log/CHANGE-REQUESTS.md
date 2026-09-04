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

## 2026-09-04 (Phase 04)

### CR-004 — unknown tenant-setting key on write is 400, not 404 — APPROVED (applied)

| | |
|---|---|
| Raised by | Phase 04 implementation |
| Affects | `API_CONTRACT.md §2`, `apps/api/src/modules/platform/tenancy/settings.service.ts`, `apps/api/test/settings.spec.ts` |
| Type | Status-code change on an existing endpoint |

`PUT /settings/{key}` answered `404 NOT_FOUND` for a key outside the typed registry
(PHASE_02 behaviour, asserted by `settings.spec.ts`). `PHASE_04_PROMPT §5.8` requires
"unknown key → 400", and it is the correct code: the key is a *value inside the request*,
validated against a registry that ships with the code, not a resource that may or may not
exist. The 404 also made a typo indistinguishable from "this route does not exist",
which is exactly the confusion a client cannot resolve on its own.

`PUT /settings/{key}` now returns `400 VALIDATION_FAILED` with `errors[0].field = "key"`,
matching the bulk path (`PATCH /tenant`), which already validated keys this way. The
integration test was updated in the same commit; no other endpoint changed.

### CR-005 — additional platform-service endpoints and permissions — APPROVED (applied)

| | |
|---|---|
| Raised by | Phase 04 implementation |
| Affects | `API_CONTRACT.md §2`, `SECURITY_ARCHITECTURE.md §5`, `packages/contracts/src/permissions.ts` |
| Type | Additive endpoints + additive permission codes |

`API_CONTRACT §2` lists only `POST /files/presign`, `GET /notifications` and
`POST /notifications/{id}/read` for the platform services. The Phase-04 deliverables in
`PHASE_04_PROMPT §4–§5` cannot be reached through that surface: a presigned upload has to
be *finalized*, a file has to be *downloadable*, the isolation harness needs a read-by-id
and a list for every resource it proves, and "worker health logging" needs somewhere to
be observed. Added, all read-only or lifecycle-completing:

| Path | Perm |
|---|---|
| `GET /files`, `GET /files/{id}`, `POST /files/{id}/finalize`, `GET /files/{id}/download` | `platform.file.upload` |
| `GET /files/{id}/content` | none — app-signed URL (see below) |
| `GET /notifications/{id}`, `POST /notifications` | `platform.notification.view` / `platform.notification.manage` |
| `GET /jobs/outbox`, `GET /jobs/health` | `platform.job.view` |

Three permission codes were added to the registry and to the `platform` row of
`SECURITY_ARCHITECTURE §5`: `platform.notification.view`, `platform.notification.manage`,
`platform.job.view`. `platform.audit.view` and `platform.file.upload` already existed.

`GET /files/{id}/content` is the one unauthenticated route. A browser following a download
link cannot attach a bearer token, so the capability is an HMAC signature over
`(fileId, tenantId, expiry)` minted by `GET /files/{id}/download` and valid for
`FILES_DOWNLOAD_URL_TTL_SECONDS` (default 300 s). The tenant is read from the signed
payload, never from the request, so the route cannot be pointed at another tenant's file;
a tampered, expired or cross-tenant signature is a 401. The alternative — proxying bytes
through the API — was rejected because it would put every upload and download on the
request path of the application process (TARGET_ARCHITECTURE §8).

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
