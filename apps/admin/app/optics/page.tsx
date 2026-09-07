import { ModulePage, type Feature } from '../../components/module-page';
const features: Feature[] = [
  { name: 'Prescription cards', endpoint: '/optics/prescriptions', action: 'SPH/CYL/AX/ADD/IPD for right and left eye' },
  { name: 'Other Column grid', endpoint: '/optics/prescriptions', action: 'typed JSON R1..L5 lens/options grid' },
  { name: 'Invoice print section', endpoint: '/optics/invoice-lines/{lineId}/print-section', action: 'conditional prescription block on invoice print' },
];
export default function Page() { return <ModulePage title="النظارات والعدسات" subtitle="وصفات النظر وربطها بالعميل وبند الفاتورة." features={features} kind="optics" />; }
