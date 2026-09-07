import Link from 'next/link';

import { PortalShell } from '../../../components/portal-shell';
import { SimpleTable } from '../../../components/table';

export default function InvoicesPage() { return <PortalShell><h1>فواتيري</h1><div className="toolbar"><input className="input" placeholder="بحث" /><Link className="btn" href="/portal/invoices/demo">تفاصيل نموذجية</Link></div><SimpleTable rows={[{ id: 'demo', number: 'SI-000001', date: '2026-09-07', total: 'SAR 115.00', status: 'paid' }]} columns={['number','date','total','status']} /></PortalShell>; }
