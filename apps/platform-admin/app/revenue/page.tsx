'use client';

import { Empty, ErrorBox, Loading, Screen } from '../../components/screen';
import { apiData } from '../../lib/api';
import { useQuery } from '../../lib/use-query';

/**
 * لوحة الإيراد — شاشة يطلبها P-C4 («revenue dashboard: MRR · ARR · المتأخّر»).
 *
 * التعريفات مثبَّتة في العقود لا في الشاشة، لأن الرقم الذي لا تعريف له لا يُدار:
 *
 * * **MRR** = مجموع المكافئ الشهري للتراخيص المتعاقَدة (`active` · `past_due`). الباقة السنوية
 *   تدخل بمكافئها الشهري (4990 سنوياً = 415.83 شهرياً)، والتجربة والموقوف مؤقتاً ليسا إيراداً.
 * * **ARR** = `MRR × 12`.
 * * **المتأخّر** = غير المسدَّد على فاتورة صادرة تجاوزت تاريخ استحقاقها، و**غير المسدَّد** كله
 *   يُعرض بجانبه لأن ما استحقّ اليوم سيصير متأخّراً غداً.
 * * **المحصَّل هذا الشهر** = مجموع الدفعات المسجَّلة منذ بداية الشهر.
 *
 * ولا تُجمَع عملتان: حين تحمل الباقات أكثر من عملة يظهر وسم «عملات مختلطة» — جمع الريال
 * بالدولار بلا سعر صرف رقمٌ كاذب.
 */

type Revenue = {
  currency: string;
  mrr: string;
  arr: string;
  outstanding: string;
  overdue: string;
  overdueCount: number;
  mixedCurrency: boolean;
  collectedThisMonth: string;
  counts: { active: number; trialing: number; pastDue: number; paused: number; canceled: number };
  upcoming: Array<{
    invoiceId: string;
    number: string | null;
    tenantName: string;
    dueDate: string | null;
    total: string;
    remaining: string;
    daysOverdue: number;
  }>;
};

function dateText(value: string | null): string {
  return value ? new Date(value).toLocaleDateString('ar-SA') : '—';
}

export default function RevenuePage() {
  const revenue = useQuery<Revenue>(() => apiData<Revenue>('/platform/revenue'), []);
  // A `const` of the resolved value, not `revenue.data` in every cell: TypeScript keeps the
  // narrowing inside the map callbacks only if the reference itself cannot change.
  const data = revenue.data;

  return (
    <Screen
      title="الإيراد"
      subtitle="الإيراد المتكرّر والمتأخّر: أرقام شهرية واحدة، بتعريف مكتوب — لا رسوم بيانية بلا معنى."
      crumbs={['المنصة', 'العملاء والتراخيص']}
      actions={
        <button className="btn" type="button" onClick={revenue.reload}>
          تحديث
        </button>
      }
    >
      {revenue.status === 'loading' && <Loading />}
      {revenue.status === 'error' && <ErrorBox message={revenue.error} onRetry={revenue.reload} />}
      {revenue.status === 'success' && data && (
        <>
          <section className="card">
            <h2>الإيراد المتكرّر</h2>
            <div className="grid cols-2">
              <div>
                <p className="muted small" style={{ margin: 0 }}>
                  MRR — الإيراد الشهري المتكرّر
                </p>
                <h1 style={{ margin: 0 }}>
                  {Number(data.mrr).toLocaleString('ar-SA', { minimumFractionDigits: 2 })}{' '}
                  <span className="muted small">{data.currency}</span>
                </h1>
              </div>
              <div>
                <p className="muted small" style={{ margin: 0 }}>
                  ARR — الإيراد السنوي المتكرّر (×12)
                </p>
                <h1 style={{ margin: 0 }}>
                  {Number(data.arr).toLocaleString('ar-SA', { minimumFractionDigits: 2 })}{' '}
                  <span className="muted small">{data.currency}</span>
                </h1>
              </div>
            </div>
            {data.mixedCurrency && (
              <p className="alert warn">الباقات تحمل أكثر من عملة: الأرقام أعلاه بعملة العرض وحدها.</p>
            )}
          </section>

          <section className="card">
            <h2>المتأخّر والتحصيل</h2>
            <dl className="kv">
              <dt>المتأخّر</dt>
              <dd>
                {Number(data.overdue).toLocaleString('ar-SA', { minimumFractionDigits: 2 })} {data.currency}
                {data.overdueCount > 0 && ` · ${data.overdueCount} فاتورة`}
              </dd>
              <dt>كل ما لم يُسدَّد</dt>
              <dd>
                {Number(data.outstanding).toLocaleString('ar-SA', { minimumFractionDigits: 2 })} {data.currency}
              </dd>
              <dt>المحصَّل هذا الشهر</dt>
              <dd>
                {Number(data.collectedThisMonth).toLocaleString('ar-SA', { minimumFractionDigits: 2 })}{' '}
                {data.currency}
              </dd>
            </dl>
          </section>

          <section className="card">
            <h2>التراخيص</h2>
            <dl className="kv">
              <dt>فعّالة</dt>
              <dd>{data.counts.active}</dd>
              <dt>متأخّرة</dt>
              <dd>{data.counts.pastDue}</dd>
              <dt>تجربة</dt>
              <dd>{data.counts.trialing}</dd>
              <dt>موقوفة مؤقتاً</dt>
              <dd>{data.counts.paused}</dd>
              <dt>ملغاة</dt>
              <dd>{data.counts.canceled}</dd>
            </dl>
            <p className="muted small" style={{ marginBottom: 0 }}>
              MRR يعدّ «فعّالة» و«متأخّرة» فقط: من تأخّر في السداد ما زال متعاقداً، ومن يُجرّب لم يشترِ بعد.
            </p>
          </section>

          <section className="card">
            <h2>فواتير قادمة</h2>
            {data.upcoming.length === 0 ? (
              <Empty title="لا فواتير قائمة" detail="لا فاتورة صادرة غير مدفوعة في هذه اللحظة." />
            ) : (
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>الفاتورة</th>
                      <th>العميل</th>
                      <th>الاستحقاق</th>
                      <th className="num">الإجمالي</th>
                      <th className="num">المتبقّي</th>
                      <th>الحالة</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.upcoming.map((row) => (
                      <tr key={row.invoiceId}>
                        <td dir="ltr">{row.number ?? '—'}</td>
                        <td>{row.tenantName}</td>
                        <td dir="ltr">{dateText(row.dueDate)}</td>
                        <td className="num">
                          {Number(row.total).toLocaleString('ar-SA', { minimumFractionDigits: 2 })} {data.currency}
                        </td>
                        <td className="num">
                          {Number(row.remaining).toLocaleString('ar-SA', { minimumFractionDigits: 2 })} {data.currency}
                        </td>
                        <td>
                          {row.daysOverdue > 0 ? (
                            <span className="badge failed">متأخّرة {row.daysOverdue} يوماً</span>
                          ) : (
                            <span className="badge pending">تنتظر السداد</span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}
    </Screen>
  );
}
