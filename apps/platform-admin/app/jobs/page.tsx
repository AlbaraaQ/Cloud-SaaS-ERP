'use client';

import { useState } from 'react';

import { Empty, ErrorBox, Forbidden, Loading, Screen } from '../../components/screen';
import { apiData } from '../../lib/api';
import { useSession } from '../../lib/session';
import { useQuery } from '../../lib/use-query';

/**
 * المهام والطوابير — the outbox across every customer, **with the two actions** (P-C9).
 *
 * P-C1 opened this page read-only and said so in its subtitle («إعادة المحاولة في جزء
 * العمليات»). This is that part: «إعادة» تُعيد صفّاً معلَّقاً أو ميتاً إلى الطابور بمحاولاتٍ
 * مصفَّرة، و«إلغاء» توسم الصفّ `dead` بسببٍ مكتوب في قرأه لاحقاً، والاثنان مقيَّدان بـ
 * `console.jobs.manage` — فمدقّق المنصة يقرأ الطابور ولا يُشغّله.
 *
 * والأعمدة الثلاثة التي أُضيفت هنا هي ما يجعل الفعل قراراً لا قماراً: **الاستحقاق**
 * (`runAt` — مهمّةٌ مجدولة بلا وقتها لا تُثبت جدولتها)، و**مفاتيح الحمولة** (بلا قيمٍ:
 * قرار «أُعيد المحاولة» لا يحتاج بيانات عميل)، و**نوع الخطأ** كما هو بلا تلخيص.
 */

type JobRow = {
  id: string;
  tenantId: string;
  tenantCode: string | null;
  queue: string;
  type: string;
  status: 'pending' | 'published' | 'dead';
  attempts: number;
  lastError: string | null;
  runAt: string;
  processedAt: string | null;
  createdAt: string;
  payloadKeys: string[];
};

type JobPage = { data: JobRow[]; meta: { total: number; limit: number; offset: number } };

type Heartbeat = { running: boolean; enabled: boolean; oldestPendingAgeSeconds: number | null };

const STATUS_LABEL: Record<string, string> = {
  pending: 'بانتظار النشر',
  published: 'نُشرت',
  dead: 'ميتة',
};

const STATUS_CLASS: Record<string, string> = {
  pending: 'pending',
  published: 'active',
  dead: 'failed',
};

export default function JobsPage() {
  const { canConsole } = useSession();
  const [status, setStatus] = useState('');
  const [onlyTenant, setOnlyTenant] = useState('');
  const [reason, setReason] = useState('');
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>(undefined);
  const [busyId, setBusyId] = useState<string | null>(null);

  const jobs = useQuery<JobPage>(() => {
    const params = new URLSearchParams({ limit: '100' });
    if (status) params.set('filter[status]', status);
    if (onlyTenant.trim()) params.set('filter[tenantId]', onlyTenant.trim());
    return apiData<JobPage>(`/platform/jobs?${params.toString()}`);
  }, [status, onlyTenant]);

  const heartbeat = useQuery<Heartbeat>(() => apiData<Heartbeat>('/platform/jobs/heartbeat'), []);

  const rows = jobs.data?.data ?? [];
  const canManage = canConsole('console.jobs.manage');
  const reasonReady = reason.trim().length >= 5;

  async function act(row: JobRow, action: 'retry' | 'cancel') {
    const verb = action === 'retry' ? 'إعادة' : 'إلغاء';
    if (
      !window.confirm(`${verb} المهمّة «${row.type}» للعميل ${row.tenantCode ?? row.tenantId.slice(0, 8)}؟`)
    ) {
      return;
    }
    setBusyId(row.id);
    setNotice(undefined);
    try {
      const updated = await apiData<JobRow>(`/platform/jobs/${row.id}/${action}`, {
        method: 'POST',
        body: JSON.stringify({ reason: reason.trim() }),
      });
      setNotice({
        kind: 'ok',
        text:
          action === 'retry'
            ? `أُعيدت «${updated.type}» إلى الطابور بمحاولاتٍ مصفَّرة (${updated.status}).`
            : `أُلغيت «${updated.type}» فصارت ${STATUS_LABEL[updated.status] ?? updated.status} بسببٍ مسجَّل.`,
      });
      jobs.reload();
      heartbeat.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: apiMessage(error) });
    } finally {
      setBusyId(null);
    }
  }

  return (
    <Screen
      title="المهام والطوابير"
      subtitle="صندوق الأحداث الصادرة (outbox) عبر كل العملاء: ما بانتظار النشر، وما فشل، ومتى — مع إعادة المحاولة والإلغاء بسببٍ مكتوب."
      crumbs={['المنصة', 'التشغيل']}
      actions={
        <>
          <button className="btn" type="button" onClick={jobs.reload}>
            تحديث
          </button>
          <button className="btn" type="button" onClick={heartbeat.reload}>
            نبض العامل
          </button>
        </>
      }
    >
      {heartbeat.status === 'success' && (
        <div className="card tight no-print">
          <div className="row" style={{ alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
            <span className={`badge ${heartbeat.data?.running ? 'active' : 'pending'}`}>
              {heartbeat.data?.running ? 'عاملٌ يعمل' : 'لا عامل يعمل (WORKER=0)'}
            </span>
            <span className={`badge ${heartbeat.data?.enabled ? 'active' : 'pending'}`}>
              {heartbeat.data?.enabled ? 'الطابور موصول (Redis)' : 'الطابور في القاعدة فقط'}
            </span>
            <span className="muted small">
              {heartbeat.data?.oldestPendingAgeSeconds === null
                ? 'لا صفوف معلَّقة الآن.'
                : `أقدم صفٍّ معلَّق قبل ${formatAge(heartbeat.data?.oldestPendingAgeSeconds ?? 0)}.`}
            </span>
          </div>
          <p className="muted small" style={{ margin: '6px 0 0' }}>
            «لا عامل يعمل» ليس عطلاً بذاته: الصفوف تبقى معلَّقة في القاعدة بلا خسارة — لكنها لا تُنفَّذ.
            الاثنان معاً (لا عامل + تراكم) هما الحادثة.
          </p>
        </div>
      )}

      <div className="card tight no-print">
        <div className="row" style={{ flexWrap: 'wrap' }}>
          <label className="field" style={{ minWidth: 200 }}>
            <span>الحالة</span>
            <select className="input" value={status} onChange={(event) => setStatus(event.target.value)}>
              <option value="">الكل</option>
              <option value="pending">بانتظار النشر</option>
              <option value="published">نُشرت</option>
              <option value="dead">ميتة</option>
            </select>
          </label>
          <label className="field" style={{ minWidth: 280 }}>
            <span>معرّف العميل (اختياري)</span>
            <input
              className="input"
              dir="ltr"
              value={onlyTenant}
              onChange={(event) => setOnlyTenant(event.target.value)}
              placeholder="uuid"
            />
          </label>
          <label className="field" style={{ minWidth: 320, flex: 1 }}>
            <span>سبب الفعل (يُسجَّل في التدقيق — 5 محارف على الأقل)</span>
            <input
              className="input"
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              placeholder="مثال: مزوّد البريد عاد للعمل بعد انقطاع"
            />
          </label>
        </div>
        {canManage ? (
          <p className="muted small" style={{ margin: '6px 0 0' }}>
            الإجراءان يظهران على الصفوف المعلَّقة والميتة فقط. المهمّة التي نُفِّذت لا تُعاد: تنفيذُ ما نُفِّذ
            مرّتين قرارُ ناشره لا قرار لوحة المنصة.
          </p>
        ) : (
          <p className="muted small" style={{ margin: '6px 0 0' }}>
            صلاحيتك تسمح بالقراءة فقط (`console.jobs.view`): لا إعادة ولا إلغاء من هنا، ولا يظهران.
          </p>
        )}
      </div>

      {notice && <div className={`alert ${notice.kind === 'ok' ? '' : 'danger'}`}>{notice.text}</div>}

      {jobs.status === 'loading' && <Loading />}
      {jobs.status === 'forbidden' && <Forbidden />}
      {jobs.status === 'error' && <ErrorBox message={jobs.error} onRetry={jobs.reload} />}
      {jobs.status === 'success' &&
        (rows.length === 0 ? (
          <Empty
            title="لا توجد مهام بهذا المرشّح"
            detail="هذا هو الوضع الطبيعي عندما يعمل العامل ولا توجد مهام فاشلة."
          />
        ) : (
          <>
            <p className="muted small">
              {rows.length} من {jobs.data?.meta.total ?? rows.length} مهمّة
            </p>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>الاستحقاق</th>
                    <th>العميل</th>
                    <th>الطابور</th>
                    <th>النوع</th>
                    <th>الحالة</th>
                    <th className="num">المحاولات</th>
                    <th>مفاتيح الحمولة</th>
                    <th>آخر خطأ</th>
                    {canManage && <th className="no-print">إجراء</th>}
                  </tr>
                </thead>
                <tbody>
                  {rows.map((row) => (
                    <tr key={row.id}>
                      <td dir="ltr">{new Date(row.runAt).toLocaleString('ar-SA')}</td>
                      <td dir="ltr">{row.tenantCode ?? row.tenantId.slice(0, 8)}</td>
                      <td dir="ltr">{row.queue}</td>
                      <td dir="ltr">{row.type}</td>
                      <td>
                        <span className={`badge ${STATUS_CLASS[row.status] ?? 'pending'}`}>
                          {STATUS_LABEL[row.status] ?? row.status}
                        </span>
                      </td>
                      <td className="num">{row.attempts}</td>
                      <td dir="ltr" className="small">
                        {row.payloadKeys.length === 0 ? '—' : row.payloadKeys.join(' · ')}
                      </td>
                      <td dir="ltr" className="small">
                        {row.lastError ? row.lastError.slice(0, 80) : '—'}
                      </td>
                      {canManage && (
                        <td className="no-print">
                          <div className="row" style={{ gap: 6 }}>
                            <button
                              className="btn small"
                              type="button"
                              disabled={!reasonReady || busyId === row.id || row.status === 'published'}
                              title={reasonReady ? 'إعادة إلى الطابور' : 'اكتب السبب أولاً'}
                              onClick={() => void act(row, 'retry')}
                            >
                              إعادة
                            </button>
                            <button
                              className="btn small danger"
                              type="button"
                              disabled={!reasonReady || busyId === row.id || row.status === 'published'}
                              title={reasonReady ? 'إلغاء المهمّة' : 'اكتب السبب أولاً'}
                              onClick={() => void act(row, 'cancel')}
                            >
                              إلغاء
                            </button>
                          </div>
                        </td>
                      )}
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

function formatAge(seconds: number): string {
  if (seconds < 60) return `${seconds} ثانية`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)} دقيقة`;
  if (seconds < 86_400) return `${Math.floor(seconds / 3600)} ساعة`;
  return `${Math.floor(seconds / 86_400)} يوم`;
}

function apiMessage(error: unknown): string {
  if (error instanceof Error && error.message.length > 0) return error.message;
  return 'تعذّر تنفيذ الفعل.';
}
