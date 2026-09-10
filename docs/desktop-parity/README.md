# Desktop parity programme — من الدسكتوب إلى السحابة

The desktop product (`Desktop_ERP/`, SmartAuditERP — WPF + SQL Server, ~39K lines of
business logic in `Class/`, 273 screens in `Form_WPF/`, 95 DevExpress `.repx` reports)
is the functional specification. The cloud (this repo) is a clean-room rebuild:
**same behaviours and Arabic data, new architecture** (NestJS API + Next.js surfaces +
Postgres, multi-tenant, RBAC).

## Rules of the programme

1. Desktop code is **read for rules, never copied for architecture** — no `SqlClient`,
   no `MessageBox` in services, no static globals. Every ported rule lands behind a
   real API endpoint with tests.
2. Arabic names/codes stay verbatim (chart of accounts, invoice-type labels, units).
3. No mock UIs: a screen ships only when its endpoint exists; otherwise `status: 'api'`
   in `apps/staff/lib/navigation.ts`.
4. API compatibility is kept — porting adds endpoints/fields, never renames existing ones.
5. Each phase ends with: code + specs green + this README's status table updated.

## Status

| Phase | Scope | Status | Doc |
|---|---|---|---|
| 00 | Survey: architecture map, invoice matrix, file index | ✅ done | `PHASE_00_SURVEY.md` |
| 01 | Staff users/permissions UI + navigation dedup | ✅ done | `PHASE_01_USERS_NAV.md` |
| 02 | Sales invoice engine (calc, save, post, returns, notes) | ⬜ next | `ROADMAP_PHASES_02_11.md` |
| 03 | Purchase engine (invoices, returns, landed cost) | ⬜ | `ROADMAP_PHASES_02_11.md` |
| 04 | POS + shifts + cashier close | ⬜ | `ROADMAP_PHASES_02_11.md` |
| 05 | Inventory (stock in/out, transfers, serials, barcode, expiry) | ⬜ | `ROADMAP_PHASES_02_11.md` |
| 06 | Treasury (receipts, payments, safes/banks, cheques) | ⬜ | `ROADMAP_PHASES_02_11.md` |
| 07 | Accounting (manual entries, periods, trial balance, cost centres) | ⬜ | `ROADMAP_PHASES_02_11.md` |
| 08 | HRM (employees, attendance, payroll, custody) | ⬜ | `ROADMAP_PHASES_02_11.md` |
| 09 | Verticals (contracting/projects, marina, optics, tailoring, Salla…) | ⬜ | `ROADMAP_PHASES_02_11.md` |
| 10 | Reports (95 `.repx` → cloud report engine) | ⬜ | `ROADMAP_PHASES_02_11.md` |
| 11 | Zatca e-invoicing + ETA + integrations hardening | ⬜ | `ROADMAP_PHASES_02_11.md` |

Deferred by the owner: marketing CMS, per-tenant mobile-shop module.
Done earlier, outside this programme: RBAC reorganisation (PR #4), desktop chart
seeding (112 accounts + COGS extension, `packages/database/src/desktop-coa.ts`).
