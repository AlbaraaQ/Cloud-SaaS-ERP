'use client';

import Decimal from 'decimal.js';
import { useMemo, useState } from 'react';

import { DataTable, Notice } from '../../../components/data-view';
import { Screen } from '../../../components/screen';
import { StatTile, StatTiles } from '../../../components/ui';
import { ApiError, apiData, apiList, apiPost } from '../../../lib/api';
import {
  branchOptions,
  dateTime,
  defaultOf,
  listBranches,
  listEmployees,
  money,
  today,
  type Branch,
  type Employee,
} from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

/**
 * 📊 إغلاقات اليومية — `Form_WPF/frmCloseShift.xaml` («عرض وإدارة إغلاقات وردية
 * الموظفين»).
 *
 * The desktop reads `CasherClosed` joined with `CasherClosed_Sub`
 * (`frmCloseShift.xaml.cs:214` and `:270`) and puts one row per close on the grid: what
 * the drawer took (💵 النقدي · 🌐 الشبكة · 💰 مجموع الشبكة والنقدي · 📋 آجل · 🧾 الضريبة ·
 * 🏷️ الخصم · 💹 الصافي) beside what left it (📤 المصاريف · 🛒 المشتريات · 🚗 توصيل ·
 * ☕ الضيافة · 🛡️ تأمين), with `🏦 رصيد الصندوق` against `📉 الفرق` first among equals.
 *
 * The cloud could close a drawer before this screen, but the close had no number, no grid
 * and none of those columns — and the only way to see one was the POS shift page, which
 * is where a cashier *works*, not where an accountant *audits*.
 */

type Expenses = {
  total: string;
  delivery: string;
  hospitality: string;
  purchases: string;
  insurance: string;
  other: string;
};

type DayClose = {
  id: string;
  number: string | null;
  status: 'open' | 'closed';
  employee: string;
  employeeId: string;
  openedAt: string;
  closedAt: string | null;
  net: string;
  safeBalance: string;
  expected: string;
  diff: string;
  postponed: string;
  network: string;
  cash: string;
  sumCashAndNetwork: string;
  tax: string;
  discount: string;
  expenses: Expenses;
};

type CloseDetail = {
  shift: { id: string; number: string | null; status: string };
  counts: Array<{ denomination: string; count: number; total: string }>;
  lines: Array<{ kind: string; method: string | null; amount: string }>;
};

/** The denominations a cashier actually counts — ريال and its halves. */
const DENOMINATIONS = ['500', '200', '100', '50', '20', '10', '5', '1', '0.5'];

const BLANK: DayClose[] = [];

export default function DayClosePage() {
  const { can } = useSession();
  const [branchId, setBranchId] = useState('');
  const [employeeId, setEmployeeId] = useState('');
  const [allPeriod, setAllPeriod] = useState(true);
  const [from, setFrom] = useState(today());
  const [to, setTo] = useState(today());
  const [counts, setCounts] = useState<Record<string, string>>({});
  const [opened, setOpened] = useState<DayClose | undefined>();
  const [detail, setDetail] = useState<CloseDetail | undefined>();
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();

  const branches = useQuery<Branch[]>(() => listBranches(), []);
  const branchRows = branches.data ?? [];
  const effectiveBranch = branchId || defaultOf(branchRows)?.id || '';

  const employees = useQuery<Employee[]>(() => listEmployees(), []);
  const employeeRows = employees.data ?? [];

  const query = useMemo(() => {
    const params = new URLSearchParams();
    if (effectiveBranch) params.set('branch_id', effectiveBranch);
    if (!allPeriod) {
      if (from) params.set('from', from);
      if (to) params.set('to', to);
    }
    if (employeeId) params.set('membership_id', employeeId);
    return params.toString();
  }, [effectiveBranch, allPeriod, from, to, employeeId]);

  const closes = useQuery<DayClose[]>(() => apiList<DayClose>(`/shift-closes/day-closes?${query}`), [query]);
  const rows = closes.data ?? BLANK;

  const totals = useMemo(() => {
    const addUp = (pick: (row: DayClose) => string) =>
      rows.reduce((running, row) => running.plus(new Decimal(pick(row) || '0')), new Decimal(0));
    return {
      count: rows.length,
      cash: addUp((row) => row.cash),
      network: addUp((row) => row.network),
      tax: addUp((row) => row.tax),
      expenses: addUp((row) => row.expenses.total),
      diff: addUp((row) => row.diff),
    };
  }, [rows]);

  const countedValue = DENOMINATIONS.reduce(
    (sum, denomination) => sum + Number(denomination) * Number(counts[denomination] ?? 0),
    0,
  );

  async function run(action: () => Promise<unknown>, okText: string) {
    setBusy(true);
    setNotice(undefined);
    try {
      await action();
      setNotice({ kind: 'ok', text: okText });
      setCounts({});
      setOpened(undefined);
      closes.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  async function showDetail(row: DayClose) {
    if (detail?.shift.id === row.id) {
      setDetail(undefined);
      return;
    }
    const loaded = await apiData<CloseDetail>(`/shift-closes/${row.id}`);
    setDetail(loaded);
  }

  return (
    <Screen
      title="📊 إغلاقات اليومية"
      subtitle="عرض وإدارة إغلاقات وردية الموظفين — كما في frmCloseShift: ما أخذه الدرج وما خرج منه، ورصيد الصندوق مقابل الفرق."
      crumbs={['الخزينة', 'العمليات']}
    >
      <StatTiles>
        <StatTile label="📊 عدد الإغلاقات" value={totals.count} hint="في الفترة المحددة" />
        <StatTile label="💵 إجمالي النقدي" value={money(totals.cash.toFixed(4))} tone="ok" />
        <StatTile label="🌐 إجمالي الشبكة" value={money(totals.network.toFixed(4))} tone="brand" />
        <StatTile label="🧾 الضريبة" value={money(totals.tax.toFixed(4))} />
        <StatTile label="📤 المصاريف" value={money(totals.expenses.toFixed(4))} tone="danger" />
        <StatTile
          label="📉 مجموع الفروق"
          value={money(totals.diff.toFixed(4))}
          hint="المعدود − المتوقع"
          tone={totals.diff.isZero() ? 'ok' : 'warn'}
        />
      </StatTiles>

      {/* الوردية المفتوحة — فتح الدرج وجرده، وهو ما يحتاجه الكاشير قبل ما يحتاجه المحاسب */}
      <div className="card">
        <h2>🏦 الوردية الحالية</h2>
        <Notice notice={notice} />
        {opened ? (
          <>
            <dl className="kv">
              <dt>👤 الموظف</dt>
              <dd>{opened.employee || '—'}</dd>
              <dt>📅 إغلاق من</dt>
              <dd>{dateTime(opened.openedAt)}</dd>
              <dt>💵 النقدي</dt>
              <dd>{money(opened.cash)}</dd>
              <dt>🌐 الشبكة</dt>
              <dd>{money(opened.network)}</dd>
              <dt>📋 آجل</dt>
              <dd>{money(opened.postponed)}</dd>
              <dt>📤 المصاريف</dt>
              <dd>{money(opened.expenses.total)}</dd>
              <dt>النقد المتوقع الآن</dt>
              <dd>
                <strong>{money(opened.expected)}</strong>
              </dd>
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
              <dt>🏦 رصيد الصندوق (المعدود)</dt>
              <dd>
                <strong>{money(String(countedValue))}</strong>
              </dd>
              <dt>📉 الفرق</dt>
              <dd>
                <strong>{money(new Decimal(countedValue).minus(new Decimal(opened.expected || '0')).toFixed(4))}</strong>
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
                      apiPost(`/shift-closes/${opened.id}/close`, {
                        counts: DENOMINATIONS.filter((denomination) => Number(counts[denomination] ?? 0) > 0).map(
                          (denomination) => ({ denomination, count: Number(counts[denomination] ?? 0) }),
                        ),
                      }),
                    'تم إغلاق الوردية وترقيمها.',
                  )
                }
              >
                ✔ إغلاق الوردية
              </button>
            )}
          </>
        ) : (
          <>
            <p className="muted">
              اختر وردية مفتوحة من الجدول لجردها وإغلاقها، أو افتح وردية جديدة لهذا الفرع.
            </p>
            {can('treasury.shift.close') && (
              <button
                className="btn primary"
                type="button"
                disabled={busy || !effectiveBranch}
                onClick={() =>
                  run(() => apiPost('/shift-closes/open', { branchId: effectiveBranch }), 'تم فتح الوردية.')
                }
              >
                ➕ فتح وردية
              </button>
            )}
          </>
        )}
      </div>

      {/* 🔍 بيانات البحث — `frmCloseShift.xaml`: من تاريخ / إلى تاريخ، 📆 كل الفترة، 👤 الموظف، 👁️ عرض */}
      <div className="card toolbar">
        <label className="field">
          <span>🏢 الفرع</span>
          <select className="input" value={effectiveBranch} onChange={(event) => setBranchId(event.target.value)}>
            {branchOptions(branchRows).map((option) => (
              <option key={option.id} value={option.id}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>📅 التاريخ من</span>
          <input
            className="input"
            type="date"
            dir="ltr"
            value={from}
            disabled={allPeriod}
            onChange={(event) => setFrom(event.target.value)}
          />
        </label>
        <label className="field">
          <span>📅 إلى</span>
          <input
            className="input"
            type="date"
            dir="ltr"
            value={to}
            disabled={allPeriod}
            onChange={(event) => setTo(event.target.value)}
          />
        </label>
        <label className="field">
          <span>👤 الموظف</span>
          <select className="input" value={employeeId} onChange={(event) => setEmployeeId(event.target.value)}>
            <option value="">📋 الكل</option>
            {employeeRows.map((employee) => (
              <option key={employee.id} value={employee.membershipId ?? ''}>{employee.name}</option>
            ))}
          </select>
        </label>
        <label className="row" style={{ gap: 6 }}>
          <input type="checkbox" checked={allPeriod} onChange={(event) => setAllPeriod(event.target.checked)} />
          <span className="small">📆 كل الفترة</span>
        </label>
        <button className="btn primary" type="button" onClick={() => closes.reload()}>
          👁️ عرض
        </button>
      </div>

      <div className="card">
        {closes.status === 'loading' ? (
          <p className="muted">… جارٍ تحميل الإغلاقات</p>
        ) : rows.length === 0 ? (
          <p className="muted">لا توجد إغلاقات في الفترة المحددة.</p>
        ) : (
          <DataTable
            rows={rows}
            rowKey={(row) => row.id}
            onRowClick={(row) => (row.status === 'open' ? setOpened(row) : void showDetail(row))}
            activeKey={opened?.id ?? detail?.shift.id}
            columns={[
              { key: 'number', header: '🔢 الرقم', align: 'ltr', cell: (row) => row.number ?? '—' },
              { key: 'employee', header: '👤 الموظف', cell: (row) => row.employee || '—' },
              { key: 'openedAt', header: '📅 إغلاق من', align: 'ltr', cell: (row) => dateTime(row.openedAt) },
              {
                key: 'closedAt',
                header: '📅 إغلاق إلى',
                align: 'ltr',
                cell: (row) => (row.closedAt ? dateTime(row.closedAt) : 'مفتوحة'),
              },
              { key: 'postponed', header: '📋 آجل', align: 'num', cell: (row) => money(row.postponed) },
              { key: 'network', header: '🌐 الشبكة', align: 'num', cell: (row) => money(row.network) },
              { key: 'cash', header: '💵 النقدي', align: 'num', cell: (row) => money(row.cash) },
              {
                key: 'sumCashAndNetwork',
                header: '💰 مجموع الشبكة والنقدي',
                align: 'num',
                cell: (row) => money(row.sumCashAndNetwork),
              },
              { key: 'expenses', header: '📤 المصاريف', align: 'num', cell: (row) => money(row.expenses.total) },
              { key: 'purchases', header: '🛒 المشتريات', align: 'num', cell: (row) => money(row.expenses.purchases) },
              { key: 'delivery', header: '🚗 توصيل', align: 'num', cell: (row) => money(row.expenses.delivery) },
              { key: 'insurance', header: '🛡️ تأمين', align: 'num', cell: (row) => money(row.expenses.insurance) },
              { key: 'hospitality', header: '☕ الضيافة', align: 'num', cell: (row) => money(row.expenses.hospitality) },
              { key: 'discount', header: '🏷️ الخصم', align: 'num', cell: (row) => money(row.discount) },
              { key: 'tax', header: '🧾 الضريبة', align: 'num', cell: (row) => money(row.tax) },
              { key: 'net', header: '💹 الصافي', align: 'num', cell: (row) => money(row.net) },
              { key: 'safeBalance', header: '🏦 رصيد الصندوق', align: 'num', cell: (row) => money(row.safeBalance) },
              {
                key: 'diff',
                header: '📉 الفرق',
                align: 'num',
                cell: (row) => (
                  <strong style={new Decimal(row.diff || '0').abs().gt(0) ? { color: '#b3261e' } : undefined}>
                    {money(row.diff)}
                  </strong>
                ),
              },
            ]}
            footer={[
              <strong key="foot-label">المجموع</strong>,
              '',
              '',
              '',
              '',
              <strong key="foot-network">{money(totals.network.toFixed(4))}</strong>,
              <strong key="foot-cash">{money(totals.cash.toFixed(4))}</strong>,
              '',
              <strong key="foot-expenses">{money(totals.expenses.toFixed(4))}</strong>,
              '',
              '',
              '',
              '',
              '',
              <strong key="foot-tax">{money(totals.tax.toFixed(4))}</strong>,
              '',
              '',
              <strong key="foot-diff">{money(totals.diff.toFixed(4))}</strong>,
            ]}
            expanded={(row) =>
              detail?.shift.id === row.id ? (
                <div className="grid">
                  <p className="muted" style={{ margin: 0 }}>
                    🧾 الملاحظات المعدودة ({detail.counts.length}) · سطور الملخّص ({detail.lines.length})
                  </p>
                  {detail.counts.length > 0 && (
                    <table className="compact">
                      <thead>
                        <tr>
                          <th>الفئة</th>
                          <th>العدد</th>
                          <th>الإجمالي</th>
                        </tr>
                      </thead>
                      <tbody>
                        {detail.counts.map((line) => (
                          <tr key={line.denomination}>
                            <td>{line.denomination}</td>
                            <td>{line.count}</td>
                            <td>{money(line.total)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
                  {detail.lines.length > 0 && (
                    <table className="compact">
                      <thead>
                        <tr>
                          <th>النوع</th>
                          <th>الطريقة</th>
                          <th>المبلغ</th>
                        </tr>
                      </thead>
                      <tbody>
                        {detail.lines.map((line, index) => (
                          <tr key={`${line.kind}-${index}`}>
                            <td>{line.kind}</td>
                            <td>{line.method ?? '—'}</td>
                            <td>{money(line.amount)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
                </div>
              ) : null
            }
          />
        )}
      </div>
    </Screen>
  );
}
