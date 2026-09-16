import './globals.css';

import type { Metadata } from 'next';
import Link from 'next/link';
import type { ReactNode } from 'react';

import { publicRoutes } from '../lib/navigation';

export const metadata: Metadata = { title: 'Cloud SaaS ERP', description: 'نظام تخطيط موارد المؤسسات السحابي — الأسعار والاشتراك والتحقق' };

export default function RootLayout({ children }: { children: ReactNode }) {
  return <html lang="ar" dir="rtl"><body><div className="wrap"><header className="top"><Link className="brand" href="/"><span className="logo">ERP</span><span>Cloud SaaS ERP</span></Link><nav className="nav" aria-label="public navigation">{publicRoutes.map((route) => <Link href={route.href} key={route.key}>{route.labelAr}</Link>)}<Link href="/onboarding">اشترك</Link><Link className="btn primary" href="/login">دخول</Link></nav></header><main style={{ marginTop: 18 }}>{children}</main></div></body></html>;
}
