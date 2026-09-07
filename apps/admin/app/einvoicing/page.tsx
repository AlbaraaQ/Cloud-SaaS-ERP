import { ModulePage } from '../../components/module-page';

const features = [{ name: 'Credentials', endpoint: '/einvoice/credentials', action: 'masked manage' },{ name: 'Submissions', endpoint: '/einvoice/submissions', action: 'monitor/retry' },{ name: 'Submit invoice', endpoint: '/sales-invoices/{id}/einvoice/submit', action: 'submit' },{ name: 'Health', endpoint: '/einvoice/health', action: 'read' }];

export default function Page() { return <ModulePage title='الفوترة الإلكترونية' subtitle='بيانات الاعتماد والإرسال والصحة وسجل ZATCA/ETA.' features={features} kind='einvoicing' />; }
