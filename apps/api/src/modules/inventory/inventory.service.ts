/* eslint-disable no-restricted-syntax */
import { Inject, Injectable } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { and, asc, eq, inArray, sql } from 'drizzle-orm';
import { DomainError, newId } from '@erp/contracts';
import {
  inventoryTransactions,
  itemLots,
  itemSerials,
  stockBalances,
  stockTransfers,
  stockTransferLines,
  withTenantTx,
  type DatabaseHandle,
  type DrizzleTx,
  type StockBalance,
} from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';

export type InventoryLine = {
  itemId: string;
  warehouseId: string;
  qty: string;
  unitCost?: string;
  direction: 'in' | 'out';
  docType: string;
  docId: string;
  lineId?: string;
  costing?: 'inWithCost' | 'outAtAvg' | 'returnAtOriginalCost';
  lotId?: string;
  serialId?: string;
};

@Injectable()
export class InventoryService {
  constructor(@Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle) {}

  /**
   * Transfer register. `createTransfer` and `receiveTransfer` existed without any way to
   * read the result back, which made the مناقلة screen impossible to build.
   */
  async listTransfers(tenantId: string, status?: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const rows = await tx
        .select()
        .from(stockTransfers)
        .where(and(eq(stockTransfers.tenantId, tenantId), status ? eq(stockTransfers.status, status) : undefined))
        .orderBy(sql`${stockTransfers.createdAt} DESC`)
        .limit(200);
      if (rows.length === 0) return [];
      const lines = await tx
        .select()
        .from(stockTransferLines)
        .where(and(eq(stockTransferLines.tenantId, tenantId), inArray(stockTransferLines.transferId, rows.map((row) => row.id))));
      return rows.map((row) => ({ ...row, lines: lines.filter((line) => line.transferId === row.id) }));
    });
  }

  async record(tenantId: string, lines: InventoryLine[]) {
    return withTenantTx(this.database.db, tenantId, (tx) => this.recordInTx(tx, tenantId, lines));
  }

  async recordInTx(tx: DrizzleTx, tenantId: string, lines: InventoryLine[]) {
    if (!lines.length) throw new DomainError('INVENTORY_LINES_REQUIRED', 'At least one inventory line is required', 422);
    const created: string[] = [];
    for (const line of lines) {
      const quantity = new Decimal(line.qty);
      if (!quantity.isFinite() || quantity.lte(0)) throw new DomainError('INVALID_STOCK_QUANTITY', 'Quantity must be positive', 422);

      await tx.execute(sql`
        INSERT INTO stock_balances (tenant_id, item_id, warehouse_id, quantity, value, average_cost, version, updated_at)
        VALUES (${tenantId}, ${line.itemId}, ${line.warehouseId}, 0, 0, 0, 1, now())
        ON CONFLICT (tenant_id, item_id, warehouse_id) DO NOTHING
      `);
      const [balance] = rowsOf<StockBalance>(await tx.execute(sql`
        SELECT tenant_id AS "tenantId", item_id AS "itemId", warehouse_id AS "warehouseId",
               quantity, value, average_cost AS "averageCost", version, updated_at AS "updatedAt"
        FROM stock_balances
        WHERE tenant_id = ${tenantId} AND item_id = ${line.itemId} AND warehouse_id = ${line.warehouseId}
        FOR UPDATE
      `));
      if (!balance) throw new DomainError('STOCK_BALANCE_LOCK_FAILED', 'Could not lock stock balance row', 500);

      const currentQty = new Decimal(balance.quantity);
      const currentValue = new Decimal(balance.value);
      const average = currentQty.gt(0) ? currentValue.div(currentQty) : new Decimal(line.unitCost ?? '0');
      const unitCost = line.direction === 'in' ? new Decimal(line.unitCost ?? '0') : average;
      const nextQty = line.direction === 'in' ? currentQty.plus(quantity) : currentQty.minus(quantity);
      if (nextQty.lt(0)) throw new DomainError('STOCK_INSUFFICIENT', 'Stock is insufficient for this movement', 422);
      const nextValue = line.direction === 'in' ? currentValue.plus(quantity.mul(unitCost)) : currentValue.minus(quantity.mul(average));
      const averageCost = nextQty.isZero() ? '0.0000' : nextValue.div(nextQty).toFixed(4);
      const id = newId();

      await tx.insert(inventoryTransactions).values({
        id,
        tenantId,
        itemId: line.itemId,
        warehouseId: line.warehouseId,
        occurredAt: new Date(),
        docType: line.docType,
        docId: line.docId,
        lineId: line.lineId,
        direction: line.direction,
        qty: line.qty,
        baseQty: line.qty,
        unitCost: unitCost.toFixed(4),
        totalCost: quantity.mul(unitCost).toFixed(4),
        costing: line.costing ?? (line.direction === 'in' ? 'inWithCost' : 'outAtAvg'),
        lotId: line.lotId,
        serialId: line.serialId,
      });
      await tx.update(stockBalances).set({
        quantity: nextQty.toFixed(4),
        value: nextValue.toFixed(4),
        averageCost,
        version: balance.version + 1,
        updatedAt: new Date(),
      }).where(and(eq(stockBalances.tenantId, tenantId), eq(stockBalances.itemId, line.itemId), eq(stockBalances.warehouseId, line.warehouseId)));

      if (line.serialId) {
        await tx.update(itemSerials).set({
          status: line.direction === 'out' ? 'sold' : 'available',
          warehouseId: line.direction === 'out' ? null : line.warehouseId,
          updatedAt: new Date(),
        }).where(and(eq(itemSerials.tenantId, tenantId), eq(itemSerials.id, line.serialId)));
      }
      created.push(id);
    }
    return { transactionIds: created };
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

  async transfer(tenantId: string, input: { transferId: string; fromWarehouseId: string; toWarehouseId: string; lines: Array<{ itemId: string; qty: string; unitCost?: string; lotId?: string; serialId?: string }> }) {
    if (!input.lines.length || input.fromWarehouseId === input.toWarehouseId) throw new DomainError('INVALID_STOCK_TRANSFER', 'A transfer requires distinct warehouses and at least one line', 422);
    const outbound = input.lines.map((line) => ({ ...line, warehouseId: input.fromWarehouseId, direction: 'out' as const, docType: 'stock_transfer', docId: input.transferId, costing: 'outAtAvg' as const }));
    const inbound = input.lines.map((line) => ({ ...line, warehouseId: input.toWarehouseId, direction: 'in' as const, docType: 'stock_transfer', docId: input.transferId, costing: 'inWithCost' as const }));
    return this.record(tenantId, [...outbound, ...inbound]);
  }

  async adjust(tenantId: string, input: { adjustmentId: string; itemId: string; warehouseId: string; countedQty: string; unitCost?: string; approved: boolean; journalEntryId?: string }) {
    if (!input.approved) throw new DomainError('ADJUSTMENT_APPROVAL_REQUIRED', 'Stock adjustments require approval before posting', 422);
    if (!input.journalEntryId) throw new DomainError('ADJUSTMENT_JOURNAL_REQUIRED', 'An approved adjustment must reference a journal entry', 422);
    const current = await this.levels(tenantId, input.warehouseId, input.itemId);
    const existing = current[0];
    const delta = new Decimal(input.countedQty).minus(existing?.quantity ?? '0');
    if (delta.isZero()) return { adjustmentId: input.adjustmentId, transactionIds: [] };
    return this.record(tenantId, [{ itemId: input.itemId, warehouseId: input.warehouseId, qty: delta.abs().toFixed(4), unitCost: input.unitCost ?? existing?.averageCost ?? '0', direction: delta.gt(0) ? 'in' : 'out', docType: 'stock_adjustment', docId: input.adjustmentId, costing: delta.gt(0) ? 'inWithCost' : 'outAtAvg' }]);
  }

  async createLot(tenantId: string, input: { itemId: string; lotNo: string; expiryDate?: string; receivedAt?: string }) {
    const [lot] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(itemLots).values({ id: newId(), tenantId, itemId: input.itemId, lotNo: input.lotNo, expiryDate: input.expiryDate, receivedAt: input.receivedAt ? new Date(input.receivedAt) : null }).returning());
    return lot;
  }

  listLots(tenantId: string, itemId?: string) { return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(itemLots).where(and(eq(itemLots.tenantId, tenantId), itemId ? eq(itemLots.itemId, itemId) : undefined)).orderBy(asc(itemLots.lotNo))); }

  async createSerial(tenantId: string, input: { itemId: string; serialNo: string; lotId?: string; warehouseId?: string; status?: string }) {
    const [serial] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(itemSerials).values({ id: newId(), tenantId, itemId: input.itemId, serialNo: input.serialNo, lotId: input.lotId, warehouseId: input.warehouseId, status: input.status ?? 'available' }).returning());
    return serial;
  }

  listSerials(tenantId: string, itemId?: string, status?: string) { return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(itemSerials).where(and(eq(itemSerials.tenantId, tenantId), itemId ? eq(itemSerials.itemId, itemId) : undefined, status ? eq(itemSerials.status, status) : undefined)).orderBy(asc(itemSerials.serialNo))); }

  async reserveSerials(tenantId: string, serialIds: string[]) {
    if (!serialIds.length) throw new DomainError('SERIALS_REQUIRED', 'At least one serial is required', 422);
    return this.transitionSerials(tenantId, serialIds, ['available'], 'reserved');
  }

  releaseSerials(tenantId: string, serialIds: string[]) { return this.transitionSerials(tenantId, serialIds, ['reserved'], 'available'); }
  consumeSerials(tenantId: string, serialIds: string[]) { return this.transitionSerials(tenantId, serialIds, ['available', 'reserved'], 'sold'); }
  returnSerials(tenantId: string, serialIds: string[]) { return this.transitionSerials(tenantId, serialIds, ['sold'], 'available'); }

  private async transitionSerials(tenantId: string, serialIds: string[], fromStatuses: string[], toStatus: string) {
    if (!serialIds.length) throw new DomainError('SERIALS_REQUIRED', 'At least one serial is required', 422);
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const rows = await tx.select().from(itemSerials).where(and(eq(itemSerials.tenantId, tenantId), inArray(itemSerials.id, serialIds)));
      if (rows.length !== serialIds.length || rows.some((row) => !fromStatuses.includes(row.status))) throw new DomainError('SERIAL_INVALID_STATE', 'One or more serials cannot transition to the requested state', 422);
      for (const serial of rows) await tx.update(itemSerials).set({ status: toStatus, updatedAt: new Date() }).where(and(eq(itemSerials.tenantId, tenantId), eq(itemSerials.id, serial.id), inArray(itemSerials.status, fromStatuses)));
      return { serialIds, status: toStatus };
    });
  }

  async createTransfer(tenantId: string, input: { id: string; number: string; fromWarehouseId: string; toWarehouseId: string; lines: Array<{ itemId: string; qty: string; unitCost?: string; lotId?: string; serialIds?: string[] }> }) {
    if (!input.lines.length || input.fromWarehouseId === input.toWarehouseId) throw new DomainError('INVALID_STOCK_TRANSFER', 'A transfer requires distinct warehouses and at least one line', 422);
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      await tx.insert(stockTransfers).values({ id: input.id, tenantId, number: input.number, fromWarehouseId: input.fromWarehouseId, toWarehouseId: input.toWarehouseId, status: 'draft' });
      await tx.insert(stockTransferLines).values(input.lines.map((line, index) => ({ transferId: input.id, tenantId, lineNo: index + 1, itemId: line.itemId, qty: line.qty, unitCost: line.unitCost ?? '0', lotId: line.lotId, serialIds: line.serialIds ?? [] })));
      return { id: input.id, status: 'draft' };
    });
  }

  async sendTransfer(tenantId: string, transferId: string) {
    const transfer = await withTenantTx(this.database.db, tenantId, async (tx) => (await tx.select().from(stockTransfers).where(and(eq(stockTransfers.tenantId, tenantId), eq(stockTransfers.id, transferId))))[0]);
    if (!transfer || transfer.status !== 'draft') throw new DomainError('TRANSFER_INVALID_STATE', 'Only draft transfers can be sent', 422);
    const lines = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(stockTransferLines).where(and(eq(stockTransferLines.tenantId, tenantId), eq(stockTransferLines.transferId, transferId))));
    const result = await this.record(tenantId, lines.map((line) => ({ itemId: line.itemId, warehouseId: transfer.fromWarehouseId, qty: line.qty, unitCost: line.unitCost, direction: 'out' as const, docType: 'stock_transfer', docId: transferId, lineId: newId(), lotId: line.lotId ?? undefined, costing: 'outAtAvg' as const })));
    await withTenantTx(this.database.db, tenantId, (tx) => tx.update(stockTransfers).set({ status: 'in_transit', sentAt: new Date(), updatedAt: new Date() }).where(and(eq(stockTransfers.tenantId, tenantId), eq(stockTransfers.id, transferId))));
    return { transferId, status: 'in_transit', transactionIds: result.transactionIds };
  }

  async receiveTransfer(tenantId: string, transferId: string, received: Array<{ lineNo: number; qty: string }>) {
    const transfer = await withTenantTx(this.database.db, tenantId, async (tx) => (await tx.select().from(stockTransfers).where(and(eq(stockTransfers.tenantId, tenantId), eq(stockTransfers.id, transferId))))[0]);
    if (!transfer || !['in_transit', 'partially_received'].includes(transfer.status)) throw new DomainError('TRANSFER_INVALID_STATE', 'Transfer is not awaiting receipt', 422);
    const lines = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(stockTransferLines).where(and(eq(stockTransferLines.tenantId, tenantId), eq(stockTransferLines.transferId, transferId))));
    const byLine = new Map(lines.map((line) => [line.lineNo, line]));
    const movements: InventoryLine[] = [];
    for (const input of received) { const line = byLine.get(input.lineNo); if (!line) throw new DomainError('TRANSFER_LINE_NOT_FOUND', 'Transfer line was not found', 404); const qty = new Decimal(input.qty); const already = new Decimal(line.receivedQty); const requested = new Decimal(line.qty); if (!qty.gt(0) || already.plus(qty).gt(requested)) throw new DomainError('TRANSFER_RECEIPT_INVALID', 'Received quantity exceeds transfer quantity', 422); movements.push({ itemId: line.itemId, warehouseId: transfer.toWarehouseId, qty: qty.toFixed(4), unitCost: line.unitCost, direction: 'in', docType: 'stock_transfer_receipt', docId: transferId, lineId: newId(), lotId: line.lotId ?? undefined, costing: 'inWithCost' }); }
    const result = await this.record(tenantId, movements);
    await withTenantTx(this.database.db, tenantId, async (tx) => { for (const input of received) { const line = byLine.get(input.lineNo); if (line) await tx.update(stockTransferLines).set({ receivedQty: sql`${stockTransferLines.receivedQty} + ${input.qty}` }).where(and(eq(stockTransferLines.tenantId, tenantId), eq(stockTransferLines.transferId, transferId), eq(stockTransferLines.lineNo, input.lineNo))); } const updated = await tx.select().from(stockTransferLines).where(and(eq(stockTransferLines.tenantId, tenantId), eq(stockTransferLines.transferId, transferId))); const complete = updated.every((line) => new Decimal(line.receivedQty).eq(new Decimal(line.qty))); await tx.update(stockTransfers).set({ status: complete ? 'received' : 'partially_received', receivedAt: complete ? new Date() : undefined, updatedAt: new Date() }).where(and(eq(stockTransfers.tenantId, tenantId), eq(stockTransfers.id, transferId))); });
    return { transferId, transactionIds: result.transactionIds };
  }

  async cancelTransfer(tenantId: string, transferId: string) {
    const result = await withTenantTx(this.database.db, tenantId, async (tx) => { const row = (await tx.select().from(stockTransfers).where(and(eq(stockTransfers.tenantId, tenantId), eq(stockTransfers.id, transferId))))[0]; if (!row || ['received', 'cancelled'].includes(row.status)) throw new DomainError('TRANSFER_INVALID_STATE', 'Transfer cannot be cancelled', 422); await tx.update(stockTransfers).set({ status: 'cancelled', cancelledAt: new Date(), updatedAt: new Date() }).where(and(eq(stockTransfers.tenantId, tenantId), eq(stockTransfers.id, transferId))); return { transferId, status: 'cancelled' }; });
    return result;
  }
}

function rowsOf<T>(result: unknown): T[] {
  return Array.isArray(result) ? (result as T[]) : ((result as { rows?: T[] }).rows ?? []);
}
