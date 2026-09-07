import { Empty } from '../../../components/card';
import { QuickSaleForm } from '../../../components/forms';
import { PortalShell } from '../../../components/portal-shell';

export default function QuickSalePage() { return <PortalShell><h1>بيع سريع</h1><Empty title="portal.quick_sale" detail="إذا كان العلم غير مفعل تظهر رسالة: اتصل بمدير النظام لتفعيل البيع السريع." /><section className="card"><QuickSaleForm /></section></PortalShell>; }
