'use client';

import { Screen } from '../../../components/screen';
import { useLang, type Lang } from '../../../lib/i18n';

/**
 * اللغة — the desktop tree's language screen.
 *
 * The preference is stored locally (localStorage) and flips the whole document between
 * RTL/Arabic and LTR/English. The chrome follows it immediately; the navigation labels
 * switch too, because every screen carries both names. Individual screens remain
 * Arabic-first (they mirror Arabic source documents) — see `lib/i18n.tsx`.
 */
export default function LanguagePage() {
  const { lang, setLang, dir } = useLang();

  const option = (value: Lang, titleAr: string, titleEn: string, detail: string) => (
    <label
      className="card"
      style={{
        display: 'flex',
        gap: 12,
        alignItems: 'flex-start',
        cursor: 'pointer',
        outline: lang === value ? '2px solid var(--brand, #4f46e5)' : undefined,
      }}
    >
      <input
        type="radio"
        name="language"
        value={value}
        checked={lang === value}
        onChange={() => setLang(value)}
        style={{ marginTop: 6 }}
      />
      <span>
        <strong>
          {titleAr} — {titleEn}
        </strong>
        <p className="muted" style={{ margin: '4px 0 0' }}>
          {detail}
        </p>
      </span>
    </label>
  );

  return (
    <Screen
      title="اللغة"
      subtitle="Language"
      crumbs={['الإعدادات', 'عامة']}
    >
      <section className="card" dir={dir}>
        <div className="grid" style={{ gap: 12 }}>
          {option('ar', 'العربية', 'Arabic', 'اتجاه الكتابة من اليمين إلى اليسار (RTL). هذا هو الوضع الافتراضي للنظام.')}
          {option('en', 'الإنجليزية', 'English', 'Left-to-right (LTR). Shell, menus and the login screen switch; screen content stays Arabic-first.')}
        </div>
      </section>

      <section className="card">
        <strong>{lang === 'ar' ? 'معاينة' : 'Preview'}</strong>
        <p className="muted">
          {lang === 'ar'
            ? 'هكذا تظهر النصوص العامة في النظام: الرئيسية، تسجيل الخروج، رسائل الدخول، وحالات الشاشات.'
            : 'This is how shared chrome text renders: Home, Sign out, login messages and screen states.'}
        </p>
        <p className="alert" role="note">
          {lang === 'ar'
            ? 'تنطبق اللغة على واجهة النظام العامة وقوائم التنقل وشاشة الدخول، بينما تبقى بيانات المستندات والتقارير بلغتها الأصلية.'
            : 'The preference applies to the shared chrome, navigation and login screen; document and report content keeps its original language.'}
        </p>
      </section>
    </Screen>
  );
}
