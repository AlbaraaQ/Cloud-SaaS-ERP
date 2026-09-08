'use client';

import { useEffect, useState } from 'react';

import { KpiCard } from '../../components/kpi-card';
import { apiFetch } from '../../lib/api';

type Tenant = { id: string; code: string; name: string; status: string; plan_name?: string };

function token() { const value = globalThis.document.cookie.split('; ').find((part) => part.startsWith('erp_access_token=')); return value ? decodeURIComponent(value.split('=')[1] ?? '') : undefined; }

export default function PlatformPage() {
  const [tenant, setTenant] = useState<Tenant | null>(null);
  const [error, setError] = useState('');
  useEffect(() => { apiFetch<{ data: Tenant }>('/tenant', token()).then((result) => setTenant(result.data)).catch(() => setError('تعذر تحميل بيانات الشركة. تحقق من صلاحيات الحساب.')); }, []);
  return <div className="grid"><section className="section-title"><div><h1>إدارة الحسابات والشركات</h1><p className="muted">إدارة بيانات الشركة وسياق المستأجر المتصل حالياً.</p></div></section>{error && <p className="muted" role="alert">{error}</p>}<div className="grid cols"><KpiCard label="الشركة" value={tenant?.name ?? 'غير متاح'} /><KpiCard label="رمز الشركة" value={tenant?.code ?? '-'} /><KpiCard label="الحالة" value={tenant?.status ?? '-'} /><KpiCard label="الباقة" value={tenant?.plan_name ?? 'راجع الاشتراكات'} /></div><section className="card"><h2>إدارة الوصول</h2><p className="muted">طلبات التفعيل اليدوي والاشتراكات تتم إدارتها من صفحة اشتراكات العملاء.</p><a className="btn primary" href="/billing">فتح الاشتراكات</a></section></div>;
}
