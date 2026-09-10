'use client';

import { useState } from 'react';

import { DataTable, QueryView } from '../../../components/data-view';
import { Screen } from '../../../components/screen';
import { apiList } from '../../../lib/api';
import { arabicName, dateTime, itemLabel, listItems, listWarehouses, money, quantity, type Item, type Warehouse } from '../../../lib/lookups';
import { useQuery } from '../../../lib/use-query';

type Movement = {
  id: string;
  itemId: string;
  warehouseId: string;
  occurredAt: string;
  docType: string;
  direction: 'in' | 'out';
  qty: string;
  unitCost: string;
  totalCost: string;
};

const DOC_LABELS: Record<string, string> = {
  sales_invoice: 'فاتورة مبيعات',
  purchase_invoice: 'فاتورة مشتريات',
  stock_transfer: 'مناقلة (صرف)',
  stock_transfer_receipt: 'مناقلة (استلام)',
  stock_adjustment: 'تسوية مخزنية',
  opening_stock: 'بضاعة أول مدة',
};

export default function MovementsPage() {
  const [itemId, setItemId] = useState('');
  const [warehouseId, setWarehouseId] = useState('');
  const items = useQuery<Item[]>(() => listItems(), []);
  const warehouses = useQuery<Warehouse[]>(() => listWarehouses(), []);
  const query = new URLSearchParams({ ...(itemId ? { item_id: itemId } : {}), ...(warehouseId ? { warehouse_id: warehouseId } : {}) }).toString();
  const movements = useQuery<Movement[]>(() => apiList<Movement>(`/inventory/movements${query ? `?${query}` : ''}`), [itemId, warehouseId]);

  const itemRows = items.data ?? [];
  const warehouseRows = warehouses.data ?? [];

  return (
    <Screen
      title="حركة مادة (تفصيلي)"
      subtitle="كل حركة دخول وخروج بترتيب زمني، بالتكلفة التي رُحّلت بها."
      crumbs={['المستودعات', 'تقارير مستودعية']}
    >
      <div className="card toolbar">
        <label className="field">
          <span>المادة</span>
          <select className="input" value={itemId} onChange={(event) => setItemId(event.target.value)}>
            <option value="">كل المواد</option>
            {itemRows.map((row) => (
              <option key={row.id} value={row.id}>
                {itemLabel(row)}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>المستودع</span>
          <select className="input" value={warehouseId} onChange={(event) => setWarehouseId(event.target.value)}>
            <option value="">كل المستودعات</option>
            {warehouseRows.map((row) => (
              <option key={row.id} value={row.id}>
                {arabicName(row)}
              </option>
            ))}
          </select>
        </label>
      </div>

      <QueryView query={movements} empty="لا توجد حركات" emptyDetail="تظهر الحركات بعد ترحيل فاتورة أو مناقلة أو تسوية.">
        {(rows) => (
          <DataTable
            rows={rows}
            rowKey={(row) => row.id}
            columns={[
              { key: 'at', header: 'التاريخ', align: 'ltr', cell: (row) => dateTime(row.occurredAt) },
              {
                key: 'item',
                header: 'المادة',
                cell: (row) => {
                  const item = itemRows.find((entry) => entry.id === row.itemId);
                  return item ? itemLabel(item) : row.itemId;
                },
              },
              { key: 'doc', header: 'المستند', cell: (row) => DOC_LABELS[row.docType] ?? row.docType },
              { key: 'dir', header: 'الاتجاه', cell: (row) => (row.direction === 'in' ? 'وارد' : 'صادر') },
              { key: 'qty', header: 'الكمية', align: 'num', cell: (row) => quantity(row.qty) },
              { key: 'unit', header: 'تكلفة الوحدة', align: 'num', cell: (row) => money(row.unitCost) },
              { key: 'total', header: 'الإجمالي', align: 'num', cell: (row) => money(row.totalCost) },
            ]}
          />
        )}
      </QueryView>
    </Screen>
  );
}
