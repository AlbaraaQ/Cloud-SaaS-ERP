'use client';

import { Empty, ErrorBox, Forbidden, Loading, Screen } from '../../../components/screen';
import { apiData } from '../../../lib/api';
import { useQuery } from '../../../lib/use-query';

type Branch = {
  id: string;
  code?: string;
  nameAr?: string;
  name_ar?: string;
  nameEn?: string | null;
  isDefault?: boolean;
  is_default?: boolean;
  isActive?: boolean;
  is_active?: boolean;
};

export default function BranchesPage() {
  const branches = useQuery<Branch[]>(() => apiData<Branch[]>('/branches'), []);
  const rows = branches.data ?? [];

  return (
    <Screen title="بطاقة فرع" subtitle="فروع المنشأة. كل مستند يصدر تحت فرع، والترقيم مستقل لكل فرع." crumbs={['الإعدادات', 'تعاريف المنشأة']}>
      {branches.status === 'loading' && <Loading />}
      {branches.status === 'forbidden' && <Forbidden />}
      {branches.status === 'error' && <ErrorBox message={branches.error} onRetry={branches.reload} />}
      {branches.status === 'success' &&
        (rows.length === 0 ? (
          <Empty title="لا توجد فروع" />
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>الرمز</th>
                  <th>الاسم</th>
                  <th>افتراضي</th>
                  <th>الحالة</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((branch) => (
                  <tr key={branch.id}>
                    <td dir="ltr">{branch.code ?? '—'}</td>
                    <td>{branch.nameAr ?? branch.name_ar ?? branch.nameEn ?? '—'}</td>
                    <td>{(branch.isDefault ?? branch.is_default) ? <span className="badge active">نعم</span> : '—'}</td>
                    <td>
                      <span className={`badge ${(branch.isActive ?? branch.is_active) === false ? 'planned' : 'active'}`}>
                        {(branch.isActive ?? branch.is_active) === false ? 'موقوف' : 'نشط'}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}
    </Screen>
  );
}
