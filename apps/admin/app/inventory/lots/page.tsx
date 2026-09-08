'use client';

import { Directory } from '../../../components/directory';
import { apiList, apiPost } from '../../../lib/api';
import { itemLabel, listItems, shortDate, type Item } from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type Lot = { id: string; itemId: string; lotNo: string; expiryDate?: string | null; receivedAt?: string | null };

export default function LotsPage() {
  const { can } = useSession();
  const lots = useQuery<Lot[]>(() => apiList<Lot>('/inventory/lots'), []);
  const items = useQuery<Item[]>(() => listItems(), []);
  const itemRows = items.data ?? [];

  return (
    <Directory<Lot>
      title="صلاحية المواد (الدفعات)"
      subtitle="دفعات الإنتاج وتواريخ انتهاء الصلاحية المرتبطة بالمواد."
      crumbs={['المستودعات', 'تقارير مستودعية']}
      query={lots}
      canCreate={can('inventory.adjust')}
      createLabel="دفعة جديدة"
      blocked={itemRows.length === 0 ? 'أضف مادة واحدة على الأقل من «دليل المواد».' : undefined}
      fields={[
        { name: 'itemId', label: 'المادة', type: 'select', required: true, options: itemRows.map((row) => ({ id: row.id, label: itemLabel(row) })) },
        { name: 'lotNo', label: 'رقم الدفعة', required: true, ltr: true },
        { name: 'expiryDate', label: 'تاريخ انتهاء الصلاحية', type: 'date' },
      ]}
      onCreate={(values) =>
        apiPost('/inventory/lots', {
          itemId: String(values.itemId),
          lotNo: String(values.lotNo).trim(),
          expiryDate: String(values.expiryDate) || undefined,
        })
      }
      successText={(values) => `تمت إضافة الدفعة ${String(values.lotNo)}.`}
      rowKey={(row) => row.id}
      empty="لا توجد دفعات"
      columns={[
        {
          key: 'item',
          header: 'المادة',
          cell: (row) => {
            const item = itemRows.find((entry) => entry.id === row.itemId);
            return item ? itemLabel(item) : row.itemId;
          },
        },
        { key: 'lot', header: 'رقم الدفعة', align: 'ltr', cell: (row) => row.lotNo },
        { key: 'expiry', header: 'الصلاحية', align: 'ltr', cell: (row) => shortDate(row.expiryDate) },
      ]}
    />
  );
}
