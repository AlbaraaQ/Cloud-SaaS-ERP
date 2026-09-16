'use client';

import Link from 'next/link';
import { useParams } from 'next/navigation';
import { useState } from 'react';

import { DataTable, Notice } from '../../../../components/data-view';
import { ErrorBox, Loading, Screen } from '../../../../components/screen';
import { ApiError, apiData, apiPost } from '../../../../lib/api';
import { accountLabel, listAccounts, postableOf, typeOf, type Account } from '../../../../lib/accounts';
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

type InvoiceLine = { id: string; lineNo: number; itemId: string; description: string | null; quantity: string; unitPrice: string; net: string; tax: string; total: string; allocatedCost: string; unitCostAtPost: string | null };
type LandedCost = { id: string; costName: string; amount: string; allocationTarget: string; accountId: string | null };
type Invoice = {
  id: string;
  number: string | null;
  kind: string;
  status: string;
  branchId: string;
  warehouseId: string | null;
  partyId: string;
  supplierReferenceNo: string | null;
  currency: string;
  subtotal: string;
  taxTotal: string;
  total: string;
  paidTotal: string;
  additionalCostTotal: string;
  createdAt: string;
  postedAt: string | null;
  lines: InvoiceLine[];
  costs: LandedCost[];
};

export default function PurchaseInvoiceDetailPage() {
  const params = useParams<{ id: string }>();
  const invoiceId = String(params.id);
  const { can } = useSession();
  const invoice = useQuery<Invoice>(() => apiData<Invoice>(`/purchase-invoices/${invoiceId}`), [invoiceId]);
  const items = useQuery<Item[]>(() => listItems(), []);
  const suppliers = useQuery<Party[]>(() => listParties('supplier'), []);
  const cashLocations = useQuery<CashLocation[]>(() => listCashLocations(), []);
  const accounts = useQuery<Account[]>(() => listAccounts(), []);

  const [settlement, setSettlement] = useState<'credit' | 'cash' | 'bank'>('credit');
  const [settleLocationId, setSettleLocationId] = useState('');
  const [costName, setCostName] = useState('');
  const [costAmountText, setCostAmountText] = useState('');
  const [costTarget, setCostTarget] = useState<'inventory' | 'expense'>('inventory');
  const [costAccountId, setCostAccountId] = useState('');
  const [payAmountText, setPayAmountText] = useState('');
  const [voidReason, setVoidReason] = useState('');
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger' | 'info'; text: string } | undefined>();

  if (invoice.status === 'loading') return <Loading />;
  if (invoice.status !== 'success' || !invoice.data) {
    return (
      <Screen title="فاتورة مشتريات" crumbs={['المشتريات']}>
        <ErrorBox message={invoice.error ?? 'تعذّر تحميل الفاتورة'} onRetry={invoice.reload} />
      </Screen>
    );
  }

  const doc = invoice.data;
  const supplier = (suppliers.data ?? []).find((row) => row.id === doc.partyId);
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
    // receives the stock at landed cost, and distributes the additional costs —
    // all in one transaction. The screen only declares how the invoice settles.
    if (settlement !== 'credit' && !settleLocationId) {
      throw new ApiError(422, 'VALIDATION_FAILED', 'اختر الصندوق أو البنك الذي سدد المبلغ.');
    }
    const location = (cashLocations.data ?? []).find((row) => row.id === settleLocationId);
    const settlementAccountId = location?.accountId ?? location?.account_id ?? undefined;
    if (settlement !== 'credit' && !settlementAccountId) {
      throw new ApiError(422, 'VALIDATION_FAILED', 'الموقع المختار غير مربوط بحساب محاسبي.');
    }
    if (doc.costs.some((entry) => entry.allocationTarget === 'expense' && !entry.accountId)) {
      throw new ApiError(422, 'VALIDATION_FAILED', 'مصروف مباشر بلا حساب — احذفه وأعده مربوطاً بحساب مصروف.');
    }
    await apiPost(`/purchase-invoices/${doc.id}/post`, {
      settlement,
      settlementAccountId,
      settlementCashLocationId: settleLocationId || undefined,
    });
  }

  return (
    <Screen
      title={`فاتورة مشتريات ${doc.number ?? '(مسودة)'}`}
      subtitle={`${supplier ? partyLabel(supplier) : '—'} — ${statusLabel(doc.status)}`}
      crumbs={['المشتريات', 'العمليات']}
      actions={
        <>
          <Link className="btn primary" href={`/print/purchase-invoice/${doc.id}`}>
            طباعة
          </Link>
          <Link className="btn" href="/purchases/invoices">
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
            <dt>مرجع المورد</dt>
            <dd>{doc.supplierReferenceNo ?? '—'}</dd>
            <dt>التاريخ</dt>
            <dd>{shortDate(doc.createdAt)}</dd>
            <dt>الترحيل</dt>
            <dd>{doc.postedAt ? dateTime(doc.postedAt) : '—'}</dd>
            <dt>قبل الضريبة</dt>
            <dd>{money(doc.subtotal, doc.currency)}</dd>
            <dt>الضريبة</dt>
            <dd>{money(doc.taxTotal, doc.currency)}</dd>
            <dt>مصاريف إضافية</dt>
            <dd>{money(doc.additionalCostTotal, doc.currency)}</dd>
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

          {doc.status === 'draft' && can('purchase.cost.manage') && (
            <>
              <h3>مصروف إضافي (شحن، تخليص…)</h3>
              <div className="form-grid">
                <label className="field">
                  <span>البيان</span>
                  <input className="input" value={costName} onChange={(event) => setCostName(event.target.value)} />
                </label>
                <label className="field">
                  <span>المبلغ</span>
                  <input className="input" dir="ltr" inputMode="decimal" value={costAmountText} onChange={(event) => setCostAmountText(event.target.value)} />
                </label>
                <label className="field">
                  <span>التوزيع</span>
                  <select className="input" value={costTarget} onChange={(event) => setCostTarget(event.target.value as 'inventory' | 'expense')}>
                    <option value="inventory">على تكلفة الأصناف (يُنسب للمخزون)</option>
                    <option value="expense">مصروف مباشر (حساب مصروف)</option>
                  </select>
                </label>
                {costTarget === 'expense' && (
                  <label className="field">
                    <span>حساب المصروف *</span>
                    <select className="input" value={costAccountId} onChange={(event) => setCostAccountId(event.target.value)}>
                      <option value="">— اختر —</option>
                      {(accounts.data ?? [])
                        .filter((row) => postableOf(row) && typeOf(row) === 'expense')
                        .map((row) => (
                          <option key={row.id} value={row.id}>
                            {accountLabel(row)}
                          </option>
                        ))}
                    </select>
                  </label>
                )}
              </div>
              <button
                className="btn"
                type="button"
                disabled={busy || !costName.trim() || !costAmountText.trim() || (costTarget === 'expense' && !costAccountId)}
                onClick={() =>
                  run(async () => {
                    await apiPost(`/purchase-invoices/${doc.id}/costs`, {
                      costName: costName.trim(),
                      amount: costAmountText.trim(),
                      allocationTarget: costTarget,
                      accountId: costTarget === 'expense' ? costAccountId : undefined,
                    });
                    setCostName('');
                    setCostAmountText('');
                    setCostAccountId('');
                  }, costTarget === 'inventory' ? 'تمت إضافة المصروف؛ سيوزَّع على تكلفة الأصناف عند الترحيل.' : 'تمت إضافة المصروف المباشر.')
                }
              >
                إضافة المصروف
              </button>
            </>
          )}

          {doc.status === 'draft' && can('purchase.invoice.post') && (
            <>
              <h3>الترحيل</h3>
              <label className="field">
                <span>طريقة السداد</span>
                <select className="input" value={settlement} onChange={(event) => setSettlement(event.target.value as 'credit' | 'cash' | 'bank')}>
                  <option value="credit">على حساب المورد (ذمم)</option>
                  <option value="cash">نقداً من الصندوق</option>
                  <option value="bank">تحويل بنكي</option>
                </select>
              </label>
              {settlement !== 'credit' && (
                <label className="field">
                  <span>الصندوق / البنك المسدد *</span>
                  <select className="input" value={settleLocationId} onChange={(event) => setSettleLocationId(event.target.value)}>
                    <option value="">— اختر —</option>
                    {(cashLocations.data ?? []).map((row) => (
                      <option key={row.id} value={row.id}>
                        {cashLocationLabel(row)}
                      </option>
                    ))}
                  </select>
                  <span className="muted small">يُقيَّد المبلغ على حساب هذا الموقع وتُسجَّل دفعة بنفس القيمة.</span>
                </label>
              )}
              <button className="btn primary" type="button" disabled={busy} onClick={() => run(post, 'تم ترحيل الفاتورة وأُدخلت البضاعة للمخزون بتكلفتها النهائية.')}>
                {busy ? 'جارٍ الترحيل…' : 'ترحيل الفاتورة'}
              </button>
            </>
          )}

          {doc.status === 'posted' && can('purchase.invoice.pay') && dueValue > 0 && (
            <>
              <h3>سداد للمورد</h3>
              <label className="field">
                <span>المبلغ</span>
                <input className="input" dir="ltr" inputMode="decimal" value={payAmountText} onChange={(event) => setPayAmountText(event.target.value)} />
              </label>
              <button
                className="btn"
                type="button"
                disabled={busy || !payAmountText.trim()}
                onClick={() => run(() => apiPost(`/purchase-invoices/${doc.id}/payments`, { amount: payAmountText.trim(), idempotencyKey: crypto.randomUUID() }), 'تم تسجيل السداد.')}
              >
                تسجيل السداد
              </button>
              <p className="muted small">لتسجيل الصرف نقداً من الصندوق استخدم «سند صرف لمورد» في شاشة السندات.</p>
            </>
          )}

          {doc.status === 'posted' && can('purchase.invoice.void') && (
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
                onClick={() => run(() => apiPost(`/purchase-invoices/${doc.id}/void`, { reason: voidReason.trim() }), 'تم إلغاء الفاتورة.')}
              >
                إلغاء الفاتورة
              </button>
            </>
          )}
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
            { key: 'alloc', header: 'مصاريف محمّلة', align: 'num', cell: (row) => money(row.allocatedCost) },
            { key: 'unitcost', header: 'التكلفة النهائية', align: 'num', cell: (row) => money(row.unitCostAtPost) },
            { key: 'total', header: 'الإجمالي', align: 'num', cell: (row) => money(row.total) },
          ]}
        />
      </div>

      {doc.costs.length > 0 && (
        <div className="card">
          <h2>المصاريف الإضافية</h2>
          <DataTable
            rows={doc.costs}
            rowKey={(row) => row.id}
            columns={[
              { key: 'name', header: 'البيان', cell: (row) => row.costName },
              { key: 'target', header: 'التوزيع', cell: (row) => (row.allocationTarget === 'inventory' ? 'على تكلفة الأصناف' : 'مصروف مباشر') },
              { key: 'amount', header: 'المبلغ', align: 'num', cell: (row) => money(row.amount) },
            ]}
          />
        </div>
      )}
    </Screen>
  );
}
