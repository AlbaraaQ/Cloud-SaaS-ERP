import { ModulePage } from '../../components/module-page';

const features = [{ name: 'Tenant settings', endpoint: '/tenant', action: 'view/update' },{ name: 'Memberships', endpoint: '/memberships', action: 'invite/manage' },{ name: 'Roles', endpoint: '/roles', action: 'permissions' },{ name: 'Audit log', endpoint: '/audit-log', action: 'read-only' },{ name: 'Files', endpoint: '/files', action: 'presign/finalize/download' },{ name: 'Notifications', endpoint: '/notifications', action: 'inbox' },{ name: 'Jobs', endpoint: '/jobs', action: 'queue health' }];

export default function Page() { return <ModulePage title='المنصة والإعدادات' subtitle='إدارة المستأجر والأدوار والصلاحيات والتدقيق والملفات والوظائف.' features={features} kind='platform' />; }
