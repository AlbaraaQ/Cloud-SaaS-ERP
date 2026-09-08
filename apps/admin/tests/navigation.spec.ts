import { existsSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

import { describe, expect, it } from 'vitest';

import { allScreens, findScreenByHref, modules, screenCounts, visibleModules } from '../lib/navigation.js';

const appDir = join(dirname(fileURLToPath(import.meta.url)), '..', 'app');

/** '/sales/invoices?kind=x' -> 'app/sales/invoices/page.tsx' (dynamic segments resolved by hand). */
function pageFileFor(href: string): string {
  const path = href.split('?')[0].split('#')[0].replace(/^\//, '');
  return join(appDir, path, 'page.tsx');
}

describe('admin navigation tree', () => {
  it('mirrors the desktop product modules', () => {
    expect(modules.map((module) => module.key)).toEqual([
      'console',
      'accounting',
      'inventory',
      'purchases',
      'sales',
      'hrm',
      'marina',
      'projects',
      'settings',
      'support',
    ]);
  });

  it('gives every screen a unique key and a route', () => {
    const keys = allScreens.map((screen) => screen.key);
    expect(new Set(keys).size).toBe(keys.length);
    expect(allScreens.every((screen) => screen.href.startsWith('/'))).toBe(true);
  });

  it('routes every unimplemented screen through the scaffold namespace', () => {
    const planned = allScreens.filter((screen) => screen.status !== 'ready');
    expect(planned.every((screen) => screen.href.startsWith('/s/'))).toBe(true);
  });

  it('points every ready screen at a page that actually exists', () => {
    const missing = allScreens
      .filter((screen) => screen.status === 'ready')
      .filter((screen) => !existsSync(pageFileFor(screen.href)))
      .map((screen) => `${screen.key} -> ${screen.href}`);
    expect(missing).toEqual([]);
  });

  it('keeps every ready screen out of the scaffold namespace', () => {
    const ready = allScreens.filter((screen) => screen.status === 'ready');
    expect(ready.some((screen) => screen.href.startsWith('/s/'))).toBe(false);
    expect(ready.length).toBeGreaterThan(60);
  });

  it('resolves a screen from its href, ignoring the query string', () => {
    expect(findScreenByHref('/accounting/accounts')?.key).toBe('coa');
    expect(findScreenByHref('/accounting/journal-entries/new?kind=opening')?.key).toBe('opening-entry');
  });

  it('hides the platform console from non platform admins', () => {
    const asTenantOwner = visibleModules(['*'], false);
    expect(asTenantOwner.some((module) => module.key === 'console')).toBe(false);

    const asOperator = visibleModules(['*'], true);
    expect(asOperator.some((module) => module.key === 'console')).toBe(true);
  });

  it('filters items the user has no permission for', () => {
    const readOnly = visibleModules(['accounting.account.view'], false);
    const accounting = readOnly.find((module) => module.key === 'accounting');
    expect(accounting).toBeDefined();
    const keys = accounting?.groups.flatMap((group) => group.items.map((item) => item.key)) ?? [];
    expect(keys).toContain('coa');
    expect(keys).not.toContain('journal-voucher');
  });

  it('reports honest implementation counts', () => {
    const counts = screenCounts();
    expect(counts.total).toBe(counts.ready + counts.api + counts.planned);
    expect(counts.ready).toBeGreaterThan(0);
  });
});
