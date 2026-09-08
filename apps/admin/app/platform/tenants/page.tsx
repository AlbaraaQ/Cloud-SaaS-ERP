'use client';

import Link from 'next/link';
import { useState } from 'react';

import { Empty, ErrorBox, Loading, Screen } from '../../../components/screen';
import { ApiError, apiData, apiPatch, apiPost } from '../../../lib/api';
import { useQuery } from '../../../lib/use-query';

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

  const tenants = useQuery<Tenant[]>(() => {
    const params = new URLSearchParams();
    if (applied.search) params.set('search', applied.search);
    if (applied.status) params.set('status', applied.status);
    return apiData<Tenant[]>(`/platform/tenants?${params.toString()}`);
  }, [applied]);

  const plans = useQuery<Plan[]>(() => apiData<Plan[]>('/platform/plans'), []);

  async function changeStatus(tenant: Tenant, next: 'active' | 'suspended' | 'archived') {
    if (next !== 'active' && !window.confirm(`تأكيد ${next === 'suspended' ? 'إيقاف' : 'أرشفة'} «${tenant.name}»؟`)) return;
    setMessage(undefined);
    try {
      await apiPatch(`/platform/tenants/${tenant.id}/status`, { status: next });
      setMessage({ kind: 'ok', text: 'تم تحديث حالة العميل.' });
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
        <Link className="btn primary" href="/platform/tenants/new">
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
                      <strong>{tenant.name}</strong>
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
                        <button className="btn sm primary" type="button" onClick={() => setGranting(tenant)}>
                          ترخيص
                        </button>
                        {tenant.status === 'active' ? (
                          <button className="btn sm danger" type="button" onClick={() => void changeStatus(tenant, 'suspended')}>
                            إيقاف
                          </button>
                        ) : (
                          <button className="btn sm" type="button" onClick={() => void changeStatus(tenant, 'active')}>
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
          لا توجد باقات نشطة. أنشئ باقة أولاً من صفحة <Link href="/platform/plans">الباقات</Link>.
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
