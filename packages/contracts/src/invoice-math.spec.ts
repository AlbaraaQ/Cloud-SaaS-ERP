import { describe, expect, it } from 'vitest';

import { calculateInvoiceTotals } from './invoice-math.js';

describe('calculateInvoiceTotals', () => {
  it('calculates exclusive VAT and line discounts with decimal precision', () => {
    const result = calculateInvoiceTotals({ lines: [{ quantity: '2.5', unitPrice: '10.00', discountRate: '10', discountAmount: '0', taxRate: '15' }] });
    expect(result.subtotal).toBe('22.5');
    expect(result.tax).toBe('3.375');
    expect(result.total).toBe('25.875');
  });

  it('extracts inclusive VAT without floating point drift', () => {
    const result = calculateInvoiceTotals({ priceIncludesVat: true, lines: [{ quantity: '1', unitPrice: '115', discountRate: '0', discountAmount: '0', taxRate: '15' }] });
    expect(result.tax).toBe('15');
    expect(result.total).toBe('115');
  });

  it('applies document discount, extra tax, and withholding', () => {
    const result = calculateInvoiceTotals({ invoiceDiscount: '5', extraTax: '2', withholding: '3', lines: [{ quantity: '1', unitPrice: '100', discountRate: '0', discountAmount: '0', taxRate: '0' }] });
    expect(result.total).toBe('94');
  });

  it('rounds half up at the configured precision', () => {
    const result = calculateInvoiceTotals({ scale: 2, lines: [{ quantity: '1', unitPrice: '1.005', discountRate: '0', discountAmount: '0', taxRate: '0' }] });
    expect(result.subtotal).toBe('1.01');
  });
});
