'use client';

import { useState } from 'react';

import { QueryView } from '../../../components/data-view';
import {
  ItemPicker,
  PeriodPicker,
  WarehousePicker,
  docTypeLabel,
} from '../../../components/inventory-filters';
import { Screen } from '../../../components/screen';
import { itemCard, money, quantity, shortDate, type ItemCardRow } from '../../../lib/lookups';
import { useQuery } from '../../../lib/use-query';

/**
 * بطاقة الصنف — the item card, which is the stock ledger read the way a storekeeper
 * reads it: opening balance, every movement of the period with a running balance beside
 * it, and the closing balance that has to agree with the balance table.
 *
 * The desktop answered this from `Inventorybalance()` and `TotalItemStock(branch, date)`.
 * The value column matters as much as the quantity one: a card that only counts units
 * cannot answer what the stock is worth.
 */
export default function ItemCardPage() {
  const [itemId, setItemId] = useState('');
  const [warehouseId, setWarehouseId] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [applied, setApplied] = useState<
    { itemId: string; warehouseId: string; from: string; to: string } | undefined
  >();

  const card = useQuery(
    () =>
      applied
        ? itemCard({
            item_id: applied.itemId,
            warehouse_id: applied.warehouseId,
            from: applied.from,
            to: applied.to,
          })
        : Promise.resolve(undefined),
    [applied?.itemId, applied?.warehouseId, applied?.from, applied?.to],
  );

  return (
    <Screen
      title="بطاقة الصنف"
      subtitle="حركة الصنف خلال فترة: رصيد افتتاحي، كل حركة ورصيدها المتحرك، والرصيد الختامي وقيمته."
      crumbs={['المستودعات', 'التقارير']}
    >
      <div className="card form-grid">
        <ItemPicker value={itemId} onChange={setItemId} />
        <WarehousePicker value={warehouseId} onChange={setWarehouseId} includeAll />
        <PeriodPicker from={from} to={to} onFrom={setFrom} onTo={setTo} />
        <div className="row">
          <button
            className="btn primary"
            type="button"
            disabled={!itemId}
            onClick={() => setApplied({ itemId, warehouseId, from, to })}
          >
            عرض البطاقة
          </button>
          {applied && (
            <button className="btn" type="button" onClick={() => setApplied(undefined)}>
              تفريغ
            </button>
          )}
        </div>
      </div>

      {!applied ? (
        <div className="card">
          <p className="muted">
            اختر صنفاً وفترة لعرض بطاقته. ترك الفترة فارغاً يعرض كل الحركات منذ أول حركة للصنف.
          </p>
        </div>
      ) : (
        <QueryView
          query={card}
          empty="لا حركات لهذا الصنف"
          emptyDetail="لا توجد حركات مخزون مطابقة للفلاتر المختارة."
        >
          {(data) =>
            data ? (
              <>
                <div className="grid cols">
                  {[
                    { label: 'رصيد افتتاحي', qty: data.opening.quantity, value: data.opening.value },
                    { label: 'وارد الفترة', qty: data.totals.inQty, value: data.totals.inValue },
                    { label: 'صادر الفترة', qty: data.totals.outQty, value: data.totals.outValue },
                    { label: 'رصيد ختامي', qty: data.closing.quantity, value: data.closing.value },
                  ].map((tile) => (
                    <div className="card tight" key={tile.label}>
                      <span className="muted small">{tile.label}</span>
                      <div>
                        <strong dir="ltr">{quantity(tile.qty)}</strong>
                      </div>
                      <span className="muted small" dir="ltr">
                        {money(tile.value)}
                      </span>
                    </div>
                  ))}
                </div>

                <div className="card">
                  <h2>{`${data.sku ?? ''} — ${data.nameAr ?? ''}`}</h2>
                  <div className="table-wrap">
                    <table className="table">
                      <thead>
                        <tr>
                          <th>التاريخ</th>
                          <th>المستند</th>
                          <th>المستودع</th>
                          <th>الكمية</th>
                          <th>الوحدة</th>
                          <th>الرصيد</th>
                          <th>التكلفة</th>
                          <th>القيمة</th>
                        </tr>
                      </thead>
                      <tbody>
                        {data.rows.map((row: ItemCardRow) => (
                          <tr key={row.id}>
                            <td dir="ltr">{shortDate(row.occurredAt)}</td>
                            <td>{docTypeLabel(row.docType)}</td>
                            <td>{row.warehouseNameAr ?? '—'}</td>
                            <td
                              dir="ltr"
                              style={{
                                color:
                                  row.direction === 'in' ? 'var(--ok, #1a7f37)' : 'var(--danger, #b42318)',
                              }}
                            >
                              {`${row.direction === 'in' ? '+' : '−'}${quantity(row.baseQty)}`}
                            </td>
                            <td>
                              {row.unitNameAr ?? 'الوحدة الأساسية'}
                              {row.qty !== row.baseQty && (
                                <span
                                  className="muted small"
                                  dir="ltr"
                                >{` (${quantity(row.qty)} ×${Number(row.factor).toLocaleString('ar-EG')})`}</span>
                              )}
                            </td>
                            <td dir="ltr">
                              <strong>{quantity(row.balanceQty)}</strong>
                            </td>
                            <td dir="ltr">{money(row.unitCost)}</td>
                            <td dir="ltr">{money(row.totalCost)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              </>
            ) : (
              <div className="card">
                <p className="muted">—</p>
              </div>
            )
          }
        </QueryView>
      )}
    </Screen>
  );
}
