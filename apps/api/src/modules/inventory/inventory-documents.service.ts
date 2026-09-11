/* eslint-disable no-restricted-syntax */
import { Inject, Injectable } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { and, desc, eq, inArray, isNull } from 'drizzle-orm';
import { DomainError, newId } from '@erp/contracts';
import {
  accounts,
  branches,
  inventoryDocumentLines,
  inventoryDocuments,
  inventoryTransactions,
  itemUnits,
  items,
  journalEntries,
  warehouses,
  withTenantTx,
  type DatabaseHandle,
  type DrizzleTx,
} from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';
import { AccountingService, type JournalLineInput } from '../accounting/accounting.service.js';
import { PostingProfilesService } from '../organization/posting-profiles/posting-profiles.service.js';
import { tryGetAuthContext } from '../platform/context/tenant-context.js';
import { SequencesService } from '../platform-services/index.js';

import { InventoryService, type InventoryLine } from './inventory.service.js';

export type InventoryDocumentKind = 'opening' | 'receipt' | 'issue' | 'adjustment';
export type InventoryDocumentLineInput = {
  itemId: string;
  /** Omitted means the item's base unit. */
  unitId?: string;
  quantity: string;
  /** Cost per entered unit. Required when the line enters stock. */
  unitCost?: string;
  lotId?: string;
  serialIds?: string[];
  /** Required only for adjustment documents. */
  adjustmentDirection?: 'in' | 'out';
  note?: string;
};
export type InventoryDocumentInput = {
  branchId: string;
  warehouseId: string;
  kind: InventoryDocumentKind;
  documentDate?: string;
  reason?: string;
  /** The offset account; inventory account itself comes from the posting profile. */
  counterAccountId?: string;
  notes?: string;
  lines: InventoryDocumentLineInput[];
};

const dec = (value: string | number | null | undefined) => new Decimal(value ?? '0');
const today = () => new Date().toISOString().slice(0, 10);

/**
 * Stock documents own the desktop's invType 4/5/9 flows:
 *
 * - `opening` = بضاعة أول المدة (invType 9)
 * - `receipt` = فاتورة إدخال (invType 4)
 * - `issue` = فاتورة إخراج (invType 5)
 * - `adjustment` = approved inventory variance
 *
 * A document can be drafted freely, but posting resolves the inventory account from the
 * branch profile and builds the counter legs itself. Clients cannot inject a journal or
 * stock ledger line, and the document, journal and stock movement all commit together.
 */
@Injectable()
export class InventoryDocumentsService {
  constructor(
    @Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle,
    private readonly inventory: InventoryService,
    private readonly accounting: AccountingService,
    private readonly profiles: PostingProfilesService,
    private readonly sequences: SequencesService,
  ) {}

  async list(tenantId: string, filters: { kind?: string; status?: string; warehouseId?: string } = {}) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const documents = await tx
        .select()
        .from(inventoryDocuments)
        .where(
          and(
            eq(inventoryDocuments.tenantId, tenantId),
            filters.kind ? eq(inventoryDocuments.kind, filters.kind) : undefined,
            filters.status ? eq(inventoryDocuments.status, filters.status) : undefined,
            filters.warehouseId ? eq(inventoryDocuments.warehouseId, filters.warehouseId) : undefined,
          ),
        )
        .orderBy(desc(inventoryDocuments.documentDate), desc(inventoryDocuments.createdAt))
        .limit(200);
      if (!documents.length) return [];
      const lines = await tx
        .select()
        .from(inventoryDocumentLines)
        .where(
          and(
            eq(inventoryDocumentLines.tenantId, tenantId),
            inArray(
              inventoryDocumentLines.documentId,
              documents.map((document) => document.id),
            ),
          ),
        );
      return documents.map((document) => ({
        ...document,
        lines: lines
          .filter((line) => line.documentId === document.id)
          .sort((left, right) => left.lineNo - right.lineNo),
      }));
    });
  }

  get(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, (tx) => this.load(tx, tenantId, id));
  }

  async create(tenantId: string, input: InventoryDocumentInput) {
    this.assertInput(input);
    const id = newId();
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      await this.assertBranchWarehouseInTx(tx, tenantId, input.branchId, input.warehouseId);
      const normalized = [] as Array<{
        id: string;
        documentId: string;
        tenantId: string;
        lineNo: number;
        itemId: string;
        unitId: string;
        quantity: string;
        baseQuantity: string;
        unitCost: string;
        lotId?: string;
        serialIds: string[];
        adjustmentDirection?: 'in' | 'out';
        note?: string;
        createdBy?: string;
      }>;
      for (const [index, line] of input.lines.entries()) {
        const result = await this.resolveDocumentLineInTx(tx, tenantId, line);
        normalized.push({
          id: newId(),
          documentId: id,
          tenantId,
          lineNo: index + 1,
          itemId: line.itemId,
          unitId: result.unitId,
          quantity: result.quantity,
          baseQuantity: result.baseQuantity,
          unitCost: result.unitCost,
          lotId: line.lotId,
          serialIds: line.serialIds ?? [],
          adjustmentDirection: line.adjustmentDirection,
          note: line.note,
          createdBy: tryGetAuthContext()?.userId,
        });
      }
      const allocated = await this.sequences.next(
        { tenantId, branchId: input.branchId, docType: `inventory_${input.kind}` },
        tx,
        { prefix: prefixFor(input.kind), padding: 6 },
      );
      await tx.insert(inventoryDocuments).values({
        id,
        tenantId,
        branchId: input.branchId,
        warehouseId: input.warehouseId,
        number: allocated.display,
        kind: input.kind,
        status: 'draft',
        documentDate: input.documentDate ?? today(),
        reason: input.reason?.trim() || null,
        counterAccountId: input.counterAccountId ?? null,
        notes: input.notes?.trim() || null,
        createdBy: tryGetAuthContext()?.userId,
      });
      await tx.insert(inventoryDocumentLines).values(normalized);
      return this.load(tx, tenantId, id);
    });
  }

  /**
   * A saved document remains an editable draft until it is posted. Rebuild its lines in
   * the same transaction as the header so a scanner/unit edit cannot leave stale
   * base-quantity rows behind. Kind and allocated number are deliberately immutable.
   */
  async updateDraft(tenantId: string, id: string, input: InventoryDocumentInput) {
    this.assertInput(input);
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const existing = await this.load(tx, tenantId, id);
      if (existing.status !== 'draft') {
        throw new DomainError('INVENTORY_DOCUMENT_INVALID_STATUS', 'Only a draft inventory document can be changed', 409);
      }
      if (input.kind !== existing.kind) {
        throw new DomainError('INVENTORY_DOCUMENT_KIND_IMMUTABLE', 'Document kind cannot be changed after it is numbered', 422, {
          field: 'kind',
        });
      }
      await this.assertBranchWarehouseInTx(tx, tenantId, input.branchId, input.warehouseId);
      const normalized = [] as Array<{
        id: string;
        documentId: string;
        tenantId: string;
        lineNo: number;
        itemId: string;
        unitId: string;
        quantity: string;
        baseQuantity: string;
        unitCost: string;
        lotId?: string;
        serialIds: string[];
        adjustmentDirection?: 'in' | 'out';
        note?: string;
        createdBy?: string;
      }>;
      for (const [index, line] of input.lines.entries()) {
        const result = await this.resolveDocumentLineInTx(tx, tenantId, line);
        normalized.push({
          id: newId(),
          documentId: id,
          tenantId,
          lineNo: index + 1,
          itemId: line.itemId,
          unitId: result.unitId,
          quantity: result.quantity,
          baseQuantity: result.baseQuantity,
          unitCost: result.unitCost,
          lotId: line.lotId,
          serialIds: line.serialIds ?? [],
          adjustmentDirection: line.adjustmentDirection,
          note: line.note,
          createdBy: tryGetAuthContext()?.userId,
        });
      }
      await tx
        .update(inventoryDocuments)
        .set({
          branchId: input.branchId,
          warehouseId: input.warehouseId,
          documentDate: input.documentDate ?? existing.documentDate,
          reason: input.reason?.trim() || null,
          counterAccountId: input.counterAccountId ?? null,
          notes: input.notes?.trim() || null,
          updatedAt: new Date(),
          updatedBy: tryGetAuthContext()?.userId,
          version: existing.version + 1,
        })
        .where(and(eq(inventoryDocuments.tenantId, tenantId), eq(inventoryDocuments.id, id), eq(inventoryDocuments.status, 'draft')));
      await tx
        .delete(inventoryDocumentLines)
        .where(and(eq(inventoryDocumentLines.tenantId, tenantId), eq(inventoryDocumentLines.documentId, id)));
      await tx.insert(inventoryDocumentLines).values(normalized);
      return this.load(tx, tenantId, id);
    });
  }

  async post(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const document = await this.load(tx, tenantId, id);
      if (document.status !== 'draft') {
        throw new DomainError('INVENTORY_DOCUMENT_INVALID_STATUS', 'Only a draft inventory document can be posted', 409);
      }
      if (!document.counterAccountId) {
        throw new DomainError('INVENTORY_COUNTER_ACCOUNT_REQUIRED', 'Choose an offset account before posting stock', 422, {
          field: 'counterAccountId',
        });
      }
      await this.assertCounterAccountInTx(tx, tenantId, document.counterAccountId);
      const profile = await this.profiles.resolvePostProfileInTx(
        tx,
        tenantId,
        document.branchId,
        document.kind === 'opening' ? 'opening_balance' : 'stock_adjustment',
      );
      const inventoryAccountId = profile.mapping.inventoryAccountId;
      if (!inventoryAccountId) {
        throw new DomainError('INVENTORY_PROFILE_KEY_MISSING', 'Posting profile has no inventoryAccountId', 422, {
          field: 'inventoryAccountId',
        });
      }

      const movements = await this.inventory.recordInTx(
        tx,
        tenantId,
        document.lines.map((line) => this.movementFor(document, line)),
      );
      const totalIn = movements.movements
        .filter((movement) => movement.direction === 'in')
        .reduce((sum, movement) => sum.plus(movement.totalCost), new Decimal(0));
      const totalOut = movements.movements
        .filter((movement) => movement.direction === 'out')
        .reduce((sum, movement) => sum.plus(movement.totalCost), new Decimal(0));

      const fiscalPeriodId = await this.accounting.openPeriodForDateInTx(tx, tenantId, document.documentDate);
      const journal = await this.accounting.postJournalInTx(tx, tenantId, {
        branchId: document.branchId,
        fiscalPeriodId,
        date: document.documentDate,
        description: `${labelFor(document.kind as InventoryDocumentKind)} ${document.number}${document.reason ? ` — ${document.reason}` : ''}`,
        sourceType: 'inventory_document',
        sourceId: document.id,
        idempotencyKey: `inventory-document-post:${document.id}`,
        lines: this.journalLines(inventoryAccountId, document.counterAccountId, totalIn, totalOut),
      });
      await tx
        .update(inventoryDocuments)
        .set({
          status: 'posted',
          journalEntryId: journal?.id ?? null,
          postedAt: new Date(),
          updatedAt: new Date(),
          updatedBy: tryGetAuthContext()?.userId,
        })
        .where(
          and(
            eq(inventoryDocuments.tenantId, tenantId),
            eq(inventoryDocuments.id, id),
            eq(inventoryDocuments.status, 'draft'),
          ),
        );
      return this.load(tx, tenantId, id);
    });
  }

  async cancel(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const document = await this.load(tx, tenantId, id);
      if (document.status !== 'draft') {
        throw new DomainError('INVENTORY_DOCUMENT_INVALID_STATUS', 'Only a draft inventory document can be cancelled', 409);
      }
      await tx
        .update(inventoryDocuments)
        .set({ status: 'cancelled', cancelledAt: new Date(), updatedAt: new Date(), updatedBy: tryGetAuthContext()?.userId })
        .where(and(eq(inventoryDocuments.tenantId, tenantId), eq(inventoryDocuments.id, id)));
      return this.load(tx, tenantId, id);
    });
  }

  /**
   * A posted stock document is reversed, never deleted. The mirror movement uses the
   * original unit cost for an incoming reversal and keeps the original journal linked by
   * `reversalOf`; the immutable ledger remains auditable.
   */
  async void(tenantId: string, id: string, reason: string) {
    if (!reason.trim()) {
      throw new DomainError('INVENTORY_DOCUMENT_VOID_REASON_REQUIRED', 'A void reason is required', 422);
    }
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const document = await this.load(tx, tenantId, id);
      if (document.status !== 'posted') {
        throw new DomainError('INVENTORY_DOCUMENT_INVALID_STATUS', 'Only a posted inventory document can be voided', 409);
      }
      const existing = document.journalEntryId
        ? (
            await tx
              .select({ id: journalEntries.id })
              .from(journalEntries)
              .where(and(eq(journalEntries.tenantId, tenantId), eq(journalEntries.reversalOf, document.journalEntryId)))
          )[0]
        : undefined;
      if (existing) {
        throw new DomainError('INVENTORY_DOCUMENT_ALREADY_VOIDED', 'This inventory document has already been reversed', 409);
      }

      const sourceMovements = await tx
        .select()
        .from(inventoryTransactions)
        .where(
          and(
            eq(inventoryTransactions.tenantId, tenantId),
            eq(inventoryTransactions.docType, `inventory_${document.kind}`),
            eq(inventoryTransactions.docId, document.id),
          ),
        );
      if (!sourceMovements.length) {
        throw new DomainError('INVENTORY_DOCUMENT_MOVEMENTS_MISSING', 'No stock movements were found for this document', 409);
      }
      // An incoming document is reversed out at the *current* pool average. If inventory
      // has subsequently moved, pretending it is still worth the old cost would corrupt
      // moving average. The automatically generated reversal journal uses the actual
      // mirror amount, so stock and accounting remain in agreement.
      const mirrors: InventoryLine[] = sourceMovements.map((movement) => ({
        itemId: movement.itemId,
        warehouseId: movement.warehouseId,
        qty: movement.baseQty,
        unitCost: movement.unitCost,
        direction: movement.direction === 'in' ? 'out' : 'in',
        docType: 'inventory_document_void',
        docId: document.id,
        lineId: newId(),
        lotId: movement.lotId ?? undefined,
        serialId: movement.serialId ?? undefined,
        costing: movement.direction === 'in' ? 'outAtAvg' : 'returnAtOriginalCost',
      }));
      const mirror = await this.inventory.recordInTx(tx, tenantId, mirrors);
      const totalIn = mirror.movements
        .filter((movement) => movement.direction === 'in')
        .reduce((sum, movement) => sum.plus(movement.totalCost), new Decimal(0));
      const totalOut = mirror.movements
        .filter((movement) => movement.direction === 'out')
        .reduce((sum, movement) => sum.plus(movement.totalCost), new Decimal(0));

      const profile = await this.profiles.resolvePostProfileInTx(
        tx,
        tenantId,
        document.branchId,
        document.kind === 'opening' ? 'opening_balance' : 'stock_adjustment',
      );
      const inventoryAccountId = profile.mapping.inventoryAccountId;
      if (!inventoryAccountId || !document.counterAccountId) {
        throw new DomainError('INVENTORY_PROFILE_KEY_MISSING', 'The document needs inventory and counter accounts to reverse', 422);
      }
      const fiscalPeriodId = await this.accounting.openPeriodForDateInTx(tx, tenantId, today());
      const reversal = await this.accounting.postJournalInTx(tx, tenantId, {
        branchId: document.branchId,
        fiscalPeriodId,
        date: today(),
        description: `عكس ${document.number}: ${reason.trim()}`,
        sourceType: 'inventory_document_void',
        sourceId: document.id,
        idempotencyKey: `inventory-document-void:${document.id}`,
        kind: 'reversal',
        reversalOf: document.journalEntryId ?? undefined,
        lines: this.journalLines(inventoryAccountId, document.counterAccountId, totalIn, totalOut),
      });
      if (document.journalEntryId && reversal?.id) {
        await tx
          .update(journalEntries)
          .set({ status: 'void', updatedAt: new Date() })
          .where(and(eq(journalEntries.tenantId, tenantId), eq(journalEntries.id, document.journalEntryId)));

      }
      await tx
        .update(inventoryDocuments)
        .set({ status: 'voided', voidedAt: new Date(), updatedAt: new Date(), updatedBy: tryGetAuthContext()?.userId })
        .where(and(eq(inventoryDocuments.tenantId, tenantId), eq(inventoryDocuments.id, id)));
      return this.load(tx, tenantId, id);
    });
  }

  private assertInput(input: InventoryDocumentInput) {
    if (!['opening', 'receipt', 'issue', 'adjustment'].includes(input.kind)) {
      throw new DomainError('INVENTORY_DOCUMENT_KIND_INVALID', 'Inventory document kind is invalid', 422);
    }
    if (!input.branchId || !input.warehouseId) {
      throw new DomainError('INVENTORY_DOCUMENT_TARGET_REQUIRED', 'Branch and warehouse are required', 422);
    }
    if (!input.lines?.length) {
      throw new DomainError('INVENTORY_DOCUMENT_LINES_REQUIRED', 'An inventory document needs at least one line', 422);
    }
    if (input.documentDate && Number.isNaN(new Date(`${input.documentDate}T00:00:00Z`).getTime())) {
      throw new DomainError('INVENTORY_DOCUMENT_DATE_INVALID', 'Document date is invalid', 422);
    }
    for (const line of input.lines) {
      const quantity = dec(line.quantity);
      if (!line.itemId || !quantity.isFinite() || quantity.lte(0)) {
        throw new DomainError('INVENTORY_DOCUMENT_QTY_INVALID', 'Every document line needs an item and positive quantity', 422);
      }
      if (input.kind === 'adjustment' && !line.adjustmentDirection) {
        throw new DomainError('INVENTORY_ADJUSTMENT_DIRECTION_REQUIRED', 'An adjustment line needs an in/out direction', 422);
      }
      const entering = this.directionFor(input.kind, line.adjustmentDirection) === 'in';
      if (entering && (line.unitCost === undefined || !dec(line.unitCost).isFinite() || dec(line.unitCost).lt(0))) {
        throw new DomainError('INVENTORY_RECEIPT_COST_REQUIRED', 'Incoming stock needs a valid unit cost', 422, {
          field: 'unitCost',
        });
      }
    }
  }

  private async resolveDocumentLineInTx(tx: DrizzleTx, tenantId: string, line: InventoryDocumentLineInput) {
    const [item] = await tx
      .select({ id: items.id, kind: items.kind, baseUnitId: items.baseUnitId })
      .from(items)
      .where(and(eq(items.tenantId, tenantId), eq(items.id, line.itemId), isNull(items.deletedAt)));
    if (!item) throw new DomainError('ITEM_NOT_FOUND', 'Inventory item was not found', 404);
    if (item.kind !== 'stock') {
      throw new DomainError('INVENTORY_ITEM_NOT_STOCKED', 'Only stock-kind items can appear on an inventory document', 422);
    }
    const unitId = line.unitId ?? item.baseUnitId;
    let ratio = new Decimal(1);
    if (unitId !== item.baseUnitId) {
      const [unit] = await tx
        .select({ ratio: itemUnits.ratio })
        .from(itemUnits)
        .where(and(eq(itemUnits.itemId, item.id), eq(itemUnits.unitId, unitId)));
      if (!unit) {
        throw new DomainError('INVENTORY_ITEM_UNIT_INVALID', 'The selected unit is not configured for this item', 422, {
          field: 'unitId',
        });
      }
      ratio = dec(unit.ratio);
    }
    const quantity = dec(line.quantity);
    const cost = dec(line.unitCost);
    if (!cost.isFinite() || cost.lt(0)) {
      throw new DomainError('INVENTORY_UNIT_COST_INVALID', 'Unit cost must be a non-negative number', 422);
    }
    return {
      unitId,
      quantity: quantity.toFixed(4),
      baseQuantity: quantity.mul(ratio).toFixed(4),
      unitCost: cost.toFixed(4),
    };
  }

  private async assertBranchWarehouseInTx(tx: DrizzleTx, tenantId: string, branchId: string, warehouseId: string) {
    const [branch] = await tx
      .select({ id: branches.id })
      .from(branches)
      .where(and(eq(branches.tenantId, tenantId), eq(branches.id, branchId), isNull(branches.deletedAt)));
    if (!branch) throw new DomainError('BRANCH_NOT_FOUND', 'Branch was not found', 404);
    const [warehouse] = await tx
      .select({ id: warehouses.id, branchId: warehouses.branchId })
      .from(warehouses)
      .where(and(eq(warehouses.tenantId, tenantId), eq(warehouses.id, warehouseId), isNull(warehouses.deletedAt)));
    if (!warehouse) throw new DomainError('WAREHOUSE_NOT_FOUND', 'Warehouse was not found', 404);
    if (warehouse.branchId && warehouse.branchId !== branchId) {
      throw new DomainError('INVENTORY_DOCUMENT_BRANCH_MISMATCH', 'The warehouse belongs to another branch', 422);
    }
  }

  private async assertCounterAccountInTx(tx: DrizzleTx, tenantId: string, accountId: string) {
    const [account] = await tx
      .select({ id: accounts.id, isPostable: accounts.isPostable })
      .from(accounts)
      .where(and(eq(accounts.tenantId, tenantId), eq(accounts.id, accountId), isNull(accounts.deletedAt)));
    if (!account || !account.isPostable) {
      throw new DomainError('INVENTORY_COUNTER_ACCOUNT_INVALID', 'Counter account must be a postable account in this tenant', 422, {
        field: 'counterAccountId',
      });
    }
  }

  private movementFor(
    document: Awaited<ReturnType<InventoryDocumentsService['load']>>,
    line: Awaited<ReturnType<InventoryDocumentsService['load']>>['lines'][number],
  ): InventoryLine {
    const direction = this.directionFor(document.kind as InventoryDocumentKind, line.adjustmentDirection as 'in' | 'out' | null);
    return {
      itemId: line.itemId,
      warehouseId: document.warehouseId,
      qty: line.quantity,
      unitId: line.unitId,
      unitCost: line.unitCost,
      direction,
      docType: `inventory_${document.kind}`,
      docId: document.id,
      lineId: line.id,
      lotId: line.lotId ?? undefined,
      serialIds: line.serialIds,
      costing: direction === 'in' ? 'inWithCost' : 'outAtAvg',
      // Ledger and journal share the document accounting date. Noon UTC avoids a date-only
      // value crossing the previous/next local day while preserving the desktop document's
      // intended chronology in inventory activity reports.
      occurredAt: new Date(`${document.documentDate}T12:00:00.000Z`),
      metadata: { inventoryDocumentId: document.id, inventoryDocumentNumber: document.number, lineNo: line.lineNo },
    };
  }

  private directionFor(kind: InventoryDocumentKind, adjustmentDirection?: 'in' | 'out' | null): 'in' | 'out' {
    if (kind === 'issue') return 'out';
    if (kind === 'adjustment') {
      if (adjustmentDirection === 'in' || adjustmentDirection === 'out') return adjustmentDirection;
      throw new DomainError('INVENTORY_ADJUSTMENT_DIRECTION_REQUIRED', 'An adjustment line needs an in/out direction', 422);
    }
    return 'in';
  }

  private journalLines(
    inventoryAccountId: string,
    counterAccountId: string,
    totalIn: Decimal,
    totalOut: Decimal,
  ): JournalLineInput[] {
    const lines: JournalLineInput[] = [];
    if (totalIn.gt(0)) {
      lines.push({ accountId: inventoryAccountId, debit: totalIn.toFixed(4), credit: '0.0000' });
      lines.push({ accountId: counterAccountId, debit: '0.0000', credit: totalIn.toFixed(4) });
    }
    if (totalOut.gt(0)) {
      lines.push({ accountId: counterAccountId, debit: totalOut.toFixed(4), credit: '0.0000' });
      lines.push({ accountId: inventoryAccountId, debit: '0.0000', credit: totalOut.toFixed(4) });
    }
    if (!lines.length) {
      throw new DomainError('INVENTORY_DOCUMENT_VALUE_REQUIRED', 'The inventory document has no value to post', 422);
    }
    return lines;
  }

  private async load(tx: DrizzleTx, tenantId: string, id: string) {
    const [document] = await tx
      .select()
      .from(inventoryDocuments)
      .where(and(eq(inventoryDocuments.tenantId, tenantId), eq(inventoryDocuments.id, id)));
    if (!document) throw new DomainError('INVENTORY_DOCUMENT_NOT_FOUND', 'Inventory document was not found', 404);
    const lines = await tx
      .select()
      .from(inventoryDocumentLines)
      .where(and(eq(inventoryDocumentLines.tenantId, tenantId), eq(inventoryDocumentLines.documentId, id)))
      .orderBy(inventoryDocumentLines.lineNo);
    return { ...document, lines };
  }
}

function prefixFor(kind: InventoryDocumentKind): string {
  if (kind === 'opening') return 'OS-';
  if (kind === 'receipt') return 'IR-';
  if (kind === 'issue') return 'IO-';
  return 'ADJ-';
}

function labelFor(kind: InventoryDocumentKind): string {
  if (kind === 'opening') return 'بضاعة أول المدة';
  if (kind === 'receipt') return 'فاتورة إدخال مخزني';
  if (kind === 'issue') return 'فاتورة إخراج مخزني';
  return 'تسوية مخزنية';
}
