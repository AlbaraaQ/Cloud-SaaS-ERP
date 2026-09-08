'use client';

import { useState } from 'react';

import { Empty, ErrorBox, Forbidden, Loading, Screen } from '../../../components/screen';
import { ApiError, apiData, apiPost } from '../../../lib/api';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type CostCenter = { id: string; code: string; nameAr?: string; name_ar?: string; nameEn?: string | null; parentId?: string | null };

export default function CostCentersPage() {
  const { can } = useSession();
  const centers = useQuery<CostCenter[]>(() => apiData<CostCenter[]>('/cost-centers'), []);
  const [creating, setCreating] = useState(false);
  const [code, setCode] = useState('');
  const [nameAr, setNameAr] = useState('');
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setMessage(undefined);
    try {
      await apiPost('/cost-centers', { code: code.trim(), nameAr: nameAr.trim() });
      setMessage({ kind: 'ok', text: 'تم إنشاء مركز التكلفة.' });
      setCode('');
      setNameAr('');
      centers.reload();
    } catch (error) {
      setMessage({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  return (
    <Screen
      title="مراكز التكلفة"
      subtitle="تُستخدم لتوزيع المصروفات والإيرادات على الأنشطة والفروع."
      crumbs={['المحاسبة', 'تعاريف']}
      actions={
        can('accounting.account.manage') ? (
          <button className="btn primary" type="button" onClick={() => setCreating(!creating)}>
            {creating ? 'إغلاق' : 'مركز تكلفة جديد'}
          </button>
        ) : null
      }
    >
      {creating && (
        <form className="card" onSubmit={submit}>
          <h2>مركز تكلفة جديد</h2>
          <div className="form-grid">
            <label className="field">
              <span>الرمز *</span>
              <input className="input" dir="ltr" value={code} onChange={(event) => setCode(event.target.value)} required />
            </label>
            <label className="field">
              <span>الاسم *</span>
              <input className="input" value={nameAr} onChange={(event) => setNameAr(event.target.value)} required />
            </label>
          </div>
          {message && <p className={`alert ${message.kind}`}>{message.text}</p>}
          <button className="btn primary" type="submit" disabled={busy}>
            {busy ? 'جارٍ الحفظ…' : 'حفظ'}
          </button>
        </form>
      )}

      {centers.status === 'loading' && <Loading />}
      {centers.status === 'forbidden' && <Forbidden />}
      {centers.status === 'error' && <ErrorBox message={centers.error} onRetry={centers.reload} />}
      {centers.status === 'success' &&
        ((centers.data ?? []).length === 0 ? (
          <Empty title="لا توجد مراكز تكلفة" />
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>الرمز</th>
                  <th>الاسم</th>
                </tr>
              </thead>
              <tbody>
                {(centers.data ?? []).map((center) => (
                  <tr key={center.id}>
                    <td dir="ltr">{center.code}</td>
                    <td>{center.nameAr ?? center.name_ar}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}
    </Screen>
  );
}
