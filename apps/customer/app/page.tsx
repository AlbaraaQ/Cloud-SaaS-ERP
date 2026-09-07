import Link from 'next/link';

import { Kpi } from '../components/card';
import { copy } from '../lib/i18n';

export default function Home() {
  return <div className="grid"><section className="hero"><p className="muted">Cloud SaaS ERP</p><h1>{copy.ar.title}</h1><p>{copy.ar.subtitle}</p><div className="toolbar"><Link className="btn primary" href="/portal">{copy.ar.portal}</Link><Link className="btn" href="/verify">{copy.ar.verify}</Link><Link className="btn" href="/contact">{copy.ar.contact}</Link></div></section><section className="grid cols"><Kpi label="فواتير إلكترونية" value="QR" hint="ZATCA verification friendly" /><Kpi label="كشف حساب" value="PDF/CSV" hint="self-service exports" /><Kpi label="تهيئة سريعة" value="5 خطوات" hint="company to first branch" /></section></div>;
}
