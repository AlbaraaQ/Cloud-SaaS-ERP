'use client';

import { useState } from 'react';

import { DataTable, Notice, QueryView } from '../../../../components/data-view';
import { Screen } from '../../../../components/screen';
import { ApiError, apiFetch, apiPost } from '../../../../lib/api';
import { dateTime } from '../../../../lib/lookups';
import { useQuery } from '../../../../lib/use-query';

type Submission = {
  id: string;
  invoiceId: string;
  authority: string;
  environment: string;
  status: string;
  uuid: string | null;
  hash: string | null;
  attempts: string;
  error: string | null;
  submittedAt: string | null;
  createdAt: string;
};

const STATUS_LABELS: Record<string, string> = {
  reported: 'مُبلَّغ',
  cleared: 'مُصادق',
  rejected: 'مرفوض',
  failed: 'فشل',
  pending: 'قيد الإرسال',
  not_implemented: 'غير مدعوم',
};

/** Every invoice sent to the authority, with the hash chain and a retry for failures. */
export default function ZatcaSyncPage() {
  const [status, setStatus] = useState('');
  const submissions = useQuery<Submission[]>(() => apiFetch<Submission[]>(`/einvoice/submissions${status ? `?status=${status}` : ''}`), [status]);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger' | 'info'; text: string } | undefined>();

  async function retry(row: Submission) {
    setBusy(true);
    setNotice(undefined);
    try {
      await apiPost(`/einvoice/submissions/${row.id}/retry`, {});
      setNotice({ kind: 'ok', text: 'أُعيد إرسال الفاتورة.' });
      submissions.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  return (
    <Screen
      title="مزامنة الفواتير مع هيئة الزكاة والضريبة"
      subtitle="حالة كل فاتورة أُرسلت للهيئة، وسلسلة التجزئة (hash chain) التي تربطها بسابقتها."
      crumbs={['الإعدادات', 'المزامنة']}
    >
      <div className="card toolbar">
        <label className="field">
          <span>الحالة</span>
          <select className="input" value={status} onChange={(event) => setStatus(event.target.value)}>
            <option value="">الكل</option>
            <option value="reported">مُبلَّغ</option>
            <option value="cleared">مُصادق</option>
            <option value="rejected">مرفوض</option>
            <option value="failed">فشل</option>
          </select>
        </label>
        <span className="muted small">الإرسال يتم من شاشة الفاتورة بعد ترحيلها؛ هذه الشاشة للمتابعة وإعادة المحاولة.</span>
      </div>
      <Notice notice={notice} />

      <QueryView query={submissions} empty="لا توجد فواتير مُرسَلة" emptyDetail="رحّل فاتورة ثم أرسلها للهيئة لتظهر هنا.">
        {(rows) => (
          <div className="card">
            <DataTable
              columns={[
                { key: 'created', header: 'التاريخ', cell: (row: Submission) => dateTime(row.submittedAt ?? row.createdAt) },
                { key: 'environment', header: 'البيئة', cell: (row: Submission) => (row.environment === 'production' ? 'إنتاج' : 'تجريبية') },
                { key: 'status', header: 'الحالة', cell: (row: Submission) => STATUS_LABELS[row.status] ?? row.status },
                { key: 'uuid', header: 'UUID', align: 'ltr', cell: (row: Submission) => (row.uuid ? row.uuid.slice(0, 13) + '…' : '—') },
                { key: 'hash', header: 'التجزئة', align: 'ltr', cell: (row: Submission) => (row.hash ? row.hash.slice(0, 12) + '…' : '—') },
                { key: 'attempts', header: 'المحاولات', align: 'num', cell: (row: Submission) => row.attempts },
                { key: 'error', header: 'الخطأ', cell: (row: Submission) => row.error ?? '—' },
                {
                  key: 'actions',
                  header: '',
                  cell: (row: Submission) =>
                    row.status === 'not_implemented' ? (
                      <span className="muted">—</span>
                    ) : (
                      <button type="button" className="btn sm" disabled={busy} onClick={() => retry(row)}>
                        إعادة إرسال
                      </button>
                    ),
                },
              ]}
              rows={rows}
              rowKey={(row) => row.id}
            />
          </div>
        )}
      </QueryView>
    </Screen>
  );
}
