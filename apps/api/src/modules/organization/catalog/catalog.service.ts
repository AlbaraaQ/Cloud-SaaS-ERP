import { Injectable, Inject } from '@nestjs/common';
import { and, asc, eq, ilike, isNull, sql } from 'drizzle-orm';
import { DomainError, errorCodes } from '@erp/contracts';
import { itemCategories, items, newId, taxGroups, unitsOfMeasure, withTenantTx, type DatabaseHandle, type DrizzleTx } from '@erp/database';

import { DATABASE_HANDLE } from '../../../database/database.module.js';

export type CatalogItemInput = { sku: string; barcode?: string; nameAr: string; nameEn?: string; categoryId: string; baseUnitId: string; kind?: 'stock' | 'service' | 'composite'; salePrice?: string; purchasePrice?: string; taxGroupId?: string };
export type CategoryInput = { code: string; nameAr: string; nameEn?: string; parentId?: string };
export type UnitInput = { code: string; nameAr: string; nameEn?: string };
export type TaxGroupInput = { nameAr: string; nameEn?: string; rate: string; vatAccountId?: string; isInclusiveDefault?: boolean };

export type CatalogItemPatch = { sku?: string; barcode?: string | null; nameAr?: string; nameEn?: string | null; categoryId?: string; kind?: 'stock' | 'service' | 'composite'; salePrice?: string; purchasePrice?: string; taxGroupId?: string | null; showInPos?: boolean };
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

@Injectable()
export class CatalogService {
  constructor(@Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle) {}

  async listItems(tenantId: string, q?: string) {
    return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(items).where(and(eq(items.tenantId, tenantId), isNull(items.deletedAt), q ? ilike(items.nameAr, `%${q}%`) : undefined)).orderBy(asc(items.nameAr)).limit(100));
  }

  async createItem(tenantId: string, input: CatalogItemInput) {
    const id = newId();
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      await tx.insert(items).values({ id, tenantId, sku: input.sku, barcode: input.barcode, nameAr: input.nameAr, nameEn: input.nameEn, categoryId: input.categoryId, baseUnitId: input.baseUnitId, kind: input.kind ?? 'stock', salePrice: input.salePrice, purchasePrice: input.purchasePrice, taxGroupId: input.taxGroupId });
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
      if (patch.sku && patch.sku !== current.sku && (await this.itemHasMovements(tx, tenantId, id))) {
        throw new DomainError(IMMUTABLE_AFTER_USE, 'This item already has movements; its SKU can no longer be changed', 409);
      }
      await tx
        .update(items)
        .set({
          sku: patch.sku ?? current.sku,
          barcode: patch.barcode === undefined ? current.barcode : patch.barcode,
          nameAr: patch.nameAr ?? current.nameAr,
          nameEn: patch.nameEn === undefined ? current.nameEn : patch.nameEn,
          categoryId: patch.categoryId ?? current.categoryId,
          kind: patch.kind ?? current.kind,
          salePrice: patch.salePrice ?? current.salePrice,
          purchasePrice: patch.purchasePrice ?? current.purchasePrice,
          taxGroupId: patch.taxGroupId === undefined ? current.taxGroupId : patch.taxGroupId,
          showInPos: patch.showInPos ?? current.showInPos,
          updatedAt: new Date(),
        })
        .where(and(eq(items.tenantId, tenantId), eq(items.id, id)));
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
