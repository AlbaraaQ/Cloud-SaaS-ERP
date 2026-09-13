'use client';

import { useState } from 'react';

import { Empty, ErrorBox, Loading, Screen } from '../../components/screen';
import { ApiError, apiData, apiDelete, apiPost } from '../../lib/api';
import { useQuery } from '../../lib/use-query';

type PlatformRole = {
  code: string;
  name: string;
  nameAr: string;
  description: string;
  permissions: string[];
  holderCount: number;
};

type DirectoryUser = {
  id: string;
  email: string;
  full_name: string;
  is_platform_admin: boolean;
  platform_roles: string[];
};

export default function PlatformRolesPage() {
  const roles = useQuery<PlatformRole[]>(() => apiData<PlatformRole[]>('/platform/roles'));
  const [search, setSearch] = useState('');
  const [applied, setApplied] = useState('');
  const users = useQuery<DirectoryUser[]>(
    () => apiData<DirectoryUser[]>(`/platform/users${applied ? `?search=${encodeURIComponent(applied)}` : ''}`),
    [applied],
  );
  const [busy, setBusy] = useState<string | undefined>();
  const [error, setError] = useState<string | undefined>();

  async function mutate(userId: string, roleCode: string, grant: boolean) {
    const key = `${userId}:${roleCode}`;
    setBusy(key);
    setError(undefined);
    try {
      if (grant) {
        await apiPost(`/platform/users/${userId}/roles`, { roleCode });
      } else {
        await apiDelete(`/platform/users/${userId}/roles/${roleCode}`);
      }
      await Promise.all([users.reload(), roles.reload()]);
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'تعذر تنفيذ العملية');
    } finally {
      setBusy(undefined);
    }
  }

  const catalogue = roles.data ?? [];
  const rows = (users.data ?? []).filter((row) => row.is_platform_admin || row.platform_roles.length > 0);

  return (
    <Screen title="أدوار المنصة" subtitle="منح أدوار المشغّلين وسحبها — لا توجد صلاحيات عامة هنا." crumbs={['المنصة', 'التشغيل']}>
      {roles.status === 'loading' && <Loading />}
      {roles.status === 'error' && <ErrorBox message={roles.error} onRetry={roles.reload} />}
      {roles.status === 'success' &&
        (catalogue.length === 0 ? (
          <Empty title="لا توجد أدوار" />
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>الدور</th>
                  <th>الرمز</th>
                  <th>الوصف</th>
                  <th className="num">الحائزون</th>
                  <th className="num">صلاحيات</th>
                </tr>
              </thead>
              <tbody>
                {catalogue.map((role) => (
                  <tr key={role.code}>
                    <td>{role.nameAr}</td>
                    <td dir="ltr">
                      <code>{role.code}</code>
                    </td>
                    <td className="muted">{role.description}</td>
                    <td className="num">{role.holderCount}</td>
                    <td className="num">{role.permissions.length}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}

      <div className="card tight no-print" style={{ marginTop: 18 }}>
        <h3 style={{ marginTop: 0 }}>منح / سحب</h3>
        <div className="row">
          <input
            className="input"
            style={{ maxWidth: 280 }}
            placeholder="بحث بالبريد أو الاسم"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
          <button className="btn primary" type="button" onClick={() => setApplied(search)}>
            بحث
          </button>
        </div>
        <p className="muted" style={{ marginBottom: 0 }}>
          يُعرض فقط حائزو أدوار المنصة والحسابات المميزة بصلاحية المنصة. الحسابات العادية تُدار من صفحة المستخدمين في بيئة المستأجر.
        </p>
      </div>

      {error && (
        <p className="alert danger" role="alert">
          {error}
        </p>
      )}

      {users.status === 'loading' && <Loading />}
      {users.status === 'error' && <ErrorBox message={users.error} onRetry={users.reload} />}
      {users.status === 'success' &&
        (rows.length === 0 ? (
          <Empty title="لا توجد نتائج" />
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>الحساب</th>
                  {catalogue.map((role) => (
                    <th key={role.code} title={role.description}>
                      {role.nameAr}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.id}>
                    <td>
                      <div>{row.full_name}</div>
                      <div className="muted" dir="ltr">
                        {row.email}
                      </div>
                    </td>
                    {catalogue.map((role) => {
                      const held = row.platform_roles.includes(role.code);
                      const key = `${row.id}:${role.code}`;
                      return (
                        <td key={role.code}>
                          <button
                            className={`btn ${held ? '' : 'primary'}`}
                            type="button"
                            disabled={busy === key}
                            onClick={() => void mutate(row.id, role.code, !held)}
                          >
                            {busy === key ? '…' : held ? 'سحب' : 'منح'}
                          </button>
                        </td>
                      );
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}
    </Screen>
  );
}
