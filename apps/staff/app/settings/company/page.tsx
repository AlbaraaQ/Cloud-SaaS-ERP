'use client';

import { Empty, ErrorBox, Forbidden, Loading, Screen } from '../../../components/screen';
import { apiData } from '../../../lib/api';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type Profile = Record<string, unknown> | null;

const LABELS: Record<string, string> = {
  legalNameAr: 'الاسم القانوني (عربي)',
  legalNameEn: 'الاسم القانوني (إنجليزي)',
  vatNumber: 'الرقم الضريبي',
  crNumber: 'السجل التجاري',
  addressAr: 'العنوان',
  phone: 'الهاتف',
  email: 'البريد الإلكتروني',
  website: 'الموقع',
};

export default function CompanyProfilePage() {
  const { me } = useSession();
  const profile = useQuery<Profile>(() => apiData<Profile>('/company-profile'), []);

  return (
    <Screen title="بطاقة المنشأة" subtitle="البيانات القانونية المطبوعة على الفواتير والمستندات." crumbs={['الإعدادات', 'تعاريف المنشأة']}>
      <section className="card">
        <h2>الحساب على المنصة</h2>
        <dl className="kv">
          <dt>اسم المنشأة</dt>
          <dd>{me?.membership.tenantName}</dd>
          <dt>رمز المنشأة</dt>
          <dd dir="ltr">{me?.membership.tenantCode}</dd>
          <dt>دورك</dt>
          <dd>{me?.membership.isOwner ? 'مالك' : 'مستخدم'}</dd>
        </dl>
      </section>

      {profile.status === 'loading' && <Loading rows={3} />}
      {profile.status === 'forbidden' && <Forbidden />}
      {profile.status === 'error' && <ErrorBox message={profile.error} onRetry={profile.reload} />}
      {profile.status === 'success' && (
        <section className="card">
          <h2>البيانات القانونية</h2>
          {!profile.data ? (
            <Empty title="لم تُسجَّل بعد" detail="تُضبط عبر PATCH /company-profile — شاشة التحرير قيد التنفيذ." />
          ) : (
            <dl className="kv">
              {Object.entries(profile.data).map(([key, value]) => (
                <>
                  <dt key={`k-${key}`}>{LABELS[key] ?? key}</dt>
                  <dd key={`v-${key}`} dir="auto">{value === null || value === undefined ? '—' : String(value)}</dd>
                </>
              ))}
            </dl>
          )}
        </section>
      )}
    </Screen>
  );
}
