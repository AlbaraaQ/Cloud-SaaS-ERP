import { ModulePage } from '../../components/module-page';

const features = [{ name: 'Run wizard', endpoint: '/migration/runs', action: 'analyze/dry_run/import' },{ name: 'Issues', endpoint: '/migration/runs/{id}/issues', action: 'grid' },{ name: 'Reconciliation', endpoint: '/migration/runs/{id}/reconciliation', action: 'R1-R7' },{ name: 'Legacy lookup', endpoint: '/compat/docs/status', action: 'GlobalID status' },{ name: 'Compat devices', endpoint: '/compat/devices', action: 'register/revoke' },{ name: 'Cursors', endpoint: '/compat/sync/cursor', action: 'reset' }];

export default function Page() { return <ModulePage title='الهجرة والتوافق' subtitle='معالج الهجرة وسجل القضايا والمطابقة وأجهزة سطح المكتب.' features={features} kind='migration' />; }
