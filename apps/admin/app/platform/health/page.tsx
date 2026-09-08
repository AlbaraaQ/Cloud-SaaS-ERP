'use client';

import { useEffect, useState } from 'react';

import { Screen } from '../../../components/screen';
import { apiBaseUrl } from '../../../lib/api';

type Probe = { name: string; url: string; status: 'checking' | 'up' | 'down'; detail?: string };

const PROBES: Array<{ name: string; url: string }> = [
  { name: 'الواجهة البرمجية — جاهزية', url: '/api/health/ready' },
  { name: 'الواجهة البرمجية — حياة', url: '/api/health/live' },
];

export default function HealthPage() {
  const [probes, setProbes] = useState<Probe[]>(PROBES.map((probe) => ({ ...probe, status: 'checking' })));
  const [nonce, setNonce] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setProbes(PROBES.map((probe) => ({ ...probe, status: 'checking' })));
    void Promise.all(
      PROBES.map(async (probe): Promise<Probe> => {
        try {
          const response = await fetch(probe.url, { cache: 'no-store' });
          const text = await response.text();
          return { ...probe, status: response.ok ? 'up' : 'down', detail: text.slice(0, 200) };
        } catch (error) {
          return { ...probe, status: 'down', detail: error instanceof Error ? error.message : String(error) };
        }
      }),
    ).then((results) => {
      if (!cancelled) setProbes(results);
    });
    return () => {
      cancelled = true;
    };
  }, [nonce]);

  return (
    <Screen
      title="صحة النظام"
      subtitle="فحوصات مباشرة تُنفَّذ من متصفحك عبر نفس المسار الذي تستخدمه الواجهة."
      crumbs={['المنصة', 'التشغيل']}
      actions={
        <button className="btn" type="button" onClick={() => setNonce((value) => value + 1)}>
          إعادة الفحص
        </button>
      }
    >
      <div className="card">
        <dl className="kv">
          <dt>عنوان الـ API المستخدم</dt>
          <dd dir="ltr">{apiBaseUrl}</dd>
          <dt>وضع الاتصال</dt>
          <dd>{apiBaseUrl.startsWith('/') ? 'نفس المصدر عبر وسيط Next.js (موصى به)' : 'مصدر خارجي — يتطلب ضبط CORS'}</dd>
        </dl>
      </div>

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>الفحص</th>
              <th>المسار</th>
              <th>الحالة</th>
              <th>التفاصيل</th>
            </tr>
          </thead>
          <tbody>
            {probes.map((probe) => (
              <tr key={probe.url}>
                <td>{probe.name}</td>
                <td dir="ltr" className="small">{probe.url}</td>
                <td>
                  <span className={`badge ${probe.status === 'up' ? 'active' : probe.status === 'down' ? 'failed' : 'pending'}`}>
                    {probe.status === 'up' ? 'سليم' : probe.status === 'down' ? 'متعطل' : 'جارٍ الفحص'}
                  </span>
                </td>
                <td dir="ltr" className="small">{probe.detail ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </Screen>
  );
}
