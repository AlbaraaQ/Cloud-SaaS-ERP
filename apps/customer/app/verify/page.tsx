import { VerifyForm } from '../../components/forms';

export default function VerifyPage() { return <div className="grid"><section className="hero"><h1>التحقق العام من الفاتورة</h1><p>أدخل UUID/hash لعرض بيانات محدودة فقط: المصدر، التاريخ، الإجمالي، وحالة QR. الصفحة محمية بمعدل طلبات صارم.</p></section><VerifyForm /></div>; }
