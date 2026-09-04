# PHASE_17_PROMPT — Admin Panel (Next.js)

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 17 of 23. The API for core modules (P03–P14) exists
and is contract-stable (`docs/API_CONTRACT.md` + `packages/contracts/openapi.json`).
You now build `apps/admin` — the back-office + tenant operations UI. Design system:
Next.js 15 App Router, Tailwind + shadcn/ui, TanStack Query, react-hook-form + zod
(shared schemas from `@erp/contracts`), RTL-first Arabic default with ar/en switch,
tenant-local timezone & currency formatting. Every requirement lives in
`docs/ADMIN_PANEL_MASTER_REQUIREMENTS.md` — treat it as the product spec.

## 1. CURRENT PHASE
**#17 — Admin Panel**: complete UI for: platform/settings, organization, catalog,
accounting, parties, inventory, sales, purchases, treasury, einvoicing console,
reporting center, migration console (P15 APIs).

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/ADMIN_PANEL_MASTER_REQUIREMENTS.md` (ALL — primary spec)
4. `docs/API_CONTRACT.md` (ALL) 5. `docs/SECURITY_ARCHITECTURE.md` §5 (hide vs disable)
6. `docs/DOMAIN_MODEL.md` §5 state machines (UI badges mirror these names)
7. `docs/TESTING_STRATEGY.md` §8 8. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS (verify; else stop)
API live with OpenAPI; seeded demo tenant fixtures runnable locally (P03–P15).

## 4. GOAL & SCOPE
### In scope (map 1:1 to master req sections)
Shell (§0) · sections 1–12 screens as specified · dashboard KPIs using reporting
endpoints · print via browser print CSS + API PDF artifacts · branch switcher,
permission-driven hiding, all empty/loading/error/forbidden states · column chooser
persistence per user (localStorage + server setting when available).
### Out of scope (DO NOT DO)
Customer app (P18) · vertical pack screens (P19–22 add their own) · new API endpoints
(missing capability = CR against API_CONTRACT, not local workaround) · theming beyond
tokens defined · data grids virtualization beyond 500 rows/pages (documented limit).

## 5. EXACT TASKS
1. App bootstrap: auth flow (login/refresh via interceptors, forced-reset handling),
   tenant/branch context, i18n ar/en (RTL verified), theme tokens.
2. Kits: DataTable (server side), Form, StatusBadge set (state machine names), KpiCard,
   EmptyState/ErrorState/Skeleton, ConfirmDialog w/ diff display for void/reverse,
   MoneyInput & QtyInput using decimal.js strings, FilterBar, ReportRunner.
3. Module sections per master §1–§12 in order; each with routes, pages, forms,
   tables, actions, wiring to endpoints from API_CONTRACT only.
4. Dashboard home §0 + per-module widgets listed.
5. Migration console: run wizard, progress, issues grid, reconciliation viewer,
   legacy ID lookup, compat devices.
6. E2E (Playwright): login→create customer→create item→post sale→pay→shift close→
   TB opens (per TESTING §8), one per critical flow group max, headless CI.
7. Accessibility pass (focus order, aria for dialogs, keyboard shortcuts for save/post).
8. STATUS.md + update master req file with 🟢/⚪ coverage markers per section row.

## 6. DATABASE IMPACT
None (API consumption only).

## 7. API IMPACT
None — consumption only; discrepancies → change requests against backend docs.

## 8. SECURITY REQUIREMENTS
Tokens in memory + refresh cookie httpOnly if API provided (else Authorization header
pattern per security doc — frozen); no secrets in client bundle; 401/403 flows;
permission-based hiding; CSP headers via Next config.

## 9. TESTING REQUIREMENTS
Component tests for kits (MoneyInput rounding, DataTable pager), route-level tests
with MSW for forms (invalid + conflict + idempotent replay UX), Playwright happy path.

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md; `apps/admin/README.md` (runbook + structure); coverage markers in master file.

## 11. ACCEPTANCE CRITERIA
- All §1–§12 sections navigable and functional against local API; states present;
  RTL/LTR switch correct; posted docs render read-only with explain tooltips.
- E2E happy path green; typecheck/lint/build green.

## 12. DEFINITION OF DONE
verify green incl. e2e · coverage markers updated · docs · protocol §8 report.

## 13. DELIVERABLES
`apps/admin` complete for core modules + kits + e2e + docs updates.

## 14. DO NOT DO
Backend changes (CR instead) · customer app · vertical modules UI · new deps without
justification · inline money math in components (use shared calculators).
