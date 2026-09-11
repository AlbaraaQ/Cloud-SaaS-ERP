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
  const lotRows = lots.data ?? [];
  const today = new Date().toISOString().slice(0, 10);

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
      tiles={[
        { label: 'عدد الدفعات', value: lotRows.length, hint: 'دفعة مسجّلة', tone: 'brand' },
        {
          label: 'منتهية',
          value: lotRows.filter((row) => row.expiryDate && row.expiryDate < today).length,
          hint: 'تجاوزت تاريخ الصلاحية',
          tone: lotRows.some((row) => row.expiryDate && row.expiryDate < today) ? 'danger' : 'ok',
        },
        {
          label: 'دفعات منتهية خلال شهر',
          value: lotRows.filter((row) => {
            if (!row.expiryDate || row.expiryDate < today) return false;
            const days = Math.round((new Date(row.expiryDate).getTime() - Date.now()) / 86400000);
            return days <= 30;
          }).length,
          hint: 'تحتاج متابعة',
          tone: 'warn',
        },
        {
          label: 'بدون تاريخ صلاحية',
          value: lotRows.filter((row) => !row.expiryDate).length,
          hint: 'لن تظهر في تقرير الصلاحية',
        },
        { label: 'مواد لها دفعات', value: new Set(lotRows.map((row) => row.itemId)).size, hint: 'مادة' },
      ]}
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
