import { ProfileRequestForm } from '../../../components/forms';
import { PortalShell } from '../../../components/portal-shell';

export default function ProfilePage() { return <PortalShell><section className="card"><h1>طلب تعديل البيانات</h1><p className="muted">الطلب ينشئ مهمة/إشعار للإدارة ولا يغير بيانات master مباشرة.</p><ProfileRequestForm /></section></PortalShell>; }
