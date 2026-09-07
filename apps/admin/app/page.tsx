'use client';

import { DataTable } from '../components/data-table';
import { KpiCard } from '../components/kpi-card';
import { StatusBadge } from '../components/status-badge';
import { messages } from '../lib/i18n';
import { sections } from '../lib/navigation';

export default function DashboardPage() {
  const rows = sections.slice(1).map((section) => ({ id: section.key, module: section.labelAr, endpoint: section.href, permission: section.permission, state: 'active' }));
  return <div className="grid"><section className="section-title"><div><h1>{messages.ar.title}</h1><p className="muted">{messages.ar.subtitle}</p></div></section><div className="grid cols"><KpiCard label="مبيعات اليوم" value="SAR 0.00" hint="from sales-by-day" /><KpiCard label="النقدية" value="SAR 0.00" hint="cash locations" /><KpiCard label="تنبيهات الهجرة" value="0" hint="blocking issues" /><KpiCard label="الفواتير الإلكترونية" value="healthy" hint="ZATCA sandbox" /></div><DataTable id="dashboard-modules" rows={rows} columns={[{ key: 'module', title: 'القسم' }, { key: 'endpoint', title: 'Route' }, { key: 'permission', title: 'Permission' }, { key: 'state', title: 'State', render: (row) => <StatusBadge value={String(row.state)} /> }]} /></div>;
}
