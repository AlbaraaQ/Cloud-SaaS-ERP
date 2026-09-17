import type { Metadata } from 'next';

import { HelpIndexView } from '../../../components/site/views';
import { fetchHelp } from '../../../lib/content';
import { staticMetadata } from '../../../lib/meta';

export const dynamic = 'force-dynamic';

export async function generateMetadata(): Promise<Metadata> {
  return staticMetadata({ locale: 'en', path: '/en/help', titleKey: 'help.title', descriptionKey: 'help.subtitle' });
}

export default async function HelpPageEn({ searchParams }: { searchParams: Promise<{ q?: string }> }) {
  const { q } = await searchParams;
  const { items } = await fetchHelp({ q, limit: 30 });
  return <HelpIndexView locale="en" items={items} query={q} />;
}
