import { existsSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

import { describe, expect, it } from 'vitest';

import { portalRoutes, publicRoutes } from '../lib/navigation.js';

const appDir = fileURLToPath(new URL('../app', import.meta.url));

describe('customer route groups', () => {
  it('separates public and authenticated portal paths', () => {
    expect(publicRoutes.every((route) => !route.href.startsWith('/portal'))).toBe(true);
    expect(portalRoutes.length).toBeGreaterThan(3);
  });

  /** A link in the sidebar that leads to a 404 is worse than no link, so every route must have a page. */
  it('has a page file behind every advertised route', () => {
    for (const route of [...publicRoutes, ...portalRoutes]) {
      const relative = route.href === '/' ? 'page.tsx' : `${route.href.slice(1)}/page.tsx`;
      expect(existsSync(join(appDir, relative)), route.href).toBe(true);
    }
  });
});
