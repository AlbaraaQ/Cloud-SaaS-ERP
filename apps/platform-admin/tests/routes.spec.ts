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

  /** The old `/platform/*` prefix must not leak into links after the flattening. */
  it('links no route under the legacy /platform prefix', () => {
    const guard = readFileSync(join(testDir, '..', 'components', 'platform-guard.tsx'), 'utf8');
    expect(guard).not.toContain("'/platform");
    expect(guard).not.toContain('"/platform');
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
});
