'use client';

import Link from 'next/link';
import { useParams } from 'next/navigation';
import { useState } from 'react';

import { DataTable, Notice } from '../../../../components/data-view';
import { ErrorBox, Loading, Screen } from '../../../../components/screen';
import { ApiError, apiData, apiPost } from '../../../../lib/api';
import {
  cashLocationLabel,
  dateTime,
  itemLabel,
  listCashLocations,
  listItems,
  listParties,
  money,
  partyLabel,
  quantity,
  shortDate,
  statusLabel,
  today,
  type CashLocation,
  type Item,
  type Party,
} from '../../../../lib/lookups';
import { cogsAmountFor, inventoryLinesFor, loadPostingProfile, periodForDate, requireAccount, salesJournalLines } from '../../../../lib/posting';
import { useSession } from '../../../../lib/session';
import { useQuery } from '../../../../lib/use-query';

type InvoiceLine = { id: string; lineNo: number; itemId: string | null; description: string | null; quantity: string; unitPrice: string; net: string; tax: string; total: string };
type Payment = { id: string; method: string; amount: string; reference: string | null; createdAt: string };
type Invoice = {
  id: string;
  number: string | null;
  kind: string;
  status: string;
  branchId: string;
  warehouseId: string | null;
  partyId: string | null;
  cashCustomerName: string | null;
  currency: string;
  subtotal: string;
  taxTotal: string;
  total: string;
  paidTotal: string;
  paymentStatus: string;
  createdAt: string;
  postedAt: string | null;
  lines: InvoiceLine[];
  payments: Payment[];
};

export default function SalesInvoiceDetailPage() {
  const params = useParams<{ id: string }>();
  const invoiceId = String(params.id);
  const { can } = useSession();
  const invoice = useQuery<Invoice>(() => apiData<Invoice>(`/sales/invoices/${invoiceId}`), [invoiceId]);
  const items = useQuery<Item[]>(() => listItems(), []);
  const parties = useQuery<Party[]>(() => listParties('customer'), []);
  const cashLocations = useQuery<CashLocation[]>(() => listCashLocations(), []);

  const [settlement, setSettlement] = useState<'credit' | 'cash' | 'bank'>('credit');
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger' | 'info'; text: string } | undefined>();
  const [payAmountText, setPayAmountText] = useState('');
  const [payMethod, setPayMethod] = useState('cash');
  const [payLocationId, setPayLocationId] = useState('');
  const [voidReason, setVoidReason] = useState('');

  if (invoice.status === 'loading') return <Loading />;
  if (invoice.status !== 'success' || !invoice.data) {
    return (
      <Screen title="فاتورة مبيعات" crumbs={['المبيعات']}>
        <ErrorBox message={invoice.error ?? 'تعذّر تحميل الفاتورة'} onRetry={invoice.reload} />
      </Screen>
    );
  }

  const doc = invoice.data;
  const isReturn = doc.kind === 'sale_return';
  const partyName = doc.cashCustomerName
    ? `${doc.cashCustomerName} (نقدي)`
    : partyLabel((parties.data ?? []).find((row) => row.id === doc.partyId) ?? { id: '', name: '—' });
  const dueValue = Number(doc.total) - Number(doc.paidTotal);

  async function run(action: () => Promise<unknown>, okText: string) {
    setBusy(true);
    setNotice(undefined);
    try {
      await action();
      setNotice({ kind: 'ok', text: okText });
      invoice.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  async function post() {
    const profile = await loadPostingProfile(doc.branchId, 'sales_invoice');
    const period = await periodForDate(today());
    if (!period) throw new ApiError(422, 'PERIOD_NOT_FOUND', 'لا توجد فترة محاسبية مفتوحة تغطي تاريخ اليوم.');

    await apiPost(`/sales/invoices/${doc.id}/post`, {
      fiscalPeriodId: period.id,
      journalLines: salesJournalLines(
        profile,
        { subtotal: doc.subtotal, taxTotal: doc.taxTotal, total: doc.total },
        { settlement, partyId: doc.partyId ?? undefined, isReturn, description: `فاتورة مبيعات ${doc.number ?? ''}`.trim() },
      ),
      inventoryLines: inventoryLinesFor(doc.lines.map((line) => ({ itemId: line.itemId ?? undefined, quantity: line.quantity })), doc.warehouseId ?? undefined, isReturn ? 'in' : 'out', 'sales_invoice'),
    });

    // Cost of sales, from what inventory actually valued the outgoing movements at.
    if (!isReturn && doc.warehouseId) {
      const cogsValue = await cogsAmountFor(doc.id, doc.warehouseId);
      if (cogsValue > 0) {
        await apiPost('/journal-entries', {
          branchId: doc.branchId,
          fiscalPeriodId: period.id,
          date: today(),
          description: `تكلفة البضاعة المباعة — ${doc.number ?? doc.id.slice(0, 8)}`,
          lines: [
            { accountId: requireAccount(profile, 'cogsAccountId'), debit: cogsValue.toFixed(4) },
            { accountId: requireAccount(profile, 'inventoryAccountId'), credit: cogsValue.toFixed(4) },
          ],
        });
      }
    }
  }

  return (
    <Screen
      title={`فاتورة مبيعات ${doc.number ?? '(مسودة)'}`}
      subtitle={`${partyName} — ${statusLabel(doc.status)}`}
      crumbs={['المبيعات', 'العمليات']}
      actions={
        <>
          <Link className="btn primary" href={`/print/sales-invoice/${doc.id}`}>
            طباعة
          </Link>
          <Link className="btn" href="/sales/invoices">
            كل الفواتير
          </Link>
        </>
      }
    >
      <div className="grid cols-2">
        <div className="card">
          <h2>بيانات الفاتورة</h2>
          <dl className="kv">
            <dt>الحالة</dt>
            <dd>{statusLabel(doc.status)}</dd>
            <dt>التاريخ</dt>
            <dd>{shortDate(doc.createdAt)}</dd>
            <dt>الترحيل</dt>
            <dd>{doc.postedAt ? dateTime(doc.postedAt) : '—'}</dd>
            <dt>الإجمالي قبل الضريبة</dt>
            <dd>{money(doc.subtotal, doc.currency)}</dd>
            <dt>الضريبة</dt>
            <dd>{money(doc.taxTotal, doc.currency)}</dd>
            <dt>الإجمالي</dt>
            <dd>
              <strong>{money(doc.total, doc.currency)}</strong>
            </dd>
            <dt>المدفوع</dt>
            <dd>{money(doc.paidTotal, doc.currency)}</dd>
            <dt>المتبقي</dt>
            <dd>{money(dueValue, doc.currency)}</dd>
          </dl>
        </div>

        <div className="card">
          <h2>الإجراءات</h2>
          <Notice notice={notice} />

          {doc.status === 'draft' && can('sales.invoice.post') && (
            <>
              <label className="field">
                <span>طريقة التحصيل عند الترحيل</span>
                <select className="input" value={settlement} onChange={(event) => setSettlement(event.target.value as 'credit' | 'cash' | 'bank')}>
                  <option value="credit">على حساب العميل (ذمم)</option>
                  <option value="cash">نقداً (الصندوق)</option>
                  <option value="bank">بنك</option>
                </select>
              </label>
              <button className="btn primary" type="button" disabled={busy} onClick={() => run(post, 'تم ترحيل الفاتورة وقيدها المحاسبي وحركتها المخزنية.')}>
                {busy ? 'جارٍ الترحيل…' : 'ترحيل الفاتورة'}
              </button>
            </>
          )}

          {doc.status === 'posted' && can('sales.invoice.pay') && dueValue > 0 && (
            <>
              <h3>تحصيل دفعة</h3>
              <div className="form-grid">
                <label className="field">
                  <span>المبلغ</span>
                  <input className="input" dir="ltr" inputMode="decimal" value={payAmountText} onChange={(event) => setPayAmountText(event.target.value)} />
                </label>
                <label className="field">
                  <span>الطريقة</span>
                  <select className="input" value={payMethod} onChange={(event) => setPayMethod(event.target.value)}>
                    <option value="cash">نقداً</option>
                    <option value="card">شبكة</option>
                    <option value="bank">تحويل بنكي</option>
                  </select>
                </label>
                <label className="field">
                  <span>الصندوق / البنك</span>
                  <select className="input" value={payLocationId} onChange={(event) => setPayLocationId(event.target.value)}>
                    <option value="">— اختر —</option>
                    {(cashLocations.data ?? []).map((row) => (
                      <option key={row.id} value={row.id}>
                        {cashLocationLabel(row)}
                      </option>
                    ))}
                  </select>
                </label>
              </div>
              <button
                className="btn"
                type="button"
                disabled={busy}
                onClick={() =>
                  run(
                    () =>
                      apiPost(`/sales/invoices/${doc.id}/payments`, {
                        method: payMethod,
                        amount: payAmountText,
                        cashLocationId: payLocationId || undefined,
                        idempotencyKey: crypto.randomUUID(),
                      }),
                    'تم تسجيل الدفعة.',
                  )
                }
              >
                تسجيل الدفعة
              </button>
            </>
          )}

          {doc.status === 'posted' && can('sales.invoice.void') && (
            <>
              <h3>إلغاء الفاتورة</h3>
              <label className="field">
                <span>سبب الإلغاء</span>
                <input className="input" value={voidReason} onChange={(event) => setVoidReason(event.target.value)} />
              </label>
              <button
                className="btn danger"
                type="button"
                disabled={busy || !voidReason.trim()}
                onClick={() => run(() => apiPost(`/sales/invoices/${doc.id}/void`, { reason: voidReason.trim() }), 'تم إلغاء الفاتورة.')}
              >
                إلغاء الفاتورة
              </button>
            </>
          )}

          {doc.status === 'voided' && <p className="alert warn">هذه الفاتورة ملغاة.</p>}
        </div>
      </div>

      <div className="card">
        <h2>الأصناف</h2>
        <DataTable
          rows={doc.lines}
          rowKey={(row) => row.id}
          columns={[
            { key: 'no', header: '#', align: 'num', cell: (row) => row.lineNo },
            {
              key: 'item',
              header: 'المادة',
              cell: (row) => {
                const item = (items.data ?? []).find((entry) => entry.id === row.itemId);
                return item ? itemLabel(item) : (row.description ?? '—');
              },
            },
            { key: 'qty', header: 'الكمية', align: 'num', cell: (row) => quantity(row.quantity) },
            { key: 'price', header: 'السعر', align: 'num', cell: (row) => money(row.unitPrice) },
            { key: 'net', header: 'الصافي', align: 'num', cell: (row) => money(row.net) },
            { key: 'tax', header: 'الضريبة', align: 'num', cell: (row) => money(row.tax) },
            { key: 'total', header: 'الإجمالي', align: 'num', cell: (row) => money(row.total) },
          ]}
        />
      </div>

      {doc.payments.length > 0 && (
        <div className="card">
          <h2>الدفعات</h2>
          <DataTable
            rows={doc.payments}
            rowKey={(row) => row.id}
            columns={[
              { key: 'at', header: 'التاريخ', align: 'ltr', cell: (row) => dateTime(row.createdAt) },
              { key: 'method', header: 'الطريقة', cell: (row) => row.method },
              { key: 'amount', header: 'المبلغ', align: 'num', cell: (row) => money(row.amount) },
              { key: 'ref', header: 'المرجع', cell: (row) => row.reference ?? '—' },
            ]}
          />
        </div>
      )}
    </Screen>
  );
}
