import type { Metadata } from 'next';

import { HelpIndexView } from '../../components/site/views';
import { fetchHelp } from '../../lib/content';
import { staticMetadata } from '../../lib/meta';

export const dynamic = 'force-dynamic';

export async function generateMetadata(): Promise<Metadata> {
  return staticMetadata({ locale: 'ar', path: '/help', titleKey: 'help.title', descriptionKey: 'help.subtitle' });
}

export default async function HelpPage({ searchParams }: { searchParams: Promise<{ q?: string }> }) {
  const { q } = await searchParams;
  const { items } = await fetchHelp({ q, limit: 30 });
  return <HelpIndexView locale="ar" items={items} query={q} />;
}
