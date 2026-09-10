'use client';

import { Directory } from '../../../components/directory';
import { apiDelete, apiList, apiPatch, apiPost } from '../../../lib/api';
import { arabicName, branchOptions, listBranches, money, shortDate, statusLabel, type Branch } from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type Department = { id: string; code: string; name: string };
type Job = { id: string; code: string; name: string };
type Employee = {
  id: string;
  employeeNo: string;
  name: string;
  branchId?: string | null;
  departmentId?: string | null;
  jobId?: string | null;
  hireDate?: string | null;
  status: string;
  salaryComponents?: Record<string, string>;
  bank?: Record<string, string | undefined> | null;
};

/** The basic salary is the only component every payroll run needs; the rest are optional. */
const COMPONENT_LABELS: Record<string, string> = {
  basic: 'الراتب الأساسي',
  housing: 'بدل سكن',
  transport: 'بدل مواصلات',
  other: 'بدلات أخرى',
};

export default function EmployeesPage() {
  const { can } = useSession();
  const employees = useQuery<Employee[]>(() => apiList<Employee>('/hrm/employees'), []);
  const departments = useQuery<Department[]>(() => apiList<Department>('/hrm/departments'), []);
  const jobs = useQuery<Job[]>(() => apiList<Job>('/hrm/jobs'), []);
  const branches = useQuery<Branch[]>(() => listBranches(), []);

  const grossOf = (row: Employee) =>
    Object.values(row.salaryComponents ?? {}).reduce((sum, value) => sum + Number(value || 0), 0);

  return (
    <Directory<Employee>
      title="تعريف موظف"
      subtitle="بطاقة الموظف ومكوّنات راتبه؛ منها يبدأ استحقاق الرواتب الشهري."
      crumbs={['الموظفين والرواتب', 'التعاريف']}
      query={employees}
      canCreate={can('hrm.manage')}
      createLabel="موظف جديد"
      fields={[
        { name: 'employeeNo', label: 'الرقم الوظيفي', required: true, ltr: true },
        { name: 'name', label: 'الاسم', required: true },
        { name: 'branchId', label: 'الفرع', type: 'select', options: branchOptions(branches.data ?? []) },
        {
          name: 'departmentId',
          label: 'الإدارة',
          type: 'select',
          options: (departments.data ?? []).map((row) => ({ id: row.id, label: `${row.code} — ${row.name}` })),
        },
        { name: 'jobId', label: 'الوظيفة', type: 'select', options: (jobs.data ?? []).map((row) => ({ id: row.id, label: `${row.code} — ${row.name}` })) },
        { name: 'hireDate', label: 'تاريخ التعيين', type: 'date' },
        { name: 'basic', label: 'الراتب الأساسي', type: 'number', required: true },
        { name: 'housing', label: 'بدل سكن', type: 'number' },
        { name: 'transport', label: 'بدل مواصلات', type: 'number' },
        { name: 'iban', label: 'الآيبان', ltr: true },
      ]}
      onCreate={(values) =>
        apiPost('/hrm/employees', {
          employeeNo: String(values.employeeNo).trim(),
          name: String(values.name).trim(),
          branchId: String(values.branchId) || undefined,
          departmentId: String(values.departmentId) || undefined,
          jobId: String(values.jobId) || undefined,
          hireDate: String(values.hireDate) || undefined,
          salaryComponents: Object.fromEntries(
            (['basic', 'housing', 'transport'] as const)
              .map((key) => [key, String(values[key] ?? '').trim()])
              .filter(([, value]) => value !== ''),
          ),
          bank: String(values.iban).trim() ? { iban: String(values.iban).trim() } : undefined,
        })
      }
      edit={
        can('hrm.manage')
          ? {
              toForm: (row) => ({
                employeeNo: row.employeeNo,
                name: row.name,
                branchId: row.branchId ?? '',
                departmentId: row.departmentId ?? '',
                jobId: row.jobId ?? '',
                hireDate: row.hireDate ? String(row.hireDate).slice(0, 10) : '',
                basic: row.salaryComponents?.basic ?? '',
                housing: row.salaryComponents?.housing ?? '',
                transport: row.salaryComponents?.transport ?? '',
                iban: row.bank?.iban ?? '',
              }),
              onUpdate: (row, values) =>
                apiPatch(`/hrm/employees/${row.id}`, {
                  employeeNo: String(values.employeeNo).trim(),
                  name: String(values.name).trim(),
                  branchId: String(values.branchId) || null,
                  departmentId: String(values.departmentId) || null,
                  jobId: String(values.jobId) || null,
                  hireDate: String(values.hireDate) || null,
                  salaryComponents: Object.fromEntries(
                    (['basic', 'housing', 'transport'] as const)
                      .map((key) => [key, String(values[key] ?? '').trim()])
                      .filter(([, value]) => value !== ''),
                  ),
                  bank: String(values.iban).trim() ? { iban: String(values.iban).trim() } : {},
                }),
            }
          : undefined
      }
      onDelete={can('hrm.manage') ? (row) => apiDelete(`/hrm/employees/${row.id}`) : undefined}
      deleteLabel="إنهاء الخدمة"
      confirmDelete={(row) => `هل تريد إنهاء خدمة الموظف ${row.name}؟ إذا سبق أن دخل في مسير رواتب فسيتم أرشفته فقط.`}
      rowLabel={(row) => `الموظف ${row.name}`}
      successText={(values) => `تمت إضافة الموظف ${String(values.name)}.`}
      rowKey={(row) => row.id}
      empty="لا يوجد موظفون"
      emptyDetail="أضف بطاقات الموظفين ليمكن احتساب الرواتب."
      columns={[
        { key: 'no', header: 'الرقم', align: 'ltr', cell: (row) => row.employeeNo },
        { key: 'name', header: 'الاسم', cell: (row) => row.name },
        {
          key: 'department',
          header: 'الإدارة',
          cell: (row) => (departments.data ?? []).find((entry) => entry.id === row.departmentId)?.name ?? '—',
        },
        { key: 'job', header: 'الوظيفة', cell: (row) => (jobs.data ?? []).find((entry) => entry.id === row.jobId)?.name ?? '—' },
        {
          key: 'branch',
          header: 'الفرع',
          cell: (row) => {
            const branch = (branches.data ?? []).find((entry) => entry.id === row.branchId);
            return branch ? arabicName(branch) : '—';
          },
        },
        { key: 'hired', header: 'التعيين', align: 'ltr', cell: (row) => shortDate(row.hireDate) },
        {
          key: 'components',
          header: 'مكوّنات الراتب',
          cell: (row) =>
            Object.entries(row.salaryComponents ?? {})
              .map(([key, value]) => `${COMPONENT_LABELS[key] ?? key}: ${money(value)}`)
              .join(' · ') || '—',
        },
        { key: 'gross', header: 'الإجمالي', align: 'num', cell: (row) => money(grossOf(row)) },
        { key: 'status', header: 'الحالة', cell: (row) => statusLabel(row.status) },
      ]}
    />
  );
}
