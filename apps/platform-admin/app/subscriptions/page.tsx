'use client';

import { useState } from 'react';

import { Empty, ErrorBox, Loading, Screen } from '../../components/screen';
import { ApiError, apiData, apiPost } from '../../lib/api';
import { useQuery } from '../../lib/use-query';

type Subscription = {
  id: string;
  status: string;
  provider: string;
  current_period_start: string | null;
  current_period_end: string | null;
  activated_at: string | null;
  tenant_code: string;
  tenant_name: string;
  tenant_status: string;
  plan_name: string;
  plan_code: string;
  amount: string;
  currency: string;
  interval: string;
};

export default function SubscriptionsPage() {
  const [status, setStatus] = useState('');
  const [message, setMessage] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();

  const subscriptions = useQuery<Subscription[]>(
    () => apiData<Subscription[]>(`/platform/subscriptions${status ? `?status=${status}` : ''}`),
    [status],
  );

  async function cancel(id: string) {
    if (!window.confirm('تأكيد إلغاء هذا الترخيص؟')) return;
    try {
      await apiPost(`/platform/subscriptions/${id}/cancel`, {});
      setMessage({ kind: 'ok', text: 'تم إلغاء الترخيص.' });
      subscriptions.reload();
    } catch (error) {
      setMessage({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    }
  }

  const rows = subscriptions.data ?? [];

  return (
    <Screen
      title="الاشتراكات والتراخيص"
      subtitle="كل ترخيص صادر عبر المنصة: يدوي أو عبر Stripe."
      crumbs={['المنصة', 'العملاء والتراخيص']}
      actions={
        <select className="input" style={{ maxWidth: 180 }} value={status} onChange={(event) => setStatus(event.target.value)}>
          <option value="">كل الحالات</option>
          <option value="active">فعّال</option>
          <option value="past_due">متأخر</option>
          <option value="canceled">ملغى</option>
          <option value="incomplete">غير مكتمل</option>
        </select>
      }
    >
      {message && <p className={`alert ${message.kind}`}>{message.text}</p>}
      {subscriptions.status === 'loading' && <Loading />}
      {subscriptions.status === 'error' && <ErrorBox message={subscriptions.error} onRetry={subscriptions.reload} />}
      {subscriptions.status === 'success' &&
        (rows.length === 0 ? (
          <Empty title="لا توجد تراخيص" detail="أصدر ترخيصاً من صفحة العملاء." />
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
                  <th />
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.id}>
                    <td>
                      <strong>{row.tenant_name}</strong>
                      <div className="muted small" dir="ltr">
                        {row.tenant_code}
                      </div>
                    </td>
                    <td>{row.plan_name}</td>
                    <td className="num">
                      {Number(row.amount).toLocaleString('ar-SA', { minimumFractionDigits: 2 })} {row.currency}
                      <div className="muted small">{row.interval === 'year' ? 'سنوي' : 'شهري'}</div>
                    </td>
                    <td>{row.provider === 'stripe' ? 'Stripe' : 'يدوي'}</td>
                    <td>
                      <span className={`badge ${row.status}`}>{row.status}</span>
                    </td>
                    <td dir="ltr">
                      {row.current_period_start ? new Date(row.current_period_start).toLocaleDateString('ar-SA') : '—'}
                    </td>
                    <td dir="ltr">
                      {row.current_period_end ? new Date(row.current_period_end).toLocaleDateString('ar-SA') : '—'}
                    </td>
                    <td>
                      {row.status === 'active' && (
                        <button className="btn sm danger" type="button" onClick={() => void cancel(row.id)}>
                          إلغاء
                        </button>
                      )}
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
