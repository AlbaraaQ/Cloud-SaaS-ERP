'use client';

/**
 * P-M10 — **اختبار أ/ب على البطل** (`docs/roadmap/MARKETING_SITE_PLAN.md` §5): نسختان من عنوان
 * البطل ودعوته، تُداران من نظام المحتوى بلا كود، وتُقاسان بعدد من رآها ومن بدأ اشتراكاً بعدها.
 *
 * **كيف يعمل؟** الصفحة تُرسم بالعنوان الأساسي (من إعدادات الموقع) كما هي اليوم — فالمحتوى
 * يظهر كاملاً بلا انتظار، والزاحف يرى الأساسية دائماً. ثم يقرأ هذا المكوّن **النسخ المنشورة**
 * للصفحة `slug` من `/public/content/<slug>`، ويختار واحدةً بمعرّفه العشوائي (دالّة العقد
 * `pickContentVariant`) في **متصفّحه** — فلا يُرسل المعرّف ليُختار له، ولا يعرف الخادم من رأى
 * ماذا (وهو ما يقيسه السكربت الحيّ).
 *
 * **ومتى لا يفعل شيئاً؟** إن لم تكن ثمّة نسخٌ منشورة، أو كان العنوان نفسه، فلا يتغيّر حرف:
 * التجربة غيابُها هو الأصل. والاختيار يقع بلا موافقةٍ لأنه لا يُرسل شيئاً، أما **الإبلاغ** عن
 * العرض (`experiment_exposure`) فلا يقع إلا بموافقة — لأن الإبلاغ حدثٌ يُخزَّن.
 *
 * **وهو يرسم البطل بنفسه** لا يستقبل دالّة: دالّةٌ لا تُسلَّم من خادمٍ إلى عميل (Next يرفض
 * تمرير الدوالّ عبر الحدّ)، والبديل كان مكوّناً لكل حقلٍ من حقول البطل — وهو تفكيكٌ بلا سبب.
 */

import { useEffect, useState } from 'react';
import Link from 'next/link';
import type { PublicContentVariant } from '@erp/contracts';

import { ensureVisitorId, experimentHeadline, experimentVariant } from '../../lib/analytics';
import { trackGoal } from '../../lib/track';

type Headline = { title: string; ctaLabel: string | null; ctaHref: string | null };

export function HeroExperiment({
  slug,
  title,
  badge,
  ctaLabel,
  ctaHref,
  actions,
}: {
  slug: string;
  title: string;
  badge: string;
  /** الدعوة الأساسية: تُستبدل بدعوة النسخة إن وُجدت لها كتلة `cta`. */
  ctaLabel: string;
  ctaHref: string;
  /** بقيّة الروابط تُرسم كما هي — التجربة تُغيّر الجملة والزرّ الأساسي وحدهما. */
  actions: Array<{ href: string; label: string; className: string }>;
}) {
  const [headline, setHeadline] = useState<Headline>({ title, ctaLabel, ctaHref });

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      try {
        const response = await fetch(`/api/v1/public/content/${encodeURIComponent(slug)}`, {
          headers: { accept: 'application/json' },
        });
        if (!response.ok) return;
        const payload = (await response.json()) as { data?: { variants?: PublicContentVariant[] } };
        const variants = payload.data?.variants ?? [];
        if (variants.length === 0) return;
        // الاختيار محليّ: المعرّف لا يغادر المتصفّح ليُختار له.
        const visitor = ensureVisitorId(globalThis.localStorage ?? null);
        if (!visitor) return;
        const chosen = experimentVariant({ slug, variants, visitor });
        const next = experimentHeadline(chosen);
        if (cancelled || !next) return;
        setHeadline(next);
        trackGoal('experiment_exposure', { experiment: slug, variant: chosen?.key ?? '' });
      } catch {
        // تجربةٌ تفشل صامتةً: الأساسية هي الافتراض، ولا يُقال للزائر شيء.
      }
    };
    void load();
    return () => {
      cancelled = true;
    };
  }, [slug]);

  return (
    <section className="hero">
      <p className="pill">{badge}</p>
      <h1>{headline.title}</h1>
      <div className="toolbar">
        <Link className="btn primary" href={headline.ctaHref ?? ctaHref} data-goal="signup_start">
          {headline.ctaLabel ?? ctaLabel}
        </Link>
        {actions.map((action) => (
          <Link key={`${action.href}-${action.label}`} className={action.className} href={action.href}>
            {action.label}
          </Link>
        ))}
      </div>
    </section>
  );
}
