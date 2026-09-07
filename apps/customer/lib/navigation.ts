export type PortalRoute = { key: string; href: string; labelAr: string; labelEn: string; permission?: string; flag?: string };
export const publicRoutes: PortalRoute[] = [
  { key: 'home', href: '/', labelAr: 'الرئيسية', labelEn: 'Home' },
  { key: 'pricing', href: '/pricing', labelAr: 'الأسعار', labelEn: 'Pricing' },
  { key: 'contact', href: '/contact', labelAr: 'تواصل', labelEn: 'Contact' },
  { key: 'verify', href: '/verify', labelAr: 'تحقق', labelEn: 'Verify' },
];
export const portalRoutes: PortalRoute[] = [
  { key: 'portal', href: '/portal', labelAr: 'لوحة العميل', labelEn: 'Dashboard', permission: 'parties.view' },
  { key: 'invoices', href: '/portal/invoices', labelAr: 'فواتيري', labelEn: 'Invoices', permission: 'sales.view', flag: 'portal.customer_access' },
  { key: 'statement', href: '/portal/statement', labelAr: 'كشف الحساب', labelEn: 'Statement', permission: 'parties.view' },
  { key: 'payments', href: '/portal/payments', labelAr: 'المدفوعات', labelEn: 'Payments', permission: 'treasury.view' },
  { key: 'profile', href: '/portal/profile', labelAr: 'طلب تعديل البيانات', labelEn: 'Profile request', permission: 'parties.view' },
  { key: 'notifications', href: '/portal/notifications', labelAr: 'الإشعارات', labelEn: 'Notifications', permission: 'platform.notification.view' },
  { key: 'quick-sale', href: '/portal/quick-sale', labelAr: 'بيع سريع', labelEn: 'Quick sale', permission: 'sales.invoice.create', flag: 'portal.quick_sale' },
  { key: 'stock', href: '/portal/stock', labelAr: 'استعلام مخزون', labelEn: 'Stock lookup', permission: 'inventory.view' },
  { key: 'tasks', href: '/portal/tasks', labelAr: 'المهام', labelEn: 'Tasks', permission: 'platform.notification.view' },
  { key: 'onboarding', href: '/onboarding', labelAr: 'التهيئة', labelEn: 'Onboarding' },
];
