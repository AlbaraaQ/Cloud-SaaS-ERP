'use client';

import Link from 'next/link';
import { useMemo, useState } from 'react';

import { Notice } from '../../../components/data-view';
import { Screen } from '../../../components/screen';
import { computeTotals, type LineDraft } from '../../../components/invoice-editor';
import { ApiError, apiData, apiList, apiPost } from '../../../lib/api';
import {
  arabicName,
  cashLocationLabel,
  defaultOf,
  itemLabel,
  listBranches,
  listCashLocations,
  listCategories,
  listItems,
  listParties,
  listTaxGroups,
  listWarehouses,
  money,
  partyLabel,
  type Branch,
  type CashLocation,
  type Category,
  type Item,
  type Party,
  type TaxGroup,
  type Warehouse,
} from '../../../lib/lookups';
import { BankChooser } from '../../../components/bank-chooser';
import { CashCustomerPicker } from '../../../components/cash-customer-picker';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type Ticket = { line: LineDraft; item: Item };

type Method = 'cash' | 'card' | 'bank' | 'credit';

type Shift = {
  id: string;
  branchId: string;
  status: string;
  openedAt: string;
  expectedCash: string;
  countedCash: string;
  diff: string;
};

type Receipt = {
  invoiceId: string;
  number: string | null;
  subtotal: string;
  taxTotal: string;
  total: string;
  paidTotal: string;
  paymentStatus: string;
  method: Method;
  cashLocationId: string | null;
  shiftId: string | null;
  change: string;
  tendered: string | null;
};

const METHODS: Array<{ id: Method; label: string; hint: string }> = [
  { id: 'cash', label: 'نقداً', hint: 'يُقبض في الصندوق ويظهر في جرد اليومية' },
  { id: 'card', label: 'شبكة', hint: 'يُقفل على حساب البنك ويظهر كتحصيل شبكة' },
  { id: 'bank', label: 'تحويل بنكي', hint: 'يُقفل على حساب البنك' },
  { id: 'credit', label: 'آجل', hint: 'يُرحّل على حساب العميل (ذمم مدينة)' },
];

const QUICK_CASH = ['20', '50', '100', '200', '500'];

/**
 * Point of sale — one screen, one hand on the keyboard.
 *
 * The whole sale now leaves the browser as a single `POST /pos/checkout`: the API
 * creates the invoice, posts it (journal + stock relief + COGS from the branch
 * posting profile), settles it into the drawer the cashier picked and links it to
 * the open shift — all inside one database transaction. A half-posted sale is
 * therefore impossible, and the closing report can see the till's own takings.
 */
export default function PosPage() {
  const { can } = useSession();
  const branches = useQuery<Branch[]>(() => listBranches(), []);
  const warehouses = useQuery<Warehouse[]>(() => listWarehouses(), []);
  const items = useQuery<Item[]>(() => listItems(), []);
  const categories = useQuery<Category[]>(() => listCategories(), []);
  const taxGroups = useQuery<TaxGroup[]>(() => listTaxGroups(), []);
  const cashLocations = useQuery<CashLocation[]>(() => listCashLocations(), []);
  const customers = useQuery<Party[]>(() => listParties('customer'), []);

  const branchRow = defaultOf(branches.data ?? []);
  const branchId = branchRow?.id ?? '';
  const warehouseRows = (warehouses.data ?? []).filter(
    (row) => !branchId || row.branchId === branchId || !row.branchId,
  );
  const [warehouseId, setWarehouseId] = useState('');
  const effectiveWarehouse = warehouseId || defaultOf(warehouseRows)?.id || '';

  const shift = useQuery<Shift | null>(
    () =>
      branchId
        ? apiData<Shift>(`/shift-closes/current?branch_id=${branchId}`).catch(() => null)
        : Promise.resolve(null),
    [branchId],
  );
  const recent = useQuery<
    Array<{
      id: string;
      number: string | null;
      total: string;
      orderType?: string | null;
      status: string;
      paymentStatus?: string;
    }>
  >(() => apiList('/sales/invoices'), []);

  const [search, setSearch] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [ticket, setTicket] = useState<Ticket[]>([]);
  const [customerMode, setCustomerMode] = useState<'walkin' | 'account'>('walkin');
  const [customerName, setCustomerName] = useState('');
  const [customerMobile, setCustomerMobile] = useState('');
  const [partyId, setPartyId] = useState('');
  /**
   * 🏦 اختر البنك / 👤 عميل نقدي — `frmPayBank` و`frmCashCustomer` are windows opened
   * *from* the sale, and each returns one answer to it: which bank the transfer went to,
   * and which walk-in the invoice is written for.
   */
  const [pickingBank, setPickingBank] = useState(false);
  const [pickingCustomer, setPickingCustomer] = useState(false);
  const [method, setMethod] = useState<Method>('cash');
  const [cashLocationId, setCashLocationId] = useState('');
  const [tendered, setTendered] = useState('');
  const [invoiceDiscount, setInvoiceDiscount] = useState('');
  const [priceIncludesVat, setPriceIncludesVat] = useState(true);
  const [busy, setBusy] = useState(false);
  const [receipt, setReceipt] = useState<Receipt | undefined>();
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger' | 'info'; text: string } | undefined>();

  const defaultRate = (taxGroups.data ?? [])[0]?.rate;
  const drawerKind = method === 'cash' ? 'safe' : 'bank';
  const drawers = (cashLocations.data ?? []).filter(
    (row) => (row.kind ?? 'safe') === drawerKind && (!branchId || !row.branchId || row.branchId === branchId),
  );
  const effectiveDrawer = cashLocationId || defaultOf(drawers)?.id || '';

  const lines = ticket.map((entry) => entry.line);
  const totals = computeTotals(lines, { priceIncludesVat, invoiceDiscount });
  const tenderedValue = Number(tendered || 0);
  const saleValue = Number(totals.total);
  const change = tenderedValue > 0 ? tenderedValue - saleValue : 0;

  const visible = useMemo(() => {
    const needle = search.trim();
    return (items.data ?? []).filter((row) => {
      if ((row.kind ?? 'stock') !== 'stock') return false;
      if (categoryId && (row.categoryId ?? row.category_id) !== categoryId) return false;
      if (!needle) return true;
      return itemLabel(row).includes(needle) || (row.barcode ?? '').includes(needle);
    });
  }, [items.data, search, categoryId]);

  function add(item: Item) {
    const taxPercent = defaultRate ? String(Number(defaultRate) * 100) : '15';
    setTicket((current) => {
      const existing = current.find((entry) => entry.item.id === item.id);
      if (existing) {
        return current.map((entry) =>
          entry.item.id === item.id
            ? { ...entry, line: { ...entry.line, quantityText: String(Number(entry.line.quantityText) + 1) } }
            : entry,
        );
      }
      return [
        ...current,
        {
          item,
          line: {
            itemId: item.id,
            description: '',
            quantityText: '1',
            unitPriceText: String(item.salePrice ?? item.sale_price ?? '0'),
            discountRateText: '',
            taxRateText: taxPercent,
            taxGroupId: '',
          },
        },
      ];
    });
  }

  function patch(itemId: string, changes: Partial<LineDraft>) {
    setTicket((current) =>
      current.map((entry) =>
        entry.item.id === itemId ? { ...entry, line: { ...entry.line, ...changes } } : entry,
      ),
    );
  }

  async function openShift() {
    setBusy(true);
    setNotice(undefined);
    try {
      await apiPost('/shift-closes/open', { branchId });
      setNotice({ kind: 'ok', text: 'تم فتح الوردية — أصبحت مبيعاتك النقدية تُنسب إليها.' });
      shift.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  async function checkout() {
    setBusy(true);
    setNotice(undefined);
    try {
      if (ticket.length === 0) throw new ApiError(422, 'VALIDATION_FAILED', 'السلة فارغة.');
      if (!branchId) throw new ApiError(422, 'VALIDATION_FAILED', 'لا يوجد فرع مُعرّف لهذا المستخدم.');
      if (!effectiveWarehouse)
        throw new ApiError(422, 'VALIDATION_FAILED', 'اختر المستودع الذي تُصرف منه الأصناف.');
      if (method !== 'credit' && !effectiveDrawer)
        throw new ApiError(
          422,
          'VALIDATION_FAILED',
          `اختر ${method === 'cash' ? 'الصندوق' : 'البنك'} الذي يستلم المبلغ.`,
        );
      if (method === 'credit' && customerMode !== 'account')
        throw new ApiError(422, 'VALIDATION_FAILED', 'البيع الآجل يحتاج حساب عميل.');

      const response = await apiPost<{ data: Receipt }>('/pos/checkout', {
        branchId,
        warehouseId: effectiveWarehouse,
        priceIncludesVat,
        invoiceDiscount: invoiceDiscount || undefined,
        orderType: 'pos',
        shiftId: shift.data?.id,
        partyId: customerMode === 'account' ? partyId || undefined : undefined,
        cashCustomerName: customerMode === 'walkin' ? customerName.trim() || 'عميل نقدي' : undefined,
        cashCustomerMobile: customerMode === 'walkin' ? customerMobile.trim() || undefined : undefined,
        lines: ticket.map((entry) => ({
          itemId: entry.item.id,
          quantity: entry.line.quantityText,
          unitPrice: entry.line.unitPriceText,
          taxRate: entry.line.taxRateText,
          discountRate: entry.line.discountRateText || undefined,
        })),
        payment: {
          method,
          cashLocationId: method === 'credit' ? undefined : effectiveDrawer,
          tendered: method === 'cash' && tendered ? tendered : undefined,
        },
      });

      setReceipt(response.data);
      setNotice({
        kind: 'ok',
        text: `تم البيع — الفاتورة ${response.data.number ?? ''} بمبلغ ${money(response.data.total)}.`,
      });
      setTicket([]);
      setTendered('');
      setInvoiceDiscount('');
      recent.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  if (!can('pos.operate')) {
    return (
      <Screen title="نقطة البيع" crumbs={['المبيعات']}>
        <div className="card state">
          <strong>لا تملك صلاحية تشغيل نقطة البيع</strong>
          <span>تحتاج صلاحية pos.operate.</span>
        </div>
      </Screen>
    );
  }

  return (
    <Screen
      title="نقطة البيع"
      subtitle={`${branchRow ? arabicName(branchRow) : 'بدون فرع'} — ${warehouseRows.find((row) => row.id === effectiveWarehouse) ? arabicName(warehouseRows.find((row) => row.id === effectiveWarehouse)!) : 'بدون مستودع'} · ${priceIncludesVat ? 'الأسعار شاملة الضريبة' : 'الأسعار قبل الضريبة'}`}
      crumbs={['المبيعات', 'العمليات']}
      actions={
        <>
          <span className={`badge ${shift.data ? 'posted' : 'draft'}`}>
            {shift.data
              ? `وردية مفتوحة منذ ${new Date(shift.data.openedAt).toLocaleTimeString('ar')}`
              : 'لا توجد وردية مفتوحة'}
          </span>
          {!shift.data && branchId && (
            <button className="btn sm" type="button" disabled={busy} onClick={openShift}>
              فتح وردية
            </button>
          )}
          <Link className="btn sm" href="/sales/shifts">
            إغلاق اليومية
          </Link>
        </>
      }
    >
      <div className="grid cols-2">
        <div className="card">
          <h2>الأصناف</h2>
          <div className="toolbar" style={{ marginBottom: 8 }}>
            <input
              className="input"
              placeholder="بحث بالاسم أو الباركود…"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              style={{ maxWidth: 260 }}
            />
            <label className="row" style={{ alignItems: 'center', gap: 6, fontSize: 12 }}>
              <input
                type="checkbox"
                checked={priceIncludesVat}
                onChange={(event) => setPriceIncludesVat(event.target.checked)}
              />
              الأسعار شاملة الضريبة
            </label>
          </div>
          <div className="chips" style={{ marginBottom: 10 }}>
            <button
              className={`chip ${categoryId === '' ? 'on' : ''}`}
              type="button"
              onClick={() => setCategoryId('')}
            >
              الكل
            </button>
            {(categories.data ?? []).map((row) => (
              <button
                key={row.id}
                className={`chip ${categoryId === row.id ? 'on' : ''}`}
                type="button"
                onClick={() => setCategoryId(row.id)}
              >
                {arabicName(row)}
              </button>
            ))}
          </div>
          <div className="pos-tiles">
            {visible.slice(0, 60).map((item) => (
              <button className="pos-tile" type="button" key={item.id} onClick={() => add(item)}>
                <strong>{arabicName(item)}</strong>
                <span>{money(item.salePrice ?? item.sale_price)}</span>
              </button>
            ))}
          </div>
          {visible.length === 0 && (
            <p className="muted">لا توجد أصناف مطابقة. أضف المواد من «المستودعات ← دليل المواد».</p>
          )}
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
                    <th>خصم %</th>
                    <th>الإجمالي</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {ticket.map((entry) => {
                    const line = computeTotals([entry.line], { priceIncludesVat }).lines[0];
                    return (
                      <tr key={entry.item.id}>
                        <td>{arabicName(entry.item)}</td>
                        <td>
                          <input
                            className="input"
                            dir="ltr"
                            inputMode="decimal"
                            value={entry.line.quantityText}
                            onChange={(event) => patch(entry.item.id, { quantityText: event.target.value })}
                          />
                        </td>
                        <td>
                          <input
                            className="input"
                            dir="ltr"
                            inputMode="decimal"
                            value={entry.line.unitPriceText}
                            onChange={(event) => patch(entry.item.id, { unitPriceText: event.target.value })}
                          />
                        </td>
                        <td>
                          <input
                            className="input"
                            dir="ltr"
                            inputMode="decimal"
                            value={entry.line.discountRateText}
                            onChange={(event) =>
                              patch(entry.item.id, { discountRateText: event.target.value })
                            }
                          />
                        </td>
                        <td>{money(line?.total ?? '0')}</td>
                        <td>
                          <button
                            className="btn sm"
                            type="button"
                            onClick={() =>
                              setTicket((current) => current.filter((row) => row.item.id !== entry.item.id))
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
          )}

          <dl className="kv" style={{ marginTop: 10 }}>
            <dt>قبل الضريبة</dt>
            <dd>{money(totals.subtotal)}</dd>
            <dt>الضريبة</dt>
            <dd>{money(totals.tax)}</dd>
            <dt>المطلوب</dt>
            <dd className="pos-total">{money(totals.total)}</dd>
          </dl>

          <div className="form-grid">
            <label className="field">
              <span>المستودع</span>
              <select
                className="input"
                value={effectiveWarehouse}
                onChange={(event) => setWarehouseId(event.target.value)}
              >
                {warehouseRows.map((row) => (
                  <option key={row.id} value={row.id}>
                    {arabicName(row)}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>خصم على الفاتورة</span>
              <input
                className="input"
                dir="ltr"
                inputMode="decimal"
                value={invoiceDiscount}
                onChange={(event) => setInvoiceDiscount(event.target.value)}
              />
            </label>
            <label className="field">
              <span>العميل</span>
              <select
                className="input"
                value={customerMode}
                onChange={(event) => setCustomerMode(event.target.value as 'walkin' | 'account')}
              >
                <option value="walkin">عميل نقدي</option>
                <option value="account">حساب عميل</option>
              </select>
            </label>
            {customerMode === 'walkin' ? (
              <>
                <label className="field">
                  <span>🏷️ الاسم:</span>
                  <div className="row" style={{ flexWrap: 'nowrap' }}>
                    <input
                      className="input"
                      placeholder="عميل نقدي"
                      value={customerName}
                      onChange={(event) => setCustomerName(event.target.value)}
                    />
                    <button type="button" className="btn" onClick={() => setPickingCustomer(true)}>
                      👤 عميل نقدي
                    </button>
                  </div>
                </label>
                <label className="field">
                  <span>📱 رقم الجوال:</span>
                  <input
                    className="input"
                    dir="ltr"
                    inputMode="tel"
                    value={customerMobile}
                    onChange={(event) => setCustomerMobile(event.target.value)}
                  />
                </label>
              </>
            ) : (
              <label className="field">
                <span>حساب العميل</span>
                <select
                  className="input"
                  value={partyId}
                  onChange={(event) => setPartyId(event.target.value)}
                >
                  <option value="">— اختر —</option>
                  {(customers.data ?? []).map((row) => (
                    <option key={row.id} value={row.id}>
                      {partyLabel(row)}
                    </option>
                  ))}
                </select>
              </label>
            )}
          </div>

          <h2 style={{ marginTop: 6 }}>الدفع</h2>
          <div className="chips">
            {METHODS.map((row) => (
              <button
                key={row.id}
                className={`chip ${method === row.id ? 'on' : ''}`}
                type="button"
                onClick={() => setMethod(row.id)}
                title={row.hint}
              >
                {row.label}
              </button>
            ))}
          </div>
          <p className="muted small">{METHODS.find((row) => row.id === method)?.hint}</p>

          {method === 'bank' ? (
            /**
             * 🏦 تحويل بنكي — `frmPayBank.xaml`: a tile per bank, `✔ موافق` / `✖ خروج`,
             * and no sale without a named bank. `EntryOper.cs` L493/L620 then debits
             * *that* bank's account instead of the generic شبكة account, so the choice
             * cannot be a dropdown default the cashier never looked at.
             */
            <div className="card tight" style={{ marginTop: 8 }}>
              <div className="card-head">🏦 البنوك المتاحة</div>
              <div className="row" style={{ alignItems: 'center', justifyContent: 'space-between' }}>
                <span>{effectiveDrawer ? cashLocationLabel(drawers.find((row) => row.id === effectiveDrawer) ?? ({} as CashLocation)) : 'لم يُختر بنك'}</span>
                <button type="button" className="btn primary" onClick={() => setPickingBank(true)}>
                  🏦 اختر البنك
                </button>
              </div>
            </div>
          ) : null}

          {method !== 'credit' && method !== 'bank' && (
            <label className="field" style={{ marginTop: 8 }}>
              <span>{method === 'cash' ? 'الصندوق' : 'حساب التحصيل'}</span>
              <select
                className="input"
                value={effectiveDrawer}
                onChange={(event) => setCashLocationId(event.target.value)}
              >
                <option value="">— اختر —</option>
                {drawers.map((row) => (
                  <option key={row.id} value={row.id}>
                    {cashLocationLabel(row)}
                  </option>
                ))}
              </select>
            </label>
          )}

          {method === 'cash' && (
            <>
              <label className="field">
                <span>المبلغ المستلم</span>
                <input
                  className="input"
                  dir="ltr"
                  inputMode="decimal"
                  value={tendered}
                  onChange={(event) => setTendered(event.target.value)}
                />
              </label>
              <div className="chips">
                {QUICK_CASH.map((value) => (
                  <button className="chip" type="button" key={value} onClick={() => setTendered(value)}>
                    {value}
                  </button>
                ))}
                <button className="chip" type="button" onClick={() => setTendered(totals.total)}>
                  المبلغ بالضبط
                </button>
              </div>
              <dl className="kv" style={{ marginTop: 8 }}>
                <dt>الباقي</dt>
                <dd className={change < 0 ? 'pos-change-due' : 'pos-change'}>{money(change)}</dd>
              </dl>
            </>
          )}

          <Notice notice={notice} />
          <button
            className="btn primary block"
            type="button"
            disabled={busy || ticket.length === 0}
            onClick={checkout}
          >
            {busy ? 'جارٍ إتمام البيع…' : `إتمام البيع — ${money(totals.total)}`}
          </button>
          {!shift.data && (
            <p className="muted small">لا توجد وردية مفتوحة: لن تُنسب هذه المبيعة إلى جرد اليومية.</p>
          )}
        </div>
      </div>

      {receipt && (
        <div className="card receipt">
          <div className="toolbar no-print" style={{ justifyContent: 'space-between' }}>
            <h2>آخر فاتورة</h2>
            <div className="toolbar">
              <button className="btn sm" type="button" onClick={() => window.print()}>
                طباعة
              </button>
              <Link className="btn sm" href={`/sales/invoices/${receipt.invoiceId}`}>
                تفاصيل الفاتورة
              </Link>
            </div>
          </div>
          <dl className="kv">
            <dt>الرقم</dt>
            <dd>{receipt.number ?? '—'}</dd>
            <dt>الإجمالي</dt>
            <dd>{money(receipt.total)}</dd>
            <dt>الضريبة</dt>
            <dd>{money(receipt.taxTotal)}</dd>
            <dt>طريقة الدفع</dt>
            <dd>{METHODS.find((row) => row.id === receipt.method)?.label ?? receipt.method}</dd>
            {receipt.method === 'cash' && (
              <>
                <dt>المستلم</dt>
                <dd>{receipt.tendered ? money(receipt.tendered) : '—'}</dd>
                <dt>الباقي</dt>
                <dd>{money(receipt.change)}</dd>
              </>
            )}
            <dt>الحالة</dt>
            <dd>{receipt.paymentStatus === 'paid' ? 'مدفوعة' : 'آجلة'}</dd>
            {receipt.shiftId && (
              <>
                <dt>الوردية</dt>
                <dd>
                  <Link href="/sales/shifts">مربوطة بوردية مفتوحة</Link>
                </dd>
              </>
            )}
          </dl>
        </div>
      )}

      <div className="card">
        <h2>آخر مبيعات نقطة البيع</h2>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>الرقم</th>
                <th>الإجمالي</th>
                <th>الحالة</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {(recent.data ?? [])
                .filter((row) => row.orderType === 'pos')
                .slice(0, 8)
                .map((row) => (
                  <tr key={row.id}>
                    <td>{row.number ?? '—'}</td>
                    <td>{money(row.total)}</td>
                    <td>
                      <span className={`badge ${row.status === 'posted' ? 'posted' : row.status}`}>
                        {row.status}
                      </span>
                    </td>
                    <td>
                      <Link className="btn sm" href={`/sales/invoices/${row.id}`}>
                        فتح
                      </Link>
                    </td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      </div>

      {/**
       * 🏦 `frmPayBank` — the bank is chosen at the moment of payment, or the sale is
       * not made: `SelectedBankId == 0` is the desktop's "يرجى اختر بنك أولًا".
       */}
      {pickingBank ? (
        <BankChooser
          banks={drawers}
          selectedId={effectiveDrawer}
          loading={cashLocations.status === 'loading'}
          onPick={(bank) => {
            setCashLocationId(bank.id);
            setPickingBank(false);
          }}
          onCancel={() => setPickingBank(false)}
        />
      ) : null}

      {/** 👤 `frmCashCustomer` — a walk-in is a name and a mobile, not a ledger account. */}
      {pickingCustomer ? (
        <CashCustomerPicker
          value={{ name: customerName, mobile: customerMobile }}
          onPick={(customer) => {
            setCustomerName(customer.name);
            setCustomerMobile(customer.mobile);
            setPickingCustomer(false);
          }}
          onClose={() => setPickingCustomer(false)}
        />
      ) : null}
    </Screen>
  );
}
