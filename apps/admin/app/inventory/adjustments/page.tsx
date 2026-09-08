'use client';

import { useState } from 'react';

import { Notice } from '../../../components/data-view';
import { Screen } from '../../../components/screen';
import { accountLabel, listAccounts, postableOf, type Account } from '../../../lib/accounts';
import { ApiError, apiList, apiPost } from '../../../lib/api';
import { arabicName, itemLabel, listItems, listWarehouses, money, quantity, today, type Item, type Warehouse } from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type Level = { itemId: string; warehouseId: string; quantity: string; averageCost: string };

/**
 * A stock adjustment is never only a stock movement: the API refuses to post one without
 * an approved journal entry behind it. This screen therefore does the two steps the
 * desktop product does in one dialog — value the difference, post the journal, then post
 * the adjustment — and shows the resulting entry before anything is written.
 */
export default function StockAdjustmentsPage() {
  const { can } = useSession();
  const items = useQuery<Item[]>(() => listItems(), []);
  const warehouses = useQuery<Warehouse[]>(() => listWarehouses(), []);
  const accounts = useQuery<Account[]>(() => listAccounts(), []);

  const [itemId, setItemId] = useState('');
  const [warehouseId, setWarehouseId] = useState('');
  const [countedText, setCountedText] = useState('');
  const [unitCostText, setUnitCostText] = useState('');
  const [inventoryAccountId, setInventoryAccountId] = useState('');
  const [counterAccountId, setCounterAccountId] = useState('');
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger' | 'info'; text: string } | undefined>();

  const levels = useQuery<Level[]>(
    () => (itemId && warehouseId ? apiList<Level>(`/inventory/levels?item_id=${itemId}&warehouse_id=${warehouseId}`) : Promise.resolve([])),
    [itemId, warehouseId],
  );

  const current = (levels.data ?? [])[0];
  const currentQty = Number(current?.quantity ?? 0);
  const counted = Number(countedText);
  const delta = Number.isFinite(counted) ? counted - currentQty : 0;
  const unitCost = Number(unitCostText || current?.averageCost || 0);
  const deltaValue = Math.abs(delta) * unitCost;
  const postable = (accounts.data ?? []).filter((account) => postableOf(account));

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setNotice(undefined);
    try {
      if (delta === 0) throw new ApiError(422, 'VALIDATION_FAILED', 'الكمية المجرودة تساوي الرصيد الحالي — لا حاجة لتسوية.');
      const increase = delta > 0;
      const journal = await apiPost<{ id: string; number: string }>('/journal-entries', {
        date: today(),
        description: `تسوية مخزنية — ${itemLabel((items.data ?? []).find((row) => row.id === itemId) ?? { id: itemId, sku: itemId })}`,
        lines: [
          { accountId: increase ? inventoryAccountId : counterAccountId, debit: deltaValue.toFixed(4), description: 'تسوية مخزنية' },
          { accountId: increase ? counterAccountId : inventoryAccountId, credit: deltaValue.toFixed(4), description: 'تسوية مخزنية' },
        ],
      });

      await apiPost('/inventory/adjustments/post', {
        adjustmentId: crypto.randomUUID(),
        itemId,
        warehouseId,
        countedQty: String(counted),
        unitCost: unitCost ? String(unitCost) : undefined,
        approved: true,
        journalEntryId: journal.id,
      });

      setNotice({ kind: 'ok', text: `تمت التسوية وتم ترحيل القيد ${journal.number}.` });
      setCountedText('');
      levels.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  if (!can('inventory.adjust')) {
    return (
      <Screen title="تسوية مخزنية" crumbs={['المستودعات', 'العمليات']}>
        <div className="card state">
          <strong>لا تملك صلاحية إجراء التسويات المخزنية</strong>
        </div>
      </Screen>
    );
  }

  return (
    <Screen
      title="تسوية مخزنية"
      subtitle="تعديل رصيد مادة في مستودع بعد الجرد الفعلي، مع ترحيل قيد الفرق تلقائياً."
      crumbs={['المستودعات', 'العمليات']}
    >
      <form className="card" onSubmit={submit}>
        <div className="form-grid">
          <label className="field">
            <span>المادة *</span>
            <select className="input" value={itemId} onChange={(event) => setItemId(event.target.value)} required>
              <option value="">— اختر —</option>
              {(items.data ?? []).map((row) => (
                <option key={row.id} value={row.id}>
                  {itemLabel(row)}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>المستودع *</span>
            <select className="input" value={warehouseId} onChange={(event) => setWarehouseId(event.target.value)} required>
              <option value="">— اختر —</option>
              {(warehouses.data ?? []).map((row) => (
                <option key={row.id} value={row.id}>
                  {arabicName(row)}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>الكمية المجرودة *</span>
            <input className="input" dir="ltr" inputMode="decimal" value={countedText} onChange={(event) => setCountedText(event.target.value)} required />
          </label>
          <label className="field">
            <span>تكلفة الوحدة</span>
            <input
              className="input"
              dir="ltr"
              inputMode="decimal"
              placeholder={current?.averageCost ?? '0'}
              value={unitCostText}
              onChange={(event) => setUnitCostText(event.target.value)}
            />
            <span className="muted small">افتراضياً متوسط التكلفة الحالي.</span>
          </label>
          <label className="field">
            <span>حساب المخزون *</span>
            <select className="input" value={inventoryAccountId} onChange={(event) => setInventoryAccountId(event.target.value)} required>
              <option value="">— اختر —</option>
              {postable.map((account) => (
                <option key={account.id} value={account.id}>
                  {accountLabel(account)}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>الحساب المقابل (فروقات الجرد) *</span>
            <select className="input" value={counterAccountId} onChange={(event) => setCounterAccountId(event.target.value)} required>
              <option value="">— اختر —</option>
              {postable.map((account) => (
                <option key={account.id} value={account.id}>
                  {accountLabel(account)}
                </option>
              ))}
            </select>
          </label>
        </div>

        {itemId && warehouseId && (
          <dl className="kv">
            <dt>الرصيد الحالي</dt>
            <dd>{quantity(currentQty)}</dd>
            <dt>الفرق</dt>
            <dd>{delta === 0 ? '—' : `${delta > 0 ? '+' : ''}${quantity(delta)}`}</dd>
            <dt>قيمة الفرق</dt>
            <dd>{money(deltaValue)}</dd>
          </dl>
        )}

        <Notice notice={notice} />
        <button className="btn primary" type="submit" disabled={busy}>
          {busy ? 'جارٍ الترحيل…' : 'ترحيل التسوية'}
        </button>
      </form>
    </Screen>
  );
}
