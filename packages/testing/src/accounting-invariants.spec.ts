import { describe, expect, it } from 'vitest';

import { isBalanced, mirrorLines, requiresReversalReason } from './accounting-invariants.js';

describe('accounting invariants', () => {
  it('requires balanced non-zero journals', () => {
    expect(isBalanced([{ debit: '100.00', credit: '0' }, { debit: '0', credit: '100.00' }])).toBe(true);
    expect(isBalanced([{ debit: '100.00', credit: '0' }, { debit: '0', credit: '99.99' }])).toBe(false);
  });

  it('mirrors every debit and credit for reversals', () => {
    expect(mirrorLines([{ debit: '125.50', credit: '0' }, { debit: '0', credit: '125.50' }])).toEqual([
      { debit: '0', credit: '125.50' },
      { debit: '125.50', credit: '0' },
    ]);
  });

  it('requires a reason when reopening or reversing', () => {
    expect(requiresReversalReason('Correction')).toBe(true);
    expect(requiresReversalReason('  ')).toBe(false);
  });
});
