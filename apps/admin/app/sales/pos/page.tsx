'use client';

import { useState } from 'react';

import { Notice } from '../../../components/data-view';
import { Screen } from '../../../components/screen';
import { computeTotals, emptyLine, type LineDraft } from '../../../components/invoice-editor';
import { ApiError, apiPost } from '../../../lib/api';
import {
  arabicName,
  cashLocationLabel,
  defaultOf,
  itemLabel,
  listBranches,
  listCashLocations,
  listItems,
  listTaxGroups,
  listWarehouses,
  money,
  today,
  type Branch,
  type CashLocation,
  type Item,
  type TaxGroup,
  type Warehouse,
} from '../../../lib/lookups';
import { cogsAmountFor, inventoryLinesFor, loadPostingProfile, periodForDate, requireAccount, salesJournalLines } from '../../../lib/posting';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type Ticket = { item: Item; quantityText: string; unitPriceText: string; taxRateText: string };

/**
 * Point of sale — one screen, one keyboard hand: pick items, take the money, print.
 * Behind the button it does what the desktop till did: create the invoice, post it with
 * its journal and its stock movement, then register the payment, all in one round trip
 * chain so the cashier never sees a half-finished sale.
 */
export default function PosPage() {
  const { can } = useSession();
  const branches = useQuery<Branch[]>(() => listBranches(), []);
  const warehouses = useQuery<Warehouse[]>(() => listWarehouses(), []);
  const items = useQuery<Item[]>(() => listItems(), []);
  const taxGroups = useQuery<TaxGroup[]>(() => listTaxGroups(), []);
  const cashLocations = useQuery<CashLocation[]>(() => listCashLocations(), []);

  const [search, setSearch] = useState('');
  const [ticket, setTicket] = useState<Ticket[]>([]);
  const [customerName, setCustomerName] = useState('');
  const [method, setMethod] = useState<'cash' | 'card'>('cash');
  const [cashLocationId, setCashLocationId] = useState('');
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger' | 'info'; text: string } | undefined>();

  const branchRow = defaultOf(branches.data ?? []);
  const warehouseRow = defaultOf((warehouses.data ?? []).filter((row) => !branchRow || row.branchId === branchRow.id));
  const defaultRate = (taxGroups.data ?? [])[0]?.rate;
  const visible = (items.data ?? []).filter((row) => !search.trim() || itemLabel(row).includes(search.trim()));

  const lines: LineDraft[] = ticket.map((entry) => ({
    ...emptyLine(entry.taxRateText),
    itemId: entry.item.id,
    quantityText: entry.quantityText,
    unitPriceText: entry.unitPriceText,
  }));
  const totals = computeTotals(lines, { priceIncludesVat: true });

  function add(item: Item) {
    setTicket((current) => {
      const existing = current.find((entry) => entry.item.id === item.id);
      if (existing) {
        return current.map((entry) => (entry.item.id === item.id ? { ...entry, quantityText: String(Number(entry.quantityText) + 1) } : entry));
      }
      return [
        ...current,
        {
          item,
          quantityText: '1',
          unitPriceText: String(item.salePrice ?? item.sale_price ?? '0'),
          taxRateText: defaultRate ? String(Number(defaultRate) * 100) : '15',
        },
      ];
    });
  }

  async function checkout() {
    setBusy(true);
    setNotice(undefined);
    try {
      if (ticket.length === 0) throw new ApiError(422, 'VALIDATION_FAILED', 'السلة فارغة.');
      if (!branchRow) throw new ApiError(422, 'VALIDATION_FAILED', 'لا يوجد فرع مُعرّف.');
      if (method === 'cash' && !cashLocationId) throw new ApiError(422, 'VALIDATION_FAILED', 'اختر الصندوق الذي يستلم النقد.');

      const profile = await loadPostingProfile(branchRow.id, 'sales_invoice');
      const period = await periodForDate(today());
      if (!period) throw new ApiError(422, 'PERIOD_NOT_FOUND', 'لا توجد فترة محاسبية مفتوحة تغطي تاريخ اليوم.');

      const invoice = await apiPost<{ id: string; number: string | null; subtotal: string; taxTotal: string; total: string }>('/sales/invoices', {
        branchId: branchRow.id,
        warehouseId: warehouseRow?.id,
        cashCustomerName: customerName.trim() || 'عميل نقدي',
        kind: 'sale',
        priceIncludesVat: true,
        lines: ticket.map((entry) => ({
          itemId: entry.item.id,
          quantity: entry.quantityText,
          unitPrice: entry.unitPriceText,
          taxRate: entry.taxRateText,
        })),
      });

      await apiPost(`/sales/invoices/${invoice.id}/post`, {
        fiscalPeriodId: period.id,
        journalLines: salesJournalLines(
          profile,
          { subtotal: invoice.subtotal, taxTotal: invoice.taxTotal, total: invoice.total },
          { settlement: method === 'cash' ? 'cash' : 'bank', description: 'بيع نقطة بيع' },
        ),
        inventoryLines: inventoryLinesFor(
          ticket.map((entry) => ({ itemId: entry.item.id, quantity: entry.quantityText })),
          warehouseRow?.id,
          'out',
          'sales_invoice',
        ),
      });

      if (warehouseRow?.id) {
        const cogsValue = await cogsAmountFor(invoice.id, warehouseRow.id);
        if (cogsValue > 0) {
          await apiPost('/journal-entries', {
            branchId: branchRow.id,
            fiscalPeriodId: period.id,
            date: today(),
            description: 'تكلفة مبيعات نقطة البيع',
            lines: [
              { accountId: requireAccount(profile, 'cogsAccountId'), debit: cogsValue.toFixed(4) },
              { accountId: requireAccount(profile, 'inventoryAccountId'), credit: cogsValue.toFixed(4) },
            ],
          });
        }
      }

      await apiPost(`/sales/invoices/${invoice.id}/payments`, {
        method,
        amount: invoice.total,
        cashLocationId: cashLocationId || undefined,
        idempotencyKey: crypto.randomUUID(),
      });

      setNotice({ kind: 'ok', text: `تم البيع — الفاتورة ${invoice.number ?? ''} بمبلغ ${money(invoice.total)}.` });
      setTicket([]);
      setCustomerName('');
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  if (!can('sales.invoice.create')) {
    return (
      <Screen title="نقطة البيع" crumbs={['المبيعات']}>
        <div className="card state">
          <strong>لا تملك صلاحية البيع</strong>
        </div>
      </Screen>
    );
  }

  return (
    <Screen
      title="نقطة البيع"
      subtitle={`${branchRow ? arabicName(branchRow) : 'بدون فرع'} — ${warehouseRow ? arabicName(warehouseRow) : 'بدون مستودع'} · الأسعار شاملة الضريبة`}
      crumbs={['المبيعات', 'العمليات']}
    >
      <div className="grid cols-2">
        <div className="card">
          <h2>الأصناف</h2>
          <input className="input" placeholder="بحث عن صنف…" value={search} onChange={(event) => setSearch(event.target.value)} />
          <div className="chips">
            {visible.slice(0, 40).map((item) => (
              <button className="chip" type="button" key={item.id} onClick={() => add(item)}>
                {arabicName(item)} · {money(item.salePrice ?? item.sale_price)}
              </button>
            ))}
          </div>
          {visible.length === 0 && <p className="muted">لا توجد أصناف مطابقة. أضف المواد من «المستودعات ← دليل المواد».</p>}
        </div>

        <div className="card">
          <h2>الفاتورة الحالية</h2>
          {ticket.length === 0 ? (
            <p className="muted">اضغط على صنف لإضافته.</p>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>الصنف</th>
                    <th>الكمية</th>
                    <th>السعر</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {ticket.map((entry) => (
                    <tr key={entry.item.id}>
                      <td>{arabicName(entry.item)}</td>
                      <td>
                        <input
                          className="input"
                          dir="ltr"
                          inputMode="decimal"
                          value={entry.quantityText}
                          onChange={(event) =>
                            setTicket((current) => current.map((row) => (row.item.id === entry.item.id ? { ...row, quantityText: event.target.value } : row)))
                          }
                        />
                      </td>
                      <td>
                        <input
                          className="input"
                          dir="ltr"
                          inputMode="decimal"
                          value={entry.unitPriceText}
                          onChange={(event) =>
                            setTicket((current) => current.map((row) => (row.item.id === entry.item.id ? { ...row, unitPriceText: event.target.value } : row)))
                          }
                        />
                      </td>
                      <td>
                        <button className="btn sm" type="button" onClick={() => setTicket((current) => current.filter((row) => row.item.id !== entry.item.id))}>
                          حذف
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          <dl className="kv">
            <dt>قبل الضريبة</dt>
            <dd>{money(totals.subtotal)}</dd>
            <dt>الضريبة</dt>
            <dd>{money(totals.tax)}</dd>
            <dt>المطلوب</dt>
            <dd>
              <strong>{money(totals.total)}</strong>
            </dd>
          </dl>

          <div className="form-grid">
            <label className="field">
              <span>اسم العميل</span>
              <input className="input" placeholder="عميل نقدي" value={customerName} onChange={(event) => setCustomerName(event.target.value)} />
            </label>
            <label className="field">
              <span>طريقة الدفع</span>
              <select className="input" value={method} onChange={(event) => setMethod(event.target.value as 'cash' | 'card')}>
                <option value="cash">نقداً</option>
                <option value="card">شبكة</option>
              </select>
            </label>
            <label className="field">
              <span>الصندوق</span>
              <select className="input" value={cashLocationId} onChange={(event) => setCashLocationId(event.target.value)}>
                <option value="">— اختر —</option>
                {(cashLocations.data ?? []).map((row) => (
                  <option key={row.id} value={row.id}>
                    {cashLocationLabel(row)}
                  </option>
                ))}
              </select>
            </label>
          </div>

          <Notice notice={notice} />
          <button className="btn primary block" type="button" disabled={busy || ticket.length === 0} onClick={checkout}>
            {busy ? 'جارٍ إتمام البيع…' : `إتمام البيع — ${money(totals.total)}`}
          </button>
        </div>
      </div>
    </Screen>
  );
}
