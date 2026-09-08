'use client';

import { useMemo, useState } from 'react';

import { Empty, ErrorBox, Forbidden, Loading, Screen } from '../../../components/screen';
import { ApiError, apiData, apiDelete, apiPatch, apiPost } from '../../../lib/api';
import { ACCOUNT_TYPE_LABELS, downloadCsv, nameOf, parentOf, postableOf, typeOf, type Account } from '../../../lib/accounts';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

export default function ChartOfAccountsPage() {
  const { can } = useSession();
  const accounts = useQuery<Account[]>(() => apiData<Account[]>('/accounts'), []);
  const [search, setSearch] = useState('');
  const [type, setType] = useState('');
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<Account | undefined>();
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();

  function startEdit(account: Account) {
    setEditing(account);
    setCreating(true);
    setNotice(undefined);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  async function remove(account: Account) {
    if (!window.confirm(`هل تريد حذف الحساب ${account.code} — ${nameOf(account)}؟`)) return;
    setNotice(undefined);
    try {
      await apiDelete(`/accounts/${account.id}`);
      setNotice({ kind: 'ok', text: `تم حذف الحساب ${account.code}.` });
      accounts.reload();
    } catch (error) {
      // The API refuses to delete an account that carries entries or sub-accounts, and
      // says which of the two it is — that message is more useful than a generic failure.
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    }
  }

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
            <button
              className="btn primary"
              type="button"
              onClick={() => {
                setEditing(undefined);
                setCreating(!creating);
              }}
            >
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
        <AccountForm
          key={editing?.id ?? 'new'}
          accounts={accounts.data ?? []}
          editing={editing}
          onCancel={() => {
            setEditing(undefined);
            setCreating(false);
          }}
          onDone={() => {
            setEditing(undefined);
            setCreating(false);
            accounts.reload();
          }}
        />
      )}

      {notice && <p className={`alert ${notice.kind}`}>{notice.text}</p>}

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
                  {can('accounting.account.manage') && <th />}
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
                    {can('accounting.account.manage') && (
                      <td>
                        <span className="row">
                          <button className="btn sm" type="button" onClick={() => startEdit(account)}>
                            تعديل
                          </button>
                          <button className="btn sm danger" type="button" onClick={() => void remove(account)}>
                            حذف
                          </button>
                        </span>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}
    </Screen>
  );
}

/**
 * One form for both creating and correcting an account. In edit mode the API still has
 * the final word: the number, nature and side of an account that already carries journal
 * entries are refused server-side, and the refusal is shown here as-is.
 */
function AccountForm({
  accounts,
  editing,
  onDone,
  onCancel,
}: {
  accounts: Account[];
  editing?: Account;
  onDone: () => void;
  onCancel: () => void;
}) {
  const [code, setCode] = useState(editing?.code ?? '');
  const [nameAr, setNameAr] = useState(editing ? nameOf(editing) : '');
  const [nameEn, setNameEn] = useState(editing?.nameEn ?? editing?.name_en ?? '');
  const [type, setType] = useState(editing ? typeOf(editing) || 'asset' : 'asset');
  const [parentId, setParentId] = useState(editing ? (parentOf(editing) ?? '') : '');
  const [isPostable, setIsPostable] = useState(editing ? postableOf(editing) : true);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setMessage(undefined);
    const payload = {
      code: code.trim(),
      nameAr: nameAr.trim(),
      nameEn: nameEn.trim() || undefined,
      type,
      parentId: parentId || undefined,
      isPostable,
    };
    try {
      if (editing) {
        await apiPatch(`/accounts/${editing.id}`, { ...payload, parentId: parentId || null });
        setMessage({ kind: 'ok', text: 'تم حفظ التعديل.' });
      } else {
        await apiPost('/accounts', payload);
        setMessage({ kind: 'ok', text: 'تم إنشاء الحساب.' });
        setCode('');
        setNameAr('');
        setNameEn('');
      }
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
      <h2>{editing ? `تعديل الحساب ${editing.code}` : 'حساب جديد'}</h2>
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
            {accounts
              .filter((account) => account.id !== editing?.id)
              .map((account) => (
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
          {busy ? 'جارٍ الحفظ…' : editing ? 'حفظ التعديل' : 'حفظ'}
        </button>
        <button className="btn" type="button" onClick={onCancel} disabled={busy}>
          إلغاء
        </button>
      </div>
    </form>
  );
}
