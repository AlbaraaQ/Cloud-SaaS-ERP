'use client';

import { useState } from 'react';

import { Empty, ErrorBox, Forbidden, Loading, Screen } from '../../../components/screen';
import { ApiError, apiData, apiPost } from '../../../lib/api';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type FiscalYear = { id: string; name: string; startDate?: string; endDate?: string; start_date?: string; end_date?: string; status: string };
type Period = { id: string; name: string; fiscalYearId?: string; fiscal_year_id?: string; startDate?: string; endDate?: string; start_date?: string; end_date?: string; status: string };

const dateOf = (row: { startDate?: string; start_date?: string }) => row.startDate ?? row.start_date ?? '';
const endOf = (row: { endDate?: string; end_date?: string }) => row.endDate ?? row.end_date ?? '';

export default function FiscalPeriodsPage() {
  const { can } = useSession();
  const years = useQuery<FiscalYear[]>(() => apiData<FiscalYear[]>('/fiscal-years'), []);
  const periods = useQuery<Period[]>(() => apiData<Period[]>('/fiscal-periods'), []);
  const [busy, setBusy] = useState<string | undefined>();
  const [message, setMessage] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();
  const [creating, setCreating] = useState(false);

  async function act(period: Period, action: 'close' | 'reopen') {
    setBusy(period.id);
    setMessage(undefined);
    try {
      if (action === 'close') await apiPost(`/fiscal-periods/${period.id}/close`, {});
      else {
        const reason = window.prompt('سبب إعادة الفتح (إلزامي):');
        if (!reason) {
          setBusy(undefined);
          return;
        }
        await apiPost(`/fiscal-periods/${period.id}/reopen`, { reason });
      }
      setMessage({ kind: 'ok', text: action === 'close' ? 'تم إقفال الفترة.' : 'تم فتح الفترة.' });
      periods.reload();
    } catch (error) {
      setMessage({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(undefined);
    }
  }

  const rows = (periods.data ?? []).slice().sort((left, right) => dateOf(left).localeCompare(dateOf(right)));

  return (
    <Screen
      title="الفترات المحاسبية"
      subtitle="لا يمكن ترحيل أي مستند خارج فترة مفتوحة. ابدأ بفتح سنة مالية."
      crumbs={['المحاسبة', 'العمليات']}
      actions={
        can('accounting.period.close') ? (
          <button className="btn primary" type="button" onClick={() => setCreating(!creating)}>
            {creating ? 'إغلاق النموذج' : 'سنة مالية جديدة'}
          </button>
        ) : null
      }
    >
      {creating && (
        <NewFiscalYearForm
          onDone={() => {
            setCreating(false);
            years.reload();
            periods.reload();
          }}
        />
      )}

      {message && <p className={`alert ${message.kind}`}>{message.text}</p>}

      <section className="card">
        <h2>السنوات المالية</h2>
        {years.status === 'loading' && <Loading rows={2} />}
        {years.status === 'error' && <ErrorBox message={years.error} onRetry={years.reload} />}
        {years.status === 'success' &&
          ((years.data ?? []).length === 0 ? (
            <p className="muted">لا توجد سنة مالية بعد — أنشئ واحدة لبدء العمل.</p>
          ) : (
            <div className="table-wrap" style={{ maxHeight: 240 }}>
              <table>
                <thead>
                  <tr>
                    <th>الاسم</th>
                    <th>من</th>
                    <th>إلى</th>
                    <th>الحالة</th>
                  </tr>
                </thead>
                <tbody>
                  {(years.data ?? []).map((year) => (
                    <tr key={year.id}>
                      <td>{year.name}</td>
                      <td dir="ltr">{dateOf(year)}</td>
                      <td dir="ltr">{endOf(year)}</td>
                      <td>
                        <span className={`badge ${year.status === 'open' ? 'active' : ''}`}>{year.status}</span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ))}
      </section>

      <section className="card">
        <h2>الفترات</h2>
        {periods.status === 'loading' && <Loading rows={3} />}
        {periods.status === 'forbidden' && <Forbidden />}
        {periods.status === 'error' && <ErrorBox message={periods.error} onRetry={periods.reload} />}
        {periods.status === 'success' &&
          (rows.length === 0 ? (
            <Empty title="لا توجد فترات" detail="تُنشأ الفترات تلقائياً عند إنشاء سنة مالية." />
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>الفترة</th>
                    <th>من</th>
                    <th>إلى</th>
                    <th>الحالة</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {rows.map((period) => (
                    <tr key={period.id}>
                      <td>{period.name}</td>
                      <td dir="ltr">{dateOf(period)}</td>
                      <td dir="ltr">{endOf(period)}</td>
                      <td>
                        <span className={`badge ${period.status === 'open' ? 'active' : 'planned'}`}>
                          {period.status === 'open' ? 'مفتوحة' : 'مقفلة'}
                        </span>
                      </td>
                      <td>
                        {period.status === 'open'
                          ? can('accounting.period.close') && (
                              <button className="btn sm" type="button" disabled={busy === period.id} onClick={() => void act(period, 'close')}>
                                إقفال
                              </button>
                            )
                          : can('accounting.period.reopen') && (
                              <button className="btn sm" type="button" disabled={busy === period.id} onClick={() => void act(period, 'reopen')}>
                                إعادة فتح
                              </button>
                            )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ))}
      </section>
    </Screen>
  );
}

function NewFiscalYearForm({ onDone }: { onDone: () => void }) {
  const year = new Date().getFullYear();
  const [name, setName] = useState(String(year));
  const [startDate, setStartDate] = useState(`${year}-01-01`);
  const [endDate, setEndDate] = useState(`${year}-12-31`);
  const [monthly, setMonthly] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | undefined>();

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(undefined);
    try {
      await apiPost('/fiscal-years', { name, startDate, endDate, generateMonthlyPeriods: monthly });
      onDone();
    } catch (caught) {
      setError(caught instanceof ApiError ? `${caught.message}${caught.detail ? ` — ${caught.detail}` : ''}` : String(caught));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="card" onSubmit={submit}>
      <h2>سنة مالية جديدة</h2>
      <div className="form-grid">
        <label className="field">
          <span>الاسم *</span>
          <input className="input" value={name} onChange={(event) => setName(event.target.value)} required />
        </label>
        <label className="field">
          <span>تاريخ البداية *</span>
          <input className="input" type="date" value={startDate} onChange={(event) => setStartDate(event.target.value)} required />
        </label>
        <label className="field">
          <span>تاريخ النهاية *</span>
          <input className="input" type="date" value={endDate} onChange={(event) => setEndDate(event.target.value)} required />
        </label>
        <label className="field">
          <span>توليد الفترات</span>
          <select className="input" value={monthly ? '1' : '0'} onChange={(event) => setMonthly(event.target.value === '1')}>
            <option value="1">فترات شهرية تلقائية</option>
            <option value="0">بدون فترات (يدوي)</option>
          </select>
        </label>
      </div>
      {error && <p className="alert danger">{error}</p>}
      <div className="toolbar">
        <button className="btn primary" type="submit" disabled={busy}>
          {busy ? 'جارٍ الإنشاء…' : 'إنشاء'}
        </button>
      </div>
    </form>
  );
}
