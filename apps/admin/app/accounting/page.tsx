import { ModulePage } from '../../components/module-page';

const features = [{ name: 'Accounts', endpoint: '/accounts', action: 'tree/crud' },{ name: 'Fiscal years', endpoint: '/fiscal-years', action: 'manage' },{ name: 'Fiscal periods', endpoint: '/fiscal-periods', action: 'close/reopen/lock' },{ name: 'Journal entries', endpoint: '/journal-entries', action: 'draft/post/reverse' },{ name: 'Cost centers', endpoint: '/cost-centers', action: 'crud' },{ name: 'Opening balances', endpoint: '/opening-balances', action: 'post' },{ name: 'Trial balance', endpoint: '/statements/trial-balance', action: 'report' },{ name: 'General ledger', endpoint: '/statements/general-ledger', action: 'report' }];

export default function Page() { return <ModulePage title='المحاسبة' subtitle='شجرة الحسابات والفترات والقيود والميزان والأستاذ.' features={features} kind='accounting' />; }
