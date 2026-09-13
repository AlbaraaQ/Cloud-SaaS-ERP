import { createHash } from 'node:crypto';

import { Inject, Injectable } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { alias } from 'drizzle-orm/pg-core';
import { and, desc, eq, gte, ilike, isNull, lte, or, sql } from 'drizzle-orm';
import { DomainError, newId } from '@erp/contracts';
import { accounts, attendanceLogs, departments, employees, jobs, payrollRunLines, payrollRuns, salaryAdjustments, tenantSettings, withTenantTx, type DatabaseHandle, type Employee } from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';
import { AccountingService, defaultNormalBalance, type AccountType, type JournalLineInput } from '../accounting/accounting.service.js';
import { TreasuryService } from '../treasury/treasury.service.js';

import { calculatePayrollLine, monthEnd } from './payroll-calculator.js';

export type DepartmentInput = { code: string; name: string; branchId?: string; parentId?: string | null };
export type JobInput = { code: string; name: string };
export type EmployeeInput = {
  employeeNo: string;
  name: string;
  branchId?: string | null;
  departmentId?: string | null;
  jobId?: string | null;
  membershipId?: string | null;
  hireDate?: string | null;
  status?: EmployeeStatus;
  /** 👤 بيانات الموظف الأساسية — `frmEmployees.xaml` «بيانات الموظف الأساسية». */
  birthDate?: string | null;
  insuranceNo?: string | null;
  nationalId?: string | null;
  maritalStatus?: string | null;
  nationality?: string | null;
  gender?: EmployeeGender | null;
  phone?: string | null;
  mobile?: string | null;
  email?: string | null;
  /** 👤 البيانات التكميلية — `frmEmployees.xaml` «البيانات التكميلية». */
  address?: string | null;
  notes?: string | null;
  salaryComponents?: Record<string, string>;
  bank?: Record<string, string | undefined>;
  salaryExpenseAccountId?: string | null;
  salaryPayableAccountId?: string | null;
  costCenterId?: string | null;
  /** رقم الحساب — taken from the card when the accountant typed one, allocated otherwise. */
  accountCode?: string | null;
  createAccount?: boolean;
};
export type EmployeeStatus = 'active' | 'suspended' | 'terminated';
export type EmployeeGender = 'male' | 'female';
export type EmployeeQuery = { q?: string; status?: string; branchId?: string; departmentId?: string; jobId?: string };
export type DepartmentPatch = Partial<DepartmentInput>;
export type JobPatch = Partial<JobInput>;
export type EmployeePatch = Partial<EmployeeInput>;

/**
 * «الرواتب والمستحقات» — the seven rows of `frmEmployees.xaml`, in the window's own order.
 * `CalculateTotalSalary()` (L500) adds all seven for «إجمالي الرواتب والمستحقات», and
 * `calculatePayrollLine` sums the same map for the payroll run, so the card and the
 * مسير cannot disagree about what an employee earns.
 */
export const SALARY_COMPONENT_LABELS: Record<string, string> = {
  basic: 'الراتب الأساسي',
  housing: 'بدل سكن',
  transport: 'بدل مواصلات',
  food: 'طعام',
  medical: 'طبي',
  fixedBonus: 'مكافأة ثابتة',
  other: 'أخرى',
};
export type AdjustmentInput = { employeeId: string; kind: 'addition' | 'deduction'; componentCode: string; valueText: string; startsOn: string; endsOn?: string; recurring?: boolean; subFromSalary?: boolean; cashLocationId?: string; reason?: string };
export type RunInput = { yearMonth: string; periodId?: string; currency?: string; unpaidDaysByEmployee?: Record<string, number> };
export type PostRunInput = { journalEntryId?: string; branchId?: string; fiscalPeriodId?: string; journalLines?: JournalLineInput[] };
export type PayRunInput = { branchId: string; cashLocationId: string; method?: 'cash' | 'cheque' | 'bank_transfer' | 'card'; fiscalPeriodId?: string };

/** `Departments.manag_id` — the إدارة above a قسم, joined under a second name. */
const management = alias(departments, 'management');

/** What every employee read returns: the row plus the names the card and the grid print. */
const employeeCardSelect = {
  row: employees,
  departmentName: departments.name,
  departmentParentId: departments.parentId,
  managementName: management.name,
  jobName: jobs.name,
  accountCode: accounts.code,
};

/** «النوع» — `frmEmployees.xaml` rdMale / rdFemale. */
const GENDER_LABELS: Record<string, string> = { male: 'ذكر', female: 'أنثى' };
/** «الحالة» — `frmEmployees.xaml` cmbStatus; the desktop reads a `States` lookup. */
const STATUS_LABELS: Record<string, string> = { active: 'نشط', suspended: 'موقوف', terminated: 'منتهي الخدمة' };

/**
 * The card as `frmEmployees` reads it back. `managementId`/`managementName` are the
 * employee's إدارة — the parent of the assigned قسم, or the assigned row itself when the
 * employee belongs to the إدارة directly — which is why the card stores one department
 * and not two that could disagree.
 */
function toEmployeeCard(entry: { row: Employee; departmentName: string | null; departmentParentId: string | null; managementName: string | null; jobName: string | null; accountCode: string | null }) {
  const { row, departmentName, departmentParentId, managementName, jobName, accountCode } = entry;
  const components = row.salaryComponents ?? {};
  const totalSalary = Object.values(components).reduce((sum, valueText) => sum.plus(new Decimal(valueText || '0')), new Decimal(0)).toFixed(4);
  return {
    ...maskEmployee(row),
    departmentName: departmentName ?? null,
    departmentKind: row.departmentId ? (departmentParentId ? 'section' : 'management') : null,
    managementId: row.departmentId ? departmentParentId ?? row.departmentId : null,
    managementName: row.departmentId ? managementName ?? departmentName ?? null : null,
    sectionId: departmentParentId ? row.departmentId : null,
    sectionName: departmentParentId ? departmentName ?? null : null,
    jobName: jobName ?? null,
    accountCode: accountCode ?? null,
    totalSalary,
    genderLabel: GENDER_LABELS[row.gender ?? ''] ?? null,
    statusLabel: STATUS_LABELS[row.status] ?? row.status,
  };
}

@Injectable()
export class HrmService {
  constructor(@Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle, private readonly accounting: AccountingService, private readonly treasury: TreasuryService) {}

  async ensureEnabled(tenantId: string) { const [flag] = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(tenantSettings).where(and(eq(tenantSettings.tenantId, tenantId), eq(tenantSettings.key, 'pack.hrm'))).limit(1)); if (flag && flag.value !== true && flag.value !== 'true') throw new DomainError('NOT_FOUND', 'HRM pack is disabled for this tenant', 404); }
  /**
   * 🏢 الإدارات والأقسام — `frmManagement.xaml` («الإدارات») and `frmDepartments.xaml`
   * («إدخال بيانات الإدارات والأقسام») print two grids: managements, and departments
   * with «اسم الإدارة التابع لها» beside each. One table carries both levels, so a row
   * with no parent is an إدارة and a row with a parent is a قسم — and `kind` says which.
   */
  async listDepartments(tenantId: string) {
    await this.ensureEnabled(tenantId);
    const rows = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select({
          row: departments,
          parentName: management.name,
          employeeCount: sql<number>`(SELECT COUNT(*)::int FROM employees e WHERE e.tenant_id = ${tenantId} AND e.department_id = ${departments.id} AND e.deleted_at IS NULL)`,
          sectionCount: sql<number>`(SELECT COUNT(*)::int FROM departments c WHERE c.tenant_id = ${tenantId} AND c.parent_id = ${departments.id} AND c.deleted_at IS NULL)`,
        })
        .from(departments)
        .leftJoin(management, eq(management.id, departments.parentId))
        .where(and(eq(departments.tenantId, tenantId), isNull(departments.deletedAt)))
        .orderBy(departments.code));
    return rows.map(({ row, parentName, employeeCount, sectionCount }) => ({
      ...row,
      kind: row.parentId ? 'section' : 'management',
      parentName: parentName ?? null,
      employeeCount,
      sectionCount,
    }));
  }

  async createDepartment(tenantId: string, input: DepartmentInput) {
    await this.ensureEnabled(tenantId);
    if (!input.name?.trim()) throw new DomainError('DEPARTMENT_NAME_REQUIRED', 'يجب إدخال اسم القسم', 422);
    if (input.parentId) await this.assertManagement(tenantId, input.parentId);
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(departments).values({ id: newId(), tenantId, code: input.code, name: input.name.trim(), branchId: input.branchId, parentId: input.parentId ?? null }).returning());
    return row;
  }

  async updateDepartment(tenantId: string, id: string, patch: DepartmentPatch) {
    await this.ensureEnabled(tenantId);
    if (patch.name !== undefined && !patch.name.trim()) throw new DomainError('DEPARTMENT_NAME_REQUIRED', 'يجب إدخال اسم القسم', 422);
    if (patch.parentId) await this.assertManagement(tenantId, patch.parentId, id);
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) => tx.update(departments).set({ ...(patch.code === undefined ? {} : { code: patch.code }), ...(patch.name === undefined ? {} : { name: patch.name.trim() }), ...(patch.branchId === undefined ? {} : { branchId: patch.branchId }), ...(patch.parentId === undefined ? {} : { parentId: patch.parentId }), updatedAt: new Date() }).where(and(eq(departments.tenantId, tenantId), eq(departments.id, id), isNull(departments.deletedAt))).returning());
    if (!row) throw new DomainError('NOT_FOUND', 'Department not found', 404);
    return row;
  }

  /**
   * A department that still has staff — or أقسام — on it is kept: deleting it would
   * strip their org unit. «لا يمكن حذف إدارة لها أقسام» is the cloud's own wording; the
   * desktop deletes the row and leaves its departments pointing at nothing.
   */
  async deleteDepartment(tenantId: string, id: string) {
    await this.ensureEnabled(tenantId);
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const used = await tx.execute(sql`SELECT EXISTS (SELECT 1 FROM employees WHERE tenant_id = ${tenantId} AND department_id = ${id} AND deleted_at IS NULL) AS used`);
      if ((rowsOf(used)[0] as { used: boolean }).used) throw new DomainError('DEPARTMENT_IN_USE', 'Move the employees to another department first', 409);
      const children = await tx.execute(sql`SELECT EXISTS (SELECT 1 FROM departments WHERE tenant_id = ${tenantId} AND parent_id = ${id} AND deleted_at IS NULL) AS used`);
      if ((rowsOf(children)[0] as { used: boolean }).used) throw new DomainError('DEPARTMENT_HAS_SECTIONS', 'لا يمكن حذف إدارة لها أقسام', 409);
      const result = await tx.update(departments).set({ deletedAt: new Date(), updatedAt: new Date() }).where(and(eq(departments.tenantId, tenantId), eq(departments.id, id), isNull(departments.deletedAt)));
      if (!result.rowCount) throw new DomainError('NOT_FOUND', 'Department not found', 404);
      return { id, deleted: true };
    });
  }

  /** `frmDepartments.xaml.cs` L120 — a قسم is saved under an إدارة, never under another قسم. */
  private async assertManagement(tenantId: string, parentId: string, movingId?: string) {
    const [parent] = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(departments).where(and(eq(departments.tenantId, tenantId), eq(departments.id, parentId), isNull(departments.deletedAt))).limit(1));
    if (!parent) throw new DomainError('NOT_FOUND', 'Management not found', 404);
    if (parent.parentId) throw new DomainError('DEPARTMENT_LEVEL_INVALID', 'لا يمكن إضافة قسم تحت قسم آخر', 422);
    if (movingId && parent.id === movingId) throw new DomainError('DEPARTMENT_CYCLE', 'لا يمكن أن يكون القسم تابعاً لنفسه', 422);
    return parent;
  }
  async listJobs(tenantId: string) { await this.ensureEnabled(tenantId); return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(jobs).where(eq(jobs.tenantId, tenantId)).orderBy(jobs.code)); }
  async createJob(tenantId: string, input: JobInput) { await this.ensureEnabled(tenantId); const [row] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(jobs).values({ id: newId(), tenantId, code: input.code, name: input.name }).returning()); return row; }
  async updateJob(tenantId: string, id: string, patch: JobPatch) { await this.ensureEnabled(tenantId); const [row] = await withTenantTx(this.database.db, tenantId, (tx) => tx.update(jobs).set({ ...(patch.code === undefined ? {} : { code: patch.code }), ...(patch.name === undefined ? {} : { name: patch.name }), updatedAt: new Date() }).where(and(eq(jobs.tenantId, tenantId), eq(jobs.id, id))).returning()); if (!row) throw new DomainError('NOT_FOUND', 'Job not found', 404); return row; }
  async deleteJob(tenantId: string, id: string) { await this.ensureEnabled(tenantId); return withTenantTx(this.database.db, tenantId, async (tx) => { const used = await tx.execute(sql`SELECT EXISTS (SELECT 1 FROM employees WHERE tenant_id = ${tenantId} AND job_id = ${id} AND deleted_at IS NULL) AS used`); if ((rowsOf(used)[0] as { used: boolean }).used) throw new DomainError('JOB_IN_USE', 'Employees are still assigned to this job title', 409); const result = await tx.delete(jobs).where(and(eq(jobs.tenantId, tenantId), eq(jobs.id, id))); if (!result.rowCount) throw new DomainError('NOT_FOUND', 'Job not found', 404); return { id, deleted: true }; }); }

  /**
   * 👤 قائمة الموظفين — `frmEmployees.xaml` «قائمة الموظفين» prints
   * `الرقم · الاسم · إجمالي المرتب` and `BtnSearch_Click` narrows it with
   * `name LIKE N'%…%'`. The cloud's list used to be bare rows; it now carries the names
   * the card shows (so the grid needs no second request per row) and the إجمالي of the
   * seven salary rows, which is the column the window actually prints.
   */
  async listEmployees(tenantId: string, query: EmployeeQuery = {}) {
    await this.ensureEnabled(tenantId);
    const rows = await withTenantTx(this.database.db, tenantId, (tx) => tx.select(employeeCardSelect).from(employees)
      .leftJoin(departments, eq(departments.id, employees.departmentId))
      .leftJoin(management, eq(management.id, departments.parentId))
      .leftJoin(jobs, eq(jobs.id, employees.jobId))
      .leftJoin(accounts, eq(accounts.id, employees.employeeAccountId))
      .where(and(
        eq(employees.tenantId, tenantId),
        isNull(employees.deletedAt),
        query.q?.trim() ? or(ilike(employees.name, `%${query.q.trim()}%`), ilike(employees.employeeNo, `%${query.q.trim()}%`)) : undefined,
        query.status ? eq(employees.status, query.status) : undefined,
        query.branchId ? eq(employees.branchId, query.branchId) : undefined,
        // Filtering by إدارة means its أقسام too — the card's «الإدارة» holds both.
        query.departmentId ? or(eq(employees.departmentId, query.departmentId), eq(departments.parentId, query.departmentId)) : undefined,
        query.jobId ? eq(employees.jobId, query.jobId) : undefined,
      ))
      .orderBy(employees.employeeNo));
    return rows.map(toEmployeeCard);
  }

  /** 👤 تعريف موظف — the card behind `frmEmployees.xaml`, read back by id. */
  async readEmployee(tenantId: string, id: string) {
    await this.ensureEnabled(tenantId);
    const rows = await withTenantTx(this.database.db, tenantId, (tx) => tx.select(employeeCardSelect).from(employees)
      .leftJoin(departments, eq(departments.id, employees.departmentId))
      .leftJoin(management, eq(management.id, departments.parentId))
      .leftJoin(jobs, eq(jobs.id, employees.jobId))
      .leftJoin(accounts, eq(accounts.id, employees.employeeAccountId))
      .where(and(eq(employees.tenantId, tenantId), eq(employees.id, id), isNull(employees.deletedAt)))
      .limit(1));
    const [row] = rows;
    if (!row) throw new DomainError('NOT_FOUND', 'Employee not found', 404);
    return toEmployeeCard(row);
  }

  async createEmployee(tenantId: string, input: EmployeeInput) {
    await this.ensureEnabled(tenantId);
    const name = input.name?.trim();
    // `frmEmployees.xaml.cs` L520 — the one thing the window will not save without.
    if (!name) throw new DomainError('EMPLOYEE_NAME_REQUIRED', 'يجب إدخال اسم الموظف', 422);
    if (!input.employeeNo?.trim()) throw new DomainError('EMPLOYEE_NO_REQUIRED', 'يجب إدخال رقم الموظف', 422);
    validateComponents(input.salaryComponents ?? {});
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(employees).values({ id: newId(), tenantId, employeeNo: input.employeeNo.trim(), name, branchId: input.branchId, departmentId: input.departmentId, jobId: input.jobId, membershipId: input.membershipId, status: input.status ?? 'active', hireDate: input.hireDate, birthDate: input.birthDate, insuranceNo: input.insuranceNo, nationalId: input.nationalId, maritalStatus: input.maritalStatus, nationality: input.nationality, gender: input.gender, phone: input.phone, mobile: input.mobile, email: input.email, address: input.address, notes: input.notes, salaryComponents: input.salaryComponents ?? {}, bank: input.bank ?? {}, salaryExpenseAccountId: input.salaryExpenseAccountId, salaryPayableAccountId: input.salaryPayableAccountId, costCenterId: input.costCenterId }).returning());
    if (!row) throw new DomainError('INTERNAL', 'Employee was not created', 500);
    if (input.createAccount !== false) await this.employeeAccount(tenantId, row.id, name, input.branchId, input.accountCode ?? undefined, undefined);
    return this.readEmployee(tenantId, row.id);
  }

  async updateEmployee(tenantId: string, id: string, patch: EmployeePatch) {
    await this.ensureEnabled(tenantId);
    if (patch.name !== undefined && !patch.name.trim()) throw new DomainError('EMPLOYEE_NAME_REQUIRED', 'يجب إدخال اسم الموظف', 422);
    if (patch.salaryComponents) validateComponents(patch.salaryComponents);
    const [current] = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(employees).where(and(eq(employees.tenantId, tenantId), eq(employees.id, id), isNull(employees.deletedAt))).limit(1));
    if (!current) throw new DomainError('NOT_FOUND', 'Employee not found', 404);
    const nextName = patch.name?.trim();
    await withTenantTx(this.database.db, tenantId, (tx) => tx.update(employees).set({
      ...(patch.employeeNo === undefined ? {} : { employeeNo: patch.employeeNo.trim() }),
      ...(nextName === undefined ? {} : { name: nextName }),
      ...(patch.branchId === undefined ? {} : { branchId: patch.branchId }),
      ...(patch.departmentId === undefined ? {} : { departmentId: patch.departmentId }),
      ...(patch.jobId === undefined ? {} : { jobId: patch.jobId }),
      ...(patch.membershipId === undefined ? {} : { membershipId: patch.membershipId }),
      ...(patch.hireDate === undefined ? {} : { hireDate: patch.hireDate }),
      ...(patch.birthDate === undefined ? {} : { birthDate: patch.birthDate }),
      ...(patch.insuranceNo === undefined ? {} : { insuranceNo: patch.insuranceNo }),
      ...(patch.nationalId === undefined ? {} : { nationalId: patch.nationalId }),
      ...(patch.maritalStatus === undefined ? {} : { maritalStatus: patch.maritalStatus }),
      ...(patch.nationality === undefined ? {} : { nationality: patch.nationality }),
      ...(patch.gender === undefined ? {} : { gender: patch.gender }),
      ...(patch.phone === undefined ? {} : { phone: patch.phone }),
      ...(patch.mobile === undefined ? {} : { mobile: patch.mobile }),
      ...(patch.email === undefined ? {} : { email: patch.email }),
      ...(patch.address === undefined ? {} : { address: patch.address }),
      ...(patch.notes === undefined ? {} : { notes: patch.notes }),
      ...(patch.salaryComponents === undefined ? {} : { salaryComponents: patch.salaryComponents }),
      ...(patch.bank === undefined ? {} : { bank: patch.bank }),
      ...(patch.salaryExpenseAccountId === undefined ? {} : { salaryExpenseAccountId: patch.salaryExpenseAccountId }),
      ...(patch.salaryPayableAccountId === undefined ? {} : { salaryPayableAccountId: patch.salaryPayableAccountId }),
      ...(patch.costCenterId === undefined ? {} : { costCenterId: patch.costCenterId }),
      ...(patch.status === undefined ? {} : { status: patch.status }),
      updatedAt: new Date(),
    }).where(and(eq(employees.tenantId, tenantId), eq(employees.id, id), isNull(employees.deletedAt))));
    // `frmEmployees.xaml.cs` L600 `SaveAccounts` renames the row when the code already
    // exists: the account *is* the employee inside the ledger, so a renamed employee
    // whose account kept the old name prints two different people.
    if (nextName !== undefined && nextName !== current.name && current.employeeAccountId) {
      await this.employeeAccount(tenantId, id, nextName, patch.branchId ?? current.branchId, patch.accountCode ?? undefined, current.employeeAccountId);
    }
    return this.readEmployee(tenantId, id);
  }

  /**
   * `frmEmployees.xaml.cs` L730 refuses twice before it deletes: an employee with a
   * user account («لا يمكن حذف موظف مرتبط بمستخدم») and one who sold anything
   * («لا يمكن حذف موظف مرتبط بفواتير»). The cloud adds a third — an employee who has
   * appeared on a مسير رواتب — because the desktop's salary tables carry the employee
   * id with no constraint at all, and a run's lines would outlive their name.
   *
   * What "deleted" means is unchanged: the row is soft-deleted and marked terminated,
   * so the ledger keeps every figure it already posted.
   */
  async deleteEmployee(tenantId: string, id: string) {
    await this.ensureEnabled(tenantId);
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [current] = await tx.select().from(employees).where(and(eq(employees.tenantId, tenantId), eq(employees.id, id), isNull(employees.deletedAt))).limit(1);
      if (!current) throw new DomainError('NOT_FOUND', 'Employee not found', 404);
      const hasUser = await tx.execute(sql`SELECT EXISTS (SELECT 1 FROM memberships WHERE tenant_id = ${tenantId} AND id = ${current.membershipId}) AS used`);
      if ((rowsOf(hasUser)[0] as { used: boolean }).used) throw new DomainError('EMPLOYEE_HAS_USER', 'لا يمكن حذف موظف مرتبط بمستخدم', 409);
      const hasInvoices = await tx.execute(sql`SELECT EXISTS (SELECT 1 FROM sales_invoices WHERE tenant_id = ${tenantId} AND salesman_id = ${id}) AS used`);
      if ((rowsOf(hasInvoices)[0] as { used: boolean }).used) throw new DomainError('EMPLOYEE_HAS_INVOICES', 'لا يمكن حذف موظف مرتبط بفواتير', 409);
      const onPayroll = await tx.execute(sql`SELECT EXISTS (SELECT 1 FROM payroll_run_lines WHERE tenant_id = ${tenantId} AND employee_id = ${id}) AS used`);
      if ((rowsOf(onPayroll)[0] as { used: boolean }).used) throw new DomainError('EMPLOYEE_ON_PAYROLL', 'لا يمكن حذف موظف له مسير رواتب', 409);
      const result = await tx.update(employees).set({ status: 'terminated', deletedAt: new Date(), updatedAt: new Date() }).where(and(eq(employees.tenantId, tenantId), eq(employees.id, id), isNull(employees.deletedAt)));
      if (!result.rowCount) throw new DomainError('NOT_FOUND', 'Employee not found', 404);
      return { id, archived: true, deleted: false };
    });
  }

  /**
   * 👤 رقم الحساب — `frmEmployees.xaml.cs` L600 `SaveAccounts` writes a `TreeAccount`
   * row named after the employee under the branch's employee account
   * (`ParentCode = Common.CurrentBranch.EmployeeAcc`, default 2241 per
   * `Class/Common.cs` L998) and renames it when the code is already there.
   *
   * If the tenant's chart has no account at that code there is nothing to hang the
   * employee on, and the card is still saved — exactly as the desktop behaves, where
   * `SaveAccounts`' failure is caught and ignored.
   */
  private async employeeAccount(tenantId: string, employeeId: string, employeeName: string, branchId: string | null | undefined, requestedCode: string | undefined, existingAccountId: string | undefined) {
    const root = await this.employeeAccountRoot(tenantId);
    if (!root) return undefined;
    if (existingAccountId) {
      try {
        await this.accounting.updateAccount(tenantId, existingAccountId, { nameAr: employeeName });
      } catch {
        // An account that cannot be renamed (posted, or already gone) still carries the
        // employee's money; the card must not fail because of it.
      }
      return this.accounting.readAccount(tenantId, existingAccountId);
    }
    const code = requestedCode?.trim() || (await this.nextEmployeeAccountCode(tenantId, root.code));
    const [taken] = await withTenantTx(this.database.db, tenantId, (tx) => tx.select({ id: accounts.id }).from(accounts).where(and(eq(accounts.tenantId, tenantId), eq(accounts.code, code), isNull(accounts.deletedAt))).limit(1));
    if (taken) throw new DomainError('EMPLOYEE_ACCOUNT_CODE_TAKEN', `رقم الحساب ${code} مستخدم بالفعل`, 409);
    // The employee hangs under the root's own branch of the tree, so it inherits the
    // root's nature rather than choosing one: the desktop writes `Nature = 1` for every
    // employee account, and the seeded root («موظفين الفرع الرئيسي») is a liability.
    const account = await this.accounting.createAccount(tenantId, { code, nameAr: employeeName, type: root.type as AccountType, normalBalance: (root.normalBalance ?? defaultNormalBalance(root.type as AccountType)) as 'debit' | 'credit', parentId: root.id, isPostable: true });
    await withTenantTx(this.database.db, tenantId, (tx) => tx.update(employees).set({ employeeAccountId: account.id, updatedAt: new Date() }).where(and(eq(employees.tenantId, tenantId), eq(employees.id, employeeId))));
    return account;
  }

  /** The chart account employees hang under: `2241` («موظفين الفرع الرئيسي»), overridable. */
  private async employeeAccountRoot(tenantId: string) {
    const [setting] = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(tenantSettings).where(and(eq(tenantSettings.tenantId, tenantId), eq(tenantSettings.key, 'hrm.employee_account_code'))).limit(1));
    const code = typeof setting?.value === 'string' ? setting.value : (setting?.value as { value?: string } | undefined)?.value ?? '2241';
    const [root] = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(accounts).where(and(eq(accounts.tenantId, tenantId), eq(accounts.code, code), isNull(accounts.deletedAt))).limit(1));
    return root;
  }

  /** `MaxId("Code", "Accounts_Index", parent)` — the next free child code under the root. */
  private async nextEmployeeAccountCode(tenantId: string, rootCode: string) {
    const children = await withTenantTx(this.database.db, tenantId, async (tx) => {
      const [root] = await tx.select({ id: accounts.id }).from(accounts).where(and(eq(accounts.tenantId, tenantId), eq(accounts.code, rootCode), isNull(accounts.deletedAt))).limit(1);
      if (!root) return [];
      return tx.select({ code: accounts.code }).from(accounts).where(and(eq(accounts.tenantId, tenantId), eq(accounts.parentId, root.id), isNull(accounts.deletedAt)));
    });
    let highest = 0;
    let width = 4;
    for (const child of children) {
      const suffix = child.code.startsWith(rootCode) ? child.code.slice(rootCode.length) : '';
      if (!/^\d+$/.test(suffix)) continue;
      highest = Math.max(highest, Number(suffix));
      width = Math.max(width, suffix.length);
    }
    return `${rootCode}${String(highest + 1).padStart(width, '0')}`;
  }

  async importAttendanceCsv(tenantId: string, csv: string) { await this.ensureEnabled(tenantId); const rows = parseAttendance(csv); let inserted = 0; let skipped = 0; await withTenantTx(this.database.db, tenantId, async (tx) => { for (const row of rows) { const fingerprint = attendanceFingerprint(row); const result = await tx.execute(sql`INSERT INTO attendance_logs (id, tenant_id, machine, enroll, punch_at, direction, fingerprint, payload) VALUES (${newId()}, ${tenantId}, ${row.machine}, ${row.enroll}, ${new Date(row.datetime)}, ${row.inout}, ${fingerprint}, ${JSON.stringify(row)}::jsonb) ON CONFLICT (tenant_id, fingerprint) DO NOTHING RETURNING id`); if (rowsOf(result).length) inserted += 1; else skipped += 1; } }); return { data: { inserted, skipped, parsed: rows.length } }; }

  async attendanceSummary(tenantId: string, enroll: string, from: string, to: string) { await this.ensureEnabled(tenantId); const rows = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(attendanceLogs).where(and(eq(attendanceLogs.tenantId, tenantId), eq(attendanceLogs.enroll, enroll), gte(attendanceLogs.punchAt, new Date(from)), lte(attendanceLogs.punchAt, new Date(to)))).orderBy(attendanceLogs.punchAt)); let minutes = new Decimal(0); let open: Date | undefined; for (const row of rows) { if (row.direction === 'in') open = row.punchAt; else if (row.direction === 'out' && open) { minutes = minutes.plus(new Decimal(row.punchAt.getTime() - open.getTime()).div(60_000)); open = undefined; } } return { data: { enroll, punches: rows.length, workingHours: minutes.div(60).toFixed(2), method: 'naive in/out pairing; no RC-10 evaluation engine' } }; }

  async listAdjustments(tenantId: string) { await this.ensureEnabled(tenantId); return { data: await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(salaryAdjustments).where(eq(salaryAdjustments.tenantId, tenantId)).orderBy(desc(salaryAdjustments.createdAt)).limit(200)) }; }
  async createAdjustment(tenantId: string, input: AdjustmentInput) { await this.ensureEnabled(tenantId); if (new Decimal(input.valueText).lte(0)) throw new DomainError('VALIDATION_FAILED', 'Adjustment value must be positive', 422); const [row] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(salaryAdjustments).values({ id: newId(), tenantId, employeeId: input.employeeId, kind: input.kind, componentCode: input.componentCode, valueText: input.valueText, startsOn: input.startsOn, endsOn: input.endsOn, recurring: input.recurring ?? false, subFromSalary: input.subFromSalary ?? input.kind === 'deduction', cashLocationId: input.cashLocationId, reason: input.reason }).returning()); return row; }
  async approveAdjustment(tenantId: string, id: string) { await this.ensureEnabled(tenantId); const [row] = await withTenantTx(this.database.db, tenantId, (tx) => tx.update(salaryAdjustments).set({ status: 'approved', updatedAt: new Date() }).where(and(eq(salaryAdjustments.tenantId, tenantId), eq(salaryAdjustments.id, id), eq(salaryAdjustments.status, 'draft'))).returning()); if (!row) throw new DomainError('NOT_FOUND', 'Draft adjustment not found', 404); return row; }

  async preview(tenantId: string, input: RunInput) { await this.ensureEnabled(tenantId); const staff = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(employees).where(and(eq(employees.tenantId, tenantId), eq(employees.status, 'active'), isNull(employees.deletedAt)))); const activeAdjustments = await this.adjustmentsForMonth(tenantId, input.yearMonth); const lines = staff.map((employee) => calculatePayrollLine({ id: employee.id, name: employee.name, components: employee.salaryComponents, unpaidDays: input.unpaidDaysByEmployee?.[employee.id] ?? 0, adjustments: activeAdjustments.filter((entry) => entry.employeeId === employee.id).map((entry) => ({ kind: entry.kind as 'addition' | 'deduction', valueText: entry.valueText })) })); return { data: { yearMonth: input.yearMonth, employeeCount: lines.length, netPayable: sumLines(lines.map((line) => line.net)), lines } }; }

  async createRun(tenantId: string, input: RunInput) { const preview = await this.preview(tenantId, input); const id = newId(); await withTenantTx(this.database.db, tenantId, async (tx) => { await tx.insert(payrollRuns).values({ id, tenantId, yearMonth: input.yearMonth, periodId: input.periodId, currency: input.currency ?? 'SAR', summary: { preview: preview.data } }); if (preview.data.lines.length) await tx.insert(payrollRunLines).values(preview.data.lines.map((line, index) => ({ runId: id, lineNo: index + 1, tenantId, employeeId: line.employeeId, components: line.components, additions: line.additions, deductions: line.deductions, gross: line.gross, net: line.net, status: 'draft', payslipHtml: payslipHtml(input.yearMonth, line.employeeName, line.net) }))); }); return this.readRun(tenantId, id); }
  async readRun(tenantId: string, id: string) { await this.ensureEnabled(tenantId); const [run] = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(payrollRuns).where(and(eq(payrollRuns.tenantId, tenantId), eq(payrollRuns.id, id))).limit(1)); if (!run) throw new DomainError('NOT_FOUND', 'Payroll run not found', 404); const lines = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(payrollRunLines).where(and(eq(payrollRunLines.tenantId, tenantId), eq(payrollRunLines.runId, id))).orderBy(payrollRunLines.lineNo)); return { data: { ...run, lines } }; }
  async listRuns(tenantId: string) { await this.ensureEnabled(tenantId); return { data: await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(payrollRuns).where(eq(payrollRuns.tenantId, tenantId)).orderBy(payrollRuns.yearMonth)) }; }
  async postRun(tenantId: string, id: string, input: PostRunInput = {}) { await this.ensureEnabled(tenantId); const current = await this.readRun(tenantId, id); if (current.data.status !== 'draft') throw new DomainError('PAYROLL_RUN_IMMUTABLE', 'Only draft payroll runs can be posted', 409); let journalEntryId = input.journalEntryId; if (!journalEntryId && input.journalLines?.length && input.branchId && (input.fiscalPeriodId ?? current.data.periodId)) { const journal = await this.accounting.postJournal(tenantId, { branchId: input.branchId, fiscalPeriodId: input.fiscalPeriodId ?? current.data.periodId!, date: new Date().toISOString().slice(0, 10), description: `Payroll ${current.data.yearMonth}`, lines: input.journalLines }); if (!journal) throw new DomainError('INTERNAL', 'Payroll journal was not created', 500); journalEntryId = journal.id; } await withTenantTx(this.database.db, tenantId, (tx) => tx.update(payrollRuns).set({ status: 'posted', journalEntryId, postedAt: new Date(), updatedAt: new Date() }).where(and(eq(payrollRuns.tenantId, tenantId), eq(payrollRuns.id, id)))); return this.readRun(tenantId, id); }
  async payRun(tenantId: string, id: string, input: PayRunInput) { await this.ensureEnabled(tenantId); const current = await this.readRun(tenantId, id); if (current.data.status !== 'posted') throw new DomainError('PAYROLL_RUN_NOT_POSTED', 'Only posted payroll runs can be paid', 409); const payValue = sumLines(current.data.lines.map((line) => line.net)); const voucher = await this.treasury.createVoucher(tenantId, { branchId: input.branchId, kind: 'payment', subtype: 'salary', date: new Date().toISOString().slice(0, 10), cashLocationId: input.cashLocationId, method: input.method ?? 'cash', amount: payValue, netAmount: payValue, idempotencyKey: `payroll:${id}` }); const posted = voucher?.id ? await this.treasury.postVoucher(tenantId, voucher.id, { fiscalPeriodId: input.fiscalPeriodId }) : undefined; await withTenantTx(this.database.db, tenantId, (tx) => tx.update(payrollRuns).set({ status: 'paid', voucherId: posted?.id, paidAt: new Date(), updatedAt: new Date() }).where(and(eq(payrollRuns.tenantId, tenantId), eq(payrollRuns.id, id)))); return this.readRun(tenantId, id); }
  async reverseRun(tenantId: string, id: string, reason: string) { await this.ensureEnabled(tenantId); if (!reason.trim()) throw new DomainError('VALIDATION_FAILED', 'Reversal reason is required', 422); const current = await this.readRun(tenantId, id); if (current.data.status !== 'posted' && current.data.status !== 'paid') throw new DomainError('PAYROLL_REVERSAL_INVALID', 'Only posted or paid runs can be reversed', 409); await withTenantTx(this.database.db, tenantId, (tx) => tx.update(payrollRuns).set({ status: 'reversed', reversedAt: new Date(), reversalReason: reason, updatedAt: new Date() }).where(and(eq(payrollRuns.tenantId, tenantId), eq(payrollRuns.id, id)))); return this.readRun(tenantId, id); }

  private async adjustmentsForMonth(tenantId: string, yearMonth: string) { const start = `${yearMonth}-01`; const end = monthEnd(yearMonth); return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(salaryAdjustments).where(and(eq(salaryAdjustments.tenantId, tenantId), eq(salaryAdjustments.status, 'approved'), lte(salaryAdjustments.startsOn, end), or(isNull(salaryAdjustments.endsOn), gte(salaryAdjustments.endsOn, start)) ?? sql`true`))); }
}

function validateComponents(components: Record<string, string>) { for (const entry of Object.entries(components)) if (!new Decimal(entry[1] || '0').isFinite()) throw new DomainError('VALIDATION_FAILED', `Invalid salary component ${entry[0]}`, 422); }
function maskEmployee(employee: Employee) { const bank = employee.bank ?? {}; return { ...employee, bank: { ...bank, iban: maskIban(bank.iban) } }; }
function maskIban(value: string | undefined) { return value ? `${value.slice(0, 4)}********${value.slice(-4)}` : undefined; }
function parseAttendance(csv: string): Array<{ machine: string; enroll: string; datetime: string; inout: string }> { const lines = csv.trim().split(/\r?\n/).filter(Boolean); return lines.slice(lines[0]?.toLowerCase().includes('machine') ? 1 : 0).map((line) => { const [rawMachine, rawEnroll, rawDatetime, rawInout] = line.split(',').map((part) => part?.trim() ?? ''); const inout = rawInout === 'out' ? 'out' : rawInout === 'in' ? 'in' : 'unknown'; return { machine: rawMachine ?? '', enroll: rawEnroll ?? '', datetime: rawDatetime ?? '', inout }; }); }
function attendanceFingerprint(row: { machine: string; enroll: string; datetime: string; inout: string }) { return createHash('sha256').update(`${row.machine}|${row.enroll}|${row.datetime}|${row.inout}`).digest('hex'); }
function rowsOf(result: unknown): Array<Record<string, unknown>> { return Array.isArray(result) ? result as Array<Record<string, unknown>> : ((result as { rows?: Array<Record<string, unknown>> }).rows ?? []); }
function sumLines(values: string[]): string { return values.reduce((sum, valueText) => sum.plus(new Decimal(valueText)), new Decimal(0)).toFixed(4); }
function payslipHtml(yearMonth: string, employeeName: string, netValue: string) { return `<!doctype html><html dir="rtl"><body><h1>قسيمة راتب ${escapeHtml(yearMonth)}</h1><p>${escapeHtml(employeeName)}: ${escapeHtml(netValue)}</p></body></html>`; }
function escapeHtml(value: string) { return value.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;'); }
