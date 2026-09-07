export type Locale = 'ar' | 'en';

export const messages = {
  ar: {
    title: 'لوحة إدارة ERP السحابية',
    subtitle: 'تشغيل المستأجر والعمليات المالية والمخزون والهجرة من شاشة واحدة.',
    search: 'بحث',
    new: 'جديد',
    post: 'ترحيل',
    void: 'إلغاء',
    export: 'تصدير',
    print: 'طباعة',
    save: 'حفظ',
    forbidden: 'ليس لديك صلاحية لهذا القسم',
  },
  en: {
    title: 'Cloud ERP Admin Panel',
    subtitle: 'Tenant operations, finance, inventory, and migration in one workspace.',
    search: 'Search',
    new: 'New',
    post: 'Post',
    void: 'Void',
    export: 'Export',
    print: 'Print',
    save: 'Save',
    forbidden: 'You do not have permission for this section',
  },
} as const;

export function directionOf(locale: Locale): 'rtl' | 'ltr' { return locale === 'ar' ? 'rtl' : 'ltr'; }
export function nextLocale(locale: Locale): Locale { return locale === 'ar' ? 'en' : 'ar'; }
