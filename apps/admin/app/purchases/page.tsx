import { ModulePage } from '../../components/module-page';

const features = [{ name: 'Purchase invoices', endpoint: '/purchase-invoices', action: 'create/post/void' },{ name: 'Landed cost preview', endpoint: '/purchase-invoices/preview-landed-cost', action: 'preview' },{ name: 'Costs', endpoint: '/purchase-invoices/{id}/costs', action: 'allocate' },{ name: 'Supplier payments', endpoint: '/purchase-invoices/{id}/payments', action: 'pay' },{ name: 'Returns', endpoint: '/purchase-invoices', action: 'purchase_return' }];

export default function Page() { return <ModulePage title='المشتريات' subtitle='فواتير الموردين والتكاليف الإضافية والمدفوعات.' features={features} kind='purchases' />; }
