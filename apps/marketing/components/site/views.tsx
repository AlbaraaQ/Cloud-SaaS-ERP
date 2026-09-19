/**
 * P-M1 · P-M2 — أجسام الصفحات.
 *
 * كل صفحةٍ هنا مكوّنٌ **خادميّ** يأخذ لغته وبيناته من ملفّ مسارٍ رفيع (thin route)؛
 * فملفّات الصفحات تبقى أسطراً قليلة، والمنطق واحد لا يُكرَّر بين `/blog` و`/en/blog`.
 *
 * وقاعدة الخطّة §3.1 محفوظة في كل قسم: **لا رقمٌ ولا شهادةٌ مخترعة**. الأقسام التي مصدرها
 * نظام المحتوى (آراء، أسئلة، مقالات) تُخفي نفسها حين لا محتوى؛ والأقسام التي مصدرها المنتج
 * (الوحدات، الخطوات) تسمياتُها من شجرة `apps/staff` مع ذكر الملف والسطر في `lib/modules.ts`.
 */

import Link from 'next/link';

import {
  summaryFor,
  titleFor,
  type ContentPageDetail,
  type ContentPageSummary,
  type FaqItem,
} from '../../lib/content';
import { t, type Locale } from '../../lib/i18n';
import { industries, industryPath, INDUSTRIES_PATH, type Industry } from '../../lib/industries';
import { einvoicingPoints, onboardingSteps, siteModules } from '../../lib/modules';
import { trustAxes, trustLimitsAr } from '../../lib/trust';
import { hrefFor, SITE_PATHS } from '../../lib/site';

import { ContentBlocks } from './blocks';
import { Breadcrumbs, EmptyState, FaqList, ModuleCard, PostCard, SectionHeading, StepList } from './pieces';

type ShellLike = { taglineAr: string; taglineEn: string; brandName: string };

export function HomeView({
  locale,
  shell,
  faq,
  cases,
}: {
  locale: Locale;
  shell: ShellLike;
  faq: FaqItem[];
  cases: ContentPageSummary[];
}) {
  const l = (path: string) => hrefFor(path, locale);
  // عنوان البطل من **إعدادات الموقع** (`site.tagline_ar` في `/public/site`) لا من نصٍّ في
  // الكود: تغييره من شاشة الإعدادات في اللوحة يكفي، بلا نشرة.
  const heroTitle = locale === 'en' ? shell.taglineEn : shell.taglineAr;
  const heroSummary = '';

  return (
    <>
      <section className="hero">
        <p className="pill">{t(locale, 'home.hero.badge')}</p>
        <h1>{heroTitle}</h1>
        {heroSummary ? <p className="hero-lead">{heroSummary}</p> : null}
        <div className="toolbar">
          <Link className="btn primary" href="/onboarding">
            {t(locale, 'cta.start')}
          </Link>
          <Link className="btn" href={l(SITE_PATHS.features)}>
            {t(locale, 'cta.explore')}
          </Link>
          <Link className="btn ghost" href={l(SITE_PATHS.help)}>
            {t(locale, 'nav.help')}
          </Link>
        </div>
      </section>

      <section className="section">
        <SectionHeading
          title={t(locale, 'home.modules.title')}
          subtitle={t(locale, 'home.modules.subtitle')}
          action={{ href: l(SITE_PATHS.features), label: t(locale, 'cta.explore') }}
        />
        <div className="grid cols">
          {siteModules.map((module) => (
            <ModuleCard key={module.key} module={module} locale={locale} />
          ))}
        </div>
      </section>

      <section className="section">
        <SectionHeading title={t(locale, 'home.steps.title')} />
        <StepList steps={onboardingSteps} locale={locale} />
      </section>

      <section className="section einvoicing-band">
        <SectionHeading
          title={t(locale, 'home.einvoicing.title')}
          action={{ href: l(SITE_PATHS.einvoicing), label: t(locale, 'cta.readMore') }}
        />
        <div className="grid cols">
          {einvoicingPoints.map((point) => (
            <article className="card" key={point.titleAr}>
              <h3>{locale === 'en' ? point.titleEn : point.titleAr}</h3>
              <p className="muted">{locale === 'en' ? point.bodyEn : point.bodyAr}</p>
            </article>
          ))}
        </div>
        <p>
          <Link className="text-link" href={locale === 'ar' ? SITE_PATHS.verify : '/verify'}>
            {locale === 'ar' ? 'تحقّق من فاتورة الآن' : 'Verify an invoice now'}
          </Link>
        </p>
      </section>

      {cases.length > 0 ? (
        <section className="section">
          <SectionHeading
            title={t(locale, 'home.cases.title')}
            action={{ href: l(SITE_PATHS.cases), label: t(locale, 'cta.readMore') }}
          />
          <div className="grid cols">
            {cases.slice(0, 3).map((item) => (
              <PostCard key={item.slug} post={item} locale={locale} basePath={l(SITE_PATHS.cases)} />
            ))}
          </div>
        </section>
      ) : null}

      {faq.length > 0 ? (
        <section className="section">
          <SectionHeading title={t(locale, 'home.faq.title')} />
          <FaqList items={faq} />
        </section>
      ) : null}

      <section className="section cta-final">
        <h2>{t(locale, 'home.final.title')}</h2>
        <p className="muted">{t(locale, 'home.final.body')}</p>
        <div className="toolbar">
          <Link className="btn primary" href="/onboarding">
            {t(locale, 'cta.start')}
          </Link>
          <a className="btn" href="mailto:">
            {t(locale, 'cta.talk')}
          </a>
        </div>
      </section>
    </>
  );
}

export function FeaturesView({ locale, page }: { locale: Locale; page: ContentPageDetail | null }) {
  return (
    <>
      <header className="page-head">
        <h1>{page ? titleFor(page, locale) : t(locale, 'features.title')}</h1>
        <p className="muted">{page ? summaryFor(page, locale) : t(locale, 'features.subtitle')}</p>
      </header>
      {page && page.blocks.length > 0 ? <ContentBlocks blocks={page.blocks} locale={locale} /> : null}
      <section className="section">
        <div className="grid cols">
          {siteModules.map((module) => (
            <article className="card module-card" key={module.key}>
              <span className="card-icon" aria-hidden="true">
                {module.icon}
              </span>
              <h3>{locale === 'en' ? module.labelEn : module.labelAr}</h3>
              <p className="muted">{locale === 'en' ? module.blurbEn : module.blurbAr}</p>
              <p className="source-line" dir="ltr">
                {module.source}
              </p>
            </article>
          ))}
        </div>
      </section>
      <section className="section einvoicing-band">
        <SectionHeading
          title={t(locale, 'einvoicing.title')}
          action={{ href: hrefFor(SITE_PATHS.einvoicing, locale), label: t(locale, 'features.link') }}
        />
      </section>
    </>
  );
}

export function EinvoicingView({ locale, page }: { locale: Locale; page: ContentPageDetail | null }) {
  return (
    <>
      <header className="page-head">
        <h1>{page ? titleFor(page, locale) : t(locale, 'einvoicing.title')}</h1>
        {page ? <p className="muted">{summaryFor(page, locale)}</p> : null}
      </header>
      {page && page.blocks.length > 0 ? (
        <ContentBlocks blocks={page.blocks} locale={locale} />
      ) : (
        <div className="grid cols">
          {einvoicingPoints.map((point) => (
            <article className="card" key={point.titleAr}>
              <h3>{locale === 'en' ? point.titleEn : point.titleAr}</h3>
              <p className="muted">{locale === 'en' ? point.bodyEn : point.bodyAr}</p>
            </article>
          ))}
        </div>
      )}
      <section className="section cta-final">
        <h2>{locale === 'ar' ? 'تحقّق من فاتورة' : 'Verify an invoice'}</h2>
        <p className="muted">
          {locale === 'ar'
            ? 'أدخل رقم الفاتورة أو المسح الضوئي لرمز QR — بلا حساب وبلا بيانات.'
            : 'Enter the invoice number or paste the QR payload — no account needed.'}
        </p>
        <Link className="btn primary" href="/verify">
          {t(locale, 'nav.verify')}
        </Link>
      </section>
    </>
  );
}

export function BlogIndexView({
  locale,
  posts,
  categories,
  activeCategory,
}: {
  locale: Locale;
  posts: ContentPageSummary[];
  categories: string[];
  activeCategory?: string;
}) {
  const base = hrefFor(SITE_PATHS.blog, locale);
  return (
    <>
      <header className="page-head">
        <h1>{t(locale, 'blog.title')}</h1>
        <p className="muted">{t(locale, 'blog.subtitle')}</p>
      </header>
      {categories.length > 0 ? (
        <nav className="chip-row" aria-label={t(locale, 'help.category')}>
          <Link className={activeCategory ? 'chip' : 'chip active'} href={base}>
            {t(locale, 'blog.all')}
          </Link>
          {categories.map((category) => (
            <Link
              className={category === activeCategory ? 'chip active' : 'chip'}
              key={category}
              href={`${base}?category=${encodeURIComponent(category)}`}
            >
              {category}
            </Link>
          ))}
        </nav>
      ) : null}
      {posts.length === 0 ? (
        <EmptyState label={t(locale, 'blog.empty')} />
      ) : (
        <div className="grid cols">
          {posts.map((post) => (
            <PostCard key={post.slug} post={post} locale={locale} basePath={base} />
          ))}
        </div>
      )}
    </>
  );
}

export function HelpIndexView({
  locale,
  items,
  query,
}: {
  locale: Locale;
  items: ContentPageSummary[];
  query?: string;
}) {
  const base = hrefFor(SITE_PATHS.help, locale);
  return (
    <>
      <header className="page-head">
        <h1>{t(locale, 'help.title')}</h1>
        <p className="muted">{t(locale, 'help.subtitle')}</p>
      </header>
      <form className="search-row" action={base} method="get" role="search">
        <label className="sr-only" htmlFor="help-q">
          {t(locale, 'help.search')}
        </label>
        <input
          className="input"
          id="help-q"
          name="q"
          type="search"
          defaultValue={query ?? ''}
          placeholder={t(locale, 'help.search')}
        />
        <button className="btn primary" type="submit">
          {t(locale, 'help.searchCta')}
        </button>
      </form>
      {items.length === 0 ? (
        <EmptyState label={query ? t(locale, 'help.empty') : t(locale, 'help.emptyAll')} />
      ) : (
        <div className="grid cols">
          {items.map((item) => (
            <PostCard key={item.slug} post={item} locale={locale} basePath={base} />
          ))}
        </div>
      )}
    </>
  );
}

export function ArticleView({
  locale,
  page,
  basePath,
  baseLabel,
  trail,
}: {
  locale: Locale;
  page: ContentPageDetail;
  basePath: string;
  baseLabel: string;
  trail?: Array<{ href: string; label: string }>;
}) {
  return (
    <article className="article">
      <Breadcrumbs trail={trail ?? [{ href: locale === 'ar' ? '/' : '/en', label: locale === 'ar' ? 'الرئيسية' : 'Home' }, { href: basePath, label: baseLabel }]} />
      <header className="page-head">
        {page.category ? <span className="pill">{page.category}</span> : null}
        <h1>{titleFor(page, locale)}</h1>
        <p className="meta-line">
          {page.authorName ? <span>{page.authorName}</span> : null}
          {page.publishedAt ? <time dateTime={page.publishedAt}>{page.publishedAt.slice(0, 10)}</time> : null}
        </p>
      </header>
      <ContentBlocks blocks={page.blocks} locale={locale} />
      <p className="back-link">
        <Link className="text-link" href={basePath}>
          ← {baseLabel}
        </Link>
      </p>
    </article>
  );
}

export function LegalView({ locale, page }: { locale: Locale; page: ContentPageDetail }) {
  return (
    <article className="article legal">
      <h1>{titleFor(page, locale)}</h1>
      {page.blocks.length > 0 ? <ContentBlocks blocks={page.blocks} locale={locale} /> : null}
    </article>
  );
}

export function CasesIndexView({ locale, cases }: { locale: Locale; cases: ContentPageSummary[] }) {
  const base = hrefFor(SITE_PATHS.cases, locale);
  return (
    <>
      <header className="page-head">
        <h1>{t(locale, 'cases.title')}</h1>
      </header>
      {cases.length === 0 ? (
        <EmptyState label={t(locale, 'cases.empty')} />
      ) : (
        <div className="grid cols">
          {cases.map((item) => (
            <PostCard key={item.slug} post={item} locale={locale} basePath={base} />
          ))}
        </div>
      )}
    </>
  );
}

/**
 * P-M8 — `/trust`: كل بندٍ ومعه مصدره.
 *
 * والشكل تابعٌ للقرار: البطاقة تحمل **الدليل** في سطرٍ صغير (`source-line`) لا في حاشية، لأن
 * صفحةَ ثقةٍ بلا دليلٍ صفحةُ إعلان. و«ما لا ندّعيه» قسمٌ كامل في آخرها، لا حاشيةً صغيرة: حدُّ
 * المنتج جزءٌ من وصفه.
 *
 * وصفحةُ المحتوى (من نظام إدارة المحتوى) تُعرض **فوق** الأقسام الثابتة إن وُجدت: من كتب نصّاً
 * تحريرياً في اللوحة لا يُنازع الأرقام الثابتة، بل يقدّم لها مقدّمة.
 */
export function TrustView({ locale, page }: { locale: Locale; page: ContentPageDetail | null }) {
  return (
    <>
      <header className="page-head">
        <h1>{page ? titleFor(page, locale) : t(locale, 'trust.title')}</h1>
        <p className="muted">{page ? summaryFor(page, locale) : t(locale, 'trust.subtitle')}</p>
      </header>
      {page && page.blocks.length > 0 ? <ContentBlocks blocks={page.blocks} locale={locale} /> : null}

      {trustAxes.map((axis) => (
        <section className="section" key={axis.key}>
          <SectionHeading title={`${axis.icon} ${axis.titleAr}`} subtitle={axis.leadAr} />
          <div className="grid cols">
            {axis.points.map((point) => (
              <article className="card" key={point.titleAr}>
                <h3>{point.titleAr}</h3>
                <p className="muted">{point.bodyAr}</p>
                <p className="source-line" dir="ltr">
                  {point.source}
                </p>
              </article>
            ))}
          </div>
        </section>
      ))}

      <section className="section">
        <SectionHeading title={t(locale, 'trust.limits.title')} subtitle={t(locale, 'trust.limits.subtitle')} />
        <ul className="verify-notes">
          {trustLimitsAr.map((line) => (
            <li key={line}>{line}</li>
          ))}
        </ul>
      </section>

      <section className="section cta-final">
        <h2>{t(locale, 'trust.final.title')}</h2>
        <p className="muted">{t(locale, 'trust.final.body')}</p>
        <div className="toolbar">
          <Link className="btn primary" href={hrefFor(SITE_PATHS.verify, locale)}>
            {t(locale, 'nav.verify')}
          </Link>
          <Link className="btn" href={hrefFor(SITE_PATHS.contact, locale)}>
            {t(locale, 'nav.contact')}
          </Link>
        </div>
      </section>
    </>
  );
}

/** P-M8 — `/industries`: البطاقة تقول **الوحدة التي تُباع**، لا شعاراً عن «حلولٍ متكاملة». */
export function IndustriesView({ locale, page }: { locale: Locale; page: ContentPageDetail | null }) {
  return (
    <>
      <header className="page-head">
        <h1>{page ? titleFor(page, locale) : t(locale, 'industries.title')}</h1>
        <p className="muted">{page ? summaryFor(page, locale) : t(locale, 'industries.subtitle')}</p>
      </header>
      {page && page.blocks.length > 0 ? <ContentBlocks blocks={page.blocks} locale={locale} /> : null}
      <div className="grid cols">
        {industries.map((industry) => (
          <article className="card module-card" key={industry.slug}>
            <span className="card-icon" aria-hidden="true">
              {industry.icon}
            </span>
            <h3>{industry.labelAr}</h3>
            <p className="muted">{industry.summaryAr}</p>
            <p>
              <Link className="text-link" href={industryPath(industry.slug)}>
                {t(locale, 'industries.open')}
              </Link>
            </p>
            <p className="source-line" dir="ltr">
              {industry.module.source}
            </p>
          </article>
        ))}
      </div>
    </>
  );
}

/**
 * P-M8 — صفحة قطاع: **لا وعدَ بلا شاشة**.
 *
 * كل سطرٍ في «الشاشات» مقابله شاشةٌ في تطبيق العمل بتسميتها الحرفية ومسارها وملفّها وسطرها؛
 * ومن قرأ سطراً ولم يجد شاشةً وراءه، فذلك خطأٌ في هذه الصفحة لا في المنتج. وهذا هو الفرق بين
 * صفحة قطاعٍ حقيقية وصفحةٍ تُنقل من موقعٍ آخر بأسماءٍ أخرى.
 */
export function IndustryView({ locale, industry }: { locale: Locale; industry: Industry }) {
  return (
    <>
      <Breadcrumbs
        trail={[
          { href: hrefFor('/', locale), label: t(locale, 'nav.home') },
          { href: hrefFor(INDUSTRIES_PATH, locale), label: t(locale, 'industries.title') },
          { href: industryPath(industry.slug), label: industry.labelAr },
        ]}
      />

      <header className="page-head">
        <h1>
          <span aria-hidden="true">{industry.icon}</span> {industry.labelAr}
        </h1>
        <p className="muted">{industry.summaryAr}</p>
        <p className="source-line" dir="ltr">
          {industry.module.label} — {industry.module.source}
        </p>
      </header>

      <section className="section">
        <SectionHeading title={t(locale, 'industries.pains.title')} />
        <ul className="industry-pains">
          {industry.pictureAr.map((line) => (
            <li key={line}>{line}</li>
          ))}
        </ul>
      </section>

      <section className="section">
        <SectionHeading
          title={t(locale, 'industries.screens.title')}
          subtitle={t(locale, 'industries.screens.subtitle')}
        />
        <div className="grid cols">
          {industry.screens.map((screen) => (
            <article className="card industry-card" key={screen.label}>
              <h3>{screen.label}</h3>
              <p className="muted">{screen.whatAr}</p>
              <p className="source-line" dir="ltr">
                {screen.href} · {screen.source}
              </p>
            </article>
          ))}
        </div>
      </section>

      <section className="section">
        <SectionHeading title={t(locale, 'industries.loop.title')} />
        <ol className="verify-notes">
          {industry.loopAr.map((line) => (
            <li key={line}>{line}</li>
          ))}
        </ol>
      </section>

      <section className="section cta-final">
        <h2>{t(locale, 'industries.final.title')}</h2>
        <p className="muted">{t(locale, 'industries.final.body')}</p>
        <div className="toolbar">
          <Link className="btn primary" href={hrefFor(SITE_PATHS.demo, locale)}>
            {t(locale, 'nav.demo')}
          </Link>
          <Link className="btn" href={hrefFor(SITE_PATHS.trust, locale)}>
            {t(locale, 'nav.trust')}
          </Link>
          <Link className="btn" href={hrefFor(SITE_PATHS.verify, locale)}>
            {t(locale, 'nav.verify')}
          </Link>
        </div>
      </section>
    </>
  );
}

export function RouteNotFound({ locale }: { locale: Locale }) {
  return (
    <section className="not-found">
      <p className="error-code" aria-hidden="true">
        404
      </p>
      <h1>{t(locale, 'error.notFound.title')}</h1>
      <p className="muted">{t(locale, 'error.notFound.body')}</p>
      <Link className="btn primary" href={hrefFor('/', locale)}>
        {t(locale, 'error.notFound.cta')}
      </Link>
    </section>
  );
}

export function MaintenanceView({ locale }: { locale: Locale }) {
  return (
    <section className="not-found">
      <h1>{t(locale, 'maintenance.title')}</h1>
      <p className="muted">{t(locale, 'maintenance.body')}</p>
      <Link className="btn" href={hrefFor(SITE_PATHS.help, locale)}>
        {t(locale, 'nav.help')}
      </Link>
    </section>
  );
}
