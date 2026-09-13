'use client';

import { useState } from 'react';

import { DataTable, Notice, QueryView } from '../../../../components/data-view';
import { Screen } from '../../../../components/screen';
import { ApiError, apiData, apiPost } from '../../../../lib/api';
import { dateTime, money } from '../../../../lib/lookups';
import { useQuery } from '../../../../lib/use-query';

type Connection = { id: string; storeId: string };
type Product = {
  itemId: string;
  sku: string;
  nameAr: string;
  categoryName: string | null;
  salePrice: string | null;
  showInPos: boolean;
  connectionId: string | null;
  remoteId: string | null;
  status: string | null;
  diffFlags: string[] | null;
  attempts: number | null;
  syncedAt: string | null;
};
type ExportLog = { id: string; itemId: string | null; action: string; status: string; error: string | null; createdAt: string };

const STATUS_LABELS: Record<string, string> = { synced: 'مطابق', changed: 'يحتاج تصدير', pending: 'لم يُصدَّر', failed: 'فشل' };
const FLAG_LABELS: Record<string, string> = { name: 'الاسم', price: 'السعر', cost: 'التكلفة', qty: 'الكمية' };

/** Products screen: what the store knows about each item, and what still needs pushing. */
export default function SallaProductsPage() {
  const products = useQuery<Product[]>(() => apiData<Product[]>('/integrations/salla/products'), []);
  const connections = useQuery<Connection[]>(() => apiData<Connection[]>('/integrations/salla/connections'), []);
  const logs = useQuery<ExportLog[]>(() => apiData<ExportLog[]>('/integrations/salla/export-log'), []);
  const [connectionId, setConnectionId] = useState('');
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger' | 'info'; text: string } | undefined>();

  const connectionRows = connections.data ?? [];
  const activeConnection = connectionId || connectionRows[0]?.id || '';

  async function run(action: () => Promise<unknown>, okText: string) {
    setBusy(true);
    setNotice(undefined);
    try {
      await action();
      setNotice({ kind: 'ok', text: okText });
      products.reload();
      logs.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  return (
    <Screen
      title="منتجات متجر سلة"
      subtitle="حالة كل صنف مقابل المتجر الإلكتروني: ما تغيّر محلياً، وما تم تصديره فعلاً."
      crumbs={['المستودعات', 'متجر سلة']}
      actions={
        <button
          type="button"
          className="btn primary"
          disabled={busy || !activeConnection}
          onClick={() => run(() => apiPost('/integrations/salla/export-next', {}), 'تم تنفيذ أول عملية في طابور التصدير.')}
        >
          تنفيذ التصدير التالي
        </button>
      }
    >
      <div className="card toolbar">
        <label className="field">
          <span>المتجر</span>
          <select className="input" value={activeConnection} onChange={(event) => setConnectionId(event.target.value)}>
            {connectionRows.length === 0 ? <option value="">لا يوجد متجر مرتبط</option> : null}
            {connectionRows.map((row) => (
              <option key={row.id} value={row.id}>
                {row.storeId}
              </option>
            ))}
          </select>
        </label>
        <span className="muted small">أضف الصنف إلى الطابور، ثم نفّذ التصدير — كل محاولة تُسجَّل في سجل التصدير أسفل الشاشة.</span>
      </div>
      <Notice notice={notice} />

      <QueryView query={products} empty="لا توجد أصناف" emptyDetail="أضف أصنافاً من دليل المواد أولاً.">
        {(rows) => (
          <div className="card">
            <DataTable
              columns={[
                { key: 'sku', header: 'الرمز', align: 'ltr', cell: (row: Product) => row.sku },
                { key: 'name', header: 'الصنف', cell: (row: Product) => row.nameAr },
                { key: 'category', header: 'المجموعة', cell: (row: Product) => row.categoryName ?? '—' },
                { key: 'price', header: 'سعر البيع', align: 'num', cell: (row: Product) => money(row.salePrice) },
                { key: 'status', header: 'حالة المزامنة', cell: (row: Product) => STATUS_LABELS[row.status ?? 'pending'] ?? row.status },
                {
                  key: 'diff',
                  header: 'التغييرات',
                  cell: (row: Product) => (row.diffFlags?.length ? row.diffFlags.map((flag) => FLAG_LABELS[flag] ?? flag).join('، ') : '—'),
                },
                { key: 'synced', header: 'آخر مزامنة', cell: (row: Product) => dateTime(row.syncedAt) },
                {
                  key: 'actions',
                  header: '',
                  cell: (row: Product) => (
                    <button
                      type="button"
                      className="btn sm"
                      disabled={busy || !activeConnection}
                      onClick={() => run(() => apiPost('/integrations/salla/export-queue', { connectionId: activeConnection, itemId: row.itemId, action: 'update' }), `تمت إضافة ${row.nameAr} إلى طابور التصدير.`)}
                    >
                      أضف للطابور
                    </button>
                  ),
                },
              ]}
              rows={rows}
              rowKey={(row) => row.itemId}
            />
          </div>
        )}
      </QueryView>

      <div className="card">
        <h3>سجل التصدير</h3>
        <QueryView query={logs} empty="لا توجد عمليات تصدير بعد">
          {(rows) => (
            <DataTable
              columns={[
                { key: 'created', header: 'الوقت', cell: (row: ExportLog) => dateTime(row.createdAt) },
                { key: 'action', header: 'العملية', cell: (row: ExportLog) => row.action },
                { key: 'status', header: 'الحالة', cell: (row: ExportLog) => (row.status === 'sent' ? 'تم الإرسال' : row.status === 'queued' ? 'في الطابور' : row.status) },
                { key: 'error', header: 'الخطأ', cell: (row: ExportLog) => row.error ?? '—' },
              ]}
              rows={rows}
              rowKey={(row) => row.id}
            />
          )}
        </QueryView>
      </div>
    </Screen>
  );
}
