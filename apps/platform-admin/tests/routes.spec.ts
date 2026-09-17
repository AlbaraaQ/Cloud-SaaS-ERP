import { existsSync, readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

import { describe, expect, it } from 'vitest';

const testDir = dirname(fileURLToPath(import.meta.url));
const appDir = join(testDir, '..', 'app');

const CONSOLE_ROUTES = [
  '/',
  '/tenants',
  '/tenants/new',
  '/subscriptions',
  '/plans',
  '/activation-requests',
  '/users',
  '/roles',
  '/audit',
  '/health',
  '/jobs',
  // P-C1 — the settings screen the console writes through `PUT /platform/settings`.
  '/settings',
  // P-C2 — بطاقة العميل: one dynamic route, reached from the customers list.
  '/tenants/[id]',
];

function pageFileFor(href: string): string {
  const relative = href === '/' ? 'page.tsx' : `${href.replace(/^\//, '')}/page.tsx`;
  return join(appDir, relative);
}

describe('platform console routes', () => {
  it('has a page file behind every console route', () => {
    for (const href of CONSOLE_ROUTES) {
      expect(existsSync(pageFileFor(href)), href).toBe(true);
    }
  });

  /**
   * The old `/platform/*` **page** prefix must not come back after the surface separation.
   *
   * P-C1 made this check precise: the shell legitimately quotes API paths (`/platform/audit`)
   * and they all start with the same word. What must never exist again is a *link* to a page
   * under that prefix, so the scan now looks at `href` values only.
   */
  it('links no page under the legacy /platform prefix', () => {
    const shell = readFileSync(join(testDir, '..', 'components', 'platform-guard.tsx'), 'utf8');
    const hrefs = [...shell.matchAll(/href=(?:"([^"]+)"|\{`([^`]+)`\}|\{'([^']+)'\})/g)]
      .map((match) => match[1] ?? match[2] ?? match[3] ?? '')
      .filter((href) => !href.startsWith('${'));
    expect(hrefs.length).toBeGreaterThan(0);
    for (const href of hrefs) {
      expect(href.startsWith('/platform'), href).toBe(false);
    }
  });

  /** Console login is operator-only: no signup path may exist on this surface. */
  it('offers no self-service signup', () => {
    const login = readFileSync(join(testDir, '..', 'components', 'login-screen.tsx'), 'utf8');
    const code = login.replace(/\/\*[\s\S]*?\*\//g, '').replace(/\/\/.*$/gm, '');
    expect(code).not.toContain('SignupPanel');
    expect(code).not.toContain('/signup');
    expect(code).not.toContain('/onboarding');
    expect(login).not.toContain('إنشاء حساب');
    expect(login).not.toContain('اشترك');
  });

  /**
   * P-C2 — the card is reachable and complete.
   *
   * A dynamic route is only a *real* screen if something links to it, so this checks both
   * halves: the list page links to `/tenants/:id`, and the card carries the plan's eight
   * tabs and calls the endpoints that back them.
   */
  it('links every customer row to its card', () => {
    const list = readFileSync(join(appDir, 'tenants', 'page.tsx'), 'utf8');
    expect(list).toContain('href={`/tenants/${tenant.id}`}');
  });

  it('renders the card with the plan’s tab names, in order', () => {
    const card = readFileSync(join(appDir, 'tenants', '[id]', 'page.tsx'), 'utf8');
    for (const label of [
      'نظرة عامة',
      'الاشتراك',
      'المستخدمون',
      'الاستخدام',
      'الرايات',
      'الصحة',
      'التدقيق',
      'الملاحظات',
    ]) {
      expect(card, label).toContain(label);
    }
    // The order is part of the contract with the plan, not an accident of typing.
    const positions = ['نظرة عامة', 'الاشتراك', 'المستخدمون', 'الاستخدام', 'الرايات', 'الصحة', 'التدقيق', 'الملاحظات'].map(
      (label) => card.indexOf(`label: '${label}'`),
    );
    expect(positions.every((position) => position >= 0)).toBe(true);
    expect([...positions].sort((a, b) => a - b)).toEqual(positions);
  });

  it('calls the P-C2 endpoints and no tenant-plane shortcut', () => {
    const card = readFileSync(join(appDir, 'tenants', '[id]', 'page.tsx'), 'utf8');
    for (const call of [
      '`/platform/tenants/${tenantId}`',
      '`/platform/tenants/${tenantId}/usage`',
      '`/platform/tenants/${tenantId}/health`',
      '`/platform/tenants/${tenantId}/notes`',
      '`/platform/tenants/${tenantId}/settings',
      '`/platform/tenants/${tenantId}/flags`',
      '`/platform/tenants/${tenantId}/branding`',
      '`/platform/tenants/${tenantId}/status`',
      '`/platform/tenants/${tenantId}/owner/transfer`',
    ]) {
      expect(card, call).toContain(call);
    }
    // The card is the platform plane only: it must never call a `/api/v1/tenants/…` route,
    // which is the customer's own surface and carries the tenant session's permissions.
    expect(card).not.toContain('/api/v1/tenant');
  });

  /** P-C1: the sidebar is a real tree, and the four groups are the plan's. */
  it('renders the shell from the navigation tree', () => {
    const shell = readFileSync(join(testDir, '..', 'components', 'platform-guard.tsx'), 'utf8');
    expect(shell).toContain('visibleConsoleGroups');
    expect(shell).toContain('canConsole');
    expect(shell).toContain('Ctrl+K');
  });
});
