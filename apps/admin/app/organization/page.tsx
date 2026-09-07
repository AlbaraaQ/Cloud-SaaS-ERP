import { ModulePage } from '../../components/module-page';

const features = [{ name: 'Company profile', endpoint: '/company-profile', action: 'get/put' },{ name: 'Branches', endpoint: '/branches', action: 'crud' },{ name: 'Warehouses', endpoint: '/warehouses', action: 'crud' },{ name: 'Cash locations', endpoint: '/cash-locations', action: 'crud + balances' },{ name: 'Currencies', endpoint: '/currencies', action: 'fx rates' },{ name: 'Price lists', endpoint: '/price-lists', action: 'prices' },{ name: 'Posting profiles', endpoint: '/branch-posting-profiles', action: 'resolve' }];

export default function Page() { return <ModulePage title='المؤسسة' subtitle='الشركة والفروع والمستودعات والخزن والعملات وقوائم الأسعار وبروفايلات الترحيل.' features={features} kind='organization' />; }
