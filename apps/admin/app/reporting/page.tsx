import { ModulePage } from '../../components/module-page';

const features = [{ name: 'Catalog', endpoint: '/reports', action: 'list' },{ name: 'Runner', endpoint: '/reports/{key}', action: 'run' },{ name: 'Export', endpoint: '/reports/{key}/export', action: 'queue' },{ name: 'Invoice print', endpoint: '/sales-invoices/{id}/print', action: 'print css' },{ name: 'Shift print', endpoint: '/shift-closes/{id}/print-data', action: 'print css' }];

export default function Page() { return <ModulePage title='مركز التقارير' subtitle='تشغيل التقارير وتصدير CSV/XLSX/PDF وقوالب الطباعة.' features={features} kind='reporting' />; }
