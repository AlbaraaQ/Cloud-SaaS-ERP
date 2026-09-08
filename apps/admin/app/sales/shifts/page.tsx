'use client';

import { useState } from 'react';

import { DataTable, Notice, QueryView } from '../../../components/data-view';
import { Screen } from '../../../components/screen';
import { ApiError, apiData, apiList, apiPost } from '../../../lib/api';
import { branchOptions, dateTime, defaultOf, listBranches, money, statusLabel, type Branch } from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type Shift = {
  id: string;
  branchId: string;
  status: string;
  openedAt: string;
  closedAt: string | null;
  expectedCash: string;
  countedCash: string;
  diff: string;
};

const DENOMINATIONS = ['500', '200', '100', '50', '20', '10', '5', '1', '0.5'];

export default function ShiftsPage() {
  const { can } = useSession();
  const branches = useQuery<Branch[]>(() => listBranches(), []);
  const branchRows = branches.data ?? [];
  const [branchId, setBranchId] = useState('');
  const effectiveBranch = branchId || defaultOf(branchRows)?.id || '';

  const shifts = useQuery<Shift[]>(() => apiList<Shift>(`/shift-closes${effectiveBranch ? `?branch_id=${effectiveBranch}` : ''}`), [effectiveBranch]);
  const current = useQuery<Shift | null>(
    () => (effectiveBranch ? apiData<Shift | null>(`/shift-closes/current?branch_id=${effectiveBranch}`) : Promise.resolve(null)),
    [effectiveBranch],
  );

  const [counts, setCounts] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();

  const countedValue = DENOMINATIONS.reduce((sum, denomination) => sum + Number(denomination) * Number(counts[denomination] ?? 0), 0);

  async function run(action: () => Promise<unknown>, okText: string) {
    setBusy(true);
    setNotice(undefined);
    try {
      await action();
      setNotice({ kind: 'ok', text: okText });
      shifts.reload();
      current.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  const openShift = current.data;

  return (
    <Screen
      title="إغلاق اليومية"
      subtitle="فتح وردية الكاشير، ثم جرد النقد وإغلاقها بمقارنة المتوقع بالمعدود."
      crumbs={['المبيعات', 'العمليات']}
    >
      <div className="card toolbar">
        <label className="field">
          <span>الفرع</span>
          <select className="input" value={effectiveBranch} onChange={(event) => setBranchId(event.target.value)}>
            {branchOptions(branchRows).map((option) => (
              <option key={option.id} value={option.id}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
      </div>

      <div className="card">
        <h2>الوردية الحالية</h2>
        <Notice notice={notice} />
        {openShift ? (
          <>
            <dl className="kv">
              <dt>فُتحت</dt>
              <dd>{dateTime(openShift.openedAt)}</dd>
              <dt>النقد المتوقع</dt>
              <dd>{money(openShift.expectedCash)}</dd>
            </dl>

            <h3>جرد النقد</h3>
            <div className="form-grid">
              {DENOMINATIONS.map((denomination) => (
                <label className="field" key={denomination}>
                  <span>فئة {denomination}</span>
                  <input
                    className="input"
                    dir="ltr"
                    inputMode="numeric"
                    value={counts[denomination] ?? ''}
                    onChange={(event) => setCounts((data) => ({ ...data, [denomination]: event.target.value }))}
                  />
                </label>
              ))}
            </div>
            <dl className="kv">
              <dt>إجمالي المعدود</dt>
              <dd>
                <strong>{money(countedValue)}</strong>
              </dd>
            </dl>

            {can('treasury.shift.close') && (
              <button
                className="btn primary"
                type="button"
                disabled={busy}
                onClick={() =>
                  run(
                    () =>
                      apiPost(`/shift-closes/${openShift.id}/close`, {
                        counts: DENOMINATIONS.filter((denomination) => Number(counts[denomination] ?? 0) > 0).map((denomination) => ({
                          denomination,
                          count: Number(counts[denomination] ?? 0),
                        })),
                      }),
                    'تم إغلاق الوردية.',
                  )
                }
              >
                إغلاق الوردية
              </button>
            )}
          </>
        ) : (
          <>
            <p className="muted">لا توجد وردية مفتوحة لهذا الفرع.</p>
            {can('treasury.shift.close') && (
              <button className="btn primary" type="button" disabled={busy || !effectiveBranch} onClick={() => run(() => apiPost('/shift-closes/open', { branchId: effectiveBranch }), 'تم فتح الوردية.')}>
                فتح وردية
              </button>
            )}
          </>
        )}
      </div>

      <QueryView query={shifts} empty="لا توجد إغلاقات" emptyDetail="ستظهر الورديات المغلقة هنا.">
        {(rows) => (
          <DataTable
            rows={rows}
            rowKey={(row) => row.id}
            columns={[
              { key: 'opened', header: 'الفتح', align: 'ltr', cell: (row) => dateTime(row.openedAt) },
              { key: 'closed', header: 'الإغلاق', align: 'ltr', cell: (row) => (row.closedAt ? dateTime(row.closedAt) : '—') },
              { key: 'expected', header: 'المتوقع', align: 'num', cell: (row) => money(row.expectedCash) },
              { key: 'counted', header: 'المعدود', align: 'num', cell: (row) => money(row.countedCash) },
              { key: 'diff', header: 'الفرق', align: 'num', cell: (row) => money(row.diff) },
              { key: 'status', header: 'الحالة', cell: (row) => <span className="badge">{statusLabel(row.status)}</span> },
            ]}
          />
        )}
      </QueryView>
    </Screen>
  );
}
