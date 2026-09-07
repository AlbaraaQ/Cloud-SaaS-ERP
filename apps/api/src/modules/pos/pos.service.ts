import { Inject, Injectable } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { and, eq, sql } from 'drizzle-orm';
import { DomainError, newId } from '@erp/contracts';
import { diningTables, orderEvents, salesInvoices, tableCategories, tenantSettings, withTenantTx, type DatabaseHandle } from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';
import { SalesService } from '../sales/sales.service.js';
import { SequencesService } from '../platform-services/index.js';

export type TableCategoryInput = { branchId: string; name: string; sortOrder?: number; printerName?: string };
export type DiningTableInput = { branchId: string; categoryId: string; tableNo: string; name: string; seats?: number };
export type OrderItemInput = { itemId?: string; description?: string; qty: string; unitValue: string; modifiers?: Array<Record<string, unknown>> };

type OpenLine = { lineKey: string; itemId?: string | null; description?: string | null; qty: string; unitValue: string; modifiers: Array<Record<string, unknown>> };

@Injectable()
export class PosService {
  constructor(@Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle, private readonly sales: SalesService, private readonly sequences: SequencesService) {}

  async ensureEnabled(tenantId: string) {
    const [flag] = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(tenantSettings).where(and(eq(tenantSettings.tenantId, tenantId), eq(tenantSettings.key, 'pack.pos'))).limit(1));
    if (flag && flag.value !== true && flag.value !== 'true') throw new DomainError('NOT_FOUND', 'POS pack is disabled for this tenant', 404);
  }

  async categories(tenantId: string, branchId?: string) { await this.ensureEnabled(tenantId); return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(tableCategories).where(and(eq(tableCategories.tenantId, tenantId), branchId ? eq(tableCategories.branchId, branchId) : sql`true`)).orderBy(tableCategories.sortOrder)); }
  async createCategory(tenantId: string, input: TableCategoryInput) { await this.ensureEnabled(tenantId); const [row] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(tableCategories).values({ id: newId(), tenantId, branchId: input.branchId, name: input.name, sortOrder: input.sortOrder ?? 0, printerName: input.printerName }).returning()); return row; }

  async tables(tenantId: string, branchId?: string) { await this.ensureEnabled(tenantId); return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(diningTables).where(and(eq(diningTables.tenantId, tenantId), branchId ? eq(diningTables.branchId, branchId) : sql`true`)).orderBy(diningTables.tableNo)); }
  async createTable(tenantId: string, input: DiningTableInput) { await this.ensureEnabled(tenantId); const [row] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(diningTables).values({ id: newId(), tenantId, branchId: input.branchId, categoryId: input.categoryId, tableNo: input.tableNo, name: input.name, seats: input.seats ?? 4 }).returning()); return row; }

  async openTable(tenantId: string, id: string, businessDay = today()) { await this.ensureEnabled(tenantId); return withTenantTx(this.database.db, tenantId, async (tx) => { const [row] = await tx.select().from(diningTables).where(and(eq(diningTables.tenantId, tenantId), eq(diningTables.id, id))).limit(1); if (!row) throw new DomainError('NOT_FOUND', 'Dining table not found', 404); if (row.status === 'disabled') throw new DomainError('POS_TABLE_DISABLED', 'Table is disabled', 422); await tx.update(diningTables).set({ status: 'open', openedAt: row.openedAt ?? new Date(), updatedAt: new Date() }).where(and(eq(diningTables.tenantId, tenantId), eq(diningTables.id, id))); await tx.insert(orderEvents).values({ id: newId(), tenantId, branchId: row.branchId, tableId: id, kind: 'open', businessDay, lineKey: newId() }); return { id, status: 'open' }; }); }

  async addItem(tenantId: string, tableId: string, input: OrderItemInput, businessDay = today()) { await this.ensureEnabled(tenantId); if (new Decimal(input.qty).lte(0)) throw new DomainError('VALIDATION_FAILED', 'Quantity must be positive', 422); const [table] = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(diningTables).where(and(eq(diningTables.tenantId, tenantId), eq(diningTables.id, tableId))).limit(1)); if (!table || table.status === 'disabled') throw new DomainError('NOT_FOUND', 'Dining table not found or disabled', 404); const [row] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(orderEvents).values({ id: newId(), tenantId, branchId: table.branchId, tableId, kind: 'add_item', businessDay, lineKey: newId(), itemId: input.itemId, description: input.description, qty: input.qty, unitValue: input.unitValue, modifiers: input.modifiers ?? [], firedAt: new Date() }).returning()); return row; }

  async voidItem(tenantId: string, eventId: string, reason: string) { await this.ensureEnabled(tenantId); if (!reason.trim()) throw new DomainError('POS_VOID_REASON_REQUIRED', 'Void reason is required', 422); return withTenantTx(this.database.db, tenantId, async (tx) => { const [event] = await tx.select().from(orderEvents).where(and(eq(orderEvents.tenantId, tenantId), eq(orderEvents.id, eventId))).limit(1); if (!event || event.kind !== 'add_item') throw new DomainError('NOT_FOUND', 'Order item event not found', 404); const [row] = await tx.insert(orderEvents).values({ id: newId(), tenantId, branchId: event.branchId, tableId: event.tableId, kind: 'void_item', businessDay: event.businessDay, lineKey: event.lineKey, itemId: event.itemId, description: event.description, qty: event.qty, unitValue: event.unitValue, modifiers: event.modifiers, reason, voidedAt: new Date() }).returning(); return row; }); }

  async merge(tenantId: string, sourceTableId: string, targetTableId: string) { await this.ensureEnabled(tenantId); if (sourceTableId === targetTableId) throw new DomainError('VALIDATION_FAILED', 'Merge requires two different tables', 422); await withTenantTx(this.database.db, tenantId, (tx) => tx.update(diningTables).set({ status: 'merged', combinedInto: targetTableId, updatedAt: new Date() }).where(and(eq(diningTables.tenantId, tenantId), eq(diningTables.id, sourceTableId)))); return { data: { sourceTableId, targetTableId, status: 'merged' } }; }
  async split(tenantId: string, tableId: string) { await this.ensureEnabled(tenantId); await withTenantTx(this.database.db, tenantId, (tx) => tx.update(diningTables).set({ status: 'open', combinedInto: null, updatedAt: new Date() }).where(and(eq(diningTables.tenantId, tenantId), eq(diningTables.id, tableId)))); return { data: { tableId, status: 'open' } }; }

  async sendToInvoice(tenantId: string, tableId: string, cashCustomerName = 'POS customer', orderType = 'table', businessDay = today()) {
    await this.ensureEnabled(tenantId);
    const openLines = await this.openLines(tenantId, tableId);
    if (!openLines.length) throw new DomainError('POS_ORDER_EMPTY', 'No active POS lines to invoice', 422);
    const [table] = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(diningTables).where(and(eq(diningTables.tenantId, tenantId), eq(diningTables.id, tableId))).limit(1));
    if (!table) throw new DomainError('NOT_FOUND', 'Dining table not found', 404);
    const allocated = await withTenantTx(this.database.db, tenantId, (tx) => this.sequences.next({ tenantId, branchId: table.branchId, docType: `pos_order:${businessDay}` }, tx, { prefix: `POS-${businessDay.replaceAll('-', '')}-`, padding: 4 }));
    const invoice = await this.sales.create(tenantId, { branchId: table.branchId, cashCustomerName, kind: 'sale', lines: openLines.map((line) => ({ itemId: line.itemId ?? undefined, description: decoratedDescription(line), quantity: line.qty, unitPrice: line.unitValue, taxRate: '0' })) });
    await withTenantTx(this.database.db, tenantId, async (tx) => { await tx.update(salesInvoices).set({ orderType, tableNo: table.tableNo, updatedAt: new Date() }).where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, invoice.id))); await tx.update(orderEvents).set({ invoiceId: invoice.id, updatedAt: new Date() }).where(and(eq(orderEvents.tenantId, tenantId), eq(orderEvents.tableId, tableId), eq(orderEvents.kind, 'add_item'))); await tx.insert(orderEvents).values({ id: newId(), tenantId, branchId: table.branchId, tableId, kind: 'send_to_invoice', businessDay, lineKey: allocated.display, invoiceId: invoice.id }); await tx.update(diningTables).set({ currentInvoiceId: invoice.id, updatedAt: new Date() }).where(and(eq(diningTables.tenantId, tenantId), eq(diningTables.id, tableId))); });
    return { data: { orderNo: allocated.display, invoiceId: invoice.id, status: invoice.status, tableNo: table.tableNo } };
  }

  async close(tenantId: string, tableId: string) { await this.ensureEnabled(tenantId); return withTenantTx(this.database.db, tenantId, async (tx) => { const [row] = await tx.select().from(diningTables).where(and(eq(diningTables.tenantId, tenantId), eq(diningTables.id, tableId))).limit(1); if (!row?.currentInvoiceId) throw new DomainError('POS_TABLE_NOT_INVOICED', 'Send table to invoice before closing', 422); const posted = await this.sales.post(tenantId, row.currentInvoiceId); await tx.update(diningTables).set({ status: 'closed', closedAt: new Date(), currentInvoiceId: null, updatedAt: new Date() }).where(and(eq(diningTables.tenantId, tenantId), eq(diningTables.id, tableId))); return { data: { tableId, invoiceId: posted.id, number: posted.number, status: 'closed' } }; }); }

  async openLines(tenantId: string, tableId: string): Promise<OpenLine[]> { const rows = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(orderEvents).where(and(eq(orderEvents.tenantId, tenantId), eq(orderEvents.tableId, tableId))).orderBy(orderEvents.createdAt)); const voided = new Set(rows.filter((row) => row.kind === 'void_item').map((row) => row.lineKey)); return rows.filter((row) => row.kind === 'add_item' && !voided.has(row.lineKey) && !row.invoiceId).map((row) => ({ lineKey: row.lineKey, itemId: row.itemId, description: row.description, qty: row.qty, unitValue: row.unitValue, modifiers: row.modifiers })); }
}

function today(): string { return new Date().toISOString().slice(0, 10); }
function decoratedDescription(line: OpenLine): string { return JSON.stringify({ text: line.description ?? 'POS item', modifiers: line.modifiers }); }
