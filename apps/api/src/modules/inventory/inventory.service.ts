/* eslint-disable no-restricted-syntax */
import { Inject, Injectable } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { and, asc, desc, eq, inArray, isNull, sql } from 'drizzle-orm';
import { DomainError, newId } from '@erp/contracts';
import {
  inventoryTransactions,
  itemLots,
  itemSerials,
  itemUnits,
  items,
  stockBalances,
  stockTransfers,
  stockTransferLines,
  unitsOfMeasure,
  warehouses,
  withTenantTx,
  type DatabaseHandle,
  type DrizzleTx,
  type StockBalance,
} from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';
import { tryGetAuthContext } from '../platform/context/tenant-context.js';
import { SequencesService } from '../platform-services/index.js';

/**
 * One append-only stock movement expressed in the unit selected by the user. `qty` is
 * converted to `baseQty` before it reaches the stock pool; costs are stored per base unit
 * so moving average valuation never changes meaning when a carton is used instead of a
 * piece. `serialIds` is deliberately plural: a line for three serialized items produces
 * three immutable ledger rows rather than a JSON blob that cannot be audited.
 */
export type InventoryLine = {
  itemId: string;
  warehouseId: string;
  qty: string;
  unitId?: string;
  /** Cost per selected unit for incoming movements. Outgoing cost is always moving average. */
  unitCost?: string;
  direction: 'in' | 'out';
  docType: string;
  docId: string;
  /** Unique ledger line key. For serialised lines it is generated per serial. */
  lineId?: string;
  /** Source document line retained in movement metadata when a line fans out to serial rows. */
  sourceLineId?: string;
  costing?: 'inWithCost' | 'outAtAvg' | 'returnAtOriginalCost';
  lotId?: string;
  /** Legacy singular form; prefer `serialIds`. */
  serialId?: string;
  serialIds?: string[];
  metadata?: Record<string, unknown>;
  occurredAt?: Date;
  /** Internal reversal paths use this only after they have validated serial state. */
  skipSerialLifecycle?: boolean;
};

type ResolvedLine = {
  input: InventoryLine;
  item: {
    id: string;
    baseUnitId: string;
    kind: string;
    trackLot: boolean;
    trackSerial: boolean;
  };
  enteredQty: Decimal;
  baseQty: Decimal;
  unitId: string;
  ratio: Decimal;
  enteredUnitCost: Decimal;
  serialIds: string[];
};

type RecordedMovement = {
  id: string;
  direction: 'in' | 'out';
  itemId: string;
  warehouseId: string;
  lineId: string;
  sourceLineId?: string;
  serialId?: string;
  qty: string;
  baseQty: string;
  unitId: string;
  unitCost: string;
  totalCost: string;
};

const dec = (value: string | number | null | undefined) => new Decimal(value ?? '0');
const dateOnly = () => new Date().toISOString().slice(0, 10);

@Injectable()
export class InventoryService {
  constructor(
    @Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle,
    private readonly sequences: SequencesService,
  ) {}

  /** Transfer register, including its lines, for the real مناقلة screen. */
  async listTransfers(tenantId: string, status?: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const rows = await tx
        .select()
        .from(stockTransfers)
        .where(and(eq(stockTransfers.tenantId, tenantId), status ? eq(stockTransfers.status, status) : undefined))
        .orderBy(desc(stockTransfers.createdAt))
        .limit(200);
      if (rows.length === 0) return [];
      const lines = await tx
        .select()
        .from(stockTransferLines)
        .where(
          and(
            eq(stockTransferLines.tenantId, tenantId),
            inArray(
              stockTransferLines.transferId,
              rows.map((row) => row.id),
            ),
          ),
        );
      return rows.map((row) => ({
        ...row,
        lines: lines
          .filter((line) => line.transferId === row.id)
          .sort((left, right) => left.lineNo - right.lineNo),
      }));
    });
  }

  async getTransfer(tenantId: string, transferId: string) {
    return withTenantTx(this.database.db, tenantId, (tx) => this.loadTransfer(tx, tenantId, transferId));
  }

  async record(tenantId: string, lines: InventoryLine[]) {
    return withTenantTx(this.database.db, tenantId, (tx) => this.recordInTx(tx, tenantId, lines));
  }

  /**
   * The sole mutation boundary for stock. Every caller (sales, purchases, production,
   * transfers and the Phase-05 stock documents) uses this transaction-scoped version so
   * an invoice/journal can never commit without its corresponding inventory movement.
   */
  async recordInTx(tx: DrizzleTx, tenantId: string, lines: InventoryLine[]) {
    if (!lines.length) {
      throw new DomainError('INVENTORY_LINES_REQUIRED', 'At least one inventory line is required', 422);
    }

    const movements: RecordedMovement[] = [];
    for (const input of lines) {
      const line = await this.resolveLine(tx, tenantId, input);
      const serialCount = line.serialIds.length;
      if (serialCount > 0) {
        if (!line.baseQty.isInteger() || !line.baseQty.eq(serialCount)) {
          throw new DomainError(
            'INVENTORY_SERIAL_QTY_MISMATCH',
            'A serialized movement needs exactly one serial number per base unit',
            422,
          );
        }
        // A carton may contain multiple serialized base units. Splitting the entered
        // quantity preserves the original line total while keeping one serial per ledger
        // row. The serial id is also a stable, document-local idempotency key.
        const sliceQty = line.enteredQty.div(serialCount);
        for (const serialId of line.serialIds) {
          movements.push(
            await this.recordOneInTx(tx, tenantId, line, {
              qty: sliceQty,
              baseQty: new Decimal(1),
              lineId: serialId,
              sourceLineId: input.sourceLineId ?? input.lineId,
              serialId,
            }),
          );
        }
      } else {
        movements.push(
          await this.recordOneInTx(tx, tenantId, line, {
            qty: line.enteredQty,
            baseQty: line.baseQty,
            lineId: input.lineId ?? newId(),
            sourceLineId: input.sourceLineId ?? input.lineId,
          }),
        );
      }
    }
    return { transactionIds: movements.map((movement) => movement.id), movements };
  }

  private async resolveLine(tx: DrizzleTx, tenantId: string, input: InventoryLine): Promise<ResolvedLine> {
    const enteredQty = dec(input.qty);
    if (!enteredQty.isFinite() || enteredQty.lte(0)) {
      throw new DomainError('INVALID_STOCK_QUANTITY', 'Quantity must be positive', 422);
    }

    const [item] = await tx
      .select({
        id: items.id,
        baseUnitId: items.baseUnitId,
        kind: items.kind,
        trackLot: items.trackLot,
        trackSerial: items.trackSerial,
      })
      .from(items)
      .where(and(eq(items.tenantId, tenantId), eq(items.id, input.itemId), isNull(items.deletedAt)));
    if (!item) throw new DomainError('ITEM_NOT_FOUND', 'Inventory item was not found', 404);
    if (item.kind !== 'stock') {
      throw new DomainError('INVENTORY_ITEM_NOT_STOCKED', 'Only stock-kind items can move through the inventory ledger', 422);
    }

    const [warehouse] = await tx
      .select({ id: warehouses.id })
      .from(warehouses)
      .where(and(eq(warehouses.tenantId, tenantId), eq(warehouses.id, input.warehouseId), isNull(warehouses.deletedAt)));
    if (!warehouse) throw new DomainError('WAREHOUSE_NOT_FOUND', 'Warehouse was not found', 404);

    const unitId = input.unitId ?? item.baseUnitId;
    let ratio = new Decimal(1);
    if (unitId !== item.baseUnitId) {
      const [unit] = await tx
        .select({ ratio: itemUnits.ratio })
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
      if (!unit) {
        throw new DomainError(
          'INVENTORY_ITEM_UNIT_INVALID',
          'The selected unit is not configured for this item',
          422,
          { field: 'unitId' },
        );
      }
      ratio = dec(unit.ratio);
    }
    if (!ratio.isFinite() || ratio.lte(0)) {
      throw new DomainError('INVENTORY_ITEM_UNIT_INVALID', 'Item unit ratio must be greater than zero', 422);
    }

    const enteredUnitCost = dec(input.unitCost);
    if (!enteredUnitCost.isFinite() || enteredUnitCost.lt(0)) {
      throw new DomainError('INVENTORY_UNIT_COST_INVALID', 'Unit cost must be a non-negative number', 422);
    }

    const serialIds = [...new Set([...(input.serialIds ?? []), ...(input.serialId ? [input.serialId] : [])])];
    if ((input.serialIds?.length ?? 0) + (input.serialId ? 1 : 0) !== serialIds.length) {
      throw new DomainError('INVENTORY_SERIAL_DUPLICATE', 'A serial number can appear only once in one movement', 422);
    }
    if (item.trackLot && !input.lotId) {
      throw new DomainError('INVENTORY_LOT_REQUIRED', 'This item requires a batch/lot on every movement', 422);
    }
    if (!item.trackSerial && serialIds.length > 0) {
      throw new DomainError('INVENTORY_SERIAL_NOT_TRACKED', 'This item is not configured for serial tracking', 422);
    }
    if (item.trackSerial && serialIds.length === 0) {
      throw new DomainError('INVENTORY_SERIAL_REQUIRED', 'This item requires serial numbers on every movement', 422);
    }

    const baseQty = enteredQty.mul(ratio);
    if (input.lotId) await this.assertLotInTx(tx, tenantId, item.id, input.warehouseId, input.lotId, input.direction, baseQty);
    if (!input.skipSerialLifecycle) {
      for (const serialId of serialIds) {
        await this.assertSerialInTx(tx, tenantId, item.id, input.warehouseId, serialId, input.direction, input.docType, input.costing);
      }
    }

    return { input, item, enteredQty, baseQty, unitId, ratio, enteredUnitCost, serialIds };
  }

  private async assertLotInTx(
    tx: DrizzleTx,
    tenantId: string,
    itemId: string,
    warehouseId: string,
    lotId: string,
    direction: 'in' | 'out',
    baseQty: Decimal,
  ) {
    const [lot] = await tx
      .select()
      .from(itemLots)
      .where(and(eq(itemLots.tenantId, tenantId), eq(itemLots.id, lotId), isNull(itemLots.deletedAt)));
    if (!lot || lot.itemId !== itemId) {
      throw new DomainError('INVENTORY_LOT_INVALID', 'The selected lot does not belong to this item', 422, {
        field: 'lotId',
      });
    }
    if (direction === 'out' && lot.expiryDate && lot.expiryDate < dateOnly()) {
      throw new DomainError('INVENTORY_LOT_EXPIRED', 'Expired lots cannot be issued from stock', 422, {
        field: 'lotId',
      });
    }
    if (direction === 'out') {
      const available = await this.lotOnHandInTx(tx, tenantId, lotId, warehouseId);
      if (available.lt(baseQty)) {
        throw new DomainError('INVENTORY_LOT_INSUFFICIENT', 'The selected lot does not contain enough stock', 422, {
          field: 'lotId',
        });
      }
    }
  }

  private async assertSerialInTx(
    tx: DrizzleTx,
    tenantId: string,
    itemId: string,
    warehouseId: string,
    serialId: string,
    direction: 'in' | 'out',
    docType: string,
    costing?: InventoryLine['costing'],
  ) {
    const [serial] = await tx
      .select()
      .from(itemSerials)
      .where(and(eq(itemSerials.tenantId, tenantId), eq(itemSerials.id, serialId), isNull(itemSerials.deletedAt)));
    if (!serial || serial.itemId !== itemId) {
      throw new DomainError('INVENTORY_SERIAL_INVALID', 'The selected serial does not belong to this item', 422, {
        field: 'serialIds',
      });
    }

    if (direction === 'out') {
      if (!['available', 'reserved'].includes(serial.status) || serial.warehouseId !== warehouseId) {
        throw new DomainError('INVENTORY_SERIAL_UNAVAILABLE', 'The selected serial is not available in this warehouse', 422, {
          field: 'serialIds',
        });
      }
      return;
    }

    const returning = docType === 'sales_return' || docType === 'sales_void' || costing === 'returnAtOriginalCost';
    const receivingTransfer = docType === 'stock_transfer_receipt';
    const cancellingTransfer = docType === 'stock_transfer_cancel';
    const revertingReceipt = docType === 'purchase_void';
    const canReceive = receivingTransfer || cancellingTransfer
      ? serial.status === 'in_transit'
      : returning
        ? ['sold', 'issued'].includes(serial.status)
        : revertingReceipt
          ? serial.status === 'available' && serial.warehouseId === warehouseId
          : (serial.status === 'pending_receipt' || (serial.status === 'available' && !serial.warehouseId));
    if (!canReceive) {
      throw new DomainError('INVENTORY_SERIAL_INVALID_STATE', 'The serial cannot enter stock in its current state', 422, {
        field: 'serialIds',
      });
    }
  }

  private async recordOneInTx(
    tx: DrizzleTx,
    tenantId: string,
    line: ResolvedLine,
    part: { qty: Decimal; baseQty: Decimal; lineId: string; sourceLineId?: string; serialId?: string },
  ): Promise<RecordedMovement> {
    const input = line.input;
    await tx.execute(sql`
      INSERT INTO stock_balances (tenant_id, item_id, warehouse_id, quantity, value, average_cost, version, updated_at)
      VALUES (${tenantId}, ${input.itemId}, ${input.warehouseId}, 0, 0, 0, 1, now())
      ON CONFLICT (tenant_id, item_id, warehouse_id) DO NOTHING
    `);
    const [balance] = rowsOf<StockBalance>(
      await tx.execute(sql`
        SELECT tenant_id AS "tenantId", item_id AS "itemId", warehouse_id AS "warehouseId",
               quantity, value, average_cost AS "averageCost", version, updated_at AS "updatedAt"
        FROM stock_balances
        WHERE tenant_id = ${tenantId} AND item_id = ${input.itemId} AND warehouse_id = ${input.warehouseId}
        FOR UPDATE
      `),
    );
    if (!balance) throw new DomainError('STOCK_BALANCE_LOCK_FAILED', 'Could not lock stock balance row', 500);

    const currentQty = dec(balance.quantity);
    const currentValue = dec(balance.value);
    const suppliedBaseCost = line.enteredUnitCost.div(line.ratio);
    const average = currentQty.gt(0) ? currentValue.div(currentQty) : suppliedBaseCost;
    // Incoming unit costs belong to the selected unit; outgoing movements always carry
    // the pool average. This is what makes a transfer/sale value-neutral at the source.
    const baseUnitCost = input.direction === 'in' ? suppliedBaseCost : average;
    const nextQty = input.direction === 'in' ? currentQty.plus(part.baseQty) : currentQty.minus(part.baseQty);
    if (nextQty.lt(0)) {
      throw new DomainError('STOCK_INSUFFICIENT', 'Stock is insufficient for this movement', 422);
    }
    const movementValue = part.baseQty.mul(baseUnitCost);
    const nextValue = input.direction === 'in' ? currentValue.plus(movementValue) : currentValue.minus(movementValue);
    const averageCost = nextQty.isZero() ? '0.0000' : nextValue.div(nextQty).toFixed(4);
    const id = newId();

    if (part.serialId && !input.skipSerialLifecycle) {
      await this.transitionSerialForMovementInTx(
        tx,
        tenantId,
        part.serialId,
        input.warehouseId,
        input.direction,
        input.docType,
        input.costing,
      );
    }

    await tx.insert(inventoryTransactions).values({
      id,
      tenantId,
      itemId: input.itemId,
      warehouseId: input.warehouseId,
      occurredAt: input.occurredAt ?? new Date(),
      docType: input.docType,
      docId: input.docId,
      lineId: part.lineId,
      direction: input.direction,
      qty: part.qty.toFixed(4),
      baseQty: part.baseQty.toFixed(4),
      unitId: line.unitId,
      unitCost: baseUnitCost.toFixed(4),
      totalCost: movementValue.toFixed(4),
      costing: input.costing ?? (input.direction === 'in' ? 'inWithCost' : 'outAtAvg'),
      lotId: input.lotId,
      serialId: part.serialId,
      metadata: {
        ...(input.metadata ?? {}),
        ...(part.sourceLineId ? { sourceLineId: part.sourceLineId } : {}),
      },
      createdBy: tryGetAuthContext()?.userId,
    });
    await tx
      .update(stockBalances)
      .set({
        quantity: nextQty.toFixed(4),
        value: nextValue.toFixed(4),
        averageCost,
        version: balance.version + 1,
        updatedAt: new Date(),
      })
      .where(
        and(
          eq(stockBalances.tenantId, tenantId),
          eq(stockBalances.itemId, input.itemId),
          eq(stockBalances.warehouseId, input.warehouseId),
        ),
      );

    return {
      id,
      direction: input.direction,
      itemId: input.itemId,
      warehouseId: input.warehouseId,
      lineId: part.lineId,
      sourceLineId: part.sourceLineId,
      serialId: part.serialId,
      qty: part.qty.toFixed(4),
      baseQty: part.baseQty.toFixed(4),
      unitId: line.unitId,
      unitCost: baseUnitCost.toFixed(4),
      totalCost: movementValue.toFixed(4),
    };
  }

  private async transitionSerialForMovementInTx(
    tx: DrizzleTx,
    tenantId: string,
    serialId: string,
    warehouseId: string,
    direction: 'in' | 'out',
    docType: string,
    costing?: InventoryLine['costing'],
  ) {
    let status: string;
    let nextWarehouseId: string | null;
    if (direction === 'out') {
      status = docType === 'stock_transfer' ? 'in_transit' : docType.startsWith('sales') ? 'sold' : 'issued';
      nextWarehouseId = null;
    } else if (docType === 'purchase_void') {
      status = 'pending_receipt';
      nextWarehouseId = null;
    } else {
      // A return, transfer receipt and a normal receipt all leave the serial available
      // in the target warehouse after their guard above has checked its previous state.
      void costing;
      status = 'available';
      nextWarehouseId = warehouseId;
    }
    await tx
      .update(itemSerials)
      .set({ status, warehouseId: nextWarehouseId, updatedAt: new Date(), updatedBy: tryGetAuthContext()?.userId })
      .where(and(eq(itemSerials.tenantId, tenantId), eq(itemSerials.id, serialId)));
  }

  levels(tenantId: string, warehouseId?: string, itemId?: string) {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(stockBalances)
        .where(
          and(
            eq(stockBalances.tenantId, tenantId),
            warehouseId ? eq(stockBalances.warehouseId, warehouseId) : undefined,
            itemId ? eq(stockBalances.itemId, itemId) : undefined,
          ),
        )
        .orderBy(asc(stockBalances.itemId)),
    );
  }

  /** Items at/below their configured reorder point, calculated from the ledger balance. */
  async reorderReport(tenantId: string, warehouseId?: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const stockItems = await tx
        .select({
          id: items.id,
          sku: items.sku,
          nameAr: items.nameAr,
          nameEn: items.nameEn,
          minQty: items.minQty,
          maxQty: items.maxQty,
          baseUnitId: items.baseUnitId,
        })
        .from(items)
        .where(and(eq(items.tenantId, tenantId), eq(items.kind, 'stock'), isNull(items.deletedAt)));
      const balances = await tx
        .select({ itemId: stockBalances.itemId, quantity: stockBalances.quantity })
        .from(stockBalances)
        .where(
          and(
            eq(stockBalances.tenantId, tenantId),
            warehouseId ? eq(stockBalances.warehouseId, warehouseId) : undefined,
          ),
        );
      const quantities = new Map<string, Decimal>();
      for (const balance of balances) {
        quantities.set(balance.itemId, (quantities.get(balance.itemId) ?? new Decimal(0)).plus(balance.quantity));
      }
      return stockItems
        .map((item) => {
          const onHand = quantities.get(item.id) ?? new Decimal(0);
          const minQty = dec(item.minQty);
          const maxQty = item.maxQty ? dec(item.maxQty) : undefined;
          return {
            ...item,
            onHand: onHand.toFixed(4),
            belowMin: onHand.lte(minQty),
            suggestedQty: Decimal.max(maxQty && maxQty.gt(onHand) ? maxQty.minus(onHand) : minQty.minus(onHand), 0).toFixed(4),
          };
        })
        .filter((item) => item.belowMin)
        .sort((left, right) => dec(right.suggestedQty).comparedTo(dec(left.suggestedQty)));
    });
  }

  movements(tenantId: string, itemId?: string, warehouseId?: string) {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(inventoryTransactions)
        .where(
          and(
            eq(inventoryTransactions.tenantId, tenantId),
            itemId ? eq(inventoryTransactions.itemId, itemId) : undefined,
            warehouseId ? eq(inventoryTransactions.warehouseId, warehouseId) : undefined,
          ),
        )
        .orderBy(asc(inventoryTransactions.occurredAt)),
    );
  }

  async valuationAsOf(tenantId: string, asOf: Date, warehouseId?: string, itemId?: string) {
    if (Number.isNaN(asOf.getTime())) {
      throw new DomainError('INVENTORY_AS_OF_INVALID', 'as_of must be a valid ISO date', 422);
    }
    const rows = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(inventoryTransactions)
        .where(
          and(
            eq(inventoryTransactions.tenantId, tenantId),
            sql`${inventoryTransactions.occurredAt} <= ${asOf}`,
            warehouseId ? eq(inventoryTransactions.warehouseId, warehouseId) : undefined,
            itemId ? eq(inventoryTransactions.itemId, itemId) : undefined,
          ),
        )
        .orderBy(asc(inventoryTransactions.occurredAt), asc(inventoryTransactions.createdAt)),
    );
    const totals = new Map<string, { quantity: Decimal; value: Decimal }>();
    for (const row of rows) {
      const key = `${row.itemId}:${row.warehouseId}`;
      const current = totals.get(key) ?? { quantity: new Decimal(0), value: new Decimal(0) };
      const quantity = dec(row.baseQty);
      const value = dec(row.totalCost);
      if (row.direction === 'in') {
        current.quantity = current.quantity.plus(quantity);
        current.value = current.value.plus(value);
      } else {
        current.quantity = current.quantity.minus(quantity);
        current.value = current.value.minus(value);
      }
      totals.set(key, current);
    }
    return [...totals].map(([key, total]) => {
      const [resultItemId, resultWarehouseId] = key.split(':');
      if (!resultItemId || !resultWarehouseId) {
        throw new DomainError('INVENTORY_REPLAY_INVALID_KEY', 'Inventory replay produced an invalid balance key', 500);
      }
      return {
        itemId: resultItemId,
        warehouseId: resultWarehouseId,
        quantity: total.quantity.toFixed(4),
        value: total.value.toFixed(4),
        averageCost: total.quantity.isZero() ? '0.0000' : total.value.div(total.quantity).toFixed(4),
      };
    });
  }

  async recomputeBalances(tenantId: string, warehouseId?: string, itemId?: string) {
    const valuation = await this.valuationAsOf(tenantId, new Date('9999-12-31T23:59:59.999Z'), warehouseId, itemId);
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      for (const row of valuation) {
        await tx
          .insert(stockBalances)
          .values({
            tenantId,
            itemId: row.itemId,
            warehouseId: row.warehouseId,
            quantity: row.quantity,
            value: row.value,
            averageCost: row.averageCost,
            version: 1,
            updatedAt: new Date(),
          })
          .onConflictDoUpdate({
            target: [stockBalances.tenantId, stockBalances.itemId, stockBalances.warehouseId],
            set: {
              quantity: row.quantity,
              value: row.value,
              averageCost: row.averageCost,
              version: sql`${stockBalances.version} + 1`,
              updatedAt: new Date(),
            },
          });
      }
      return { recomputed: valuation.length };
    });
  }

  /**
   * Legacy immediate transfer endpoint. New UI uses draft → send → receive below, but
   * this remains compatible and now preserves the actual source moving-average value.
   */
  async transfer(
    tenantId: string,
    input: {
      transferId: string;
      fromWarehouseId: string;
      toWarehouseId: string;
      lines: Array<{ itemId: string; qty: string; unitId?: string; unitCost?: string; lotId?: string; serialId?: string; serialIds?: string[] }>;
    },
  ) {
    if (!input.lines.length || input.fromWarehouseId === input.toWarehouseId) {
      throw new DomainError('INVALID_STOCK_TRANSFER', 'A transfer requires distinct warehouses and at least one line', 422);
    }
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const outbound = await this.recordInTx(
        tx,
        tenantId,
        input.lines.map((line) => ({
          ...line,
          warehouseId: input.fromWarehouseId,
          direction: 'out' as const,
          docType: 'stock_transfer',
          docId: input.transferId,
          costing: 'outAtAvg' as const,
        })),
      );
      const inbound = await this.recordInTx(
        tx,
        tenantId,
        outbound.movements.map((movement) => ({
          itemId: movement.itemId,
          warehouseId: input.toWarehouseId,
          // Use base units on the inbound leg. The source cost is then copied exactly,
          // regardless of the unit in which the user typed the transfer.
          qty: movement.baseQty,
          unitCost: movement.unitCost,
          direction: 'in' as const,
          docType: 'stock_transfer_receipt',
          docId: input.transferId,
          lineId: movement.lineId,
          serialId: movement.serialId,
          costing: 'inWithCost' as const,
        })),
      );
      return { transactionIds: [...outbound.transactionIds, ...inbound.transactionIds] };
    });
  }

  async adjust(
    tenantId: string,
    input: {
      adjustmentId: string;
      itemId: string;
      warehouseId: string;
      countedQty: string;
      unitCost?: string;
      approved: boolean;
      journalEntryId?: string;
      unitId?: string;
      lotId?: string;
      serialIds?: string[];
    },
  ) {
    if (!input.approved) {
      throw new DomainError('ADJUSTMENT_APPROVAL_REQUIRED', 'Stock adjustments require approval before posting', 422);
    }
    if (!input.journalEntryId) {
      throw new DomainError('ADJUSTMENT_JOURNAL_REQUIRED', 'An approved adjustment must reference a journal entry', 422);
    }
    const current = await this.levels(tenantId, input.warehouseId, input.itemId);
    const existing = current[0];
    const delta = dec(input.countedQty).minus(existing?.quantity ?? '0');
    if (delta.isZero()) return { adjustmentId: input.adjustmentId, transactionIds: [] };
    return this.record(tenantId, [
      {
        itemId: input.itemId,
        warehouseId: input.warehouseId,
        qty: delta.abs().toFixed(4),
        unitId: input.unitId,
        unitCost: input.unitCost ?? existing?.averageCost ?? '0',
        direction: delta.gt(0) ? 'in' : 'out',
        docType: 'stock_adjustment',
        docId: input.adjustmentId,
        lotId: input.lotId,
        serialIds: input.serialIds,
        costing: delta.gt(0) ? 'inWithCost' : 'outAtAvg',
      },
    ]);
  }

  async createLot(
    tenantId: string,
    input: { itemId: string; lotNo: string; expiryDate?: string; receivedAt?: string },
  ) {
    if (!input.lotNo?.trim()) throw new DomainError('INVENTORY_LOT_NO_REQUIRED', 'Lot number is required', 422);
    if (input.expiryDate && Number.isNaN(new Date(`${input.expiryDate}T00:00:00Z`).getTime())) {
      throw new DomainError('INVENTORY_LOT_EXPIRY_INVALID', 'Expiry date is invalid', 422);
    }
    const [lot] = await withTenantTx(this.database.db, tenantId, async (tx) => {
      await this.requireStockItemInTx(tx, tenantId, input.itemId);
      return tx
        .insert(itemLots)
        .values({
          id: newId(),
          tenantId,
          itemId: input.itemId,
          lotNo: input.lotNo.trim(),
          expiryDate: input.expiryDate,
          receivedAt: input.receivedAt ? new Date(input.receivedAt) : null,
          createdBy: tryGetAuthContext()?.userId,
        })
        .returning();
    });
    return lot;
  }

  async listLots(tenantId: string, itemId?: string, warehouseId?: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const lots = await tx
        .select()
        .from(itemLots)
        .where(
          and(
            eq(itemLots.tenantId, tenantId),
            isNull(itemLots.deletedAt),
            itemId ? eq(itemLots.itemId, itemId) : undefined,
          ),
        )
        .orderBy(asc(itemLots.expiryDate), asc(itemLots.lotNo));
      const result = [];
      for (const lot of lots) {
        const onHand = await this.lotOnHandInTx(tx, tenantId, lot.id, warehouseId);
        result.push({
          ...lot,
          onHand: onHand.toFixed(4),
          expired: Boolean(lot.expiryDate && lot.expiryDate < dateOnly()),
        });
      }
      return result;
    });
  }

  async expiringLots(tenantId: string, input: { warehouseId?: string; days?: number } = {}) {
    const days = Math.max(0, Math.min(3650, input.days ?? 30));
    const cutoff = new Date();
    cutoff.setUTCDate(cutoff.getUTCDate() + days);
    const cutoffText = cutoff.toISOString().slice(0, 10);
    const rows = await this.listLots(tenantId, undefined, input.warehouseId);
    return rows.filter((row) => row.expiryDate && row.expiryDate <= cutoffText && dec(row.onHand).gt(0));
  }

  private async lotOnHandInTx(tx: DrizzleTx, tenantId: string, lotId: string, warehouseId?: string) {
    const rows = await tx
      .select({ direction: inventoryTransactions.direction, baseQty: inventoryTransactions.baseQty })
      .from(inventoryTransactions)
      .where(
        and(
          eq(inventoryTransactions.tenantId, tenantId),
          eq(inventoryTransactions.lotId, lotId),
          warehouseId ? eq(inventoryTransactions.warehouseId, warehouseId) : undefined,
        ),
      );
    return rows.reduce(
      (sum, row) => sum.plus(row.direction === 'in' ? dec(row.baseQty) : dec(row.baseQty).negated()),
      new Decimal(0),
    );
  }

  async createSerial(
    tenantId: string,
    input: { itemId: string; serialNo: string; lotId?: string; warehouseId?: string; status?: string },
  ) {
    if (!input.serialNo?.trim()) throw new DomainError('INVENTORY_SERIAL_NO_REQUIRED', 'Serial number is required', 422);
    const requestedStatus = input.status ?? 'pending_receipt';
    if (!['pending_receipt', 'available', 'reserved', 'sold', 'issued', 'scrapped', 'in_transit'].includes(requestedStatus)) {
      throw new DomainError('INVENTORY_SERIAL_STATUS_INVALID', 'Serial status is invalid', 422);
    }
    const [serial] = await withTenantTx(this.database.db, tenantId, async (tx) => {
      const item = await this.requireStockItemInTx(tx, tenantId, input.itemId);
      if (!item.trackSerial) {
        throw new DomainError('INVENTORY_SERIAL_NOT_TRACKED', 'Enable serial tracking on the item before registering serials', 422);
      }
      if (input.lotId) {
        const [lot] = await tx
          .select({ id: itemLots.id, itemId: itemLots.itemId })
          .from(itemLots)
          .where(and(eq(itemLots.tenantId, tenantId), eq(itemLots.id, input.lotId), isNull(itemLots.deletedAt)));
        if (!lot || lot.itemId !== input.itemId) {
          throw new DomainError('INVENTORY_LOT_INVALID', 'The selected lot does not belong to this item', 422);
        }
      }
      if (input.warehouseId) {
        const [warehouse] = await tx
          .select({ id: warehouses.id })
          .from(warehouses)
          .where(and(eq(warehouses.tenantId, tenantId), eq(warehouses.id, input.warehouseId), isNull(warehouses.deletedAt)));
        if (!warehouse) throw new DomainError('WAREHOUSE_NOT_FOUND', 'Warehouse was not found', 404);
      }
      // Registering a serial is not a stock receipt. New serials deliberately stay
      // pending until a receipt/opening document puts their unit into the ledger.
      if (requestedStatus !== 'pending_receipt' && !input.warehouseId) {
        throw new DomainError('INVENTORY_SERIAL_WAREHOUSE_REQUIRED', 'A non-pending serial needs a warehouse', 422);
      }
      return tx
        .insert(itemSerials)
        .values({
          id: newId(),
          tenantId,
          itemId: input.itemId,
          serialNo: input.serialNo.trim(),
          lotId: input.lotId,
          warehouseId: input.warehouseId,
          status: requestedStatus,
          createdBy: tryGetAuthContext()?.userId,
        })
        .returning();
    });
    return serial;
  }

  listSerials(tenantId: string, itemId?: string, status?: string, warehouseId?: string) {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(itemSerials)
        .where(
          and(
            eq(itemSerials.tenantId, tenantId),
            isNull(itemSerials.deletedAt),
            itemId ? eq(itemSerials.itemId, itemId) : undefined,
            status ? eq(itemSerials.status, status) : undefined,
            warehouseId ? eq(itemSerials.warehouseId, warehouseId) : undefined,
          ),
        )
        .orderBy(asc(itemSerials.serialNo)),
    );
  }

  async reserveSerials(tenantId: string, serialIds: string[]) {
    return this.transitionSerials(tenantId, serialIds, ['available'], 'reserved');
  }

  releaseSerials(tenantId: string, serialIds: string[]) {
    return this.transitionSerials(tenantId, serialIds, ['reserved'], 'available');
  }

  consumeSerials(tenantId: string, serialIds: string[]) {
    return this.transitionSerials(tenantId, serialIds, ['available', 'reserved'], 'sold');
  }

  returnSerials(tenantId: string, serialIds: string[]) {
    return this.transitionSerials(tenantId, serialIds, ['sold', 'issued'], 'available');
  }

  scrapSerials(tenantId: string, serialIds: string[]) {
    return this.transitionSerials(tenantId, serialIds, ['available', 'reserved', 'issued', 'sold'], 'scrapped');
  }

  private async transitionSerials(tenantId: string, serialIds: string[], fromStatuses: string[], toStatus: string) {
    if (!serialIds.length) throw new DomainError('SERIALS_REQUIRED', 'At least one serial is required', 422);
    if (new Set(serialIds).size !== serialIds.length) {
      throw new DomainError('INVENTORY_SERIAL_DUPLICATE', 'A serial can appear only once', 422);
    }
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const rows = await tx
        .select()
        .from(itemSerials)
        .where(and(eq(itemSerials.tenantId, tenantId), inArray(itemSerials.id, serialIds), isNull(itemSerials.deletedAt)));
      if (rows.length !== serialIds.length || rows.some((row) => !fromStatuses.includes(row.status))) {
        throw new DomainError('SERIAL_INVALID_STATE', 'One or more serials cannot transition to the requested state', 422);
      }
      for (const serial of rows) {
        await tx
          .update(itemSerials)
          .set({
            status: toStatus,
            warehouseId: toStatus === 'available' ? serial.warehouseId : toStatus === 'reserved' ? serial.warehouseId : null,
            updatedAt: new Date(),
            updatedBy: tryGetAuthContext()?.userId,
          })
          .where(and(eq(itemSerials.tenantId, tenantId), eq(itemSerials.id, serial.id)));
      }
      return { serialIds, status: toStatus };
    });
  }

  /**
   * `number` is optional: the server, never the browser, allocates the transfer series.
   */
  async createTransfer(
    tenantId: string,
    input: {
      id?: string;
      number?: string;
      fromWarehouseId: string;
      toWarehouseId: string;
      lines: Array<{ itemId: string; qty: string; unitId?: string; unitCost?: string; lotId?: string; serialIds?: string[] }>;
    },
  ) {
    if (!input.lines.length || input.fromWarehouseId === input.toWarehouseId) {
      throw new DomainError('INVALID_STOCK_TRANSFER', 'A transfer requires distinct warehouses and at least one line', 422);
    }
    if (new Set(input.lines.map((line) => line.itemId)).size !== input.lines.length) {
      throw new DomainError('TRANSFER_DUPLICATE_ITEM', 'Combine duplicate item lines before creating a transfer', 422);
    }
    const id = input.id ?? newId();
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      await this.assertWarehouseInTx(tx, tenantId, input.fromWarehouseId);
      await this.assertWarehouseInTx(tx, tenantId, input.toWarehouseId);
      const values = [];
      for (const [index, line] of input.lines.entries()) {
        const item = await this.requireStockItemInTx(tx, tenantId, line.itemId);
        const qty = dec(line.qty);
        if (!qty.isFinite() || qty.lte(0)) {
          throw new DomainError('INVALID_STOCK_QUANTITY', 'Quantity must be positive', 422);
        }
        if (line.unitId && line.unitId !== item.baseUnitId) {
          const [unit] = await tx
            .select({ ratio: itemUnits.ratio })
            .from(itemUnits)
            .innerJoin(unitsOfMeasure, eq(unitsOfMeasure.id, itemUnits.unitId))
            .where(
              and(
                eq(itemUnits.itemId, item.id),
                eq(itemUnits.unitId, line.unitId),
                eq(unitsOfMeasure.tenantId, tenantId),
                isNull(unitsOfMeasure.deletedAt),
              ),
            );
          if (!unit) {
            throw new DomainError('INVENTORY_ITEM_UNIT_INVALID', 'The selected unit is not configured for this item', 422, {
              field: 'unitId',
            });
          }
        }
        // A draft is a pick list, not a reservation: the source may run out before the
        // store sends it. Availability, lot balance and serial state are intentionally
        // checked inside `sendTransfer`, where the ledger move is atomic.
        values.push({
          transferId: id,
          tenantId,
          lineNo: index + 1,
          itemId: line.itemId,
          unitId: line.unitId,
          qty: qty.toFixed(4),
          unitCost: line.unitCost ?? '0',
          lotId: line.lotId,
          serialIds: line.serialIds ?? [],
        });
      }
      const number =
        input.number ??
        (await this.sequences.next({ tenantId, docType: 'stock_transfer' }, tx, { prefix: 'TR-', padding: 6 })).display;
      await tx.insert(stockTransfers).values({
        id,
        tenantId,
        number,
        fromWarehouseId: input.fromWarehouseId,
        toWarehouseId: input.toWarehouseId,
        status: 'draft',
        createdBy: tryGetAuthContext()?.userId,
      });
      await tx.insert(stockTransferLines).values(values);
      return { id, number, status: 'draft' };
    });
  }

  async sendTransfer(tenantId: string, transferId: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const transfer = await this.loadTransfer(tx, tenantId, transferId);
      if (transfer.status !== 'draft') {
        throw new DomainError('TRANSFER_INVALID_STATE', 'Only a draft transfer can be sent', 422);
      }
      const outbound = await this.recordInTx(
        tx,
        tenantId,
        transfer.lines.map((line) => ({
          itemId: line.itemId,
          warehouseId: transfer.fromWarehouseId,
          qty: line.qty,
          unitId: line.unitId ?? undefined,
          direction: 'out' as const,
          docType: 'stock_transfer',
          docId: transferId,
          lineId: line.serialIds.length ? undefined : newId(),
          lotId: line.lotId ?? undefined,
          serialIds: line.serialIds,
          costing: 'outAtAvg' as const,
          metadata: { transferLineNo: line.lineNo },
        })),
      );
      for (const line of transfer.lines) {
        const moved = outbound.movements.filter((movement) => movement.itemId === line.itemId);
        const total = moved.reduce((sum, movement) => sum.plus(movement.totalCost), new Decimal(0));
        const quantity = moved.reduce((sum, movement) => sum.plus(movement.qty), new Decimal(0));
        await tx
          .update(stockTransferLines)
          .set({
            sentQty: line.qty,
            unitCost: quantity.isZero() ? '0.0000' : total.div(quantity).toFixed(4),
          })
          .where(
            and(
              eq(stockTransferLines.tenantId, tenantId),
              eq(stockTransferLines.transferId, transferId),
              eq(stockTransferLines.lineNo, line.lineNo),
            ),
          );
      }
      await tx
        .update(stockTransfers)
        .set({ status: 'in_transit', sentAt: new Date(), updatedAt: new Date(), updatedBy: tryGetAuthContext()?.userId })
        .where(and(eq(stockTransfers.tenantId, tenantId), eq(stockTransfers.id, transferId), eq(stockTransfers.status, 'draft')));
      return { transferId, status: 'in_transit', transactionIds: outbound.transactionIds };
    });
  }

  async receiveTransfer(
    tenantId: string,
    transferId: string,
    received: Array<{ lineNo: number; qty: string; serialIds?: string[] }>,
  ) {
    if (!received.length) {
      throw new DomainError('TRANSFER_RECEIPT_REQUIRED', 'Choose at least one transfer line to receive', 422);
    }
    if (new Set(received.map((line) => line.lineNo)).size !== received.length) {
      throw new DomainError('TRANSFER_RECEIPT_DUPLICATE_LINE', 'Each transfer line can be received only once per action', 422);
    }
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const transfer = await this.loadTransfer(tx, tenantId, transferId);
      if (!['in_transit', 'partially_received'].includes(transfer.status)) {
        throw new DomainError('TRANSFER_INVALID_STATE', 'Transfer is not awaiting receipt', 422);
      }
      const byLine = new Map(transfer.lines.map((line) => [line.lineNo, line]));
      const receivedSerialRows = await tx
        .select({ serialId: inventoryTransactions.serialId })
        .from(inventoryTransactions)
        .where(
          and(
            eq(inventoryTransactions.tenantId, tenantId),
            eq(inventoryTransactions.docId, transferId),
            eq(inventoryTransactions.docType, 'stock_transfer_receipt'),
          ),
        );
      const alreadyReceivedSerials = new Set(
        receivedSerialRows.map((row) => row.serialId).filter((value): value is string => Boolean(value)),
      );
      const movements: InventoryLine[] = [];
      for (const input of received) {
        const line = byLine.get(input.lineNo);
        if (!line) throw new DomainError('TRANSFER_LINE_NOT_FOUND', 'Transfer line was not found', 404);
        const qty = dec(input.qty);
        const remaining = dec(line.sentQty || line.qty).minus(line.receivedQty);
        if (!qty.isFinite() || qty.lte(0) || qty.gt(remaining)) {
          throw new DomainError('TRANSFER_RECEIPT_INVALID', 'Received quantity exceeds transfer quantity', 422);
        }
        const sentSerials = line.serialIds ?? [];
        let serialIds = input.serialIds ?? [];
        if (sentSerials.length) {
          const unreceived = sentSerials.filter((serialId) => !alreadyReceivedSerials.has(serialId));
          if (!serialIds.length && qty.eq(remaining)) serialIds = unreceived;
          if (!serialIds.length || serialIds.some((serialId) => !unreceived.includes(serialId))) {
            throw new DomainError('TRANSFER_RECEIPT_SERIAL_REQUIRED', 'Choose the serials received on this partial transfer', 422);
          }
        }
        movements.push({
          itemId: line.itemId,
          warehouseId: transfer.toWarehouseId,
          qty: qty.toFixed(4),
          unitId: line.unitId ?? undefined,
          unitCost: line.unitCost,
          direction: 'in',
          docType: 'stock_transfer_receipt',
          docId: transferId,
          lineId: serialIds.length ? undefined : newId(),
          lotId: line.lotId ?? undefined,
          serialIds,
          costing: 'inWithCost',
          metadata: { transferLineNo: line.lineNo },
        });
      }
      const result = await this.recordInTx(tx, tenantId, movements);
      for (const input of received) {
        await tx
          .update(stockTransferLines)
          .set({ receivedQty: sql`${stockTransferLines.receivedQty} + ${input.qty}` })
          .where(
            and(
              eq(stockTransferLines.tenantId, tenantId),
              eq(stockTransferLines.transferId, transferId),
              eq(stockTransferLines.lineNo, input.lineNo),
            ),
          );
      }
      const updated = await tx
        .select()
        .from(stockTransferLines)
        .where(and(eq(stockTransferLines.tenantId, tenantId), eq(stockTransferLines.transferId, transferId)));
      const complete = updated.every((line) => dec(line.receivedQty).eq(dec(line.sentQty || line.qty)));
      await tx
        .update(stockTransfers)
        .set({
          status: complete ? 'received' : 'partially_received',
          receivedAt: complete ? new Date() : null,
          updatedAt: new Date(),
          updatedBy: tryGetAuthContext()?.userId,
        })
        .where(and(eq(stockTransfers.tenantId, tenantId), eq(stockTransfers.id, transferId)));
      return { transferId, status: complete ? 'received' : 'partially_received', transactionIds: result.transactionIds };
    });
  }

  /**
   * Cancellation is safe only before the destination has received anything. Once a branch
   * starts issuing received goods, a cancellation must become a new return transfer rather
   * than silently rewriting its history.
   */
  async cancelTransfer(tenantId: string, transferId: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const transfer = await this.loadTransfer(tx, tenantId, transferId);
      if (['received', 'cancelled'].includes(transfer.status)) {
        throw new DomainError('TRANSFER_INVALID_STATE', 'Transfer cannot be cancelled', 422);
      }
      if (transfer.status === 'partially_received') {
        throw new DomainError(
          'TRANSFER_CANCEL_PARTIALLY_RECEIVED',
          'Create a reverse transfer for a partially received transfer',
          409,
        );
      }
      if (transfer.status === 'in_transit') {
        const sent = await tx
          .select()
          .from(inventoryTransactions)
          .where(
            and(
              eq(inventoryTransactions.tenantId, tenantId),
              eq(inventoryTransactions.docId, transferId),
              eq(inventoryTransactions.docType, 'stock_transfer'),
            ),
          );
        if (sent.length) {
          await this.recordInTx(
            tx,
            tenantId,
            sent.map((movement) => ({
              itemId: movement.itemId,
              warehouseId: movement.warehouseId,
              qty: movement.baseQty,
              unitCost: movement.unitCost,
              direction: 'in' as const,
              docType: 'stock_transfer_cancel',
              docId: transferId,
              lineId: newId(),
              lotId: movement.lotId ?? undefined,
              serialId: movement.serialId ?? undefined,
              costing: 'returnAtOriginalCost' as const,
            })),
          );
        }
      }
      await tx
        .update(stockTransfers)
        .set({ status: 'cancelled', cancelledAt: new Date(), updatedAt: new Date(), updatedBy: tryGetAuthContext()?.userId })
        .where(and(eq(stockTransfers.tenantId, tenantId), eq(stockTransfers.id, transferId)));
      return { transferId, status: 'cancelled' };
    });
  }

  private async loadTransfer(tx: DrizzleTx, tenantId: string, transferId: string) {
    const [transfer] = await tx
      .select()
      .from(stockTransfers)
      .where(and(eq(stockTransfers.tenantId, tenantId), eq(stockTransfers.id, transferId)));
    if (!transfer) throw new DomainError('TRANSFER_NOT_FOUND', 'Stock transfer was not found', 404);
    const lines = await tx
      .select()
      .from(stockTransferLines)
      .where(and(eq(stockTransferLines.tenantId, tenantId), eq(stockTransferLines.transferId, transferId)));
    return { ...transfer, lines: lines.sort((left, right) => left.lineNo - right.lineNo) };
  }

  private async assertWarehouseInTx(tx: DrizzleTx, tenantId: string, warehouseId: string) {
    const [warehouse] = await tx
      .select({ id: warehouses.id })
      .from(warehouses)
      .where(and(eq(warehouses.tenantId, tenantId), eq(warehouses.id, warehouseId), isNull(warehouses.deletedAt)));
    if (!warehouse) throw new DomainError('WAREHOUSE_NOT_FOUND', 'Warehouse was not found', 404);
  }

  private async requireStockItemInTx(tx: DrizzleTx, tenantId: string, itemId: string) {
    const [item] = await tx
      .select()
      .from(items)
      .where(and(eq(items.tenantId, tenantId), eq(items.id, itemId), isNull(items.deletedAt)));
    if (!item) throw new DomainError('ITEM_NOT_FOUND', 'Inventory item was not found', 404);
    if (item.kind !== 'stock') {
      throw new DomainError('INVENTORY_ITEM_NOT_STOCKED', 'Only stock-kind items can have inventory controls', 422);
    }
    return item;
  }
}

function rowsOf<T>(result: unknown): T[] {
  return Array.isArray(result) ? (result as T[]) : ((result as { rows?: T[] }).rows ?? []);
}
