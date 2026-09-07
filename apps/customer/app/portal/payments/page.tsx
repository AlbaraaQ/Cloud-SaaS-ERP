import { PortalShell } from '../../../components/portal-shell';
import { SimpleTable } from '../../../components/table';

export default function PaymentsPage() { return <PortalShell><h1>سجل المدفوعات</h1><SimpleTable rows={[{ id: 'v1', voucher: 'RV-000001', method: 'cash', allocated: '115.00', status: 'paid' }]} columns={['voucher','method','allocated','status']} /></PortalShell>; }
