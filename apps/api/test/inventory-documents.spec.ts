import { afterAll, beforeAll, describe, expect, it } from 'vitest';

import { OrgProvisioningService } from '../src/modules/organization/provisioning/org-provisioning.service.js';

import {
  ALL_ORGANIZATION_PERMISSIONS,
  ALL_PLATFORM_PERMISSIONS,
  createActor,
  type Actor,
} from './fixtures.js';
import { api } from './http.js';
import { createTestApp, type TestApp } from './test-app.js';

type InventoryDocument = {
  id: string;
  number: string;
  status: string;
  kind: string;
  journalEntryId: string | null;
  lines: Array<{
    id: string;
    itemId: string;
    unitId: string;
    quantity: string;
    baseQuantity: string;
    unitCost: string;
    adjustmentDirection: string | null;
  }>;
};

const data = <T>(body: Record<string, unknown>) => (body.data ?? body) as T;
const code = (body: Record<string, unknown>) =>
  (body.error as { code?: string } | undefined)?.code ?? (body.code as string | undefined);

/**
 * Phase 05's operator-authored stock documents.
 *
 * The desktop's input/output screen stored one user document then made its inventory and
 * entry effects. These checks prove the cloud equivalent retains the selected unit, posts
 * ledger + balanced journal atomically, reverses rather than deletes, and never leaks a
 * tenant's document to another tenant.
 */
describe('Phase 05 inventory documents', () => {
  let ctx: TestApp;
  let alpha: Actor;
  let beta: Actor;
  let branchId = '';
  let warehouseId = '';
  let itemId = '';
  let baseUnitId = '';
  let cartonUnitId = '';
  let counterAccountId = '';

  const permissions = [
    ...ALL_PLATFORM_PERMISSIONS,
    ...ALL_ORGANIZATION_PERMISSIONS,
    'catalog.item.view',
    'catalog.item.manage',
    'catalog.category.manage',
    'catalog.unit.manage',
    'inventory.view',
    'inventory.adjust',
    'inventory.adjust.approve',
    'accounting.reports.view',
    'accounting.account.view',
    'accounting.period.close',
  ];

  beforeAll(async () => {
    ctx = await createTestApp('inventory-documents');
    alpha = await createActor(ctx, {
      tenantCode: 'inventory-documents-alpha',
      email: 'owner@inventory-documents-alpha.test',
      permissions,
    });
    beta = await createActor(ctx, {
      tenantCode: 'inventory-documents-beta',
      email: 'owner@inventory-documents-beta.test',
      permissions,
    });

    const defaults = await ctx.app.get(OrgProvisioningService).provisionOrgDefaults(alpha.tenantId);
    branchId = defaults.branchId;
    warehouseId = defaults.warehouseId;

    const year = new Date().getUTCFullYear();
    const fiscal = await api(ctx.server, 'post', '/api/v1/fiscal-years', {
      token: alpha.token,
      body: { name: `FY${year}`, startDate: `${year}-01-01`, endDate: `${year}-12-31` },
    });
    expect(fiscal.status).toBe(201);

    const accounts = await api(ctx.server, 'get', '/api/v1/accounts', { token: alpha.token });
    counterAccountId = data<Array<{ id: string; code: string }>>(accounts.body).find((row) => row.code === '1211001')!.id;
    expect(counterAccountId).toBeTruthy();

    const category = await api(ctx.server, 'post', '/api/v1/organization/catalog/categories', {
      token: alpha.token,
      body: { code: 'INV-DOC', nameAr: 'اختبار مستندات المخزون' },
    });
    const baseUnit = await api(ctx.server, 'post', '/api/v1/organization/catalog/units', {
      token: alpha.token,
      body: { code: 'PCS-DOC', nameAr: 'قطعة' },
    });
    const cartonUnit = await api(ctx.server, 'post', '/api/v1/organization/catalog/units', {
      token: alpha.token,
      body: { code: 'CTN-DOC', nameAr: 'كرتون' },
    });
    baseUnitId = data<{ id: string }>(baseUnit.body).id;
    cartonUnitId = data<{ id: string }>(cartonUnit.body).id;

    const item = await api(ctx.server, 'post', '/api/v1/organization/catalog/items', {
      token: alpha.token,
      body: {
        sku: 'STOCK-DOC',
        nameAr: 'مادة مستندات المخزون',
        categoryId: data<{ id: string }>(category.body).id,
        baseUnitId,
        kind: 'stock',
      },
    });
    itemId = data<{ id: string }>(item.body).id;
    const alternate = await api(ctx.server, 'post', `/api/v1/organization/catalog/items/${itemId}/units`, {
      token: alpha.token,
      body: { unitId: cartonUnitId, ratio: '12', barcode: 'INV-DOC-CARTON' },
    });
    expect(alternate.status).toBe(201);
  }, 240_000);

  afterAll(async () => ctx.close());

  const level = async () => {
    const response = await api(ctx.server, 'get', `/api/v1/inventory/levels?warehouse_id=${warehouseId}&item_id=${itemId}`, {
      token: alpha.token,
    });
    return data<Array<{ quantity: string; value: string; averageCost: string }>>(response.body)[0];
  };

  it('posts an opening-stock carton document with the selected unit, ledger and balanced journal in one operation', async () => {
    const created = await api(ctx.server, 'post', '/api/v1/inventory/documents', {
      token: alpha.token,
      body: {
        kind: 'opening',
        branchId,
        warehouseId,
        documentDate: new Date().toISOString().slice(0, 10),
        reason: 'رصيد افتتاحي معتمد',
        counterAccountId,
        lines: [{ itemId, unitId: cartonUnitId, quantity: '2', unitCost: '60' }],
      },
    });
    expect(created.status).toBe(201);
    const draft = data<InventoryDocument>(created.body);
    expect(draft).toMatchObject({ status: 'draft', kind: 'opening' });
    expect(draft.number).toMatch(/^OS-\d{6}$/);
    expect(draft.lines).toEqual([
      expect.objectContaining({
        itemId,
        unitId: cartonUnitId,
        quantity: '2.0000',
        baseQuantity: '24.0000',
        unitCost: '60.0000',
      }),
    ]);

    const posted = await api(ctx.server, 'post', `/api/v1/inventory/documents/${draft.id}/post`, {
      token: alpha.token,
      body: {},
    });
    expect(posted.status).toBe(201);
    const document = data<InventoryDocument>(posted.body);
    expect(document.status).toBe('posted');
    expect(document.journalEntryId).toBeTruthy();

    expect(await level()).toMatchObject({ quantity: '24.0000', value: '120.0000', averageCost: '5.0000' });
    const movements = await api(ctx.server, 'get', `/api/v1/inventory/movements?item_id=${itemId}&warehouse_id=${warehouseId}`, {
      token: alpha.token,
    });
    const openingMovement = data<Array<{ docId: string; qty: string; baseQty: string; unitId: string; unitCost: string; totalCost: string }>>(movements.body)
      .find((row) => row.docId === draft.id);
    expect(openingMovement).toMatchObject({
      qty: '2.0000',
      baseQty: '24.0000',
      unitId: cartonUnitId,
      unitCost: '5.0000',
      totalCost: '120.0000',
    });

    const journal = await api(ctx.server, 'get', `/api/v1/journal-entries/${document.journalEntryId}`, { token: alpha.token });
    expect(journal.status).toBe(200);
    const lines = data<{ lines: Array<{ debit: string; credit: string }> }>(journal.body).lines;
    expect(lines.reduce((sum, line) => sum + Number(line.debit), 0)).toBe(120);
    expect(lines.reduce((sum, line) => sum + Number(line.credit), 0)).toBe(120);
  });

  it('allows a draft to be corrected without changing its allocated number or document kind', async () => {
    const created = await api(ctx.server, 'post', '/api/v1/inventory/documents', {
      token: alpha.token,
      body: {
        kind: 'receipt', branchId, warehouseId, counterAccountId,
        reason: 'مسودة تحتاج تصحيحاً',
        lines: [{ itemId, quantity: '1', unitCost: '2' }],
      },
    });
    expect(created.status).toBe(201);
    const draft = data<InventoryDocument>(created.body);

    const corrected = await api(ctx.server, 'patch', `/api/v1/inventory/documents/${draft.id}`, {
      token: alpha.token,
      body: {
        kind: 'receipt', branchId, warehouseId, counterAccountId,
        reason: 'تم التصحيح قبل الترحيل',
        lines: [{ itemId, unitId: cartonUnitId, quantity: '1', unitCost: '48' }],
      },
    });
    expect(corrected.status).toBe(200);
    expect(data<InventoryDocument>(corrected.body)).toMatchObject({
      id: draft.id,
      number: draft.number,
      status: 'draft',
      reason: 'تم التصحيح قبل الترحيل',
      lines: [expect.objectContaining({ unitId: cartonUnitId, baseQuantity: '12.0000', unitCost: '48.0000' })],
    });

    const invalidKind = await api(ctx.server, 'patch', `/api/v1/inventory/documents/${draft.id}`, {
      token: alpha.token,
      body: { kind: 'issue', branchId, warehouseId, counterAccountId, lines: [{ itemId, quantity: '1' }] },
    });
    expect(invalidKind.status).toBe(422);
    expect(code(invalidKind.body)).toBe('INVENTORY_DOCUMENT_KIND_IMMUTABLE');
  });

  it('reverses a posted opening document instead of deleting it, including its stock value and journal', async () => {
    const documents = await api(ctx.server, 'get', '/api/v1/inventory/documents?kind=opening&status=posted', { token: alpha.token });
    const opening = data<InventoryDocument[]>(documents.body)[0]!;

    const voided = await api(ctx.server, 'post', `/api/v1/inventory/documents/${opening.id}/void`, {
      token: alpha.token,
      body: { reason: 'إعادة إدخال الرصيد الافتتاحي' },
    });
    expect(voided.status).toBe(201);
    expect(data<InventoryDocument>(voided.body).status).toBe('voided');
    expect(await level()).toMatchObject({ quantity: '0.0000', value: '0.0000', averageCost: '0.0000' });

    const source = await api(ctx.server, 'get', `/api/v1/journal-entries/${opening.journalEntryId}`, { token: alpha.token });
    expect(data<{ status: string }>(source.body).status).toBe('void');
    const journals = await api(ctx.server, 'get', '/api/v1/journal-entries?limit=50', { token: alpha.token });
    expect(
      data<Array<{ kind: string; reversalOf: string | null }>>(journals.body).some(
        (entry) => entry.kind === 'reversal' && entry.reversalOf === opening.journalEntryId,
      ),
    ).toBe(true);
  });

  it('uses moving-average cost for a goods issue and leaves an insufficient draft untouched', async () => {
    const receipt = await api(ctx.server, 'post', '/api/v1/inventory/documents', {
      token: alpha.token,
      body: {
        kind: 'receipt', branchId, warehouseId, counterAccountId,
        lines: [{ itemId, unitId: baseUnitId, quantity: '10', unitCost: '7' }],
      },
    });
    const receiptDraft = data<InventoryDocument>(receipt.body);
    expect((await api(ctx.server, 'post', `/api/v1/inventory/documents/${receiptDraft.id}/post`, { token: alpha.token, body: {} })).status).toBe(201);
    expect(await level()).toMatchObject({ quantity: '10.0000', value: '70.0000', averageCost: '7.0000' });

    const issue = await api(ctx.server, 'post', '/api/v1/inventory/documents', {
      token: alpha.token,
      body: {
        kind: 'issue', branchId, warehouseId, counterAccountId,
        lines: [{ itemId, quantity: '3' }],
      },
    });
    const issueDraft = data<InventoryDocument>(issue.body);
    const issuePosted = await api(ctx.server, 'post', `/api/v1/inventory/documents/${issueDraft.id}/post`, { token: alpha.token, body: {} });
    expect(issuePosted.status).toBe(201);
    expect(await level()).toMatchObject({ quantity: '7.0000', value: '49.0000', averageCost: '7.0000' });

    const excessive = await api(ctx.server, 'post', '/api/v1/inventory/documents', {
      token: alpha.token,
      body: {
        kind: 'issue', branchId, warehouseId, counterAccountId,
        lines: [{ itemId, quantity: '8' }],
      },
    });
    const excessiveDraft = data<InventoryDocument>(excessive.body);
    const denied = await api(ctx.server, 'post', `/api/v1/inventory/documents/${excessiveDraft.id}/post`, { token: alpha.token, body: {} });
    expect(denied.status).toBe(422);
    expect(code(denied.body)).toBe('STOCK_INSUFFICIENT');
    const reread = await api(ctx.server, 'get', `/api/v1/inventory/documents/${excessiveDraft.id}`, { token: alpha.token });
    expect(data<InventoryDocument>(reread.body).status).toBe('draft');
    expect(await level()).toMatchObject({ quantity: '7.0000', value: '49.0000' });
  });

  it('posts a mixed stock adjustment once, preserving both directions and rejecting a second post', async () => {
    const adjustment = await api(ctx.server, 'post', '/api/v1/inventory/documents', {
      token: alpha.token,
      body: {
        kind: 'adjustment',
        branchId,
        warehouseId,
        counterAccountId,
        reason: 'فرق جرد مختلط',
        lines: [
          { itemId, quantity: '2', unitCost: '9', adjustmentDirection: 'in' },
          { itemId, quantity: '1', unitCost: '0', adjustmentDirection: 'out' },
        ],
      },
    });
    expect(adjustment.status).toBe(201);
    const draft = data<InventoryDocument>(adjustment.body);
    expect(draft.lines.map((line) => line.adjustmentDirection)).toEqual(['in', 'out']);

    const posted = await api(ctx.server, 'post', `/api/v1/inventory/documents/${draft.id}/post`, {
      token: alpha.token,
      body: {},
    });
    expect(posted.status).toBe(201);
    expect(data<InventoryDocument>(posted.body).journalEntryId).toBeTruthy();

    const doublePost = await api(ctx.server, 'post', `/api/v1/inventory/documents/${draft.id}/post`, {
      token: alpha.token,
      body: {},
    });
    expect(doublePost.status).toBe(409);
    expect(code(doublePost.body)).toBe('INVENTORY_DOCUMENT_INVALID_STATUS');

    const movements = await api(ctx.server, 'get', `/api/v1/inventory/movements?item_id=${itemId}&warehouse_id=${warehouseId}`, {
      token: alpha.token,
    });
    const adjustmentDirections = data<Array<{ docId: string; direction: string }>>(movements.body)
      .filter((movement) => movement.docId === draft.id)
      .map((movement) => movement.direction)
      .sort();
    expect(adjustmentDirections).toEqual(['in', 'out']);
    expect(await level()).toMatchObject({ quantity: '8.0000' });
  });

  it('prints a posted inventory document from the immutable ledger valuation', async () => {
    const documents = await api(ctx.server, 'get', '/api/v1/inventory/documents?kind=adjustment&status=posted', {
      token: alpha.token,
    });
    const adjustment = data<InventoryDocument[]>(documents.body)[0]!;
    const printed = await api(ctx.server, 'get', `/api/v1/reports/print/inventory-documents/${adjustment.id}`, {
      token: alpha.token,
    });
    expect(printed.status).toBe(200);
    const html = (printed.body as { html: string }).html;
    expect(html).toContain('تسوية مخزنية');
    expect(html).toContain('مادة مستندات المخزون');
    expect(html).toContain('فرق جرد مختلط');
    expect(html).toContain('قيمة الداخل');
    expect(html).toContain('قيمة الخارج');
    expect(html).toContain('@page');
  });

  it('keeps documents tenant-scoped and protects create/post actions with distinct permissions', async () => {
    const issue = await api(ctx.server, 'get', '/api/v1/inventory/documents?kind=issue', { token: alpha.token });
    const alphaDocument = data<InventoryDocument[]>(issue.body)[0]!;
    const foreignRead = await api(ctx.server, 'get', `/api/v1/inventory/documents/${alphaDocument.id}`, { token: beta.token });
    expect(foreignRead.status).toBe(404);

    const viewer = await createActor(ctx, {
      tenantId: alpha.tenantId,
      tenantCode: alpha.tenantCode,
      email: 'inventory-documents-viewer@alpha.test',
      permissions: ['inventory.view'],
      roleNames: ['Inventory document viewer'],
      isOwner: false,
    });
    const forbiddenCreate = await api(ctx.server, 'post', '/api/v1/inventory/documents', {
      token: viewer.token,
      body: { kind: 'receipt', branchId, warehouseId, lines: [{ itemId, quantity: '1', unitCost: '1' }] },
    });
    expect(forbiddenCreate.status).toBe(403);

    const draft = await api(ctx.server, 'post', '/api/v1/inventory/documents', {
      token: alpha.token,
      body: { kind: 'receipt', branchId, warehouseId, counterAccountId, lines: [{ itemId, quantity: '1', unitCost: '1' }] },
    });
    const forbiddenPost = await api(ctx.server, 'post', `/api/v1/inventory/documents/${data<InventoryDocument>(draft.body).id}/post`, {
      token: viewer.token,
      body: {},
    });
    expect(forbiddenPost.status).toBe(403);

    // Printing is an inventory operation as well: a read-only warehouse viewer may print
    // a document without acquiring broad reporting/export permission.
    const viewerPrint = await api(ctx.server, 'get', `/api/v1/reports/print/inventory-documents/${alphaDocument.id}`, {
      token: viewer.token,
    });
    expect(viewerPrint.status).toBe(200);
  });
});
