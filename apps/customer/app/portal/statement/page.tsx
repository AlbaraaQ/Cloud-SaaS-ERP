import { PortalShell } from '../../../components/portal-shell';
import { SimpleTable } from '../../../components/table';

export default function StatementPage() { return <PortalShell><h1>كشف الحساب</h1><div className="toolbar"><input className="input" type="date" /><input className="input" type="date" /><button className="btn primary" type="button">عرض</button><button className="btn" type="button">PDF/CSV</button></div><SimpleTable rows={[{ id: '1', date: '2026-09-07', description: 'رصيد افتتاحي', debit: '0.00', credit: '0.00', running: '0.00' }]} columns={['date','description','debit','credit','running']} /></PortalShell>; }
