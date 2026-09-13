'use client';

import { useState } from 'react';

import { DataTable, Notice, QueryView } from '../../../components/data-view';
import { FormFields, type FormValues } from '../../../components/directory';
import { Screen } from '../../../components/screen';
import { ApiError, apiFetch } from '../../../lib/api';
import { dateTime } from '../../../lib/lookups';
import { useQuery } from '../../../lib/use-query';

type Credential = {
  id: string;
  authority: string;
  environment: string;
  csr: string | null;
  privateKeyMasked: string | null;
  csidMasked: string | null;
  secretMasked: string | null;
  validFrom: string | null;
  validTo: string | null;
  updatedAt: string | null;
};

type Health = { status: string; credentials: number };

/**
 * ZATCA onboarding credentials.
 *
 * Secrets are encrypted at rest and only ever returned masked, so this screen writes
 * them and reports their presence — it never displays them again.
 */
export default function ZatcaSettingsPage() {
  const credentials = useQuery<Credential[]>(() => apiFetch<Credential[]>('/einvoice/credentials'), []);
  const health = useQuery<Health>(() => apiFetch<Health>('/einvoice/health'), []);
  const [values, setValues] = useState<FormValues>({ environment: 'simulation', csr: '', privateKey: '', csid: '', secret: '', validFrom: '', validTo: '' });
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger' | 'info'; text: string } | undefined>();

  async function save(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setNotice(undefined);
    try {
      await apiFetch('/einvoice/credentials', {
        method: 'PUT',
        body: JSON.stringify({
          authority: 'zatca',
          environment: String(values.environment || 'simulation'),
          csr: values.csr ? String(values.csr) : undefined,
          privateKey: values.privateKey ? String(values.privateKey) : undefined,
          csid: values.csid ? String(values.csid) : undefined,
          secret: values.secret ? String(values.secret) : undefined,
          validFrom: values.validFrom ? new Date(`${String(values.validFrom)}T00:00:00Z`).toISOString() : undefined,
          validTo: values.validTo ? new Date(`${String(values.validTo)}T00:00:00Z`).toISOString() : undefined,
        }),
      });
      setNotice({ kind: 'ok', text: 'تم حفظ بيانات الاعتماد مشفّرة.' });
      setValues({ ...values, privateKey: '', csid: '', secret: '' });
      credentials.reload();
      health.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  return (
    <Screen
      title="إعدادات الربط مع هيئة الزكاة والضريبة"
      subtitle="بيانات اعتماد الفوترة الإلكترونية (ZATCA) لكل بيئة: التجريبية ثم الإنتاج."
      crumbs={['الإعدادات', 'المنشأة']}
      actions={<span className="chip">بيانات اعتماد مسجّلة: {health.data?.credentials ?? 0}</span>}
    >
      <form className="card" onSubmit={save}>
        <h3>حفظ بيانات الاعتماد</h3>
        <FormFields
          fields={[
            { name: 'environment', label: 'البيئة', type: 'select', required: true, options: [{ id: 'simulation', label: 'تجريبية (Simulation)' }, { id: 'production', label: 'إنتاج (Production)' }] },
            { name: 'csid', label: 'CSID', ltr: true },
            { name: 'secret', label: 'Secret', ltr: true },
            { name: 'csr', label: 'CSR', type: 'textarea', ltr: true, wide: true },
            { name: 'privateKey', label: 'المفتاح الخاص', type: 'textarea', ltr: true, wide: true, hint: 'يُخزَّن مشفّراً بـ AES-256-GCM ولا يُعرض مرة أخرى.' },
            { name: 'validFrom', label: 'صالح من', type: 'date' },
            { name: 'validTo', label: 'صالح حتى', type: 'date' },
          ]}
          values={values}
          onChange={setValues}
        />
        <Notice notice={notice} />
        <button className="btn primary" type="submit" disabled={busy}>
          {busy ? 'جارٍ الحفظ…' : 'حفظ'}
        </button>
      </form>

      <QueryView query={credentials} empty="لم تُسجَّل بيانات اعتماد بعد" emptyDetail="ابدأ بالبيئة التجريبية قبل طلب شهادة الإنتاج.">
        {(rows) => (
          <div className="card">
            <DataTable
              columns={[
                { key: 'authority', header: 'الجهة', align: 'ltr', cell: (row: Credential) => row.authority.toUpperCase() },
                { key: 'environment', header: 'البيئة', cell: (row: Credential) => (row.environment === 'production' ? 'إنتاج' : 'تجريبية') },
                { key: 'csid', header: 'CSID', cell: (row: Credential) => row.csidMasked ?? '—' },
                { key: 'secret', header: 'Secret', cell: (row: Credential) => row.secretMasked ?? '—' },
                { key: 'key', header: 'المفتاح الخاص', cell: (row: Credential) => row.privateKeyMasked ?? '—' },
                { key: 'validTo', header: 'صالح حتى', cell: (row: Credential) => dateTime(row.validTo) },
                { key: 'updated', header: 'آخر تحديث', cell: (row: Credential) => dateTime(row.updatedAt) },
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
