'use client';

import Link from 'next/link';

import { Screen } from '../components/screen';
import { apiData } from '../lib/api';
import { modules, screenCounts, visibleModules } from '../lib/navigation';
import { useSession } from '../lib/session';
import { useQuery } from '../lib/use-query';

type Subscription = {
  status: string;
  plan_name?: string;
  plan_code?: string;
  amount?: string;
  currency?: string;
  current_period_end?: string | null;
} | null;

/** The platform console is a separate deployment; link out instead of routing in-app. */
function platformConsoleUrl(): string {
  const base = (process.env.NEXT_PUBLIC_PLATFORM_URL ?? '').replace(/\/+$/, '');
  return `${base}/`;
}

export default function DashboardPage() {
  const { me, isPlatformAdmin, can } = useSession();
  const counts = screenCounts();

  const subscription = useQuery<Subscription>(() => apiData<Subscription>('/billing/subscription'), []);
  const trial = useQuery<{ lines?: unknown[] } | unknown[]>(
    () => (can('accounting.reports.view') ? apiData('/statements/trial-balance') : Promise.resolve([])),
    [can('accounting.reports.view')],
  );

  const tree = visibleModules(me?.permissions ?? [], isPlatformAdmin);
  const trialRows = Array.isArray(trial.data) ? trial.data.length : ((trial.data as { lines?: unknown[] })?.lines?.length ?? 0);

  return (
    <Screen
      title={`أهلاً ${me?.user.fullName ?? ''}`}
      subtitle="نقطة البداية لكل وحدات النظام. الشاشات الجاهزة مميّزة بنقطة خضراء في القائمة الجانبية."
      actions={
        isPlatformAdmin ? (
          <a className="btn primary" href={platformConsoleUrl()}>
            لوحة تحكم المنصة
          </a>
        ) : null
      }
    >
      <div className="grid cols">
        <article className="card">
          <p className="muted">حالة الاشتراك</p>
          <div className="kpi" style={{ fontSize: 20 }}>
            {subscription.status === 'loading'
              ? '…'
              : subscription.data
                ? subscription.data.status === 'active'
                  ? 'مفعّل'
                  : subscription.data.status
                : 'بدون اشتراك'}
          </div>
          <small className="muted">{subscription.data?.plan_name ?? 'لم يتم اختيار باقة بعد'}</small>
        </article>
        <article className="card">
          <p className="muted">حسابات في ميزان المراجعة</p>
          <div className="kpi">{trial.status === 'loading' ? '…' : trialRows}</div>
          <small className="muted">/statements/trial-balance</small>
        </article>
        <article className="card">
          <p className="muted">شاشات جاهزة</p>
          <div className="kpi">
            {counts.ready}
            <span className="muted" style={{ fontSize: 14 }}>
              {' '}
              / {counts.total}
            </span>
          </div>
          <small className="muted">
            {counts.api} شاشة واجهتها البرمجية جاهزة · {counts.planned} قيد التطوير
          </small>
        </article>
        <article className="card">
          <p className="muted">صلاحياتك</p>
          <div className="kpi">{me?.permissions.includes('*') ? 'كاملة' : (me?.permissions.length ?? 0)}</div>
          <small className="muted">{me?.membership.isOwner ? 'مالك المنشأة' : 'مستخدم'}</small>
        </article>
      </div>

      <section className="card">
        <h2>وحدات النظام</h2>
        <div className="grid cols-2">
          {tree.map((module) => {
            const screenCount = module.groups.reduce((sum, group) => sum + group.items.length, 0);
            const readyCount = module.groups.reduce(
              (sum, group) => sum + group.items.filter((item) => item.status === 'ready').length,
              0,
            );
            return (
              <Link className="card tight" key={module.key} href={module.href} style={{ display: 'block' }}>
                <div className="row" style={{ justifyContent: 'space-between', alignItems: 'center' }}>
                  <strong>
                    {module.icon} {module.labelAr}
                  </strong>
                  <span className="badge">{readyCount}/{screenCount}</span>
                </div>
                <p className="muted small" style={{ margin: '6px 0 0' }}>
                  {module.groups.map((group) => group.labelAr).join(' · ')}
                </p>
              </Link>
            );
          })}
        </div>
      </section>

      <section className="card">
        <h2>خارطة التنفيذ</h2>
        <p className="muted small">
          إجمالي الشاشات المخطط لها في الشجرة: {counts.total}. هذه القائمة تعكس الحالة الحقيقية لكل شاشة —
          لا توجد شاشات تعرض بيانات وهمية.
        </p>
        <div className="table-wrap" style={{ maxHeight: 320 }}>
          <table>
            <thead>
              <tr>
                <th>الوحدة</th>
                <th>جاهزة</th>
                <th>API جاهز</th>
                <th>قيد التطوير</th>
              </tr>
            </thead>
            <tbody>
              {modules.map((module) => {
                const items = module.groups.flatMap((group) => group.items);
                return (
                  <tr key={module.key}>
                    <td>
                      {module.icon} {module.labelAr}
                    </td>
                    <td className="num">{items.filter((item) => item.status === 'ready').length}</td>
                    <td className="num">{items.filter((item) => item.status === 'api').length}</td>
                    <td className="num">{items.filter((item) => item.status === 'planned').length}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </section>
    </Screen>
  );
}
