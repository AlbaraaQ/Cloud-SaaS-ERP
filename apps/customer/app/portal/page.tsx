import { Kpi } from '../../components/card';
import { PortalShell } from '../../components/portal-shell';
import { SimpleTable } from '../../components/table';

export default function PortalDashboard() { return <PortalShell><section><h1>لوحة العميل</h1><p className="muted">الأرصدة المفتوحة، آخر المستندات، والمدفوعات المعلقة.</p></section><div className="grid cols"><Kpi label="الرصيد المفتوح" value="SAR 0.00" /><Kpi label="فواتير حديثة" value="0" /><Kpi label="مدفوعات معلقة" value="0" /></div><SimpleTable rows={[{ id: 'empty', document: 'لا توجد مستندات', date: '-', status: 'ready' }]} columns={['document','date','status']} /></PortalShell>; }
