import { ModulePage } from '../../components/module-page';

const features = [{ name: 'Sales invoices', endpoint: '/sales-invoices', action: 'create/post/void' },{ name: 'Payments', endpoint: '/sales-invoices/{id}/payments', action: 'pay' },{ name: 'Returns', endpoint: '/sales-adjustment-notes', action: 'credit/debit' },{ name: 'Offers', endpoint: '/offers', action: 'evaluate' },{ name: 'Print', endpoint: '/sales-invoices/{id}/print', action: 'browser print' }];

export default function Page() { return <ModulePage title='المبيعات' subtitle='فواتير البيع والمرتجعات والمدفوعات والعروض والطباعة.' features={features} kind='sales' />; }
