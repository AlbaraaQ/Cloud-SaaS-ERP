import { ModulePage } from '../../components/module-page';

const features = [{ name: 'Vouchers', endpoint: '/vouchers', action: 'draft/post/void' },{ name: 'Cheques', endpoint: '/vouchers/{id}/cheque', action: 'clear/bounce/collect' },{ name: 'Cash transfers', endpoint: '/cash-transfers', action: 'send/receive' },{ name: 'Expense types', endpoint: '/expense-types', action: 'manage' },{ name: 'Shift close', endpoint: '/shift-closes', action: 'open/close/print' },{ name: 'Cash balance', endpoint: '/cash-locations/{id}/balance', action: 'recalc' }];

export default function Page() { return <ModulePage title='الخزينة' subtitle='السندات والشيكات والتحويلات وإغلاق الورديات.' features={features} kind='treasury' />; }
