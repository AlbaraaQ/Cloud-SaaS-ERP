import { describe, expect, it } from 'vitest';

import { portalRoutes, publicRoutes } from '../lib/navigation.js';

describe('customer route groups', () => {
  it('separates public and authenticated portal paths', () => {
    expect(publicRoutes.every((route) => !route.href.startsWith('/portal'))).toBe(true);
    expect(portalRoutes.filter((route) => route.href.startsWith('/portal')).length).toBeGreaterThan(7);
  });
});
