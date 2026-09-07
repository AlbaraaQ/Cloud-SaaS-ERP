import './globals.css';

import type { Metadata } from 'next';
import type { ReactNode } from 'react';
import Link from 'next/link';

import { sections } from '../lib/navigation';

export const metadata: Metadata = { title: 'Cloud ERP Admin', description: 'Arabic-first ERP back office' };

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return <html lang="ar" dir="rtl"><body><div className="shell"><aside className="side"><div className="brand"><span className="logo">ERP</span><span>Cloud SaaS ERP</span></div><nav className="nav" aria-label="admin sections">{sections.map((section) => <Link href={section.href} key={section.key}>{section.labelAr}<br /><small>{section.labelEn}</small></Link>)}</nav></aside><main className="main"><header className="topbar"><div><strong>المستأجر التجريبي</strong><p className="muted">فرع: الرئيسي · Asia/Riyadh · SAR</p></div><div className="row"><button className="btn" type="button">AR / EN</button><button className="btn" type="button">تبديل الفرع</button><button className="btn primary" type="button">حفظ Ctrl+S</button></div></header>{children}</main></div></body></html>;
}
