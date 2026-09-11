'use client';

import Link from 'next/link';
import { useParams } from 'next/navigation';
import { useEffect, useState } from 'react';

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
  type CashLocation,
  type Item,
  type Party,
} from '../../../../lib/lookups';
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

  const [settlement, setSettlement] = useState<'credit' | 'cash' | 'card' | 'bank'>('credit');
  const [settleLocationId, setSettleLocationId] = useState('');
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger' | 'info'; text: string } | undefined>();
  const [payAmountText, setPayAmountText] = useState('');
  const [payMethod, setPayMethod] = useState('cash');
  const [payLocationId, setPayLocationId] = useState('');
  const [voidReason, setVoidReason] = useState('');

  // A walk-in/customer-name invoice has no receivable subledger. Default it to an
  // immediate settlement as soon as its data arrives, instead of presenting the
  // legacy "credit" default and letting the user discover the error at posting.
  useEffect(() => {
    if (invoice.data?.cashCustomerName && !invoice.data.partyId) setSettlement('cash');
  }, [invoice.data?.cashCustomerName, invoice.data?.partyId]);

  if (invoice.status === 'loading') return <Loading />;
  if (invoice.status !== 'success' || !invoice.data) {
    return (
      <Screen title="فاتورة مبيعات" crumbs={['المبيعات']}>
        <ErrorBox message={invoice.error ?? 'تعذّر تحميل الفاتورة'} onRetry={invoice.reload} />
      </Screen>
    );
  }

  const doc = invoice.data;
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
    // The posting engine builds the journal from the branch's posting profile,
    // relieves the warehouse at average cost, and stamps each line's cost — all
    // in one transaction. The screen only declares how the invoice settles.
    if (!doc.partyId && settlement === 'credit') {
      throw new ApiError(422, 'SALES_CASH_CUSTOMER_SETTLEMENT_REQUIRED', 'العميل النقدي يجب أن يُرحَّل إلى صندوق أو بنك، وليس على الذمم.');
    }
    if (settlement !== 'credit' && !settleLocationId) {
      throw new ApiError(422, 'VALIDATION_FAILED', 'اختر الصندوق أو البنك الذي استلم المبلغ.');
    }
    const location = (cashLocations.data ?? []).find((row) => row.id === settleLocationId);
    const settlementAccountId = location?.accountId ?? location?.account_id ?? undefined;
    if (settlement !== 'credit' && !settlementAccountId) {
      throw new ApiError(422, 'VALIDATION_FAILED', 'الموقع المختار غير مربوط بحساب محاسبي.');
    }
    await apiPost(`/sales/invoices/${doc.id}/post`, {
      settlement,
      settlementAccountId,
      settlementCashLocationId: settleLocationId || undefined,
    });
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
                <select
                  className="input"
                  value={settlement}
                  onChange={(event) => setSettlement(event.target.value as 'credit' | 'cash' | 'card' | 'bank')}
                >
                  <option value="credit" disabled={!doc.partyId}>على حساب العميل (ذمم)</option>
                  <option value="cash">نقداً (الصندوق)</option>
                  <option value="card">شبكة / بطاقة</option>
                  <option value="bank">تحويل بنكي</option>
                </select>
                {!doc.partyId && <span className="muted small">العميل النقدي لا يُرحَّل على الذمم؛ اختر صندوقاً أو بنكاً.</span>}
              </label>
              {settlement !== 'credit' && (
                <label className="field">
                  <span>{settlement === 'cash' ? 'الصندوق المستلم' : 'البنك المستلم'} *</span>
                  <select className="input" value={settleLocationId} onChange={(event) => setSettleLocationId(event.target.value)}>
                    <option value="">— اختر —</option>
                    {(cashLocations.data ?? [])
                      .filter((row) => settlement === 'cash' ? row.kind === 'safe' : row.kind === 'bank')
                      .map((row) => (
                        <option key={row.id} value={row.id}>
                          {cashLocationLabel(row)}
                        </option>
                      ))}
                  </select>
                  <span className="muted small">يُقيَّد المبلغ على حساب هذا الموقع وتُسجَّل دفعة بنفس القيمة.</span>
                </label>
              )}
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
