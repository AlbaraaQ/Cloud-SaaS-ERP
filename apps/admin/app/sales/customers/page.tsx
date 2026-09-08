'use client';

import Link from 'next/link';

import { Directory } from '../../../components/directory';
import { apiPost } from '../../../lib/api';
import { listParties, money, type Party } from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

export default function CustomersPage() {
  const { can } = useSession();
  const parties = useQuery<Party[]>(() => listParties('customer'), []);

  return (
    <Directory<Party>
      title="بطاقة عميل"
      subtitle="ملف العميل: الرمز، الرقم الضريبي، سقف الائتمان — يُستخدم في الفواتير وكشف الحساب."
      crumbs={['المبيعات', 'التعاريف']}
      query={parties}
      canCreate={can('parties.manage')}
      createLabel="عميل جديد"
      fields={[
        { name: 'code', label: 'الرمز', ltr: true },
        { name: 'name', label: 'الاسم', required: true },
        { name: 'phone', label: 'الجوال', ltr: true },
        { name: 'taxNo', label: 'الرقم الضريبي', ltr: true },
        { name: 'creditLimit', label: 'سقف الائتمان', type: 'number' },
      ]}
      onCreate={(values) =>
        apiPost('/parties', {
          kind: 'customer',
          code: String(values.code).trim() || undefined,
          name: String(values.name).trim(),
          phone: String(values.phone).trim() || undefined,
          taxNo: String(values.taxNo).trim() || undefined,
          creditLimit: String(values.creditLimit).trim() || undefined,
        })
      }
      successText={(values) => `تمت إضافة العميل ${String(values.name)}.`}
      rowKey={(row) => row.id}
      empty="لا يوجد عملاء"
      columns={[
        { key: 'code', header: 'الرمز', align: 'ltr', cell: (row) => row.code ?? '—' },
        { key: 'name', header: 'الاسم', cell: (row) => row.name },
        { key: 'phone', header: 'الجوال', align: 'ltr', cell: (row) => row.phone ?? '—' },
        { key: 'tax', header: 'الرقم الضريبي', align: 'ltr', cell: (row) => row.taxNo ?? row.tax_no ?? '—' },
        { key: 'credit', header: 'سقف الائتمان', align: 'num', cell: (row) => money(row.creditLimit) },
        {
          key: 'statement',
          header: '',
          cell: (row) => (
            <Link className="btn sm" href={`/sales/statements?partyId=${row.id}`}>
              كشف حساب
            </Link>
          ),
        },
      ]}
    />
  );
}
