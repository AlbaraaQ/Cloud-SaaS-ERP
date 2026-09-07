import { PortalShell } from '../../../components/portal-shell';
import { SimpleTable } from '../../../components/table';

export default function StockPage() { return <PortalShell><h1>استعلام المخزون</h1><div className="toolbar"><input className="input" placeholder="بحث صنف" /><button className="btn primary" type="button">استعلام</button></div><SimpleTable rows={[{ id: 'i1', item: 'SKU-1', warehouse: 'الرئيسي', quantity: '0.0000', status: 'ready' }]} columns={['item','warehouse','quantity','status']} /></PortalShell>; }
