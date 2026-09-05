# API_CONTRACT

> **Level B — NORMATIVE.** Every endpoint added by any phase MUST appear here first
> (or in the same PR/turn) with DTOs in `packages/contracts`. Formats: JSON; UUID ids;
> money as decimal strings; ISO-8601 datetimes; enums as lowercase_snake text.

## 0. Envelope & Errors

```jsonc
// list   → { "data": [ ... ], "meta": { "total": 120, "limit": 50, "offset": 0 } }
// single → { "data": { ... } }
// error  → 4xx/5xx, application/problem+json:
{ "type": "about:blank", "title": "Forbidden", "status": 403,
  "code": "FORBIDDEN", "detail": "permission sales.invoice.post required",
  "traceId": "01J…" }
```

Stable error codes (seed registry, extend only): `UNAUTHENTICATED, FORBIDDEN,
TENANT_SUSPENDED, TENANT_CONTEXT_MISSING, VALIDATION_FAILED, FILTER_NOT_ALLOWED,
NOT_FOUND, VERSION_CONFLICT, IDEMPOTENCY_REPLAY, ACCOUNT_NOT_POSTABLE,
ACCOUNT_PROFILE_MISSING, JOURNAL_UNBALANCED,
ACCOUNTING_PERIOD_CLOSED, ACCOUNTING_PERIOD_LOCKED_MODULE, DOCUMENT_ALREADY_POSTED,
DOCUMENT_NOT_DRAFT, PARTY_CREDIT_LIMIT_EXCEEDED, STOCK_INSUFFICIENT, SEQUENCE_EXHAUSTED,
EINVOICE_REJECTED, MIGRATION_CONFLICT, RATE_LIMITED, INTERNAL`.

Standard query params: `limit, offset, sort, q, filter[...]`, `include=...` allow-listed.
Headers: `Authorization: Bearer`, `X-Branch-Id?`, `Idempotency-Key?`, `X-Request-Id?`.

## 1. Auth & Identity (`/api/v1`)

| Method & Path | Body → Response | Perm |
|---|---|---|
| POST `/auth/login` | `{email,password,tenantCode, mfaCode?}` → `{data:{accessToken, refreshToken, user, memberships}}` — `memberships` holds **only** the membership in the authenticated tenant (CR-003: returning the others would enumerate tenants). Wrong password, unknown e-mail and unknown tenant all return the same opaque 401. | public |
| POST `/auth/refresh` | `{refreshToken}` → rotated pair | public |
| POST `/auth/logout` | `{}` → 204 | auth |
| GET `/me` | → user+membership+`permissions[]`+branch scope | auth |
| POST `/auth/change-password` | `{current,new}` → 204 | auth |
| GET `/permissions` | → registry list | auth |

## 2. Tenancy & Access

| Path | Notes | Perm |
|---|---|---|
| GET/PATCH `/tenant` | read/update own tenant; `GET` needs `platform.tenant.view`, `PATCH` needs `platform.tenant.manage`; bulk `settings` are validated key-by-key | `platform.tenant.view` / `platform.tenant.manage` |
| GET/POST `/memberships`, GET/PATCH/DELETE `/memberships/{id}` | invite users, branch scope, status. `GET /{id}` added by CR-002 (required by the isolation harness); a foreign id is a 404, not a 403. | `platform.membership.manage` |
| GET/POST `/roles`, GET/PUT `/roles/{id}`, POST `/roles/{id}/permissions` | RBAC mgmt. `GET /{id}` added by CR-002. System role names are immutable (422). | `platform.role.manage` |
| GET `/audit-log` | filter `entity/entityId/action/actorUserId/from/to`; newest first; read-only (UPDATE/DELETE revoked from the API role) | `platform.audit.view` |
| POST `/files/presign` `{name,mime,sizeBytes,entity?,entityId?}` → `{fileId, uploadUrl, objectKey, requiredHeaders, expiresAt}` | `platform.file.upload` |
| GET `/files`, GET `/files/{id}`, POST `/files/{id}/finalize`, GET `/files/{id}/download` | CR-005. `finalize` flips `pending→ready` and validates the attachment target; `download` mints a short-lived app-signed URL | `platform.file.upload` |
| GET `/files/{id}/content?tenant&expires&signature` | **public by design** — a browser download cannot send a bearer token; the HMAC signature is the capability and carries the tenant. 302 to object storage, 401 on a bad/expired signature | none |
| GET `/notifications`, GET `/notifications/{id}`, POST `/notifications/{id}/read` | membership inbox (never user-wide); `meta.unread` on the list; mark-read is idempotent | `platform.notification.view` |
| POST `/notifications` `{membershipId?,type,payload?}` | CR-005; unknown membership in this tenant → 422 | `platform.notification.manage` |
| GET `/jobs/outbox`, GET `/jobs/health` | CR-005; read-only view of the transactional outbox and the queue driver | `platform.job.view` |
| PUT `/settings/{key}` / GET `/settings` | typed tenant settings; an unknown key is **400 `VALIDATION_FAILED`** (CR-004), a bad value is 400 | `platform.settings.manage` |

## 3. Organization

`GET/POST/PATCH/DELETE /branches` · `/warehouses` · `/cash-locations` · `/price-lists`
(+ `/{id}/items`) · `GET/POST/PATCH /currencies` (keyed by ISO code; deactivated, never
deleted) · `GET/POST/PATCH/DELETE /fx-rates` · `GET/POST/DELETE
/branch-posting-profiles` (POST upserts on `(branchId, docType)`) ·
`/company-profile` (GET/PUT, 1:1 per tenant).
Perms: `organization.{entity}.manage|view`.

`DELETE` is a **soft delete** on master data and a hard delete on `fx_rates`,
`price_list_items` and `branch_posting_profiles` (CR-008). Activate/default toggles are
PATCH fields (`isActive`, `isDefault`), not routes; every PATCH/PUT accepts an optional
`version` and answers `409 VERSION_CONFLICT` when it is stale.

Read-only resolution surfaces (CR-008):

| Path | Answer |
|---|---|
| `GET /cash-locations/{id}/balances` | one row per currency; `0` until PHASE_12 writes them |
| `GET /fx-rates/resolve?from&to&date` | `{rate, source: identity\|direct\|inverse\|triangulated, effectiveFrom, via}` |
| `GET /branch-posting-profiles/resolve?branchId&docType` | the winning mapping + which rung matched; `422 ACCOUNT_PROFILE_MISSING` when nothing does |

Lists honour the membership's `branch_scope`; a row outside it is `404`, never `403`.
Bank IBANs are masked in list responses and returned in full only on the detail read.


## 4. Catalog

`GET/POST/PATCH/DELETE(soft) /items` · `/items/{id}/units` · `/items/{id}/barcodes`
· `/items/{id}/components` · `/item-categories` · `/units-of-measure` · `/tax-groups`
· POST `/items/import` (CSV async) · GET `/items/{id}/price-history`.
Perms `catalog.{entity}.manage|view`. Item DTO key fields: `sku,name_ar,name_en,
category_id, base_unit_id, kind, sale_price, tax_group_id, track_lot, track_serial`.

## 5. Accounting

| Path | Notes | Perm |
|---|---|---|
| `/accounts` CRUD + tree `?flat=false` | parent/code rules | `accounting.account.*` |
| `/fiscal-years`, `/fiscal-periods`; POST `/fiscal-periods/{id}/close` `/reopen` `{reason}` | close checklist | `accounting.period.*` |
| `/journal-entries` GET/POST(draft); POST `/{id}/post`, `/{id}/reverse {date,reason}` | posted immutable | `accounting.journal.create/post/reverse` |
| `/cost-centers` CRUD | | `accounting.costcenter.*` |
| `/opening-balances` POST bulk draft + POST `/opening-balances/post` | one batch/year | `accounting.opening.manage` |
| GET `/statements/trial-balance` (date range, tree), `/statements/general-ledger?account_id&from&to`, `/statements/account-statement` | prev-balance row | `accounting.reports.view` |

Journal DTO: `{ date, branch_id, description, lines:[{account_id, debit?, credit?,
cost_center_id?, party_id?, currency_amount?, currency_code?, description?}] }`.

## 6. Parties & AR/AP

`/parties` CRUD, `filter[kind]=customer|supplier` · `/parties/{id}/contacts` ·
GET `/parties/{id}/balance` → `{receivable,payable,open_invoices[]}` ·
GET `/parties/{id}/statement` · POST `/allocations` `{voucher_id, invoice_id, amount}`.
Perms `parties.{manage,view,allocate}`.

## 7. Inventory

GET `/inventory/levels?warehouse_id&item_id` · GET `/inventory/movements?…` ·
`/stock-adjustments` CRUD + POST `/{id}/approve` (posts ledger+journal) ·
`/stock-transfers` CRUD + `/{id}/send` `/{id}/receive {lines:[{received_qty}]}` ·
GET `/item-lots`, `/item-serials`. Perms `inventory.{adjust,transfer,view}`.

## 8. Sales & Purchases

Sales: `GET/POST /sales-invoices` (draft) · `GET /sales-invoices/{id}` · PATCH draft only
· POST `/{id}/post` (idempotent; posts journal+stock; returns links) · POST `/{id}/void {reason}`
· POST `/{id}/payments {method, cash_location_id?, amount}` · GET `/sales-invoices/{id}/print`
(PDF later=artifact) · `/sales-adjustment-notes` · `/offers`.
Create DTO essentials: `{ branch_id, warehouse_id, kind, party_id?|cash_customer?,
price_includes_vat, currency_code?, lines:[{item_id, unit_id, qty, unit_price,
discount_amount?, tax_group_id?|tax_rate?, description?}], invoice_discount?,
pay_method?, payments?[...], reference_invoice_id? (returns) }`.
Purchases mirror: `/purchase-invoices`, `/{id}/post` (computes landed cost),
`/{id}/payments`, `/purchase-invoices/{id}/costs`. Perms `sales.invoice.*`,
`purchase.invoice.*` (create/post/void/pay/view).

## 9. Treasury

`/vouchers?filter[kind]=receipt` CRUD(draft) + `post/void` + `POST /vouchers/{id}/cheque`
transitions (`clear|bounce`) + allocations endpoint §6 · `/cash-transfers` + `receive` ·
`/expense-types` · `/shift-closes` open/current/close `{counts:[{denomination,count}]}` ·
GET `/cash-locations/{id}/balance`. Perms `treasury.{voucher,transfer,shift}.manage/view`.

## 10. E-Invoicing

`/einvoice/credentials` PUT/GET (masked) · GET `/einvoice/submissions?filter[status]`
· POST `/einvoice/submissions/{id}/retry` · POST `/sales-invoices/{id}/einvoice/submit`
(queued) · GET `/einvoice/health`. Perms `einvoice.{manage,submit,view}`.

## 11. Reporting (P14)

`GET /reports/{reportKey}` with documented param sets per key:
`sales-by-day, sales-by-category, sales-by-item, sales-by-payment, sales-by-ordertype,
inventory-valuation, item-movement, expiry-report, stock-limits, ar-aging, ap-aging,
vat-return, trial-balance, general-ledger, profit-loss, balance-sheet, cashier-shift,
party-statement, serial-tracking, batch-tracking`. Async: POST `/reports/{key}/export
{format:csv|xlsx|pdf}` → job → file download. Perm `reporting.{key}.view`.

## 12. Migration & Compat

`/migration/runs` POST `{mode:dry_run|import, source:{…}}` (starts job), GET status,
GET `/migration/runs/{id}/issues`, GET `/migration/runs/{id}/reconciliation` ·
`/compat/v1/*` (P16): `POST /compat/auth/device`, `GET /compat/master/items?since=`,
`GET /compat/master/parties?since=`, `POST /compat/docs/sales-invoice`,
`POST /compat/docs/voucher`, `GET /compat/sync/cursor`. Device-key auth, per-tenant.

## 13. Admin-plane (platform owner)

Separate guard `is_platform_admin`: `/platform/tenants` CRUD + `suspend/activate`,
`/platform/users`, `/platform/stats`, `/platform/migrations`. Never mixed with tenant routes.
