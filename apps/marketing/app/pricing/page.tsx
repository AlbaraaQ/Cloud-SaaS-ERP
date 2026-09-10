'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';

import { Kpi } from '../../components/card';
import { portalFetch } from '../../lib/api';
import { surfaceHref } from '../../lib/surfaces';

type Plan = { id: string; code: string; name: string; interval: 'month' | 'year'; amount: string; currency: string };

export default function PricingPage() {
  const [plans, setPlans] = useState<Plan[]>([]);
  const [message, setMessage] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    portalFetch<Plan[]>('/billing/plans')
      .then((result) => setPlans(result))
      .catch(() => setMessage('تعذر تحميل الباقات المتاحة'))
      .finally(() => setLoading(false));
  }, []);

  return <div className="grid">
    <section className="hero"><h1>الاشتراك في النظام المحاسبي</h1><p>اختر الباقة المناسبة لشركتك، وأنشئ منشأتك في خطوة واحدة.</p></section>
    {message && <p className="muted" role="status">{message}</p>}
    {loading ? <p className="muted">جاري تحميل الباقات...</p> : plans.length === 0 ? <p className="muted">لا توجد باقات مفعلة حالياً.</p> : <div className="grid cols">
      {plans.map((plan) => <article className="card" key={plan.id}>
        <Kpi label={plan.name} value={`${plan.amount} ${plan.currency}`} /><p className="muted">{plan.interval === 'month' ? 'اشتراك شهري' : 'اشتراك سنوي'}</p>
        <Link className="btn primary" href="/onboarding">اشترك بهذه الباقة</Link>
      </article>)}
    </div>}
    <p className="muted">لديك حساب؟ اطلب التفعيل من شاشة الترخيص في <a href={surfaceHref('staff', '/support/license')}>لوحة الإدارة</a>.</p>
  </div>;
}
