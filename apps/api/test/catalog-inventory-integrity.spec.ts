import { randomUUID } from 'node:crypto';

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

/**
 * Phase 05 catalog / inventory contract.
 *
 * A barcode is not merely a display field: POS and warehouse documents use it to
 * choose an item *and a concrete unit*. These integration tests exercise the
 * synchronised primary/unit register, tenant isolation, permission gates and the
 * irreversible conversion-ratio rule once a unit has entered the stock ledger.
 */
describe('Phase 05 catalog scanner-code and item-unit integrity', () => {
  let ctx: TestApp;
  let alpha: Actor;
  let beta: Actor;
  let warehouseId = '';
  let categoryId = '';
  let baseUnitId = '';
  let boxUnitId = '';
  let itemId = '';

  const data = <T>(body: Record<string, unknown>) => (body.data ?? body) as T;
  const codeOf = (body: Record<string, unknown>) =>
    (body.error as { code?: string } | undefined)?.code ?? (body.code as string | undefined);

  beforeAll(async () => {
    ctx = await createTestApp('catalog-inventory-integrity');
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
    ];
    alpha = await createActor(ctx, {
      tenantCode: 'catalog-alpha',
      email: 'owner@catalog-alpha.test',
      permissions,
    });
    beta = await createActor(ctx, {
      tenantCode: 'catalog-beta',
      email: 'owner@catalog-beta.test',
      permissions,
    });

    const defaults = await ctx.app.get(OrgProvisioningService).provisionOrgDefaults(alpha.tenantId);
    warehouseId = defaults.warehouseId;

    const category = await api(ctx.server, 'post', '/api/v1/organization/catalog/categories', {
      token: alpha.token,
      body: { code: 'CAT-05', nameAr: 'فئة اختبار المخزون' },
    });
    expect(category.status).toBe(201);
    categoryId = data<{ id: string }>(category.body).id;

    const baseUnit = await api(ctx.server, 'post', '/api/v1/organization/catalog/units', {
      token: alpha.token,
      body: { code: 'PCS-05', nameAr: 'قطعة' },
    });
    expect(baseUnit.status).toBe(201);
    baseUnitId = data<{ id: string }>(baseUnit.body).id;

    const boxUnit = await api(ctx.server, 'post', '/api/v1/organization/catalog/units', {
      token: alpha.token,
      body: { code: 'BOX-05', nameAr: 'كرتون' },
    });
    expect(boxUnit.status).toBe(201);
    boxUnitId = data<{ id: string }>(boxUnit.body).id;
  }, 240_000);

  afterAll(async () => ctx.close());

  it('keeps a primary barcode synchronised when the item card changes it', async () => {
    const created = await api(ctx.server, 'post', '/api/v1/organization/catalog/items', {
      token: alpha.token,
      body: {
        sku: 'SKU-CAT-05',
        barcode: '  BASE-05-OLD  ',
        nameAr: 'صنف متعدد الوحدات',
        categoryId,
        baseUnitId,
        salePrice: '10.0000',
        purchasePrice: '5.0000',
        minQty: '3',
        maxQty: '30',
        maxDiscountPct: '12.5',
        maxDiscountAmt: '15',
        weightedScale: true,
        showInPos: false,
      },
    });
    expect(created.status).toBe(201);
    const createdItem = data<{
      id: string;
      minQty: string;
      maxQty: string | null;
      maxDiscountPct: string | null;
      maxDiscountAmt: string | null;
      weightedScale: boolean;
      showInPos: boolean;
    }>(created.body);
    itemId = createdItem.id;
    expect(createdItem).toMatchObject({
      minQty: '3.0000',
      maxQty: '30.0000',
      maxDiscountPct: '12.5000',
      maxDiscountAmt: '15.0000',
      weightedScale: true,
      showInPos: false,
    });

    const firstLookup = await api(ctx.server, 'get', '/api/v1/organization/catalog/items/lookup?code=BASE-05-OLD', {
      token: alpha.token,
    });
    expect(firstLookup.status).toBe(200);
    expect(data<{ item: { id: string }; unitId: string; matchedAs: string }>(firstLookup.body)).toMatchObject({
      item: { id: itemId },
      unitId: baseUnitId,
      matchedAs: 'primary_barcode',
    });

    const changed = await api(ctx.server, 'patch', `/api/v1/organization/catalog/items/${itemId}`, {
      token: alpha.token,
      body: { barcode: 'BASE-05-NEW' },
    });
    expect(changed.status).toBe(200);
    expect(data<{ barcode: string | null }>(changed.body).barcode).toBe('BASE-05-NEW');

    // The directory search is the card's entry point, so it must cover user-facing
    // Arabic/name fields as well as scanner-facing SKU and primary barcode fields.
    for (const query of ['متعدد', 'SKU-CAT-05', 'BASE-05-NEW']) {
      const listed = await api(ctx.server, 'get', `/api/v1/organization/catalog/items?q=${encodeURIComponent(query)}`, {
        token: alpha.token,
      });
      expect(listed.status).toBe(200);
      expect(data<Array<{ id: string }>>(listed.body)).toContainEqual(expect.objectContaining({ id: itemId }));
    }

    // Empty prices mean "clear the optional default", not a zero price. This keeps
    // the item card's blank numeric fields faithful when an operator removes a price.
    const clearedPrices = await api(ctx.server, 'patch', `/api/v1/organization/catalog/items/${itemId}`, {
      token: alpha.token,
      body: { salePrice: null, purchasePrice: null },
    });
    expect(clearedPrices.status).toBe(200);
    expect(data<{ salePrice: string | null; purchasePrice: string | null }>(clearedPrices.body)).toMatchObject({
      salePrice: null,
      purchasePrice: null,
    });

    const oldLookup = await api(ctx.server, 'get', '/api/v1/organization/catalog/items/lookup?code=BASE-05-OLD', {
      token: alpha.token,
    });
    expect(oldLookup.status).toBe(404);

    const barcodes = await api(ctx.server, 'get', `/api/v1/organization/catalog/items/${itemId}/barcodes`, {
      token: alpha.token,
    });
    expect(barcodes.status).toBe(200);
    const payload = data<{
      primary: { barcode: string; unitId: string } | null;
      barcodes: Array<{ barcode: string; unitId: string | null; source: string; primary: boolean }>;
    }>(barcodes.body);
    expect(payload.primary).toEqual({ barcode: 'BASE-05-NEW', unitId: baseUnitId, primary: true });
    expect(payload.barcodes).toContainEqual(
      expect.objectContaining({ barcode: 'BASE-05-NEW', unitId: baseUnitId, source: 'primary', primary: true }),
    );
  });

  it('registers an alternate-unit barcode once and resolves its ratio for a scanner', async () => {
    const unit = await api(ctx.server, 'post', `/api/v1/organization/catalog/items/${itemId}/units`, {
      token: alpha.token,
      body: {
        unitId: boxUnitId,
        ratio: '12',
        barcode: 'BOX-05-PRIMARY',
        salePrice: '120.0000',
        purchasePrice: '60.0000',
        isDefaultSale: true,
      },
    });
    expect(unit.status).toBe(201);

    const primaryUnitLookup = await api(ctx.server, 'get', '/api/v1/organization/catalog/items/lookup?code=BOX-05-PRIMARY', {
      token: alpha.token,
    });
    expect(primaryUnitLookup.status).toBe(200);
    expect(data<{ item: { id: string }; unitId: string; ratio: string; matchedAs: string }>(primaryUnitLookup.body)).toMatchObject({
      item: { id: itemId },
      unitId: boxUnitId,
      ratio: '12.000000',
      matchedAs: 'unit_barcode',
    });

    const clearedUnitPrices = await api(ctx.server, 'post', `/api/v1/organization/catalog/items/${itemId}/units`, {
      token: alpha.token,
      body: { unitId: boxUnitId, ratio: '12', salePrice: null, purchasePrice: null },
    });
    expect(clearedUnitPrices.status).toBe(201);
    expect(data<{ salePrice: string | null; purchasePrice: string | null }>(clearedUnitPrices.body)).toMatchObject({
      salePrice: null,
      purchasePrice: null,
    });

    const alternate = await api(ctx.server, 'post', `/api/v1/organization/catalog/items/${itemId}/barcodes`, {
      token: alpha.token,
      body: { barcode: 'BOX-05-ALT', unitId: boxUnitId },
    });
    expect(alternate.status).toBe(201);

    const alternateLookup = await api(ctx.server, 'get', '/api/v1/organization/catalog/items/lookup?code=BOX-05-ALT', {
      token: alpha.token,
    });
    expect(alternateLookup.status).toBe(200);
    expect(data<{ unitId: string; ratio: string; matchedAs: string }>(alternateLookup.body)).toMatchObject({
      unitId: boxUnitId,
      ratio: '12.000000',
      matchedAs: 'barcode',
    });
  });

  it('makes a supplier/legacy code a visible, unique scanner code with an auditable note', async () => {
    const added = await api(ctx.server, 'post', `/api/v1/organization/catalog/items/${itemId}/alternative-codes`, {
      token: alpha.token,
      body: { code: ' SUPPLIER-05 ', notes: 'رمز المورد في فاتورة التوريد' },
    });
    expect(added.status).toBe(201);
    expect(data<{ code: string; notes: string | null }>(added.body)).toMatchObject({
      code: 'SUPPLIER-05',
      notes: 'رمز المورد في فاتورة التوريد',
    });

    const resolved = await api(ctx.server, 'get', '/api/v1/organization/catalog/items/lookup?code=SUPPLIER-05', {
      token: alpha.token,
    });
    expect(resolved.status).toBe(200);
    expect(data<{ item: { id: string }; unitId: string; matchedAs: string }>(resolved.body)).toMatchObject({
      item: { id: itemId },
      unitId: baseUnitId,
      matchedAs: 'alternative_code',
    });

    const savedAgain = await api(ctx.server, 'post', `/api/v1/organization/catalog/items/${itemId}/alternative-codes`, {
      token: alpha.token,
      body: { code: 'SUPPLIER-05', notes: 'رمز المورد المحدّث' },
    });
    expect(savedAgain.status).toBe(201);
    const codes = await api(ctx.server, 'get', `/api/v1/organization/catalog/items/${itemId}/alternative-codes`, {
      token: alpha.token,
    });
    expect(data<Array<{ code: string; notes: string | null }>>(codes.body)).toEqual([
      expect.objectContaining({ code: 'SUPPLIER-05', notes: 'رمز المورد المحدّث' }),
    ]);
  });

  it('blocks scanner-code collisions across barcode, alternative-code and SKU fields but allows the same code in another tenant', async () => {
    const duplicateBarcode = await api(ctx.server, 'post', '/api/v1/organization/catalog/items', {
      token: alpha.token,
      body: {
        sku: 'SKU-COLLISION-BARCODE',
        barcode: 'BOX-05-PRIMARY',
        nameAr: 'صنف متعارض',
        categoryId,
        baseUnitId,
      },
    });
    expect(duplicateBarcode.status).toBe(409);
    expect(codeOf(duplicateBarcode.body)).toBe('CATALOG_BARCODE_IN_USE');

    const duplicateSku = await api(ctx.server, 'post', '/api/v1/organization/catalog/items', {
      token: alpha.token,
      body: {
        sku: 'SUPPLIER-05',
        nameAr: 'صنف SKU متعارض',
        categoryId,
        baseUnitId,
      },
    });
    expect(duplicateSku.status).toBe(409);
    expect(codeOf(duplicateSku.body)).toBe('CATALOG_BARCODE_IN_USE');

    const betaDefaults = await ctx.app.get(OrgProvisioningService).provisionOrgDefaults(beta.tenantId);
    expect(betaDefaults.branchId).toBeTruthy();
    const betaCategory = await api(ctx.server, 'post', '/api/v1/organization/catalog/categories', {
      token: beta.token,
      body: { code: 'CAT-05', nameAr: 'فئة بيتا' },
    });
    const betaUnit = await api(ctx.server, 'post', '/api/v1/organization/catalog/units', {
      token: beta.token,
      body: { code: 'PCS-05', nameAr: 'قطعة بيتا' },
    });
    const betaItem = await api(ctx.server, 'post', '/api/v1/organization/catalog/items', {
      token: beta.token,
      body: {
        sku: 'SKU-BETA-05',
        barcode: 'BOX-05-PRIMARY',
        nameAr: 'صنف بيتا المستقل',
        categoryId: data<{ id: string }>(betaCategory.body).id,
        baseUnitId: data<{ id: string }>(betaUnit.body).id,
      },
    });
    expect(betaItem.status).toBe(201);
  });

  it('retires an alternative code without leaving it in scanner lookup', async () => {
    const removed = await api(ctx.server, 'delete', `/api/v1/organization/catalog/items/${itemId}/alternative-codes/SUPPLIER-05`, {
      token: alpha.token,
    });
    expect(removed.status).toBe(200);
    const lookup = await api(ctx.server, 'get', '/api/v1/organization/catalog/items/lookup?code=SUPPLIER-05', {
      token: alpha.token,
    });
    expect(lookup.status).toBe(404);
  });

  it('locks an alternate unit ratio and deletion after it has entered the stock ledger', async () => {
    const recorded = await api(ctx.server, 'post', '/api/v1/inventory/ledger/record', {
      token: alpha.token,
      body: {
        lines: [
          {
            itemId,
            warehouseId,
            unitId: boxUnitId,
            qty: '1.0000',
            unitCost: '60.0000',
            direction: 'in',
            docType: 'catalog-integrity-test',
            docId: randomUUID(),
            lineId: randomUUID(),
          },
        ],
      },
    });
    expect(recorded.status).toBe(201);

    const changedRatio = await api(ctx.server, 'post', `/api/v1/organization/catalog/items/${itemId}/units`, {
      token: alpha.token,
      body: { unitId: boxUnitId, ratio: '24', barcode: 'BOX-05-PRIMARY' },
    });
    expect(changedRatio.status).toBe(409);
    expect(codeOf(changedRatio.body)).toBe('CATALOG_ITEM_UNIT_RATIO_LOCKED');

    const removed = await api(ctx.server, 'delete', `/api/v1/organization/catalog/items/${itemId}/units/${boxUnitId}`, {
      token: alpha.token,
    });
    expect(removed.status).toBe(409);
    expect(codeOf(removed.body)).toBe('CATALOG_ITEM_UNIT_IN_USE');
  });

  it('enforces item-unit tenant isolation and catalog write permissions', async () => {
    const foreignRead = await api(ctx.server, 'get', `/api/v1/organization/catalog/items/${itemId}/units`, {
      token: beta.token,
    });
    expect(foreignRead.status).toBe(404);

    const viewer = await createActor(ctx, {
      tenantId: alpha.tenantId,
      tenantCode: alpha.tenantCode,
      email: 'catalog-viewer@catalog-alpha.test',
      permissions: ['catalog.item.view'],
      roleNames: ['Catalog viewer'],
      isOwner: false,
    });
    const forbidden = await api(ctx.server, 'post', `/api/v1/organization/catalog/items/${itemId}/barcodes`, {
      token: viewer.token,
      body: { barcode: 'FORBIDDEN-05', unitId: baseUnitId },
    });
    expect(forbidden.status).toBe(403);

    const forbiddenAlternative = await api(ctx.server, 'post', `/api/v1/organization/catalog/items/${itemId}/alternative-codes`, {
      token: viewer.token,
      body: { code: 'FORBIDDEN-ALT-05', notes: 'لا يحق للمشاهد الحفظ' },
    });
    expect(forbiddenAlternative.status).toBe(403);
  });
});

export {};
