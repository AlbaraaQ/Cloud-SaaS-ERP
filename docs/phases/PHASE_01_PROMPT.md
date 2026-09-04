# PHASE_01_PROMPT — Repository & Engineering Standards Bootstrap

## 0. PROJECT CONTEXT (standalone)
You are building **Phase 01 of 23** of a cloud **multi-tenant SaaS ERP** replacing a
legacy C#/SQL Server desktop ERP. Stack (frozen): Node 22, TypeScript strict, pnpm
monorepo, NestJS API (built in P02), PostgreSQL 16 + Drizzle, Next.js 15 frontends
(later phases), BullMQ/Redis. Today you create NO application code — only the
repository skeleton, tooling and standards so that 22 subsequent phases slot in
without drift. Canonical documentation is already vendored in `docs/` — treat
`docs/PROJECT_CONTRACT.md` as law (UUID v7 ids, numeric(20,4) money + decimal.js,
tenant_id + RLS everywhere, naming rules, immutability rules).

## 1. CURRENT PHASE
- **#01 — Repository & Standards**. Position: foundation; everything depends on it.

## 2. REQUIRED INPUT DOCUMENTS (read in order)
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/TARGET_ARCHITECTURE.md` §3 (repo layout) 4. `docs/MASTER_PROJECT_PLAN.md` §6.

## 3. PREVIOUS PHASE OUTPUTS
None — first phase. `docs/` SSOT already exists at repo root.

## 4. GOAL & SCOPE
### In scope
- pnpm workspaces monorepo: `apps/{api,admin,customer,migrator}` placeholders,
  `packages/{database,contracts,config,testing}`, `infrastructure/`, `tests/`.
- Toolchain: tsconfig.base (strict), ESLint flat config (typescript-eslint,
  boundaries, no-float-money custom rules config, import order), Prettier, commitlint,
  husky/lint-staged (or plain scripts), `npm run verify` orchestrator script
  (typegen→tsc→lint→test placeholder→build).
- CI workflow skeleton (GitHub Actions or note for platform): install, verify, oasdiff placeholder.
- `docs/STATUS.md` initialized with Phase-01 entry format; `docs/change-log/` folder.
- `.env.example` files per app listing expected vars (no secrets).
- Architecture guardrails doc-page check: eslint boundaries mirroring module rules of
  TARGET_ARCHITECTURE §4.
### Out of scope (DO NOT DO)
- Any NestJS/Next.js runtime code (P02, P17, P18). Any DB schema. Docker files beyond
  a compose skeleton with postgres/redis/minio/mailhog services defined.

## 5. EXACT TASKS
1. Create workspace manifests & folder tree exactly per TARGET_ARCHITECTURE §3.
2. Configure TS/ESLint/Prettier; prove `pnpm -r lint` passes on empty shells.
3. Add custom lint guard: ban `number` for identifiers matching
   `/(price|amount|total|balance|cost|rate)/i` (project money rule) as ESLint rule config
   or a documented custom rule file in `packages/config/eslint-rules/`.
4. Create `packages/config` with zod env schema for shared vars (DATABASE_URL,
   REDIS_URL, JWT_*, S3_*, DATA_ENC_KEY placeholders) + `tenant-settings` typed key registry skeleton.
5. Create `packages/testing` skeleton exporting nothing yet but compiling.
6. `infrastructure/docker-compose.yml`: postgres:16, redis:7, minio, mailhog + volumes.
7. Scripts: root `package.json` — `dev`, `build`, `test`, `verify`, `db:generate`,
   `db:migrate`. Wire verify to run real commands but tolerate empty packages (echo steps ok).
8. Write `docs/STATUS.md` (columns: phase, date, state, notes) + PHASE.md conventions note.
9. Fact-check: no contradicting the frozen layout; ADR needed for any deviation (stop instead).

## 6. DATABASE IMPACT
None (no schema work). Do not create drizzle schema files.

## 7. API IMPACT
None. Leave `apps/api` as a documented placeholder (`README.md` inside + empty src/).

## 8. SECURITY REQUIREMENTS
- `.env*` ignored except `*.example`. gitleaks config present in CI skeleton.
- No credentials anywhere; compose uses local dev passwords only, documented as dev-only.

## 9. TESTING REQUIREMENTS
- `verify` runs successfully end-to-end (may be no-op tests).
- One smoke test proving workspace linking works (`packages/config` imported by a
  trivial check script in `apps/api` placeholder executed via `pnpm test:smoke`).

## 10. DOCUMENTATION REQUIREMENTS
- Update `docs/STATUS.md`. Ensure `docs/README.md` repo-layout section still matches
  reality (fix forward if layout improved; note under ADR only if structural).
- Admin/Customer UI masters: no change (nothing visual yet).

## 11. ACCEPTANCE CRITERIA
- Fresh clone → `pnpm install && pnpm verify` exits 0.
- Folder tree matches TARGET_ARCHITECTURE §3 (script or manual check listed in report).
- All contracts documented here exist; STATUS.md entry added.

## 12. DEFINITION OF DONE
Tasks complete · verify green · docs updated · final report per AI_DEVELOPMENT_PROTOCOL §8.

## 13. DELIVERABLES
Monorepo skeleton toolchain + compose + STATUS.md + lint guard for money rule.

## 14. DO NOT DO
Runtime code · schemas · UI · migrations · package upgrades beyond stated stack ·
docs rewrites (only STATUS + this phase's notes).
