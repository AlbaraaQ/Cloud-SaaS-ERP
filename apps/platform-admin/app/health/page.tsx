'use client';

import { Screen } from '../../components/screen';
import { apiBaseUrl, apiData } from '../../lib/api';
import { useQuery } from '../../lib/use-query';

/**
 * صحة النظام — الصفحة تقرأ الآن `GET /platform/health/detailed` (P-C9).
 *
 * قبل هذا الجزء كانت الشاشة تفحص `GET /api/health/ready` من **متصفّح المشغّل**، وهذا كان
 * يجيب سؤالاً آخر: «هل يصل متصفّحي إلى الواجهة؟» لا «هل الخدمة بخير؟» — ومسار الوكيل في
 * Next.js ينجح حتى ومزوّد البريد معطَّل. الآن تُقاس ستّة مجسّات على الخادم (قاعدة · Redis ·
 * تخزين · بريد · طابور · عامل) ومعها عدّادات الطلبات ودلاء الاستجابة، ويبقى الفحص المحلي
 * صفاً واحداً في الأسفل: هو دليلٌ على أن *هذا المتصفّح* يرى الـAPI، لا على صحة الخدمة.
 */

type Probe = {
  name: string;
  status: 'up' | 'degraded' | 'down' | 'not_configured';
  detail: string;
  latencyMs: number | null;
};

type Health = {
  status: 'ok' | 'degraded' | 'down';
  checkedAt: string;
  startedAt: string;
  uptimeSeconds: number;
  incident: { active: boolean; message: string | null };
  requests: { count: number; errors: number; errorRate: number; p95Ms: number };
  backlog: { pending: number; published: number; dead: number; oldestPendingAgeSeconds: number | null };
  probes: Probe[];
};

const PROBE_LABEL: Record<string, string> = {
  database: 'قاعدة البيانات',
  redis: 'Redis',
  storage: 'مخزن الملفات',
  email: 'البريد',
  queue: 'الطابور',
  worker: 'العامل',
};

const STATUS_LABEL: Record<string, string> = {
  up: 'سليم',
  degraded: 'متدهوّر',
  down: 'متوقّف',
  not_configured: 'غير مهيّأ',
};

const STATUS_CLASS: Record<string, string> = {
  up: 'active',
  degraded: 'pending',
  down: 'failed',
  not_configured: 'pending',
};

const OVERALL: Record<string, { label: string; className: string }> = {
  ok: { label: 'الخدمة سليمة', className: 'active' },
  degraded: { label: 'الخدمة متدهوّرة', className: 'pending' },
  down: { label: 'الخدمة متوقّفة', className: 'failed' },
};

export default function HealthPage() {
  const health = useQuery<Health>(() => apiData<Health>('/platform/health/detailed'), []);
  const data = health.data;

  return (
    <Screen
      title="صحة النظام"
      subtitle="مجسّاتٌ تُقاس على الخادم لحظة الطلب، ومعها عدّادات الطلبات وحجم الطابور. «غير مهيّأ» ليس عطلاً — العطل أن يكون شيءٌ مهيّأً ومتوقّفاً."
      crumbs={['المنصة', 'التشغيل']}
      actions={
        <button className="btn" type="button" onClick={health.reload}>
          إعادة الفحص
        </button>
      }
    >
      {health.status === 'loading' && <div className="card">جارٍ الفحص…</div>}
      {health.status === 'forbidden' && <div className="card">لا تملك `console.health.view`.</div>}
      {health.status === 'error' && (
        <div className="card">
          <strong>تعذّر الفحص.</strong> <span className="muted">{health.error}</span>
        </div>
      )}

      {health.status === 'success' && data && (
        <>
          {data.incident.active && (
            <div className="alert danger">
              <strong>حادثة معلَنة:</strong> {data.incident.message ?? 'صيانة جارية'}
              <div className="muted small" style={{ marginTop: 4 }}>
                اللافتة من `platform.maintenance*` في إعدادات المنصة — لا نصٌّ في هذه الشاشة.
              </div>
            </div>
          )}

          <div className="card tight">
            <div className="row" style={{ alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
              <span className={`badge ${OVERALL[data.status]?.className ?? 'pending'}`}>
                {OVERALL[data.status]?.label ?? data.status}
              </span>
              <span className="muted small">
                مُقلعة منذ {formatDuration(data.uptimeSeconds)} · آخر فحص{' '}
                {new Date(data.checkedAt).toLocaleTimeString('ar-SA')}
              </span>
            </div>
          </div>

          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>المجسّ</th>
                  <th>الحالة</th>
                  <th className="num">زمن الاستجابة</th>
                  <th>التفصيل</th>
                </tr>
              </thead>
              <tbody>
                {data.probes.map((probe) => (
                  <tr key={probe.name}>
                    <td>{PROBE_LABEL[probe.name] ?? probe.name}</td>
                    <td>
                      <span className={`badge ${STATUS_CLASS[probe.status] ?? 'pending'}`}>
                        {STATUS_LABEL[probe.status] ?? probe.status}
                      </span>
                    </td>
                    <td className="num">{probe.latencyMs === null ? '—' : `${probe.latencyMs} ms`}</td>
                    <td className="small">{probe.detail}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="grid two">
            <div className="card">
              <h3 style={{ marginTop: 0 }}>الطلبات</h3>
              <dl className="kv">
                <dt>العدد الكلي</dt>
                <dd className="num">{data.requests.count.toLocaleString('ar-SA')}</dd>
                <dt>أخطاء 5xx</dt>
                <dd className="num">{data.requests.errors.toLocaleString('ar-SA')}</dd>
                <dt>نسبة الخطأ</dt>
                <dd className="num">{(data.requests.errorRate * 100).toFixed(2)}٪</dd>
                <dt>p95</dt>
                <dd className="num">{data.requests.p95Ms.toLocaleString('ar-SA')} ms</dd>
              </dl>
              <p className="muted small">
                الأرقام من عدّاد العملية ودلائها (تُجمع من كل مسار) — لا من تخزينٍ ثانٍ ولا من تقدير. p95 هي
                أقلّ دلوٍّ يغطّي 95٪ من الطلبات.
              </p>
            </div>

            <div className="card">
              <h3 style={{ marginTop: 0 }}>الطابور</h3>
              <dl className="kv">
                <dt>بانتظار النشر</dt>
                <dd className="num">{data.backlog.pending.toLocaleString('ar-SA')}</dd>
                <dt>نُشرت</dt>
                <dd className="num">{data.backlog.published.toLocaleString('ar-SA')}</dd>
                <dt>ميتة</dt>
                <dd className="num">{data.backlog.dead.toLocaleString('ar-SA')}</dd>
                <dt>أقدم معلَّقة</dt>
                <dd>
                  {data.backlog.oldestPendingAgeSeconds === null
                    ? 'لا شيء'
                    : formatDuration(data.backlog.oldestPendingAgeSeconds)}
                </dd>
              </dl>
              <p className="muted small">
                الفعل على هذه الصفوف في <a href="/jobs">المهام والطوابير</a> — هذه الشاشة تقيس ولا تعالج.
              </p>
            </div>
          </div>
        </>
      )}

      <div className="card tight">
        <dl className="kv">
          <dt>عنوان الـ API المستخدم</dt>
          <dd dir="ltr">{apiBaseUrl}</dd>
          <dt>وضع الاتصال</dt>
          <dd>
            {apiBaseUrl.startsWith('/')
              ? 'نفس المصدر عبر وسيط Next.js (موصى به)'
              : 'مصدر خارجي — يتطلب ضبط CORS'}
          </dd>
        </dl>
        <p className="muted small" style={{ marginBottom: 0 }}>
          هذه الصفحة تقرأ من الـAPI نفسه، فوصولها دليلٌ على أن الجلسة والوسيط سليمان — أما المجسّات أعلاه فهي
          التي تُجيب «هل الخدمة بخير؟».
        </p>
      </div>
    </Screen>
  );
}

function formatDuration(seconds: number): string {
  if (seconds < 60) return `${seconds} ثانية`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)} دقيقة`;
  if (seconds < 86_400) return `${Math.floor(seconds / 3600)} ساعة`;
  return `${Math.floor(seconds / 86_400)} يوم`;
}
