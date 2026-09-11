import { Inject, Injectable } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { and, asc, eq, isNull, ne, sql } from 'drizzle-orm';
import { calculateInvoiceTotals, DomainError, newId, permissionGrants } from '@erp/contracts';
import {
  branches,
  cashLocations,
  diningTables,
  itemUnits,
  items,
  orderEvents,
  posHeldTickets,
  posShortcutItems,
  salesInvoices,
  shiftCloses,
  tableCategories,
  tenantSettings,
  warehouses,
  withTenantTx,
  type DatabaseHandle,
  type DrizzleTx,
} from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';
import { SalesService } from '../sales/sales.service.js';
import { getTenantContext } from '../platform/context/tenant-context.js';
import { SequencesService } from '../platform-services/index.js';

export type TableCategoryInput = { branchId: string; name: string; sortOrder?: number; printerName?: string };
export type DiningTableInput = {
  branchId: string;
  categoryId: string;
  tableNo: string;
  name: string;
  seats?: number;
};
export type OrderItemInput = {
  itemId?: string;
  description?: string;
  qty: string;
  unitValue: string;
  modifiers?: Array<Record<string, unknown>>;
};

/** One cart line at the till — the smallest shape a cashier screen can send. */
export type PosCheckoutLine = {
  itemId: string;
  /** Omitted means the item's base unit. */
  unitId?: string;
  lotId?: string;
  serialIds?: string[];
  quantity: string;
  unitPrice: string;
  taxRate?: string;
  discountAmount?: string;
  description?: string;
};
/**
 * How the till takes the money. `card` is a network (bank-card) tender: it settles
 * to the branch's bank account exactly like `bank`, but keeps its own payment row
 * so the shift report can tell network takings from transfers.
 */
export type PosCheckoutPayment = {
  method: 'cash' | 'card' | 'bank' | 'credit';
  /** Portion of the invoice settled by this tender; required for a split ticket. */
  amount?: string;
  cashLocationId?: string;
  settlementAccountId?: string;
  /** Cash handed over by the customer; the difference is returned as change. */
  tendered?: string;
  reference?: string;
};
export type PosHeldTicketInput = {
  branchId: string;
  slot: number;
  /** Client cart state only; it never becomes a sale until checkout is called. */
  payload: Record<string, unknown>;
};
export type PosShortcutInput = { branchId: string; slot: number; itemId: string };

export type PosCheckoutInput = {
  branchId: string;
  warehouseId?: string;
  partyId?: string;
  cashCustomerName?: string;
  cashCustomerMobile?: string;
  priceIncludesVat?: boolean;
  invoiceDiscount?: string;
  orderType?: string;
  /** Force the sale onto a specific shift; defaults to the caller's open shift. */
  shiftId?: string;
  taxType?: 'simplified' | 'standard';
  lines: PosCheckoutLine[];
  /** Legacy single tender. */
  payment?: PosCheckoutPayment;
  /** Desktop multi-tender payment: amounts must exactly cover the invoice. */
  payments?: PosCheckoutPayment[];
};

type OpenLine = {
  lineKey: string;
  itemId?: string | null;
  description?: string | null;
  qty: string;
  unitValue: string;
  modifiers: Array<Record<string, unknown>>;
};

@Injectable()
export class PosService {
  constructor(
    @Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle,
    private readonly sales: SalesService,
    private readonly sequences: SequencesService,
  ) {}

  async ensureEnabled(tenantId: string) {
    const [flag] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(tenantSettings)
        .where(and(eq(tenantSettings.tenantId, tenantId), eq(tenantSettings.key, 'pack.pos')))
        .limit(1),
    );
    if (flag && flag.value !== true && flag.value !== 'true')
      throw new DomainError('NOT_FOUND', 'POS pack is disabled for this tenant', 404);
  }

  /** The desktop's eight hold/recall slots, persisted per cashier rather than in browser state. */
  async holdTicket(tenantId: string, userId: string, input: PosHeldTicketInput) {
    await this.ensureEnabled(tenantId);
    this.assertSlot(input.slot, 8, 'POS_HOLD_SLOT_INVALID');
    if (!input.payload || Array.isArray(input.payload) || typeof input.payload !== 'object') {
      throw new DomainError('POS_HOLD_PAYLOAD_INVALID', 'Held ticket payload must be an object', 422, { field: 'payload' });
    }
    if (Buffer.byteLength(JSON.stringify(input.payload), 'utf8') > 128_000) {
      throw new DomainError('POS_HOLD_PAYLOAD_TOO_LARGE', 'Held ticket payload is too large', 422, { field: 'payload' });
    }
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      await this.assertPosBranchInTx(tx, tenantId, input.branchId);
      // Re-saving a numbered slot supersedes the live ticket but preserves its audit
      // record. The partial unique index makes this safe even under two till tabs.
      await tx
        .update(posHeldTickets)
        .set({ status: 'cancelled', cancelledAt: new Date(), updatedAt: new Date(), updatedBy: userId })
        .where(
          and(
            eq(posHeldTickets.tenantId, tenantId),
            eq(posHeldTickets.branchId, input.branchId),
            eq(posHeldTickets.userId, userId),
            eq(posHeldTickets.slot, input.slot),
            eq(posHeldTickets.status, 'held'),
          ),
        );
      const [ticket] = await tx
        .insert(posHeldTickets)
        .values({
          id: newId(),
          tenantId,
          branchId: input.branchId,
          userId,
          slot: input.slot,
          status: 'held',
          payload: input.payload,
          heldAt: new Date(),
          createdBy: userId,
        })
        .returning();
      return ticket;
    });
  }

  listHeldTickets(tenantId: string, userId: string, branchId?: string, includeHistory = false) {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(posHeldTickets)
        .where(
          and(
            eq(posHeldTickets.tenantId, tenantId),
            eq(posHeldTickets.userId, userId),
            branchId ? eq(posHeldTickets.branchId, branchId) : undefined,
            includeHistory ? undefined : eq(posHeldTickets.status, 'held'),
          ),
        )
        .orderBy(asc(posHeldTickets.slot), asc(posHeldTickets.heldAt)),
    );
  }

  async recallHeldTicket(tenantId: string, userId: string, ticketId: string) {
    await this.ensureEnabled(tenantId);
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [ticket] = await tx
        .select()
        .from(posHeldTickets)
        .where(
          and(
            eq(posHeldTickets.tenantId, tenantId),
            eq(posHeldTickets.id, ticketId),
            eq(posHeldTickets.userId, userId),
            eq(posHeldTickets.status, 'held'),
          ),
        );
      if (!ticket) throw new DomainError('POS_HOLD_NOT_FOUND', 'Held ticket was not found', 404);
      const [recalled] = await tx
        .update(posHeldTickets)
        .set({ status: 'recalled', recalledAt: new Date(), updatedAt: new Date(), updatedBy: userId })
        .where(and(eq(posHeldTickets.tenantId, tenantId), eq(posHeldTickets.id, ticketId), eq(posHeldTickets.status, 'held')))
        .returning();
      if (!recalled) throw new DomainError('POS_HOLD_ALREADY_RECALLED', 'Held ticket is no longer available', 409);
      return recalled;
    });
  }

  async cancelHeldTicket(tenantId: string, userId: string, ticketId: string) {
    await this.ensureEnabled(tenantId);
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [cancelled] = await tx
        .update(posHeldTickets)
        .set({ status: 'cancelled', cancelledAt: new Date(), updatedAt: new Date(), updatedBy: userId })
        .where(
          and(
            eq(posHeldTickets.tenantId, tenantId),
            eq(posHeldTickets.id, ticketId),
            eq(posHeldTickets.userId, userId),
            eq(posHeldTickets.status, 'held'),
          ),
        )
        .returning();
      if (!cancelled) throw new DomainError('POS_HOLD_NOT_FOUND', 'Held ticket was not found or already recalled', 404);
      return cancelled;
    });
  }

  async listShortcuts(tenantId: string, branchId: string) {
    await this.ensureEnabled(tenantId);
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select({
          id: posShortcutItems.id,
          branchId: posShortcutItems.branchId,
          slot: posShortcutItems.slot,
          itemId: posShortcutItems.itemId,
          sku: items.sku,
          nameAr: items.nameAr,
          nameEn: items.nameEn,
          salePrice: items.salePrice,
        })
        .from(posShortcutItems)
        .innerJoin(items, eq(items.id, posShortcutItems.itemId))
        .where(
          and(
            eq(posShortcutItems.tenantId, tenantId),
            eq(posShortcutItems.branchId, branchId),
            eq(items.tenantId, tenantId),
            isNull(items.deletedAt),
          ),
        )
        .orderBy(asc(posShortcutItems.slot)),
    );
  }

  async setShortcut(tenantId: string, userId: string, input: PosShortcutInput) {
    await this.ensureEnabled(tenantId);
    this.assertSlot(input.slot, 48, 'POS_SHORTCUT_SLOT_INVALID');
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      await this.assertPosBranchInTx(tx, tenantId, input.branchId);
      const [item] = await tx
        .select({ id: items.id, showInPos: items.showInPos })
        .from(items)
        .where(and(eq(items.tenantId, tenantId), eq(items.id, input.itemId), isNull(items.deletedAt)));
      if (!item || !item.showInPos) {
        throw new DomainError('POS_SHORTCUT_ITEM_INVALID', 'Shortcut item must be an active POS item', 422, {
          field: 'itemId',
        });
      }
      // The branch has a unique item constraint as well as a unique slot constraint.
      // Removing an old placement first makes moving a tile a single atomic action.
      await tx
        .delete(posShortcutItems)
        .where(
          and(
            eq(posShortcutItems.tenantId, tenantId),
            eq(posShortcutItems.branchId, input.branchId),
            eq(posShortcutItems.itemId, input.itemId),
            ne(posShortcutItems.slot, input.slot),
          ),
        );
      const [shortcut] = await tx
        .insert(posShortcutItems)
        .values({
          id: newId(),
          tenantId,
          branchId: input.branchId,
          slot: input.slot,
          itemId: input.itemId,
          createdBy: userId,
          updatedBy: userId,
        })
        .onConflictDoUpdate({
          target: [posShortcutItems.tenantId, posShortcutItems.branchId, posShortcutItems.slot],
          set: { itemId: input.itemId, updatedAt: new Date(), updatedBy: userId },
        })
        .returning();
      return shortcut;
    });
  }

  async removeShortcut(tenantId: string, branchId: string, slot: number) {
    await this.ensureEnabled(tenantId);
    this.assertSlot(slot, 48, 'POS_SHORTCUT_SLOT_INVALID');
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [removed] = await tx
        .delete(posShortcutItems)
        .where(
          and(
            eq(posShortcutItems.tenantId, tenantId),
            eq(posShortcutItems.branchId, branchId),
            eq(posShortcutItems.slot, slot),
          ),
        )
        .returning();
      if (!removed) throw new DomainError('POS_SHORTCUT_NOT_FOUND', 'Shortcut slot is not configured', 404);
      return { slot, deleted: true };
    });
  }

  private assertSlot(slot: number, maximum: number, code: string) {
    if (!Number.isInteger(slot) || slot < 1 || slot > maximum) {
      throw new DomainError(code, `Slot must be between 1 and ${maximum}`, 422, { field: 'slot' });
    }
  }

  private async assertPosBranchInTx(tx: DrizzleTx, tenantId: string, branchId: string) {
    const [branch] = await tx
      .select({ id: branches.id, isActive: branches.isActive })
      .from(branches)
      .where(and(eq(branches.tenantId, tenantId), eq(branches.id, branchId), isNull(branches.deletedAt)));
    if (!branch || !branch.isActive) {
      throw new DomainError('POS_BRANCH_INVALID', 'POS branch was not found or is inactive', 422, { field: 'branchId' });
    }
  }

  async categories(tenantId: string, branchId?: string) {
    await this.ensureEnabled(tenantId);
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(tableCategories)
        .where(
          and(
            eq(tableCategories.tenantId, tenantId),
            branchId ? eq(tableCategories.branchId, branchId) : sql`true`,
          ),
        )
        .orderBy(tableCategories.sortOrder),
    );
  }
  async createCategory(tenantId: string, input: TableCategoryInput) {
    await this.ensureEnabled(tenantId);
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .insert(tableCategories)
        .values({
          id: newId(),
          tenantId,
          branchId: input.branchId,
          name: input.name,
          sortOrder: input.sortOrder ?? 0,
          printerName: input.printerName,
        })
        .returning(),
    );
    return row;
  }

  async tables(tenantId: string, branchId?: string) {
    await this.ensureEnabled(tenantId);
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(diningTables)
        .where(
          and(
            eq(diningTables.tenantId, tenantId),
            branchId ? eq(diningTables.branchId, branchId) : sql`true`,
          ),
        )
        .orderBy(diningTables.tableNo),
    );
  }
  async createTable(tenantId: string, input: DiningTableInput) {
    await this.ensureEnabled(tenantId);
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .insert(diningTables)
        .values({
          id: newId(),
          tenantId,
          branchId: input.branchId,
          categoryId: input.categoryId,
          tableNo: input.tableNo,
          name: input.name,
          seats: input.seats ?? 4,
        })
        .returning(),
    );
    return row;
  }

  async openTable(tenantId: string, id: string, businessDay = today()) {
    await this.ensureEnabled(tenantId);
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [row] = await tx
        .select()
        .from(diningTables)
        .where(and(eq(diningTables.tenantId, tenantId), eq(diningTables.id, id)))
        .limit(1);
      if (!row) throw new DomainError('NOT_FOUND', 'Dining table not found', 404);
      if (row.status === 'disabled') throw new DomainError('POS_TABLE_DISABLED', 'Table is disabled', 422);
      await tx
        .update(diningTables)
        .set({ status: 'open', openedAt: row.openedAt ?? new Date(), updatedAt: new Date() })
        .where(and(eq(diningTables.tenantId, tenantId), eq(diningTables.id, id)));
      await tx
        .insert(orderEvents)
        .values({
          id: newId(),
          tenantId,
          branchId: row.branchId,
          tableId: id,
          kind: 'open',
          businessDay,
          lineKey: newId(),
        });
      return { id, status: 'open' };
    });
  }

  async addItem(tenantId: string, tableId: string, input: OrderItemInput, businessDay = today()) {
    await this.ensureEnabled(tenantId);
    if (new Decimal(input.qty).lte(0))
      throw new DomainError('VALIDATION_FAILED', 'Quantity must be positive', 422);
    const [table] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(diningTables)
        .where(and(eq(diningTables.tenantId, tenantId), eq(diningTables.id, tableId)))
        .limit(1),
    );
    if (!table || table.status === 'disabled')
      throw new DomainError('NOT_FOUND', 'Dining table not found or disabled', 404);
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .insert(orderEvents)
        .values({
          id: newId(),
          tenantId,
          branchId: table.branchId,
          tableId,
          kind: 'add_item',
          businessDay,
          lineKey: newId(),
          itemId: input.itemId,
          description: input.description,
          qty: input.qty,
          unitValue: input.unitValue,
          modifiers: input.modifiers ?? [],
          firedAt: new Date(),
        })
        .returning(),
    );
    return row;
  }

  async voidItem(tenantId: string, eventId: string, reason: string) {
    await this.ensureEnabled(tenantId);
    if (!reason.trim()) throw new DomainError('POS_VOID_REASON_REQUIRED', 'Void reason is required', 422);
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [event] = await tx
        .select()
        .from(orderEvents)
        .where(and(eq(orderEvents.tenantId, tenantId), eq(orderEvents.id, eventId)))
        .limit(1);
      if (!event || event.kind !== 'add_item')
        throw new DomainError('NOT_FOUND', 'Order item event not found', 404);
      const [row] = await tx
        .insert(orderEvents)
        .values({
          id: newId(),
          tenantId,
          branchId: event.branchId,
          tableId: event.tableId,
          kind: 'void_item',
          businessDay: event.businessDay,
          lineKey: event.lineKey,
          itemId: event.itemId,
          description: event.description,
          qty: event.qty,
          unitValue: event.unitValue,
          modifiers: event.modifiers,
          reason,
          voidedAt: new Date(),
        })
        .returning();
      return row;
    });
  }

  async merge(tenantId: string, sourceTableId: string, targetTableId: string) {
    await this.ensureEnabled(tenantId);
    if (sourceTableId === targetTableId)
      throw new DomainError('VALIDATION_FAILED', 'Merge requires two different tables', 422);
    await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .update(diningTables)
        .set({ status: 'merged', combinedInto: targetTableId, updatedAt: new Date() })
        .where(and(eq(diningTables.tenantId, tenantId), eq(diningTables.id, sourceTableId))),
    );
    return { data: { sourceTableId, targetTableId, status: 'merged' } };
  }
  async split(tenantId: string, tableId: string) {
    await this.ensureEnabled(tenantId);
    await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .update(diningTables)
        .set({ status: 'open', combinedInto: null, updatedAt: new Date() })
        .where(and(eq(diningTables.tenantId, tenantId), eq(diningTables.id, tableId))),
    );
    return { data: { tableId, status: 'open' } };
  }

  async sendToInvoice(
    tenantId: string,
    tableId: string,
    cashCustomerName = 'POS customer',
    orderType = 'table',
    businessDay = today(),
  ) {
    await this.ensureEnabled(tenantId);
    const openLines = await this.openLines(tenantId, tableId);
    if (!openLines.length) throw new DomainError('POS_ORDER_EMPTY', 'No active POS lines to invoice', 422);
    const [table] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(diningTables)
        .where(and(eq(diningTables.tenantId, tenantId), eq(diningTables.id, tableId)))
        .limit(1),
    );
    if (!table) throw new DomainError('NOT_FOUND', 'Dining table not found', 404);
    const allocated = await withTenantTx(this.database.db, tenantId, (tx) =>
      this.sequences.next({ tenantId, branchId: table.branchId, docType: `pos_order:${businessDay}` }, tx, {
        prefix: `POS-${businessDay.replaceAll('-', '')}-`,
        padding: 4,
      }),
    );
    // A table order is a stock-moving sale, and the posting engine (rightly) refuses
    // one without a warehouse. The till's own branch default is the only sensible
    // answer — the desktop `frmPOS` always sold out of the branch's store.
    const [warehouse] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select({ id: warehouses.id })
        .from(warehouses)
        .where(
          and(
            eq(warehouses.tenantId, tenantId),
            eq(warehouses.branchId, table.branchId),
            eq(warehouses.isActive, true),
            isNull(warehouses.deletedAt),
          ),
        )
        .orderBy(warehouses.isDefault, warehouses.code)
        .limit(1),
    );
    const invoice = await this.sales.create(tenantId, {
      branchId: table.branchId,
      warehouseId: warehouse?.id,
      cashCustomerName,
      kind: 'sale',
      lines: openLines.map((line) => ({
        itemId: line.itemId ?? undefined,
        description: decoratedDescription(line),
        quantity: line.qty,
        unitPrice: line.unitValue,
        taxRate: '0',
      })),
    });
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      await tx
        .update(salesInvoices)
        .set({ orderType, tableNo: table.tableNo, updatedAt: new Date() })
        .where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, invoice.id)));
      await tx
        .update(orderEvents)
        .set({ invoiceId: invoice.id, updatedAt: new Date() })
        .where(
          and(
            eq(orderEvents.tenantId, tenantId),
            eq(orderEvents.tableId, tableId),
            eq(orderEvents.kind, 'add_item'),
          ),
        );
      await tx
        .insert(orderEvents)
        .values({
          id: newId(),
          tenantId,
          branchId: table.branchId,
          tableId,
          kind: 'send_to_invoice',
          businessDay,
          lineKey: allocated.display,
          invoiceId: invoice.id,
        });
      await tx
        .update(diningTables)
        .set({ currentInvoiceId: invoice.id, updatedAt: new Date() })
        .where(and(eq(diningTables.tenantId, tenantId), eq(diningTables.id, tableId)));
    });
    return {
      data: {
        orderNo: allocated.display,
        invoiceId: invoice.id,
        status: invoice.status,
        tableNo: table.tableNo,
      },
    };
  }

  /**
   * Closes a table: posts the order invoice and settles it the way the guest paid.
   * `settlement` defaults to `credit` (the table goes on the room/customer account),
   * which is what the old behaviour did implicitly — but a cashier can now close a
   * table against cash or a card and have the payment row written in the same
   * transaction as the journal.
   */
  async close(
    tenantId: string,
    tableId: string,
    settlementInput?: {
      settlement?: 'credit' | 'cash' | 'card' | 'bank';
      cashLocationId?: string;
      settlementAccountId?: string;
    },
  ) {
    await this.ensureEnabled(tenantId);
    const [table] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(diningTables)
        .where(and(eq(diningTables.tenantId, tenantId), eq(diningTables.id, tableId)))
        .limit(1),
    );
    if (!table?.currentInvoiceId)
      throw new DomainError('POS_TABLE_NOT_INVOICED', 'Send table to invoice before closing', 422);
    const method = settlementInput?.settlement ?? 'credit';
    const target =
      method === 'credit'
        ? undefined
        : await this.resolveTenderTarget(tenantId, table.branchId, {
            method,
            cashLocationId: settlementInput?.cashLocationId,
            settlementAccountId: settlementInput?.settlementAccountId,
          });
    const posted = await this.sales.post(tenantId, table.currentInvoiceId, {
      settlement: method,
      settlementAccountId: target?.accountId,
      settlementCashLocationId: target?.cashLocationId,
    });
    await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .update(diningTables)
        .set({ status: 'closed', closedAt: new Date(), currentInvoiceId: null, updatedAt: new Date() })
        .where(and(eq(diningTables.tenantId, tenantId), eq(diningTables.id, tableId))),
    );
    return {
      data: {
        tableId,
        invoiceId: posted.id,
        number: posted.number,
        total: posted.total,
        paidTotal: posted.paidTotal,
        paymentStatus: posted.paymentStatus,
        method,
        status: 'closed',
      },
    };
  }

  // ── Till checkout ───────────────────────────────────────────────────────────
  //
  // The desktop `frmPOS` wrote the invoice, its stock movement, its entry and its
  // payment from one Save button. Until Phase 04 the cloud screen did that in three
  // browser round trips, each of which could fail and leave a sale that exists in
  // `sales_invoices` but has no journal, no stock movement or no payment — the
  // exact "half-finished sale" a till must never produce.
  //
  // `checkout` is the engine contract: one API call, one transaction. It borrows
  // the sales engine wholesale (numbering, gates, average-cost stock relief,
  // profile-built journal, settlement payment) and adds what only a till knows:
  // the drawer the money went into, the shift that captured the sale, and the
  // change owed back to the customer.

  /** Resolves the drawer/account a tender lands in — never an unvalidated guess. */
  private async resolveTenderTarget(tenantId: string, branchId: string, payment: PosCheckoutPayment) {
    const kind = payment.method === 'cash' ? 'safe' : 'bank';
    const rows = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(cashLocations)
        .where(
          and(
            eq(cashLocations.tenantId, tenantId),
            eq(cashLocations.branchId, branchId),
            eq(cashLocations.isActive, true),
            isNull(cashLocations.deletedAt),
            payment.cashLocationId ? eq(cashLocations.id, payment.cashLocationId) : sql`true`,
          ),
        ),
    );
    if (payment.cashLocationId) {
      const named = rows[0];
      if (!named)
        throw new DomainError(
          'POS_CASH_LOCATION_INVALID',
          'The cash location does not belong to this branch',
          422,
          { field: 'cashLocationId' },
        );
      const accountId = payment.settlementAccountId ?? named.accountId ?? undefined;
      if (!accountId)
        throw new DomainError(
          'POS_SETTLEMENT_ACCOUNT_REQUIRED',
          'This cash location has no linked account — link one in Settings › Cash locations',
          422,
          { field: 'cashLocationId' },
        );
      return { cashLocationId: named.id, accountId };
    }
    // No drawer named: fall back to the branch default of the tender's kind, so a
    // cashier screen with a single till never has to ask.
    const fallback =
      rows.find((row) => row.kind === kind && row.isDefault) ?? rows.find((row) => row.kind === kind);
    const accountId = payment.settlementAccountId ?? fallback?.accountId ?? undefined;
    if (!accountId)
      throw new DomainError(
        'POS_SETTLEMENT_ACCOUNT_REQUIRED',
        'No cash location with a linked account is configured for this branch',
        422,
        { field: 'cashLocationId' },
      );
    return { cashLocationId: fallback?.id, accountId };
  }

  /**
   * Which shift captures this sale. An explicit `shiftId` is validated; otherwise
   * the caller's own open shift at that branch is used, and a till with no shift
   * simply records none — unless the tenant turned `pos.requireShift` on, in which
   * case cash cannot be taken outside a shift (the desktop's day-close discipline).
   */
  private async resolveShiftId(
    tenantId: string,
    branchId: string,
    userId: string,
    requested: string | undefined,
    isCash: boolean,
  ): Promise<string | undefined> {
    const [shift] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(shiftCloses)
        .where(
          requested
            ? and(eq(shiftCloses.tenantId, tenantId), eq(shiftCloses.id, requested))
            : and(
                eq(shiftCloses.tenantId, tenantId),
                eq(shiftCloses.branchId, branchId),
                eq(shiftCloses.userId, userId),
                eq(shiftCloses.status, 'open'),
              ),
        ),
    );
    if (requested) {
      if (!shift)
        throw new DomainError('POS_SHIFT_INVALID', 'Cashier shift was not found', 404, { field: 'shiftId' });
      if (shift.status !== 'open')
        throw new DomainError('POS_SHIFT_CLOSED', 'This cashier shift is already closed', 409, {
          field: 'shiftId',
        });
      if (shift.branchId !== branchId)
        throw new DomainError('POS_SHIFT_BRANCH_MISMATCH', 'This shift belongs to another branch', 422, {
          field: 'shiftId',
        });
      return shift.id;
    }
    if (!shift && isCash && (await this.requireShift(tenantId))) {
      throw new DomainError('POS_SHIFT_REQUIRED', 'Open a cashier shift before taking cash', 422);
    }
    return shift?.id;
  }

  private async requireShift(tenantId: string): Promise<boolean> {
    const [flag] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(tenantSettings)
        .where(and(eq(tenantSettings.tenantId, tenantId), eq(tenantSettings.key, 'pos.requireShift')))
        .limit(1),
    );
    return flag?.value === true || flag?.value === 'true';
  }

  /** One call, one transaction: the sale, its stock, its journal, and every tender. */
  async checkout(tenantId: string, userId: string, input: PosCheckoutInput) {
    await this.ensureEnabled(tenantId);
    if (!input.lines.length) throw new DomainError('POS_CART_EMPTY', 'The cart is empty', 422);
    if (input.payment && input.payments?.length) {
      throw new DomainError('POS_PAYMENT_SHAPE_INVALID', 'Use payment or payments, not both', 422, { field: 'payments' });
    }
    const tenderInputs = input.payments?.length ? input.payments : input.payment ? [input.payment] : [];
    if (!tenderInputs.length) {
      throw new DomainError('POS_PAYMENT_REQUIRED', 'Choose at least one payment method', 422, { field: 'payments' });
    }
    for (const line of input.lines) {
      if (!line.itemId) throw new DomainError('POS_LINE_ITEM_REQUIRED', 'Every cart line needs an item', 422);
      if (!new Decimal(line.quantity).isFinite() || new Decimal(line.quantity).lte(0))
        throw new DomainError('POS_LINE_QUANTITY_INVALID', 'Cart quantities must be positive', 422);
      if (!new Decimal(line.unitPrice).isFinite() || new Decimal(line.unitPrice).lt(0))
        throw new DomainError('POS_LINE_PRICE_INVALID', 'Cart prices must be non-negative', 422);
    }

    // The engine recomputes and remains authoritative; this pre-check exists so a
    // short cash tender is refused before any invoice, stock or journal row exists.
    const priceIncludesVat = input.priceIncludesVat ?? true;
    const totals = calculateInvoiceTotals({
      lines: input.lines.map((line) => ({ ...line, taxRate: line.taxRate ?? '0' })),
      priceIncludesVat,
      invoiceDiscount: input.invoiceDiscount,
    });
    const split = Boolean(input.payments?.length);
    const payments = tenderInputs.map((payment) => {
      if (!['cash', 'card', 'bank', 'credit'].includes(payment.method)) {
        throw new DomainError('POS_PAYMENT_METHOD_INVALID', 'Unsupported payment method', 422, {
          field: 'payments',
        });
      }
      if (split && payment.amount === undefined) {
        throw new DomainError('POS_PAYMENT_AMOUNT_REQUIRED', 'Each split tender needs an amount', 422, {
          field: 'payments',
        });
      }
      const tenderValue = new Decimal(payment.amount ?? totals.total);
      if (!tenderValue.isFinite() || tenderValue.lte(0)) {
        throw new DomainError('POS_PAYMENT_AMOUNT_INVALID', 'Tender amount must be positive', 422, {
          field: 'payments',
        });
      }
      return { ...payment, amount: tenderValue };
    });
    const allocated = payments.reduce((sum, payment) => sum.plus(payment.amount), new Decimal(0));
    if (allocated.minus(totals.total).abs().gt('0.00005')) {
      throw new DomainError('POS_PAYMENT_TOTAL_MISMATCH', 'Tender amounts must equal the sale total', 422, {
        field: 'payments',
      });
    }
    if (payments.some((payment) => payment.method === 'credit') && !input.partyId) {
      throw new DomainError(
        'POS_CREDIT_CUSTOMER_REQUIRED',
        'A postponed sale needs a customer account',
        422,
        { field: 'partyId' },
      );
    }

    let tenderedTotal = new Decimal(0);
    let changeTotal = new Decimal(0);
    for (const payment of payments) {
      if (payment.tendered === undefined) continue;
      if (payment.method !== 'cash') {
        throw new DomainError('POS_TENDER_METHOD_INVALID', 'Tendered cash is valid only for cash payments', 422, {
          field: 'payments',
        });
      }
      const tendered = new Decimal(payment.tendered);
      if (!tendered.isFinite()) {
        throw new DomainError('POS_TENDER_INVALID', 'The tendered amount is not a number', 422, {
          field: 'payments',
        });
      }
      if (tendered.lt(payment.amount)) {
        throw new DomainError('POS_INSUFFICIENT_CASH', 'The tendered amount is less than the cash portion', 422, {
          field: 'payments',
        });
      }
      tenderedTotal = tenderedTotal.plus(tendered);
      changeTotal = changeTotal.plus(tendered.minus(payment.amount));
    }

    await this.assertPricePolicy(tenantId, input.lines);
    const targets = await Promise.all(
      payments.map(async (payment) => ({
        payment,
        target:
          payment.method === 'credit'
            ? undefined
            : await this.resolveTenderTarget(tenantId, input.branchId, {
                ...payment,
                amount: payment.amount.toFixed(4),
              }),
      })),
    );
    const shiftId = await this.resolveShiftId(
      tenantId,
      input.branchId,
      userId,
      input.shiftId,
      payments.some((payment) => payment.method === 'cash'),
    );

    const invoice = await this.sales.createAndPost(
      tenantId,
      {
        branchId: input.branchId,
        warehouseId: input.warehouseId,
        partyId: input.partyId,
        cashCustomerName: input.cashCustomerName ?? (input.partyId ? undefined : 'عميل نقدي'),
        cashCustomerMobile: input.cashCustomerMobile,
        kind: 'sale',
        priceIncludesVat,
        invoiceDiscount: input.invoiceDiscount,
        orderType: input.orderType ?? 'pos',
        shiftId,
        cashierId: userId,
        taxType: input.taxType ?? 'simplified',
        lines: input.lines.map((line) => ({
          itemId: line.itemId,
          unitId: line.unitId,
          lotId: line.lotId,
          serialIds: line.serialIds,
          description: line.description,
          quantity: line.quantity,
          unitPrice: line.unitPrice,
          taxRate: line.taxRate ?? '0',
          discountAmount: line.discountAmount ?? '0',
        })),
      },
      {
        settlements: targets.map(({ payment, target }) => ({
          method: payment.method,
          amount: payment.amount.toFixed(4),
          settlementAccountId: target?.accountId,
          cashLocationId: target?.cashLocationId,
          reference: payment.reference,
        })),
      },
    );

    return {
      data: {
        invoiceId: invoice.id,
        number: invoice.number,
        subtotal: invoice.subtotal,
        taxTotal: invoice.taxTotal,
        total: invoice.total,
        paidTotal: invoice.paidTotal,
        paymentStatus: invoice.paymentStatus,
        // Legacy fields remain available for a one-tender desktop bridge client.
        method: payments.length === 1 ? payments[0]!.method : 'split',
        cashLocationId: payments.length === 1 ? targets[0]?.target?.cashLocationId ?? null : null,
        shiftId: shiftId ?? null,
        change: changeTotal.toFixed(4),
        tendered: tenderedTotal.gt(0) ? tenderedTotal.toFixed(4) : null,
        payments: targets.map(({ payment, target }) => ({
          method: payment.method,
          amount: payment.amount.toFixed(4),
          cashLocationId: target?.cashLocationId ?? null,
          reference: payment.reference ?? null,
        })),
      },
    };
  }

  /**
   * A non-catalog price is a sensitive cashier override. The permission check uses
   * `permissionGrants`, not `includes`, so tenant owners and legacy aliases retain
   * their documented wildcard semantics.
   */
  private async assertPricePolicy(tenantId: string, lines: PosCheckoutLine[]) {
    const permissions = getTenantContext().permissions;
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      for (const line of lines) {
        const [item] = await tx
          .select({
            id: items.id,
            salePrice: items.salePrice,
            maxDiscountPct: items.maxDiscountPct,
            maxDiscountAmt: items.maxDiscountAmt,
            showInPos: items.showInPos,
          })
          .from(items)
          .where(and(eq(items.tenantId, tenantId), eq(items.id, line.itemId), isNull(items.deletedAt)));
        if (!item || !item.showInPos) {
          throw new DomainError('POS_ITEM_NOT_AVAILABLE', 'This item is not available at the POS', 422, {
            field: 'lines',
          });
        }
        let configured = item.salePrice === null ? undefined : new Decimal(item.salePrice);
        if (line.unitId) {
          const [unit] = await tx
            .select({ salePrice: itemUnits.salePrice })
            .from(itemUnits)
            .where(and(eq(itemUnits.itemId, item.id), eq(itemUnits.unitId, line.unitId)));
          if (!unit) {
            throw new DomainError('POS_ITEM_UNIT_INVALID', 'The selected unit is not configured for this item', 422, {
              field: 'lines',
            });
          }
          if (unit.salePrice !== null) configured = new Decimal(unit.salePrice);
        }
        // An item without a configured shelf price is intentionally priced by the
        // cashier (we still validate non-negative input above). Once a price exists,
        // any difference is an override and must have the dedicated permission.
        if (!configured || new Decimal(line.unitPrice).minus(configured).abs().lte('0.00005')) continue;
        if (!permissionGrants(permissions, 'pos.priceoverride')) {
          throw new DomainError('POS_PRICE_OVERRIDE_FORBIDDEN', 'Price override permission is required', 403, {
            field: 'lines',
          });
        }
        if (new Decimal(line.unitPrice).gte(configured)) continue;
        const reduction = configured.minus(line.unitPrice);
        const pct = configured.isZero() ? new Decimal(0) : reduction.div(configured).mul(100);
        if (item.maxDiscountAmt !== null && reduction.gt(item.maxDiscountAmt)) {
          throw new DomainError('POS_PRICE_OVERRIDE_LIMIT', 'Price reduction exceeds the item amount cap', 422, {
            field: 'lines',
          });
        }
        if (item.maxDiscountPct !== null && pct.gt(item.maxDiscountPct)) {
          throw new DomainError('POS_PRICE_OVERRIDE_LIMIT', 'Price reduction exceeds the item percentage cap', 422, {
            field: 'lines',
          });
        }
      }
    });
  }

  async openLines(tenantId: string, tableId: string): Promise<OpenLine[]> {
    const rows = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(orderEvents)
        .where(and(eq(orderEvents.tenantId, tenantId), eq(orderEvents.tableId, tableId)))
        .orderBy(orderEvents.createdAt),
    );
    const voided = new Set(rows.filter((row) => row.kind === 'void_item').map((row) => row.lineKey));
    return rows
      .filter((row) => row.kind === 'add_item' && !voided.has(row.lineKey) && !row.invoiceId)
      .map((row) => ({
        lineKey: row.lineKey,
        itemId: row.itemId,
        description: row.description,
        qty: row.qty,
        unitValue: row.unitValue,
        modifiers: row.modifiers,
      }));
  }
}

function today(): string {
  return new Date().toISOString().slice(0, 10);
}
function decoratedDescription(line: OpenLine): string {
  return JSON.stringify({ text: line.description ?? 'POS item', modifiers: line.modifiers });
}
