'use client';

import { useState } from 'react';

import { DataTable, Notice, QueryView } from '../../../components/data-view';
import { Screen } from '../../../components/screen';
import { ApiError, apiData, apiList, apiPost } from '../../../lib/api';
import {
  branchOptions,
  dateTime,
  defaultOf,
  listBranches,
  listParties,
  money,
  partyLabel,
  statusLabel,
  type Branch,
  type Party,
} from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type Vessel = { id: string; code: string; name: string };
type Marina = { groups: Array<{ id: string; name: string }>; vessels: Vessel[] };
type Booking = {
  id: string;
  branchId: string;
  partyId: string;
  vesselId: string;
  startsAt: string;
  endsAt: string;
  companions: number | null;
  insuranceAmount: string;
  status: string;
  invoiceId: string | null;
};

const localNow = () => new Date(Date.now() - new Date().getTimezoneOffset() * 60000).toISOString().slice(0, 16);

export default function MarinaBookingsPage() {
  const { can } = useSession();
  const bookings = useQuery<Booking[]>(() => apiList<Booking>('/marina/bookings'), []);
  const marina = useQuery<Marina>(() => apiData<Marina>('/marina'), []);
  const parties = useQuery<Party[]>(() => listParties(), []);
  const branches = useQuery<Branch[]>(() => listBranches(), []);

  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({ branchId: '', partyId: '', vesselId: '', startsAt: localNow(), endsAt: localNow(), companions: '', insuranceText: '' });
  const [addition, setAddition] = useState({ bookingId: '', description: '', amountText: '' });
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger' | 'info'; text: string } | undefined>();

  const branchRows = branches.data ?? [];
  const effectiveBranch = form.branchId || defaultOf(branchRows)?.id || '';
  const vessels = marina.data?.vessels ?? [];

  async function run(action: () => Promise<unknown>, okText: string) {
    setBusy(true);
    setNotice(undefined);
    try {
      await action();
      setNotice({ kind: 'ok', text: okText });
      bookings.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  return (
    <Screen
      title="حجوزات المراكب"
      subtitle="حجز مركب لفترة محددة، إضافة خدمات عليه، ثم إصدار فاتورة التأجير المحسوبة من تسعير المجموعة."
      crumbs={['إدارة المراسي', 'العمليات']}
      actions={
        can('marina.manage') ? (
          <button className="btn primary" type="button" onClick={() => setOpen(!open)}>
            {open ? 'إغلاق' : 'حجز جديد'}
          </button>
        ) : null
      }
    >
      {open && (
        <form
          className="card"
          onSubmit={(event) => {
            event.preventDefault();
            run(
              () =>
                apiPost('/marina/bookings', {
                  branchId: effectiveBranch,
                  partyId: form.partyId,
                  vesselId: form.vesselId,
                  startsAt: new Date(form.startsAt).toISOString(),
                  endsAt: new Date(form.endsAt).toISOString(),
                  companions: form.companions ? Number(form.companions) : undefined,
                  insuranceAmount: form.insuranceText.trim() || undefined,
                }),
              'تم إنشاء الحجز.',
            );
          }}
        >
          <h2>حجز جديد</h2>
          <div className="form-grid">
            <label className="field">
              <span>الفرع *</span>
              <select className="input" value={effectiveBranch} onChange={(event) => setForm({ ...form, branchId: event.target.value })} required>
                {branchOptions(branchRows).map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>العميل *</span>
              <select className="input" value={form.partyId} onChange={(event) => setForm({ ...form, partyId: event.target.value })} required>
                <option value="">— اختر —</option>
                {(parties.data ?? []).map((row) => (
                  <option key={row.id} value={row.id}>
                    {partyLabel(row)}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>المركب *</span>
              <select className="input" value={form.vesselId} onChange={(event) => setForm({ ...form, vesselId: event.target.value })} required>
                <option value="">— اختر —</option>
                {vessels.map((row) => (
                  <option key={row.id} value={row.id}>
                    {`${row.code} — ${row.name}`}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>من *</span>
              <input className="input" type="datetime-local" dir="ltr" value={form.startsAt} onChange={(event) => setForm({ ...form, startsAt: event.target.value })} required />
            </label>
            <label className="field">
              <span>إلى *</span>
              <input className="input" type="datetime-local" dir="ltr" value={form.endsAt} onChange={(event) => setForm({ ...form, endsAt: event.target.value })} required />
            </label>
            <label className="field">
              <span>عدد المرافقين</span>
              <input className="input" dir="ltr" inputMode="numeric" value={form.companions} onChange={(event) => setForm({ ...form, companions: event.target.value })} />
            </label>
            <label className="field">
              <span>مبلغ التأمين</span>
              <input className="input" dir="ltr" inputMode="decimal" value={form.insuranceText} onChange={(event) => setForm({ ...form, insuranceText: event.target.value })} />
            </label>
          </div>
          <Notice notice={notice} />
          <button className="btn primary" type="submit" disabled={busy}>
            {busy ? 'جارٍ الحفظ…' : 'حفظ الحجز'}
          </button>
        </form>
      )}

      {!open && <Notice notice={notice} />}

      <div className="card">
        <h2>إضافة خدمة على حجز</h2>
        <div className="form-grid">
          <label className="field">
            <span>الحجز</span>
            <select className="input" value={addition.bookingId} onChange={(event) => setAddition({ ...addition, bookingId: event.target.value })}>
              <option value="">— اختر —</option>
              {(bookings.data ?? []).map((row) => {
                const vessel = vessels.find((entry) => entry.id === row.vesselId);
                return (
                  <option key={row.id} value={row.id}>
                    {`${vessel?.name ?? row.vesselId} — ${dateTime(row.startsAt)}`}
                  </option>
                );
              })}
            </select>
          </label>
          <label className="field">
            <span>الوصف</span>
            <input className="input" value={addition.description} onChange={(event) => setAddition({ ...addition, description: event.target.value })} />
          </label>
          <label className="field">
            <span>المبلغ</span>
            <input className="input" dir="ltr" inputMode="decimal" value={addition.amountText} onChange={(event) => setAddition({ ...addition, amountText: event.target.value })} />
          </label>
        </div>
        <button
          className="btn"
          type="button"
          disabled={busy || !can('marina.manage') || !addition.bookingId || !addition.description.trim()}
          onClick={() =>
            run(async () => {
              await apiPost(`/marina/bookings/${addition.bookingId}/additions`, { description: addition.description.trim(), amount: addition.amountText.trim() || '0' });
              setAddition({ bookingId: '', description: '', amountText: '' });
            }, 'تمت إضافة الخدمة على الحجز.')
          }
        >
          إضافة الخدمة
        </button>
      </div>

      <QueryView query={bookings} empty="لا توجد حجوزات" emptyDetail="أنشئ حجزاً جديداً لمركب.">
        {(rows) => (
          <DataTable
            rows={rows}
            rowKey={(row) => row.id}
            columns={[
              { key: 'vessel', header: 'المركب', cell: (row) => vessels.find((entry) => entry.id === row.vesselId)?.name ?? '—' },
              {
                key: 'party',
                header: 'العميل',
                cell: (row) => {
                  const party = (parties.data ?? []).find((entry) => entry.id === row.partyId);
                  return party ? partyLabel(party) : '—';
                },
              },
              { key: 'from', header: 'من', align: 'ltr', cell: (row) => dateTime(row.startsAt) },
              { key: 'to', header: 'إلى', align: 'ltr', cell: (row) => dateTime(row.endsAt) },
              { key: 'companions', header: 'المرافقون', align: 'num', cell: (row) => row.companions ?? '—' },
              { key: 'insurance', header: 'التأمين', align: 'num', cell: (row) => money(row.insuranceAmount) },
              { key: 'status', header: 'الحالة', cell: (row) => <span className="badge">{statusLabel(row.status)}</span> },
              {
                key: 'actions',
                header: '',
                cell: (row) =>
                  !row.invoiceId && can('marina.invoice') ? (
                    <button className="btn sm primary" type="button" disabled={busy} onClick={() => run(() => apiPost(`/marina/bookings/${row.id}/rental-invoice`, {}), 'تم إصدار فاتورة التأجير.')}>
                      إصدار فاتورة
                    </button>
                  ) : (
                    <span className="muted small">{row.invoiceId ? 'مفوترة' : '—'}</span>
                  ),
              },
            ]}
          />
        )}
      </QueryView>
    </Screen>
  );
}
