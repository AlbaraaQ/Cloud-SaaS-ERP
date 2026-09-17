import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

import { platformPermissionRegistry } from '@erp/contracts';
import { describe, expect, it } from 'vitest';

import { consoleGroups, consoleItems, groupForPath, visibleConsoleGroups } from '../lib/navigation.js';

/**
 * The console's navigation gate — the same contract the staff surface enforces in
 * `apps/staff/tests/navigation.spec.ts`: **every menu item that says `ready` has a page
 * file behind it, and every item names a console permission that really exists**.
 *
 * P-C1 rebuilt the sidebar around four groups; this suite is what stops the next part from
 * adding a link to a page that was never written.
 */

const testDir = dirname(fileURLToPath(import.meta.url));
const appDir = join(testDir, '..', 'app');

function pageFileFor(href: string): string {
  const relative = href === '/' ? 'page.tsx' : `${href.replace(/^\//, '')}/page.tsx`;
  return join(appDir, relative);
}

const consoleCodes = new Set(platformPermissionRegistry.map((entry) => entry.code));

describe('platform console navigation', () => {
  it('gives every ready item a page file', () => {
    const ready = consoleItems.filter((entry) => entry.status === 'ready');
    expect(ready.length).toBeGreaterThanOrEqual(12);
    for (const entry of ready) {
      expect(existsSync(pageFileFor(entry.href)), `${entry.href} (${entry.labelAr})`).toBe(true);
    }
  });

  it('names only declared console permissions', () => {
    for (const entry of consoleItems) {
      expect(entry.permission.startsWith('console.'), entry.labelAr).toBe(true);
      expect(consoleCodes.has(entry.permission), `${entry.labelAr} → ${entry.permission}`).toBe(true);
    }
  });

  it('declares the four groups the plan asks for', () => {
    expect(consoleGroups.map((group) => group.key)).toEqual(['customers', 'money', 'operations', 'platform']);
    for (const group of consoleGroups) {
      expect(group.labelAr.length).toBeGreaterThan(0);
      expect(group.items.length).toBeGreaterThan(0);
    }
  });

  it('keeps one page per href and one key per item', () => {
    const hrefs = consoleItems.map((entry) => entry.href);
    const keys = consoleItems.map((entry) => entry.key);
    expect(new Set(hrefs).size).toBe(hrefs.length);
    expect(new Set(keys).size).toBe(keys.length);
  });

  it('hides exactly what the permissions deny', () => {
    const support = visibleConsoleGroups(['console.tenants.view', 'console.health.view', 'console.support.manage']);
    const supportHrefs = support.flatMap((group) => group.items.map((entry) => entry.href));
    expect(supportHrefs).toContain('/tenants');
    expect(supportHrefs).not.toContain('/plans');
    expect(supportHrefs).not.toContain('/audit');
    expect(supportHrefs).not.toContain('/users');

    // Nobody sees anything with no codes at all — the console never assumes access.
    expect(visibleConsoleGroups([])).toEqual([]);
  });

  it('resolves the breadcrumb group from the path', () => {
    expect(groupForPath('/audit')?.labelAr).toBe('التشغيل');
    expect(groupForPath('/tenants')?.labelAr).toBe('العملاء');
    expect(groupForPath('/settings')?.labelAr).toBe('المنصة');
    expect(groupForPath('/does-not-exist')).toBeUndefined();
  });

  it('surfaces the console permissions from the session, not the tenant list', () => {
    const session = readFileSync(join(testDir, '..', 'lib', 'session.tsx'), 'utf8');
    expect(session).toContain('platformPermissions');
    expect(session).toContain('canConsole');
  });

  it('ships every console page as a real page file', () => {
    // The eleven pre-P-C1 pages plus إعدادات المنصة.
    const pages = readdirSync(appDir, { withFileTypes: true })
      .filter((entry) => entry.isDirectory())
      .map((entry) => entry.name);
    for (const expected of [
      'tenants',
      'subscriptions',
      'plans',
      'activation-requests',
      'users',
      'roles',
      'audit',
      'health',
      'jobs',
      'settings',
    ]) {
      expect(pages, expected).toContain(expected);
    }
  });
});
