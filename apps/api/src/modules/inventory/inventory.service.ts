/* eslint-disable no-restricted-syntax */
import { Inject, Injectable } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { and, asc, eq, sql } from 'drizzle-orm';
import { DomainError, newId } from '@erp/contracts';
import { inventoryTransactions, stockBalances, withTenantTx, type DatabaseHandle } from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';

export type InventoryLine = { itemId: string; warehouseId: string; qty: string; unitCost?: string; direction: 'in' | 'out'; docType: string; docId: string; lineId?: string; costing?: 'inWithCost' | 'outAtAvg' | 'returnAtOriginalCost'; lotId?: string; serialId?: string };

@Injectable()
export class InventoryService {
  constructor(@Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle) {}

  async record(tenantId: string, lines: InventoryLine[]) {
    if (!lines.length) throw new DomainError('INVENTORY_LINES_REQUIRED', 'At least one inventory line is required', 422);
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const created: string[] = [];
      for (const line of lines) {
        const quantity = new Decimal(line.qty);
        if (!quantity.isFinite() || quantity.lte(0)) throw new DomainError('INVALID_STOCK_QUANTITY', 'Quantity must be positive', 422);
        const balances = await tx.select().from(stockBalances).where(and(eq(stockBalances.tenantId, tenantId), eq(stockBalances.itemId, line.itemId), eq(stockBalances.warehouseId, line.warehouseId)));
        const balance = balances[0];
        const currentQty = new Decimal(balance?.quantity ?? '0');
        const currentValue = new Decimal(balance?.value ?? '0');
        const average = currentQty.gt(0) ? currentValue.div(currentQty) : new Decimal(line.unitCost ?? '0');
        const unitCost = line.direction === 'in' ? new Decimal(line.unitCost ?? '0') : average;
        const nextQty = line.direction === 'in' ? currentQty.plus(quantity) : currentQty.minus(quantity);
        if (nextQty.lt(0)) throw new DomainError('STOCK_INSUFFICIENT', 'Stock is insufficient for this movement', 422);
        const nextValue = line.direction === 'in' ? currentValue.plus(quantity.mul(unitCost)) : currentValue.minus(quantity.mul(average));
        const id = newId();
        await tx.insert(inventoryTransactions).values({ id, tenantId, itemId: line.itemId, warehouseId: line.warehouseId, occurredAt: new Date(), docType: line.docType, docId: line.docId, lineId: line.lineId, direction: line.direction, qty: line.qty, baseQty: line.qty, unitCost: unitCost.toFixed(4), totalCost: quantity.mul(unitCost).toFixed(4), costing: line.costing ?? (line.direction === 'in' ? 'inWithCost' : 'outAtAvg'), lotId: line.lotId, serialId: line.serialId });
        await tx.insert(stockBalances).values({ tenantId, itemId: line.itemId, warehouseId: line.warehouseId, quantity: nextQty.toFixed(4), value: nextValue.toFixed(4), averageCost: nextQty.isZero() ? '0' : nextValue.div(nextQty).toFixed(4), version: 1, updatedAt: new Date() }).onConflictDoUpdate({ target: [stockBalances.tenantId, stockBalances.itemId, stockBalances.warehouseId], set: { quantity: nextQty.toFixed(4), value: nextValue.toFixed(4), averageCost: nextQty.isZero() ? '0' : nextValue.div(nextQty).toFixed(4), version: sql`${stockBalances.version} + 1`, updatedAt: new Date() } });
        created.push(id);
      }
      return { transactionIds: created };
    });
  }

  levels(tenantId: string, warehouseId?: string, itemId?: string) { return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(stockBalances).where(and(eq(stockBalances.tenantId, tenantId), warehouseId ? eq(stockBalances.warehouseId, warehouseId) : undefined, itemId ? eq(stockBalances.itemId, itemId) : undefined)).orderBy(asc(stockBalances.itemId))); }
  movements(tenantId: string, itemId?: string, warehouseId?: string) { return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(inventoryTransactions).where(and(eq(inventoryTransactions.tenantId, tenantId), itemId ? eq(inventoryTransactions.itemId, itemId) : undefined, warehouseId ? eq(inventoryTransactions.warehouseId, warehouseId) : undefined)).orderBy(asc(inventoryTransactions.occurredAt))); }

  async valuationAsOf(tenantId: string, asOf: Date, warehouseId?: string, itemId?: string) {
    const rows = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(inventoryTransactions).where(and(eq(inventoryTransactions.tenantId, tenantId), sql`${inventoryTransactions.occurredAt} <= ${asOf}`, warehouseId ? eq(inventoryTransactions.warehouseId, warehouseId) : undefined, itemId ? eq(inventoryTransactions.itemId, itemId) : undefined)).orderBy(asc(inventoryTransactions.occurredAt)));
    const totals = new Map<string, { quantity: Decimal; value: Decimal }>();
    for (const row of rows) {
      const key = `${row.itemId}:${row.warehouseId}`;
      const current = totals.get(key) ?? { quantity: new Decimal(0), value: new Decimal(0) };
      const quantity = new Decimal(row.baseQty);
      const value = new Decimal(row.totalCost);
      if (row.direction === 'in') { current.quantity = current.quantity.plus(quantity); current.value = current.value.plus(value); } else { current.quantity = current.quantity.minus(quantity); current.value = current.value.minus(value); }
      totals.set(key, current);
    }
    return [...totals].map(([key, total]) => { const parts = key.split(':'); const resultItemId = parts[0]; const resultWarehouseId = parts[1]; if (!resultItemId || !resultWarehouseId) throw new DomainError('INVENTORY_REPLAY_INVALID_KEY', 'Inventory replay produced an invalid balance key', 500); return { itemId: resultItemId, warehouseId: resultWarehouseId, quantity: total.quantity.toFixed(4), value: total.value.toFixed(4), averageCost: total.quantity.isZero() ? '0.0000' : total.value.div(total.quantity).toFixed(4) }; });
  }

  async recomputeBalances(tenantId: string, warehouseId?: string, itemId?: string) {
    const valuation = await this.valuationAsOf(tenantId, new Date('9999-12-31T23:59:59.999Z'), warehouseId, itemId);
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      for (const row of valuation) {
        await tx.insert(stockBalances).values({ tenantId, itemId: row.itemId, warehouseId: row.warehouseId, quantity: row.quantity, value: row.value, averageCost: row.averageCost, version: 1, updatedAt: new Date() }).onConflictDoUpdate({ target: [stockBalances.tenantId, stockBalances.itemId, stockBalances.warehouseId], set: { quantity: row.quantity, value: row.value, averageCost: row.averageCost, version: sql`${stockBalances.version} + 1`, updatedAt: new Date() } });
      }
      return { recomputed: valuation.length };
    });
  }
}
