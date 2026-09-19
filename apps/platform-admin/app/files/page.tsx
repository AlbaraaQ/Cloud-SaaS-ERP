'use client';

import { useState } from 'react';

import { Empty, ErrorBox, Forbidden, Loading, Screen } from '../../components/screen';
import { apiData } from '../../lib/api';
import { useSession } from '../../lib/session';
import { useQuery } from '../../lib/use-query';

/**
 * الملفات — the cross-tenant file manager (P-C9).
 *
 * السؤال الذي تجيب عنه هذه الشاشة ليس «كم ملفاً عندنا» بل **«أيُّ ملفٍّ لم يُفحص؟»**.
 * وحكم الفحص لا يُخترع هنا: هو `meta.scan` الذي يكتبه مسار الرفع في مسار التدقيق
 * (`FilesService.finalize` عبر منفذ `VirusScanner`)، وهذه الشاشة تقرؤه وتعرضه كما هو.
 *
 * ولهذا يقول العمود **«لم يُفحص»** للملفّ الذي `scan = null`، ولا يقول «نظيف»: الماسح
 * المربوط في هذا المستودع لا يفعل شيئاً فيُعيد `skipped`، وترجمةُ `skipped` إلى «سليم»
 * هي بالضبط الكذبة التي تمنع ظهور الحاجة إلى ماسحٍ حقيقي.
 */

type FileRow = {
  id: string;
  tenantId: string;
  tenantCode: string | null;
  name: string;
  mime: string;
  sizeBytes: number;
  status: 'pending' | 'ready' | 'deleted';
  entity: string | null;
  entityId: string | null;
  uploadedByLabel: string | null;
  createdAt: string;
  deletedAt: string | null;
  scan: {
    verdict: 'clean' | 'infected' | 'skipped';
    scanner: string;
    detail: string | null;
    recordedAt: string;
  } | null;
};

type FilePage = { data: FileRow[]; meta: { total: number; limit: number; offset: number } };

type ScanResult = {
  fileId: string;
  verdict: string;
  scanner: string;
  detail: string | null;
  scannedAt: string;
};

const STATUS_LABEL: Record<string, string> = {
  pending: 'بانتظار الرفع',
  ready: 'جاهز',
  deleted: 'محجور',
};

const STATUS_CLASS: Record<string, string> = {
  pending: 'pending',
  ready: 'active',
  deleted: 'failed',
};

const SCAN_LABEL: Record<string, string> = {
  clean: 'نظيف',
  infected: 'مُصاب',
  skipped: 'لم يُفحص فعلياً',
};

const SCAN_CLASS: Record<string, string> = {
  clean: 'active',
  infected: 'failed',
  skipped: 'pending',
};

export default function FilesPage() {
  const { canConsole } = useSession();
  const [status, setStatus] = useState('');
  const [scan, setScan] = useState('');
  const [term, setTerm] = useState('');
  const [reason, setReason] = useState('');
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>(undefined);
  const [busyId, setBusyId] = useState<string | null>(null);

  const files = useQuery<FilePage>(() => {
    const params = new URLSearchParams({ limit: '100' });
    if (status) params.set('filter[status]', status);
    if (scan) params.set('filter[scan]', scan);
    if (term.trim()) params.set('q', term.trim());
    return apiData<FilePage>(`/platform/files?${params.toString()}`);
  }, [status, scan, term]);

  const rows = files.data?.data ?? [];
  const canManage = canConsole('console.jobs.manage');
  const reasonReady = reason.trim().length >= 5;

  async function scanNow(row: FileRow) {
    setBusyId(row.id);
    setNotice(undefined);
    try {
      const result = await apiData<ScanResult>(`/platform/files/${row.id}/scan`, {
        method: 'POST',
        body: JSON.stringify({}),
      });
      setNotice({
        kind: result.verdict === 'infected' ? 'danger' : 'ok',
        text: `الفحص الآن: ${SCAN_LABEL[result.verdict] ?? result.verdict} — الماسح «${result.scanner}».`,
      });
      files.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: apiMessage(error) });
    } finally {
      setBusyId(null);
    }
  }

  async function quarantine(row: FileRow) {
    if (!window.confirm(`حجر الملف «${row.name}»؟ يمنع تنزيله وتبقى ميتاداته دليلاً.`)) return;
    setBusyId(row.id);
    setNotice(undefined);
    try {
      await apiData<FileRow>(`/platform/files/${row.id}`, {
        method: 'DELETE',
        body: JSON.stringify({ reason: reason.trim() }),
      });
      setNotice({ kind: 'ok', text: `حُجر «${row.name}» — الميتاداتا باقية والسبب مسجَّل.` });
      files.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: apiMessage(error) });
    } finally {
      setBusyId(null);
    }
  }

  return (
    <Screen
      title="الملفات"
      subtitle="مدير ملفات عبر كل العملاء: الحالة، والارتباط بكيان، وحكم آخر فحص — وحكم الفحص مقروءٌ من مسار التدقيق لا محسوبٌ في الشاشة."
      crumbs={['المنصة', 'التشغيل']}
      actions={
        <button className="btn" type="button" onClick={files.reload}>
          تحديث
        </button>
      }
    >
      <div className="card tight no-print">
        <div className="row" style={{ flexWrap: 'wrap' }}>
          <label className="field" style={{ minWidth: 220, flex: 1 }}>
            <span>بحث في الاسم</span>
            <input
              className="input"
              value={term}
              onChange={(event) => setTerm(event.target.value)}
              placeholder="كشف"
            />
          </label>
          <label className="field" style={{ minWidth: 180 }}>
            <span>حالة الملف</span>
            <select className="input" value={status} onChange={(event) => setStatus(event.target.value)}>
              <option value="">الكل</option>
              <option value="pending">بانتظار الرفع</option>
              <option value="ready">جاهز</option>
              <option value="deleted">محجور</option>
            </select>
          </label>
          <label className="field" style={{ minWidth: 200 }}>
            <span>حكم الفحص</span>
            <select className="input" value={scan} onChange={(event) => setScan(event.target.value)}>
              <option value="">الكل</option>
              <option value="clean">نظيف</option>
              <option value="infected">مُصاب</option>
              <option value="skipped">لم يُفحص فعلياً</option>
              <option value="none">لم يُفحص بعد</option>
            </select>
          </label>
          <label className="field" style={{ minWidth: 300, flex: 1 }}>
            <span>سبب الحجر (5 محارف على الأقل — يُسجَّل في التدقيق)</span>
            <input
              className="input"
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              placeholder="مثال: محتوى مشتبه به أبلغ عنه العميل"
            />
          </label>
        </div>
        <p className="muted small" style={{ margin: '6px 0 0' }}>
          «لم يُفحص» و«لم يُفحص فعلياً» مختلفان: الأول ملفٌّ لم يمرّ على الماسح أصلاً (لا سطر تدقيق)، والثاني
          مرّ عليه فقال الماسح إنه لا يفحص (الماسح المُهيّأ هنا لا يفعل).
        </p>
      </div>

      {notice && <div className={`alert ${notice.kind === 'ok' ? '' : 'danger'}`}>{notice.text}</div>}

      {files.status === 'loading' && <Loading />}
      {files.status === 'forbidden' && <Forbidden />}
      {files.status === 'error' && <ErrorBox message={files.error} onRetry={files.reload} />}
      {files.status === 'success' &&
        (rows.length === 0 ? (
          <Empty title="لا ملفات بهذا المرشّح" detail="جرّب مرشّح «لم يُفحص بعد» — هو سؤال هذه الشاشة." />
        ) : (
          <>
            <p className="muted small">
              {rows.length} من {files.data?.meta.total ?? rows.length} ملفاً
            </p>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>الملف</th>
                    <th>العميل</th>
                    <th>الحجم</th>
                    <th>الحالة</th>
                    <th>مربوط بـ</th>
                    <th>حكم الفحص</th>
                    <th>رُفع في</th>
                    {canManage && <th className="no-print">إجراء</th>}
                  </tr>
                </thead>
                <tbody>
                  {rows.map((row) => (
                    <tr key={row.id}>
                      <td>
                        <div dir="ltr">{row.name}</div>
                        <div className="muted small" dir="ltr">
                          {row.mime} · {row.uploadedByLabel ?? 'بلا رافع'}
                        </div>
                      </td>
                      <td dir="ltr">{row.tenantCode ?? row.tenantId.slice(0, 8)}</td>
                      <td className="num">{formatBytes(row.sizeBytes)}</td>
                      <td>
                        <span className={`badge ${STATUS_CLASS[row.status] ?? 'pending'}`}>
                          {STATUS_LABEL[row.status] ?? row.status}
                        </span>
                      </td>
                      <td dir="ltr" className="small">
                        {row.entity ? `${row.entity}/${row.entityId?.slice(0, 8) ?? ''}` : '—'}
                      </td>
                      <td>
                        {row.scan ? (
                          <>
                            <span className={`badge ${SCAN_CLASS[row.scan.verdict] ?? 'pending'}`}>
                              {SCAN_LABEL[row.scan.verdict] ?? row.scan.verdict}
                            </span>
                            <div className="muted small" dir="ltr">
                              {row.scan.scanner} · {new Date(row.scan.recordedAt).toLocaleString('ar-SA')}
                            </div>
                          </>
                        ) : (
                          <span className="badge">لم يُفحص</span>
                        )}
                      </td>
                      <td dir="ltr">{new Date(row.createdAt).toLocaleString('ar-SA')}</td>
                      {canManage && (
                        <td className="no-print">
                          <div className="row" style={{ gap: 6 }}>
                            <button
                              className="btn small"
                              type="button"
                              disabled={busyId === row.id}
                              onClick={() => void scanNow(row)}
                            >
                              افحص الآن
                            </button>
                            <button
                              className="btn small danger"
                              type="button"
                              disabled={busyId === row.id || row.status === 'deleted' || !reasonReady}
                              title={reasonReady ? 'حجر الملف' : 'اكتب السبب أولاً'}
                              onClick={() => void quarantine(row)}
                            >
                              حجر
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

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} بايت`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} ك.ب`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} م.ب`;
}

function apiMessage(error: unknown): string {
  if (error instanceof Error && error.message.length > 0) return error.message;
  return 'تعذّر تنفيذ الفعل.';
}
