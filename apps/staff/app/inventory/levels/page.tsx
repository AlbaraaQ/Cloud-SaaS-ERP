'use client';

import { useState } from 'react';

import { DataTable, QueryView } from '../../../components/data-view';
import { Screen } from '../../../components/screen';
import { apiList } from '../../../lib/api';
import { downloadCsv } from '../../../lib/accounts';
import { arabicName, itemLabel, listItems, listWarehouses, money, quantity, type Item, type Warehouse } from '../../../lib/lookups';
import { useQuery } from '../../../lib/use-query';

type Level = { itemId: string; warehouseId: string; quantity: string; value: string; averageCost: string };

export default function StockLevelsPage() {
  const [warehouseId, setWarehouseId] = useState('');
  const [itemId, setItemId] = useState('');
  const warehouses = useQuery<Warehouse[]>(() => listWarehouses(), []);
  const items = useQuery<Item[]>(() => listItems(), []);
  const levels = useQuery<Level[]>(
    () =>
      apiList<Level>(
        `/inventory/levels${warehouseId || itemId ? `?${new URLSearchParams({ ...(warehouseId ? { warehouse_id: warehouseId } : {}), ...(itemId ? { item_id: itemId } : {}) }).toString()}` : ''}`,
      ),
    [warehouseId, itemId],
  );

  const itemRows = items.data ?? [];
  const warehouseRows = warehouses.data ?? [];
  const nameOfItem = (id: string) => {
    const item = itemRows.find((row) => row.id === id);
    return item ? itemLabel(item) : id;
  };
  const nameOfWarehouse = (id: string) => {
    const warehouse = warehouseRows.find((row) => row.id === id);
    return warehouse ? arabicName(warehouse) : id;
  };

  return (
    <Screen
      title="جرد المواد"
      subtitle="الأرصدة الحالية لكل مادة في كل مستودع، بمتوسط التكلفة والقيمة الدفترية."
      crumbs={['المستودعات', 'تقارير مستودعية']}
      actions={
        <button
          className="btn"
          type="button"
          disabled={levels.status !== 'success' || (levels.data ?? []).length === 0}
          onClick={() =>
            downloadCsv(
              'stock-levels.csv',
              ['المادة', 'المستودع', 'الكمية', 'متوسط التكلفة', 'القيمة'],
              (levels.data ?? []).map((row) => [nameOfItem(row.itemId), nameOfWarehouse(row.warehouseId), row.quantity, row.averageCost, row.value]),
            )
          }
        >
          تصدير CSV
        </button>
      }
    >
      <div className="card toolbar">
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
      </div>

      <QueryView query={levels} empty="لا توجد أرصدة" emptyDetail="لم تُسجَّل أي حركة مخزنية بعد.">
        {(rows) => (
          <DataTable
            rows={rows}
            rowKey={(row) => `${row.itemId}:${row.warehouseId}`}
            columns={[
              { key: 'item', header: 'المادة', cell: (row) => nameOfItem(row.itemId) },
              { key: 'warehouse', header: 'المستودع', cell: (row) => nameOfWarehouse(row.warehouseId) },
              { key: 'qty', header: 'الكمية', align: 'num', cell: (row) => quantity(row.quantity) },
              { key: 'avg', header: 'متوسط التكلفة', align: 'num', cell: (row) => money(row.averageCost) },
              { key: 'value', header: 'القيمة', align: 'num', cell: (row) => money(row.value) },
            ]}
          />
        )}
      </QueryView>
    </Screen>
  );
}
