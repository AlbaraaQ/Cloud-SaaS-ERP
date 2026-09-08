'use client';

import { useEffect, useState } from 'react';

import { Kpi } from '../../components/card';
import { portalFetch } from '../../lib/api';

type Plan = { id: string; code: string; name: string; interval: 'month' | 'year'; amount: string; currency: string };

export default function PricingPage() {
  const [plans, setPlans] = useState<Plan[]>([]);
  const [selected, setSelected] = useState<string>();
  const [message, setMessage] = useState('');
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    portalFetch<Plan[]>('/billing/plans')
      .then((result) => setPlans(result))
      .catch(() => setMessage('تعذر تحميل الباقات المتاحة'))
      .finally(() => setLoading(false));
  }, []);

  async function requestActivation() {
    if (!selected) return;
    setSubmitting(true);
    setMessage('');
    try {
      await portalFetch('/billing/activation-requests', {
        method: 'POST',
        body: JSON.stringify({ planId: selected }),
      });
      setMessage('تم إرسال طلب الاشتراك. ستتم مراجعته من الإدارة.');
    } catch {
      setMessage('يجب تسجيل الدخول لإرسال طلب الاشتراك أو حدث خطأ في الطلب.');
    } finally {
      setSubmitting(false);
    }
  }

  return <div className="grid">
    <section className="hero"><h1>الاشتراك في النظام المحاسبي</h1><p>اختر الباقة المناسبة لشركتك، ثم أرسل طلب التفعيل اليدوي للإدارة.</p></section>
    {message && <p className="muted" role="status">{message}</p>}
    {loading ? <p className="muted">جاري تحميل الباقات...</p> : plans.length === 0 ? <p className="muted">لا توجد باقات مفعلة حالياً.</p> : <div className="grid cols">
      {plans.map((plan) => <button className={`card ${selected === plan.id ? 'selected' : ''}`} key={plan.id} type="button" onClick={() => setSelected(plan.id)}>
        <Kpi label={plan.name} value={`${plan.amount} ${plan.currency}`} /><p className="muted">شهرياً: {plan.interval === 'month' ? 'نعم' : 'لا'}</p>
      </button>)}
    </div>}
    <button className="btn primary" type="button" disabled={!selected || submitting} onClick={requestActivation}>{submitting ? 'جاري الإرسال...' : 'إرسال طلب التفعيل'}</button>
  </div>;
}
