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

| RBAC-REORG | 2026-09-10 | COMPLETE | Surface + RBAC reorganisation on `arena/01a0889e-cloud-saas-erp`: Family-A platform roles (`platform_memberships`, `pam` claim) replacing the blanket `is_platform_admin`; canonical `tenant.*` permissions with legacy `platform.*` aliases; `memberships.kind` (staff/buyer/api); standalone devices registry. Four separately-deployable surfaces on one API/DB: `apps/marketing` (:3002), `apps/staff` (:3001), `apps/platform-admin` (:3003), `apps/customer-portal` (:3004) — all build + tests green (staff 35, marketing 6, platform-admin 3, portal 6). New real screens: platform roles grant/revoke, licence activation requests, tenant audit log, smart login with `?token=` bridge. Docs: `docs/architecture-rbac/01–06`. See final report in PR. |

## Admin web UI — desktop menu coverage (2026-09-08)

`apps/admin/lib/navigation.ts` is the single source of truth for the screen tree that
mirrors the customer's desktop product. `tests/navigation.spec.ts` fails the build if a
menu item claims `ready` without a page file behind it, so these counts are checked, not
asserted by hand.

| State | Count | Meaning |
|---|---|---|
| `ready` | 189 | A real screen reading and writing the live API. |
| `api` | 0 | The endpoint exists; the screen is still the scaffold. |
| `planned` | 1 | Neither screen nor endpoint yet; routed under `/s/…`. |
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

Round 5 wired the contracting side of projects (migration `0026`):

* **عقد مقاول** — `contractor_contracts` + `contractor_contract_lines`. The contract value
  is derived from the lines rather than typed on the header, because a header that
  disagrees with its own breakdown is how a subcontractor ends up over-certified. The
  agreed advance lives on the contract (not on a payment) since it is recovered across
  many certificates, so the running `advance_recovered` total is contract state. A
  contract must be activated before any money can be certified against it, and it cannot
  be closed while a certificate is still unpaid.
* **سند دفع لمقاول** — `contractor_payments`. Retention and advance recovery are
  **computed from the contract**, never taken from the request; the form previews them,
  the server decides them. Four kinds behave differently on purpose: an `advance` is a
  prepayment (no retention, does not consume the contract value, capped by the agreed
  advance), `progress`/`final` carry retention and may recover the advance, and
  `retention_release` can never exceed the retention actually held. Cumulative gross on
  the value-consuming kinds is capped at the contract value
  (422 `CONTRACTOR_PAYMENT_EXCEEDS_CONTRACT`), and a cancelled certificate gives its value
  back. Paying issues a **draft** payment voucher through `TreasuryService` and links it
  via `voucher_id`; posting to the ledger stays in treasury, so the money has one door
  into the journal instead of two.
* **عروض المشاريع** — `project_offers` + `project_offer_lines`. Accepting an offer is
  ledger-neutral. Converting one creates the project and copies the offer lines into the
  BOQ, which is the only way the offered numbers and the project's numbers are guaranteed
  to agree; an offer converts exactly once (409 `OFFER_ALREADY_CONVERTED`) and an expired
  offer must be re-issued first.

New permission `projects.contractor.pay` (121 total) gates approving and paying a
certificate; creating one still needs only `projects.manage`, so the person who measures
the work is not necessarily the person who releases the cash. Endpoints live under
`/contracting/*` rather than `/projects/*` because `GET /projects/:id` already owns that
segment.

Round 6 wired the two documents that reverse or transform recorded value (migration
`0027`):

* **مرتجع مقاولات** — `contracting_returns` + lines. A posted progress bill cannot be
  edited: it has already produced a numbered sales invoice and moved every BOQ term's
  billed-to-date figure. The return is therefore its own document, and posting it moves
  **both** halves at once — the BOQ term gives its value back so the work can be
  re-billed, and a **draft** credit note is raised against the bill's invoice for the net
  after the withheld retention is released. Doing one without the other leaves the project
  either double-billed or permanently short of its own contract value. Per term, the
  cumulative return can never exceed what that bill certified (422
  `CONTRACTING_RETURN_EXCEEDS_BILL`), and cancelling a draft return frees its value again.
  Report key `contracting-returns`.
* **أمر الإنتاج** — `production_orders` + `production_order_components`. Components leave
  the warehouse at its moving average and the finished item is valued at exactly the total
  that left, divided by the produced quantity; the order is ledger-neutral by construction
  because inventory value is conserved, so it raises no journal entry. The output item may
  not be one of its own components, a component may not repeat (combine the quantities),
  and completion fails on `STOCK_INSUFFICIENT` rather than driving stock negative. The
  components are **optional**: when none are typed they are read from the item card's bill of
  materials (`item_components`, `GET/POST/DELETE /organization/catalog/items/:id/components`)
  and scaled by the produced quantity, each in the unit the card named — the desktop's
  `Qty = qty × BaseQty × UnitEquality`. An item with no recipe and an order with no typed
  components is refused `PRODUCTION_COMPONENTS_REQUIRED`. A component's unit must be its own
  base unit or one defined on its card, and a recipe may not form a loop
  (`CATALOG_COMPONENT_CYCLE`). The
  `unit_id` (0038) holds the unit the produced quantity was counted in, so `2 علب` of a
  six-piece box puts twelve pieces on the shelf. The
  `line_id` of each stock movement is the item id, so the existing
  `(tenant, doc_type, doc_id, line_id)` unique index enforces one movement per item per
  order. Screen `/inventory/production`, report key `production-orders`.

* **الأرقام التسلسلية والدفعات** — `item_serials` + `item_lots`. A serial is a state machine
  (`available → reserved → sold → available`) driven from `/inventory/serials` the way
  `frmItemSerialNo` drives it: two grids (`📋 الأرقام المتاحة` / `📤 الأرقام المباعة`), a
  generator that makes a batch off one prefix (`POST /inventory/serials/generate`,
  all-or-nothing, `409 SERIAL_DUPLICATE` on a clash), and `DELETE /inventory/serials/:id`
  for a number that never left the shelf — a sold one is refused `422 SERIAL_INVALID_STATE`,
  because deleting it is how a stock count stops adding up. A lot carrying serials answers
  `409 LOT_IN_USE`. Screens `/inventory/serials`, `/inventory/lots`, report keys
  `serial-tracking`, `expiry-report`.
* **الرقم التسلسلي على سطر المستند** (`InvoiceItemDetail.ItemSerialNo`,
  `Class/InvoiceOper.cs:1635`) — migration `0039` puts `serial_nos` on the voucher,
  adjustment and transfer line tables and adds `stock_document_serials`, so a document says
  *which piece* it moved and a number can be traced back to the documents that moved it
  (`GET /inventory/serials/:id/documents`, permission `inventory.view`). The numbers are
  resolved at posting, not at saving: a draft invents no pieces for stock that has not
  arrived, a count that disagrees with the quantity is `422 SERIAL_COUNT_MISMATCH`, a number
  in another warehouse is `422 SERIAL_WRONG_WAREHOUSE`, and selling a number twice is
  `422 SERIAL_INVALID_STATE`. إلغاء is the mirror image and deliberately asymmetric: a
  receipt's numbers are withdrawn only while they are still on the shelf, an issue's numbers
  go back on it. A مناقلة contributes two legs — the send that reserves the piece and the
  receipt that releases it. Screens: the four stock document grids gained a
  `🔢 الأرقام التسلسلية` column and a paste-box with a live count; the serials screen gained
  a `🔍` trace per row.

* **المرحلة 06 — الخزينة، الجزء الأول: سند القبض وسند الصرف** (`frmSandQ` / `frmSandD` /
  `frmSandVAT` / `frmPaymentVoucher`، و`Class/ReceiptOper.cs` L21 `BindReceiptToEntry`).
  Migration `0040` puts the document's context on the row — `description` (📝 البيان، وهو
  نفسه بيان القيد كما في `entry.Note = Receipt.Notes`), `voucher_time` (⏰ الوقت، فكشف
  الصندوق يُرشَّح بالساعة), `salesman_id → employees` (👔 المندوب) و`foreign_amount`
  (💲 قيمة السند بعملتها) — and the engine now builds the entry a voucher writes instead
  of waiting for the caller to supply lines: the cash location's own account against the
  party's receivable/payable account, then the posting profile. Callers who forgot — HRM's
  `payRun` chief among them — used to move cash out of the safe with **no entry at all**.
  A cheque is a promise, not money: `chequesInHandAccountId` (أوراق القبض) holds it until
  clearance, which now posts its own entry (`مدين الصندوق / دائن أوراق القبض`), and a
  bounced cheque puts the debt back on the customer and is terminal
  (`422 CHEQUE_INVALID_STATE`). `PATCH /vouchers/:id` edits a whole draft the way
  `frmSandQ.xaml.cs:903` does and seals a posted one (`409 VOUCHER_IMMUTABLE`);
  `GET /vouchers?from=&to=&q=` is the 🔍 panel of `frmSandQD`/`frmSandSD`. Screen
  `/treasury/vouchers` rebuilt as a document: tabs 📥 سند قبض / 📤 سند صرف, a search panel,
  a document header, `💼 تفاصيل الدفع` with the cheque block behind the bankish methods,
  and a grid with `🔢 الرقم · 📅 التاريخ · ⏰ الوقت · الطرف · 📝 البيان · 🏦 الصندوق ·
  💳 نوع الدفع · 💰 المبلغ · 📋 الحالة`. New profile key `chequesInHandAccountId`. Tests
  `apps/api/test/treasury-vouchers.spec.ts` (7) and `scripts/verify-treasury.mjs`
  (6 sections against the live stack).

* **المرحلة 06 — الخزينة، الجزء الثاني: تعريف الخزن والبنوك** (`frmTreasury.xaml` +
  `.xaml.cs` L87 grid / L222 «يجب اختيار موظف مسئول»، و`frmBanks.xaml`). Migration `0041`
  is additive: `cash_locations.notes` for 📝 ملاحظات, and a real `cash_location_custodians`
  table for the desktop's `Stock_Emps` — one row per (tenant, safe, employee), unique so an
  employee cannot be signed twice onto the same safe, indexed by employee, RLS like the rest.
  The desktop deletes `Stock_Emps` first and inserts after, so a typed typo leaves a safe
  with no custodian on the way to failing; here the employees are validated **before any
  write**, and emptying a safe of its custodians is refused with the same
  «يجب اختيار موظف مسئول» rather than performed. Banks keep the full `frmBanks` card —
  🌍 الدولة، 🏙️ المدينة، 📍 المنطقة، تليفون، موبايل، 💰 نسبة الاقتطاع — carried in the
  `bank` JSON block, so no column touches half a table that is safes. Screens
  `/treasury/safes` and `/treasury/banks` are one component with the desktop's three tabs
  (📋 بيانات · 👤 مسئولي الصندوق · 📝 ملاحظات), and الخزينة became its own `🏦` module in
  the staff navigation as it is in `Desktop_ERP`, taking 📄 سند قبض / 📄 سند صرف /
  📒 بطاقة حساب المصاريف / 📊 إغلاق اليومية home from المحاسبة › العمليات — routes
  untouched, endpoints untouched, duplicates removed. Tests
  `apps/api/test/treasury-custody.spec.ts` (6) and section 7 of
  `scripts/verify-treasury.mjs`. 488 API tests, 36 staff tests, 71 contract tests.

* **المرحلة 06 — الخزينة، الجزء الثالث: حركة الصندوق** (`Form_WPF/frmRptKhzna.xaml` +
  `.xaml.cs` L156–L260، والتقرير `Reports/RptKhzna.repx`). The decisive thing the desktop
  does here is that the statement is read from the **ledger**, not from the receipts: it
  resolves the safe's account and groups `Entry_sub` by entry, so a sale, a salary and a
  transfer are movements of the same safe. The cloud's `cash-movement` report summed
  receipts and payments per box, which silently omitted every movement the treasury screen
  had not created. `GET /cash-locations/:id/movements` now returns the statement:
  `رصيد سابق` opening row (only when a period is chosen, dated `من تاريخ − يوم` as at
  L200), a running `⚖️ الرصيد`, and the two cards `⚖️ الرصيد الإجمالي` /
  `📅 رصيد الفترة المحددة` (L482/L502) — with `من وقت / إلى وقت`, and only posted entries,
  so a draft never moves a safe on paper. No migration: `vouchers.voucher_time` came with
  part one. **One justified deviation:** the desktop's `Entry.date` carries the time, ours
  carries it on the voucher, so a movement with no recorded time is never hidden and never
  pushed into the opening balance — hiding a real entry from a statement is the worse
  error — while timed movements obey the window exactly and the totals stay continuous.
  Screen `/treasury/movements` with the desktop's filter panel, its eight columns, six
  cards and its CSV header verbatim (`م,العملية,الرقم,التاريخ,وارد,صادر,الرصيد,البيان`).
  Tests `apps/api/test/treasury-movements.spec.ts` (6) and section 8 of
  `scripts/verify-treasury.mjs` (13 checks). 494 API tests, 36 staff tests, 71 contract
  tests. The `🏦 حركة الصندوق` screen sits in a new التقارير group of the الخزينة module;
  the desktop files it under المحاسبة › تقارير محاسبية, but a safe's statement belongs
  with the safe now that الخزينة is a module of its own.

* **المرحلة 06 — الخزينة، الجزء الرابع: إغلاقات اليومية** (`Form_WPF/frmCloseShift.xaml`
  + `.xaml.cs` L214/L270/L330/L676/L735، `frmCloseShiftDetails.xaml`،
  `frmCloseShiftInv.xaml`، والمحرك الحقيقي في `Form_WPF/ClosShiftAndroid.xaml.cs`
  L592 وL780–L930). The grid's first column is `🔢 الرقم`, and in the desktop that is
  `CasherClosed.ClosedID` — **a close is a document the cashier signs**, and
  `BindCloseShiftToEntry1` builds a journal entry around it. The cloud created its
  `shift_closes` row when the drawer was *opened* and gave it no number at all, so
  "which close was Tuesday's?" had a uuid for an answer. Migration `0042` adds
  `shift_closes.number` with a partial unique index, **nullable on purpose**: an open
  shift is a draft, and the number is allocated at close from `document_sequences`
  (`CS-`, padding 6) exactly as a voucher is numbered on posting. No renumbering, no
  backfill: old rows keep their emptiness until a new shift is closed.
  `GET /shift-closes/day-closes` (filters `from`/`to`/`branch_id`/`membership_id`/
  `user_id`) is the list; `GET /shift-closes/:id` is one close with its two children —
  🧾 الملاحظات المعدودة and the summary lines — and answers `404 SHIFT_NOT_FOUND` for an
  unknown *or non-uuid* id. Three decisions carry the desktop's intent: **a draft is not
  cash** (📤 المصاريف و💵 النقدي read *posted* vouchers, so an unposted expense never
  shrinks a drawer on paper); **🏦 رصيد الصندوق is what was counted, 💵 النقدي is what
  was expected**, and 📉 الفرق is between them — which is why an open row's safe balance
  is empty rather than wrong; and **a close is a snapshot**, frozen into `summary` so a
  voucher posted afterwards cannot rewrite a signed sheet. 🚗 توصيل · ☕ ضيافة · 🛒
  المشتريات · 🛡️ تأمين come from the *name* of the expense type whose account the voucher
  points at, because the desktop reads columns our invoices do not carry and a tenant that
  names its types in Arabic gets the split for free. Filtering by 👤 الموظف resolves the
  membership to its users, and an id that is nobody's returns an **empty list, not the
  whole book** — a silently dropped filter is worse than a missing one. Screen
  `/treasury/day-close`: six cards, a 🏦 الوردية الحالية card with nine denominations and
  a live 📉 الفرق, a filter panel, and a grid whose eighteen headers are `frmCloseShift`'s
  own labels with a totals footer and an expandable detail per row. Tests
  `apps/api/test/treasury-dayclose.spec.ts` (8) and section 9 of
  `scripts/verify-treasury.mjs` (23 checks, and it closes a drawer left open by an earlier
  run so it stays re-runnable). 502 API tests, 36 staff tests, 71 contract tests.
  **Deferred with a reason:** the close's journal entry (`BindCloseShiftToEntry1`) waits
  for the accounting part of this phase, so entries come from one engine and not two, and
  the printed reports (`Reports/rptCloseShift.repx`, `rptCloseday.repx`,
  `rptClosedayCust.repx`) wait for the reporting phase — `printShiftData` only prepares
  their data.

* **المرحلة 06 — الخزينة، الجزء الخامس: التحويل البنكي والعميل النقدي**
  (`Form_WPF/frmPayBank.xaml` + `.xaml.cs` `LoadBanks`/`BankTile_Click`،
  `Form_WPF/frmCashCustomer.xaml` + `.xaml.cs` `SearchCustomers`، والقاعدة في
  `Class/EntryOper.cs` L493/L537/L620). Two small windows with one idea each.
  **🏦 التحويل البنكي** is a chooser, and its answer decides an *account*: the desktop
  refuses a transfer with no bank («يرجى اختيار بنك أولًا») because `EntryOper.cs` keeps
  a named transfer out of the generic شبكة bucket and, at close, debits **that bank's own
  account** instead of `1221001`. The cloud could already route a transfer to a bank, but
  nothing read the choice back: `shiftTakings` now groups bank payments **per bank**,
  `closeShift` writes one signed `bank-transfer` line per bank, and every day-close row
  carries `banks[]` — live while the drawer is open, frozen in `summary` once it is
  counted. The bank rides in `metadata`, because `party_id` is a foreign key to `parties`
  and a bank is not a party. 🌐 الشبكة still carries the full amount: the breakdown is a
  detail *inside* it, not a subtraction from it, or 💰 مجموع الشبكة والنقدي would stop
  adding up. **👤 العميل النقدي** is the opposite kind of answer: `SearchCustomers` does
  not open a customer table, it reads the invoices — `SELECT CashCustomerName,
  CashCustomerMobile FROM inv WHERE … AND CashCustomerName <> ''` — because a walk-in is
  a name and a mobile **written on the sale**, which is why a till can produce one
  without opening a ledger account. `GET /sales/cash-customers?name=&mobile=` is that
  query: exact on mobile, partial on name, grouped so a name is one answer with an
  invoice count. **No migration — deliberately**: `invoice_payments.cash_location_id`
  and `sales_invoices.cash_customer_name/mobile` already existed; what was missing was
  the rule that reads them, not a column. Screens: a `🏦 اختر البنك` window wired into
  `/sales/pos` (replacing a dropdown a cashier clicks past) and `/treasury/vouchers`, and
  a `👤 عميل نقدي` picker plus its own page `/sales/cash-customers`. Tests
  `apps/api/test/treasury-bank-transfer.spec.ts` (8) and
  `apps/api/test/sales-cash-customer.spec.ts` (7), and section 10 of
  `scripts/verify-treasury.mjs` (12 checks). **517** API tests, 36 staff tests,
  71 contract tests. **One justified deviation:** the desktop's cash-customer grid starts
  empty and fills only on a keystroke; a list screen that opens empty looks broken, so
  with no search term the API returns the most recently served names.

* **المرحلة 06 — الخزينة، الجزء السادس: مناقلة الخزن**
  (`Form_WPF/frmSafesTransfer.xaml` + `.xaml.cs` L690/L863/L872،
  `Reports/rptSafeTransfer.repx`، والجدولان في `CrystalLiteDB.txt` L1620 `SafesTransfer`
  وL1642 `SafesTransfer_Sub`). **The decisive finding came before the code**: the
  desktop's `SafesTransfer` table has **no amount column** — its sub-table carries *items*
  (`ItemId`, `value` = quantity, `AvrgCost`, `ReceivedValue`, `Diff`) and
  `rptSafeTransfer.repx` prints الصنف / الفئة / المستودع / الباركود / الكمية. So
  `frmSafesTransfer` is a **مناقلة أصناف بين المخازن**, and moving *money* between safes
  is done in the desktop with a سند صرف and a سند قبض. The cloud therefore needed both
  halves: a new money screen `/treasury/transfers` on `/cash-transfers` carrying the
  window's own three tabs (📦 التحويل · 📥 استلام تحويل · 🔍 البحث) and its state machine
  (draft → sent → received, with `🗑️ حذف` for drafts only), and the missing 🔍 tab on the
  item screen `/inventory/transfers` (`🔢 رقم التحويل` · `📅 من تاريخ` · `📅 إلى تاريخ` ·
  `📋 كل الفترة` · `🔍 بحث`). `transfers()` now returns `fromName`/`toName` resolved
  server-side by joining `cash_locations` twice under aliases, so the grid never shows a
  raw uuid where the desktop shows a name; `cancelTransfer` writes `voided`, the terminal
  state migration `0011` already allows, rather than inventing `cancelled` and a migration
  to go with it. **No migration — again deliberately.** Screens gated by
  `treasury.view`, actions by `treasury.transfer.manage`; 📦 استلام الكل shows a live
  count of what is on the road. Tests `apps/api/test/treasury-transfers.spec.ts` (9) and
  section 11 of `scripts/verify-treasury.mjs` (19 checks, walking the whole lifecycle
  against the live stack). **526** API tests, 36 staff tests, 71 contract tests.
  **Two justified deviations:** `🏦 من خزنة` / `🏦 إلى خزنة` are the window's
  `🏪 من مخزن` / `🏪 إلى مخزن` with the store replaced by the safe — this module moves
  cash, not stock; and `📋 الحالة` names a column the desktop grid leaves unheaded.

New permissions `inventory.production.manage` and `inventory.production.complete` (123
total): planning a recipe and consuming the warehouse against it are different decisions.
Posting a contracting return reuses `projects.bill.post` — reversing certified work is the
same authority taken backwards.

### Round 7 — file-level operations and the report designer (2026-09-08)

The last six screens in the tree are the ones that can destroy a company's data, so each
was built around what it refuses to do. Migration `0028` adds `backup_runs`, `restore_runs`,
`maintenance_runs`, `company_files` and `report_layouts`, all under FORCE RLS.

* **النسخ الإحتياطي** (`/settings/backup`, `POST /settings/backups`) — a logical, tenant-scoped
  snapshot: every table carrying `tenant_id`, read through RLS, with per-table row counts and
  a sha256 checksum. It excludes identity (`users`, `memberships`, `roles`) and the audit log,
  because re-importing credentials or a rewritten audit trail is an attack, not a restore. Past
  50 000 rows it fails with `BACKUP_TOO_LARGE` and points at `pg_dump` instead of writing an
  export nobody could restore.
* **إستعادة البيانات** (`/settings/restore`) — dry run by default; applying is **additive only**
  (insert-missing, never delete or overwrite) and requires the file code typed back
  (`RESTORE_CONFIRMATION_REQUIRED`). Tables are retried across passes so foreign-key order
  resolves itself, and `tenant_id` is forced to the current file so a foreign snapshot cannot
  smuggle rows across.
* **تدوير البيانات** (`/settings/data-rotation`) — deletes operational logs only (notifications,
  outbox jobs, idempotency keys) older than a cutoff that must be at least 90 days in the past
  (`ROTATION_CUTOFF_TOO_RECENT`), and shows the documents it is *not* deleting next to them. The
  audit log cannot be rotated at all: migration `0001` revokes DELETE on it from `erp_api`, and
  the preview reports that as `retainedByDesign` rather than pretending otherwise.
* **صيانة الفواتير** (`/settings/invoice-maintenance`) — scans for header totals that disagree
  with their lines, posted invoices with no journal entry, numbering gaps and stale drafts.
  The repair rewrites **draft** totals only; a posted discrepancy is reported for a credit note,
  never silently edited.
* **إنشاء ملف** (`/settings/new-file`) — a sibling company file is a new tenant, provisioned
  through the signup path so it starts **unlicensed** with a pending activation request: a tenant
  permission must never be able to mint licensed tenants. Master data (accounts, catalog, parties,
  structure) can be copied with every id remapped; documents, balances and users never cross.
* **مصمم التقارير** (`/support/report-designer`, `/reports/layouts`) — layouts store presentation
  only: column choice, order, headings and default filters. The report's SQL stays in the
  server-side catalog, so a designer cannot become a query editor pointed at other tenants' data.
  Saved layouts appear as a picker on every report screen (`?layout=<id>`, `layout=none` for the
  raw columns).

Six new permissions (129 total): `settings.backup.manage`, `settings.restore.manage`,
`settings.rotation.manage`, `settings.maintenance.manage`, `settings.companyfile.create`,
`reporting.layout.manage`.

Fixed on the way: `ReportingService` read the module-level database singleton instead of the
injected handle, so under test it queried a different database than the one the test had
provisioned. Both it and `ReportLayoutsService` now take `DATABASE_HANDLE`.

Still `planned` — 1 screen: إعدادات جهاز التحضير (preparation device), excluded by the customer.

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

## Round 8 — printed documents and editable master data

- **Printing is real.** `apps/api/src/modules/reporting/print-templates.service.ts`
  replaces the one-line HTML stubs with full A4 documents (company header + VAT/CR
  number, counterparty, lines, totals, payments, tafqeet, signatures, ZATCA QR when the
  invoice has been reported). Routes: `/reports/print/{invoices|purchase-invoices|
  vouchers|journal-entries|shifts}/:id`. The admin viewer lives at
  `/print/[doc]/[id]` and every document screen links to it.
- **Master-data cards can be corrected and withdrawn.** New `PATCH`/`DELETE` endpoints for
  catalog items, categories, units and tax groups, accounts, cost centres, salesmen,
  expense cards and HRM departments/jobs/employees. `apps/admin/components/directory.tsx`
  grew `edit` and `onDelete`, and the item, category, unit, account, cost-centre, branch,
  cash-location, warehouse, customer, supplier, salesman, payment-method, expense and HRM
  screens all use them.
- **What editing refuses is the point.** A used item keeps its SKU and base unit; a used
  tax group keeps its rate; a posted account keeps its number, nature and side; a category
  or unit with items behind it, an account with children or entries, and a department with
  staff cannot be deleted at all; an item, an employee or a salesman that already appears
  on a document is archived instead of removed. Reparenting an account moves its whole
  subtree (`path`/`level`) in one statement.
- `POST /sales/salesmen` did not exist while the screen already posted to it — added, with
  `sales.salesman.manage` (permission count 130; re-run `pnpm db:seed`).

## Round 9 — real report exports

- **`POST /reports/:key/export` produces actual files.** It used to return CSV whatever the
  caller asked for, and the admin never called it at all — the screen serialised the rows it
  had already rendered, which ignored the saved layout and dropped anything not on screen.
  The export now re-runs the report server-side with the same filters and returns
  `{ filename, mimeType, encoding, content }`.
- **`xlsx` is a genuine workbook**, written by `apps/api/src/modules/reporting/xlsx.ts` — a
  dependency-free OOXML + ZIP writer (`node:zlib` deflate, hand-rolled CRC-32). Right-to-left
  sheet, frozen header row, auto-filter, `#,##0.00` numeric cells, bold totals band. Verified
  by unzipping the output and by opening it with a third-party reader.
- **`pdf` returns a print-ready A4 landscape page** on the company letterhead with the header
  band repeating on every page, handed to the browser's print dialog. No PDF renderer is
  bundled: Arabic PDF text needs an embedded font with contextual shaping, and the print
  dialog already yields a smaller, selectable document.
- **Filters are printed as words.** `branchId=<uuid>` becomes `الفرع: الفرع الرئيسي` in both
  the workbook caption band and the printed header; the lookup is best-effort and can never
  fail an export.
- Codes stay codes: only clean decimals become numeric cells, so `1101`, `SI-000006` and any
  value with leading zeros survive the trip to Excel intact.
- Tests: `xlsx.spec.ts` (6) plus four export cases in `test/printing-and-cards.spec.ts`.

## Round 9 (part 2) — ZATCA e-invoicing is a real document, not a mock

- **The invoice XML is a UBL 2.1 document** built from the tenant's own data
  (`apps/api/src/modules/einvoicing/zatca/ubl.ts`): seller party with VAT/CR and national
  address, buyer party, per-line tax categories, one `cac:TaxSubtotal` per rate, closing
  `cac:LegalMonetaryTotal`, `388`/`381` type codes and the `0100000`/`0200000` standard vs
  simplified flag. It replaces a five-element fake that no validator would have accepted.
- **The hash chain is real.** `einvoice_chain` now also carries the invoice counter (ICV,
  migration `0029`), handed out with the previous hash (PIH) under `FOR UPDATE`; both are
  embedded in the document, and the next invoice's PIH is this invoice's hash.
- **The QR is the tenant's.** TLV tags 1–5 are built from the company card and the invoice —
  the old code hard-coded `Tenant seller` and a VAT number of fifteen zeros. Tags 6–8 (hash,
  ECDSA signature, public key) appear only when the tenant has uploaded an EC private key.
- **The system no longer claims acceptance it did not get.** New submission states
  `prepared` (document built, no credentials) and `signed` (signed, no gateway configured);
  `reported`/`cleared` are written only after a real `2xx` from `ZATCA_API_BASE_URL`, and the
  HTTP response is stored. `retry` re-files the stored document instead of flipping a flag.
- Admin: الإعدادات ← المزامنة ← Zatca explains the states, shows the counter and the invoice
  profile, and can download the stored XML for any submission.
- Still credential-bound and documented as such in the module README: the XAdES signature
  block, ZATCA onboarding (CSR → compliance CSID → production CSID) and QR tag 9.
- Tests: `zatca/zatca.spec.ts` (9) and `test/einvoicing.spec.ts` (6, integration).

## Round 10 — the customer portal stops being a mock-up

- **`apps/customer` now serves real customers.** Every screen that used to render a hard-coded
  row is gone or wired: dashboard, invoices, invoice detail (lines, totals, payments, printed
  A4 HTML), statement with a running balance and a CSV download, payments, and a read-only
  "بياناتي" card. The screens with no backing API — بيع سريع، استعلام مخزون، صندوق المهام،
  الإشعارات، منتقي المستأجر — were deleted rather than left as furniture.
- **A portal login is an ordinary user with an empty role.** `portal_accounts` (migration
  `0030`, RLS forced, `UNIQUE (tenant_id, user_id)`) links a login to exactly one party; the
  membership carries the permission-free system role `Customer portal`, so every
  `@RequiresPermission` route answers 403, and `/portal/*` — which carries no permission
  decorator — resolves the party from the token, never from the request. A foreign invoice is
  a **404**, not a 403. See `apps/api/src/modules/portal/README.md`.
- **Granting access is a back-office action**: المبيعات ← أخرى ← وصول العملاء للبوابة, or the
  «بوابة العميل» button on بطاقة عميل. The generated one-time password is shown exactly once,
  and `mustChangePassword` sends the buyer to `/auth/change-password` on first sign-in.
- **The client talked to `http://localhost:3000` and therefore only ever worked on a
  developer's laptop.** It now uses the app's own origin (`/api/v1`, rewritten by
  `next.config.mjs`), which is also what makes the portal usable behind a proxy or preview URL.
- `/verify` decodes a ZATCA QR (TLV) in the browser — seller, VAT number, timestamp, total, VAT
  and whether tags 6–8 are present — instead of printing a canned sentence. No endpoint, no
  account, nothing to leak.
- Tests: `apps/api/test/portal.spec.ts` (10, containment-first) and `apps/customer` 8.
  Repository total **475**.

## Round 11 — security and settings the desktop edition always had

- **TOTP two-factor authentication is real, end to end.** Migration `0031` adds
  `users.mfa_enabled` and `mfa_recovery_codes` (SHA-256 hashes only). Enrolment is
  two-phase: `POST /auth/mfa/enroll` seals a fresh secret under AES-256-GCM
  (`secret-box.ts`, same `v1:` envelope as the e-invoicing credentials, key from
  `DATA_ENC_KEY`) but does not enforce 2FA; `POST /auth/mfa/enable` flips
  `mfa_enabled` only after a valid code proves the authenticator was actually
  configured, and issues eight one-time `XXXX-XXXX` recovery codes (ambiguous glyphs
  excluded) shown exactly once. Login with 2FA on returns 401 `MFA_REQUIRED` when the
  code is missing and treats a wrong code as a failed login (lockout counter
  advances). Recovery codes are single-use and accepted wherever the 6-digit code is.
  Disabling requires the account password. `totp.ts` is RFC 4226/6238 on
  `node:crypto`, pinned against the RFC 6238 test vectors. Admin UI: الإعدادات ←
  المستخدمون ← التحقق بخطوتين, plus a code step on the login screen.
- **Mail actually delivers.** `MAIL_TRANSPORT=smtp` now routes through a hand-rolled
  SMTP client (`SmtpMailer` — EHLO, opportunistic STARTTLS, AUTH LOGIN, dot-stuffing,
  RFC 2047 subjects; no new dependency), tested against an in-process fake relay.
  Granting portal access e-mails the buyer their one-time password unless
  `notify:false`; delivery failure never rolls back the grant. `console` stays the
  default transport; MailHog in the compose file is the target (`SMTP_HOST=localhost
  SMTP_PORT=1025`).
- **The language screen exists.** الإعدادات ← عامة ← اللغة flips the whole document
  between Arabic/RTL and English/LTR and persists in `localStorage`. The chrome — app
  shell, navigation tree (both names already lived in `navigation.ts`), login, common
  states — is fully bilingual via `lib/i18n.tsx`; screen content stays Arabic-first by
  design and the page says so.
- Tests: `test/mfa.spec.ts` (12), `totp.spec.ts` (7), `secret-box.spec.ts` (4),
  `mailer.spec.ts` (3), portal suite +2 (invite mail, `notify:false`). API **372**,
  admin **35**; repository total **508**. Migrations through **0031**.

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
