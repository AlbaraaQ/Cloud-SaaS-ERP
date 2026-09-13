'use client';

import { Directory } from '../../../components/directory';
import { apiDelete, apiList, apiPatch, apiPost } from '../../../lib/api';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type Salesman = { id: string; name: string; employeeRef?: string | null; active: boolean };

export default function SalesmenPage() {
  const { can } = useSession();
  const salesmen = useQuery<Salesman[]>(() => apiList<Salesman>('/sales/salesmen'), []);

  return (
    <Directory<Salesman>
      title="بطاقة مندوب"
      subtitle="مندوبو المبيعات المرتبطون بالفواتير لتقارير العمولة والأداء."
      crumbs={['المبيعات', 'التعاريف']}
      query={salesmen}
      canCreate={can('sales.salesman.manage')}
      createLabel="مندوب جديد"
      fields={[
        { name: 'name', label: 'الاسم', required: true },
        { name: 'employeeRef', label: 'الرقم الوظيفي', ltr: true },
        { name: 'active', label: 'نشط', type: 'checkbox' },
      ]}
      initial={{ active: true }}
      onCreate={(values) =>
        apiPost('/sales/salesmen', {
          name: String(values.name).trim(),
          employeeRef: String(values.employeeRef).trim() || undefined,
          active: Boolean(values.active),
        })
      }
      edit={
        can('sales.salesman.manage')
          ? {
              toForm: (row) => ({ name: row.name, employeeRef: row.employeeRef ?? '', active: row.active }),
              onUpdate: (row, values) =>
                apiPatch(`/sales/salesmen/${row.id}`, {
                  name: String(values.name).trim(),
                  employeeRef: String(values.employeeRef).trim() || null,
                  active: Boolean(values.active),
                }),
            }
          : undefined
      }
      onDelete={can('sales.salesman.manage') ? (row) => apiDelete(`/sales/salesmen/${row.id}`) : undefined}
      confirmDelete={(row) => `هل تريد حذف المندوب ${row.name}؟ إذا كان مرتبطاً بفواتير فسيتم إيقافه فقط.`}
      rowLabel={(row) => `المندوب ${row.name}`}
      successText={(values) => `تمت إضافة المندوب ${String(values.name)}.`}
      rowKey={(row) => row.id}
      empty="لا يوجد مندوبون"
      columns={[
        { key: 'name', header: 'الاسم', cell: (row) => row.name },
        { key: 'ref', header: 'الرقم الوظيفي', align: 'ltr', cell: (row) => row.employeeRef ?? '—' },
        { key: 'active', header: 'الحالة', cell: (row) => (row.active ? 'نشط' : 'موقوف') },
      ]}
    />
  );
}
