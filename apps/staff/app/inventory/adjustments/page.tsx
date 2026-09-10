'use client';

import { useState } from 'react';

import { DataTable, Notice, QueryView } from '../../../components/data-view';
import { Screen } from '../../../components/screen';
import { ApiError, apiData, apiList, apiPost } from '../../../lib/api';
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
  shortDate,
  statusLabel,
  type Branch,
  type Item,
  type Warehouse,
} from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

/**
 * جرد وتسوية — a counted variance against the stock ledger.
 *
 * The desktop counted one item at a time and left the accountant to build the entry by
 * hand. The cloud keeps the count as one document with as many lines as the shelf had:
 * every line remembers what the book said, what the count found, and what that
 * difference was worth; posting moves the stock to the counted quantity and writes ONE
 * balanced entry for the net variance — because a count is one decision, not one
 * decision per line.
 */

type Level = { itemId: string; warehouseId: string; quantity: string; averageCost: string };

type AdjustmentLine = {
  lineNo: number;
  itemId: string;
  expectedQty: string;
  countedQty: string;
  varianceQty?: string | null;
  varianceValue?: string | null;
  unitCost?: string | null;
};

type Adjustment = {
  id: string;
  number: string;
  status: string;
  branchId: string;
  warehouseId: string;
  reason: string;
  journalEntryId?: string | null;
  createdAt?: string;
  lines: AdjustmentLine[];
};

export default function StockAdjustmentsPage() {
  const { can } = useSession();
  const branches = useQuery<Branch[]>(() => listBranches(), []);
  const warehouses = useQuery<Warehouse[]>(() => listWarehouses(), []);
  const items = useQuery<Item[]>(() => listItems(), []);

  const branchRows = branches.data ?? [];
  const warehouseRows = warehouses.data ?? [];
  const itemRows = (items.data ?? []).filter((row) => (row.kind ?? 'stock') === 'stock');

  const [open, setOpen] = useState(false);
  const [branchId, setBranchId] = useState('');
  const [warehouseId, setWarehouseId] = useState('');
  const [reason, setReason] = useState('جرد دوري');
  const [lines, setLines] = useState<Array<{ itemId: string; countedText: string; costText: string }>>([
    { itemId: '', countedText: '', costText: '' },
  ]);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger' | 'info'; text: string } | undefined>();
  const [selected, setSelected] = useState<Adjustment | undefined>();

  const effectiveBranch = branchId || defaultOf(branchRows)?.id || '';
  const effectiveWarehouse = warehouseId || defaultOf(warehouseRows)?.id || '';

  const adjustments = useQuery<Adjustment[]>(() => apiList<Adjustment>('/inventory/adjustments'), []);
  /**
   * What the ledger currently says, so the clerk sees the expected column while typing
   * the count instead of discovering the variance afterwards.
   */
  const levels = useQuery<Level[]>(
    () =>
      effectiveWarehouse
        ? apiList<Level>(`/inventory/levels?warehouse_id=${effectiveWarehouse}`)
        : Promise.resolve([]),
    [effectiveWarehouse],
  );
  const levelOf = (itemId: string) => (levels.data ?? []).find((row) => row.itemId === itemId);

  function updateLine(
    index: number,
    patch: Partial<{ itemId: string; countedText: string; costText: string }>,
  ) {
    setLines((current) =>
      current.map((line, position) => (position === index ? { ...line, ...patch } : line)),
    );
  }

  async function reload() {
    await adjustments.reload();
    await levels.reload();
  }

  async function create(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setNotice(undefined);
    try {
      const filled = lines.filter((line) => line.itemId && line.countedText !== '');
      if (!filled.length)
        throw new ApiError(422, 'VALIDATION_FAILED', 'أضف صنفاً واحداً على الأقل بكمية مجرودة.');
      if (!reason.trim()) throw new ApiError(422, 'VALIDATION_FAILED', 'سبب الجرد مطلوب.');
      const created = await apiPost<Adjustment>('/inventory/adjustments', {
        branchId: effectiveBranch,
        warehouseId: effectiveWarehouse,
        reason: reason.trim(),
        lines: filled.map((line) => ({
          itemId: line.itemId,
          countedQty: line.countedText,
          unitCost: line.costText || undefined,
        })),
      });
      setNotice({ kind: 'ok', text: `حُفظ الجرد ${created.number} كمسودة. راجع الفروقات ثم اعتمده.` });
      setLines([{ itemId: '', countedText: '', costText: '' }]);
      setOpen(false);
      await reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  async function post(row: Adjustment) {
    setNotice(undefined);
    try {
      await apiPost(`/inventory/adjustments/${row.id}/post`, { approved: true });
      setNotice({
        kind: 'ok',
        text: `تم اعتماد ${row.number}: حُرّك المخزون إلى الكمية المجرودة وأُنشئ قيد التسوية.`,
      });
      await reload();
      if (selected?.id === row.id) setSelected(await apiData<Adjustment>(`/inventory/adjustments/${row.id}`));
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    }
  }

  const varianceOf = (line: { itemId: string; countedText: string; costText: string }) => {
    const current = Number(levelOf(line.itemId)?.quantity ?? 0);
    const counted = Number(line.countedText || 0);
    return Number.isFinite(counted) ? counted - current : 0;
  };
  const netVariance = lines.reduce((sum, line) => {
    const variance = varianceOf(line);
    const unitValue = Number(line.costText || levelOf(line.itemId)?.averageCost || 0);
    return sum + variance * unitValue;
  }, 0);

  return (
    <Screen
      title="جرد وتسوية المخزون"
      subtitle="عدّ الأرفف، سجّل الفروقات، واعتمد التسوية — قيد واحد متزن بصافي الفرق، وحركة مخزون لكل سطر."
      crumbs={['المستودعات', 'العمليات']}
      actions={
        can('inventory.adjust') ? (
          <button className="btn primary" type="button" onClick={() => setOpen(!open)}>
            {open ? 'إغلاق' : 'جرد جديد'}
          </button>
        ) : null
      }
    >
      {open && (
        <form className="card" onSubmit={create}>
          <h2>جرد جديد</h2>
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
              <span>المستودع *</span>
              <select
                className="input"
                value={effectiveWarehouse}
                onChange={(event) => setWarehouseId(event.target.value)}
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
              <span>السبب *</span>
              <input
                className="input"
                value={reason}
                onChange={(event) => setReason(event.target.value)}
                placeholder="جرد سنوي، جرد مفاجئ…"
                required
              />
            </label>
          </div>

          <h3>الأصناف المجرودة</h3>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>المادة</th>
                  <th>الرصيد الدفتري</th>
                  <th>الكمية المجرودة</th>
                  <th>الفرق</th>
                  <th>تكلفة الوحدة</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {lines.map((line, index) => {
                  const variance = varianceOf(line);
                  return (
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
                      <td dir="ltr">{quantity(levelOf(line.itemId)?.quantity ?? '0')}</td>
                      <td>
                        <input
                          className="input"
                          dir="ltr"
                          inputMode="decimal"
                          value={line.countedText}
                          onChange={(event) => updateLine(index, { countedText: event.target.value })}
                        />
                      </td>
                      <td
                        dir="ltr"
                        style={{
                          color:
                            variance === 0
                              ? undefined
                              : variance > 0
                                ? 'var(--ok, #1a7f37)'
                                : 'var(--danger, #b42318)',
                        }}
                      >
                        {line.itemId && line.countedText !== ''
                          ? variance > 0
                            ? `+${quantity(variance)}`
                            : quantity(variance)
                          : '—'}
                      </td>
                      <td>
                        <input
                          className="input"
                          dir="ltr"
                          inputMode="decimal"
                          value={line.costText}
                          placeholder={levelOf(line.itemId)?.averageCost ?? ''}
                          onChange={(event) => updateLine(index, { costText: event.target.value })}
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
                  );
                })}
              </tbody>
            </table>
          </div>
          <div className="row" style={{ gap: 8 }}>
            <button
              className="btn sm"
              type="button"
              onClick={() =>
                setLines((current) => [...current, { itemId: '', countedText: '', costText: '' }])
              }
            >
              + سطر
            </button>
            <span className="muted">
              صافي قيمة الفروقات:{' '}
              <strong dir="ltr">{money(String(Math.round(netVariance * 10000) / 10000))}</strong>
            </span>
          </div>

          <Notice notice={notice} />
          <button className="btn primary" type="submit" disabled={busy}>
            {busy ? 'جارٍ الحفظ…' : 'حفظ الجرد كمسودة'}
          </button>
        </form>
      )}

      {!open && <Notice notice={notice} />}

      <QueryView
        query={adjustments}
        empty="لا توجد عمليات جرد"
        emptyDetail="أنشئ جرداً جديداً لتسوية المخزون على الأرفف."
      >
        {(rows) => (
          <DataTable
            rows={rows}
            rowKey={(row) => row.id}
            columns={[
              { key: 'number', header: 'الرقم', align: 'ltr', cell: (row) => row.number },
              { key: 'reason', header: 'السبب', cell: (row) => row.reason },
              {
                key: 'warehouse',
                header: 'المستودع',
                cell: (row) =>
                  arabicName(warehouseRows.find((warehouse) => warehouse.id === row.warehouseId) ?? {}),
              },
              { key: 'lines', header: 'الأصناف', align: 'num', cell: (row) => row.lines.length },
              {
                key: 'variance',
                header: 'صافي الفرق',
                align: 'num',
                cell: (row) =>
                  quantity(row.lines.reduce((sum, line) => sum + Number(line.varianceQty ?? 0), 0)),
              },
              {
                key: 'value',
                header: 'قيمة الفرق',
                align: 'num',
                cell: (row) =>
                  money(
                    String(
                      Math.round(
                        row.lines.reduce(
                          (sum, line) =>
                            sum +
                            (Number(line.varianceQty ?? 0) >= 0 ? 1 : -1) * Number(line.varianceValue ?? 0),
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
                  <span className={`badge ${row.status === 'posted' ? 'ok' : ''}`}>
                    {statusLabel(row.status)}
                  </span>
                ),
              },
              {
                key: 'actions',
                header: '',
                cell: (row) => (
                  <span className="row" style={{ gap: 6 }}>
                    <button className="btn sm" type="button" onClick={() => setSelected(row)}>
                      تفاصيل
                    </button>
                    {row.status === 'draft' && can('inventory.adjust') && (
                      <button className="btn sm primary" type="button" onClick={() => void post(row)}>
                        اعتماد وترحيل
                      </button>
                    )}
                  </span>
                ),
              },
            ]}
          />
        )}
      </QueryView>

      {selected && (
        <section className="card">
          <h2>
            تفاصيل الجرد {selected.number} <span className="muted">({statusLabel(selected.status)})</span>
          </h2>
          <p className="muted">
            {selected.reason} ·{' '}
            {arabicName(warehouseRows.find((warehouse) => warehouse.id === selected.warehouseId) ?? {})} ·{' '}
            {dateTime(selected.createdAt)}
            {selected.journalEntryId ? ' · له قيد تسوية' : ''}
          </p>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>#</th>
                  <th>المادة</th>
                  <th>الرصيد الدفتري</th>
                  <th>المجرود</th>
                  <th>الفرق</th>
                  <th>قيمة الفرق</th>
                </tr>
              </thead>
              <tbody>
                {selected.lines.map((line) => (
                  <tr key={line.lineNo}>
                    <td dir="ltr">{line.lineNo}</td>
                    <td>
                      {itemLabel(
                        itemRows.find((row) => row.id === line.itemId) ??
                          ({ id: line.itemId, sku: '—' } as Item),
                      )}
                    </td>
                    <td dir="ltr">{quantity(line.expectedQty)}</td>
                    <td dir="ltr">{quantity(line.countedQty)}</td>
                    <td dir="ltr">
                      {line.varianceQty === null || line.varianceQty === undefined
                        ? '—'
                        : quantity(line.varianceQty)}
                    </td>
                    <td dir="ltr">{money(line.varianceValue)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <button className="btn sm" type="button" onClick={() => setSelected(undefined)}>
            إغلاق التفاصيل
          </button>
          <p className="muted" style={{ fontSize: 13 }}>
            آخر تحديث: {shortDate(new Date())}
          </p>
        </section>
      )}
    </Screen>
  );
}
