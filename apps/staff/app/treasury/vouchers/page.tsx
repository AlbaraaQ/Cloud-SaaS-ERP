'use client';

import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Suspense, useState } from 'react';

import { DataTable, Notice, QueryView } from '../../../components/data-view';
import { Screen } from '../../../components/screen';
import { accountLabel, listAccounts, postableOf, type Account } from '../../../lib/accounts';
import { ApiError, apiList, apiPost } from '../../../lib/api';
import {
  branchOptions,
  cashLocationLabel,
  defaultOf,
  listBranches,
  listCashLocations,
  listParties,
  money,
  partyLabel,
  shortDate,
  statusLabel,
  today,
  type Branch,
  type CashLocation,
  type Party,
} from '../../../lib/lookups';
import { periodForDate } from '../../../lib/posting';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type Voucher = {
  id: string;
  number: string | null;
  kind: 'receipt' | 'payment';
  subtype: string;
  status: string;
  date: string;
  partyId: string | null;
  cashLocationId: string;
  method: string;
  amount: string;
  currency: string;
  recipient: string | null;
};

const SUBTYPES: Array<{ id: string; label: string; kind: 'receipt' | 'payment' | 'both' }> = [
  { id: 'customer', label: 'عميل', kind: 'both' },
  { id: 'supplier', label: 'مورد', kind: 'both' },
  { id: 'expense', label: 'مصروف', kind: 'payment' },
  { id: 'salary', label: 'راتب', kind: 'payment' },
  { id: 'vat', label: 'ضريبة القيمة المضافة', kind: 'payment' },
  { id: 'account', label: 'حساب عام', kind: 'both' },
  { id: 'other', label: 'أخرى', kind: 'both' },
];

const METHODS: Array<{ id: string; label: string }> = [
  { id: 'cash', label: 'نقداً' },
  { id: 'cheque', label: 'شيك' },
  { id: 'bank_transfer', label: 'تحويل بنكي' },
  { id: 'card', label: 'شبكة' },
];

function VouchersScreen() {
  const searchParams = useSearchParams();
  const initialKind = searchParams.get('kind') === 'payment' ? 'payment' : 'receipt';
  const { can } = useSession();

  const vouchers = useQuery<Voucher[]>(() => apiList<Voucher>('/vouchers'), []);
  const branches = useQuery<Branch[]>(() => listBranches(), []);
  const cashLocations = useQuery<CashLocation[]>(() => listCashLocations(), []);
  const parties = useQuery<Party[]>(() => listParties(), []);
  const accounts = useQuery<Account[]>(() => listAccounts(), []);

  const [open, setOpen] = useState(false);
  const [kind, setKind] = useState<'receipt' | 'payment'>(initialKind);
  const [subtype, setSubtype] = useState('customer');
  const [date, setDate] = useState(today());
  const [branchId, setBranchId] = useState('');
  const [cashLocationId, setCashLocationId] = useState('');
  const [partyId, setPartyId] = useState('');
  const [counterAccountId, setCounterAccountId] = useState('');
  const [method, setMethod] = useState('cash');
  const [amountText, setAmountText] = useState('');
  const [referenceNo, setReferenceNo] = useState('');
  const [recipient, setRecipient] = useState('');
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();

  const branchRows = branches.data ?? [];
  const cashRows = cashLocations.data ?? [];
  const effectiveBranch = branchId || defaultOf(branchRows)?.id || '';
  const postable = (accounts.data ?? []).filter((account) => postableOf(account));
  const filteredSubtypes = SUBTYPES.filter((entry) => entry.kind === 'both' || entry.kind === kind);
  const rows = (vouchers.data ?? []).filter((row) => row.kind === kind);

  async function run(action: () => Promise<unknown>, okText: string) {
    setBusy(true);
    setNotice(undefined);
    try {
      await action();
      setNotice({ kind: 'ok', text: okText });
      vouchers.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  async function create(event: React.FormEvent) {
    event.preventDefault();
    await run(async () => {
      await apiPost('/vouchers', {
        branchId: effectiveBranch,
        kind,
        subtype,
        date,
        partyId: partyId || undefined,
        counterAccountId: counterAccountId || undefined,
        cashLocationId,
        method,
        amount: amountText.trim(),
        referenceNo: referenceNo.trim() || undefined,
        recipient: recipient.trim() || undefined,
        idempotencyKey: crypto.randomUUID(),
      });
      setAmountText('');
      setReferenceNo('');
      setRecipient('');
    }, 'تم إنشاء السند كمسودة. رحّله ليؤثر على الحسابات.');
  }

  /** Posting a voucher is two lines: the cash location's account against the counter account. */
  async function postVoucher(voucher: Voucher) {
    const location = cashRows.find((row) => row.id === voucher.cashLocationId);
    const cashAccountId = location?.accountId ?? location?.account_id;
    if (!cashAccountId) throw new ApiError(422, 'CASH_ACCOUNT_MISSING', 'الصندوق غير مرتبط بحساب في دليل الحسابات.');
    if (!counterAccountId) throw new ApiError(422, 'COUNTER_ACCOUNT_REQUIRED', 'اختر الحساب المقابل في نموذج السند قبل الترحيل.');
    const period = await periodForDate(voucher.date.slice(0, 10));
    if (!period) throw new ApiError(422, 'PERIOD_NOT_FOUND', 'لا توجد فترة محاسبية تغطي تاريخ السند.');

    await apiPost(`/vouchers/${voucher.id}/post`, {
      fiscalPeriodId: period.id,
      journalLines:
        voucher.kind === 'receipt'
          ? [
              { accountId: cashAccountId, debit: voucher.amount },
              { accountId: counterAccountId, credit: voucher.amount, partyId: voucher.partyId ?? undefined },
            ]
          : [
              { accountId: counterAccountId, debit: voucher.amount, partyId: voucher.partyId ?? undefined },
              { accountId: cashAccountId, credit: voucher.amount },
            ],
    });
  }

  return (
    <Screen
      title={kind === 'receipt' ? 'سندات القبض' : 'سندات الصرف'}
      subtitle="سند مسودة ← ترحيل بقيد من طرفين: الصندوق/البنك مقابل حساب الطرف أو المصروف."
      crumbs={['الخزينة', 'العمليات']}
      actions={
        can('treasury.voucher.create') ? (
          <button className="btn primary" type="button" onClick={() => setOpen(!open)}>
            {open ? 'إغلاق' : 'سند جديد'}
          </button>
        ) : null
      }
    >
      <div className="card toolbar">
        <label className="field">
          <span>النوع</span>
          <select className="input" value={kind} onChange={(event) => setKind(event.target.value as 'receipt' | 'payment')}>
            <option value="receipt">سند قبض</option>
            <option value="payment">سند صرف</option>
          </select>
        </label>
      </div>

      {open && (
        <form className="card" onSubmit={create}>
          <h2>{kind === 'receipt' ? 'سند قبض جديد' : 'سند صرف جديد'}</h2>
          <div className="form-grid">
            <label className="field">
              <span>الفرع *</span>
              <select className="input" value={effectiveBranch} onChange={(event) => setBranchId(event.target.value)} required>
                {branchOptions(branchRows).map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>الجهة</span>
              <select className="input" value={subtype} onChange={(event) => setSubtype(event.target.value)}>
                {filteredSubtypes.map((entry) => (
                  <option key={entry.id} value={entry.id}>
                    {entry.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>التاريخ *</span>
              <input className="input" type="date" dir="ltr" value={date} onChange={(event) => setDate(event.target.value)} required />
            </label>
            <label className="field">
              <span>الصندوق / البنك *</span>
              <select className="input" value={cashLocationId} onChange={(event) => setCashLocationId(event.target.value)} required>
                <option value="">— اختر —</option>
                {cashRows.map((row) => (
                  <option key={row.id} value={row.id}>
                    {cashLocationLabel(row)}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>الطرف (عميل / مورد)</span>
              <select className="input" value={partyId} onChange={(event) => setPartyId(event.target.value)}>
                <option value="">— بدون —</option>
                {(parties.data ?? []).map((row) => (
                  <option key={row.id} value={row.id}>
                    {partyLabel(row)}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>الحساب المقابل *</span>
              <select className="input" value={counterAccountId} onChange={(event) => setCounterAccountId(event.target.value)} required>
                <option value="">— اختر —</option>
                {postable.map((account) => (
                  <option key={account.id} value={account.id}>
                    {accountLabel(account)}
                  </option>
                ))}
              </select>
              <span className="muted small">حساب العميل/المورد أو حساب المصروف الذي يقابل حركة الصندوق.</span>
            </label>
            <label className="field">
              <span>طريقة الدفع</span>
              <select className="input" value={method} onChange={(event) => setMethod(event.target.value)}>
                {METHODS.map((entry) => (
                  <option key={entry.id} value={entry.id}>
                    {entry.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>المبلغ *</span>
              <input className="input" dir="ltr" inputMode="decimal" value={amountText} onChange={(event) => setAmountText(event.target.value)} required />
            </label>
            <label className="field">
              <span>المرجع</span>
              <input className="input" dir="ltr" value={referenceNo} onChange={(event) => setReferenceNo(event.target.value)} />
            </label>
            <label className="field">
              <span>المستلم / الدافع</span>
              <input className="input" value={recipient} onChange={(event) => setRecipient(event.target.value)} />
            </label>
          </div>
          <Notice notice={notice} />
          <button className="btn primary" type="submit" disabled={busy}>
            {busy ? 'جارٍ الحفظ…' : 'حفظ السند'}
          </button>
        </form>
      )}

      {!open && <Notice notice={notice} />}

      <QueryView query={vouchers} isEmpty={() => rows.length === 0} empty="لا توجد سندات" emptyDetail="أنشئ سند قبض أو صرف جديداً.">
        {() => (
          <DataTable
            rows={rows}
            rowKey={(row) => row.id}
            columns={[
              { key: 'number', header: 'الرقم', align: 'ltr', cell: (row) => row.number ?? 'مسودة' },
              { key: 'date', header: 'التاريخ', align: 'ltr', cell: (row) => shortDate(row.date) },
              {
                key: 'party',
                header: 'الطرف',
                cell: (row) => {
                  const party = (parties.data ?? []).find((entry) => entry.id === row.partyId);
                  return party ? partyLabel(party) : (row.recipient ?? '—');
                },
              },
              {
                key: 'cash',
                header: 'الصندوق',
                cell: (row) => {
                  const location = cashRows.find((entry) => entry.id === row.cashLocationId);
                  return location ? cashLocationLabel(location) : '—';
                },
              },
              { key: 'method', header: 'الطريقة', cell: (row) => METHODS.find((entry) => entry.id === row.method)?.label ?? row.method },
              { key: 'amount', header: 'المبلغ', align: 'num', cell: (row) => money(row.amount, row.currency) },
              { key: 'status', header: 'الحالة', cell: (row) => <span className="badge">{statusLabel(row.status)}</span> },
              {
                key: 'actions',
                header: '',
                cell: (row) => (
                  <span className="row">
                    <Link className="btn sm" href={`/print/voucher/${row.id}`}>
                      طباعة
                    </Link>
                    {row.status === 'draft' && can('treasury.voucher.post') && (
                      <button className="btn sm primary" type="button" disabled={busy} onClick={() => run(() => postVoucher(row), 'تم ترحيل السند.')}>
                        ترحيل
                      </button>
                    )}
                    {row.status === 'posted' && can('treasury.voucher.void') && (
                      <button
                        className="btn sm danger"
                        type="button"
                        disabled={busy}
                        onClick={() => run(() => apiPost(`/vouchers/${row.id}/void`, { reason: 'إلغاء من لوحة السندات' }), 'تم إلغاء السند.')}
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

export default function VouchersPage() {
  return (
    <Suspense fallback={null}>
      <VouchersScreen />
    </Suspense>
  );
}
