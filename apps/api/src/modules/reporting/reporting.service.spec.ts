import { describe, expect, it } from 'vitest';

import { REPORT_KEYS, ReportingService } from './reporting.service.js';

describe('reporting phase 14 registry', () => {
  it('registers the phase 14 v1 report catalog', () => {
    expect(REPORT_KEYS).toContain('sales-by-day');
    expect(REPORT_KEYS).toContain('inventory-valuation');
    expect(REPORT_KEYS).toContain('cashier-shift');
    expect(REPORT_KEYS.length).toBeGreaterThanOrEqual(20);
  });

  it('renders sanitized print HTML shells', () => {
    const service = new ReportingService();
    expect(service.invoicePrintHtml('<x>')).toContain('&lt;x&gt;');
  });
});
