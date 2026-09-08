'use client';

import { useSearchParams } from 'next/navigation';
import { Suspense, useMemo } from 'react';

import { Empty, ErrorBox, Forbidden, Loading, Screen } from '../../../components/screen';
import { apiData } from '../../../lib/api';
import { useQuery } from '../../../lib/use-query';

type CashLocation = {
  id: string;
  code?: string;
  nameAr?: string;
  name_ar?: string;
  type?: string;
  kind?: string;
  currencyCode?: string;
  currency_code?: string;
  accountId?: string;
  isActive?: boolean;
  is_active?: boolean;
};

function Inner() {
  const search = useSearchParams();
  const wanted = search?.get('type') ?? '';
  const locations = useQuery<CashLocation[]>(() => apiData<CashLocation[]>('/cash-locations'), []);

  const rows = useMemo(() => {
    const list = locations.data ?? [];
    if (!wanted) return list;
    return list.filter((row) => (row.type ?? row.kind ?? '').toLowerCase().includes(wanted));
  }, [locations.data, wanted]);

  const title = wanted === 'bank' ? 'بطاقة بنك' : wanted === 'cash' ? 'بطاقة صندوق' : 'الصناديق والبنوك';

  return (
    <Screen
      title={title}
      subtitle="مواقع النقدية المرتبطة بحسابات دليل الحسابات."
      crumbs={['المحاسبة', 'تعاريف']}
      actions={
        <button className="btn" type="button" onClick={() => window.print()}>
          طباعة
        </button>
      }
    >
      {locations.status === 'loading' && <Loading />}
      {locations.status === 'forbidden' && <Forbidden />}
      {locations.status === 'error' && <ErrorBox message={locations.error} onRetry={locations.reload} />}
      {locations.status === 'success' &&
        (rows.length === 0 ? (
          <Empty title="لا توجد سجلات" detail="تُنشأ خزنة رئيسية تلقائياً عند تجهيز المنشأة." />
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>الرمز</th>
                  <th>الاسم</th>
                  <th>النوع</th>
                  <th>العملة</th>
                  <th>الحالة</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.id}>
                    <td dir="ltr">{row.code ?? '—'}</td>
                    <td>{row.nameAr ?? row.name_ar ?? '—'}</td>
                    <td>{row.type ?? row.kind ?? '—'}</td>
                    <td dir="ltr">{row.currencyCode ?? row.currency_code ?? '—'}</td>
                    <td>
                      <span className={`badge ${(row.isActive ?? row.is_active) === false ? 'planned' : 'active'}`}>
                        {(row.isActive ?? row.is_active) === false ? 'موقوف' : 'نشط'}
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

export default function Page() {
  return (
    <Suspense fallback={<Loading />}>
      <Inner />
    </Suspense>
  );
}
