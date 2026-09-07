import { Empty } from '../../../components/card';
import { PortalShell } from '../../../components/portal-shell';

export default function TasksPage() { return <PortalShell><h1>صندوق المهام</h1><Empty title="لا توجد مهام" detail="تظهر هنا موافقات تغييرات الأسعار وتجاوزات الائتمان عند تفعيلها." /></PortalShell>; }
