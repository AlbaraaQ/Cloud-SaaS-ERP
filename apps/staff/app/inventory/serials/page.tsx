'use client';

import { Directory } from '../../../components/directory';
import { apiList, apiPost } from '../../../lib/api';
import { arabicName, itemLabel, listItems, listWarehouses, statusLabel, type Item, type Warehouse } from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type Serial = { id: string; itemId: string; serialNo: string; status: string; warehouseId?: string | null };

const SERIAL_STATUS: Record<string, string> = {
  available: 'متاح',
  reserved: 'محجوز',
  consumed: 'مُباع',
  returned: 'مُرتجع',
};

export default function SerialsPage() {
  const { can } = useSession();
  const serials = useQuery<Serial[]>(() => apiList<Serial>('/inventory/serials'), []);
  const items = useQuery<Item[]>(() => listItems(), []);
  const warehouses = useQuery<Warehouse[]>(() => listWarehouses(), []);
  const itemRows = items.data ?? [];
  const serialRows = serials.data ?? [];
  const count = (status: string) => serialRows.filter((row) => row.status === status).length;

  return (
    <Directory<Serial>
      title="تقرير الأرقام التسلسلية"
      subtitle="تتبّع كل قطعة برقمها التسلسلي وحالتها الحالية."
      crumbs={['المستودعات', 'تقارير مستودعية']}
      query={serials}
      canCreate={can('inventory.adjust')}
      createLabel="رقم تسلسلي جديد"
      blocked={itemRows.length === 0 ? 'أضف مادة واحدة على الأقل من «دليل المواد».' : undefined}
      fields={[
        { name: 'itemId', label: 'المادة', type: 'select', required: true, options: itemRows.map((row) => ({ id: row.id, label: itemLabel(row) })) },
        { name: 'serialNo', label: 'الرقم التسلسلي', required: true, ltr: true },
        {
          name: 'warehouseId',
          label: 'المستودع',
          type: 'select',
          options: (warehouses.data ?? []).map((row) => ({ id: row.id, label: arabicName(row) })),
        },
      ]}
      onCreate={(values) =>
        apiPost('/inventory/serials', {
          itemId: String(values.itemId),
          serialNo: String(values.serialNo).trim(),
          warehouseId: String(values.warehouseId) || undefined,
        })
      }
      successText={(values) => `تمت إضافة الرقم ${String(values.serialNo)}.`}
      rowKey={(row) => row.id}
      tiles={[
        { label: 'إجمالي الأرقام', value: serialRows.length, hint: 'رقم تسلسلي مسجّل', tone: 'brand' },
        { label: 'متاح', value: count('available'), hint: 'جاهز للبيع', tone: 'ok' },
        { label: 'محجوز', value: count('reserved'), hint: 'مرتبط بمستند', tone: 'warn' },
        { label: 'مُباع', value: count('consumed'), hint: 'خرج من المخزون' },
        { label: 'مُرتجع', value: count('returned'), hint: 'أُعيد إلى المخزون' },
      ]}
      empty="لا توجد أرقام تسلسلية"
      columns={[
        {
          key: 'item',
          header: 'المادة',
          cell: (row) => {
            const item = itemRows.find((entry) => entry.id === row.itemId);
            return item ? itemLabel(item) : row.itemId;
          },
        },
        { key: 'serial', header: 'الرقم التسلسلي', align: 'ltr', cell: (row) => row.serialNo },
        { key: 'status', header: 'الحالة', cell: (row) => SERIAL_STATUS[row.status] ?? statusLabel(row.status) },
      ]}
    />
  );
}
