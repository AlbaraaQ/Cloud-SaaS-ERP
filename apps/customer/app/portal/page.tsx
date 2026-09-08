'use client';

import { useEffect, useState } from 'react';

import { Kpi } from '../../components/card';
import { PortalShell } from '../../components/portal-shell';
import { SimpleTable } from '../../components/table';
import { portalFetch } from '../../lib/api';

type Subscription = { status: string; provider: string; plan_name: string; amount: string; currency: string; current_period_end?: string };

function token() { const value = globalThis.document.cookie.split('; ').find((part) => part.startsWith('erp_access_token=')); return value ? decodeURIComponent(value.split('=')[1] ?? '') : undefined; }

export default function PortalDashboard() {
  const [subscription, setSubscription] = useState<Subscription | null>(null);
  const [error, setError] = useState('');
  useEffect(() => { portalFetch<{ data: Subscription | null }>('/billing/subscription', token()).then((result) => setSubscription(result.data)).catch(() => setError('تعذر تحميل حالة الاشتراك')); }, []);
  return <PortalShell><section><h1>لوحة العميل</h1><p className="muted">بيانات شركتك الحقيقية وحالة اشتراكك في النظام المحاسبي.</p></section>{error && <p className="muted" role="alert">{error}</p>}<div className="grid cols"><Kpi label="حالة الاشتراك" value={subscription?.status ?? 'غير مشترك'} /><Kpi label="الباقة" value={subscription?.plan_name ?? '-'} /><Kpi label="مزود الاشتراك" value={subscription?.provider ?? '-'} /></div><SimpleTable rows={subscription ? [{ id: subscription.plan_name, plan: subscription.plan_name, amount: `${subscription.amount} ${subscription.currency}`, renewal: subscription.current_period_end ? new Date(subscription.current_period_end).toLocaleDateString('ar-SA') : '-', status: subscription.status }] : [{ id: 'empty', plan: 'لا يوجد اشتراك فعال', amount: '-', renewal: '-', status: 'pending' }]} columns={['plan','amount','renewal','status']} /></PortalShell>;
}
