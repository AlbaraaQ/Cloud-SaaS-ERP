'use client';

import { useMemo } from 'react';

import { Empty, ErrorBox, Forbidden, Loading, Screen } from '../../../components/screen';
import { apiData } from '../../../lib/api';
import { ACCOUNT_TYPE_LABELS, downloadCsv, nameOf, typeOf, type Account } from '../../../lib/accounts';
import { useQuery } from '../../../lib/use-query';

type TrialRow = { accountId: string; debit: string; credit: string; balance: string };

function money(value: string | number) {
  return Number(value || 0).toLocaleString('ar-SA', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export default function TrialBalancePage() {
  const trial = useQuery<TrialRow[]>(() => apiData<TrialRow[]>('/statements/trial-balance'), []);
  const accounts = useQuery<Account[]>(() => apiData<Account[]>('/accounts'), []);

  const rows = useMemo(() => {
    const index = new Map((accounts.data ?? []).map((account) => [account.id, account]));
    return (trial.data ?? [])
      .map((row) => {
        const account = index.get(row.accountId);
        return {
          ...row,
          code: account?.code ?? row.accountId.slice(0, 8),
          name: account ? nameOf(account) : '—',
          type: account ? typeOf(account) : '',
        };
      })
      .sort((left, right) => left.code.localeCompare(right.code));
  }, [trial.data, accounts.data]);

  const totals = rows.reduce(
    (accumulator, row) => {
      accumulator.debit += Number(row.debit || 0);
      accumulator.credit += Number(row.credit || 0);
      return accumulator;
    },
    { debit: 0, credit: 0 },
  );
  const balanced = Math.abs(totals.debit - totals.credit) < 0.005;

  return (
    <Screen
      title="ميزان المراجعة"
      subtitle="مجاميع المدين والدائن لكل حساب من القيود المرحّلة."
      crumbs={['المحاسبة', 'تقارير محاسبية']}
      actions={
        <>
          <button className="btn" type="button" onClick={() => window.print()}>
            طباعة
          </button>
          <button
            className="btn"
            type="button"
            onClick={() =>
              downloadCsv(
                'trial-balance.csv',
                ['الرمز', 'الحساب', 'النوع', 'مدين', 'دائن', 'الرصيد'],
                rows.map((row) => [row.code, row.name, row.type, row.debit, row.credit, row.balance]),
              )
            }
          >
            تصدير CSV
          </button>
          <button className="btn" type="button" onClick={trial.reload}>
            تحديث
          </button>
        </>
      }
    >
      {(trial.status === 'loading' || accounts.status === 'loading') && <Loading />}
      {trial.status === 'forbidden' && <Forbidden />}
      {trial.status === 'error' && <ErrorBox message={trial.error} onRetry={trial.reload} />}
      {trial.status === 'success' &&
        (rows.length === 0 ? (
          <Empty title="لا توجد حركات مرحّلة بعد" detail="سجّل قيداً واحداً على الأقل ليظهر ميزان المراجعة." />
        ) : (
          <>
            <div className="grid cols">
              <article className="card">
                <p className="muted">إجمالي المدين</p>
                <div className="kpi" style={{ fontSize: 22 }}>{money(totals.debit)}</div>
              </article>
              <article className="card">
                <p className="muted">إجمالي الدائن</p>
                <div className="kpi" style={{ fontSize: 22 }}>{money(totals.credit)}</div>
              </article>
              <article className="card">
                <p className="muted">حالة التوازن</p>
                <div className="kpi" style={{ fontSize: 22 }}>
                  <span className={`badge ${balanced ? 'active' : 'failed'}`}>{balanced ? 'متوازن' : 'غير متوازن'}</span>
                </div>
                <small className="muted">الفرق {money(totals.debit - totals.credit)}</small>
              </article>
            </div>

            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>الرمز</th>
                    <th>الحساب</th>
                    <th>النوع</th>
                    <th className="num">مدين</th>
                    <th className="num">دائن</th>
                    <th className="num">الرصيد</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((row) => (
                    <tr key={row.accountId}>
                      <td dir="ltr">{row.code}</td>
                      <td>{row.name}</td>
                      <td>{ACCOUNT_TYPE_LABELS[row.type] ?? row.type}</td>
                      <td className="num">{money(row.debit)}</td>
                      <td className="num">{money(row.credit)}</td>
                      <td className="num">{money(row.balance)}</td>
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
          </>
        ))}
    </Screen>
  );
}
