'use client';

import { ErrorBox, Loading, Screen } from '../../../components/screen';
import { apiData } from '../../../lib/api';
import { useQuery } from '../../../lib/use-query';

type Subscription = {
  status: string;
  provider: string;
  plan_name?: string;
  plan_code?: string;
  amount?: string;
  currency?: string;
  activated_at?: string | null;
  current_period_start?: string | null;
  current_period_end?: string | null;
} | null;

export default function LicensePage() {
  const subscription = useQuery<Subscription>(() => apiData<Subscription>('/billing/subscription'), []);

  return (
    <Screen title="الترخيص" subtitle="حالة اشتراك منشأتك في الخدمة." crumbs={['الدعم الفني']}>
      {subscription.status === 'loading' && <Loading rows={3} />}
      {subscription.status === 'error' && <ErrorBox message={subscription.error} onRetry={subscription.reload} />}
      {subscription.status === 'success' && (
        <section className="card">
          {!subscription.data ? (
            <>
              <p className="alert warn">لا يوجد ترخيص فعّال لهذه المنشأة.</p>
              <p className="muted">تواصل مع إدارة المنصة أو قدّم طلب تفعيل من بوابة العملاء.</p>
            </>
          ) : (
            <dl className="kv">
              <dt>الحالة</dt>
              <dd>
                <span className={`badge ${subscription.data.status}`}>{subscription.data.status}</span>
              </dd>
              <dt>الباقة</dt>
              <dd>{subscription.data.plan_name ?? '—'}</dd>
              <dt>القيمة</dt>
              <dd dir="ltr">
                {subscription.data.amount ?? '—'} {subscription.data.currency ?? ''}
              </dd>
              <dt>المصدر</dt>
              <dd>{subscription.data.provider === 'stripe' ? 'Stripe' : 'تفعيل يدوي'}</dd>
              <dt>تاريخ التفعيل</dt>
              <dd dir="ltr">
                {subscription.data.activated_at ? new Date(subscription.data.activated_at).toLocaleDateString('ar-SA') : '—'}
              </dd>
              <dt>ينتهي في</dt>
              <dd dir="ltr">
                {subscription.data.current_period_end
                  ? new Date(subscription.data.current_period_end).toLocaleDateString('ar-SA')
                  : '—'}
              </dd>
            </dl>
          )}
        </section>
      )}
    </Screen>
  );
}
