# Status Ledger

| Phase | Date | State | Notes |
|---|---|---|---|
| PHASE_01 | 2026-08-23 | COMPLETE | Repository and engineering standards bootstrap. Re-audited on 2026-09-04: the phase was **present but over-claimed** — `.prettierrc.json`, `.prettierignore`, `.lintstagedrc.json`, `.gitleaksignore`, `.github/workflows/ci.yml` and every `.env.example` were missing from the commit and have since been created. |
| PHASE_02 | 2026-09-04 | COMPLETE | Backend platform core (NestJS bootstrap, request pipeline, contracts/config/database packages, health probes). Originally reported `IN_PROGRESS` on 2026-09-01; the phase **failed its own acceptance bar** (`npm run verify` never reached lint/unit/integration/build) and was completed during the Phase 03 pass. See `PHASE_02_IMPLEMENTATION_REPORT.md`. |
| PHASE_03 | 2026-09-04 | COMPLETE | Tenancy, Identity & Access: 9-table platform/tenancy schema with reversible idempotent migration, RLS on all tenant-scoped tables, `api`/`migrator` database roles, auth (login/refresh/logout/change-password), RBAC, typed tenant settings, guard pipeline, isolation harness. `pnpm run verify` exits 0. See `PHASE_03_IMPLEMENTATION_REPORT.md`. |
| PHASE_04 | 2026-09-04 | COMPLETE | Platform Services: audit trail (interceptor + service API, append-only at the privilege level), files (presign/finalize/app-signed download, MIME+size allow-lists, orphan GC), notifications (membership inbox + settings-updated demo subscription), BullMQ queues with a transactional outbox and a `WORKER=1` bootstrap, `Sequences.next` (64-parallel, no duplicates), DB-backed idempotency keys replacing the Phase-02 in-memory map. 6 new tables, all under FORCE RLS. `pnpm run verify` exits 0 (148 tests). See `PHASE_04_IMPLEMENTATION_REPORT.md`. |
| PHASE_05 | 2026-09-05 | COMPLETE | Organization Structure: 10 tables (`company_profiles, branches, warehouses, cash_locations, cash_location_balances, currencies, fx_rates, price_lists, price_list_items, branch_posting_profiles`) under `ENABLE`+`FORCE` RLS, applied by `0002_organization.sql` with a reversible down file; CRUD for every `API_CONTRACT §3` resource with soft delete, single-default invariants held by partial unique indexes + an advisory lock, branch-scope-aware reads, IBAN masking, explicit before/after audit on cash locations, posting profiles and the company profile; `resolveFx` (identity/direct/inverse/triangulated, decimal.js) and `resolvePostProfile` (4-rung fallback, `ACCOUNT_PROFILE_MISSING`); idempotent `provisionOrgDefaults`. Deferred FK `document_sequences.branch_id → branches.id` added. `pnpm run verify` exits 0 (268 tests). See `PHASE_05_IMPLEMENTATION_REPORT.md`. |
| PHASE_06 | 2026-09-05 | COMPLETE | Catalog foundation: 10 tenant-scoped catalog tables with `ENABLE`+`FORCE` RLS, reversible migration `0003_catalog.sql`, Drizzle schema exports, tenant-scoped catalog service and API routes for item/category listing and item creation, composite item-kind validation, and Arabic-name search. Catalog contract tests and package/API validation passed. See `PHASE_06_IMPLEMENTATION_REPORT.md`. |
| PHASE_07 | 2026-09-07 | COMPLETE | Accounting foundation completed: sequence-backed journal numbering, module lock/unlock enforcement for fiscal periods, party subledger foreign keys after PHASE_08, Decimal-safe balancing, journal posting/reversal, period close/reopen, trial balance, general ledger, and invariant/API verification. `pnpm run verify` exits 0. See `PHASE_07_IMPLEMENTATION_REPORT.md`. |
| PHASE_08 | 2026-09-06 | COMPLETE | Party and subledger foundation: tenant-scoped parties, contacts, payment allocations, credit-limit checks, balance/statement routes, reversible migration `0005_parties.sql`, RLS, Decimal-safe financial comparisons, and API integration proofs for tenant isolation, contact lifecycle, allocation limits, and permission enforcement. `pnpm run verify` passes: 36 API test files and 223 tests. See `PHASE_08_IMPLEMENTATION_REPORT.md`. |
| PHASE_09 | 2026-09-07 | COMPLETE | Inventory ledger completed: balance rows are locked with `FOR UPDATE` for live database concurrency safety, transfer lifecycle persists draft/send/partial receipt/receipt/cancel, approved adjustment deltas require journal references, lot and serial lifecycle endpoints are available, moving-average valuation/recompute APIs and parity helpers are verified. `pnpm run verify` exits 0. See `PHASE_09_IMPLEMENTATION_REPORT.md`. |
| PHASE_10 | 2026-09-07 | COMPLETE | Sales completed: deterministic invoice totals, draft/update/post/void/payment flows, sequence-backed sales numbering, atomic posting transaction that combines inventory movements, accounting journal entries, and invoice status update, reference-linked returns with remaining-quantity enforcement, adjustment-note posting, offer evaluation, print data, tenant-scoped routes, and verification through `pnpm run verify`. See `PHASE_10_IMPLEMENTATION_REPORT.md`. |
| PHASE_11 | 2026-09-07 | COMPLETE | Purchases completed: tenant-scoped purchase invoices, lines, and additional-cost tables with RLS; supplier-required validation; draft/update/post/void/payment hooks; landed-cost preview and costs management; qty/value landed-cost allocation with deterministic largest-line remainder; stock-in/return posting to inventory; accounting journal integration inside the posting transaction; purchase permissions and OpenAPI export. `pnpm run verify` exits 0. See `PHASE_11_IMPLEMENTATION_REPORT.md`. |
| PHASE_12 | 2026-09-07 | COMPLETE | Treasury completed: unified receipt/payment vouchers, cheque state transitions, cash transfers, expense types, cashier shift close/count lines, cash-location balance writers, one-open-shift invariant, tenant-scoped RLS migration `0011_treasury.sql`, module README, updated permissions and OpenAPI export. `pnpm run verify` exits 0. See `PHASE_12_IMPLEMENTATION_REPORT.md`. |
| PHASE_13 | 2026-09-07 | COMPLETE | E-invoicing completed: credential vault with encrypted secrets/masked reads, tenant-scoped credentials/submissions/hash-chain tables, ZATCA UBL/hash/QR fixture builders, submission ledger, invoice ZATCA status sync, ETA explicit stub, endpoints, README, permissions, tests, and OpenAPI export. `pnpm run verify` exits 0. See `PHASE_13_IMPLEMENTATION_REPORT.md`. |
| PHASE_14 | 2026-09-07 | COMPLETE | Reporting completed: registry for all v1 report keys, tenant-bound report readers, export token endpoint, invoice/shift print HTML shells, README recipe, registry tests, and OpenAPI export. `pnpm run verify` exits 0. See `PHASE_14_IMPLEMENTATION_REPORT.md`. |
| PHASE_15 | 2026-09-07 | COMPLETE | Migration engine completed: apps/migrator CLI/library, W1-W10 registry plus W13/W14 artifacts, anonymized fixture source, analyze/dry_run/import/reconcile/rollback modes, idempotent legacy-id loader, R1-R7 reconciliation, rollback order, engine persistence tables with RLS, API run-management endpoints, runbook, and tests. `pnpm run verify` exits 0. See `PHASE_15_IMPLEMENTATION_REPORT.md`. |
| PHASE_16 | 2026-09-07 | COMPLETE | Legacy desktop compat gateway completed: compat device table/RLS, hashed API keys, admin device management, device auth, scoped compat tokens, master pulls with cursor watermarks/tombstones, sales/voucher push mappers with GlobalID idempotency, cursor/status endpoints, wire doc, permissions, tests, and OpenAPI export. `pnpm run verify` exits 0. See `PHASE_16_IMPLEMENTATION_REPORT.md`. |
| PHASE_17 | 2026-09-07 | COMPLETE | Admin panel completed: Next.js App Router shell, Arabic RTL default, module routes for master sections 1-12, permission-aware navigation, UI kits, report runner, migration/compat console pages, print CSS, CSP headers, README, and tests. `pnpm --filter @erp/admin build` and `pnpm run verify` exit 0. See `PHASE_17_IMPLEMENTATION_REPORT.md`. |
| PHASE_18 | 2026-09-07 | COMPLETE | Customer UI completed: Next.js mobile-first RTL portal, marketing/contact/pricing pages, auth/forced-reset/tenant-picker pages, public invoice verification, portal dashboard/invoices/statement/payments/profile requests/notifications, quick-sale/stock/tasks screens, onboarding wizard, route permission/flag metadata, README, tests, and build verification. `pnpm --filter @erp/customer build` and `pnpm run verify` exit 0. See `PHASE_18_IMPLEMENTATION_REPORT.md`. |
| PHASE_19 | 2026-09-07 | COMPLETE | Restaurant POS pack completed: tenant-flag gated POS tables/categories/order-events schema with RLS, sales invoice POS fields and line modifiers, POS permissions, `/pos/*` APIs for floor/table/order/void/merge/split/send/close, daily order sequences, sales-by-ordertype reporting hook, admin `/pos` screen, docs, tests, and OpenAPI export. `pnpm run verify` exits 0. See `PHASE_19_IMPLEMENTATION_REPORT.md`. |
| PHASE_20 | 2026-09-07 | COMPLETE | HRM & Payroll pack completed: tenant-flag gated departments/jobs/employees/attendance/adjustments/payroll schema with RLS, HRM permissions, `/hrm/*` APIs, idempotent attendance CSV import, payroll calculator/preview/run/post/pay/reverse lifecycle, salary voucher integration, payslip HTML, admin `/hrm` screen, docs, tests, and OpenAPI export. `pnpm run verify` exits 0. See `PHASE_20_IMPLEMENTATION_REPORT.md`. |
| PHASE_21 | 2026-09-07 | COMPLETE | Installments and contracting/projects packs completed: tenant-flag gated installment contract/schedule and project/BOQ/stage/progress-bill/requirement schema with FORCE RLS, sequencing for installment contracts/progress bills, `/installments/*` and `/projects/*` APIs, oldest-due collection allocation, progress bill retention/net-due calculations, sales-invoice posting/release hooks, permissions, admin routes, docs, tests and OpenAPI export. `pnpm run verify` exits 0. See `PHASE_21_IMPLEMENTATION_REPORT.md`. |
| PHASE_22 | 2026-09-07 | COMPLETE | Niche verticals and Salla integration completed: optics prescriptions, tailoring measurements, marina vessels/bookings/rental invoices, vehicle fitment and Salla OAuth/sync/webhook tables under FORCE RLS; feature-flagged `/optics`, `/tailoring`, `/marina`, `/fitment`, `/integrations/salla` APIs, encrypted Salla tokens, HMAC webhook verification, admin pages, docs, tests and OpenAPI export. `pnpm run verify` exits 0. See `PHASE_22_IMPLEMENTATION_REPORT.md`. |
| PHASE_23 | 2026-09-07 | COMPLETE (READINESS PACK) | Hardening and go-live readiness artifacts completed: `/metrics`, deeper readiness checks, retention-plan service/tests, dependency/secret security sweep with ADR-021 waivers, operations runbooks, backup/restore drill template, perf report, endpoint inventory, UAT pack, program acceptance matrix and `RELEASE_NOTES.md` v1.0.0. Repository verification exits 0, but staging, real API DB/E2E evidence, backup/PITR drill, performance numbers and UAT signatures remain environment-owner gates; this is not production sign-off. See `PHASE_23_IMPLEMENTATION_REPORT.md` and `POST_PHASE_23_GAPS_AND_NOTES.md`. |

## Admin web UI — desktop menu coverage (2026-09-08)

`apps/admin/lib/navigation.ts` is the single source of truth for the screen tree that
mirrors the customer's desktop product. `tests/navigation.spec.ts` fails the build if a
menu item claims `ready` without a page file behind it, so these counts are checked, not
asserted by hand.

| State | Count | Meaning |
|---|---|---|
| `ready` | 178 | A real screen reading and writing the live API. |
| `api` | 0 | The endpoint exists; the screen is still the scaffold. |
| `planned` | 12 | Neither screen nor endpoint yet; routed under `/s/…`. |
| **total** | **190** | |

Round 1 wired: expense cards (`/accounting/expenses`), sales credit/debit notes with
posting (`/sales/notes/[kind]`), ZATCA credentials (`/settings/zatca`) and submissions with
retry (`/settings/sync/zatca`), migration runs with issues (`/migration/runs`), offers with
a live evaluator (`/settings/offers`), price lists and their rows (`/settings/price-lists`),
and a Code 128-B barcode label sheet (`/inventory/barcodes`).

Round 2 wired the documents that were missing an entire side of the ledger:

* **Supplier credit/debit notes** — new table `purchase_adjustment_notes` (migration
  `0021`), `POST /purchase-invoices/:id/adjustment-notes`, `GET /purchases/adjustment-notes`
  and `POST /purchases/adjustment-notes/:id/post`, screen `/purchases/notes/[kind]`, report
  key `purchase-notes`. Posting now allocates the number from the document sequence on
  **both** sides (`SCN-`/`SDN-`, `PCN-`/`PDN-`); the sales side previously minted
  `AN-<epoch>-<id>`, which is not an auditable series.
* **Quotations** (`عرض سعر`) — migration `0022` adds `valid_until` and
  `converted_invoice_id` to `sales_invoices`; a quotation is numbered `QT-…` on creation,
  is refused by `POST /sales/invoices/:id/post`, and converts once into a **draft** sales
  invoice that carries the same lines (`/sales/quotations`).
* **Customer payment methods** (`طريقة دفع عميل`) — migration `0023` adds
  `payment_methods` plus `parties.payment_method_id`; the method carries the credit period
  and the cash location, one default per tenant enforced by a partial unique index
  (`/accounting/payment-methods`, also serving the settings menu entry).

The reporting catalog is at **63 keys**; the permission registry at **120** codes.
`apps/api/src/permission-codes.spec.ts` fails the build when a controller asks for a
permission the registry does not define — three such codes existed
(`sales.adjustment.create`, `sales.offer.manage`, and the new
`purchase.adjustment.create`), each of which had made its route answer 403 to every role
including the owner.

Round 3 wired the two warehouse documents that bracket a transfer (migration `0024`):

* **طلب بضاعة** — `goods_requests` + lines. A requisition is numbered `GR-…` on creation
  and moves draft → submitted → approved → fulfilled; approval may **cut the quantities
  down** (a store holding 30 of the 50 asked for approves 30), and fulfilment hands the
  *approved* quantities to a **draft** `stock_transfer`, which remains the only document
  that touches `inventory_transactions`. Approving is a separate permission from raising
  the request, because the branch asking for stock should not be the one releasing it.
  Rejection requires a reason (`/inventory/requests`).
* **توصيل مخزني** — `stock_deliveries` + lines, always against a **posted** sales invoice.
  It deliberately writes no inventory line: posting the invoice is what relieves the
  warehouse in this system, so a second stock issue would double-count every delivered
  unit and drag the average cost down. What it adds is the physical half of the sale —
  who received the goods, on what date, and how much the customer is still owed.
  `GET /inventory/deliveries/outstanding` drives the screen: you pick an invoice that
  still owes goods and the remaining quantities are prefilled. A draft delivery already
  reserves its quantity, and cancelling releases it (`/inventory/deliveries`).

While wiring fulfilment, transfer numbering moved server-side: `POST
/inventory/transfers/draft` now allocates `TR-000001` from the document sequence when the
caller omits a number. The admin screen used to mint `TR-<timestamp>` in the browser,
which is neither gap-free nor collision-proof.

Round 4 wired the marina operations and the project follow-up board (migration `0025`):

* **تحضير المراكب** — `marina_preparations`, one row per booking, holding the
  pre-departure checklist and the return. Life jackets must cover every companion on the
  booking (422 `MARINA_JACKETS_INSUFFICIENT`) because that is the one check a harbour is
  actually inspected on, and a booking cannot be prepared twice.
* **خطة الدور** — the rota already had a writer and no reader, which made the screen
  impossible: you could file a plan and never see it again. `GET /marina/operation-plans`
  now returns plans with their lines, filterable by date.
* **ربط الفواتير** — `GET /marina/bookings/uninvoiced` lists bookings that were never
  invoiced and `POST /marina/rental-invoices/link` issues the rental invoices in bulk,
  reporting per-booking failures instead of aborting the batch on the first one.
* **إغلاق اليومية** — `marina_day_closings` freezes a harbour day per branch. Afterwards
  the service refuses new bookings and new rental invoices dated into that day
  (409 `MARINA_DAY_CLOSED`), which is the entire point of the document: yesterday's cash
  and vessel movements can no longer change under the supervisor. Closing over bookings
  that were never invoiced hides revenue, so it takes an explicit `force`.
* **متابعة المشاريع** — a read-only board over the existing project endpoints: completion
  against the contract value, retention still held, stage accreditation, and per-BOQ-term
  progress. No new tables; the data was already there with nowhere to show it.

Still `planned` — 12 screens, each needing real domain work rather than another table
view: production orders and contracting returns; contractor contracts, contractor
payments and project offers; the file-level operations (backup, restore, data rotation,
new company file, invoice maintenance); the preparation-device settings; and the report
designer.

## Billing and live-data integration notes

- Neon migrations through `0019_billing_subscriptions` are applied to the connected database.
- Customer pricing, subscription status, manual activation requests, Stripe Checkout, and webhook handling use the live API paths; no UI fallback data is used for these flows.
- Admin billing review is available at `/billing`, and the platform page reads the live tenant context.
- Workspace tests pass after adding billing navigation coverage. Production acceptance still requires configured Stripe webhook signing secret and end-to-end payment verification.

## Phase-01 Notes

- Monorepo skeleton created with `apps/{api,admin,customer,migrator}` and `packages/{database,contracts,config,testing}`.
- Shared TypeScript, ESLint, Prettier, and workspace configuration established.
- Money guard and env/config skeleton added in the shared config package.
- Docker compose skeleton for postgres, redis, minio, and mailhog added.
- Verify script is wired to run the project bootstrap checks and smoke test.
- No runtime application modules or database schema were created, in line with Phase 01 scope.

## Phase-02 Notes

- NestJS platform bootstrap implemented at the API app boundary: `RequestIdMiddleware` →
  helmet/CORS → global `api/v1` prefix → zod pipes → RFC 9457 `application/problem+json`
  exception filter → request-context and idempotency interceptors.
- `packages/contracts` (error codes, problem shape, pagination/filter/sort helpers,
  permission registry, request-id), `packages/config` (env schema, tenant-settings
  registry) and `packages/database` (Drizzle client, migration runner, CLI) established.
- `/health/live` and `/health/ready` outside the versioned prefix.
- Completed items that Phase 02 had left open: ESLint flat config that actually runs,
  coverage/tooling config, the generated `packages/contracts/openapi.json` artifact, and
  `docs/PHASE_02_IMPLEMENTATION_REPORT.md`.

## Phase-03 Notes

- Schema: `tenants, users, memberships, roles, permissions, role_permissions,
  membership_roles, refresh_tokens, tenant_settings` + `erp_migrations`, applied by
  `packages/database/migrations/0000_platform_identity.sql` (267 lines) with a reversible
  counterpart in `migrations/down/`. Verified idempotent (apply → skip) and reversible
  (down → re-apply) against a real PostgreSQL 16 cluster.
- RLS `ENABLE` + `FORCE` on `memberships, roles, role_permissions, membership_roles,
  tenant_settings`; `refresh_tokens`, `tenants`, `users`, `permissions` stay platform-wide.
- Roles: `erp_api` (NOBYPASSRLS, pinned on every migration run) and `erp_migrator`
  (BYPASSRLS, migration-only), created `NOLOGIN` in SQL; `LOGIN` + password only from
  `pnpm db:roles` so no credential is ever written into a migration.
- Guard pipeline frozen by `API_ARCHITECTURE §2` and asserted by `app.module.spec.ts`:
  `RateLimitGuard → AuthGuard → TenantGuard (RLS GUC) → BranchScopeGuard → PermissionsGuard`.
- Auth: RS256 access tokens (15 min, `sub/tid/mid/scope/jti`), 256-bit rotating refresh
  tokens stored as SHA-256, family revocation on reuse, Argon2id `m=65536,t=3,p=4`,
  lockout after 5 failures, 10/min login bucket, 423 for a suspended tenant, 403 for a
  forged `tid`.
- Testing: `packages/testing` provides the `TESTING_STRATEGY §6` isolation harness; it is
  applied to `memberships` and `roles` with all four proofs plus a direct-SQL RLS probe.
  Integration tests run against an embedded PostgreSQL with no Docker dependency.
- Toolchain: workspace packages now emit `dist/` and are consumed as compiled JavaScript at
  runtime (tests still resolve them to TypeScript source through vitest aliases). This is
  what makes `pnpm run build`, `openapi:export` and the entry-point smoke check pass.

## Phase-04 Notes

- Schema: `audit_log, files, notifications, outbox_jobs, idempotency_keys,
  document_sequences` applied by `packages/database/migrations/0001_platform_services.sql`
  with a reversible counterpart in `migrations/down/`. All six carry `ENABLE`+`FORCE`
  RLS; `audit_log` additionally has `UPDATE, DELETE, TRUNCATE` revoked from `erp_api`,
  so immutability is a privilege, not a convention (proved in `test/audit.spec.ts` by
  asserting SQLSTATE `42501`).
- Audit: a global `AuditInterceptor` records every successful mutating request
  (entity/action/actor/after/meta) and auth events including failures; a service that
  knows the previous state writes the row itself inside its transaction — with a real
  `before` — and marks the request audited so exactly one row is produced. Sensitive
  keys are redacted structurally at any depth before the row is written.
- Files: `POST /files/presign` → client PUT → `POST /files/{id}/finalize` →
  `GET /files/{id}/download` → `GET /files/{id}/content` (302). SigV4 is implemented
  in-repo and verified against the AWS reference vector; object keys are
  `tenants/{tid}/{yyyy}/{mm}/{fileId}/{name}`. `VirusScanner` and the SMTP mailer are
  ports with no-op/console adapters, per the phase's out-of-scope list.
- Jobs: queues `einvoice, notifications, reports-export, migration, maintenance`; nothing
  publishes from inside a business transaction — services write `outbox_jobs` rows and
  `OutboxPublisher` drains them per tenant (`FOR UPDATE SKIP LOCKED`, exponential backoff
  capped at 1 h, dead-letter at `OUTBOX_MAX_ATTEMPTS`). No component uses BYPASSRLS.
- `WORKER=1` boots the same image as an application context with no HTTP listener; it
  starts cleanly without Redis (inert driver, outbox rows simply stay `pending`).
- Idempotency: `idempotency_keys` replaces the Phase-02 in-memory map. The stored
  response is **text**, so a replay is byte-identical; a reused key with a different
  payload is a 409 `IDEMPOTENCY_REPLAY`; a failed handler releases the claim.
- Deviations recorded as CR-004 (unknown setting key on write: 404 → 400) and CR-005
  (additional file/notification/job endpoints + `platform.notification.view|manage`,
  `platform.job.view`).
- Known gap: no Docker in the build environment, so the presign→upload→finalize→download
  flow was verified against an in-memory storage fake rather than live MinIO, and the
  BullMQ hop was verified against a recording queue fake. Everything that touches
  PostgreSQL — including all RLS and concurrency proofs — ran against a real server.

## Conventions

- `docs/` remains the authoritative documentation source.
- Phase outputs must be self-verifying and must not contradict `PROJECT_CONTRACT.md` or `TARGET_ARCHITECTURE.md`.
- Implementation for later phases starts from this foundation only.
- A phase is `COMPLETE` only when `pnpm run verify` exits 0 on a clean checkout.

## Phase-05 Notes

- Schema: ten tables in `packages/database/migrations/0002_organization.sql` (424 lines)
  with `migrations/down/0002_organization.down.sql` (56 lines). Cycle proved on a real
  PostgreSQL 16 cluster: `up → 26 tables / 21 policies / 21 FORCE-RLS relations / 73
  indexes`, `up again → 0 applied, 3 skipped`, `down → 16 tables / 11 policies`,
  `up again → 26 tables` and `erp_api` still `NOBYPASSRLS`.
- The Phase-04 hand-off is closed: `document_sequences.branch_id` now carries its
  deferred FK to `branches (id) ON DELETE RESTRICT`, and the `company_profile` logo is
  the first entity registered in the `FileAttachmentRegistry` (which PHASE_04 shipped
  deliberately empty).
- Defaults: one default branch / warehouse / price list per tenant, one **per kind** for
  cash locations, one base currency — each enforced by a partial unique index
  (`… WHERE is_default AND deleted_at IS NULL`) and serialised by a transaction-scoped
  advisory lock, so eight concurrent "make me the default" requests all return 200 and
  exactly one default survives.
- Money and rates are decimal strings end to end (ADR-006). `resolveFx` reports which
  rung answered (`identity | direct | inverse | triangulated`) and, when triangulated,
  the pivot and the **staler** of the two legs; intermediate legs keep full precision so
  a derived rate never rounds twice.
- `resolvePostProfile(branchId, docType)` walks branch+docType → branch+`'*'` →
  tenant+docType → tenant+`'*'` and fails hard with `ACCOUNT_PROFILE_MISSING` rather than
  guessing an account.
- Account ids (`cash_locations.account_id`, `warehouses.inventory_account_id`, the
  posting-profile mapping) and `price_list_items.item_id` are shape-validated uuids with
  no FK until PHASE_07 / PHASE_06 (CR-006); every such column carries a
  `ValidatedAtRuntime` comment in the migration and the schema.
- Deviations recorded as CR-006 (deferred FKs), CR-007 (two `.view` permissions),
  CR-008 (`DELETE` = soft delete + the three read-only resolution routes), CR-009 (the
  prompt's "+9 tables" is a miscount; §4 and `DATABASE_DESIGN §5` both name ten).
