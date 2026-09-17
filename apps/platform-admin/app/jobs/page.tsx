'use client';

import { useState } from 'react';

import { Empty, ErrorBox, Forbidden, Loading, Screen } from '../../components/screen';
import { apiData } from '../../lib/api';
import { useQuery } from '../../lib/use-query';

/**
 * المهام والطوابير — the queue across every customer (P-C1).
 *
 * The page used to read `GET /jobs/outbox`: a tenant-scoped endpoint, so an operator of the
 * platform tenant saw an empty list and had no way to tell "the queue is idle" from "I am
 * looking in the wrong place". It now reads `GET /platform/jobs/outbox`, which joins each
 * job to its customer and is gated by `console.jobs.view`.
 */

type OutboxRow = {
  id: string;
  tenantId: string;
  tenantCode: string | null;
  queue: string;
  type: string;
  status: string;
  attempts: number;
  lastError: string | null;
  createdAt: string;
};

type OutboxPage = { items: OutboxRow[]; total: number; limit: number; offset: number };

const STATUS_LABEL: Record<string, string> = {
  pending: 'بانتظار النشر',
  published: 'نُشرت',
  dead: 'ميتة',
};

export default function JobsPage() {
  const [status, setStatus] = useState('');
  const outbox = useQuery<OutboxPage>(() => {
    const params = new URLSearchParams({ limit: '100' });
    if (status) params.set('status', status);
    return apiData<OutboxPage>(`/platform/jobs/outbox?${params.toString()}`);
  }, [status]);

  const rows = outbox.data?.items ?? [];

  return (
    <Screen
      title="المهام والطوابير"
      subtitle="صندوق الأحداث الصادرة (outbox) عبر كل العملاء: ما بانتظار النشر، وما فشل، ومتى. للقراءة فقط — إعادة المحاولة في جزء العمليات."
      crumbs={['المنصة', 'التشغيل']}
      actions={
        <button className="btn" type="button" onClick={outbox.reload}>
          تحديث
        </button>
      }
    >
      <div className="card tight no-print">
        <div className="row">
          <label className="field" style={{ minWidth: 200 }}>
            <span>الحالة</span>
            <select className="input" value={status} onChange={(event) => setStatus(event.target.value)}>
              <option value="">الكل</option>
              <option value="pending">بانتظار النشر</option>
              <option value="published">نُشرت</option>
              <option value="dead">ميتة</option>
            </select>
          </label>
        </div>
      </div>

      {outbox.status === 'loading' && <Loading />}
      {outbox.status === 'forbidden' && <Forbidden />}
      {outbox.status === 'error' && <ErrorBox message={outbox.error} onRetry={outbox.reload} />}
      {outbox.status === 'success' &&
        (rows.length === 0 ? (
          <Empty
            title="لا توجد أحداث معلقة"
            detail="هذا هو الوضع الطبيعي عندما يعمل العامل (worker) ولا توجد مهام فاشلة."
          />
        ) : (
          <>
            <p className="muted small">
              {rows.length} من {outbox.data?.total ?? rows.length} مهمة
            </p>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>الوقت</th>
                    <th>العميل</th>
                    <th>الطابور</th>
                    <th>النوع</th>
                    <th>الحالة</th>
                    <th className="num">المحاولات</th>
                    <th>آخر خطأ</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((row) => (
                    <tr key={row.id}>
                      <td dir="ltr">{new Date(row.createdAt).toLocaleString('ar-SA')}</td>
                      <td dir="ltr">{row.tenantCode ?? row.tenantId.slice(0, 8)}</td>
                      <td dir="ltr">{row.queue}</td>
                      <td dir="ltr">{row.type}</td>
                      <td>
                        <span className={`badge ${row.status === 'dead' ? 'failed' : row.status === 'pending' ? 'pending' : 'active'}`}>
                          {STATUS_LABEL[row.status] ?? row.status}
                        </span>
                      </td>
                      <td className="num">{row.attempts}</td>
                      <td dir="ltr" className="small">
                        {row.lastError ? row.lastError.slice(0, 80) : '—'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        ))}
    </Screen>
  );
}
