'use client';

import { useMemo, useState } from 'react';

import { Empty, ErrorBox, Forbidden, Loading, Screen } from '../../../components/screen';
import { ApiError, apiData, apiPost } from '../../../lib/api';
import { ACCOUNT_TYPE_LABELS, downloadCsv, nameOf, postableOf, typeOf, type Account } from '../../../lib/accounts';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

export default function ChartOfAccountsPage() {
  const { can } = useSession();
  const accounts = useQuery<Account[]>(() => apiData<Account[]>('/accounts'), []);
  const [search, setSearch] = useState('');
  const [type, setType] = useState('');
  const [creating, setCreating] = useState(false);

  const rows = useMemo(() => {
    const list = accounts.data ?? [];
    const needle = search.trim().toLowerCase();
    return list
      .filter((account) => (type ? typeOf(account) === type : true))
      .filter((account) =>
        needle.length === 0
          ? true
          : account.code.toLowerCase().includes(needle) || nameOf(account).toLowerCase().includes(needle),
      )
      .sort((left, right) => left.code.localeCompare(right.code));
  }, [accounts.data, search, type]);

  return (
    <Screen
      title="دليل الحسابات"
      subtitle="كل حسابات المنشأة مع النوع وقابلية الترحيل."
      crumbs={['المحاسبة', 'تعاريف']}
      actions={
        <>
          {can('accounting.account.manage') && (
            <button className="btn primary" type="button" onClick={() => setCreating(!creating)}>
              {creating ? 'إغلاق النموذج' : 'حساب جديد'}
            </button>
          )}
          <button className="btn" type="button" onClick={() => window.print()}>
            طباعة
          </button>
          <button
            className="btn"
            type="button"
            onClick={() =>
              downloadCsv(
                'chart-of-accounts.csv',
                ['الرمز', 'الاسم', 'النوع', 'ترحيل'],
                rows.map((row) => [row.code, nameOf(row), typeOf(row), postableOf(row) ? 'نعم' : 'لا']),
              )
            }
          >
            تصدير CSV
          </button>
        </>
      }
    >
      {creating && (
        <NewAccountForm
          accounts={accounts.data ?? []}
          onDone={() => {
            setCreating(false);
            accounts.reload();
          }}
        />
      )}

      <div className="card tight">
        <div className="row">
          <input
            className="input"
            style={{ maxWidth: 260 }}
            placeholder="بحث بالرمز أو الاسم"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
          <select className="input" style={{ maxWidth: 180 }} value={type} onChange={(event) => setType(event.target.value)}>
            <option value="">كل الأنواع</option>
            {Object.entries(ACCOUNT_TYPE_LABELS).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
          <span className="muted" style={{ alignSelf: 'center' }}>
            {rows.length} حساب
          </span>
        </div>
      </div>

      {accounts.status === 'loading' && <Loading />}
      {accounts.status === 'forbidden' && <Forbidden />}
      {accounts.status === 'error' && <ErrorBox message={accounts.error} onRetry={accounts.reload} />}
      {accounts.status === 'success' &&
        (rows.length === 0 ? (
          <Empty title="لا توجد حسابات" detail="ابدأ بإنشاء دليل الحسابات أو استوردها من النظام القديم عبر أداة الهجرة." />
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>الرمز</th>
                  <th>الاسم</th>
                  <th>النوع</th>
                  <th>العملة</th>
                  <th>ترحيل مباشر</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((account) => (
                  <tr key={account.id}>
                    <td dir="ltr">{account.code}</td>
                    <td>{nameOf(account)}</td>
                    <td>{ACCOUNT_TYPE_LABELS[typeOf(account)] ?? typeOf(account)}</td>
                    <td dir="ltr">{account.currencyCode ?? '—'}</td>
                    <td>{postableOf(account) ? 'نعم' : 'لا (حساب تجميعي)'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}
    </Screen>
  );
}

function NewAccountForm({ accounts, onDone }: { accounts: Account[]; onDone: () => void }) {
  const [code, setCode] = useState('');
  const [nameAr, setNameAr] = useState('');
  const [nameEn, setNameEn] = useState('');
  const [type, setType] = useState('asset');
  const [parentId, setParentId] = useState('');
  const [isPostable, setIsPostable] = useState(true);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setMessage(undefined);
    try {
      await apiPost('/accounts', {
        code: code.trim(),
        nameAr: nameAr.trim(),
        nameEn: nameEn.trim() || undefined,
        type,
        parentId: parentId || undefined,
        isPostable,
      });
      setMessage({ kind: 'ok', text: 'تم إنشاء الحساب.' });
      setCode('');
      setNameAr('');
      setNameEn('');
      onDone();
    } catch (error) {
      setMessage({
        kind: 'danger',
        text: error instanceof ApiError ? `${error.message}${error.detail ? ` — ${error.detail}` : ''}` : String(error),
      });
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="card" onSubmit={submit}>
      <h2>حساب جديد</h2>
      <div className="form-grid">
        <label className="field">
          <span>الرمز *</span>
          <input className="input" dir="ltr" value={code} onChange={(event) => setCode(event.target.value)} required />
        </label>
        <label className="field">
          <span>الاسم بالعربية *</span>
          <input className="input" value={nameAr} onChange={(event) => setNameAr(event.target.value)} required />
        </label>
        <label className="field">
          <span>الاسم بالإنجليزية</span>
          <input className="input" dir="ltr" value={nameEn} onChange={(event) => setNameEn(event.target.value)} />
        </label>
        <label className="field">
          <span>النوع *</span>
          <select className="input" value={type} onChange={(event) => setType(event.target.value)}>
            {Object.entries(ACCOUNT_TYPE_LABELS).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>الحساب الأب</span>
          <select className="input" value={parentId} onChange={(event) => setParentId(event.target.value)}>
            <option value="">— بدون (حساب رئيسي)</option>
            {accounts.map((account) => (
              <option key={account.id} value={account.id}>
                {account.code} — {nameOf(account)}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>قابل للترحيل</span>
          <select
            className="input"
            value={isPostable ? '1' : '0'}
            onChange={(event) => setIsPostable(event.target.value === '1')}
          >
            <option value="1">نعم — تُرحّل عليه القيود</option>
            <option value="0">لا — حساب تجميعي</option>
          </select>
        </label>
      </div>
      {message && <p className={`alert ${message.kind}`}>{message.text}</p>}
      <div className="toolbar">
        <button className="btn primary" type="submit" disabled={busy}>
          {busy ? 'جارٍ الحفظ…' : 'حفظ'}
        </button>
      </div>
    </form>
  );
}
