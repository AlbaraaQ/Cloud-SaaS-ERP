# MASTER_PROJECT_PLAN

> Level C — the program plan. Frozen where it overlaps PROJECT_CONTRACT/ADRs.

## 1. Vision → Scope → Strategy
See `PROJECT_OVERVIEW.md`. Strategy: SSOT docs → 23 independent, prompt-driven
phases → each phase self-verifying (`npm run verify` + phase DoD) → merge-safe
because every phase regenerates only its own module surface.

## 2. Technology & Architecture Strategy
`TARGET_ARCHITECTURE.md` (modular monolith; NestJS; Drizzle; Postgres RLS tenency;
BullMQ; Next.js frontends; repo monorepo). Rationale: ADR-001..020.

## 3. Database Strategy
`DATABASE_DESIGN.md`: UUID v7 PKs, tenant_id + RLS everywhere, numeric money,
immutable ledgers, sequences per branch/type, legacy_id everywhere migratable.

## 4. API / Security / Testing / Deployment Strategy
`API_ARCHITECTURE.md` + `API_CONTRACT.md` · `SECURITY_ARCHITECTURE.md` ·
`TESTING_STRATEGY.md` · Deployment: docker images per app; managed PG/Redis/S3;
CI gates (lint+type+unit+integration+oasdiff); environments dev/stage/prod;
P23 adds observability, backups drill, rollout plan.

## 5. Documentation Strategy
Docs-as-code in `docs/` (this set). Phase outputs update docs in the same change.
`docs/STATUS.md` = living ledger (created P01). Change requests folder per README §4.

## 6. Phase Roadmap (order is dependency-driven and frozen by ADR-020)

| Ph | Name | Objective | Depends | Main Deliverables | Complexity | Top Risks |
|---|---|---|---|---|---|---|
| 01 | Repo & Standards Bootstrap | monorepo skeleton, tooling, docs vendored | — | workspaces, lint/format/tsconfig, CI stub, STATUS.md | L | toolchain drift |
| 02 | Backend Platform Core | runnable NestJS + PG + Drizzle + logging/errors/health | 01 | apps/api boot, config pkg, problem+json, migrations pipeline | M | pool/tx plumbing |
| 03 | Tenancy & Identity | tenants/users/memberships/RBAC/auth + RLS + isolation harness | 02 | auth flows, guards, permission registry, audit columns base | H | RLS edge cases |
| 04 | Platform Services | audit log, files/S3, notifications, BullMQ jobs, sequences, idempotency, settings | 03 | services + worker | M | tx-coupled outbox |
| 05 | Organization | company, branches, warehouses, cash locations, currencies/fx, price lists, posting profiles | 04 | org module + seeds | M | profile resolution rules |
| 06 | Catalog | categories, items, units/barcodes, tax groups, components, price history | 05 | catalog module, CSV import | M | legacy unit-ratio quirks |
| 07 | Accounting Core | COA, fiscal years/periods/locks, journals + posting engine, statements, invariant suite | 05 | accounting module, T1–T10 green | H | balance correctness |
| 08 | Parties & AR/AP | parties/contacts, balances, allocations | 07 | parties module | M | credit-limit policy |
| 09 | Inventory | ledger, avg-cost valuation, balances, adjustments, transfers, lots/serials | 06,07 | inventory module | H | avg-cost edge cases |
| 10 | Sales Invoicing | invoices/returns/notes, payments, offers-lite, ZATCA fields | 08,09 | sales module | H | total formula parity (RC-07) |
| 11 | Purchases | invoices/returns, additional costs, landed cost | 10 | purchases module | M | cost allocation math |
| 12 | Treasury | vouchers, cheques, transfers, shift close, expense types | 08 | treasury module | H | Sand*/Receipts unification (RC-19) |
| 13 | E-Invoicing KSA (+ETA stub) | credentials(vaulted), signing, submission queue, status | 10 | einvoicing module | H | ZATCA cert ops |
| 14 | Reporting | report catalog, statements, agings, exports async | 07,09,10,12 | reporting module + mv strategy proof | M | perf budgets |
| 15 | Migration Engine | migrator CLI, registry, dry-run/import/reconcile/rollback | 10,12 | apps/migrator + fixtures + reconciliation | H | dirty legacy data |
| 16 | Legacy Compat Gateway | device auth, master deltas, doc push, cursors | 10,12,15(design) | /compat/* endpoints | M | dedupe/idempotency |
| 17 | Admin Panel | full admin per master requirements | 14 (APIs 03–12) | apps/admin complete for core | H | scope control |
| 18 | Customer UI | portal + marketing + verification | 17(shared) or 14 | apps/customer | M | perms surfacing |
| 19 | Restaurant POS Pack | tables, order types, kitchen print config, POS flows | 10,12,17 | pos pack + admin screens | M | legacy order lifecycle gaps |
| 20 | HR & Payroll Pack | employees, attendance log, adjustments, payroll runs | 12,17 | hrm pack | M | salary formula variants |
| 21 | Installments & Contracting | schedules + progress billing w/ retention, projects/stages | 10,12,17 | installments + projects packs | H | retention accounting rules |
| 22 | Niche & Integrations Pack | optics/tailoring/marina/fitment + Salla | 06,10,17 | niche modules + integration | H | breadth vs depth |
| 23 | Hardening & Go-Live | perf, backups/restore drill, monitoring, security review, UAT, launch | all | ops runbooks, dashboards, release | H | unknown unknowns |

## 7. Dependency Graph (text)

01→02→03→04→05→{06,07}→{08,09}→10→{11,12}→{13,14}→15→16→17→18→{19,20,21,22}→23.
(17 needs APIs of 03–12 & 14; 19–22 need 10/12/17; 15 needs 10 & 12 schemas.)

## 8. Acceptance Criteria (program level)

PROJECT_OVERVIEW §8 holds the measurable definition of success; each phase prompt
carries its own AC + DoD. Program gate: P23 sign-off checklist completed.

## 9. Risk Register (top, ranked)

| Risk | Impact | Mitigation |
|---|---|---|
| Ambiguous legacy enums (RC-01..) block faithful migration | rework of P10/12/15 | analyze-mode deliverable early (P15 starts with analyze; owner answers RC-xx before P15 import) |
| Float rounding variance vs legacy TB | reconciliation fails | tolerance policy RC-31 + variance reports; ADR-required waivers |
| Scope creep inside huge admin phase | delays | PER-BASELINE module sections; CR process |
| Avg-cost edge cases (returns at zero qty, negative stock) | wrong COGS/BS | invariant tests + legacy parity fixtures (BL-3 formulae) |
| RLS misconfiguration leaks data | breach | harness per phase + security review P23 |
| ZATCA sandbox cert delays | blocked e-invoice tests | early sandbox onboarding in P10/P13 |
| Vertical packs underestimated | P19–22 slip | prompts bounded; packs gated per tenant flags |
| Legacy desktop teams push conflicting changes during migration | dirty delta | cutover runbook: freeze window + final delta import (P15) |

## 10. Registers Required by the Brief

### 10.1 Decisions I took (no approval needed) — see ADRs 000–020
Node/NestJS/Drizzle/Postgres · modular monolith · shared-DB+RLS · UUIDv7+sequences ·
numeric money + decimal.js · immutable ledgers + reversal-only · unified parties/
vouchers/cash locations · object-storage files · masters consolidated (ADR-016) ·
23 phases & ordering (ADR-020) · docs in EN (ADR-015).

### 10.2 Decisions needing YOUR approval
1. ADR-019: launch without tenant billing/metering. 2. `allow_negative` stock default
   = false (legacy `SaleByMinus` becomes per-tenant setting). 3. Rounding tolerance
   policy for migration waivers (RC-31). 4. Customer-facing portal scope (CLIENT portal
   access) in P18 — enable/disable per tenant. 5. Dropping `salesmen` as separate
   table in favor of employees-flag (ADR-009 open part, defaults to keeping it).
6. Egypt ETA investment (adapter-only now vs certification path).

### 10.3 Questions you must answer
All 🔴 rows of `REQUIRES_CONFIRMATION.md` — minimally RC-01, RC-02, RC-03, RC-05,
RC-07, RC-08, RC-19, RC-27, RC-28, RC-31 (evidence: legacy data extracts/C# code refs).

### 10.4 Things we must NOT change from legacy without study
Invoice numbering continuity per branch/type · avg-cost valuation formula & discount
allocation (BL-3) · shift-close totals taxonomy · posting-profile mapping semantics
(SettingGeneral *Acc) · party↔COA account linkage · cheque lifecycle states ·
period/module locking model · ZATCA UUID/hash/QR persistence · legacy GlobalID lookups
(paper trail) · price-includes-VAT behavior per invoice type.

### 10.5 Files that must exist before Phase 1
All 21 canonical docs in `docs/` (done in this planning turn) + `docs/phases/*.md`
(23, done) + owner answers to 🔴 RC items can arrive later (except they gate P15
import and P10 formula parity).

## 11. Execution Notes for the Owner
Run phases strictly in order unless the graph allows parallel (17/18 after their API
deps; 19–22 parallel-safe). Paste each PHASE prompt into a fresh AI conversation
exactly as-is. Require the Final Report format. Review CRs between phases.
Store backups of docs/ before each phase start if running unattended.
