'use client';

import { Decimal } from 'decimal.js';
import { useSearchParams } from 'next/navigation';
import { Suspense, useMemo, useState } from 'react';

import { ErrorBox, Forbidden, Loading, Screen } from '../../../../components/screen';
import { ApiError, apiData, apiPost } from '../../../../lib/api';
import { accountLabel, postableOf, type Account } from '../../../../lib/accounts';
import { useSession } from '../../../../lib/session';
import { useQuery } from '../../../../lib/use-query';

type Branch = { id: string; code?: string; nameAr?: string; name_ar?: string; nameEn?: string };
type Period = { id: string; code?: string; name?: string; status?: string; startDate?: string; endDate?: string; start_date?: string; end_date?: string };
type CostCenter = { id: string; code?: string; nameAr?: string; name_ar?: string };

type Line = {
  key: number;
  accountId: string;
  debit: string;
  credit: string;
  costCenterId: string;
  description: string;
};

const emptyLine = (key: number): Line => ({ key, accountId: '', debit: '0.00', credit: '0.00', costCenterId: '', description: '' });

function decimal(value: string): Decimal {
  try {
    return new Decimal(value || '0');
  } catch {
    return new Decimal(0);
  }
}

function JournalEntryForm() {
  const { can } = useSession();
  const search = useSearchParams();
  const isOpening = search?.get('kind') === 'opening';

  const accounts = useQuery<Account[]>(() => apiData<Account[]>('/accounts'), []);
  const branches = useQuery<Branch[]>(() => apiData<Branch[]>('/branches'), []);
  const periods = useQuery<Period[]>(() => apiData<Period[]>('/fiscal-periods'), []);
  const costCenters = useQuery<CostCenter[]>(
    () => apiData<CostCenter[]>('/cost-centers').catch(() => [] as CostCenter[]),
    [],
  );

  const [branchId, setBranchId] = useState('');
  const [periodId, setPeriodId] = useState('');
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [description, setDescription] = useState(isOpening ? 'قيد افتتاحي' : '');
  const [lines, setLines] = useState<Line[]>([emptyLine(1), emptyLine(2)]);
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();

  const postable = useMemo(() => (accounts.data ?? []).filter(postableOf), [accounts.data]);
  const openPeriods = useMemo(
    () => (periods.data ?? []).filter((period) => (period.status ?? 'open') === 'open'),
    [periods.data],
  );

  const totals = useMemo(() => {
    const debit = lines.reduce((sum, line) => sum.plus(decimal(line.debit)), new Decimal(0));
    const credit = lines.reduce((sum, line) => sum.plus(decimal(line.credit)), new Decimal(0));
    return { debit, credit, difference: debit.minus(credit) };
  }, [lines]);

  const balanced = totals.difference.isZero() && totals.debit.greaterThan(0);
  const filledLines = lines.filter((line) => line.accountId && (decimal(line.debit).greaterThan(0) || decimal(line.credit).greaterThan(0)));

  function patch(key: number, changes: Partial<Line>) {
    setLines((current) => current.map((line) => (line.key === key ? { ...line, ...changes } : line)));
  }

  function addLine() {
    setLines((current) => [...current, emptyLine(Math.max(0, ...current.map((line) => line.key)) + 1)]);
  }

  function removeLine(key: number) {
    setLines((current) => (current.length <= 2 ? current : current.filter((line) => line.key !== key)));
  }

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setResult(undefined);
    if (!balanced) {
      setResult({ kind: 'danger', text: 'القيد غير متوازن — مجموع المدين يجب أن يساوي مجموع الدائن ولا يساوي صفراً.' });
      return;
    }
    setBusy(true);
    try {
      const payload = {
        branchId,
        fiscalPeriodId: periodId,
        date,
        description: description.trim() || undefined,
        lines: filledLines.map((line) => ({
          accountId: line.accountId,
          debit: decimal(line.debit).toFixed(2),
          credit: decimal(line.credit).toFixed(2),
          costCenterId: line.costCenterId || undefined,
          description: line.description.trim() || undefined,
        })),
      };
      const created = await apiPost<{ id?: string; entryNo?: string }>('/journal-entries', payload, {
        idempotencyKey: `je-${Date.now()}-${Math.random().toString(36).slice(2)}`,
      });
      setResult({ kind: 'ok', text: `تم ترحيل القيد بنجاح${created?.entryNo ? ` — رقم ${created.entryNo}` : ''}.` });
      setLines([emptyLine(1), emptyLine(2)]);
      setDescription(isOpening ? 'قيد افتتاحي' : '');
    } catch (error) {
      setResult({
        kind: 'danger',
        text:
          error instanceof ApiError
            ? `${error.message}${error.detail ? ` — ${error.detail}` : ''}`
            : 'تعذر ترحيل القيد.',
      });
    } finally {
      setBusy(false);
    }
  }

  if (!can('accounting.journal.post')) return <Forbidden />;
  if (accounts.status === 'loading' || branches.status === 'loading' || periods.status === 'loading') return <Loading rows={6} />;
  if (accounts.status === 'forbidden') return <Forbidden />;
  if (accounts.status === 'error') return <ErrorBox message={accounts.error} onRetry={accounts.reload} />;

  return (
    <Screen
      title={isOpening ? 'قيد إفتتاحي' : 'سند قيد'}
      subtitle="القيد المزدوج: كل سطر إما مدين أو دائن، والمجموعان يجب أن يتساويا قبل الترحيل."
      crumbs={['المحاسبة', 'العمليات']}
      actions={
        <button className="btn" type="button" onClick={() => window.print()}>
          طباعة
        </button>
      }
    >
      <form className="grid" onSubmit={submit}>
        <section className="card">
          <div className="form-grid">
            <label className="field">
              <span>الفرع *</span>
              <select className="input" value={branchId} onChange={(event) => setBranchId(event.target.value)} required>
                <option value="">— اختر الفرع</option>
                {(branches.data ?? []).map((branch) => (
                  <option key={branch.id} value={branch.id}>
                    {branch.code ? `${branch.code} — ` : ''}
                    {branch.nameAr ?? branch.name_ar ?? branch.nameEn ?? branch.id}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>الفترة المحاسبية *</span>
              <select className="input" value={periodId} onChange={(event) => setPeriodId(event.target.value)} required>
                <option value="">— اختر الفترة</option>
                {openPeriods.map((period) => (
                  <option key={period.id} value={period.id}>
                    {period.code ?? period.name ?? period.id}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>التاريخ *</span>
              <input className="input" type="date" value={date} onChange={(event) => setDate(event.target.value)} required />
            </label>
            <label className="field" style={{ gridColumn: '1 / -1' }}>
              <span>البيان</span>
              <input className="input" value={description} onChange={(event) => setDescription(event.target.value)} />
            </label>
          </div>
          {openPeriods.length === 0 && (
            <p className="alert warn">لا توجد فترة محاسبية مفتوحة. افتح فترة من شاشة «الفترات المحاسبية» أولاً.</p>
          )}
        </section>

        <section className="card">
          <div className="row" style={{ justifyContent: 'space-between', alignItems: 'center' }}>
            <h2 style={{ margin: 0 }}>تفاصيل القيد</h2>
            <button className="btn sm" type="button" onClick={addLine}>
              + سطر
            </button>
          </div>
          <div className="table-wrap" style={{ marginTop: 10 }}>
            <table>
              <thead>
                <tr>
                  <th style={{ minWidth: 220 }}>الحساب</th>
                  <th style={{ minWidth: 120 }} className="num">مدين</th>
                  <th style={{ minWidth: 120 }} className="num">دائن</th>
                  <th style={{ minWidth: 150 }}>مركز التكلفة</th>
                  <th style={{ minWidth: 180 }}>بيان السطر</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {lines.map((line) => (
                  <tr key={line.key}>
                    <td>
                      <select
                        className="input"
                        value={line.accountId}
                        onChange={(event) => patch(line.key, { accountId: event.target.value })}
                      >
                        <option value="">— اختر الحساب</option>
                        {postable.map((account) => (
                          <option key={account.id} value={account.id}>
                            {accountLabel(account)}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td>
                      <input
                        className="input"
                        inputMode="decimal"
                        dir="ltr"
                        value={line.debit}
                        onChange={(event) => patch(line.key, { debit: event.target.value, credit: '0.00' })}
                        onBlur={(event) => patch(line.key, { debit: decimal(event.target.value).toFixed(2) })}
                      />
                    </td>
                    <td>
                      <input
                        className="input"
                        inputMode="decimal"
                        dir="ltr"
                        value={line.credit}
                        onChange={(event) => patch(line.key, { credit: event.target.value, debit: '0.00' })}
                        onBlur={(event) => patch(line.key, { credit: decimal(event.target.value).toFixed(2) })}
                      />
                    </td>
                    <td>
                      <select
                        className="input"
                        value={line.costCenterId}
                        onChange={(event) => patch(line.key, { costCenterId: event.target.value })}
                      >
                        <option value="">—</option>
                        {(costCenters.data ?? []).map((center) => (
                          <option key={center.id} value={center.id}>
                            {center.code ? `${center.code} — ` : ''}
                            {center.nameAr ?? center.name_ar ?? center.id}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td>
                      <input
                        className="input"
                        value={line.description}
                        onChange={(event) => patch(line.key, { description: event.target.value })}
                      />
                    </td>
                    <td>
                      <button className="btn sm danger" type="button" onClick={() => removeLine(line.key)} aria-label="حذف السطر">
                        ×
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <th>الإجمالي</th>
                  <th className="num">{totals.debit.toFixed(2)}</th>
                  <th className="num">{totals.credit.toFixed(2)}</th>
                  <th colSpan={3}>
                    {balanced ? (
                      <span className="badge active">متوازن</span>
                    ) : (
                      <span className="badge failed">الفرق {totals.difference.toFixed(2)}</span>
                    )}
                  </th>
                </tr>
              </tfoot>
            </table>
          </div>
        </section>

        {result && <p className={`alert ${result.kind}`}>{result.text}</p>}

        <div className="toolbar">
          <button className="btn primary" type="submit" disabled={busy || !balanced || !branchId || !periodId}>
            {busy ? 'جارٍ الترحيل…' : 'ترحيل القيد'}
          </button>
          <button
            className="btn"
            type="button"
            onClick={() => {
              setLines([emptyLine(1), emptyLine(2)]);
              setResult(undefined);
            }}
          >
            تفريغ
          </button>
        </div>
      </form>
    </Screen>
  );
}

export default function Page() {
  return (
    <Suspense fallback={<Loading rows={6} />}>
      <JournalEntryForm />
    </Suspense>
  );
}
