'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';

import { useSession } from '../lib/session';

import { Forbidden } from './screen';

const TABS = [
  { href: '/platform', label: 'نظرة عامة' },
  { href: '/platform/tenants', label: 'العملاء' },
  { href: '/platform/subscriptions', label: 'التراخيص' },
  { href: '/platform/plans', label: 'الباقات' },
  { href: '/platform/activation-requests', label: 'طلبات التفعيل' },
  { href: '/platform/users', label: 'المستخدمون' },
  { href: '/platform/audit', label: 'التدقيق' },
  { href: '/platform/health', label: 'الصحة' },
];

/** Wraps every /platform page: hard gate on the `pam` claim + a tab strip. */
export function PlatformGuard({ children }: { children: React.ReactNode }) {
  const { isPlatformAdmin } = useSession();
  const pathname = usePathname() ?? '';

  if (!isPlatformAdmin) return <Forbidden />;

  return (
    <div className="grid">
      <nav className="card tight chips no-print" aria-label="أقسام لوحة المنصة">
        {TABS.map((tab) => (
          <Link key={tab.href} href={tab.href} className={`chip ${pathname === tab.href ? 'on' : ''}`}>
            {tab.label}
          </Link>
        ))}
      </nav>
      {children}
    </div>
  );
}
