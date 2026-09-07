import { ModulePage } from '../../components/module-page';

const features = [{ name: 'Parties', endpoint: '/parties', action: 'crud' },{ name: 'Contacts', endpoint: '/parties/{id}/contacts', action: 'manage' },{ name: 'Balance', endpoint: '/parties/{id}/balance', action: 'read' },{ name: 'Statement', endpoint: '/parties/{id}/statement', action: 'read' },{ name: 'Allocations', endpoint: '/allocations', action: 'allocate' }];

export default function Page() { return <ModulePage title='العملاء والموردون' subtitle='جهات التعامل والاتصالات والأرصدة وكشوف الحساب والتخصيص.' features={features} kind='parties' />; }
