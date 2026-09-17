'use client';

import Link from 'next/link';
import { useState } from 'react';

import { Empty, ErrorBox, Loading, Screen } from '../../components/screen';
import { ApiError, apiData, apiPost } from '../../lib/api';
import { useQuery } from '../../lib/use-query';

type Tenant = {
  id: string;
  code: string;
  name: string;
  status: string;
  baseCurrency: string;
  timezone: string;
  createdAt: string;
  userCount: number;
  branchCount: number;
  subscriptionStatus: string | null;
  planName: string | null;
  planAmount: string | null;
  currentPeriodEnd: string | null;
};

type Plan = { id: string; code: string; name: string; amount: string; currency: string; interval: string; active: boolean };

const STATUS_LABEL: Record<string, string> = { active: 'نشط', suspended: 'موقوف', archived: 'مؤرشف' };

export default function TenantsPage() {
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [applied, setApplied] = useState({ search: '', status: '' });
  const [message, setMessage] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();
  const [granting, setGranting] = useState<Tenant | undefined>();
  // P-C2 made «السبب» mandatory on `POST /platform/tenants/:id/status`: suspending a
  // customer is a decision somebody must be able to explain a month later. The screen asks
  // for it before the click instead of letting the API answer 400.
  const [statusTarget, setStatusTarget] = useState<{ tenant: Tenant; next: 'active' | 'suspended' | 'archived' } | undefined>();

  const tenants = useQuery<Tenant[]>(() => {
    const params = new URLSearchParams();
    if (applied.search) params.set('search', applied.search);
    if (applied.status) params.set('status', applied.status);
    return apiData<Tenant[]>(`/platform/tenants?${params.toString()}`);
  }, [applied]);

  const plans = useQuery<Plan[]>(() => apiData<Plan[]>('/platform/plans'), []);

  async function changeStatus(tenant: Tenant, next: 'active' | 'suspended' | 'archived', reason: string) {
    setMessage(undefined);
    try {
      await apiPost(`/platform/tenants/${tenant.id}/status`, { status: next, reason });
      setMessage({ kind: 'ok', text: 'تم تحديث حالة العميل، والسبب محفوظ في تدقيقه.' });
      setStatusTarget(undefined);
      tenants.reload();
    } catch (error) {
      setMessage({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    }
  }

  return (
    <Screen
      title="العملاء (المستأجرون)"
      subtitle="كل منشأة مشتركة في الخدمة، حالتها، ترخيصها وعدد مستخدميها."
      crumbs={['المنصة', 'العملاء والتراخيص']}
      actions={
        <Link className="btn primary" href="/tenants/new">
          عميل جديد
        </Link>
      }
    >
      <div className="card tight no-print">
        <div className="row">
          <input
            className="input"
            style={{ maxWidth: 260 }}
            placeholder="بحث بالاسم أو الرمز"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
          <select className="input" style={{ maxWidth: 160 }} value={status} onChange={(event) => setStatus(event.target.value)}>
            <option value="">كل الحالات</option>
            <option value="active">نشط</option>
            <option value="suspended">موقوف</option>
            <option value="archived">مؤرشف</option>
          </select>
          <button className="btn primary" type="button" onClick={() => setApplied({ search, status })}>
            بحث
          </button>
        </div>
      </div>

      {message && <p className={`alert ${message.kind}`}>{message.text}</p>}

      {statusTarget && (
        <StatusReasonForm
          tenant={statusTarget.tenant}
          next={statusTarget.next}
          onClose={() => setStatusTarget(undefined)}
          onSubmit={(reason) => void changeStatus(statusTarget.tenant, statusTarget.next, reason)}
        />
      )}

      {granting && (
        <GrantLicenceForm
          tenant={granting}
          plans={(plans.data ?? []).filter((plan) => plan.active)}
          onClose={() => setGranting(undefined)}
          onDone={() => {
            setGranting(undefined);
            setMessage({ kind: 'ok', text: 'تم إصدار الترخيص.' });
            tenants.reload();
          }}
        />
      )}

      {tenants.status === 'loading' && <Loading />}
      {tenants.status === 'error' && <ErrorBox message={tenants.error} onRetry={tenants.reload} />}
      {tenants.status === 'success' &&
        ((tenants.data ?? []).length === 0 ? (
          <Empty title="لا يوجد عملاء" detail="أنشئ أول عميل من زر «عميل جديد»." />
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>المنشأة</th>
                  <th>الرمز</th>
                  <th>الحالة</th>
                  <th>الترخيص</th>
                  <th>ينتهي في</th>
                  <th className="num">مستخدمون</th>
                  <th className="num">فروع</th>
                  <th>الإجراءات</th>
                </tr>
              </thead>
              <tbody>
                {(tenants.data ?? []).map((tenant) => (
                  <tr key={tenant.id}>
                    <td>
                      {/* P-C2 — the card: every question about one customer, on one page. */}
                      <Link href={`/tenants/${tenant.id}`}>
                        <strong>{tenant.name}</strong>
                      </Link>
                      <div className="muted small">{new Date(tenant.createdAt).toLocaleDateString('ar-SA')}</div>
                    </td>
                    <td dir="ltr">{tenant.code}</td>
                    <td>
                      <span className={`badge ${tenant.status}`}>{STATUS_LABEL[tenant.status] ?? tenant.status}</span>
                    </td>
                    <td>
                      {tenant.subscriptionStatus ? (
                        <>
                          <span className={`badge ${tenant.subscriptionStatus}`}>{tenant.subscriptionStatus}</span>
                          <div className="muted small">{tenant.planName}</div>
                        </>
                      ) : (
                        <span className="badge planned">بدون ترخيص</span>
                      )}
                    </td>
                    <td dir="ltr">
                      {tenant.currentPeriodEnd ? new Date(tenant.currentPeriodEnd).toLocaleDateString('ar-SA') : '—'}
                    </td>
                    <td className="num">{tenant.userCount}</td>
                    <td className="num">{tenant.branchCount}</td>
                    <td>
                      <div className="row">
                        <Link className="btn sm" href={`/tenants/${tenant.id}`}>
                          البطاقة
                        </Link>
                        <button className="btn sm primary" type="button" onClick={() => setGranting(tenant)}>
                          ترخيص
                        </button>
                        {tenant.status === 'active' ? (
                          <button
                            className="btn sm danger"
                            type="button"
                            onClick={() => setStatusTarget({ tenant, next: 'suspended' })}
                          >
                            إيقاف
                          </button>
                        ) : (
                          <button
                            className="btn sm"
                            type="button"
                            onClick={() => setStatusTarget({ tenant, next: 'active' })}
                          >
                            تفعيل
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}
    </Screen>
  );
}

/** «السبب» before the click — the same requirement the API enforces. */
function StatusReasonForm({
  tenant,
  next,
  onClose,
  onSubmit,
}: {
  tenant: Tenant;
  next: 'active' | 'suspended' | 'archived';
  onClose: () => void;
  onSubmit: (reason: string) => void;
}) {
  const [reason, setReason] = useState('');
  const action = next === 'suspended' ? 'إيقاف' : next === 'archived' ? 'أرشفة' : 'إعادة تنشيط';

  return (
    <form
      className="card"
      onSubmit={(event) => {
        event.preventDefault();
        if (reason.trim().length >= 3) onSubmit(reason.trim());
      }}
    >
      <h2>
        {action} — {tenant.name}
      </h2>
      <p className="muted small">يُحفظ السبب في تدقيق العميل نفسه، ويظهر في تبويب «التدقيق» من بطاقته.</p>
      <label className="field">
        <span>السبب (٣ أحرف على الأقل)</span>
        <input className="input" value={reason} onChange={(event) => setReason(event.target.value)} autoFocus />
      </label>
      <div className="row" style={{ marginTop: 8 }}>
        <button className="btn primary" type="submit" disabled={reason.trim().length < 3}>
          تأكيد
        </button>
        <button className="btn" type="button" onClick={onClose}>
          إلغاء
        </button>
      </div>
    </form>
  );
}

function GrantLicenceForm({
  tenant,
  plans,
  onClose,
  onDone,
}: {
  tenant: Tenant;
  plans: Plan[];
  onClose: () => void;
  onDone: () => void;
}) {
  const [planId, setPlanId] = useState(plans[0]?.id ?? '');
  const [months, setMonths] = useState(12);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | undefined>();

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(undefined);
    try {
      await apiPost('/platform/subscriptions', { tenantId: tenant.id, planId, months });
      onDone();
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : String(caught));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="card" onSubmit={submit}>
      <h2>إصدار ترخيص — {tenant.name}</h2>
      <p className="muted small">يلغي أي ترخيص فعّال حالياً ويصدر ترخيصاً جديداً، ويعيد تفعيل المنشأة إن كانت موقوفة.</p>
      {plans.length === 0 ? (
        <p className="alert warn">
          لا توجد باقات نشطة. أنشئ باقة أولاً من صفحة <Link href="/plans">الباقات</Link>.
        </p>
      ) : (
        <div className="form-grid">
          <label className="field">
            <span>الباقة</span>
            <select className="input" value={planId} onChange={(event) => setPlanId(event.target.value)} required>
              {plans.map((plan) => (
                <option key={plan.id} value={plan.id}>
                  {plan.name} — {plan.amount} {plan.currency} / {plan.interval === 'year' ? 'سنة' : 'شهر'}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>المدة (بالأشهر)</span>
            <input
              className="input"
              type="number"
              min={1}
              max={120}
              value={months}
              onChange={(event) => setMonths(Number(event.target.value))}
            />
          </label>
        </div>
      )}
      {error && <p className="alert danger">{error}</p>}
      <div className="toolbar">
        <button className="btn primary" type="submit" disabled={busy || plans.length === 0}>
          {busy ? 'جارٍ الإصدار…' : 'إصدار الترخيص'}
        </button>
        <button className="btn" type="button" onClick={onClose}>
          إلغاء
        </button>
      </div>
    </form>
  );
}
