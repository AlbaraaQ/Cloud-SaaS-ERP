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

  /** P-C1: the sidebar is a real tree, and the four groups are the plan's. */
  it('renders the shell from the navigation tree', () => {
    const shell = readFileSync(join(testDir, '..', 'components', 'platform-guard.tsx'), 'utf8');
    expect(shell).toContain('visibleConsoleGroups');
    expect(shell).toContain('canConsole');
    expect(shell).toContain('Ctrl+K');
  });
});
