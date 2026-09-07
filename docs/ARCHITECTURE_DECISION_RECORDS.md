# ARCHITECTURE_DECISION_RECORDS (ADR Index)

> Level A — FROZEN. New decisions append; existing ones change only by superseding ADR.
> Format: Decision / Context / Alternatives / Reason / Consequences.

## ADR-000 Conventions snapshot
D: All canonical conventions live in `PROJECT_CONTRACT.md`.
C: 20+ executor conversations need one constitution. R: single SSOT, no drift.
Cons: changes here are intentionally heavy.

## ADR-001 Modular Monolith over Microservices
Ctx: tightly-coupled ERP transactions; small team; phase-per-conversation delivery.
Alt: microservices, serverless functions.
Why: ACID across sale→stock→journal; one deploy; module seams allow later extraction
(reporting/e-invoice first candidates). Cons: BullMQ worker shares codebase.

## ADR-002 NestJS (vs Fastify-express vanilla / Adonis)
Ctx: need enforcement points (guards/pipes/interceptors) + DI + OpenAPI generation +
test tooling. Why: matches 20+ module governance; ecosystem maturity. Cons: some
decoration ceremony; mitigated by generators used from P02.

## ADR-003 PostgreSQL + Drizzle (vs Prisma/TypeORM/MySQL)
Why: RLS, numeric, ltree, partial indexes; Drizzle = SQL-faithful types, explicit
migration SQL (accounting can't afford ORM magic). Cons: more manual relations code —
accepted.

## ADR-004 Shared-DB Multi-Tenancy with tenant_id + RLS
Why: ops simplicity at N tenants; isolation double-enforced (app + DB). Alt rejected:
db-per-tenant (this legacy's "DB per year" pain), schema-per-tenant fan-out.
Cons: strict review duty; mitigated by isolation harness in CI (per phase).

## ADR-005 UUID v7 PKs + separate human number sequences
Why: sortable, distributed-friendly (desktop offline push later), no enumeration leak
of business volume; human docs keep per-branch sequence like legacy. Cons: two id
concepts — mitigated by contract docs.

## ADR-006 Money = numeric(20,4) + decimal.js; forbid floats
Ctx: legacy float money caused rounding debt.
Cons: serialization verbosity; accepted.

## ADR-007 No cached running balances on master rows (initially)
Why: computed from immutable ledgers; legacy caches (`Total_Debts`, `ProductStocks`)
diverged in practice (RC-11/RC-20). Materialized views allowed later with proof =
placeholder. Cons: report compute cost — budgets defined in TESTING §5.

## ADR-008 Immutable posted journals & ledger; reversal-only correction
Why: audit integrity, ZATCA-era compliance; replaces legacy edit-in-place + His_Entry.
Cons: UX needs explicit void flows — covered in UI masters.

## ADR-009 Unified `parties` table with kind flags (vs Customers/Suppliers separate)
Ctx: legacy duplicated parties across 5 tables w/ shared ZATCA fields. Why: one party
360°, per-kind APIs via filters. Kept separate legacy concept: `salesmen` stays its
own table (commission domain) — enters P10 review. Cons: kind discipline via CHECKs.

## ADR-010 Unified `cash_locations` (safes+banks) and unified `vouchers` (Sand*+Receipts)
Why: legacy sprawl (5 voucher tables); same invariants everywhere. Cons: migration
complexity owned by registry maps (RC-19).

## ADR-011 Files in object storage; DB holds metadata
Why: legacy `image` columns & file paths unscalable. Cons: orphan GC job (P04).

## ADR-012 ZATCA designed-in (columns + submission ledger from P10, engine P13)
Why: compliance is not bolt-on. Cons: schema carries nullable einvoice fields early.

## ADR-013 Two frontends: admin + customer (+future POS skin inside admin pack)
Why: different release cadence & personas. Shared contracts package.
Cons: duplicated shell code — minimized via `packages/ui` if P17 shows reuse.

## ADR-014 Migration engine in-repo (`apps/migrator`) reusing domain services
Why: same invariants as runtime; no drift between import & app logic. Alt: external
ETL — rejected (duplicate rules). Cons: migrator needs heavy deps — scoped package.

## ADR-015 Tenant data docs in English; business terms bilingual
Ctx: RTL docs hurt diffs/tooling. Why: maintainability. Arabic UI copy lives in
frontend locale files (P17).

## ADR-016 One UI-master file per surface (admin/customer) instead of per-module files
Ctx: §17/§19 asked per-module + master. Consolidated to masters with per-module
SECTIONS to prevent divergence (explicit simplification, documented here).
Cons: larger single file — managed via section anchors.

## ADR-017 Installments & Contracting deferred to P21 (after core sales/treasury)
Why: they build on invoices, vouchers, parties; early risk low value. Same logic for
P19/P20/P22 packs. Cons: later phases must not retrofit core schemas — vertical
tables pre-declared in DATABASE_DESIGN §15.

## ADR-018 App-layer AES-GCM encryption for stored secrets (not pgcrypto)
Why: portable, testable, key rotation via env versioning. Cons: app responsibility —
redaction + tests in security suite.

## ADR-019 No tenant billing/metering in v1
Cons: launch requires external billing ops; revisit post-launch via ADR.

## ADR-020 Phases = 23, ordering frozen (see MASTER_PROJECT_PLAN §6)
Why: dependency-justified; each prompt self-contained. Cons: long program — offset by
per-phase verifiability.

## ADR-021 Go-live security audit waivers for build/test-only advisories
D: Phase 23 permits release with the documented `pnpm audit --audit-level high` findings waived only when the vulnerable package is not reachable from the production API/admin/customer runtime path, or when production configuration disables the vulnerable surface.
C: The September 2026 advisory feed reports high/critical findings in `vitest`, `vite`, `postcss`, and transitive build tooling. `drizzle-orm` was upgraded to the currently available patched line (`0.45.2`) during P23. Remaining high/critical findings are isolated to test runner/dev server/build CSS parsing surfaces; production containers run compiled apps and do not expose Vitest UI, Vite dev server, source-map ingestion endpoints, or user-controlled PostCSS compilation.
Alt: Block release until all upstream toolchain packages publish fixed compatible versions, or force major test/build tool upgrades that break the current TS/Vitest config.
Why: The production runtime exposure is zero under the release runbook; the risk is managed by CI-only execution, no public dev servers, source maps disabled for public builds unless explicitly approved, and recurring dependency-audit review before each release.
Cons: Security owners must revisit this waiver before v1.0.1 and remove it once compatible patched build tooling is available.
