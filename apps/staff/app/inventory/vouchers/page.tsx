'use client';

import { useSearchParams } from 'next/navigation';
import { useState } from 'react';

import { DataTable, Notice, QueryView } from '../../../components/data-view';
import { Screen } from '../../../components/screen';
import { accountLabel, listAccounts, postableOf, type Account } from '../../../lib/accounts';
import { ApiError, apiData, apiList, apiPost } from '../../../lib/api';
import {
  amountLabel,
  arabicName,
  dateTime,
  defaultOf,
  itemLabel,
  listBranches,
  listItems,
  listLots,
  listSerials,
  listWarehouses,
  money,
  quantity,
  shortDate,
  statusLabel,
  today,
  type Branch,
  type Item,
  type Lot,
  type Serial,
  type Warehouse,
} from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

/**
 * سند إدخال / إخراج مخزني — and بضاعة أول المدة as its third kind.
 *
 * The desktop (`frmInvInOutput`) saved these documents with no journal at all and let
 * the accountant repair the ledger later with `frmReGenerateEntries`. This screen is
 * the cloud replacement: a numbered, branch-scoped document that is saved as a draft,
 * then posted — which moves the stock *and* writes the balanced entry in one step, so
 * the inventory account can never drift away from the stock ledger.
 */

type VoucherLine = {
  lineNo: number;
  itemId: string;
  qty: string;
  unitCost?: string | null;
  lineCost?: string | null;
  lotId?: string | null;
  serialId?: string | null;
  note?: string | null;
};

type Voucher = {
  id: string;
  number: string;
  kind: string;
  status: string;
  branchId: string;
  warehouseId: string;
  voucherDate: string;
  reason?: string | null;
  totalCost: string;
  journalEntryId?: string | null;
  createdAt?: string;
  lines: VoucherLine[];
};

const KINDS = [
  { id: 'stock_in', label: 'سند إدخال', hint: 'زيادة المخزون بكمية وتكلفة معلومتين (إيداع، مرتجع، هدية…)' },
  { id: 'stock_out', label: 'سند إخراج', hint: 'صرف كمية بمتوسط التكلفة (تالف، هالك، عينة، استهلاك داخلي…)' },
  { id: 'opening', label: 'بضاعة أول المدة', hint: 'رصيد الافتتاح — يقيد في حساب بضاعة أول المدة' },
] as const;

type KindId = (typeof KINDS)[number]['id'];

const KIND_LABELS: Record<string, string> = { stock_in: 'إدخال', stock_out: 'إخراج', opening: 'أول المدة' };
const PREFIXES: Record<string, string> = { stock_in: 'SIN', stock_out: 'SOU', opening: 'OP' };

export default function VouchersPage() {
  const params = useSearchParams();
  const requested = params.get('kind');
  const { can } = useSession();

  const [kind, setKind] = useState<KindId>(
    requested === 'opening' || requested === 'stock_out' ? (requested as KindId) : 'stock_in',
  );
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<Voucher | undefined>();
  const [branchId, setBranchId] = useState('');
  const [warehouseId, setWarehouseId] = useState('');
  const [voucherDate, setVoucherDate] = useState(today());
  const [reason, setReason] = useState('');
  const [counterAccountId, setCounterAccountId] = useState('');
  const [allowNegative, setAllowNegative] = useState(false);
  const [lines, setLines] = useState<
    Array<{
      itemId: string;
      qtyText: string;
      costText: string;
      lotId: string;
      serialId: string;
      note: string;
    }>
  >([{ itemId: '', qtyText: '', costText: '', lotId: '', serialId: '', note: '' }]);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();

  const branches = useQuery<Branch[]>(() => listBranches(), []);
  const warehouses = useQuery<Warehouse[]>(() => listWarehouses(), []);
  const items = useQuery<Item[]>(() => listItems(), []);
  const accounts = useQuery<Account[]>(() => listAccounts(), []);
  const lots = useQuery<Lot[]>(() => listLots(), []);
  const serials = useQuery<Serial[]>(() => listSerials(), []);

  const branchRows = branches.data ?? [];
  const warehouseRows = warehouses.data ?? [];
  const itemRows = (items.data ?? []).filter((row) => (row.kind ?? 'stock') === 'stock');
  const accountRows = (accounts.data ?? []).filter((row) => postableOf(row));
  const lotRows = lots.data ?? [];
  const serialRows = serials.data ?? [];

  const effectiveBranch = branchId || defaultOf(branchRows)?.id || '';
  const effectiveWarehouse = warehouseId || defaultOf(warehouseRows)?.id || '';

  const vouchers = useQuery<Voucher[]>(
    () => apiList<Voucher>(`/inventory/vouchers${kind ? `?kind=${kind}` : ''}`),
    [kind],
  );

  const isIssue = kind === 'stock_out';
  const lineTotal = lines.reduce(
    (sum, line) => sum + Number(line.qtyText || 0) * Number(line.costText || 0),
    0,
  );

  function itemOf(id: string) {
    return itemRows.find((row) => row.id === id);
  }

  function updateLine(
    index: number,
    patch: Partial<{
      itemId: string;
      qtyText: string;
      costText: string;
      lotId: string;
      serialId: string;
      note: string;
    }>,
  ) {
    setLines((current) =>
      current.map((line, position) => (position === index ? { ...line, ...patch } : line)),
    );
  }

  async function reload() {
    await vouchers.reload();
    if (selected) {
      try {
        setSelected(await apiData<Voucher>(`/inventory/vouchers/${selected.id}`));
      } catch {
        setSelected(undefined);
      }
    }
  }

  async function createVoucher(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setNotice(undefined);
    try {
      const filled = lines.filter((line) => line.itemId && Number(line.qtyText) > 0);
      if (filled.length === 0)
        throw new ApiError(422, 'VALIDATION_FAILED', 'أضف سطراً واحداً على الأقل بكمية أكبر من صفر.');
      if (!isIssue && filled.some((line) => Number(line.costText) < 0)) {
        throw new ApiError(422, 'VALIDATION_FAILED', 'تكلفة الوحدة لا يمكن أن تكون سالبة.');
      }
      const created = await apiPost<Voucher>('/inventory/vouchers', {
        branchId: effectiveBranch,
        warehouseId: effectiveWarehouse,
        kind,
        voucherDate,
        reason: reason || undefined,
        counterAccountId: counterAccountId || undefined,
        lines: filled.map((line) => ({
          itemId: line.itemId,
          qty: line.qtyText,
          unitCost: isIssue ? undefined : line.costText || undefined,
          lotId: line.lotId || undefined,
          serialId: line.serialId || undefined,
          note: line.note || undefined,
        })),
      });
      setNotice({
        kind: 'ok',
        text: `حُفظ السند ${created.number} كمسودة. رحّله ليحرّك المخزون ويُنشئ القيد.`,
      });
      setLines([{ itemId: '', qtyText: '', costText: '', lotId: '', serialId: '', note: '' }]);
      setReason('');
      setOpen(false);
      await reload();
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
      await reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    }
  }

  return (
    <Screen
      title="سندات المخزون"
      subtitle="إدخال · إخراج · بضاعة أول المدة — كل سند مرحّل ينشئ قيداً متزناً في نفس اللحظة التي يحرك فيها المخزون."
      crumbs={['المستودعات', 'العمليات']}
      actions={
        can('inventory.adjust') ? (
          <button className="btn primary" type="button" onClick={() => setOpen(!open)}>
            {open ? 'إغلاق' : 'سند جديد'}
          </button>
        ) : null
      }
    >
      <div className="row" style={{ gap: 8, flexWrap: 'wrap' }}>
        {KINDS.map((option) => (
          <button
            key={option.id}
            type="button"
            className={option.id === kind ? 'btn primary' : 'btn'}
            onClick={() => {
              setKind(option.id);
              setNotice(undefined);
            }}
          >
            {option.label}
          </button>
        ))}
      </div>
      <p className="muted" style={{ marginTop: 6 }}>
        {KINDS.find((option) => option.id === kind)?.hint}
      </p>

      {open && (
        <form className="card" onSubmit={createVoucher}>
          <h2>
            سند {KIND_LABELS[kind]} جديد <span className="muted">({PREFIXES[kind]}…)</span>
          </h2>
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
              <span>التاريخ</span>
              <input
                className="input"
                type="date"
                dir="ltr"
                value={voucherDate}
                onChange={(event) => setVoucherDate(event.target.value)}
              />
            </label>
            <label className="field">
              <span>السبب</span>
              <input
                className="input"
                value={reason}
                onChange={(event) => setReason(event.target.value)}
                placeholder="تالف، هالك، عينة، افتتاح…"
              />
            </label>
            <label className="field">
              <span>الحساب المقابل (اختياري)</span>
              <select
                className="input"
                value={counterAccountId}
                onChange={(event) => setCounterAccountId(event.target.value)}
              >
                <option value="">— من دليل الحسابات الافتراضي —</option>
                {accountRows.map((row) => (
                  <option key={row.id} value={row.id}>
                    {accountLabel(row)}
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
                  {!isIssue && <th>تكلفة الوحدة</th>}
                  <th>دفعة</th>
                  <th>رقم تسلسلي</th>
                  <th>ملاحظة</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {lines.map((line, index) => {
                  const item = itemOf(line.itemId);
                  return (
                    <tr key={index}>
                      <td>
                        <select
                          className="input"
                          value={line.itemId}
                          onChange={(event) =>
                            updateLine(index, { itemId: event.target.value, lotId: '', serialId: '' })
                          }
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
                      {!isIssue && (
                        <td>
                          <input
                            className="input"
                            dir="ltr"
                            inputMode="decimal"
                            value={line.costText}
                            onChange={(event) => updateLine(index, { costText: event.target.value })}
                          />
                        </td>
                      )}
                      <td>
                        <select
                          className="input"
                          value={line.lotId}
                          onChange={(event) => updateLine(index, { lotId: event.target.value })}
                          disabled={!item || !item.trackLot}
                        >
                          <option value="">—</option>
                          {lotRows
                            .filter((lot) => lot.itemId === line.itemId)
                            .map((lot) => (
                              <option key={lot.id} value={lot.id}>
                                {lot.lotNo}
                                {lot.expiryDate ? ` (${shortDate(lot.expiryDate)})` : ''}
                              </option>
                            ))}
                        </select>
                      </td>
                      <td>
                        <select
                          className="input"
                          value={line.serialId}
                          onChange={(event) => updateLine(index, { serialId: event.target.value })}
                          disabled={!item || !item.trackSerial}
                        >
                          <option value="">—</option>
                          {serialRows
                            .filter((serial) => serial.itemId === line.itemId)
                            .map((serial) => (
                              <option key={serial.id} value={serial.id}>
                                {serial.serialNo}
                              </option>
                            ))}
                        </select>
                      </td>
                      <td>
                        <input
                          className="input"
                          value={line.note}
                          onChange={(event) => updateLine(index, { note: event.target.value })}
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
                setLines((current) => [
                  ...current,
                  { itemId: '', qtyText: '', costText: '', lotId: '', serialId: '', note: '' },
                ])
              }
            >
              + سطر
            </button>
            {!isIssue && (
              <span className="muted">
                الإجمالي التقديري: <strong dir="ltr">{money(amountLabel(lineTotal))}</strong>
              </span>
            )}
            {isIssue && (
              <span className="muted">يُصرف بمتوسط التكلفة — تُحسب القيمة تلقائياً عند الترحيل.</span>
            )}
          </div>

          <Notice notice={notice} />
          <button className="btn primary" type="submit" disabled={busy}>
            {busy ? 'جارٍ الحفظ…' : 'حفظ كمسودة'}
          </button>
        </form>
      )}

      {!open && <Notice notice={notice} />}

      <QueryView
        query={vouchers}
        empty="لا توجد سندات"
        emptyDetail="أنشئ سند إدخال أو إخراج لتحريك المخزون بقيد متزن."
      >
        {(rows) => (
          <DataTable
            rows={rows}
            rowKey={(row) => row.id}
            columns={[
              { key: 'number', header: 'الرقم', align: 'ltr', cell: (row) => row.number },
              { key: 'date', header: 'التاريخ', align: 'ltr', cell: (row) => shortDate(row.voucherDate) },
              {
                key: 'warehouse',
                header: 'المستودع',
                cell: (row) =>
                  arabicName(warehouseRows.find((warehouse) => warehouse.id === row.warehouseId) ?? {}),
              },
              { key: 'reason', header: 'السبب', cell: (row) => row.reason ?? '—' },
              { key: 'lines', header: 'الأسطر', align: 'num', cell: (row) => row.lines.length },
              {
                key: 'qty',
                header: 'الكمية',
                align: 'num',
                cell: (row) => quantity(row.lines.reduce((sum, line) => sum + Number(line.qty), 0)),
              },
              { key: 'value', header: 'القيمة', align: 'num', cell: (row) => money(row.totalCost) },
              {
                key: 'status',
                header: 'الحالة',
                cell: (row) => (
                  <span
                    className={`badge ${row.status === 'posted' ? 'ok' : row.status === 'voided' ? 'danger' : ''}`}
                  >
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
                      <button
                        className="btn sm primary"
                        type="button"
                        onClick={() =>
                          act(
                            () => apiPost(`/inventory/vouchers/${row.id}/post`, { allowNegative }),
                            `تم ترحيل ${row.number} وتحريك المخزون وإنشاء القيد.`,
                          )
                        }
                      >
                        ترحيل
                      </button>
                    )}
                    {row.status === 'posted' && can('inventory.adjust') && (
                      <button
                        className="btn sm danger"
                        type="button"
                        onClick={() => {
                          const why = window.prompt(`سبب إلغاء السند ${row.number}:`);
                          if (why && why.trim()) {
                            void act(
                              () => apiPost(`/inventory/vouchers/${row.id}/void`, { reason: why.trim() }),
                              `تم إلغاء ${row.number} وعكس قيده.`,
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

      {can('inventory.negative.override') && (
        <label className="field" style={{ maxWidth: 420 }}>
          <span>
            <input
              type="checkbox"
              checked={allowNegative}
              onChange={(event) => setAllowNegative(event.target.checked)}
            />{' '}
            السماح بالصرف تحت الصفر
          </span>
          <span className="muted">صالح فقط للجلسات التي تملك صلاحية تجاوز الرصيد السالب.</span>
        </label>
      )}

      {selected && (
        <section className="card">
          <h2>
            تفاصيل السند {selected.number}{' '}
            <span className="muted">({KIND_LABELS[selected.kind] ?? selected.kind})</span>
          </h2>
          <p className="muted">
            {shortDate(selected.voucherDate)} ·{' '}
            {arabicName(warehouseRows.find((warehouse) => warehouse.id === selected.warehouseId) ?? {})} ·{' '}
            {statusLabel(selected.status)}
            {selected.journalEntryId ? ' · له قيد محاسبي' : ''}
          </p>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>#</th>
                  <th>المادة</th>
                  <th>الكمية</th>
                  <th>تكلفة الوحدة</th>
                  <th>القيمة</th>
                  <th>ملاحظة</th>
                </tr>
              </thead>
              <tbody>
                {selected.lines.map((line) => (
                  <tr key={line.lineNo}>
                    <td dir="ltr">{line.lineNo}</td>
                    <td>{itemLabel(itemOf(line.itemId) ?? ({ id: line.itemId, sku: '—' } as Item))}</td>
                    <td dir="ltr">{quantity(line.qty)}</td>
                    <td dir="ltr">{money(line.unitCost)}</td>
                    <td dir="ltr">{money(line.lineCost)}</td>
                    <td>{line.note ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <button className="btn sm" type="button" onClick={() => setSelected(undefined)}>
            إغلاق التفاصيل
          </button>
        </section>
      )}

      <p className="muted" style={{ fontSize: 13 }}>
        آخر تحديث للسجل: {dateTime(new Date())}
      </p>
    </Screen>
  );
}
