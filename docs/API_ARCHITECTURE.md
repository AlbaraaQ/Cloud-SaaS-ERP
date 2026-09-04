# API_ARCHITECTURE

> Level B — CANONICAL design rationale. The normative endpoint/DTO formats live in
> `API_CONTRACT.md` — both must stay consistent.

## 1. Style & Versioning

- REST over HTTPS, JSON UTF-8. Base: `/api/v1`.
- URL versioning only (`/api/v2` for breaking changes — none planned now).
- Resource naming: plural kebab-case nouns; actions as **sub-resource verbs**:
  `POST /sales-invoices/{id}/post`, `/void`, `POST /journal-entries/{id}/reverse`.
- No RPC-style controllers; workflow endpoints are exceptions listed above (frozen set).

## 2. Request Pipeline (NestJS order — frozen)

`RequestId → pino logger → helmet/CORS → rate limit → AuthGuard (JWT) → TenantGuard
(+RLS GUC) → BranchScopeGuard (X-Branch-Id) → FeatureFlagGuard → PermissionsGuard
(@RequiresPermission) → Parse/validation pipes (zod DTO) → Module service (tx) →
AuditInterceptor (mutations) → Problem+json exception filter`.

## 3. Conventions Deep-Dive

- **Errors**: RFC 9457. `{ type, title, status, code, detail, traceId, errors?[] }`.
  `code` from central registry (`@erp/contracts`), e.g. `ACCOUNTING_PERIOD_LOCKED`,
  `PARTY_CREDIT_LIMIT_EXCEEDED`, `VALIDATION_FAILED`.
- **Validation**: zod schemas in `packages/contracts` per DTO; server is source of
  truth; UI reuses same schemas.
- **Pagination**: `limit`(1..200, def 50)`+offset`; envelope `{data, meta{total,limit,
  offset}}`; cursor variant for ledger streams (`next_cursor`).
- **Filtering**: allow-listed per resource (`filter[status]=posted&filter[dateFrom]=…`);
  unknown filters → 400 `FILTER_NOT_ALLOWED`.
- **Sorting**: `sort=-date,number` allow-listed; default documented per resource.
- **Search**: `q=` trigram-backed for parties/items.
- **Bulk**: max 500 rows; per-row results `{index, ok, error?}`.
- **Idempotency**: money mutations accept `Idempotency-Key`; stored 24 h
  (`idempotency_keys`); replay returns first response.
- **ETag/Optimistic concurrency**: `version` field; PATCH requires `If-Match` or
  `version` in body → 409 `VERSION_CONFLICT`.

## 4. AuthN/AuthZ Surface

`POST /auth/register-tenant` (bootstrap; optional public flag) · `POST /auth/login` →
`{ accessToken, refreshToken(cookie httpOnly or body) }` · `POST /auth/refresh` (rotate)
· `POST /auth/logout` · `POST /auth/mfa/*` (TOTP, optional) · `GET /me` ({user,
membership, permissions[], branches[]}) · `POST /auth/change-password` ·
forgot/reset (email token). Permissions checked per endpoint via decorator;
branch scope intersected at query layer.

## 5. Resource Families (v1 routes summarized; normative list in API_CONTRACT)

`platform`: tenants(platform plane) · users · memberships · roles · permissions ·
files(pre-signed put) · notifications · settings(tenant).
`organization`: company-profile · branches · warehouses · cash-locations ·
currencies · fx-rates · price-lists · posting-profiles.
`catalog`: item-categories · units · tax-groups · items(+item-units, barcodes,
alternative-codes, components, price-history) · item-import/export CSV.
`accounting`: accounts · fiscal-years · fiscal-periods(+locks, close, reopen) ·
journal-entries(+post/reverse) · cost-centers · opening-balances(import) ·
statements(trial-balance, general-ledger, account-statement).
`parties`: parties(+contacts) · party-balances · allocations.
`inventory`: stock(levels, movements) · stock-adjustments(+approve) · stock-transfers
(+send/receive) · lots · serials.
`sales`: sales-invoices(+post/void/payments) · sales-notes · offers.
`purchases`: purchase-invoices(+post/void) · purchase-costs allocation preview.
`treasury`: vouchers(+post/void/clear-cheque) · cash-transfers · expense-types ·
shift-closes(+count).
`einvoicing`: credentials(manage) · submissions(list/retry) · health.
`reporting`: `/reports/*` (P14 catalog) · exports (CSV/XLSX/PDF async via jobs).
`migration`: runs(create/dry-run/start/cancel) · issues · mappings · reconciliation.
`compat` (P16): `/compat/*` legacy-desktop gateway (device keys, deltas, doc push).

## 6. OpenAPI & Client Contracts

- OpenAPI generated from zod DTOs (`nestjs-zod`), published at `/api/docs` (env-gated),
  exported as `packages/contracts/openapi.json` in CI; admin/customer apps codegen or
  type-import directly.
- Breaking-change detector in CI (oasdiff) against previous `openapi.json`.

## 7. Performance Rules

- N+1 forbidden: repository methods eager-load; integration tests assert query counts
  on hot endpoints (invoices list, stock levels).
- Monetary list endpoints pre-aggregate in SQL, never in JS loops.
- Reports > 60 s become async jobs with download artifact.
- DB pool: `DATABASE_POOL_MAX` per replica; worker pools separate.

## 8. Compatibility API (P16 summary — full spec in MIGRATION doc §9)

Device-authenticated endpoints mirroring desktop flows: master-data pull deltas
(`items`, `parties`, `accounts` cursors), document push (`sales-invoice`, `voucher`),
and status polling. Payloads validated into the same services as first-class API calls —
no side doors into tables.
