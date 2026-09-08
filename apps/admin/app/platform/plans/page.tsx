'use client';

import { useState } from 'react';

import { Empty, ErrorBox, Loading, Screen } from '../../../components/screen';
import { ApiError, apiData, apiPatch, apiPost } from '../../../lib/api';
import { useQuery } from '../../../lib/use-query';

type Plan = {
  id: string;
  code: string;
  name: string;
  interval: string;
  amount: string;
  currency: string;
  stripe_price_id: string | null;
  active: boolean;
  active_subscriptions: number;
};

export default function PlansPage() {
  const plans = useQuery<Plan[]>(() => apiData<Plan[]>('/platform/plans'), []);
  const [creating, setCreating] = useState(false);
  const [message, setMessage] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();

  async function toggle(plan: Plan) {
    try {
      await apiPatch(`/platform/plans/${plan.id}/active`, { active: !plan.active });
      plans.reload();
    } catch (error) {
      setMessage({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    }
  }

  return (
    <Screen
      title="الباقات والأسعار"
      subtitle="كتالوج الاشتراكات الذي يُصدر منه الترخيص. يمكن ربط كل باقة بسعر Stripe."
      crumbs={['المنصة', 'العملاء والتراخيص']}
      actions={
        <button className="btn primary" type="button" onClick={() => setCreating(!creating)}>
          {creating ? 'إغلاق' : 'باقة جديدة'}
        </button>
      }
    >
      {creating && (
        <PlanForm
          onDone={() => {
            setCreating(false);
            setMessage({ kind: 'ok', text: 'تم حفظ الباقة.' });
            plans.reload();
          }}
        />
      )}
      {message && <p className={`alert ${message.kind}`}>{message.text}</p>}

      {plans.status === 'loading' && <Loading />}
      {plans.status === 'error' && <ErrorBox message={plans.error} onRetry={plans.reload} />}
      {plans.status === 'success' &&
        ((plans.data ?? []).length === 0 ? (
          <Empty title="لا توجد باقات" detail="أنشئ الباقة الأولى ليتمكن العملاء من الاشتراك." />
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>الرمز</th>
                  <th>الاسم</th>
                  <th className="num">السعر</th>
                  <th>الدورة</th>
                  <th>Stripe</th>
                  <th className="num">اشتراكات فعّالة</th>
                  <th>الحالة</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {(plans.data ?? []).map((plan) => (
                  <tr key={plan.id}>
                    <td dir="ltr">{plan.code}</td>
                    <td>{plan.name}</td>
                    <td className="num">
                      {Number(plan.amount).toLocaleString('ar-SA', { minimumFractionDigits: 2 })} {plan.currency}
                    </td>
                    <td>{plan.interval === 'year' ? 'سنوي' : 'شهري'}</td>
                    <td dir="ltr" className="small">{plan.stripe_price_id ?? '—'}</td>
                    <td className="num">{plan.active_subscriptions}</td>
                    <td>
                      <span className={`badge ${plan.active ? 'active' : 'planned'}`}>{plan.active ? 'نشطة' : 'متوقفة'}</span>
                    </td>
                    <td>
                      <button className="btn sm" type="button" onClick={() => void toggle(plan)}>
                        {plan.active ? 'إيقاف' : 'تفعيل'}
                      </button>
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

function PlanForm({ onDone }: { onDone: () => void }) {
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [priceText, setPriceText] = useState('0.00');
  const [interval, setInterval] = useState<'month' | 'year'>('month');
  const [currency, setCurrency] = useState('SAR');
  const [stripePriceId, setStripePriceId] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | undefined>();

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(undefined);
    try {
      await apiPost('/platform/plans', {
        code: code.trim(),
        name: name.trim(),
        interval,
        amount: priceText,
        currency,
        stripePriceId: stripePriceId.trim() || null,
        active: true,
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
      <h2>باقة جديدة</h2>
      <p className="muted small">الحفظ بنفس الرمز يحدّث الباقة القائمة بدل إنشاء نسخة ثانية.</p>
      <div className="form-grid">
        <label className="field">
          <span>الرمز *</span>
          <input className="input" dir="ltr" value={code} onChange={(event) => setCode(event.target.value)} placeholder="pro-monthly" required />
        </label>
        <label className="field">
          <span>الاسم *</span>
          <input className="input" value={name} onChange={(event) => setName(event.target.value)} placeholder="الباقة الاحترافية" required />
        </label>
        <label className="field">
          <span>السعر *</span>
          <input className="input" dir="ltr" inputMode="decimal" value={priceText} onChange={(event) => setPriceText(event.target.value)} required />
        </label>
        <label className="field">
          <span>العملة</span>
          <select className="input" value={currency} onChange={(event) => setCurrency(event.target.value)}>
            <option value="SAR">SAR</option>
            <option value="YER">YER</option>
            <option value="AED">AED</option>
            <option value="USD">USD</option>
          </select>
        </label>
        <label className="field">
          <span>الدورة</span>
          <select className="input" value={interval} onChange={(event) => setInterval(event.target.value as 'month' | 'year')}>
            <option value="month">شهرية</option>
            <option value="year">سنوية</option>
          </select>
        </label>
        <label className="field">
          <span>معرّف سعر Stripe (اختياري)</span>
          <input className="input" dir="ltr" value={stripePriceId} onChange={(event) => setStripePriceId(event.target.value)} placeholder="price_..." />
        </label>
      </div>
      {error && <p className="alert danger">{error}</p>}
      <button className="btn primary" type="submit" disabled={busy}>
        {busy ? 'جارٍ الحفظ…' : 'حفظ'}
      </button>
    </form>
  );
}
