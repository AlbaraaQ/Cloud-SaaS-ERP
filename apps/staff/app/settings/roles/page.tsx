'use client';

import { Empty, ErrorBox, Forbidden, Loading, Screen } from '../../../components/screen';
import { apiData } from '../../../lib/api';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type Role = { id: string; name: string; description?: string | null; isSystem?: boolean; is_system?: boolean };

export default function RolesPage() {
  const { me } = useSession();
  const roles = useQuery<Role[] | { items?: Role[] }>(() => apiData('/roles'), []);
  const rows: Role[] = Array.isArray(roles.data) ? roles.data : ((roles.data as { items?: Role[] })?.items ?? []);

  return (
    <Screen title="صلاحيات المستخدمين" subtitle="الأدوار ومجموعات الصلاحيات المرتبطة بها." crumbs={['الإعدادات', 'إعدادات المستخدمين']}>
      <section className="card">
        <h2>صلاحياتك الحالية</h2>
        {me?.permissions.includes('*') ? (
          <p className="alert ok">لديك كل الصلاحيات (دور المالك).</p>
        ) : (
          <div className="chips">
            {(me?.permissions ?? []).map((permission) => (
              <span className="chip small" key={permission} dir="ltr">
                {permission}
              </span>
            ))}
          </div>
        )}
      </section>

      {roles.status === 'loading' && <Loading />}
      {roles.status === 'forbidden' && <Forbidden />}
      {roles.status === 'error' && <ErrorBox message={roles.error} onRetry={roles.reload} />}
      {roles.status === 'success' &&
        (rows.length === 0 ? (
          <Empty title="لا توجد أدوار" />
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>الدور</th>
                  <th>الوصف</th>
                  <th>نظامي</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((role) => (
                  <tr key={role.id}>
                    <td>{role.name}</td>
                    <td className="muted">{role.description ?? '—'}</td>
                    <td>{(role.isSystem ?? role.is_system) ? 'نعم' : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}
    </Screen>
  );
}
