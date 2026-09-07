import { Kpi } from '../../components/card';

export default function PricingPage() { return <div className="grid"><section className="hero"><h1>الأسعار</h1><p>باقات مرنة حسب عدد الفروع والمستخدمين والتكاملات. هذه صفحة placeholder حتى اعتماد الأسعار التجارية.</p></section><div className="grid cols"><Kpi label="Starter" value="قريباً" /><Kpi label="Business" value="قريباً" /><Kpi label="Enterprise" value="تواصل معنا" /></div></div>; }
