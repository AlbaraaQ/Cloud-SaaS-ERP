'use client';

import { Empty, ErrorBox, Forbidden, Loading, Screen } from '../../components/screen';
import { apiData } from '../../lib/api';
import { useQuery } from '../../lib/use-query';

type OutboxRow = {
  id: string;
  topic?: string;
  event_type?: string;
  status?: string;
  attempts?: number;
  createdAt?: string;
  created_at?: string;
};

export default function JobsPage() {
  const outbox = useQuery<OutboxRow[] | { items?: OutboxRow[] }>(() => apiData('/jobs/outbox?limit=100'), []);
  const rows: OutboxRow[] = Array.isArray(outbox.data) ? outbox.data : ((outbox.data as { items?: OutboxRow[] })?.items ?? []);

  return (
    <Screen
      title="المهام والطوابير"
      subtitle="صندوق الأحداث الصادرة (outbox) وحالة معالجتها."
      crumbs={['المنصة', 'التشغيل']}
      actions={
        <button className="btn" type="button" onClick={outbox.reload}>
          تحديث
        </button>
      }
    >
      {outbox.status === 'loading' && <Loading />}
      {outbox.status === 'forbidden' && <Forbidden />}
      {outbox.status === 'error' && <ErrorBox message={outbox.error} onRetry={outbox.reload} />}
      {outbox.status === 'success' &&
        (rows.length === 0 ? (
          <Empty title="لا توجد أحداث معلقة" detail="هذا هو الوضع الطبيعي عندما يعمل العامل (worker)." />
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>الحدث</th>
                  <th>الحالة</th>
                  <th className="num">المحاولات</th>
                  <th>الوقت</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.id}>
                    <td dir="ltr">{row.topic ?? row.event_type ?? '—'}</td>
                    <td>
                      <span className={`badge ${row.status ?? ''}`}>{row.status ?? '—'}</span>
                    </td>
                    <td className="num">{row.attempts ?? 0}</td>
                    <td dir="ltr">{new Date(row.createdAt ?? row.created_at ?? Date.now()).toLocaleString('ar-SA')}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}
    </Screen>
  );
}
