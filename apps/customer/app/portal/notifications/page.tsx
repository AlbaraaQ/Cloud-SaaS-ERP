import { PortalShell } from '../../../components/portal-shell';
import { SimpleTable } from '../../../components/table';

export default function NotificationsPage() { return <PortalShell><h1>الإشعارات</h1><SimpleTable rows={[{ id: 'n1', type: 'einvoice.shared', message: 'تمت مشاركة فاتورة إلكترونية', status: 'ready' }]} columns={['type','message','status']} /></PortalShell>; }
