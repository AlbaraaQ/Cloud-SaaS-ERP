# Phase 20 Implementation Report

## Current status

`COMPLETE` as of 2026-09-07.

## Delivered

- HRM/payroll backend pack gated by tenant setting `pack.hrm`.
- Seven tenant-scoped FORCE-RLS tables: `departments`, `jobs`, `employees`,
  `attendance_logs`, `salary_adjustments`, `payroll_runs`, and `payroll_run_lines`.
- Reversible migration `0016_hrm_payroll.sql` plus Drizzle schema exports.
- Employee directory with optional membership link, branch/department/job refs, salary
  components, cost-center/account refs, and masked bank fields on list responses.
- Attendance CSV importer for `machine,enroll,datetime,inout`, idempotent by punch
  fingerprint, plus naïve working-hours summary documented as no RC-10 rules engine.
- Salary adjustments with addition/deduction, recurring/one-off validity windows,
  `SubFromSalary` flag, approval endpoint, and next-run preview impact.
- Deterministic payroll calculator with unpaid-days factor and non-negative net clamp.
- Payroll lifecycle: preview, create run, read/list, post, pay through salary treasury
  voucher, reverse posted/paid runs, and payslip HTML payloads.
- Admin `/hrm` page covering employee directory, editor tabs, attendance import/log,
  adjustments, payroll wizard, post/pay/reverse, and payslip print affordance.
- API contract, database design, admin requirements, BL-10, permissions, and status docs
  updated.

## Verification

`pnpm run verify` passes with type generation, TypeScript, lint, admin/customer builds,
API/migrator/package tests, smoke checks, and OpenAPI export.

## Notes

Tax/social-security engines, leave management, performance modules, and live biometric
device communication remain out of scope. Attendance stores raw punches and exposes only
a simple pairing summary until RC-10 rules are finalized.
