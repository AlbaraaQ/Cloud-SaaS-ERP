import { describe, expect, it } from 'vitest';

import { moneyText, maskParty } from './format.js';
import { portalRoutes, publicRoutes } from './navigation.js';

describe('customer portal contract', () => {
  it('covers public and portal routes from master requirements', () => {
    expect(publicRoutes.map((route) => route.key)).toContain('verify');
    expect(portalRoutes.map((route) => route.key)).toEqual(['portal','invoices','statement','payments','profile','notifications','quick-sale','stock','tasks','onboarding']);
  });
  it('formats money and masks verification PII', () => {
    expect(moneyText('10.125', 'SAR', 'en-US')).toContain('10.13');
    expect(maskParty('Customer Name')).toBe('Cu***me');
  });
});
