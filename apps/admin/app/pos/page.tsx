import { ModulePage } from '../../components/module-page';

const features = [
  { name: 'Floor map grid', endpoint: '/pos/tables', action: 'status/balance chips' },
  { name: 'Table categories', endpoint: '/pos/categories', action: 'printer routing' },
  { name: 'Open table', endpoint: '/pos/tables/{id}/open', action: 'draft order state' },
  { name: 'Order screen', endpoint: '/pos/tables/{id}/items', action: 'items/additions/notes' },
  { name: 'Void item', endpoint: '/pos/events/{id}/void', action: 'reason audit' },
  { name: 'Merge/split', endpoint: '/pos/tables/{id}/merge|split', action: 'combined table flow' },
  { name: 'Send to invoice', endpoint: '/pos/tables/{id}/send-to-invoice', action: 'daily POS number' },
  { name: 'Pay & close', endpoint: '/pos/tables/{id}/close', action: 'sales post path' },
  { name: 'Daily numbers reset', endpoint: 'sequence pos_order:YYYY-MM-DD', action: 'per branch/day view' },
];

export default function Page() { return <ModulePage title="مطعم POS" subtitle="خريطة الطاولات وتدفق الطلبات والإضافات وإعدادات طباعة المطبخ." features={features} kind="pos" />; }
