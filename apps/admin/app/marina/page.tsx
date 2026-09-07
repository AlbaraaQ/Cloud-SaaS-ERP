import { ModulePage, type Feature } from '../../components/module-page';
const features: Feature[] = [
  { name: 'Vessel groups and pricing', endpoint: '/marina/groups', action: 'hour, half-hour, day and offer prices' },
  { name: 'Vessels and owners', endpoint: '/marina/vessels', action: 'owner party links with percentage shares' },
  { name: 'Bookings', endpoint: '/marina/bookings', action: 'period, insurance, companions and additions' },
  { name: 'Rental invoice', endpoint: '/marina/bookings/{id}/rental-invoice', action: 'sales invoice service path' },
  { name: 'Violations', endpoint: '/marina/violations', action: 'lightweight violation register' },
  { name: 'Operation plan', endpoint: '/marina/operation-plans', action: 'ordered vessels per group/day' },
];
export default function Page() { return <ModulePage title="المارينا والتأجير" subtitle="القوارب والحجوزات وفواتير التأجير وخطط التشغيل." features={features} kind="marina" />; }
