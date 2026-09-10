'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';

import { useSession } from '../lib/session';

import { Forbidden } from './screen';

const TABS = [
  { href: '/', label: 'نظرة عامة' },
  { href: '/tenants', label: 'العملاء' },
  { href: '/subscriptions', label: 'التراخيص' },
  { href: '/plans', label: 'الباقات' },
  { href: '/activation-requests', label: 'طلبات التفعيل' },
  { href: '/users', label: 'المستخدمون' },
  { href: '/roles', label: 'أدوار المنصة' },
  { href: '/audit', label: 'التدقيق' },
  { href: '/health', label: 'الصحة' },
];

/**
 * Wraps every console page: hard gate on effective platform access (the `pam`
 * claim or any platform role) + a tab strip. Tenant-only sessions — including
 * tenant owners — see `Forbidden` here even if they reach this deployment.
 */
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
