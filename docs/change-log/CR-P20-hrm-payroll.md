# CR-P20 — HRM and payroll pack schema

Phase 20 adds the HRM vertical pack behind tenant feature flag `pack.hrm`.

Additive database changes:

- `departments`
- `jobs`
- `employees`
- `attendance_logs`
- `salary_adjustments`
- `payroll_runs`
- `payroll_run_lines`

The tables are tenant-scoped and protected by FORCE RLS. Payroll payment uses the already
existing treasury voucher subtype `salary`; payroll posting can link to the existing
journal engine without changing core accounting internals.
