import { describe, expect, it, vi } from 'vitest';

import { SalesService } from './sales.service.js';

describe('SalesService phase 10 lifecycle guards', () => {
  it('rejects returns from draft invoices', async () => {
    const service = Object.create(SalesService.prototype) as SalesService;
    service.get = vi.fn().mockResolvedValue({ status: 'draft', kind: 'sale', partyId: 'party-1' }) as never;
    await expect(service.returnFrom('tenant-1', 'invoice-1', {
      branchId: 'branch-1',
      lines: [{ itemId: 'item-1', quantity: '1', unitPrice: '10' }],
    })).rejects.toMatchObject({ code: 'SALES_RETURN_SOURCE_INVALID', status: 409 });
  });

  it('rejects a return that references another return', async () => {
    const service = Object.create(SalesService.prototype) as SalesService;
    service.get = vi.fn().mockResolvedValue({ status: 'posted', kind: 'sale_return', partyId: 'party-1' }) as never;
    await expect(service.returnFrom('tenant-1', 'return-1', {
      branchId: 'branch-1',
      lines: [{ itemId: 'item-1', quantity: '1', unitPrice: '10' }],
    })).rejects.toMatchObject({ code: 'SALES_RETURN_SOURCE_INVALID', status: 422 });
  });

  it('requires a fiscal period before inventory side effects', async () => {
    const service = Object.create(SalesService.prototype) as SalesService;
    service.get = vi.fn().mockResolvedValue({ id: 'invoice-1', status: 'draft', kind: 'sale', branchId: 'branch-1', total: '10' }) as never;
    const record = vi.fn();
    Object.assign(service as unknown as Record<string, unknown>, {
      inventory: { record },
      accounting: { postJournal: vi.fn() },
    });
    await expect(service.post('tenant-1', 'invoice-1', {
      journalLines: [{ accountId: 'account-1', debit: '10' }],
      inventoryLines: [{ itemId: 'item-1', warehouseId: 'warehouse-1', qty: '1', direction: 'out', docType: 'sales_invoice', docId: 'invoice-1', costing: 'outAtAvg' }],
    })).rejects.toMatchObject({ code: 'SALES_FISCAL_PERIOD_REQUIRED', status: 422 });
    expect(record).not.toHaveBeenCalled();
  });
});
