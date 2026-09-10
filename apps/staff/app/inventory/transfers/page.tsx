'use client';

import { useState } from 'react';

import { DataTable, Notice, QueryView } from '../../../components/data-view';
import { Screen } from '../../../components/screen';
import { ApiError, apiList, apiPost } from '../../../lib/api';
import {
  arabicName,
  dateTime,
  defaultOf,
  itemLabel,
  listBranches,
  listItems,
  listWarehouses,
  money,
  quantity,
  statusLabel,
  type Branch,
  type Item,
  type Warehouse,
} from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type TransferLine = { lineNo: number; itemId: string; qty: string; receivedQty: string; unitCost: string };
type Transfer = {
  id: string;
  number: string;
  status: string;
  fromWarehouseId: string;
  toWarehouseId: string;
  createdAt?: string;
  lines: TransferLine[];
};

const TRANSFER_STATUS: Record<string, string> = {
  draft: 'مسودة',
  in_transit: 'في الطريق',
  partially_received: 'مستلم جزئياً',
  received: 'مستلم',
  cancelled: 'ملغاة',
};

export default function TransfersPage() {
  const { can } = useSession();
  const transfers = useQuery<Transfer[]>(() => apiList<Transfer>('/inventory/transfers'), []);
  const branches = useQuery<Branch[]>(() => listBranches(), []);
  const warehouses = useQuery<Warehouse[]>(() => listWarehouses(), []);
  const items = useQuery<Item[]>(() => listItems(), []);
  const branchRows = branches.data ?? [];
  const warehouseRows = warehouses.data ?? [];
  const itemRows = (items.data ?? []).filter((row) => (row.kind ?? 'stock') === 'stock');

  /**
   * The branch is what lets the transfer find a posting profile, so the goods keep a
   * value while they are on the road: Dr بضاعة تحت التحويل / Cr المخزون on send, and the
   * mirror of it on receipt.
   */
  const [branchId, setBranchId] = useState('');
  const effectiveBranch = branchId || defaultOf(branchRows)?.id || '';

  const [open, setOpen] = useState(false);
  const [fromWarehouseId, setFrom] = useState('');
  const [toWarehouseId, setTo] = useState('');
  const [lines, setLines] = useState<Array<{ itemId: string; qtyText: string; unitCostText: string }>>([
    { itemId: '', qtyText: '', unitCostText: '' },
  ]);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();

  const nameOfWarehouse = (id: string) => {
    const warehouse = warehouseRows.find((row) => row.id === id);
    return warehouse ? arabicName(warehouse) : id;
  };

  function updateLine(
    index: number,
    patch: Partial<{ itemId: string; qtyText: string; unitCostText: string }>,
  ) {
    setLines((current) =>
      current.map((line, position) => (position === index ? { ...line, ...patch } : line)),
    );
  }

  async function createTransfer(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setNotice(undefined);
    try {
      const filled = lines.filter((line) => line.itemId && Number(line.qtyText) > 0);
      if (filled.length === 0)
        throw new ApiError(422, 'VALIDATION_FAILED', 'أضف سطراً واحداً على الأقل بكمية أكبر من صفر.');
      // No client-side number: the API allocates `TR-…` from the document sequence, so the
      // series stays gap-free and unique even with two users saving at once.
      await apiPost('/inventory/transfers/draft', {
        branchId: effectiveBranch,
        fromWarehouseId,
        toWarehouseId,
        lines: filled.map((line) => ({
          itemId: line.itemId,
          qty: line.qtyText,
          unitCost: line.unitCostText || undefined,
        })),
      });
      setNotice({ kind: 'ok', text: 'تم إنشاء المناقلة كمسودة. أرسِلها لخصم الكمية من المستودع المصدر.' });
      setLines([{ itemId: '', qtyText: '', unitCostText: '' }]);
      transfers.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  async function act(action: () => Promise<unknown>, okText: string) {
    setNotice(undefined);
    try {
      await action();
      setNotice({ kind: 'ok', text: okText });
      transfers.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    }
  }

  return (
    <Screen
      title="مناقلة مخزنية"
      subtitle="مسودة ← إرسال (بضاعة تحت التحويل) ← استلام (إدخال للمخزون) — المناقلة محايدة القيمة: ما يخرج من مستودع يدخل الآخر بنفس التكلفة."
      crumbs={['المستودعات', 'العمليات']}
      actions={
        can('inventory.adjust') ? (
          <button className="btn primary" type="button" onClick={() => setOpen(!open)}>
            {open ? 'إغلاق' : 'مناقلة جديدة'}
          </button>
        ) : null
      }
    >
      {open && (
        <form className="card" onSubmit={createTransfer}>
          <h2>مناقلة جديدة</h2>
          <div className="form-grid">
            <label className="field">
              <span>الفرع *</span>
              <select
                className="input"
                value={effectiveBranch}
                onChange={(event) => setBranchId(event.target.value)}
                required
              >
                <option value="">— اختر —</option>
                {branchRows.map((row) => (
                  <option key={row.id} value={row.id}>
                    {row.code ? `${row.code} — ` : ''}
                    {arabicName(row)}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>من مستودع *</span>
              <select
                className="input"
                value={fromWarehouseId}
                onChange={(event) => setFrom(event.target.value)}
                required
              >
                <option value="">— اختر —</option>
                {warehouseRows.map((row) => (
                  <option key={row.id} value={row.id}>
                    {arabicName(row)}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>إلى مستودع *</span>
              <select
                className="input"
                value={toWarehouseId}
                onChange={(event) => setTo(event.target.value)}
                required
              >
                <option value="">— اختر —</option>
                {warehouseRows
                  .filter((row) => row.id !== fromWarehouseId)
                  .map((row) => (
                    <option key={row.id} value={row.id}>
                      {arabicName(row)}
                    </option>
                  ))}
              </select>
            </label>
          </div>

          <h3>الأصناف</h3>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>المادة</th>
                  <th>الكمية</th>
                  <th>تكلفة الوحدة</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {lines.map((line, index) => (
                  <tr key={index}>
                    <td>
                      <select
                        className="input"
                        value={line.itemId}
                        onChange={(event) => updateLine(index, { itemId: event.target.value })}
                      >
                        <option value="">— اختر —</option>
                        {itemRows.map((row) => (
                          <option key={row.id} value={row.id}>
                            {itemLabel(row)}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td>
                      <input
                        className="input"
                        dir="ltr"
                        inputMode="decimal"
                        value={line.qtyText}
                        onChange={(event) => updateLine(index, { qtyText: event.target.value })}
                      />
                    </td>
                    <td>
                      <input
                        className="input"
                        dir="ltr"
                        inputMode="decimal"
                        value={line.unitCostText}
                        onChange={(event) => updateLine(index, { unitCostText: event.target.value })}
                      />
                    </td>
                    <td>
                      <button
                        className="btn sm"
                        type="button"
                        onClick={() =>
                          setLines((current) => current.filter((_, position) => position !== index))
                        }
                      >
                        حذف
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <button
            className="btn sm"
            type="button"
            onClick={() => setLines((current) => [...current, { itemId: '', qtyText: '', unitCostText: '' }])}
          >
            + سطر
          </button>

          <Notice notice={notice} />
          <button className="btn primary" type="submit" disabled={busy}>
            {busy ? 'جارٍ الحفظ…' : 'حفظ كمسودة'}
          </button>
        </form>
      )}

      {!open && <Notice notice={notice} />}

      <QueryView query={transfers} empty="لا توجد مناقلات" emptyDetail="أنشئ مناقلة لنقل بضاعة بين مستودعين.">
        {(rows) => (
          <DataTable
            rows={rows}
            rowKey={(row) => row.id}
            columns={[
              { key: 'number', header: 'الرقم', align: 'ltr', cell: (row) => row.number },
              { key: 'from', header: 'من', cell: (row) => nameOfWarehouse(row.fromWarehouseId) },
              { key: 'to', header: 'إلى', cell: (row) => nameOfWarehouse(row.toWarehouseId) },
              { key: 'lines', header: 'الأسطر', align: 'num', cell: (row) => row.lines.length },
              {
                key: 'qty',
                header: 'إجمالي الكمية',
                align: 'num',
                cell: (row) => quantity(row.lines.reduce((sum, line) => sum + Number(line.qty), 0)),
              },
              {
                key: 'value',
                header: 'القيمة',
                align: 'num',
                cell: (row) =>
                  money(
                    String(
                      Math.round(
                        row.lines.reduce(
                          (sum, line) => sum + Number(line.qty) * Number(line.unitCost ?? 0),
                          0,
                        ) * 10000,
                      ) / 10000,
                    ),
                  ),
              },
              {
                key: 'status',
                header: 'الحالة',
                cell: (row) => (
                  <span
                    className={`badge ${row.status === 'received' ? 'ok' : row.status === 'cancelled' ? 'danger' : ''}`}
                  >
                    {TRANSFER_STATUS[row.status] ?? statusLabel(row.status)}
                  </span>
                ),
              },
              { key: 'created', header: 'أُنشئت', align: 'ltr', cell: (row) => dateTime(row.createdAt) },
              {
                key: 'actions',
                header: '',
                cell: (row) => (
                  <span className="row">
                    {row.status === 'draft' && can('inventory.adjust') && (
                      <button
                        className="btn sm"
                        type="button"
                        onClick={() =>
                          act(
                            () => apiPost(`/inventory/transfers/${row.id}/send`, {}),
                            'تم إرسال المناقلة وخُصمت الكمية من المصدر.',
                          )
                        }
                      >
                        إرسال
                      </button>
                    )}
                    {['in_transit', 'partially_received'].includes(row.status) && can('inventory.adjust') && (
                      <button
                        className="btn sm primary"
                        type="button"
                        onClick={() =>
                          act(
                            () =>
                              apiPost(`/inventory/transfers/${row.id}/receive`, {
                                received: row.lines
                                  .map((line) => ({
                                    lineNo: line.lineNo,
                                    qty: String(Number(line.qty) - Number(line.receivedQty)),
                                  }))
                                  .filter((line) => Number(line.qty) > 0),
                              }),
                            'تم استلام المناقلة: دخلت البضاعة المخزون الهدف وخلا حساب بضاعة تحت التحويل.',
                          )
                        }
                      >
                        استلام كامل
                      </button>
                    )}
                    {['draft', 'in_transit'].includes(row.status) && can('inventory.adjust') && (
                      <button
                        className="btn sm danger"
                        type="button"
                        onClick={() => {
                          const why =
                            row.status === 'in_transit'
                              ? window.prompt(`إلغاء مناقلة في الطريق ${row.number} — يرجى ذكر السبب:`)
                              : '';
                          if (row.status !== 'in_transit' || (why && why.trim())) {
                            void act(
                              () =>
                                apiPost(
                                  `/inventory/transfers/${row.id}/cancel`,
                                  why && why.trim() ? { reason: why.trim() } : {},
                                ),
                              row.status === 'in_transit'
                                ? 'أُلغيت المناقلة وعادت البضاعة إلى المستودع المصدر.'
                                : 'تم إلغاء المناقلة.',
                            );
                          }
                        }}
                      >
                        إلغاء
                      </button>
                    )}
                  </span>
                ),
              },
            ]}
          />
        )}
      </QueryView>
    </Screen>
  );
}
