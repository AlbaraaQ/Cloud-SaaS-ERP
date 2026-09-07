import { ModulePage, type Feature } from '../../components/module-page';

const features: Feature[] = [
  { name: 'Installment contracts', endpoint: '/installments/contracts', action: 'list/create from sale or standalone party+item' },
  { name: 'Contract detail', endpoint: '/installments/contracts/{id}', action: 'schedule grid with due/paid/status rows' },
  { name: 'Collect dialog', endpoint: '/installments/contracts/{id}/collect', action: 'receipt voucher then oldest-row allocation' },
  { name: 'Overdue aging', endpoint: '/installments/overdue', action: 'as-of aging source for reports' },
  { name: 'Early settlement', endpoint: 'tenant setting pack.installments', action: 'flat discount rule surfaced in summary' },
];

export default function Page() { return <ModulePage title="الأقساط" subtitle="عقود التقسيط، جداول الاستحقاق، التحصيل والتقادُم." features={features} kind="installments" />; }
