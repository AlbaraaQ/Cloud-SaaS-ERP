'use client';

import { Directory } from '../../../components/directory';
import { apiPost } from '../../../lib/api';
import { arabicName, listUnits, type Unit } from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

export default function UnitsPage() {
  const { can } = useSession();
  const units = useQuery<Unit[]>(() => listUnits(), []);

  return (
    <Directory<Unit>
      title="بطاقة وحدة"
      subtitle="وحدات القياس الأساسية للمواد (حبة، كرتون، كيلوجرام…)."
      crumbs={['المستودعات', 'التعاريف']}
      query={units}
      canCreate={can('catalog.unit.manage')}
      createLabel="وحدة جديدة"
      fields={[
        { name: 'code', label: 'الرمز', required: true, ltr: true, placeholder: 'PCS' },
        { name: 'nameAr', label: 'الاسم العربي', required: true },
        { name: 'nameEn', label: 'الاسم الإنجليزي', ltr: true },
      ]}
      onCreate={(values) =>
        apiPost('/organization/catalog/units', {
          code: String(values.code).trim().toUpperCase(),
          nameAr: String(values.nameAr).trim(),
          nameEn: String(values.nameEn).trim() || undefined,
        })
      }
      successText={(values) => `تمت إضافة الوحدة ${String(values.code)}.`}
      rowKey={(row) => row.id}
      empty="لا توجد وحدات قياس"
      columns={[
        { key: 'code', header: 'الرمز', align: 'ltr', cell: (row) => row.code },
        { key: 'name', header: 'الاسم', cell: (row) => arabicName(row) },
      ]}
    />
  );
}
