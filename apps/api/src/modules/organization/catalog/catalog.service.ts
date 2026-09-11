import { Injectable, Inject } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { and, asc, eq, ilike, isNull, ne, or, sql } from 'drizzle-orm';
import { DomainError, errorCodes } from '@erp/contracts';
import { inventoryTransactions, itemAlternativeCodes, itemBarcodes, itemCategories, itemUnits, items, newId, taxGroups, unitsOfMeasure, withTenantTx, type DatabaseHandle, type DrizzleTx } from '@erp/database';

import { DATABASE_HANDLE } from '../../../database/database.module.js';

export type CatalogItemInput = {
  sku: string;
  barcode?: string;
  nameAr: string;
  nameEn?: string;
  categoryId: string;
  baseUnitId: string;
  kind?: 'stock' | 'service' | 'composite';
  salePrice?: string;
  purchasePrice?: string;
  taxGroupId?: string;
  minQty?: string;
  maxQty?: string;
  maxDiscountPct?: string;
  maxDiscountAmt?: string;
  trackLot?: boolean;
  trackSerial?: boolean;
  weightedScale?: boolean;
  showInPos?: boolean;
};
export type ItemUnitInput = {
  unitId: string;
  /** How many base units one selected unit contains. */
  ratio: string;
  barcode?: string;
  salePrice?: string | null;
  purchasePrice?: string | null;
  isDefaultPurchase?: boolean;
  isDefaultSale?: boolean;
};
export type ItemBarcodeInput = { barcode: string; unitId?: string };
export type ItemAlternativeCodeInput = { code: string; notes?: string };
export type CategoryInput = { code: string; nameAr: string; nameEn?: string; parentId?: string };
export type UnitInput = { code: string; nameAr: string; nameEn?: string };
export type TaxGroupInput = { nameAr: string; nameEn?: string; rate: string; vatAccountId?: string; isInclusiveDefault?: boolean };

export type CatalogItemPatch = {
  sku?: string;
  barcode?: string | null;
  nameAr?: string;
  nameEn?: string | null;
  categoryId?: string;
  kind?: 'stock' | 'service' | 'composite';
  salePrice?: string | null;
  purchasePrice?: string | null;
  taxGroupId?: string | null;
  minQty?: string;
  maxQty?: string | null;
  maxDiscountPct?: string | null;
  maxDiscountAmt?: string | null;
  trackLot?: boolean;
  trackSerial?: boolean;
  weightedScale?: boolean;
  showInPos?: boolean;
};
export type CategoryPatch = { code?: string; nameAr?: string; nameEn?: string | null; parentId?: string | null };
export type UnitPatch = { code?: string; nameAr?: string; nameEn?: string | null };
export type TaxGroupPatch = { nameAr?: string; nameEn?: string | null; rate?: string; vatAccountId?: string | null; isInclusiveDefault?: boolean };

/**
 * A master-data card is editable, but not infinitely: once a row has been used by a
 * document, the fields that would silently rewrite history are frozen. For an item that
 * is its base unit — every stored quantity and moving-average cost is expressed in it, so
 * changing it after the fact would reinterpret movements that already happened. Names,
 * prices, barcode, category and tax group stay editable, because those are what people
 * actually need to correct.
 */
const IMMUTABLE_AFTER_USE = 'CATALOG_ITEM_IN_USE';

type BarcodeSource = 'primary' | 'unit' | 'alternate';
type ScanCodeTarget = { itemId: string; unitId: string };
type ScanCodeConflict = { item_id: string; unit_id: string; source: string };

/**
 * Barcode scanners remove neither leading nor trailing spaces reliably, so all scanner
 * codes are normalised before persistence and lookup. `null` is the explicit "clear
 * this barcode" value used by item and unit patches.
 */
function normalizeOptionalBarcode(value: string | null | undefined): string | null | undefined {
  if (value === undefined || value === null) return value;
  return value.trim() || null;
}

function normalizeRequiredScanCode(value: string, field: 'sku' | 'barcode' | 'code'): string {
  const normalized = value.trim();
  if (!normalized) {
    throw new DomainError(
      field === 'sku' ? 'CATALOG_SKU_REQUIRED' : 'CATALOG_BARCODE_REQUIRED',
      `${field === 'sku' ? 'SKU' : field === 'barcode' ? 'Barcode' : 'Alternative code'} is required`,
      422,
      { field },
    );
  }
  return normalized;
}

@Injectable()
export class CatalogService {
  constructor(@Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle) {}

  async listItems(tenantId: string, q?: string) {
    const needle = q?.trim();
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(items)
        .where(
          and(
            eq(items.tenantId, tenantId),
            isNull(items.deletedAt),
            needle
              ? or(
                  ilike(items.nameAr, `%${needle}%`),
                  ilike(items.nameEn, `%${needle}%`),
                  ilike(items.sku, `%${needle}%`),
                  ilike(items.barcode, `%${needle}%`),
                )
              : undefined,
          ),
        )
        .orderBy(asc(items.nameAr))
        .limit(100),
    );
  }

  async createItem(tenantId: string, input: CatalogItemInput) {
    const id = newId();
    const sku = normalizeRequiredScanCode(input.sku, 'sku');
    const barcode = normalizeOptionalBarcode(input.barcode) ?? null;
    this.assertItemSettings(input);
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      await this.assertItemReferencesInTx(tx, tenantId, input.categoryId, input.baseUnitId, input.taxGroupId);
      const target = { itemId: id, unitId: input.baseUnitId };
      await this.assertScanCodeAvailableInTx(tx, tenantId, sku, target, 'sku');
      if (barcode) await this.assertScanCodeAvailableInTx(tx, tenantId, barcode, target, 'barcode');
      await tx.insert(items).values({
        id,
        tenantId,
        sku,
        barcode,
        nameAr: input.nameAr,
        nameEn: input.nameEn,
        categoryId: input.categoryId,
        baseUnitId: input.baseUnitId,
        kind: input.kind ?? 'stock',
        salePrice: input.salePrice,
        purchasePrice: input.purchasePrice,
        taxGroupId: input.taxGroupId,
        minQty: input.minQty ?? '0',
        maxQty: input.maxQty,
        maxDiscountPct: input.maxDiscountPct,
        maxDiscountAmt: input.maxDiscountAmt,
        trackLot: input.trackLot ?? false,
        trackSerial: input.trackSerial ?? false,
        weightedScale: input.weightedScale ?? false,
        showInPos: input.showInPos ?? true,
      });
      await this.syncPrimaryBarcodeInTx(tx, tenantId, target, barcode);
    });
    return this.getItem(tenantId, id);
  }

  /**
   * Update an item card. `baseUnitId` is not in the patch type at all, so it cannot be
   * changed by mistake; `sku` is refused once the item has moved, because the SKU is what
   * printed documents and barcode labels already carry.
   */
  async updateItem(tenantId: string, id: string, patch: CatalogItemPatch) {
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      const current = await this.requireItem(tx, tenantId, id);
      const sku = patch.sku === undefined ? current.sku : normalizeRequiredScanCode(patch.sku, 'sku');
      const barcode = patch.barcode === undefined ? current.barcode : normalizeOptionalBarcode(patch.barcode) ?? null;
      const target = { itemId: id, unitId: current.baseUnitId };
      this.assertItemSettings({
        minQty: patch.minQty ?? current.minQty,
        maxQty: patch.maxQty === undefined ? current.maxQty : patch.maxQty,
        maxDiscountPct: patch.maxDiscountPct === undefined ? current.maxDiscountPct : patch.maxDiscountPct,
        maxDiscountAmt: patch.maxDiscountAmt === undefined ? current.maxDiscountAmt : patch.maxDiscountAmt,
        trackLot: patch.trackLot ?? current.trackLot,
        trackSerial: patch.trackSerial ?? current.trackSerial,
      });
      if (patch.categoryId || patch.taxGroupId !== undefined) {
        await this.assertItemReferencesInTx(
          tx,
          tenantId,
          patch.categoryId ?? current.categoryId,
          current.baseUnitId,
          patch.taxGroupId === undefined ? current.taxGroupId ?? undefined : patch.taxGroupId ?? undefined,
        );
      }
      if (sku !== current.sku && (await this.itemHasMovements(tx, tenantId, id))) {
        throw new DomainError(IMMUTABLE_AFTER_USE, 'This item already has movements; its SKU can no longer be changed', 409);
      }
      if (sku !== current.sku) await this.assertScanCodeAvailableInTx(tx, tenantId, sku, target, 'sku');
      if (barcode && barcode !== current.barcode) await this.assertScanCodeAvailableInTx(tx, tenantId, barcode, target, 'barcode');
      await tx
        .update(items)
        .set({
          sku,
          barcode,
          nameAr: patch.nameAr ?? current.nameAr,
          nameEn: patch.nameEn === undefined ? current.nameEn : patch.nameEn,
          categoryId: patch.categoryId ?? current.categoryId,
          kind: patch.kind ?? current.kind,
          salePrice: patch.salePrice === undefined ? current.salePrice : patch.salePrice,
          purchasePrice: patch.purchasePrice === undefined ? current.purchasePrice : patch.purchasePrice,
          taxGroupId: patch.taxGroupId === undefined ? current.taxGroupId : patch.taxGroupId,
          minQty: patch.minQty ?? current.minQty,
          maxQty: patch.maxQty === undefined ? current.maxQty : patch.maxQty,
          maxDiscountPct: patch.maxDiscountPct === undefined ? current.maxDiscountPct : patch.maxDiscountPct,
          maxDiscountAmt: patch.maxDiscountAmt === undefined ? current.maxDiscountAmt : patch.maxDiscountAmt,
          trackLot: patch.trackLot ?? current.trackLot,
          trackSerial: patch.trackSerial ?? current.trackSerial,
          weightedScale: patch.weightedScale ?? current.weightedScale,
          showInPos: patch.showInPos ?? current.showInPos,
          updatedAt: new Date(),
        })
        .where(and(eq(items.tenantId, tenantId), eq(items.id, id)));
      if (patch.barcode !== undefined) await this.syncPrimaryBarcodeInTx(tx, tenantId, target, barcode);
    });
    return this.getItem(tenantId, id);
  }

  /**
   * Deleting an item that has moved would orphan every ledger row that points at it, so
   * a used item is **archived** (soft-deleted + deactivated) and an unused one is removed.
   * Either way it disappears from the pickers; only one of them disappears from history.
   */
  async removeItem(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      await this.requireItem(tx, tenantId, id);
      const used = await this.itemHasMovements(tx, tenantId, id);
      await tx
        .update(items)
        .set({ deletedAt: new Date(), showInPos: false, updatedAt: new Date() })
        .where(and(eq(items.tenantId, tenantId), eq(items.id, id)));
      return { id, archived: used, deleted: !used };
    });
  }

  async getItem(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [row] = await tx.select().from(items).where(and(eq(items.id, id), eq(items.tenantId, tenantId), isNull(items.deletedAt)));
      return row;
    });
  }

  /** Units configured for an item, including its immutable base unit at ratio 1. */
  async listItemUnits(tenantId: string, itemId: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const item = await this.requireItem(tx, tenantId, itemId);
      const alternatives = await tx
        .select({
          unitId: itemUnits.unitId,
          ratio: itemUnits.ratio,
          barcode: itemUnits.barcode,
          salePrice: itemUnits.salePrice,
          purchasePrice: itemUnits.purchasePrice,
          isDefaultPurchase: itemUnits.isDefaultPurchase,
          isDefaultSale: itemUnits.isDefaultSale,
          code: unitsOfMeasure.code,
          nameAr: unitsOfMeasure.nameAr,
          nameEn: unitsOfMeasure.nameEn,
        })
        .from(itemUnits)
        .innerJoin(unitsOfMeasure, eq(unitsOfMeasure.id, itemUnits.unitId))
        .where(
          and(
            eq(itemUnits.itemId, itemId),
            eq(unitsOfMeasure.tenantId, tenantId),
            isNull(unitsOfMeasure.deletedAt),
          ),
        )
        .orderBy(asc(unitsOfMeasure.code));
      const [base] = await tx
        .select({ id: unitsOfMeasure.id, code: unitsOfMeasure.code, nameAr: unitsOfMeasure.nameAr, nameEn: unitsOfMeasure.nameEn })
        .from(unitsOfMeasure)
        .where(and(eq(unitsOfMeasure.tenantId, tenantId), eq(unitsOfMeasure.id, item.baseUnitId), isNull(unitsOfMeasure.deletedAt)));
      return [
        {
          unitId: item.baseUnitId,
          ratio: '1.000000',
          barcode: item.barcode,
          salePrice: item.salePrice,
          purchasePrice: item.purchasePrice,
          isDefaultPurchase: !alternatives.some((row) => row.isDefaultPurchase),
          isDefaultSale: !alternatives.some((row) => row.isDefaultSale),
          code: base?.code ?? null,
          nameAr: base?.nameAr ?? null,
          nameEn: base?.nameEn ?? null,
          isBase: true,
        },
        ...alternatives.map((row) => ({ ...row, isBase: false })),
      ];
    });
  }

  async upsertItemUnit(tenantId: string, itemId: string, input: ItemUnitInput) {
    const ratio = new Decimal(input.ratio);
    if (!ratio.isFinite() || ratio.lte(0)) {
      throw new DomainError('CATALOG_ITEM_UNIT_RATIO_INVALID', 'Unit ratio must be greater than zero', 422, { field: 'ratio' });
    }
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const item = await this.requireItem(tx, tenantId, itemId);
      if (input.unitId === item.baseUnitId) {
        throw new DomainError('CATALOG_ITEM_UNIT_IS_BASE', 'Edit base-unit prices on the item card', 422, { field: 'unitId' });
      }
      const [unit] = await tx
        .select({ id: unitsOfMeasure.id })
        .from(unitsOfMeasure)
        .where(and(eq(unitsOfMeasure.tenantId, tenantId), eq(unitsOfMeasure.id, input.unitId), isNull(unitsOfMeasure.deletedAt)));
      if (!unit) throw new DomainError('CATALOG_UNIT_NOT_FOUND', 'Unit was not found in this tenant', 404, { field: 'unitId' });
      const [existing] = await tx
        .select()
        .from(itemUnits)
        .where(and(eq(itemUnits.itemId, itemId), eq(itemUnits.unitId, input.unitId)));
      const barcode = input.barcode === undefined ? existing?.barcode ?? null : normalizeOptionalBarcode(input.barcode) ?? null;
      const target = { itemId, unitId: input.unitId };
      if (barcode) await this.assertScanCodeAvailableInTx(tx, tenantId, barcode, target, 'barcode');
      if (existing && existing.ratio !== ratio.toFixed(6)) {
        const [movement] = await tx
          .select({ id: inventoryTransactions.id })
          .from(inventoryTransactions)
          .where(
            and(
              eq(inventoryTransactions.tenantId, tenantId),
              eq(inventoryTransactions.itemId, itemId),
              eq(inventoryTransactions.unitId, input.unitId),
            ),
          )
          .limit(1);
        if (movement) {
          throw new DomainError(
            'CATALOG_ITEM_UNIT_RATIO_LOCKED',
            'This unit has inventory history; create a new unit instead of changing its ratio',
            409,
            { field: 'ratio' },
          );
        }
      }
      if (input.isDefaultPurchase) {
        await tx.update(itemUnits).set({ isDefaultPurchase: false }).where(eq(itemUnits.itemId, itemId));
      }
      if (input.isDefaultSale) {
        await tx.update(itemUnits).set({ isDefaultSale: false }).where(eq(itemUnits.itemId, itemId));
      }
      const [row] = await tx
        .insert(itemUnits)
        .values({
          itemId,
          unitId: input.unitId,
          ratio: ratio.toFixed(6),
          barcode,
          salePrice: input.salePrice === undefined ? existing?.salePrice ?? null : input.salePrice,
          purchasePrice: input.purchasePrice === undefined ? existing?.purchasePrice ?? null : input.purchasePrice,
          isDefaultPurchase: input.isDefaultPurchase ?? existing?.isDefaultPurchase ?? false,
          isDefaultSale: input.isDefaultSale ?? existing?.isDefaultSale ?? false,
        })
        .onConflictDoUpdate({
          target: [itemUnits.itemId, itemUnits.unitId],
          set: {
            ratio: ratio.toFixed(6),
            barcode,
            salePrice: input.salePrice === undefined ? existing?.salePrice ?? null : input.salePrice,
            purchasePrice: input.purchasePrice === undefined ? existing?.purchasePrice ?? null : input.purchasePrice,
            isDefaultPurchase: input.isDefaultPurchase ?? existing?.isDefaultPurchase ?? false,
            isDefaultSale: input.isDefaultSale ?? existing?.isDefaultSale ?? false,
          },
        })
        .returning();
      await this.syncUnitBarcodeInTx(tx, tenantId, target, barcode);
      return row;
    });
  }

  async removeItemUnit(tenantId: string, itemId: string, unitId: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const item = await this.requireItem(tx, tenantId, itemId);
      if (unitId === item.baseUnitId) {
        throw new DomainError('CATALOG_ITEM_UNIT_IS_BASE', 'The base unit cannot be removed', 409);
      }
      const [movement] = await tx
        .select({ id: inventoryTransactions.id })
        .from(inventoryTransactions)
        .where(and(eq(inventoryTransactions.tenantId, tenantId), eq(inventoryTransactions.itemId, itemId), eq(inventoryTransactions.unitId, unitId)))
        .limit(1);
      if (movement) {
        throw new DomainError('CATALOG_ITEM_UNIT_IN_USE', 'Unit has inventory history and cannot be removed', 409);
      }
      const [deleted] = await tx
        .delete(itemUnits)
        .where(and(eq(itemUnits.itemId, itemId), eq(itemUnits.unitId, unitId)))
        .returning();
      if (!deleted) throw new DomainError(errorCodes.NOT_FOUND, 'Item unit was not found', 404);
      await tx.delete(itemBarcodes).where(and(eq(itemBarcodes.tenantId, tenantId), eq(itemBarcodes.itemId, itemId), eq(itemBarcodes.unitId, unitId)));
      return { itemId, unitId, deleted: true };
    });
  }

  async listItemBarcodes(tenantId: string, itemId: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const item = await this.requireItem(tx, tenantId, itemId);
      const barcodes = await tx
        .select()
        .from(itemBarcodes)
        .where(and(eq(itemBarcodes.tenantId, tenantId), eq(itemBarcodes.itemId, itemId)))
        .orderBy(asc(itemBarcodes.barcode));
      return {
        primary: item.barcode ? { barcode: item.barcode, unitId: item.baseUnitId, primary: true } : null,
        barcodes: barcodes.map((barcode) => ({ ...barcode, primary: barcode.barcode === item.barcode })),
      };
    });
  }

  async addItemBarcode(tenantId: string, itemId: string, input: ItemBarcodeInput) {
    const barcode = normalizeRequiredScanCode(input.barcode, 'barcode');
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const item = await this.requireItem(tx, tenantId, itemId);
      const unitId = input.unitId ?? item.baseUnitId;
      const target = { itemId, unitId };
      await this.assertItemUnitInTx(tx, tenantId, item, unitId);
      await this.assertScanCodeAvailableInTx(tx, tenantId, barcode, target);
      const [existing] = await tx
        .select()
        .from(itemBarcodes)
        .where(and(eq(itemBarcodes.tenantId, tenantId), eq(itemBarcodes.barcode, barcode)));
      // Idempotent when the code already identifies the same concrete item unit.
      // In particular, adding the primary or unit barcode from the list screen must
      // never demote its managed source to an ordinary alternate code.
      if (existing) return existing;
      const [row] = await tx
        .insert(itemBarcodes)
        .values({ tenantId, barcode, itemId, unitId, source: 'alternate' })
        .returning();
      return row;
    });
  }

  async removeItemBarcode(tenantId: string, itemId: string, rawBarcode: string) {
    const barcode = normalizeRequiredScanCode(rawBarcode, 'barcode');
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const item = await this.requireItem(tx, tenantId, itemId);
      const [existing] = await tx
        .select()
        .from(itemBarcodes)
        .where(and(eq(itemBarcodes.tenantId, tenantId), eq(itemBarcodes.itemId, itemId), eq(itemBarcodes.barcode, barcode)));
      if (!existing) throw new DomainError(errorCodes.NOT_FOUND, 'Barcode was not found', 404);

      // Keep the two backward-compatible fields and the canonical registry in lockstep
      // even when a caller deletes a managed barcode through the generic barcode list.
      if (item.barcode === barcode || existing.source === 'primary') {
        await tx
          .update(items)
          .set({ barcode: null, updatedAt: new Date() })
          .where(and(eq(items.tenantId, tenantId), eq(items.id, itemId)));
      }
      if (existing.unitId && existing.source === 'unit') {
        await tx
          .update(itemUnits)
          .set({ barcode: null })
          .where(and(eq(itemUnits.itemId, itemId), eq(itemUnits.unitId, existing.unitId)));
      }
      await tx
        .delete(itemBarcodes)
        .where(and(eq(itemBarcodes.tenantId, tenantId), eq(itemBarcodes.itemId, itemId), eq(itemBarcodes.barcode, barcode)));
      return { barcode, deleted: true, source: existing.source };
    });
  }

  /** Legacy/supplier codes participate in scanner lookup but remain separately labelled on the item card. */
  async listItemAlternativeCodes(tenantId: string, itemId: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      await this.requireItem(tx, tenantId, itemId);
      return tx
        .select()
        .from(itemAlternativeCodes)
        .where(
          and(
            eq(itemAlternativeCodes.tenantId, tenantId),
            eq(itemAlternativeCodes.itemId, itemId),
            isNull(itemAlternativeCodes.deletedAt),
          ),
        )
        .orderBy(asc(itemAlternativeCodes.code));
    });
  }

  /**
   * An alternative code is a scanner code, not unstructured supplier text. It must therefore
   * be unique across SKUs, primary/unit barcodes and other alternative codes in this tenant.
   * Re-saving the same code updates its note so the item card can make the operation idempotent.
   */
  async upsertItemAlternativeCode(tenantId: string, itemId: string, input: ItemAlternativeCodeInput) {
    const code = normalizeRequiredScanCode(input.code, 'code');
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const item = await this.requireItem(tx, tenantId, itemId);
      const [existing] = await tx
        .select()
        .from(itemAlternativeCodes)
        .where(
          and(
            eq(itemAlternativeCodes.tenantId, tenantId),
            eq(itemAlternativeCodes.itemId, itemId),
            eq(itemAlternativeCodes.code, code),
            isNull(itemAlternativeCodes.deletedAt),
          ),
        );
      if (existing) {
        const [updated] = await tx
          .update(itemAlternativeCodes)
          .set({ notes: input.notes === undefined ? existing.notes : input.notes || null, updatedAt: new Date() })
          .where(eq(itemAlternativeCodes.id, existing.id))
          .returning();
        return updated;
      }
      await this.assertScanCodeAvailableInTx(tx, tenantId, code, { itemId, unitId: item.baseUnitId }, 'code', false);
      const [created] = await tx
        .insert(itemAlternativeCodes)
        .values({
          id: newId(),
          tenantId,
          itemId,
          code,
          notes: input.notes || null,
        })
        .returning();
      return created;
    });
  }

  /** Soft deletion preserves the audit trail while making a code immediately available again. */
  async removeItemAlternativeCode(tenantId: string, itemId: string, rawCode: string) {
    const code = normalizeRequiredScanCode(rawCode, 'code');
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      await this.requireItem(tx, tenantId, itemId);
      const [removed] = await tx
        .update(itemAlternativeCodes)
        .set({ deletedAt: new Date(), updatedAt: new Date() })
        .where(
          and(
            eq(itemAlternativeCodes.tenantId, tenantId),
            eq(itemAlternativeCodes.itemId, itemId),
            eq(itemAlternativeCodes.code, code),
            isNull(itemAlternativeCodes.deletedAt),
          ),
        )
        .returning();
      if (!removed) throw new DomainError(errorCodes.NOT_FOUND, 'Alternative code was not found', 404);
      return { code, deleted: true };
    });
  }

  /** Barcode/SKU scan lookup returns the concrete unit and its conversion ratio. */
  async lookupItem(tenantId: string, code: string) {
    const normalized = code.trim();
    if (!normalized) throw new DomainError('CATALOG_LOOKUP_CODE_REQUIRED', 'Barcode or SKU is required', 422, { field: 'code' });
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      let itemId: string | undefined;
      let unitId: string | undefined;
      let matchedAs: 'sku' | 'primary_barcode' | 'barcode' | 'unit_barcode' | 'alternative_code' | undefined;
      const [direct] = await tx
        .select({ id: items.id, baseUnitId: items.baseUnitId, sku: items.sku, barcode: items.barcode })
        .from(items)
        .where(
          and(
            eq(items.tenantId, tenantId),
            isNull(items.deletedAt),
            or(eq(items.sku, normalized), eq(items.barcode, normalized)),
          ),
        );
      if (direct) {
        itemId = direct.id;
        unitId = direct.baseUnitId;
        matchedAs = direct.sku === normalized ? 'sku' : 'primary_barcode';
      }
      if (!itemId) {
        const [barcode] = await tx
          .select({ itemId: itemBarcodes.itemId, unitId: itemBarcodes.unitId, source: itemBarcodes.source })
          .from(itemBarcodes)
          .innerJoin(items, eq(items.id, itemBarcodes.itemId))
          .where(and(eq(itemBarcodes.tenantId, tenantId), eq(itemBarcodes.barcode, normalized), eq(items.tenantId, tenantId), isNull(items.deletedAt)));
        if (barcode) {
          itemId = barcode.itemId;
          unitId = barcode.unitId ?? undefined;
          matchedAs = barcode.source === 'primary' ? 'primary_barcode' : barcode.source === 'unit' ? 'unit_barcode' : 'barcode';
        }
      }
      if (!itemId) {
        const [unitBarcode] = await tx
          .select({ itemId: itemUnits.itemId, unitId: itemUnits.unitId })
          .from(itemUnits)
          .innerJoin(items, eq(items.id, itemUnits.itemId))
          .where(and(eq(items.tenantId, tenantId), isNull(items.deletedAt), eq(itemUnits.barcode, normalized)));
        if (unitBarcode) {
          itemId = unitBarcode.itemId;
          unitId = unitBarcode.unitId;
          matchedAs = 'unit_barcode';
        }
      }
      if (!itemId) {
        const [alternative] = await tx
          .select({ itemId: itemAlternativeCodes.itemId })
          .from(itemAlternativeCodes)
          .innerJoin(items, eq(items.id, itemAlternativeCodes.itemId))
          .where(and(eq(itemAlternativeCodes.tenantId, tenantId), eq(itemAlternativeCodes.code, normalized), isNull(itemAlternativeCodes.deletedAt), eq(items.tenantId, tenantId), isNull(items.deletedAt)));
        if (alternative) {
          itemId = alternative.itemId;
          matchedAs = 'alternative_code';
        }
      }
      if (!itemId) throw new DomainError(errorCodes.NOT_FOUND, 'No item matches this barcode or SKU', 404);
      const item = await this.requireItem(tx, tenantId, itemId);
      const concreteUnitId = unitId ?? item.baseUnitId;
      const unit = await this.assertItemUnitInTx(tx, tenantId, item, concreteUnitId);
      return {
        item,
        unitId: concreteUnitId,
        ratio: unit.ratio,
        salePrice: unit.salePrice ?? item.salePrice,
        purchasePrice: unit.purchasePrice ?? item.purchasePrice,
        matchedAs,
      };
    });
  }

  /**
   * A scanned value may be a SKU, primary barcode, unit barcode, registry barcode
   * or legacy alternative code.  All of them must resolve to exactly one item unit.
   * The advisory transaction lock closes the otherwise possible race between two
   * writers checking different legacy tables before either inserts its registry row.
   */
  private async assertScanCodeAvailableInTx(
    tx: DrizzleTx,
    tenantId: string,
    code: string,
    target: ScanCodeTarget,
    field: 'sku' | 'barcode' | 'code' = 'barcode',
    allowSameTarget = true,
  ) {
    await tx.execute(sql`SELECT pg_advisory_xact_lock(hashtext(${`${tenantId}:${code}`}))`);
    const result = await tx.execute(sql`
      SELECT i.id AS item_id, i.base_unit_id AS unit_id, 'sku' AS source
      FROM items i
      WHERE i.tenant_id = ${tenantId} AND i.deleted_at IS NULL AND i.sku = ${code}

      UNION ALL

      SELECT i.id AS item_id, i.base_unit_id AS unit_id, 'primary_barcode' AS source
      FROM items i
      WHERE i.tenant_id = ${tenantId} AND i.deleted_at IS NULL AND i.barcode = ${code}

      UNION ALL

      SELECT iu.item_id, iu.unit_id, 'unit_barcode' AS source
      FROM item_units iu
      INNER JOIN items i ON i.id = iu.item_id
      WHERE i.tenant_id = ${tenantId} AND i.deleted_at IS NULL AND iu.barcode = ${code}

      UNION ALL

      SELECT ib.item_id, COALESCE(ib.unit_id, i.base_unit_id) AS unit_id, 'barcode_registry' AS source
      FROM item_barcodes ib
      INNER JOIN items i ON i.id = ib.item_id
      WHERE ib.tenant_id = ${tenantId} AND ib.barcode = ${code}

      UNION ALL

      SELECT iac.item_id, i.base_unit_id AS unit_id, 'alternative_code' AS source
      FROM item_alternative_codes iac
      INNER JOIN items i ON i.id = iac.item_id
      WHERE iac.tenant_id = ${tenantId} AND iac.deleted_at IS NULL AND iac.code = ${code}
    `);
    const conflicts = (result.rows as ScanCodeConflict[]).filter(
      (row) => !allowSameTarget || row.item_id !== target.itemId || row.unit_id !== target.unitId,
    );
    if (conflicts.length > 0) {
      throw new DomainError(
        'CATALOG_BARCODE_IN_USE',
        'This scanner code is already assigned to another item or unit',
        409,
        { field, code, source: conflicts[0]?.source },
      );
    }
  }

  private async syncPrimaryBarcodeInTx(
    tx: DrizzleTx,
    tenantId: string,
    target: ScanCodeTarget,
    barcode: string | null,
  ) {
    return this.syncManagedBarcodeInTx(tx, tenantId, target, 'primary', barcode);
  }

  private async syncUnitBarcodeInTx(
    tx: DrizzleTx,
    tenantId: string,
    target: ScanCodeTarget,
    barcode: string | null,
  ) {
    return this.syncManagedBarcodeInTx(tx, tenantId, target, 'unit', barcode);
  }

  /**
   * `items.barcode` and `item_units.barcode` remain compatibility/display fields,
   * while `item_barcodes` is the tenant-scoped canonical registry.  A managed source
   * owns exactly one barcode for its concrete item unit; ordinary alternate rows are
   * intentionally left alone when a card changes its primary/unit barcode.
   */
  private async syncManagedBarcodeInTx(
    tx: DrizzleTx,
    tenantId: string,
    target: ScanCodeTarget,
    source: Extract<BarcodeSource, 'primary' | 'unit'>,
    barcode: string | null,
  ) {
    await tx
      .delete(itemBarcodes)
      .where(
        and(
          eq(itemBarcodes.tenantId, tenantId),
          eq(itemBarcodes.itemId, target.itemId),
          eq(itemBarcodes.unitId, target.unitId),
          eq(itemBarcodes.source, source),
          barcode ? ne(itemBarcodes.barcode, barcode) : undefined,
        ),
      );
    if (!barcode) return;

    const [existing] = await tx
      .select()
      .from(itemBarcodes)
      .where(and(eq(itemBarcodes.tenantId, tenantId), eq(itemBarcodes.barcode, barcode)));
    if (!existing) {
      await tx.insert(itemBarcodes).values({
        tenantId,
        barcode,
        itemId: target.itemId,
        unitId: target.unitId,
        source,
      });
      return;
    }
    if (existing.itemId !== target.itemId || existing.unitId !== target.unitId) {
      // `assertScanCodeAvailableInTx` normally catches this. Keep the guard here as
      // a defensive barrier should a future caller bypass that helper.
      throw new DomainError(
        'CATALOG_BARCODE_IN_USE',
        'This scanner code is already assigned to another item or unit',
        409,
        { field: 'barcode', code: barcode },
      );
    }
    const rank: Record<BarcodeSource, number> = { alternate: 0, unit: 1, primary: 2 };
    const existingRank = rank[existing.source as BarcodeSource] ?? rank.alternate;
    if (rank[source] > existingRank) {
      await tx
        .update(itemBarcodes)
        .set({ source })
        .where(and(eq(itemBarcodes.tenantId, tenantId), eq(itemBarcodes.barcode, barcode)));
    }
  }

  private async assertItemUnitInTx(
    tx: DrizzleTx,
    tenantId: string,
    item: { id: string; baseUnitId: string; salePrice: string | null; purchasePrice: string | null },
    unitId: string,
  ) {
    if (unitId === item.baseUnitId) {
      return { ratio: '1.000000', salePrice: item.salePrice, purchasePrice: item.purchasePrice };
    }
    const [unit] = await tx
      .select({ ratio: itemUnits.ratio, salePrice: itemUnits.salePrice, purchasePrice: itemUnits.purchasePrice })
      .from(itemUnits)
      .innerJoin(unitsOfMeasure, eq(unitsOfMeasure.id, itemUnits.unitId))
      .where(
        and(
          eq(itemUnits.itemId, item.id),
          eq(itemUnits.unitId, unitId),
          eq(unitsOfMeasure.tenantId, tenantId),
          isNull(unitsOfMeasure.deletedAt),
        ),
      );
    if (!unit) throw new DomainError('CATALOG_ITEM_UNIT_INVALID', 'Unit is not configured for this item', 422, { field: 'unitId' });
    return unit;
  }

  private async assertItemReferencesInTx(
    tx: DrizzleTx,
    tenantId: string,
    categoryId: string,
    baseUnitId: string,
    taxGroupId?: string,
  ) {
    const [category] = await tx
      .select({ id: itemCategories.id })
      .from(itemCategories)
      .where(and(eq(itemCategories.tenantId, tenantId), eq(itemCategories.id, categoryId), isNull(itemCategories.deletedAt)));
    if (!category) throw new DomainError('CATALOG_CATEGORY_NOT_FOUND', 'Category was not found in this tenant', 404, { field: 'categoryId' });
    const [unit] = await tx
      .select({ id: unitsOfMeasure.id })
      .from(unitsOfMeasure)
      .where(and(eq(unitsOfMeasure.tenantId, tenantId), eq(unitsOfMeasure.id, baseUnitId), isNull(unitsOfMeasure.deletedAt)));
    if (!unit) throw new DomainError('CATALOG_UNIT_NOT_FOUND', 'Base unit was not found in this tenant', 404, { field: 'baseUnitId' });
    if (taxGroupId) {
      const [tax] = await tx
        .select({ id: taxGroups.id })
        .from(taxGroups)
        .where(and(eq(taxGroups.tenantId, tenantId), eq(taxGroups.id, taxGroupId), isNull(taxGroups.deletedAt)));
      if (!tax) throw new DomainError('CATALOG_TAX_GROUP_NOT_FOUND', 'Tax group was not found in this tenant', 404, { field: 'taxGroupId' });
    }
  }

  private assertItemSettings(input: {
    minQty?: string | null;
    maxQty?: string | null;
    maxDiscountPct?: string | null;
    maxDiscountAmt?: string | null;
    trackLot?: boolean;
    trackSerial?: boolean;
  }) {
    const parseDecimal = (value: string | null | undefined) => new Decimal(value ?? '0');
    const min = parseDecimal(input.minQty);
    if (!min.isFinite() || min.lt(0)) {
      throw new DomainError('CATALOG_REORDER_LIMIT_INVALID', 'Minimum quantity must be a non-negative number', 422, { field: 'minQty' });
    }
    if (input.maxQty !== undefined && input.maxQty !== null) {
      const max = parseDecimal(input.maxQty);
      if (!max.isFinite() || max.lt(min)) {
        throw new DomainError('CATALOG_REORDER_LIMIT_INVALID', 'Maximum quantity must be at least the minimum quantity', 422, { field: 'maxQty' });
      }
    }
    for (const [field, value] of [
      ['maxDiscountPct', input.maxDiscountPct],
      ['maxDiscountAmt', input.maxDiscountAmt],
    ] as const) {
      if (value === undefined || value === null) continue;
      const parsed = parseDecimal(value);
      if (!parsed.isFinite() || parsed.lt(0) || (field === 'maxDiscountPct' && parsed.gt(100))) {
        throw new DomainError('CATALOG_DISCOUNT_LIMIT_INVALID', 'Discount limits must be non-negative (percentage at most 100)', 422, { field });
      }
    }
  }

  private async requireItem(tx: DrizzleTx, tenantId: string, id: string) {
    const [row] = await tx.select().from(items).where(and(eq(items.tenantId, tenantId), eq(items.id, id), isNull(items.deletedAt)));
    if (!row) throw new DomainError(errorCodes.NOT_FOUND, 'Item was not found', 404);
    return row;
  }

  /** Any inventory movement or invoice line is enough to make the item historical. */
  private async itemHasMovements(tx: DrizzleTx, tenantId: string, id: string) {
    const result = await tx.execute(sql`
      SELECT EXISTS (SELECT 1 FROM inventory_transactions WHERE tenant_id = ${tenantId} AND item_id = ${id})
          OR EXISTS (SELECT 1 FROM sales_invoice_lines WHERE tenant_id = ${tenantId} AND item_id = ${id})
          OR EXISTS (SELECT 1 FROM purchase_invoice_lines WHERE tenant_id = ${tenantId} AND item_id = ${id}) AS used
    `);
    return Boolean((result.rows[0] as { used: boolean }).used);
  }

  async listCategories(tenantId: string) {
    return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(itemCategories).where(and(eq(itemCategories.tenantId, tenantId), isNull(itemCategories.deletedAt))).orderBy(asc(itemCategories.nameAr)));
  }

  /**
   * An item cannot exist without a category and a base unit, so the two directories
   * below are part of the same module: without them `POST items` is unusable from a UI.
   */
  async createCategory(tenantId: string, input: CategoryInput) {
    const id = newId();
    await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(itemCategories).values({ id, tenantId, code: input.code, nameAr: input.nameAr, nameEn: input.nameEn, parentId: input.parentId }));
    const rows = await this.listCategories(tenantId);
    return rows.find((row) => row.id === id);
  }

  async updateCategory(tenantId: string, id: string, patch: CategoryPatch) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [current] = await tx.select().from(itemCategories).where(and(eq(itemCategories.tenantId, tenantId), eq(itemCategories.id, id), isNull(itemCategories.deletedAt)));
      if (!current) throw new DomainError(errorCodes.NOT_FOUND, 'Category was not found', 404);
      if (patch.parentId === id) throw new DomainError(errorCodes.VALIDATION_FAILED, 'A category cannot be its own parent', 422);
      const [row] = await tx
        .update(itemCategories)
        .set({
          code: patch.code ?? current.code,
          nameAr: patch.nameAr ?? current.nameAr,
          nameEn: patch.nameEn === undefined ? current.nameEn : patch.nameEn,
          parentId: patch.parentId === undefined ? current.parentId : patch.parentId,
          updatedAt: new Date(),
        })
        .where(and(eq(itemCategories.tenantId, tenantId), eq(itemCategories.id, id)))
        .returning();
      return row;
    });
  }

  /** A category with items behind it is kept: removing it would leave those items nameless. */
  async removeCategory(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const inUse = await tx.execute(sql`SELECT EXISTS (SELECT 1 FROM items WHERE tenant_id = ${tenantId} AND category_id = ${id} AND deleted_at IS NULL) AS used`);
      if ((inUse.rows[0] as { used: boolean }).used) {
        throw new DomainError('CATEGORY_IN_USE', 'Move the items to another category before deleting this one', 409);
      }
      const result = await tx.update(itemCategories).set({ deletedAt: new Date(), updatedAt: new Date() }).where(and(eq(itemCategories.tenantId, tenantId), eq(itemCategories.id, id), isNull(itemCategories.deletedAt)));
      if (!result.rowCount) throw new DomainError(errorCodes.NOT_FOUND, 'Category was not found', 404);
      return { id, deleted: true };
    });
  }

  async listUnits(tenantId: string) {
    return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(unitsOfMeasure).where(and(eq(unitsOfMeasure.tenantId, tenantId), isNull(unitsOfMeasure.deletedAt))).orderBy(asc(unitsOfMeasure.code)));
  }

  async createUnit(tenantId: string, input: UnitInput) {
    const id = newId();
    await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(unitsOfMeasure).values({ id, tenantId, code: input.code, nameAr: input.nameAr, nameEn: input.nameEn }));
    const rows = await this.listUnits(tenantId);
    return rows.find((row) => row.id === id);
  }

  async updateUnit(tenantId: string, id: string, patch: UnitPatch) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [current] = await tx.select().from(unitsOfMeasure).where(and(eq(unitsOfMeasure.tenantId, tenantId), eq(unitsOfMeasure.id, id), isNull(unitsOfMeasure.deletedAt)));
      if (!current) throw new DomainError(errorCodes.NOT_FOUND, 'Unit was not found', 404);
      const [row] = await tx
        .update(unitsOfMeasure)
        .set({ code: patch.code ?? current.code, nameAr: patch.nameAr ?? current.nameAr, nameEn: patch.nameEn === undefined ? current.nameEn : patch.nameEn, updatedAt: new Date() })
        .where(and(eq(unitsOfMeasure.tenantId, tenantId), eq(unitsOfMeasure.id, id)))
        .returning();
      return row;
    });
  }

  /** A unit that is some item's base unit is the meaning of that item's quantities. */
  async removeUnit(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const inUse = await tx.execute(sql`SELECT EXISTS (SELECT 1 FROM items WHERE tenant_id = ${tenantId} AND base_unit_id = ${id} AND deleted_at IS NULL) AS used`);
      if ((inUse.rows[0] as { used: boolean }).used) {
        throw new DomainError('UNIT_IN_USE', 'This unit is the base unit of at least one item', 409);
      }
      const result = await tx.update(unitsOfMeasure).set({ deletedAt: new Date(), updatedAt: new Date() }).where(and(eq(unitsOfMeasure.tenantId, tenantId), eq(unitsOfMeasure.id, id), isNull(unitsOfMeasure.deletedAt)));
      if (!result.rowCount) throw new DomainError(errorCodes.NOT_FOUND, 'Unit was not found', 404);
      return { id, deleted: true };
    });
  }

  async listTaxGroups(tenantId: string) {
    return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(taxGroups).where(and(eq(taxGroups.tenantId, tenantId), isNull(taxGroups.deletedAt))).orderBy(asc(taxGroups.nameAr)));
  }

  async createTaxGroup(tenantId: string, input: TaxGroupInput) {
    const id = newId();
    await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(taxGroups).values({ id, tenantId, nameAr: input.nameAr, nameEn: input.nameEn, rate: input.rate, vatAccountId: input.vatAccountId, isInclusiveDefault: input.isInclusiveDefault ?? false }));
    const rows = await this.listTaxGroups(tenantId);
    return rows.find((row) => row.id === id);
  }

  /**
   * A tax group's **rate** is deliberately editable only while unused. Every posted
   * invoice stored the rate it was issued with, so editing the group does not rewrite
   * them — but it would silently change what the same group means going forward, which is
   * how a 15% invoice ends up next to a 5% invoice under one name. Create a new group for
   * a new rate; rename this one freely.
   */
  async updateTaxGroup(tenantId: string, id: string, patch: TaxGroupPatch) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [current] = await tx.select().from(taxGroups).where(and(eq(taxGroups.tenantId, tenantId), eq(taxGroups.id, id), isNull(taxGroups.deletedAt)));
      if (!current) throw new DomainError(errorCodes.NOT_FOUND, 'Tax group was not found', 404);
      if (patch.rate && patch.rate !== current.rate) {
        const used = await tx.execute(sql`SELECT EXISTS (SELECT 1 FROM sales_invoice_lines WHERE tenant_id = ${tenantId} AND tax_group_id = ${id}) AS used`);
        if ((used.rows[0] as { used: boolean }).used) {
          throw new DomainError('TAX_GROUP_RATE_LOCKED', 'This tax group has already been used on invoices; create a new group for the new rate', 409);
        }
      }
      const [row] = await tx
        .update(taxGroups)
        .set({
          nameAr: patch.nameAr ?? current.nameAr,
          nameEn: patch.nameEn === undefined ? current.nameEn : patch.nameEn,
          rate: patch.rate ?? current.rate,
          vatAccountId: patch.vatAccountId === undefined ? current.vatAccountId : patch.vatAccountId,
          isInclusiveDefault: patch.isInclusiveDefault ?? current.isInclusiveDefault,
          updatedAt: new Date(),
        })
        .where(and(eq(taxGroups.tenantId, tenantId), eq(taxGroups.id, id)))
        .returning();
      return row;
    });
  }
}
