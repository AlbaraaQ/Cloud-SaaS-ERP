import { ModulePage, type Feature } from '../../components/module-page';
const features: Feature[] = [
  { name: 'Vehicle makes/models', endpoint: '/fitment/makes', action: 'catalog vehicle hierarchy' },
  { name: 'Item compatibility', endpoint: '/fitment/items', action: 'year range fitment rows' },
  { name: 'Find items for vehicle', endpoint: '/fitment/items-for-vehicle', action: 'catalog picker compatibility lookup' },
  { name: 'Find vehicles for item', endpoint: '/fitment/items/{itemId}/vehicles', action: 'reverse compatibility lookup' },
];
export default function Page() { return <ModulePage title="توافق المركبات" subtitle="ربط الأصناف بالموديلات وسنوات التصنيع." features={features} kind="fitment" />; }
