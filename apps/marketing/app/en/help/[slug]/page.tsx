import type { Metadata } from 'next';
import { notFound } from 'next/navigation';

import { ArticleView } from '../../../../components/site/views';
import { fetchPage } from '../../../../lib/content';
import { contentMetadata } from '../../../../lib/meta';

export const dynamic = 'force-dynamic';

export async function generateMetadata({ params }: { params: Promise<{ slug: string }> }): Promise<Metadata> {
  const { slug } = await params;
  const page = await fetchPage(slug);
  if (!page || page.kind !== 'help') {
    return { title: 'Not found', description: 'Article not found', robots: { index: false, follow: false } };
  }
  return contentMetadata({
    locale: 'en',
    page,
    path: `/en/help/${slug}`,
    fallbackTitle: 'Help center',
    fallbackDescription: 'An article from the help center',
  });
}

export default async function HelpArticlePageEn({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const page = await fetchPage(slug);
  if (!page || page.kind !== 'help') notFound();
  return (
    <ArticleView
      locale="en"
      page={page}
      basePath="/en/help"
      baseLabel="Help center"
      trail={[
        { href: '/en', label: 'Home' },
        { href: '/en/help', label: 'Help center' },
      ]}
    />
  );
}
