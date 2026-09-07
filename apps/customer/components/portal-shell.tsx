import type { ReactNode } from 'react';
import Link from 'next/link';

import { portalRoutes } from '../lib/navigation';

export function PortalShell({ children }: { children: ReactNode }) {
  return <div className="portal"><aside className="card side"><strong>بوابة الخدمة الذاتية</strong>{portalRoutes.map((route) => <Link href={route.href} key={route.key}>{route.labelAr}<br /><small>{route.labelEn}{route.flag ? ` · ${route.flag}` : ''}</small></Link>)}</aside><section className="grid">{children}</section></div>;
}
