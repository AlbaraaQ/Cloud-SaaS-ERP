/* global window */
'use client';

import { ConfirmDialog } from './confirm-dialog';
import { DataTable } from './data-table';
import { FilterBar } from './filter-bar';
import { KpiCard } from './kpi-card';
import { MoneyInput, QtyInput } from './inputs';
import { ReportRunner } from './report-runner';
import { EmptyState, ErrorState, ForbiddenState, Skeleton } from './states';
import { StatusBadge } from './status-badge';

export type Feature = { name: string; endpoint: string; action: string; status?: string };

export function ModulePage({ title, subtitle, features, kind }: { title: string; subtitle: string; features: Feature[]; kind: string }) {
  const rows = features.map((feature, index) => ({ id: `${kind}-${index}`, ...feature, status: feature.status ?? 'active' }));
  return <div className="grid"><section className="section-title"><div><h1>{title}</h1><p className="muted">{subtitle}</p></div><div className="toolbar"><button className="btn primary" type="button">جديد</button><button className="btn" type="button">استيراد</button><button className="btn" type="button" onClick={() => typeof window !== 'undefined' && window.print()}>طباعة</button></div></section><div className="grid cols"><KpiCard label="جاهز" value={String(features.length)} hint="API-backed widgets" /><KpiCard label="معلّق" value="0" hint="pending approvals" /><KpiCard label="محظور" value="0" hint="permission hidden" /></div><FilterBar /><DataTable id={`table-${kind}`} rows={rows} columns={[{ key: 'name', title: 'Screen' }, { key: 'endpoint', title: 'Endpoint' }, { key: 'action', title: 'Action' }, { key: 'status', title: 'Status', render: (row) => <StatusBadge value={String(row.status)} /> }]} /><section className="card"><h2>نموذج إدخال</h2><form className="form"><input className="input" aria-label="name" placeholder="الاسم / Name" /><div className="row"><MoneyInput value="0.00" /><QtyInput value="1.0000" /></div><textarea className="input" aria-label="notes" placeholder="ملاحظات" /><div className="toolbar"><button className="btn primary" type="button">حفظ</button><button className="btn" type="button">ترحيل</button><ConfirmDialog action="إلغاء مع فرق" diff={{ before: 'draft', after: 'voided' }} /></div></form></section>{kind === 'reporting' ? <ReportRunner /> : null}<div className="grid cols"><EmptyState title="حالة فارغة" detail="تظهر عند عدم وجود بيانات." /><Skeleton /><ErrorState detail="تظهر عند فشل الاتصال بالـ API." /><ForbiddenState detail="يتم إخفاء الروابط أو عرض هذه الحالة حسب الصلاحية." /></div></div>;
}
