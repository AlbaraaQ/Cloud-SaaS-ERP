import { ModulePage, type Feature } from '../../components/module-page';

const features: Feature[] = [
  { name: 'Projects list', endpoint: '/projects', action: 'project code/name/customer/status cards' },
  { name: 'Kanban by stage', endpoint: '/projects/{id}', action: 'stages with per-user accreditation' },
  { name: 'Stage templates', endpoint: '/projects/stage-templates', action: 'ordered template stages copied to projects' },
  { name: 'BOQ editor', endpoint: '/projects/{id}/boq', action: 'terms, quantities, values and previously billed totals' },
  { name: 'Progress bill editor', endpoint: '/projects/{id}/progress-bills', action: 'percent/value modes with retention and net due' },
  { name: 'Post progress bill', endpoint: '/projects/progress-bills/{id}/post', action: 'sale invoice service path with no inventory movement' },
  { name: 'Retention release', endpoint: '/projects/progress-bills/{id}/release-retention', action: 'invoice against retention receivable slot' },
  { name: 'Requirement register', endpoint: '/projects/requirements', action: 'lightweight title/status/payload tracking' },
];

export default function Page() { return <ModulePage title="المقاولات والمشاريع" subtitle="إدارة المشاريع والمراحل وبنود BOQ ومستخلصات التنفيذ والضمان." features={features} kind="projects" />; }
