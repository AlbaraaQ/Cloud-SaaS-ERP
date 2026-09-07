import { sql } from 'drizzle-orm';
import { boolean, date, index, integer, jsonb, numeric, pgTable, primaryKey, text, timestamp, uniqueIndex, uuid } from 'drizzle-orm/pg-core';

import { baseAuditColumns, baseLegacyColumns, baseSoftDeleteColumns } from '../columns.js';

import { accounts, costCenters, fiscalPeriods, journalEntries } from './accounting.js';
import { branches, cashLocations } from './organization.js';
import { memberships } from './tenancy.js';
import { vouchers } from './treasury.js';
import { tenants } from './platform.js';

const cashValue = { precision: 20, scale: 4, mode: 'string' as const };

export const departments = pgTable('departments', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), branchId: uuid('branch_id').references(() => branches.id, { onDelete: 'restrict' }), code: text('code').notNull(), name: text('name').notNull(), ...baseAuditColumns(), ...baseSoftDeleteColumns(), ...baseLegacyColumns(),
}, (table) => ({ codeKey: uniqueIndex('departments_tenant_code_key').on(table.tenantId, table.code).where(sql`deleted_at IS NULL`), branchIdx: index('departments_branch_idx').on(table.tenantId, table.branchId) }));

export const jobs = pgTable('jobs', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), code: text('code').notNull(), name: text('name').notNull(), ...baseAuditColumns(), ...baseSoftDeleteColumns(), ...baseLegacyColumns(),
}, (table) => ({ codeKey: uniqueIndex('jobs_tenant_code_key').on(table.tenantId, table.code).where(sql`deleted_at IS NULL`) }));

export const employees = pgTable('employees', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), employeeNo: text('employee_no').notNull(), name: text('name').notNull(), branchId: uuid('branch_id').references(() => branches.id, { onDelete: 'restrict' }), departmentId: uuid('department_id').references(() => departments.id, { onDelete: 'set null' }), jobId: uuid('job_id').references(() => jobs.id, { onDelete: 'set null' }), membershipId: uuid('membership_id').references(() => memberships.id, { onDelete: 'set null' }), status: text('status').notNull().default('active'), hireDate: date('hire_date'), salaryComponents: jsonb('salary_components').$type<Record<string, string>>().notNull().default({}), bank: jsonb('bank').$type<Record<string, string | undefined>>().notNull().default({}), salaryExpenseAccountId: uuid('salary_expense_account_id').references(() => accounts.id, { onDelete: 'restrict' }), salaryPayableAccountId: uuid('salary_payable_account_id').references(() => accounts.id, { onDelete: 'restrict' }), costCenterId: uuid('cost_center_id').references(() => costCenters.id, { onDelete: 'set null' }), ...baseAuditColumns(), ...baseSoftDeleteColumns(), ...baseLegacyColumns(),
}, (table) => ({ noKey: uniqueIndex('employees_tenant_no_key').on(table.tenantId, table.employeeNo).where(sql`deleted_at IS NULL`), branchIdx: index('employees_branch_idx').on(table.tenantId, table.branchId), memberIdx: index('employees_membership_idx').on(table.tenantId, table.membershipId) }));

export const attendanceLogs = pgTable('attendance_logs', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), employeeId: uuid('employee_id').references(() => employees.id, { onDelete: 'cascade' }), machine: text('machine').notNull(), enroll: text('enroll').notNull(), punchAt: timestamp('punch_at', { withTimezone: true }).notNull(), direction: text('direction').notNull(), fingerprint: text('fingerprint').notNull(), payload: jsonb('payload').$type<Record<string, unknown>>().notNull().default({}), createdAt: timestamp('created_at', { withTimezone: true }).notNull().defaultNow(),
}, (table) => ({ fingerprintKey: uniqueIndex('attendance_logs_fingerprint_key').on(table.tenantId, table.fingerprint), employeeIdx: index('attendance_logs_employee_idx').on(table.tenantId, table.employeeId, table.punchAt), enrollIdx: index('attendance_logs_enroll_idx').on(table.tenantId, table.enroll, table.punchAt) }));

export const salaryAdjustments = pgTable('salary_adjustments', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), employeeId: uuid('employee_id').notNull().references(() => employees.id, { onDelete: 'cascade' }), kind: text('kind').notNull(), componentCode: text('component_code').notNull(), valueText: numeric('value_text', cashValue).notNull(), startsOn: date('starts_on').notNull(), endsOn: date('ends_on'), recurring: boolean('recurring').notNull().default(false), subFromSalary: boolean('sub_from_salary').notNull().default(false), status: text('status').notNull().default('draft'), cashLocationId: uuid('cash_location_id').references(() => cashLocations.id, { onDelete: 'set null' }), journalEntryId: uuid('journal_entry_id').references(() => journalEntries.id, { onDelete: 'set null' }), reason: text('reason'), ...baseAuditColumns(), ...baseLegacyColumns(),
}, (table) => ({ employeeIdx: index('salary_adjustments_employee_idx').on(table.tenantId, table.employeeId, table.startsOn), statusIdx: index('salary_adjustments_status_idx').on(table.tenantId, table.status) }));

export const payrollRuns = pgTable('payroll_runs', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), periodId: uuid('period_id').references(() => fiscalPeriods.id, { onDelete: 'restrict' }), yearMonth: text('year_month').notNull(), status: text('status').notNull().default('draft'), currency: text('currency').notNull().default('SAR'), summary: jsonb('summary').$type<Record<string, unknown>>().notNull().default({}), journalEntryId: uuid('journal_entry_id').references(() => journalEntries.id, { onDelete: 'set null' }), voucherId: uuid('voucher_id').references(() => vouchers.id, { onDelete: 'set null' }), postedAt: timestamp('posted_at', { withTimezone: true }), paidAt: timestamp('paid_at', { withTimezone: true }), reversedAt: timestamp('reversed_at', { withTimezone: true }), reversalReason: text('reversal_reason'), ...baseAuditColumns(), ...baseLegacyColumns(),
}, (table) => ({ monthKey: uniqueIndex('payroll_runs_month_key').on(table.tenantId, table.yearMonth).where(sql`status <> 'reversed'`), statusIdx: index('payroll_runs_status_idx').on(table.tenantId, table.status) }));

export const payrollRunLines = pgTable('payroll_run_lines', {
  runId: uuid('run_id').notNull().references(() => payrollRuns.id, { onDelete: 'cascade' }), lineNo: integer('line_no').notNull(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), employeeId: uuid('employee_id').notNull().references(() => employees.id, { onDelete: 'restrict' }), components: jsonb('components').$type<Record<string, string>>().notNull().default({}), additions: numeric('additions', cashValue).notNull().default('0'), deductions: numeric('deductions', cashValue).notNull().default('0'), gross: numeric('gross', cashValue).notNull(), net: numeric('net', cashValue).notNull(), costCenterId: uuid('cost_center_id').references(() => costCenters.id, { onDelete: 'set null' }), status: text('status').notNull().default('draft'), payslipHtml: text('payslip_html'),
}, (table) => ({ pk: primaryKey({ columns: [table.runId, table.lineNo] }), employeeIdx: index('payroll_run_lines_employee_idx').on(table.tenantId, table.employeeId) }));

export type Employee = typeof employees.$inferSelect;
export type PayrollRun = typeof payrollRuns.$inferSelect;
