# Documentation Map — ERP SaaS (Cloud Migration of Legacy "Data16")

This folder is the **Single Source of Truth (SSOT)** for the entire project.
Every implementation phase (executed in a separate, independent AI conversation) reads
these documents and MUST NOT contradict them.

> Task language: analysis notes are written in English for tooling consistency.
> Business terms keep their Arabic equivalents in parentheses where clarity matters.
> Reading time: full set ≈ 3–4 hours. Phase executors only need the files listed in
> their phase prompt under `REQUIRED INPUT DOCUMENTS`.

---

## 1. Documentation Structure

```
docs/
├── README.md                            ← You are here (map + reading order)
├── PROJECT_OVERVIEW.md                  ← Vision, goals, summary of the whole program
├── PROJECT_CONTRACT.md                  ← NORMATIVE. Naming, conventions, frozen decisions
├── MASTER_PROJECT_PLAN.md               ← Phases, roadmap, risks, decision registers
├── REQUIRES_CONFIRMATION.md             ← Open questions registry (legacy unknowns)
│
├── LEGACY_DATABASE_ANALYSIS.md          ← Table-by-table analysis of legacy SQL Server DB
├── LEGACY_BUSINESS_LOGIC.md             ← Inferred business rules from schema/SPs/functions
│
├── TARGET_ARCHITECTURE.md               ← Modular monolith, stack, module map
├── MULTI_TENANCY.md                     ← Tenant isolation strategy (shared DB + RLS)
├── DATABASE_DESIGN.md                   ← New PostgreSQL schema contract (table-by-table)
├── DOMAIN_MODEL.md                      ← Domains, aggregates, invariants, state machines
├── ACCOUNTING_ARCHITECTURE.md           ← COA, journal engine, posting rules, periods
├── API_ARCHITECTURE.md                  ← REST design, versioning, conventions deep-dive
├── API_CONTRACT.md                      ← NORMATIVE. Endpoint index, DTO & error formats
├── SECURITY_ARCHITECTURE.md             ← AuthN/AuthZ, RBAC, secrets, hardening
├── MIGRATION_ARCHITECTURE.md            ← SQL Server → PostgreSQL ETL engine design
├── TESTING_STRATEGY.md                  ← Test pyramid + mandatory test classes per phase
│
├── ADMIN_PANEL_MASTER_REQUIREMENTS.md   ← Aggregated UI requirements for the Admin Panel
├── CUSTOMER_UI_MASTER_REQUIREMENTS.md   ← Aggregated UI requirements for Customer UI
│
├── AI_DEVELOPMENT_PROTOCOL.md           ← Mandatory working rules for any AI executor
├── ARCHITECTURE_DECISION_RECORDS.md     ← ADR log (why each key decision was taken)
│
└── phases/
    ├── PHASE_01_PROMPT.md … PHASE_23_PROMPT.md   ← Self-contained executor prompts
```

**23 implementation phases**, each executable in a fresh AI conversation with zero
memory of previous work. Phase prompts are complete: they embed all context the
executor needs and reference the canonical docs above.

---

## 2. Mandatory Reading Order (for any AI executor)

1. `AI_DEVELOPMENT_PROTOCOL.md` — how to work on this project (NON-NEGOTIABLE).
2. `PROJECT_CONTRACT.md` — naming, conventions, frozen decisions.
3. The docs listed in your phase prompt under `REQUIRED INPUT DOCUMENTS`.
4. `DATABASE_DESIGN.md` — always, before touching any table.

---

## 3. File Authority Levels

| Level | Files | Rule |
|---|---|---|
| **Frozen (Level A)** | `PROJECT_CONTRACT.md`, `ARCHITECTURE_DECISION_RECORDS.md` | Change requires explicit owner approval. Log as ADR. |
| **Canonical (Level B)** | `DATABASE_DESIGN.md`, `DOMAIN_MODEL.md`, `API_CONTRACT.md`, `SECURITY_ARCHITECTURE.md`, `MULTI_TENANCY.md`, `ACCOUNTING_ARCHITECTURE.md`, `MIGRATION_ARCHITECTURE.md` | Change allowed only through the Change Process (see §4). |
| **Reference (Level C)** | Legacy analysis, testing strategy, UI master requirements, overview, plan | Update freely when reality changes; keep consistent. |
| **Executable (Level D)** | `phases/PHASE_XX_PROMPT.md` | Immutable once its phase starts. |

---

## 4. Change Process ( summarizes AI_DEVELOPMENT_PROTOCOL §7 )

If an executor discovers that a canonical document is wrong or incomplete:

1. **STOP** work on the affected slice only (continue unaffected work).
2. Append the finding to `docs/change-log/CHANGE-REQUESTS.md` (create folder if missing)
   with: topic, evidence, proposed change, impact radius.
3. Implement the fix **only if** the change is non-structural (typo, missing index,
   missing nullable rule). Structural changes (table removal, type change, removed
   endpoint, changed permission code) require owner approval.
4. Update the affected canonical doc + add an ADR entry for Level A/B changes.
5. Re-run the validation commands listed in the phase prompt.

---

## 5. Status Badges Used In Analysis Docs

| Badge | Meaning |
|---|---|
| `CONFIRMED` | Proven directly from schema / SQL code. |
| `INFERRED` | Strongly suggested by evidence, not proven. Rationale given. |
| `UNKNOWN` | Cannot determine from available artifacts. |
| `REQUIRES_CONFIRMATION` | Must be confirmed by the product owner; tracked in `REQUIRES_CONFIRMATION.md`. |

Every claim in the legacy analysis carries one of these badges. Guessing is forbidden
(project rule §23 of the original brief).
