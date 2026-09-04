# AI_DEVELOPMENT_PROTOCOL

> Mandatory working rules for ANY AI model (or human) executing a phase. Read first.
> Violations are defects, even if the code "works".

## 1. Boot Sequence (do in order)

1. Read `docs/README.md` (map), this file, `docs/PROJECT_CONTRACT.md`.
2. Read your phase prompt fully — `docs/phases/PHASE_XX_PROMPT.md` — including
   DO NOT DO and DELIVERABLES.
3. Read the prompt's REQUIRED INPUT DOCUMENTS. Treat Level A/B docs as law.
4. Read `docs/STATUS.md` to confirm previous phases' real state (not assumptions).
5. Skim relevant `packages/*/src` + `apps/api/src/modules/*` that already exist.
6. Only then plan tasks; announce the plan; then implement.

## 2. Hard Rules

- **Scope discipline**: implement exactly the phase scope. "Can be done later" inside
  scope = defect. Out-of-scope temptation → change request, don't do it.
- **No memory reliance**: every fact you need must come from files. If a needed fact
  exists only in conversation history, STOP and write it into the proper doc first.
- **SSOT**: never contradict Level A/B documents. Conflicts → §7 change process.
- **No feature deletion**: existing behavior/endpoints/tables are never removed or
  renamed without an ADR (rule §24 of the brief applies to the new system too).
- **No new dependencies** without justification comment + entry in the phase report;
  prefer existing stack (frozen in TARGET_ARCHITECTURE §2).
- **No secrets in code/logs**; env only (`packages/config` schema).
- Conventions from `PROJECT_CONTRACT`: naming, money types, times, soft delete, RLS,
  permissions, error codes, envelope.
- Every new tenant-scoped table ships with its RLS policy + isolation tests.
- Every new endpoint lands in `API_CONTRACT.md` + OpenAPI set + integration tests.
- Every new table lands in `DATABASE_DESIGN.md` (+ Drizzle migration file).
- UI work extends the module's section in the relevant MASTER requirements doc.

## 3. Quality Bars (Definition of Done baseline, extended by each phase)

`npm run verify` green (typegen, tsc noEmit, lint use project's eslint config, unit,
integration, build) · tests added per `TESTING_STRATEGY.md` mandatory classes ·
docs updated · `docs/STATUS.md` entry added (what/why/files touched/tests) ·
phase `CHANGELOG` block written.

## 4. Coding Standards (condensed)

- TypeScript strict; no `any` (lint-enforced; `unknown` + narrowing where forced).
- Services pure-ish: dependencies injected; no cross-module imports except via
  public `index.ts` (eslint boundaries rule).
- Transactions: unit-of-work helper `withTx` from `@erp/database`; financial ops
  inside one tx (invoice→journal→ledger); never nest manual tx.
- Errors: throw `DomainError(code, detail?)` → mapped to problem+json; never leak
  stack into responses.
- Money: `decimal.js`; parse at boundaries; forbid `number` via lint.
- Dates: `DateTime` luxon or `date-fns-tz` for tenant-local business dates.
- Naming: contract §1 exactly; permission codes registered in one file per module.

## 5. Git & Files Discipline

- Conventional commits if committing (`feat(accounting): …`).
- Never edit `.next/`, `node_modules/`, lockfile by hand.
- Migrations: `pnpm db:generate` then review SQL before applying.
- Don't modify previous phases' completed modules except for integration points
  explicitly listed in your prompt; do it via their public APIs.

## 6. When You're Blocked

- Missing legacy fact needed for correctness → check `REQUIRES_CONFIRMATION.md`;
  if the answer isn't there and the item is 🔴, implement the safest default,
  mark the spot with `TODO(CONF:RC-xx)` comment, and log the assumption in STATUS.md.
- Flaky environment/build failing for unrelated reasons → report verbatim logs in
  your final summary; do not "fix" unrelated code to make it pass.

## 7. Change Process (repeat of README §4 — binding)

Stop affected slice → write `docs/change-log/CHANGE-REQUESTS.md` entry (topic,
evidence, proposal, impact) → non-structural fixes allowed immediately; structural
(table drop, DTO break, permission rename, convention change) need owner approval →
record ADR on approval → update affected canonical docs in the same change.

## 8. Final Report Format (every phase ends with)

```
## Phase Report — PH-nn
Delivered: [bullets]  | Deviations: [none/list + why]
Files: created/modified counts + notable paths
Tests: counts by class; verify output digest
Docs updated: [list]  | CRs opened: [ids]
Seeds/Env changes: [list]  | Follow-ups: [list]
```
