export type PortalRoute = { key: string; href: string; labelAr: string; labelEn: string };

export const publicRoutes: PortalRoute[] = [
  { key: 'home', href: '/', labelAr: 'الرئيسية', labelEn: 'Home' },
  { key: 'pricing', href: '/pricing', labelAr: 'الأسعار', labelEn: 'Pricing' },
  { key: 'contact', href: '/contact', labelAr: 'تواصل', labelEn: 'Contact' },
  { key: 'verify', href: '/verify', labelAr: 'تحقق من فاتورة', labelEn: 'Verify' },
];

/**
 * Split during the 2026-09 surface separation: the authenticated `portalRoutes`
 * registry now lives in `apps/customer-portal/lib/navigation.ts`. This surface
 * advertises public marketing pages only.
 */
