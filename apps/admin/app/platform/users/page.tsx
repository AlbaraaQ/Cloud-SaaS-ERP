'use client';

import { useState } from 'react';

import { Empty, ErrorBox, Loading, Screen } from '../../../components/screen';
import { apiData } from '../../../lib/api';
import { useQuery } from '../../../lib/use-query';

type User = {
  id: string;
  email: string;
  full_name: string;
  status: string;
  is_platform_admin: boolean;
  must_change_password: boolean;
  last_login_at: string | null;
  created_at: string;
  membership_count: number;
};

export default function PlatformUsersPage() {
  const [search, setSearch] = useState('');
  const [applied, setApplied] = useState('');
  const users = useQuery<User[]>(() => apiData<User[]>(`/platform/users${applied ? `?search=${encodeURIComponent(applied)}` : ''}`), [applied]);

  const rows = users.data ?? [];

  return (
    <Screen title="مستخدمو المنصة" subtitle="كل الحسابات عبر جميع المنشآت." crumbs={['المنصة', 'التشغيل']}>
      <div className="card tight no-print">
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
      </div>

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
                  <th>الاسم</th>
                  <th>البريد</th>
                  <th>الحالة</th>
                  <th className="num">عضويات</th>
                  <th>آخر دخول</th>
                  <th>الدور</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.id}>
                    <td>{row.full_name}</td>
                    <td dir="ltr">{row.email}</td>
                    <td>
                      <span className={`badge ${row.status}`}>{row.status}</span>
                      {row.must_change_password && <span className="badge pending" style={{ marginInlineStart: 4 }}>تغيير كلمة المرور</span>}
                    </td>
                    <td className="num">{row.membership_count}</td>
                    <td dir="ltr">{row.last_login_at ? new Date(row.last_login_at).toLocaleString('ar-SA') : '—'}</td>
                    <td>{row.is_platform_admin ? <span className="badge active">مدير منصة</span> : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}
    </Screen>
  );
}
