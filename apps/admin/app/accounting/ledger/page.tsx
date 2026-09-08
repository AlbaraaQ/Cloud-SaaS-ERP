'use client';

import { useMemo, useState } from 'react';

import { Empty, ErrorBox, Forbidden, Loading, Screen } from '../../../components/screen';
import { apiData } from '../../../lib/api';
import { accountLabel, downloadCsv, nameOf, postableOf, type Account } from '../../../lib/accounts';
import { useQuery } from '../../../lib/use-query';

type LedgerRow = {
  entryId: string;
  date: string;
  number: string | null;
  description: string | null;
  debit: string;
  credit: string;
};

function money(value: string | number) {
  return Number(value || 0).toLocaleString('ar-SA', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export default function AccountStatementPage() {
  const accounts = useQuery<Account[]>(() => apiData<Account[]>('/accounts'), []);
  const [accountId, setAccountId] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');

  const ledger = useQuery<LedgerRow[]>(
    () => (accountId ? apiData<LedgerRow[]>(`/statements/general-ledger/${accountId}`) : Promise.resolve([])),
    [accountId],
  );

  const account = (accounts.data ?? []).find((entry) => entry.id === accountId);

  const rows = useMemo(() => {
    const filtered = (ledger.data ?? [])
      .filter((row) => (from ? row.date >= from : true))
      .filter((row) => (to ? row.date <= to : true))
      .sort((left, right) => left.date.localeCompare(right.date) || (left.number ?? '').localeCompare(right.number ?? ''));

    let running = 0;
    return filtered.map((row) => {
      running += Number(row.debit || 0) - Number(row.credit || 0);
      return { ...row, running };
    });
  }, [ledger.data, from, to]);

  const totals = rows.reduce(
    (accumulator, row) => {
      accumulator.debit += Number(row.debit || 0);
      accumulator.credit += Number(row.credit || 0);
      return accumulator;
    },
    { debit: 0, credit: 0 },
  );

  return (
    <Screen
      title="كشف حساب"
      subtitle="حركة حساب واحد مع الرصيد التراكمي."
      crumbs={['المحاسبة', 'تقارير محاسبية']}
      actions={
        <>
          <button className="btn" type="button" onClick={() => window.print()} disabled={!accountId}>
            طباعة
          </button>
          <button
            className="btn"
            type="button"
            disabled={rows.length === 0}
            onClick={() =>
              downloadCsv(
                `statement-${account?.code ?? 'account'}.csv`,
                ['التاريخ', 'رقم القيد', 'البيان', 'مدين', 'دائن', 'الرصيد'],
                rows.map((row) => [row.date, row.number ?? '', row.description ?? '', row.debit, row.credit, row.running.toFixed(2)]),
              )
            }
          >
            تصدير CSV
          </button>
        </>
      }
    >
      <div className="card tight no-print">
        <div className="row">
          <label className="field" style={{ margin: 0, minWidth: 280, flex: 1 }}>
            <span>الحساب</span>
            <select className="input" value={accountId} onChange={(event) => setAccountId(event.target.value)}>
              <option value="">— اختر الحساب</option>
              {(accounts.data ?? [])
                .filter(postableOf)
                .sort((left, right) => left.code.localeCompare(right.code))
                .map((entry) => (
                  <option key={entry.id} value={entry.id}>
                    {accountLabel(entry)}
                  </option>
                ))}
            </select>
          </label>
          <label className="field" style={{ margin: 0 }}>
            <span>من</span>
            <input className="input" type="date" value={from} onChange={(event) => setFrom(event.target.value)} />
          </label>
          <label className="field" style={{ margin: 0 }}>
            <span>إلى</span>
            <input className="input" type="date" value={to} onChange={(event) => setTo(event.target.value)} />
          </label>
        </div>
      </div>

      {!accountId && <Empty title="اختر حساباً لعرض كشفه" />}
      {accountId && ledger.status === 'loading' && <Loading />}
      {accountId && ledger.status === 'forbidden' && <Forbidden />}
      {accountId && ledger.status === 'error' && <ErrorBox message={ledger.error} onRetry={ledger.reload} />}
      {accountId && ledger.status === 'success' && (
        <>
          <div className="card">
            <dl className="kv">
              <dt>الحساب</dt>
              <dd>
                <span dir="ltr">{account?.code}</span> — {account ? nameOf(account) : ''}
              </dd>
              <dt>عدد الحركات</dt>
              <dd>{rows.length}</dd>
              <dt>الرصيد الختامي</dt>
              <dd>{money(totals.debit - totals.credit)}</dd>
            </dl>
          </div>

          {rows.length === 0 ? (
            <Empty title="لا توجد حركات على هذا الحساب في المدى المحدد" />
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>التاريخ</th>
                    <th>رقم القيد</th>
                    <th>البيان</th>
                    <th className="num">مدين</th>
                    <th className="num">دائن</th>
                    <th className="num">الرصيد</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((row, index) => (
                    <tr key={`${row.entryId}-${index}`}>
                      <td dir="ltr">{row.date}</td>
                      <td dir="ltr">{row.number ?? '—'}</td>
                      <td>{row.description ?? '—'}</td>
                      <td className="num">{money(row.debit)}</td>
                      <td className="num">{money(row.credit)}</td>
                      <td className="num">{money(row.running)}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr>
                    <th colSpan={3}>الإجمالي</th>
                    <th className="num">{money(totals.debit)}</th>
                    <th className="num">{money(totals.credit)}</th>
                    <th className="num">{money(totals.debit - totals.credit)}</th>
                  </tr>
                </tfoot>
              </table>
            </div>
          )}
        </>
      )}
    </Screen>
  );
}
