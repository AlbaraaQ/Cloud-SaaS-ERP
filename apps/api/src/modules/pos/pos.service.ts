import { Inject, Injectable } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { and, eq, isNull, sql } from 'drizzle-orm';
import { calculateInvoiceTotals, DomainError, newId } from '@erp/contracts';
import {
  cashLocations,
  diningTables,
  orderEvents,
  salesInvoices,
  shiftCloses,
  tableCategories,
  tenantSettings,
  warehouses,
  withTenantTx,
  type DatabaseHandle,
} from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';
import { SalesService } from '../sales/sales.service.js';
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
  cashLocationId?: string;
  settlementAccountId?: string;
  /** Cash handed over by the customer; the difference is returned as change. */
  tendered?: string;
  reference?: string;
};
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
  lines: PosCheckoutLine[];
  payment: PosCheckoutPayment;
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

  /** One call, one transaction: the sale, its stock, its journal, its money. */
  async checkout(tenantId: string, userId: string, input: PosCheckoutInput) {
    await this.ensureEnabled(tenantId);
    if (!input.lines.length) throw new DomainError('POS_CART_EMPTY', 'The cart is empty', 422);
    const method = input.payment.method;
    if (!['cash', 'card', 'bank', 'credit'].includes(method))
      throw new DomainError('POS_PAYMENT_METHOD_INVALID', 'Unsupported payment method', 422, {
        field: 'payment.method',
      });
    for (const line of input.lines) {
      if (!line.itemId) throw new DomainError('POS_LINE_ITEM_REQUIRED', 'Every cart line needs an item', 422);
      if (!new Decimal(line.quantity).isFinite() || new Decimal(line.quantity).lte(0))
        throw new DomainError('POS_LINE_QUANTITY_INVALID', 'Cart quantities must be positive', 422);
    }
    if (method === 'credit' && !input.partyId)
      throw new DomainError(
        'POS_CREDIT_CUSTOMER_REQUIRED',
        'A postponed sale needs a customer account',
        422,
        { field: 'partyId' },
      );

    // The engine recomputes and remains authoritative; this pre-check exists so a
    // short tender can be refused *before* anything is written.
    const priceIncludesVat = input.priceIncludesVat ?? true;
    const totals = calculateInvoiceTotals({
      lines: input.lines.map((line) => ({ ...line, taxRate: line.taxRate ?? '0' })),
      priceIncludesVat,
      invoiceDiscount: input.invoiceDiscount,
    });
    const tendered = input.payment.tendered ? new Decimal(input.payment.tendered) : undefined;
    if (tendered && !tendered.isFinite())
      throw new DomainError('POS_TENDER_INVALID', 'The tendered amount is not a number', 422, {
        field: 'payment.tendered',
      });
    if (tendered && tendered.lt(totals.total))
      throw new DomainError('POS_INSUFFICIENT_CASH', 'The tendered amount is less than the sale total', 422, {
        field: 'payment.tendered',
      });
    const change = tendered ? tendered.minus(totals.total).toFixed(4) : '0.0000';

    const target =
      method === 'credit'
        ? undefined
        : await this.resolveTenderTarget(tenantId, input.branchId, input.payment);
    const shiftId = await this.resolveShiftId(
      tenantId,
      input.branchId,
      userId,
      input.shiftId,
      method === 'cash',
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
        lines: input.lines.map((line) => ({
          itemId: line.itemId,
          description: line.description,
          quantity: line.quantity,
          unitPrice: line.unitPrice,
          taxRate: line.taxRate ?? '0',
          discountAmount: line.discountAmount ?? '0',
        })),
      },
      {
        settlement: method === 'credit' ? 'credit' : method,
        settlementAccountId: target?.accountId,
        settlementCashLocationId: target?.cashLocationId,
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
        method,
        cashLocationId: target?.cashLocationId ?? null,
        shiftId: shiftId ?? null,
        change,
        tendered: tendered ? tendered.toFixed(4) : null,
      },
    };
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
