import { Injectable, Inject } from '@nestjs/common';
import { and, asc, eq, ilike, isNull } from 'drizzle-orm';
import { itemCategories, items, newId, taxGroups, unitsOfMeasure, withTenantTx, type DatabaseHandle } from '@erp/database';

import { DATABASE_HANDLE } from '../../../database/database.module.js';

export type CatalogItemInput = { sku: string; barcode?: string; nameAr: string; nameEn?: string; categoryId: string; baseUnitId: string; kind?: 'stock' | 'service' | 'composite'; salePrice?: string; purchasePrice?: string; taxGroupId?: string };
export type CategoryInput = { code: string; nameAr: string; nameEn?: string; parentId?: string };
export type UnitInput = { code: string; nameAr: string; nameEn?: string };
export type TaxGroupInput = { nameAr: string; nameEn?: string; rate: string; vatAccountId?: string; isInclusiveDefault?: boolean };

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

  async getItem(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [row] = await tx.select().from(items).where(and(eq(items.id, id), eq(items.tenantId, tenantId), isNull(items.deletedAt)));
      return row;
    });
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

  async listUnits(tenantId: string) {
    return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(unitsOfMeasure).where(and(eq(unitsOfMeasure.tenantId, tenantId), isNull(unitsOfMeasure.deletedAt))).orderBy(asc(unitsOfMeasure.code)));
  }

  async createUnit(tenantId: string, input: UnitInput) {
    const id = newId();
    await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(unitsOfMeasure).values({ id, tenantId, code: input.code, nameAr: input.nameAr, nameEn: input.nameEn }));
    const rows = await this.listUnits(tenantId);
    return rows.find((row) => row.id === id);
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
}
