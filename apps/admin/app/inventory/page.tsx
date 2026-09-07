import { ModulePage } from '../../components/module-page';

const features = [{ name: 'Levels', endpoint: '/inventory/levels', action: 'read' },{ name: 'Movements', endpoint: '/inventory/movements', action: 'read' },{ name: 'Adjustments', endpoint: '/inventory/adjustments/post', action: 'approve' },{ name: 'Transfers', endpoint: '/inventory/transfers', action: 'send/receive' },{ name: 'Lots', endpoint: '/inventory/lots', action: 'track' },{ name: 'Serials', endpoint: '/inventory/serials', action: 'reserve/consume' },{ name: 'Valuation', endpoint: '/inventory/valuation/as-of', action: 'report' }];

export default function Page() { return <ModulePage title='المخزون' subtitle='الأرصدة والحركات والتحويلات والتسويات والدفعات والسيريالات.' features={features} kind='inventory' />; }
