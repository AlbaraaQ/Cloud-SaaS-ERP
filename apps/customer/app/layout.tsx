import './globals.css';

import type { Metadata } from 'next';
import Link from 'next/link';
import type { ReactNode } from 'react';

import { publicRoutes } from '../lib/navigation';

export const metadata: Metadata = { title: 'ERP Customer Portal', description: 'Customer self-service portal and public verification' };

export default function RootLayout({ children }: { children: ReactNode }) {
  return <html lang="ar" dir="rtl"><body><div className="wrap"><header className="top"><Link className="brand" href="/"><span className="logo">ERP</span><span>بوابة العملاء</span></Link><nav className="nav" aria-label="public navigation">{publicRoutes.map((route) => <Link href={route.href} key={route.key}>{route.labelAr}</Link>)}<Link className="btn primary" href="/auth/login">دخول</Link></nav></header><main style={{ marginTop: 18 }}>{children}</main></div></body></html>;
}
