'use client';

import { Directory } from '../../../components/directory';
import { apiList, apiPost } from '../../../lib/api';
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
      canCreate={can('sales.invoice.create')}
      createLabel="مندوب جديد"
      fields={[
        { name: 'name', label: 'الاسم', required: true },
        { name: 'employeeRef', label: 'الرقم الوظيفي', ltr: true },
      ]}
      onCreate={(values) => apiPost('/sales/salesmen', { name: String(values.name).trim(), employeeRef: String(values.employeeRef).trim() || undefined })}
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
