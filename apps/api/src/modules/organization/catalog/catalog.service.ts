import { Injectable, Inject } from '@nestjs/common';
import { and, asc, eq, ilike, isNull } from 'drizzle-orm';
import { itemCategories, items, newId, withTenantTx, type DatabaseHandle } from '@erp/database';

import { DATABASE_HANDLE } from '../../../database/database.module.js';

export type CatalogItemInput = { sku: string; nameAr: string; nameEn?: string; categoryId: string; baseUnitId: string; kind?: 'stock' | 'service' | 'composite'; salePrice?: string; purchasePrice?: string; taxGroupId?: string };

@Injectable()
export class CatalogService {
  constructor(@Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle) {}

  async listItems(tenantId: string, q?: string) {
    return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(items).where(and(eq(items.tenantId, tenantId), isNull(items.deletedAt), q ? ilike(items.nameAr, `%${q}%`) : undefined)).orderBy(asc(items.nameAr)).limit(100));
  }

  async createItem(tenantId: string, input: CatalogItemInput) {
    const id = newId();
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      await tx.insert(items).values({ id, tenantId, sku: input.sku, nameAr: input.nameAr, nameEn: input.nameEn, categoryId: input.categoryId, baseUnitId: input.baseUnitId, kind: input.kind ?? 'stock', salePrice: input.salePrice, purchasePrice: input.purchasePrice, taxGroupId: input.taxGroupId });
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
}
