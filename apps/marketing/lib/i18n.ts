/**
 * P-M1 — العملة (i18n) في الموقع التسويقي: **لغتان حقيقيتان لا ترجمة تجميلية**.
 *
 * القرار: اللغة في **المسار** (`/en/...`) لا في كوكي ولا في `Accept-Language`. ثلاثة أسباب:
 *
 *   1. **محرّكات البحث**: صفحةٌ إنجليزية على `/blog/x` لا تُفهرَس كصفحةٍ إنجليزية إن كان
 *      الإنجليزي مخفيّاً في كوكي — والزائر يشارك رابطاً فيصل عربياً.
 *   2. **`hreflang`**: يُبنى من أزواج المسارات (`buildMetadata` في `lib/site.ts`)، وهو بلا
 *      مسارٍ للغة لا وجود له.
 *   3. **بلا حالة**: لا كوكي يُنسى ولا وميض ترجمة في أول رسم.
 *
 * والنصوص **القابلة للتغيير** لا تسكن هنا: هذه تسمياتٌ ثابتة في القشرة (تنقّل، خطأ، صيانة).
 * أمّا العناوين والفقرات والأسئلة فمن نظام إدارة المحتوى (`lib/content.ts`) — وهذا هو الفرق
 * الذي يجعل تعديل كلمةٍ لا يحتاج نشرة كود.
 */

export type Locale = 'ar' | 'en';

export const LOCALES: readonly Locale[] = ['ar', 'en'];

/** اللغة الأصل للمنصّة (السوق السعودي) — ويُقرأ من إعدادات الموقع عند الحاجة. */
export const DEFAULT_LOCALE: Locale = 'ar';

export function dir(locale: Locale): 'rtl' | 'ltr' {
  return locale === 'ar' ? 'rtl' : 'ltr';
}

export function isLocale(value: string): value is Locale {
  return (LOCALES as readonly string[]).includes(value);
}

/** من مسارٍ (`/en/blog/x`) إلى لغته؛ وما لا يبدأ بـ`/en` عربيّ. */
export function localeFromPath(pathname: string): Locale {
  return pathname === '/en' || pathname.startsWith('/en/') ? 'en' : 'ar';
}

/** المسار النظير في اللغة الأخرى — أساس `hreflang` ومبدّل اللغة. */
export function localePath(pathname: string, locale: Locale): string {
  const bare = localeFromPath(pathname) === 'en' ? pathname.replace(/^\/en(?=\/|$)/, '') || '/' : pathname;
  if (locale === 'ar') return bare;
  return bare === '/' ? '/en' : `/en${bare}`;
}

type Dictionary = Record<string, string>;

const ar: Dictionary = {
  'nav.features': 'الوحدات',
  'nav.einvoicing': 'الفاتورة الإلكترونية',
  'nav.blog': 'المدوّنة',
  'nav.cases': 'دراسات حالة',
  'nav.help': 'مركز المساعدة',
  'nav.pricing': 'الباقات',
  'nav.contact': 'تواصل',
  'nav.verify': 'تحقق من فاتورة',
  'cta.start': 'ابدأ مجاناً',
  'cta.explore': 'شاهد الوحدات',
  'cta.talk': 'تحدّث إلى المبيعات',
  'cta.login': 'دخول',
  'cta.readMore': 'اقرأ المزيد',
  'cta.backToBlog': 'كل المقالات',
  'cta.backToHelp': 'مركز المساعدة',
  'nav.signedIn': 'دخول المنشأة',
  'home.top': 'نظام تخطيط موارد المؤسسات السحابي',
  'home.modules.title': 'وحداتٌ تعمل معاً',
  'home.modules.subtitle': 'ليست تطبيقاتٍ منفصلة: كل وحدةٍ تكتب في الدفتر نفسه وفي المخزون نفسه.',
  'home.steps.title': 'كيف تبدأ في خمس خطوات',
  'home.einvoicing.title': 'الفاتورة الإلكترونية (زاتكا) جاهزة',
  'home.cases.title': 'قالوا عن النظام',
  'home.faq.title': 'أسئلةٌ شائعة',
  'home.final.title': 'جاهز تبدأ؟',
  'home.final.body': 'أنشئ منشأتك في دقائق، ثم أضف فرعك وفريقك — بلا بطاقة ولا التزام.',
  'home.hero.badge': 'مبنيّ للسوق السعودي',
  'features.title': 'الوحدات',
  'features.subtitle': 'ثماني وحداتٍ أساسية، وتعمل على بياناتٍ واحدة.',
  'features.link': 'تفاصيل الوحدة',
  'einvoicing.title': 'الفاتورة الإلكترونية',
  'blog.title': 'المدوّنة',
  'blog.subtitle': 'ما نكتبه عن المحاسبة والتشغيل في السوق السعودي.',
  'blog.empty': 'لا مقالات منشورة بعد — نكتب الأولى الآن.',
  'blog.all': 'الكل',
  'cases.title': 'دراسات حالة',
  'cases.empty': 'لا دراسات حالة منشورة بعد.',
  'help.title': 'مركز المساعدة',
  'help.subtitle': 'مقالاتٌ قصيرة لما يسأل عنه العملاء فعلاً.',
  'help.search': 'ابحث في المساعدة',
  'help.searchCta': 'ابحث',
  'help.empty': 'لا مقال يطابق بحثك.',
  'help.emptyAll': 'لا مقالات منشورة بعد.',
  'help.category': 'التصنيف',
  'error.notFound.title': 'الصفحة غير موجودة',
  'error.notFound.body': 'الرابط الذي طلبته لم يعد موجوداً أو لم يكن يوماً. جرّب من البداية.',
  'error.notFound.cta': 'إلى الصفحة الرئيسية',
  'maintenance.title': 'صيانةٌ مجدولة',
  'maintenance.body': 'نُعيد ترتيب البيت لدقائق. المعاودة قريباً — والأنظمة القائمة لم تتوقف.',
  'footer.product': 'المنتج',
  'footer.content': 'المحتوى',
  'footer.company': 'المنشورات',
  'footer.legal': 'قانوني',
  'footer.contact': 'تواصل',
  'footer.rights': 'جميع الحقوق محفوظة',
  'footer.language': 'اللغة',
  'lang.switch': 'English',
  'banner.dismiss': 'إغلاق',
  'meta.localeName': 'العربية',
};

const en: Dictionary = {
  'nav.features': 'Modules',
  'nav.einvoicing': 'E-invoicing',
  'nav.blog': 'Blog',
  'nav.cases': 'Case studies',
  'nav.help': 'Help center',
  'nav.pricing': 'Pricing',
  'nav.contact': 'Contact',
  'nav.verify': 'Verify invoice',
  'cta.start': 'Start free',
  'cta.explore': 'Explore modules',
  'cta.talk': 'Talk to sales',
  'cta.login': 'Sign in',
  'cta.readMore': 'Read more',
  'cta.backToBlog': 'All articles',
  'cta.backToHelp': 'Help center',
  'nav.signedIn': 'Workspace sign-in',
  'home.top': 'Cloud ERP for growing businesses',
  'home.modules.title': 'Modules that work together',
  'home.modules.subtitle': 'Not separate apps: every module writes to the same ledger and the same stock.',
  'home.steps.title': 'Live in five steps',
  'home.einvoicing.title': 'E-invoicing (ZATCA) ready',
  'home.cases.title': 'What customers say',
  'home.faq.title': 'Frequently asked',
  'home.final.title': 'Ready to start?',
  'home.final.body': 'Create your workspace in minutes, then add your branch and team — no card needed.',
  'home.hero.badge': 'Built for the Saudi market',
  'features.title': 'Modules',
  'features.subtitle': 'Eight core modules on one set of data.',
  'features.link': 'Module details',
  'einvoicing.title': 'E-invoicing',
  'blog.title': 'Blog',
  'blog.subtitle': 'Notes on accounting and operations in the Saudi market.',
  'blog.empty': 'No articles published yet — the first ones are being written.',
  'blog.all': 'All',
  'cases.title': 'Case studies',
  'cases.empty': 'No case studies published yet.',
  'help.title': 'Help center',
  'help.subtitle': 'Short articles for what customers actually ask.',
  'help.search': 'Search the help center',
  'help.searchCta': 'Search',
  'help.empty': 'No article matches your search.',
  'help.emptyAll': 'No articles published yet.',
  'help.category': 'Category',
  'error.notFound.title': 'Page not found',
  'error.notFound.body': 'That link is gone, or was never here. Start from the home page.',
  'error.notFound.cta': 'Go to home page',
  'maintenance.title': 'Scheduled maintenance',
  'maintenance.body': 'We are tidying up for a few minutes. Back shortly — running systems stay up.',
  'footer.product': 'Product',
  'footer.content': 'Content',
  'footer.company': 'Company',
  'footer.legal': 'Legal',
  'footer.contact': 'Contact',
  'footer.rights': 'All rights reserved',
  'footer.language': 'Language',
  'lang.switch': 'العربية',
  'banner.dismiss': 'Dismiss',
  'meta.localeName': 'English',
};

const dictionaries: Record<Locale, Dictionary> = { ar, en };

/** نصٌّ من القاموس الثابت — والمفتاح الغائب يُعيد المفتاح نفسه فلا يظهر فراغٌ صامت. */
export function t(locale: Locale, key: string): string {
  return dictionaries[locale][key] ?? dictionaries[DEFAULT_LOCALE][key] ?? key;
}

/**
 * قاموسٌ كامل للغة — يُستعمل في القشرة التي ترسم عشرات المفاتيح في مكانٍ واحد.
 * (نموذج `copy` القديم بقي للتوافق مع الصفحات القائمة.)
 */
export function dictionary(locale: Locale): Dictionary {
  return { ...dictionaries[DEFAULT_LOCALE], ...dictionaries[locale] };
}

export const copy = {
  ar: {
    title: 'نظام تخطيط موارد المؤسسات السحابي',
    subtitle: 'المحاسبة والمخزون والمبيعات والفواتير الإلكترونية — اشترك وابدأ خلال دقائق.',
    login: 'تسجيل الدخول',
    portal: 'دخول بوابة العملاء',
    verify: 'تحقق من فاتورة',
    contact: 'تواصل معنا',
  },
  en: {
    title: 'Cloud ERP for growing businesses',
    subtitle: 'Accounting, inventory, sales and e-invoicing — subscribe and start in minutes.',
    login: 'Login',
    portal: 'Open customer portal',
    verify: 'Verify invoice',
    contact: 'Contact us',
  },
} as const;
