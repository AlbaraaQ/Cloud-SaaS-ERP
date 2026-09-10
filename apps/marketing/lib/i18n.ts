export type Locale = 'ar' | 'en';
export const copy = {
  ar: { title: 'نظام تخطيط موارد المؤسسات السحابي', subtitle: 'المحاسبة والمخزون والمبيعات والفواتير الإلكترونية — اشترك وابدأ خلال دقائق.', login: 'تسجيل الدخول', portal: 'دخول بوابة العملاء', verify: 'تحقق من فاتورة', contact: 'تواصل معنا' },
  en: { title: 'Cloud ERP for growing businesses', subtitle: 'Accounting, inventory, sales and e-invoicing — subscribe and start in minutes.', login: 'Login', portal: 'Open customer portal', verify: 'Verify invoice', contact: 'Contact us' },
} as const;
export function dir(locale: Locale): 'rtl' | 'ltr' { return locale === 'ar' ? 'rtl' : 'ltr'; }
