'use client';

import Link from 'next/link';

import { ErrorBox, Loading, Screen } from '../../components/screen';
import { apiData } from '../../lib/api';
import { useQuery } from '../../lib/use-query';

type Overview = {
  tenants: { total: number; active: number; suspended: number; new_30d: number };
  subscriptions: { active: number; past_due: number; canceled: number; mrr: string };
  pendingActivations: number;
  users: { total: number; active: number };
};

export default function PlatformOverviewPage() {
  const overview = useQuery<Overview>(() => apiData<Overview>('/platform/overview'), []);

  return (
    <Screen
      title="لوحة تحكم المنصة"
      subtitle="مؤشرات تشغيل الخدمة: العملاء، التراخيص، الإيراد الشهري وطلبات التفعيل."
      crumbs={['المنصة']}
      actions={
        <>
          <Link className="btn primary" href="/platform/tenants/new">
            عميل جديد
          </Link>
          <button className="btn" type="button" onClick={overview.reload}>
            تحديث
          </button>
        </>
      }
    >
      {overview.status === 'loading' && <Loading />}
      {overview.status === 'error' && <ErrorBox message={overview.error} onRetry={overview.reload} />}
      {overview.data && (
        <>
          <div className="grid cols">
            <article className="card">
              <p className="muted">إجمالي العملاء</p>
              <div className="kpi">{overview.data.tenants.total}</div>
              <small className="muted">{overview.data.tenants.new_30d} جديد خلال 30 يوماً</small>
            </article>
            <article className="card">
              <p className="muted">عملاء نشطون</p>
              <div className="kpi" style={{ color: '#047857' }}>{overview.data.tenants.active}</div>
              <small className="muted">{overview.data.tenants.suspended} موقوف</small>
            </article>
            <article className="card">
              <p className="muted">تراخيص فعّالة</p>
              <div className="kpi">{overview.data.subscriptions.active}</div>
              <small className="muted">
                {overview.data.subscriptions.past_due} متأخر · {overview.data.subscriptions.canceled} ملغى
              </small>
            </article>
            <article className="card">
              <p className="muted">الإيراد الشهري المتكرر (MRR)</p>
              <div className="kpi" style={{ fontSize: 22 }}>
                {Number(overview.data.subscriptions.mrr || 0).toLocaleString('ar-SA', { minimumFractionDigits: 2 })}
              </div>
              <small className="muted">من الباقات الشهرية النشطة</small>
            </article>
          </div>

          <div className="grid cols-2">
            <section className="card">
              <h2>طلبات التفعيل المعلقة</h2>
              <div className="kpi">{overview.data.pendingActivations}</div>
              <p className="muted small">طلبات اشتراك يدوية بانتظار موافقتك.</p>
              <Link className="btn primary" href="/platform/activation-requests">
                مراجعة الطلبات
              </Link>
            </section>

            <section className="card">
              <h2>المستخدمون</h2>
              <dl className="kv">
                <dt>إجمالي الحسابات</dt>
                <dd>{overview.data.users.total}</dd>
                <dt>نشط</dt>
                <dd>{overview.data.users.active}</dd>
              </dl>
              <Link className="btn" href="/platform/users" style={{ marginTop: 10 }}>
                إدارة المستخدمين
              </Link>
            </section>
          </div>
        </>
      )}
    </Screen>
  );
}
