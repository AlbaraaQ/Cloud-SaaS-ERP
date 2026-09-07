import { ModulePage, type Feature } from '../../components/module-page';
const features: Feature[] = [
  { name: 'Measurement card', endpoint: '/tailoring/measurements', action: 'versioned per-customer tailoring measurements' },
  { name: 'Latest party measurements', endpoint: '/tailoring/parties/{partyId}/measurements/latest', action: 'display in party and invoice context' },
];
export default function Page() { return <ModulePage title="الخياطة والمقاسات" subtitle="بطاقات مقاسات العملاء بنسخ زمنية." features={features} kind="tailoring" />; }
