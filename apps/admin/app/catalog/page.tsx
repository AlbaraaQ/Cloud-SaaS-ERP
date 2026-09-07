import { ModulePage } from '../../components/module-page';

const features = [{ name: 'Items', endpoint: '/items', action: 'crud' },{ name: 'Categories', endpoint: '/item-categories', action: 'crud' },{ name: 'Units', endpoint: '/units-of-measure', action: 'crud' },{ name: 'Tax groups', endpoint: '/tax-groups', action: 'crud' },{ name: 'Item units', endpoint: '/items/{id}/units', action: 'manage' },{ name: 'Components', endpoint: '/items/{id}/components', action: 'bom' },{ name: 'Price history', endpoint: '/items/{id}/price-history', action: 'read' }];

export default function Page() { return <ModulePage title='الكتالوج' subtitle='الأصناف والتصنيفات والوحدات والباركود والضرائب والأسعار.' features={features} kind='catalog' />; }
