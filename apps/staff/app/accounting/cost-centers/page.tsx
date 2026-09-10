'use client';

import { Directory } from '../../../components/directory';
import { apiData, apiDelete, apiPatch, apiPost } from '../../../lib/api';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type CostCenter = { id: string; code: string; nameAr?: string; name_ar?: string; nameEn?: string | null; parentId?: string | null };

const label = (row: CostCenter) => row.nameAr ?? row.name_ar ?? row.code;

export default function CostCentersPage() {
  const { can } = useSession();
  const centers = useQuery<CostCenter[]>(() => apiData<CostCenter[]>('/cost-centers'), []);
  const manage = can('accounting.account.manage');
  const rows = centers.data ?? [];

  return (
    <Directory<CostCenter>
      title="مراكز التكلفة"
      subtitle="تُستخدم لتوزيع المصروفات والإيرادات على الأنشطة والفروع."
      crumbs={['المحاسبة', 'تعاريف']}
      query={centers}
      canCreate={manage}
      createLabel="مركز تكلفة جديد"
      fields={[
        { name: 'code', label: 'الرمز', required: true, ltr: true },
        { name: 'nameAr', label: 'الاسم', required: true },
        { name: 'nameEn', label: 'الاسم بالإنجليزية', ltr: true },
        {
          name: 'parentId',
          label: 'المركز الأب',
          type: 'select',
          options: rows.map((row) => ({ id: row.id, label: `${row.code} — ${label(row)}` })),
        },
      ]}
      onCreate={(values) =>
        apiPost('/cost-centers', {
          code: String(values.code).trim(),
          nameAr: String(values.nameAr).trim(),
          nameEn: String(values.nameEn).trim() || undefined,
          parentId: String(values.parentId) || undefined,
        })
      }
      edit={
        manage
          ? {
              toForm: (row) => ({ code: row.code, nameAr: label(row), nameEn: row.nameEn ?? '', parentId: row.parentId ?? '' }),
              onUpdate: (row, values) =>
                apiPatch(`/cost-centers/${row.id}`, {
                  code: String(values.code).trim(),
                  nameAr: String(values.nameAr).trim(),
                  nameEn: String(values.nameEn).trim() || null,
                  parentId: String(values.parentId) || null,
                }),
            }
          : undefined
      }
      onDelete={manage ? (row) => apiDelete(`/cost-centers/${row.id}`) : undefined}
      rowLabel={(row) => `مركز التكلفة ${row.code}`}
      successText={(values) => `تم إنشاء مركز التكلفة ${String(values.code)}.`}
      rowKey={(row) => row.id}
      empty="لا توجد مراكز تكلفة"
      columns={[
        { key: 'code', header: 'الرمز', align: 'ltr', cell: (row) => row.code },
        { key: 'name', header: 'الاسم', cell: (row) => label(row) },
        {
          key: 'parent',
          header: 'المركز الأب',
          cell: (row) => {
            const parent = rows.find((item) => item.id === row.parentId);
            return parent ? `${parent.code} — ${label(parent)}` : '—';
          },
        },
      ]}
    />
  );
}
