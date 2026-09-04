# PROJECT_OVERVIEW

> Level C (reference). Canonical facts live in `PROJECT_CONTRACT.md`.

## 1. Background

The customer operates a production **Desktop ERP** built with **C# + SQL Server**
(database scripted as `Data16`). It is a mature, multi-vertical system with real
business data and years of organic growth: core accounting, inventory, sales/POS,
purchasing, treasury, payroll, plus vertical modules discovered in the schema
(restaurant tables, marina/boat rental, tailoring measurements, optics prescriptions,
vehicle spare-parts fitment, contracting/project billing, installment contracts),
plus integrations (ZATCA e-invoicing KSA, ETA e-invoicing Egypt, Salla e-commerce,
MQTT branch sync).

The goal is **not** to clone this system. The goal is a **new, cloud-native,
multi-tenant SaaS ERP** that can fully replace it, with a **safe, verifiable data
migration path** per customer.

## 2. Vision

> One cloud platform. Every company (tenant) isolated. Every posting auditable.
> Every legacy customer migratable with provable numbers.

| Pillar | Commitment |
|---|---|
| Consistent | One SSOT (`docs/`), one naming system, one API contract. |
| Maintainable | Modular monolith, explicit module boundaries, dependency rules. |
| Scalable | Shared-DB multi-tenancy, pooler-friendly, horizontal API scaling. |
| Secure | Tenant isolation at app + DB (RLS) layers; audited; encrypted secrets. |
| Migratable | Deterministic, replayable, per-tenant migration with reconciliation reports. |
| Testable | Test pyramid per phase; tenant-isolation and accounting-invariant suites mandatory. |

## 3. Current System — Hard Facts (from the two schema dumps)

- ~180 tables, 13 stored procedures, 13+ table-valued functions, 1 view.
- Only **4 formal FOREIGN KEY constraints** in the entire database
  (`EquipModels→EquipMakes`, `ItemVehicleFitment→Makes/Models`, `PeriodLocks→AccountingPeriods`).
  All other integrity is **application-enforced** → migration must not assume referential integrity.
- Money stored as `float` almost everywhere (rounding debt); quantities mixed `float`/`real`.
- **One database per fiscal year** pattern (`Year_Previews`, DB name `Data16`) — the new
  system replaces this with `fiscal_years` rows.
- Soft-delete flags (`IS_Deleted`) inconsistent (nullable bit, sometimes missing).
- User passwords in `Users.pwd nvarchar(50)` — no hashing evidence (`CONFIRMED` risk).
- Arabic-first UI (nvarchar names, `DEFAULT 'عميل عام'` for walk-in customer).

Full detail: `LEGACY_DATABASE_ANALYSIS.md`, `LEGACY_BUSINESS_LOGIC.md`.

## 4. Target System — Decisions in One Paragraph

**Node.js 22 + TypeScript (strict) + NestJS modular monolith + PostgreSQL 16 +
Drizzle ORM**, pnpm monorepo with `apps/api`, `apps/admin` (Next.js), `apps/customer`
(Next.js), `apps/migrator` (ETL CLI), shared packages (`@erp/database`, `@erp/contracts`,
`@erp/config`). Multi-tenancy = **shared database + shared schema + `tenant_id` on every
business table + PostgreSQL Row-Level Security**. IDs = **UUID v7**; human document
numbers from a transactional `document_sequences` service. Money = `NUMERIC(20,4)`
handled via `decimal.js`, never IEEE floats. Posted journals are **immutable**;
corrections via reversal entries. ZATCA Phase II designed-in from the start.

Rationale for each: `ARCHITECTURE_DECISION_RECORDS.md`.

## 5. Scope

### In scope (this program)
- SaaS backend API (all modules listed in `DOMAIN_MODEL.md` §2).
- Migration engine SQL Server → PostgreSQL + reconciliation + rollback.
- Admin Panel (tenant operations + back-office) and Customer UI/portal skeleton.
- Legacy Desktop → API compatibility gateway (Desktop talks to API; **never** cloud → legacy DB).
- E-invoicing KSA (production-ready) and Egypt ETA (adapter design + stub).
- Vertical packs delivered AFTER the core: Restaurant POS, HR/Payroll, Installments &
  Contracting, Niche verticals (optics/tailoring/marina/fitment), Salla integration.

### Out of scope (this program)
- SaaS billing/metering of tenants (seats, plans) — noted as future ADR.
- Mobile apps (Android POS existed in legacy; only API readiness is provided).
- Full ETAb production certification (Egypt) — adapter + config only.
- Payroll tax localization per country beyond configurable components.

## 6. Delivery Strategy (23 phases)

Foundation (P01–P04) → Organization & Catalog (P05–P06) → Core ERP
(P07 Accounting → P08 Parties/AR-AP → P09 Inventory → P10 Sales → P11 Purchases
→ P12 Treasury) → Compliance & Insights (P13 E-Invoicing, P14 Reports) →
Migration & Legacy bridge (P15–P16) → Frontends (P17 Admin, P18 Customer) →
Vertical packs (P19–P22) → Hardening & Go-Live (P23).

Full roadmap with dependencies, complexity and risks: `MASTER_PROJECT_PLAN.md` §6–§9.
Each phase is an independent prompt: `docs/phases/PHASE_XX_PROMPT.md`.

## 7. What Must Survive the Migration (non-negotiable legacy coverage)

Every legacy capability documented in `LEGACY_DATABASE_ANALYSIS.md` §7 is either
(a) mapped into the new domain model, or (b) parked in `REQUIRES_CONFIRMATION.md`
with a proposed home — never silently dropped (project rule §24).

## 8. Definition of Success

- All 23 phases pass their Acceptance Criteria + Definition of Done.
- Trial balance of every migrated tenant matches the legacy TB to the last halala,
  or a written, approved variance note exists per account.
- Tenant isolation suite proves zero cross-tenant reads/writes.
- Posted journal cannot be mutated (DB-enforced), only reversed.
- A new tenant onboarding (without migration) completes in < 15 minutes.
- Legacy desktop can authenticate and push a sale through the compatibility API
  without touching the legacy database from the cloud.
