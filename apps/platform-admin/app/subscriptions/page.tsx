'use client';

import { useState } from 'react';

import { Empty, ErrorBox, Loading, Screen } from '../../components/screen';
import { ApiError, apiData, apiPost } from '../../lib/api';
import { useQuery } from '../../lib/use-query';

/**
 * الاشتراكات والتراخيص — P-C4: **دورة حياة** الترخيص لا صفًّا يُقرأ.
 *
 * الخطة تسمّي الحالات التي تُدار من هنا: «تجربة · تفعيل · ترقية/تخفيض · إيقاف مؤقّت · إلغاء».
 * وكل إجراء يقابل نقطة نهاية حقيقية:
 *
 * | الإجراء | النقطة | ما تعيده |
 * |---|---|---|
 * | إصدار ترخيص (وإصداره بتجربة) | `POST /platform/subscriptions` | الترخيص بحالته `trialing` أو `active` |
 * | ترقية / تخفيض | `POST /platform/subscriptions/:id/change-plan` | التقويم ومستند الفرق |
 * | إيقاف مؤقّت | `POST /platform/subscriptions/:id/pause` | `paused` بتاريخ الإيقاف |
 * | استئناف | `POST /platform/subscriptions/:id/resume` | إرجاع أيام الإيقاف إلى نهاية المدة |
 * | إلغاء (فوري أو بانتهاء المدة) | `POST /platform/subscriptions/:id/cancel` | `canceled` أو علامة نهاية المدة |
 *
 * وكل إجراء يمرّ بسببٍ مكتوب: تغيير باقة عميلٍ يمسّ فاتورته القادمة، وسؤال «لماذا أُلغي هذا
 * العميل؟» يُسأل بعد شهور.
 */

type Status = 'pending' | 'trialing' | 'active' | 'past_due' | 'paused' | 'canceled' | 'expired' | 'incomplete';

type Subscription = {
  id: string;
  status: Status;
  provider: string;
  tenantId: string;
  tenantCode: string;
  tenantName: string;
  tenantStatus: string;
  planId: string;
  planCode: string;
  planName: string;
  amount: string;
  currency: string;
  interval: 'month' | 'year';
  monthlyAmount: string;
  currentPeriodStart: string | null;
  currentPeriodEnd: string | null;
  trialEndsAt: string | null;
  pausedAt: string | null;
  resumedAt: string | null;
  cancelAtPeriodEnd: boolean;
  canceledReason: string | null;
  billingEmail: string | null;
  activatedAt: string | null;
  canceledAt: string | null;
  createdAt: string;
  dueInvoiceCount: number;
};

type Plan = { id: string; code: string; name: string; amount: string; currency: string; interval: 'month' | 'year'; active: boolean };
type Tenant = { id: string; code: string; name: string };
type ChangeResult = {
  proration: {
    periodDays: number;
    remainingDays: number;
    fromPlanCode: string;
    toPlanCode: string;
    credit: string;
    charge: string;
    net: string;
    currency: string;
  };
  invoiceId: string | null;
  invoiceNumber: string | null;
  /** المستند كما سمّاه الخادم (`invoice` · `credit_note` · `null` إن كان الفرق صفراً). */
  invoiceKind: 'invoice' | 'credit_note' | null;
};

const STATUS_LABEL: Record<Status, string> = {
  pending: 'بانتظار التفعيل',
  trialing: 'تجربة',
  active: 'فعّال',
  past_due: 'متأخر',
  paused: 'موقوف مؤقتاً',
  canceled: 'ملغى',
  expired: 'منتهٍ',
  incomplete: 'غير مكتمل',
};

/** الحالات التي لا يزال الترخيص فيها حيّاً — عليها تظهر إجراءات الحياة. */
const LIVE: readonly Status[] = ['trialing', 'active', 'past_due', 'paused'];

function dateText(value: string | null): string {
  return value ? new Date(value).toLocaleDateString('ar-SA') : '—';
}

export default function SubscriptionsPage() {
  const [status, setStatus] = useState('');
  const [message, setMessage] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();
  const [granting, setGranting] = useState(false);
  const [changeTarget, setChangeTarget] = useState<Subscription>();
  const [actionTarget, setActionTarget] = useState<{ subscription: Subscription; kind: 'pause' | 'resume' | 'cancel' }>();

  const subscriptions = useQuery<Subscription[]>(
    () => apiData<Subscription[]>(`/platform/subscriptions${status ? `?status=${status}` : ''}`),
    [status],
  );
  const plans = useQuery<Plan[]>(() => apiData<Plan[]>('/platform/plans'), []);

  const rows = subscriptions.data ?? [];

  return (
    <Screen
      title="الاشتراكات والتراخيص"
      subtitle="كل ترخيص صادر عبر المنصة: يدوي أو عبر Stripe — مع دورة حياته كاملة."
      crumbs={['المنصة', 'العملاء والتراخيص']}
      actions={
        <>
          <select className="input" style={{ maxWidth: 180 }} value={status} onChange={(event) => setStatus(event.target.value)}>
            <option value="">كل الحالات</option>
            {(Object.keys(STATUS_LABEL) as Status[]).map((key) => (
              <option key={key} value={key}>
                {STATUS_LABEL[key]}
              </option>
            ))}
          </select>
          <button className="btn primary" type="button" onClick={() => setGranting(!granting)}>
            {granting ? 'إغلاق' : 'ترخيص جديد'}
          </button>
        </>
      }
    >
      {granting && (
        <GrantForm
          plans={plans.data ?? []}
          onDone={() => {
            setGranting(false);
            setMessage({ kind: 'ok', text: 'صدر الترخيص.' });
            subscriptions.reload();
          }}
          onCancel={() => setGranting(false)}
        />
      )}
      {changeTarget && (
        <ChangePlanForm
          subscription={changeTarget}
          plans={plans.data ?? []}
          onDone={(result) => {
            setChangeTarget(undefined);
            setMessage({
              kind: 'ok',
              text:
                result.invoiceKind === null
                  ? 'تم تغيير الباقة بلا مستند (الفرق صفر).'
                  : `تم تغيير الباقة. الفرق ${result.proration.net} ${result.proration.currency} — ${
                      result.invoiceKind === 'credit_note' ? 'إشعار دائن مسودّة' : 'فاتورة مسودّة'
                    } في صفحة الفواتير.`,
            });
            subscriptions.reload();
          }}
          onCancel={() => setChangeTarget(undefined)}
        />
      )}
      {actionTarget && (
        <LifecycleForm
          subscription={actionTarget.subscription}
          kind={actionTarget.kind}
          onDone={(text) => {
            setActionTarget(undefined);
            setMessage({ kind: 'ok', text });
            subscriptions.reload();
          }}
          onCancel={() => setActionTarget(undefined)}
        />
      )}
      {message && <p className={`alert ${message.kind}`}>{message.text}</p>}

      {subscriptions.status === 'loading' && <Loading />}
      {subscriptions.status === 'error' && <ErrorBox message={subscriptions.error} onRetry={subscriptions.reload} />}
      {subscriptions.status === 'success' &&
        (rows.length === 0 ? (
          <Empty title="لا توجد تراخيص" detail="أصدر ترخيصاً من هنا أو من صفحة العملاء." />
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>العميل</th>
                  <th>الباقة</th>
                  <th className="num">القيمة</th>
                  <th>المصدر</th>
                  <th>الحالة</th>
                  <th>يبدأ</th>
                  <th>ينتهي</th>
                  <th className="num">فواتير مستحقّة</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.id}>
                    <td>
                      <strong>{row.tenantName}</strong>
                      <div className="muted small" dir="ltr">
                        {row.tenantCode}
                      </div>
                    </td>
                    <td>
                      {row.planName}
                      <div className="muted small" dir="ltr">
                        {row.planCode}
                      </div>
                    </td>
                    <td className="num">
                      {Number(row.amount).toLocaleString('ar-SA', { minimumFractionDigits: 2 })} {row.currency}
                      <div className="muted small">
                        {row.interval === 'year' ? 'سنوي' : 'شهري'} · {row.monthlyAmount} شهرياً
                      </div>
                    </td>
                    <td>{row.provider === 'stripe' ? 'Stripe' : 'يدوي'}</td>
                    <td>
                      <span className={`badge ${row.status}`}>{STATUS_LABEL[row.status]}</span>
                      {row.cancelAtPeriodEnd && <div className="muted small">يُنتهي بانتهاء المدة</div>}
                      {row.status === 'trialing' && row.trialEndsAt && (
                        <div className="muted small">تنتهي التجربة {dateText(row.trialEndsAt)}</div>
                      )}
                      {row.status === 'paused' && row.pausedAt && (
                        <div className="muted small">موقوف منذ {dateText(row.pausedAt)}</div>
                      )}
                    </td>
                    <td dir="ltr">{dateText(row.currentPeriodStart)}</td>
                    <td dir="ltr">{dateText(row.currentPeriodEnd)}</td>
                    <td className="num">{row.dueInvoiceCount}</td>
                    <td>
                      <div className="row" style={{ gap: 6, flexWrap: 'wrap' }}>
                        {LIVE.includes(row.status) && (
                          <>
                            <button className="btn sm" type="button" onClick={() => setChangeTarget(row)}>
                              ترقية/تخفيض
                            </button>
                            {row.status === 'paused' ? (
                              <button
                                className="btn sm"
                                type="button"
                                onClick={() => setActionTarget({ subscription: row, kind: 'resume' })}
                              >
                                استئناف
                              </button>
                            ) : (
                              <button
                                className="btn sm"
                                type="button"
                                onClick={() => setActionTarget({ subscription: row, kind: 'pause' })}
                              >
                                إيقاف مؤقّت
                              </button>
                            )}
                            <button
                              className="btn sm danger"
                              type="button"
                              onClick={() => setActionTarget({ subscription: row, kind: 'cancel' })}
                            >
                              إلغاء
                            </button>
                          </>
                        )}
                        {row.status === 'canceled' && row.canceledReason && (
                          <span className="muted small">{row.canceledReason}</span>
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

function GrantForm({
  plans,
  onDone,
  onCancel,
}: {
  plans: Plan[];
  onDone: () => void;
  onCancel: () => void;
}) {
  const tenants = useQuery<Tenant[]>(() => apiData<Tenant[]>('/platform/tenants'), []);
  const [tenantId, setTenantId] = useState('');
  const [planId, setPlanId] = useState('');
  const [months, setMonths] = useState(12);
  const [trialDays, setTrialDays] = useState(0);
  const [billingEmail, setBillingEmail] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | undefined>();

  const chosenTenant = tenantId || tenants.data?.[0]?.id || '';
  const chosenPlan = planId || plans[0]?.id || '';

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(undefined);
    try {
      await apiPost('/platform/subscriptions', {
        tenantId: chosenTenant,
        planId: chosenPlan,
        months,
        trialDays,
        ...(billingEmail.trim() ? { billingEmail: billingEmail.trim() } : {}),
      });
      onDone();
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : String(caught));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="card" onSubmit={submit}>
      <h2>ترخيص جديد</h2>
      <p className="muted small">
        «تجربة» = مدة بلا فاتورة تنتهي في تاريخها، و«تفعيل» = ترخيص فعّال يبدأ من اليوم. الإصدار يلغي أي ترخيص
        حيّ سابق للعميل — ترخيص واحد حيّ لكل عميل.
      </p>
      <div className="form-grid">
        <label className="field">
          <span>العميل *</span>
          <select className="input" value={chosenTenant} onChange={(event) => setTenantId(event.target.value)} required>
            <option value="">اختر عميلاً…</option>
            {(tenants.data ?? []).map((tenant) => (
              <option key={tenant.id} value={tenant.id}>
                {tenant.name} ({tenant.code})
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>الباقة *</span>
          <select className="input" value={chosenPlan} onChange={(event) => setPlanId(event.target.value)} required>
            <option value="">اختر باقة…</option>
            {plans.map((plan) => (
              <option key={plan.id} value={plan.id}>
                {plan.name} — {plan.amount} {plan.currency} / {plan.interval === 'year' ? 'سنوي' : 'شهري'}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>المدة (أشهر) *</span>
          <input
            className="input"
            dir="ltr"
            type="number"
            min={1}
            max={60}
            value={months}
            onChange={(event) => setMonths(Number(event.target.value))}
            required
          />
        </label>
        <label className="field">
          <span>أيام التجربة (0 = تفعيل فوري)</span>
          <input
            className="input"
            dir="ltr"
            type="number"
            min={0}
            max={90}
            value={trialDays}
            onChange={(event) => setTrialDays(Number(event.target.value))}
          />
        </label>
        <label className="field">
          <span>بريد الفوترة (اختياري)</span>
          <input className="input" dir="ltr" type="email" value={billingEmail} onChange={(event) => setBillingEmail(event.target.value)} />
        </label>
      </div>
      {error && <p className="alert danger">{error}</p>}
      <div className="row">
        <button className="btn primary" type="submit" disabled={busy || !chosenTenant || !chosenPlan}>
          {busy ? 'جارٍ الإصدار…' : trialDays > 0 ? 'إصدار بتجربة' : 'تفعيل الترخيص'}
        </button>
        <button className="btn" type="button" onClick={onCancel}>
          إلغاء
        </button>
      </div>
    </form>
  );
}

/**
 * ترقية/تخفيض — الشاشة تعرض الحساب **قبل** الحفظ: أيام المدة، والأيام المتبقية، ورصيد الباقة
 * القديمة، ومقابل الجديدة، والفرق. هذه الأرقام تأتي من الخدمة لا من جمعٍ في المتصفّح
 * (`platformProration` في العقود)، فما يقرؤه المشغّل هو ما ستكتبه الفاتورة.
 */
function ChangePlanForm({
  subscription,
  plans,
  onDone,
  onCancel,
}: {
  subscription: Subscription;
  plans: Plan[];
  onDone: (result: ChangeResult) => void;
  onCancel: () => void;
}) {
  const targets = plans.filter((plan) => plan.id !== subscription.planId && plan.active);
  const [planId, setPlanId] = useState(targets[0]?.id ?? '');
  const [reason, setReason] = useState('');
  const [preview, setPreview] = useState<ChangeResult['proration']>();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | undefined>();

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(undefined);
    try {
      const result = await apiPost<ChangeResult>(`/platform/subscriptions/${subscription.id}/change-plan`, {
        planId,
        reason: reason.trim(),
      });
      setPreview(result.proration);
      onDone(result);
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : String(caught));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="card" onSubmit={submit}>
      <h2>تغيير باقة — {subscription.tenantName}</h2>
      <p className="muted small">
        الترقية/التخفيض يبدأ مدة جديدة كاملة من اليوم ({subscription.interval === 'year' ? 'سنة' : 'شهر'})، ويُخصم من
        مقابلها رصيد الباقة الحالية عن الأيام غير المستهلكة. الفرق الموجب فاتورة، والسالب إشعار دائن، والصفر بلا مستند.
      </p>
      <div className="form-grid">
        <label className="field">
          <span>الباقة الحالية</span>
          <input className="input" value={`${subscription.planName} — ${subscription.amount} ${subscription.currency}`} readOnly />
        </label>
        <label className="field">
          <span>الباقة الجديدة *</span>
          <select className="input" value={planId} onChange={(event) => setPlanId(event.target.value)} required>
            <option value="">اختر باقة…</option>
            {targets.map((plan) => (
              <option key={plan.id} value={plan.id}>
                {plan.name} — {plan.amount} {plan.currency} / {plan.interval === 'year' ? 'سنوي' : 'شهري'}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>السبب *</span>
          <input className="input" value={reason} onChange={(event) => setReason(event.target.value)} required minLength={3} />
        </label>
      </div>
      {preview && (
        <dl className="kv">
          <dt>أيام المدة</dt>
          <dd>{preview.periodDays}</dd>
          <dt>الأيام المتبقية</dt>
          <dd>{preview.remainingDays}</dd>
          <dt>رصيد الباقة السابقة</dt>
          <dd>
            {preview.credit} {preview.currency}
          </dd>
          <dt>مقابل الباقة الجديدة</dt>
          <dd>
            {preview.charge} {preview.currency}
          </dd>
          <dt>الفرق</dt>
          <dd>
            {preview.net} {preview.currency}
          </dd>
        </dl>
      )}
      {error && <p className="alert danger">{error}</p>}
      <div className="row">
        <button className="btn primary" type="submit" disabled={busy || !planId}>
          {busy ? 'جارٍ التغيير…' : 'تنفيذ التغيير'}
        </button>
        <button className="btn" type="button" onClick={onCancel}>
          إغلاق
        </button>
      </div>
    </form>
  );
}

function LifecycleForm({
  subscription,
  kind,
  onDone,
  onCancel,
}: {
  subscription: Subscription;
  kind: 'pause' | 'resume' | 'cancel';
  onDone: (text: string) => void;
  onCancel: () => void;
}) {
  const [reason, setReason] = useState('');
  const [atPeriodEnd, setAtPeriodEnd] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | undefined>();

  const title =
    kind === 'pause' ? 'إيقاف مؤقّت' : kind === 'resume' ? 'استئناف الترخيص' : 'إلغاء الترخيص';

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(undefined);
    try {
      if (kind === 'cancel') {
        await apiPost(`/platform/subscriptions/${subscription.id}/cancel`, {
          reason: reason.trim(),
          atPeriodEnd,
        });
        onDone(atPeriodEnd ? 'سيُنتهي الترخيص بانتهاء المدة المدفوعة.' : 'أُلغي الترخيص فوراً، والسبب مسجَّل.');
      } else {
        await apiPost(`/platform/subscriptions/${subscription.id}/${kind}`, { reason: reason.trim() });
        onDone(kind === 'pause' ? 'أُوقف الترخيص مؤقتاً.' : 'استُؤنف الترخيص وأُعيدت أيام الإيقاف إلى نهاية المدة.');
      }
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : String(caught));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="card" onSubmit={submit}>
      <h2>
        {title} — {subscription.tenantName} ({subscription.planName})
      </h2>
      {kind === 'pause' && (
        <p className="muted small">
          الإيقاف يحجز الترخيص بلا إنهائه: الفواتير القائمة تبقى، وأيام الإيقاف تُعاد إلى نهاية المدة عند الاستئناف.
        </p>
      )}
      {kind === 'cancel' && (
        <p className="muted small">
          الإلغاء الفوري يقطع الخدمة اليوم، و«بانتهاء المدة» يترك الترخيص حيّاً حتى آخر يوم دفعه العميل.
        </p>
      )}
      <div className="form-grid">
        <label className="field">
          <span>السبب *</span>
          <input className="input" value={reason} onChange={(event) => setReason(event.target.value)} required minLength={3} />
        </label>
        {kind === 'cancel' && (
          <label className="field">
            <span>التوقيت</span>
            <select className="input" value={atPeriodEnd ? 'period' : 'now'} onChange={(event) => setAtPeriodEnd(event.target.value === 'period')}>
              <option value="now">فوري</option>
              <option value="period">بانتهاء المدة المدفوعة</option>
            </select>
          </label>
        )}
      </div>
      {error && <p className="alert danger">{error}</p>}
      <div className="row">
        <button className={`btn ${kind === 'cancel' ? 'danger' : 'primary'}`} type="submit" disabled={busy}>
          {busy ? 'جارٍ التنفيذ…' : title}
        </button>
        <button className="btn" type="button" onClick={onCancel}>
          إغلاق
        </button>
      </div>
    </form>
  );
}
