export type Locale = 'ar' | 'en';
export const copy = {
  ar: { title: 'بوابة العملاء ERP', subtitle: 'فواتيرك، كشف الحساب، المدفوعات، والإشعارات في مكان واحد.', login: 'تسجيل الدخول', portal: 'دخول البوابة', verify: 'تحقق من فاتورة', contact: 'تواصل معنا' },
  en: { title: 'ERP Customer Portal', subtitle: 'Invoices, statements, payments, and notifications in one place.', login: 'Login', portal: 'Open portal', verify: 'Verify invoice', contact: 'Contact us' },
} as const;
export function dir(locale: Locale): 'rtl' | 'ltr' { return locale === 'ar' ? 'rtl' : 'ltr'; }
