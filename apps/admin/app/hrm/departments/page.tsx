'use client';

import { Directory } from '../../../components/directory';
import { apiList, apiPost } from '../../../lib/api';
import { arabicName, branchOptions, listBranches, type Branch } from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type Department = { id: string; code: string; name: string; branchId?: string | null };

export default function DepartmentsPage() {
  const { can } = useSession();
  const departments = useQuery<Department[]>(() => apiList<Department>('/hrm/departments'), []);
  const branches = useQuery<Branch[]>(() => listBranches(), []);
  const branchRows = branches.data ?? [];

  return (
    <Directory<Department>
      title="تعريف الإدارات"
      subtitle="الهيكل الإداري الذي يُنسب إليه الموظفون وتُجمّع عليه تقارير الرواتب."
      crumbs={['الموظفين والرواتب', 'التعاريف']}
      query={departments}
      canCreate={can('hrm.manage')}
      createLabel="إدارة جديدة"
      fields={[
        { name: 'code', label: 'الرمز', required: true, ltr: true },
        { name: 'name', label: 'الاسم', required: true },
        { name: 'branchId', label: 'الفرع', type: 'select', options: branchOptions(branchRows) },
      ]}
      onCreate={(values) =>
        apiPost('/hrm/departments', {
          code: String(values.code).trim(),
          name: String(values.name).trim(),
          branchId: String(values.branchId) || undefined,
        })
      }
      successText={(values) => `تمت إضافة الإدارة ${String(values.name)}.`}
      rowKey={(row) => row.id}
      empty="لا توجد إدارات"
      columns={[
        { key: 'code', header: 'الرمز', align: 'ltr', cell: (row) => row.code },
        { key: 'name', header: 'الاسم', cell: (row) => row.name },
        {
          key: 'branch',
          header: 'الفرع',
          cell: (row) => {
            const branch = branchRows.find((entry) => entry.id === row.branchId);
            return branch ? arabicName(branch) : '—';
          },
        },
      ]}
    />
  );
}
