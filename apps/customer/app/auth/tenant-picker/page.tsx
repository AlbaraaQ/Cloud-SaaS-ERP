import { SimpleTable } from '../../../components/table';

export default function TenantPickerPage() { return <div className="grid"><h1>اختيار المستأجر</h1><SimpleTable rows={[{ id: 'demo', tenant: 'Demo tenant', status: 'ready', action: 'select' }]} columns={['tenant','status','action']} /></div>; }
