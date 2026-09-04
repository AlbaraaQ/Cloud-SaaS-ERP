# PHASE_21_PROMPT — Installments & Contracting (Projects) Packs (vertical)

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 21 of 23; flags `pack.installments`,
`pack.projects`. Legacy coverage (must not shrink): installments `cont` +
`cont_installments` (contract: total, down payment installment val, count, period,
first_date, paid, linked treasury sand_id); contracting `PM_Projects`,
`PM_GroupStages`, `PM_ProjStages` (accreditation workflow), `PM_Stages`, `PM_Status`,
`PM_Terms` (BOQ items with price/cost/ExecutionPeriod), `PM_ContractInv(Sub)`
(progress billing w/ `RestractionPk` retention), `PM_Contractor` banking,
`PM_Requirement(Sub)` opportunities; `InvContratct(_Sub)` contract invoices carrying
retention `work_guarantee`, `previously_paid_amount`, `remaining_contract_balance`,
work/awarded amounts.

## 1. CURRENT PHASE
**#21 — Installments + Contracting**: schedule-driven receivables and stage-based
project billing with retention, built strictly on core parties/invoices/vouchers.

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/DATABASE_DESIGN.md` §15 (installments/projects)
4. `docs/LEGACY_DATABASE_ANALYSIS.md` §7 (PM rows) + `InvContratct` section
5. `docs/LEGACY_BUSINESS_LOGIC.md` BL-12 6. `docs/ACCOUNTING_ARCHITECTURE.md` §4
   (retention/advances slots) 7. `docs/ADMIN_PANEL_MASTER_REQUIREMENTS.md` §14
8. `docs/REQUIRES_CONFIRMATION.md` RC-24/25. 9. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS
Sales invoices, treasury vouchers + allocations, parties, admin shell, reporting.

## 4. GOAL & SCOPE
### In scope
- Tables+RLS: `installment_contracts, installment_schedule, projects,
  project_stage_templates, project_stages, boq_terms, progress_bills,
  progress_bill_lines, project_requirements`.
- Installments: contract create (from posted sale link or standalone party+item),
  schedule generation (equal|custom template; period unit days|months config —
  RC-25 default months), status; collection action allocates voucher to contract
  installment rows (priority oldest); overdue view; early-settlement discount rule
  (config).
- Projects: project + stages from template; stage accreditation (per-user); BOQ terms
  CRUD; **progress bill** editor: % or value of term, computes cumulative previously
  paid, retention deduction (profile slot `retention_receivable`), net due this bill
  (mirrors legacy column semantics), posts a real `sales_invoices` (kind sale,
  source `progress_bill`) with zero inventory effect (service-type) OR direct journal
  via engine when `post_cogs=false` — choose invoice path, document; retention release
  action (invoice against retention slot).
- Admin UI: contracts list/detail with schedule grid + collect dialog; projects list,
  kanban-by-stage, bill editor with all computed fields + audit trail, requirement
  register lightweight.
### Out of scope (DO NOT DO)
Interest usury engines beyond flat schedule config (RC-25 note) · subcontractor
payments full cycle (AP via parties only) · Gantt/scheduling tools · document storage
beyond files attach.

## 5. EXACT TASKS
1. Migrations+RLS; contract sequencing `installment_contract`, `progress_bill`.
2. Installment calculator + schedule generator (property tests sum == contract total).
3. Collection integration + overdue queries + reports hook (aging source flag).
4. Projects services: templates→stages copy, accreditation endpoints, BOQ CRUD.
5. Progress bill computations + invoice composition + retention accounting tests
   (balanced journal incl. retention slot).
6. Admin UI per master §14 rows.
7. E2E: contract→schedule→collect two installments; project→stage accredit→bill 40%→
   retention shown→release.
8. STATUS.md; API_CONTRACT append (`/installments/*`, `/projects/*`); masters markers;
   BL-12 implemented.

## 6. DATABASE IMPACT
+8 tables RLS; links to invoices/vouchers via ids (service-validated per domain rule).

## 7. API IMPACT
Append sections; perms `installments.view/manage/collect`, `projects.view/manage/
bill.post/stage.accredit` seeds.

## 8. SECURITY REQUIREMENTS
Bill post restricted; retention release restricted; computations server-authoritative.

## 9. TESTING REQUIREMENTS
Property math (schedule sums, cumulative billing never exceeds contract/term totals),
journal balance tests, lifecycle e2e, isolation.

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md; module READMEs incl. retention accounting pattern; API_CONTRACT append.

## 11. ACCEPTANCE CRITERIA
- E2E flows green; legacy `InvContratct` semantics reproduced on fixture numbers
  (work_guarantee/previous/remaining exactly per fixture).

## 12. DEFINITION OF DONE
verify green · e2e · docs · protocol §8 report.

## 13. DELIVERABLES
Two packs backend+UI + calculators + docs.

## 14. DO NOT DO
New invoice kinds beyond configured sale usage · inventory effects from bills ·
interest engines · core edits.
