import { describe, expect, it } from 'vitest';

import { buildQrPayload, buildZatcaUbl, decryptSecret, encryptSecret } from './einvoicing.service.js';

describe('einvoicing phase 13 helpers', () => {
  it('encrypts secrets and fails closed with the wrong key', () => {
    const encrypted = encryptSecret('super-secret-csid', 'key-a');
    expect(encrypted).not.toContain('super-secret-csid');
    expect(decryptSecret(encrypted, 'key-a')).toBe('super-secret-csid');
    expect(() => decryptSecret(encrypted, 'key-b')).toThrow();
  });

  it('builds deterministic UBL and TLV QR payloads for fixtures', () => {
    const ubl = buildZatcaUbl({ id: 'invoice-1', number: 'SI-000001', kind: 'sale', total: '115.0000', taxTotal: '15.0000', postedAt: new Date('2026-09-07T00:00:00Z') });
    expect(ubl).toContain('<ID>SI-000001</ID>');
    expect(ubl).toContain('<PayableAmount>115.0000</PayableAmount>');
    expect(buildQrPayload({ seller: 'Demo', vatNo: '123', timestamp: '2026-09-07T00:00:00Z', total: '115.00', vat: '15.00' })).toMatch(/^[A-Za-z0-9+/=]+$/);
  });
});
