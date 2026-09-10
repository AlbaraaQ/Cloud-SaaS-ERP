'use client';

import { Empty, ErrorBox, Forbidden, Loading, Screen } from '../../../components/screen';
import { apiData } from '../../../lib/api';
import { useQuery } from '../../../lib/use-query';

type Membership = {
  id: string;
  displayName?: string;
  display_name?: string;
  status: string;
  isOwner?: boolean;
  is_owner?: boolean;
  roles?: Array<{ id: string; name: string }>;
};

export default function TenantUsersPage() {
  const memberships = useQuery<Membership[] | { items?: Membership[] }>(() => apiData('/memberships'), []);
  const rows: Membership[] = Array.isArray(memberships.data)
    ? memberships.data
    : ((memberships.data as { items?: Membership[] })?.items ?? []);

  return (
    <Screen title="بطاقة مستخدم" subtitle="مستخدمو هذه المنشأة وأدوارهم." crumbs={['الإعدادات', 'إعدادات المستخدمين']}>
      {memberships.status === 'loading' && <Loading />}
      {memberships.status === 'forbidden' && <Forbidden />}
      {memberships.status === 'error' && <ErrorBox message={memberships.error} onRetry={memberships.reload} />}
      {memberships.status === 'success' &&
        (rows.length === 0 ? (
          <Empty title="لا يوجد مستخدمون" />
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>الاسم</th>
                  <th>الحالة</th>
                  <th>مالك</th>
                  <th>الأدوار</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.id}>
                    <td>{row.displayName ?? row.display_name}</td>
                    <td>
                      <span className={`badge ${row.status}`}>{row.status}</span>
                    </td>
                    <td>{(row.isOwner ?? row.is_owner) ? 'نعم' : '—'}</td>
                    <td>{(row.roles ?? []).map((role) => role.name).join('، ') || '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}
    </Screen>
  );
}
