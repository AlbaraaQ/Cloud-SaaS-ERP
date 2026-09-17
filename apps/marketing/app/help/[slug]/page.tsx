import type { Metadata } from 'next';
import { notFound } from 'next/navigation';

import { ArticleView } from '../../../components/site/views';
import { fetchPage } from '../../../lib/content';
import { contentMetadata } from '../../../lib/meta';

export const dynamic = 'force-dynamic';

export async function generateMetadata({ params }: { params: Promise<{ slug: string }> }): Promise<Metadata> {
  const { slug } = await params;
  const page = await fetchPage(slug);
  // مقالٌ غير منشور أو غير موجود: لا تُبنى له ميتاداتا إطلاقاً — يُمنع فهرسه ويُعرض 404.
  if (!page || page.kind !== 'help') {
    return { title: 'غير موجود', description: 'المقال غير موجود', robots: { index: false, follow: false } };
  }
  return contentMetadata({
    locale: 'ar',
    page,
    path: `/help/${slug}`,
    fallbackTitle: 'مركز المساعدة',
    fallbackDescription: 'مقالٌ من مركز المساعدة',
  });
}

export default async function HelpArticlePage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const page = await fetchPage(slug);
  if (!page || page.kind !== 'help') notFound();
  return <ArticleView locale="ar" page={page} basePath="/help" baseLabel="مركز المساعدة" />;
}
