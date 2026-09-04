# PHASE_20_PROMPT — HR & Payroll Pack (vertical)

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 20 of 23; flag `pack.hrm`. Legacy coverage:
`Employees` (profile + recurring components: basic/housing/food/travel/medical +
bank fields + `AccCode` link), lookups (Departments/Managements/jobs/Marital_status/
Religions), `EmpBranches`, `EmpSalaryAddSub` (additions/deductions incl SubFromSalary
with Cash/Bank + EntryGlobalId), `Salary_Res(_Details)`, `SalaryPay(_Sub)` monthly
runs with journal link, `SalaryAddSubTypes`, raw biometric `Attendance` +
`SettingFingurePrint` (device registry only — no live device comms v1).
Users↔employees linkable (legacy `Users.emp` NOT NULL — optional in new model, RC-30).

## 1. CURRENT PHASE
**#20 — HR & Payroll**: employees + attendance import + adjustments + payroll runs
posting journals (expense/payable/cash) through core services.

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/DATABASE_DESIGN.md` §15 (hr) 4. `docs/LEGACY_DATABASE_ANALYSIS.md` §6
5. `docs/LEGACY_BUSINESS_LOGIC.md` BL-10 6. `docs/ACCOUNTING_ARCHITECTURE.md` §4
7. `docs/ADMIN_PANEL_MASTER_REQUIREMENTS.md` §13 8. `docs/REQUIRES_CONFIRMATION.md`
   RC-10/30 9. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS
Treasury vouchers (subtype=salary exists), posting engine, files, admin shell.

## 4. GOAL & SCOPE
### In scope
- Tables+RLS: `departments, jobs, employees, attendance_logs, salary_adjustments,
  payroll_runs, payroll_run_lines` (+ lookups seeded: marital status; religions
  optional config).
- Employee management UI + employee↔membership optional link; per-branch visibility
  respects branch scope (legacy EmpBranches → membership branch_scope when linked).
- Attendance: CSV/device-file import endpoint + log grid; NO evaluation engine (RC-10)
  — expose `working_hours` computed summary endpoint with naïve in/out pairing
  documented.
- Adjustments: add/sub component with month validity, one-off vs recurring, effect on
  next run preview.
- Payroll run wizard: period pick → compute per employee (base + recurring comps +
  adjustments + attendance-derived unpaid-days factor option (config) → net) → review
  grid → post (balanced journal: salaries expense Dr per component mapping, payroll
  payable/cash Cr; per-employee lines carry cost_center if set) → payslip print data
  (HTML template minimal) → pay action creates salary voucher (P12) linked.
- Immutable posted runs; correction = reversal + fix + new run.
### Out of scope (DO NOT DO)
Tax/social-security localization engines (config fields only) · leave management ·
performance modules · live biometric device connections.

## 5. EXACT TASKS
1. Migrations+RLS+seeds (lookups, add/sub types mirroring SalaryAddSubTypes examples
   as configurable rows).
2. Services + endpoints (namespace `/hrm/…` appended to API_CONTRACT).
3. Attendance importer (CSV columns: machine,enroll,datetime,inout) + pairing summary.
4. Payroll calculator (deterministic unit/property tests incl. unpaid-days factor).
5. Run lifecycle endpoints + post + pay integration + reversal rule tests.
6. Admin UI: directory, employee editor tabs, attendance grid, adjustments, run
   wizard, payslip print.
7. E2E: employee → adjustment → run → post → pay → journal inspect.
8. STATUS.md; API_CONTRACT append; ADMIN master §13 markers; BL-10 implemented mark.

## 6. DATABASE IMPACT
+7 tables RLS; salary voucher link; journal links.

## 7. API IMPACT
Append `/hrm/*` section; perms `hrm.view/manage/payroll.post/adjust.approve` seeds.

## 8. SECURITY REQUIREMENTS
PII handling (bank accounts masked in lists); salary data restricted perms; audit all
adjustments & posts.

## 9. TESTING REQUIREMENTS
Calculator property tests (no negatives, component sums), lifecycle integration,
reversal behavior, isolation, e2e.

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md; module README (computation order rules); masters markers.

## 11. ACCEPTANCE CRITERIA
- Fixture month: 3 employees with components+adjustments → expected nets; posted
  journal balanced; payment voucher linked; reversal undoes cleanly.
- Attendance import dedupes re-imported identical punches (idempotency by fingerprint).

## 12. DEFINITION OF DONE
verify green · e2e · docs · protocol §8 report.

## 13. DELIVERABLES
HRM pack backend+UI + importer + calculator + docs.

## 14. DO NOT DO
Core postings changes · tax engines · live device sync · leave/loan systems.
