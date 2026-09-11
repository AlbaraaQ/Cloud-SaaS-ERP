import { Inject, Injectable } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { and, desc, eq, ilike, inArray, isNotNull, sql } from 'drizzle-orm';
import { calculateInvoiceTotals, DomainError, newId } from '@erp/contracts';
import {
  accounts,
  inventoryTransactions,
  invoicePayments,
  items,
  journalEntries,
  journalEntryLines,
  offers,
  salesAdjustmentNotes,
  salesInvoiceLines,
  salesInvoices,
  salesmen,
  shiftCloses,
  stockBalances,
  withTenantTx,
  type DatabaseHandle,
  type DrizzleTx,
} from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';
import { AccountingService } from '../accounting/accounting.service.js';
import { InventoryService, type InventoryLine } from '../inventory/inventory.service.js';
import { PostingProfilesService } from '../organization/posting-profiles/posting-profiles.service.js';
import { tryGetAuthContext } from '../platform/context/tenant-context.js';
import { SequencesService } from '../platform-services/index.js';

export type SalesLineInput = {
  itemId?: string;
  description?: string;
  quantity: string;
  unitPrice: string;
  discountRate?: string;
  discountAmount?: string;
  taxRate?: string;
  taxGroupId?: string;
};
export type SalesInvoiceInput = {
  branchId: string;
  warehouseId?: string;
  partyId?: string;
  salesmanId?: string;
  referenceInvoiceId?: string;
  validUntil?: string;
  kind?: 'sale' | 'sale_return' | 'credit_note' | 'debit_note' | 'quotation';
  currency?: string;
  priceIncludesVat?: boolean;
  invoiceDiscount?: string;
  extraTax?: string;
  withholding?: string;
  lines: SalesLineInput[];
  cashCustomerName?: string;
  cashCustomerMobile?: string;
  orderType?: string;
  shiftId?: string;
};
export type PaymentInput = {
  method: 'cash' | 'card' | 'bank' | 'credit' | 'split';
  amount: string;
  idempotencyKey: string;
  cashLocationId?: string;
  reference?: string;
};
export type PostingInput = {
  fiscalPeriodId?: string;
  journalLines?: {
    accountId: string;
    debit?: string;
    credit?: string;
    partyId?: string;
    description?: string;
  }[];
  inventoryLines?: InventoryLine[];
  settlement?: 'credit' | 'cash' | 'card' | 'bank';
  settlementAccountId?: string;
  settlementCashLocationId?: string;
};

const money = (value: string) => new Decimal(value);

@Injectable()
export class SalesService {
  constructor(
    @Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle,
    private readonly inventory: InventoryService,
    private readonly accounting: AccountingService,
    private readonly sequences: SequencesService,
    private readonly profiles: PostingProfilesService,
  ) {}

  async list(tenantId: string) {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(salesInvoices)
        .where(eq(salesInvoices.tenantId, tenantId))
        .orderBy(desc(salesInvoices.createdAt))
        .limit(100),
    );
  }

  /** Reads the invoice, its lines and its payments inside an open transaction. */
  private async getInTx(tx: DrizzleTx, tenantId: string, id: string) {
    const [invoice] = await tx
      .select()
      .from(salesInvoices)
      .where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, id)));
    if (!invoice) throw new DomainError('SALES_INVOICE_NOT_FOUND', 'Sales invoice was not found', 404);
    const lines = await tx
      .select()
      .from(salesInvoiceLines)
      .where(and(eq(salesInvoiceLines.tenantId, tenantId), eq(salesInvoiceLines.invoiceId, id)));
    const payments = await tx
      .select()
      .from(invoicePayments)
      .where(and(eq(invoicePayments.tenantId, tenantId), eq(invoicePayments.invoiceId, id)));
    return { ...invoice, lines, payments };
  }

  async get(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, (tx) => this.getInTx(tx, tenantId, id));
  }

  /**
   * Creates the invoice and its lines inside an open transaction, returning its id.
   *
   * Split out of `create()` for Phase 04 so a till checkout can create *and* post
   * in one transaction: a sale that exists without its journal and its stock
   * movement is the one thing a cashier must never be able to produce, and three
   * separate round trips from a browser can always leave exactly that behind.
   */
  private async createInTx(tx: DrizzleTx, tenantId: string, input: SalesInvoiceInput): Promise<string> {
    if (!input.lines.length)
      throw new DomainError('SALES_LINES_REQUIRED', 'At least one invoice line is required', 422);
    if (!input.partyId && !input.cashCustomerName)
      throw new DomainError('SALES_CUSTOMER_REQUIRED', 'Party or cash customer name is required', 422);
    const totals = calculateInvoiceTotals({
      lines: input.lines,
      priceIncludesVat: input.priceIncludesVat,
      invoiceDiscount: input.invoiceDiscount,
      extraTax: input.extraTax,
      withholding: input.withholding,
    });
    const id = newId();
    await tx
      .insert(salesInvoices)
      .values({
        id,
        tenantId,
        branchId: input.branchId,
        warehouseId: input.warehouseId,
        referenceInvoiceId: input.referenceInvoiceId,
        partyId: input.partyId,
        salesmanId: input.salesmanId,
        kind: input.kind ?? 'sale',
        validUntil: input.validUntil,
        currency: input.currency ?? 'SAR',
        priceIncludesVat: input.priceIncludesVat ?? false,
        cashCustomerName: input.cashCustomerName,
        cashCustomerMobile: input.cashCustomerMobile,
        orderType: input.orderType,
        shiftId: input.shiftId ?? null,
        createdBy: tryGetAuthContext()?.userId,
        invoiceDiscount: totals.discount,
        extraTax: totals.extraTax,
        withholding: totals.withholding,
        subtotal: totals.subtotal,
        taxTotal: totals.tax,
        total: totals.total,
        status: 'draft',
      });
    await tx.insert(salesInvoiceLines).values(
      input.lines.map((line, index) => {
        const calculated = totals.lines[index];
        if (!calculated)
          throw new DomainError('SALES_TOTALS_INVALID', 'Invoice totals do not match invoice lines', 422);
        return {
          id: newId(),
          tenantId,
          invoiceId: id,
          lineNo: index + 1,
          itemId: line.itemId,
          description: line.description,
          quantity: line.quantity,
          unitPrice: line.unitPrice,
          discountRate: line.discountRate ?? '0',
          discountAmount: calculated.discount,
          taxGroupId: line.taxGroupId,
          taxRate: line.taxRate ?? '0',
          net: calculated.net,
          tax: calculated.tax,
          total: calculated.total,
        };
      }),
    );
    return id;
  }

  async create(tenantId: string, input: SalesInvoiceInput) {
    const id = await withTenantTx(this.database.db, tenantId, (tx) => this.createInTx(tx, tenantId, input));
    return this.get(tenantId, id);
  }

  /**
   * Create and post in one transaction — the POS checkout path (Phase 04).
   *
   * Identical gates and identical ledgers as `create` then `post`, but nothing is
   * committed until the invoice, its stock movement, its journal and its
   * settlement payment have all succeeded.
   */
  async createAndPost(tenantId: string, input: SalesInvoiceInput, posting: PostingInput = {}) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const id = await this.createInTx(tx, tenantId, input);
      const posted = await this.postInTx(tx, tenantId, id, posting);
      return posted ?? this.getInTx(tx, tenantId, id);
    });
  }

  async updateDraft(tenantId: string, id: string, input: Partial<SalesInvoiceInput>) {
    const invoice = await this.get(tenantId, id);
    if (invoice.status !== 'draft')
      throw new DomainError('SALES_INVOICE_IMMUTABLE', 'Only draft invoices can be changed', 409);
    if (invoice.shiftId)
      await withTenantTx(this.database.db, tenantId, (tx) => this.assertShiftOpen(tx, tenantId, invoice));
    if (input.lines) {
      await withTenantTx(this.database.db, tenantId, async (tx) => {
        await tx
          .delete(salesInvoiceLines)
          .where(and(eq(salesInvoiceLines.tenantId, tenantId), eq(salesInvoiceLines.invoiceId, id)));
      });
      const replacement = await this.create(tenantId, {
        ...input,
        branchId: input.branchId ?? invoice.branchId,
        lines: input.lines,
      } as SalesInvoiceInput);
      return replacement;
    }
    await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .update(salesInvoices)
        .set({
          partyId: input.partyId,
          salesmanId: input.salesmanId,
          warehouseId: input.warehouseId,
          cashCustomerName: input.cashCustomerName,
          cashCustomerMobile: input.cashCustomerMobile,
          updatedAt: new Date(),
        })
        .where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, id))),
    );
    return this.get(tenantId, id);
  }

  async post(tenantId: string, id: string, posting: PostingInput = {}) {
    // Fail fast, before a transaction is even opened: an explicit-journal posting
    // that names no period can never be written, and the gate must fire before any
    // inventory side effect (sales.service.spec).
    if (posting.journalLines?.length && !posting.fiscalPeriodId)
      throw new DomainError(
        'SALES_FISCAL_PERIOD_REQUIRED',
        'A fiscal period is required for accounting posting',
        422,
      );
    const posted = await withTenantTx(this.database.db, tenantId, (tx) =>
      this.postInTx(tx, tenantId, id, posting),
    );
    return posted ?? this.get(tenantId, id);
  }

  /**
   * Desktop `frmPOS.DeleteInv` — "نأسف! لا يمكن حذف فاتورة بعد إغلاق اليومية".
   *
   * A sale captured by a cashier shift was counted into that shift's closing
   * cash. Editing or voiding it afterwards would change the drawer without
   * changing the report that declared it, so the shift must still be open.
   * Invoices with no shift (back-office sales) are unaffected.
   */
  private async assertShiftOpen(
    tx: DrizzleTx,
    tenantId: string,
    invoice: { shiftId?: string | null },
  ): Promise<void> {
    if (!invoice.shiftId) return;
    const [shift] = await tx
      .select({ status: shiftCloses.status })
      .from(shiftCloses)
      .where(and(eq(shiftCloses.tenantId, tenantId), eq(shiftCloses.id, invoice.shiftId)));
    if (!shift || shift.status !== 'open') {
      throw new DomainError(
        'SALES_SHIFT_CLOSED',
        'This sale belongs to a closed cashier shift — issue a return instead',
        409,
      );
    }
  }

  /**
   * The whole posting engine inside an open transaction: gates, numbering, stock,
   * journal and settlement. Returns `undefined` only when a concurrent caller
   * already posted the invoice, so `post()` re-reads the committed row.
   */
  private async postInTx(tx: DrizzleTx, tenantId: string, id: string, posting: PostingInput = {}) {
    const invoice = await this.getInTx(tx, tenantId, id);
    if (invoice.status === 'posted') return invoice;
    if (invoice.status !== 'draft')
      throw new DomainError('SALES_INVOICE_INVALID_STATUS', 'Only draft invoices can be posted', 409);
    if (invoice.kind === 'quotation')
      throw new DomainError(
        'SALES_QUOTATION_NOT_POSTABLE',
        'A quotation is converted into an invoice, never posted',
        409,
      );
    // Desktop `SaveInvoice` gates, enforced at posting (drafts may stay incomplete).
    // Service-only invoices (progress bills, retentions) carry no stock, so the
    // warehouse gate applies only when stocked lines are present; callers that hand
    // us no lines (unit mocks) stay on the conservative path and must pass one.
    const stockedLines = (invoice.lines ?? []) as Array<{ itemId?: string | null; quantity?: string }>;
    const candidateIds = stockedLines
      .filter((line) => line.itemId && money(line.quantity ?? '0').gt(0))
      .map((line) => line.itemId!);
    // Unit mocks hand us no lines at all — stay conservative and require a warehouse.
    let movesStock = invoice.lines === undefined || candidateIds.length > 0;
    if (movesStock && invoice.lines !== undefined && candidateIds.length > 0 && !invoice.warehouseId) {
      // Service-only invoices carry item rows too; check their kinds before gating.
      const kinds = await tx
        .select({ kind: items.kind })
        .from(items)
        .where(and(eq(items.tenantId, tenantId), inArray(items.id, candidateIds)));
      movesStock = kinds.some((row) => row.kind === 'stock');
    }
    if ((invoice.kind === 'sale' || invoice.kind === 'sale_return') && movesStock && !invoice.warehouseId) {
      throw new DomainError(
        'SALES_WAREHOUSE_REQUIRED',
        'A warehouse is required to post a stock-moving invoice',
        422,
      );
    }
    if ((invoice.kind === 'sale' || invoice.kind === 'debit_note') && money(invoice.total).lt(0)) {
      throw new DomainError('SALES_TOTAL_INVALID', 'A sales total cannot be negative', 422);
    }
    if (!invoice.partyId && !invoice.cashCustomerName)
      throw new DomainError('SALES_CUSTOMER_REQUIRED', 'Party or cash customer name is required', 422);
    if (posting.journalLines?.length && !posting.fiscalPeriodId)
      throw new DomainError(
        'SALES_FISCAL_PERIOD_REQUIRED',
        'A fiscal period is required for accounting posting',
        422,
      );

    const [locked] = await tx
      .select()
      .from(salesInvoices)
      .where(
        and(
          eq(salesInvoices.tenantId, tenantId),
          eq(salesInvoices.id, id),
          eq(salesInvoices.status, 'draft'),
        ),
      );
    if (!locked)
      throw new DomainError('SALES_INVOICE_INVALID_STATUS', 'Only draft invoices can be posted', 409);
    const prefix =
      locked.kind === 'sale_return'
        ? 'SR-'
        : locked.kind === 'credit_note'
          ? 'CN-'
          : locked.kind === 'debit_note'
            ? 'DN-'
            : 'SI-';
    const allocated = await this.sequences.next(
      { tenantId, branchId: locked.branchId, docType: locked.kind },
      tx,
      { prefix, padding: 6 },
    );
    const number = allocated.display;
    const today = new Date().toISOString().slice(0, 10);

    if (posting.inventoryLines?.length) {
      await this.inventory.recordInTx(
        tx,
        tenantId,
        posting.inventoryLines.map((line) => ({
          ...line,
          docType: line.docType || 'sales_invoice',
          docId: id,
        })),
      );
    } else if (locked.warehouseId && (locked.kind === 'sale' || locked.kind === 'sale_return')) {
      await this.recordAutoStock(tx, tenantId, locked);
    }

    if (posting.journalLines?.length) {
      await this.accounting.postJournalInTx(tx, tenantId, {
        branchId: locked.branchId,
        fiscalPeriodId: posting.fiscalPeriodId!,
        date: today,
        description: `Sales invoice ${number}`,
        lines: posting.journalLines,
        sourceType: 'sales_invoice',
        sourceId: id,
      });
    } else if (money(locked.total).abs().gt(0)) {
      const docType =
        locked.kind === 'sale_return'
          ? 'sales_return'
          : locked.kind === 'credit_note'
            ? 'credit_note'
            : locked.kind === 'debit_note'
              ? 'debit_note'
              : 'sales_invoice';
      const profile = await this.profiles.resolvePostProfileInTx(tx, tenantId, locked.branchId, docType);
      const fiscalPeriodId =
        posting.fiscalPeriodId ?? (await this.accounting.openPeriodForDateInTx(tx, tenantId, today));
      const costRows = await tx
        .select({ costTotal: salesInvoiceLines.costTotal })
        .from(salesInvoiceLines)
        .where(and(eq(salesInvoiceLines.tenantId, tenantId), eq(salesInvoiceLines.invoiceId, id)));
      const cogsTotal = costRows.reduce((sum, row) => sum.plus(row.costTotal ?? '0'), new Decimal(0));
      const mapping = profile.mapping as unknown as Record<string, string | null | undefined>;
      if (
        !posting.journalLines?.length &&
        posting.settlement !== undefined &&
        posting.settlement !== 'credit' &&
        posting.settlementAccountId
      ) {
        const [settlementAccount] = await tx
          .select({ id: accounts.id })
          .from(accounts)
          .where(and(eq(accounts.tenantId, tenantId), eq(accounts.id, posting.settlementAccountId)));
        if (!settlementAccount)
          throw new DomainError(
            'SALES_SETTLEMENT_ACCOUNT_INVALID',
            'The settlement account does not belong to this tenant',
            422,
            { field: 'settlementAccountId' },
          );
      }
      const lines = this.buildAutoJournal(
        locked,
        mapping,
        cogsTotal,
        posting.settlement ?? 'credit',
        posting.settlementAccountId,
      );
      await this.accounting.postJournalInTx(tx, tenantId, {
        branchId: locked.branchId,
        fiscalPeriodId,
        date: today,
        description: `Sales invoice ${number}`,
        lines,
        sourceType: 'sales_invoice',
        sourceId: id,
        idempotencyKey: `sales-post:${id}`,
      });
    }
    // Immediate settlement (cash/bank) is recorded as the invoice's first payment
    // in the same transaction, so a cash sale lands fully paid with a payment
    // row the portal and the statements can see — not just a flipped flag.
    const isReturnKind = locked.kind === 'sale_return' || locked.kind === 'credit_note';
    const settled =
      !posting.journalLines?.length &&
      !isReturnKind &&
      posting.settlement !== undefined &&
      posting.settlement !== 'credit' &&
      money(locked.total).gt(0);
    if (settled) {
      await tx.insert(invoicePayments).values({
        id: newId(),
        tenantId,
        invoiceId: id,
        method: posting.settlement!,
        amount: locked.total,
        cashLocationId: posting.settlementCashLocationId ?? null,
        reference: number,
        idempotencyKey: `sales-settle:${id}`,
      });
    }
    const paymentStatus = locked.total === '0' || settled ? 'paid' : 'unpaid';
    await tx
      .update(salesInvoices)
      .set({
        status: 'posted',
        number,
        postedAt: new Date(),
        paidTotal: settled ? locked.total : locked.paidTotal,
        paymentStatus,
      })
      .where(
        and(
          eq(salesInvoices.tenantId, tenantId),
          eq(salesInvoices.id, id),
          eq(salesInvoices.status, 'draft'),
        ),
      );
    return this.getInTx(tx, tenantId, id);
  }

  /**
   * Relieves (sale) or restores (return) stock for every stocked line and stamps
   * each line's `cost_total` from the movement's average cost — the desktop
   * `SumCost` behaviour. Returns restore at the source invoice's original cost
   * (`returnAtOriginalCost`); when the source cost is unknown the current average
   * is used so the return stays value-neutral instead of corrupting the average.
   */
  private async recordAutoStock(
    tx: DrizzleTx,
    tenantId: string,
    locked: { id: string; kind: string; warehouseId: string | null; referenceInvoiceId: string | null },
  ): Promise<void> {
    const lines = await tx
      .select()
      .from(salesInvoiceLines)
      .where(and(eq(salesInvoiceLines.tenantId, tenantId), eq(salesInvoiceLines.invoiceId, locked.id)));
    const candidates = lines.filter((line) => line.itemId && money(line.quantity).gt(0));
    if (!candidates.length || !locked.warehouseId) return;
    // Services never touch the stock ledger — only `stock`-kind items relieve /
    // restore and stamp costs.
    const itemRows = await tx
      .select({ id: items.id, kind: items.kind })
      .from(items)
      .where(
        and(
          eq(items.tenantId, tenantId),
          inArray(
            items.id,
            candidates.map((line) => line.itemId!),
          ),
        ),
      );
    const stockable = new Set(itemRows.filter((row) => row.kind === 'stock').map((row) => row.id));
    const stocked = candidates.filter((line) => stockable.has(line.itemId!));
    if (!stocked.length) return;

    const sourceCost = new Map<string, Decimal>();
    if (locked.kind === 'sale_return' && locked.referenceInvoiceId) {
      const sourceLines = await tx
        .select()
        .from(salesInvoiceLines)
        .where(
          and(
            eq(salesInvoiceLines.tenantId, tenantId),
            eq(salesInvoiceLines.invoiceId, locked.referenceInvoiceId),
          ),
        );
      const costByItem = new Map<string, Decimal>();
      const qty = new Map<string, Decimal>();
      for (const line of sourceLines) {
        if (!line.itemId) continue;
        costByItem.set(line.itemId, (costByItem.get(line.itemId) ?? new Decimal(0)).plus(line.costTotal ?? '0'));
        qty.set(line.itemId, (qty.get(line.itemId) ?? new Decimal(0)).plus(line.quantity));
      }
      for (const [itemId, costTotal] of costByItem) {
        const totalQty = qty.get(itemId) ?? new Decimal(0);
        if (totalQty.gt(0)) sourceCost.set(itemId, costTotal.div(totalQty));
      }
    }

    const movements: InventoryLine[] = [];
    for (const line of stocked) {
      if (locked.kind === 'sale_return') {
        let unitCost = sourceCost.get(line.itemId!);
        if (!unitCost || unitCost.lte(0)) {
          const [stockBalance] = await tx
            .select({ averageCost: stockBalances.averageCost })
            .from(stockBalances)
            .where(
              and(
                eq(stockBalances.tenantId, tenantId),
                eq(stockBalances.itemId, line.itemId!),
                eq(stockBalances.warehouseId, locked.warehouseId),
              ),
            );
          unitCost = money(stockBalance?.averageCost ?? '0');
        }
        movements.push({
          itemId: line.itemId!,
          warehouseId: locked.warehouseId,
          qty: line.quantity,
          unitCost: unitCost.toFixed(4),
          direction: 'in',
          docType: 'sales_return',
          docId: locked.id,
          lineId: line.id,
          costing: 'returnAtOriginalCost',
        });
      } else {
        movements.push({
          itemId: line.itemId!,
          warehouseId: locked.warehouseId,
          qty: line.quantity,
          direction: 'out',
          docType: 'sales_invoice',
          docId: locked.id,
          lineId: line.id,
          costing: 'outAtAvg',
        });
      }
    }
    await this.inventory.recordInTx(tx, tenantId, movements);

    const txns = await tx
      .select({ lineId: inventoryTransactions.lineId, unitCost: inventoryTransactions.unitCost })
      .from(inventoryTransactions)
      .where(and(eq(inventoryTransactions.tenantId, tenantId), eq(inventoryTransactions.docId, locked.id)));
    for (const txn of txns) {
      if (!txn.lineId) continue;
      const line = stocked.find((candidate) => candidate.id === txn.lineId);
      if (!line) continue;
      const costTotal = money(line.quantity)
        .mul(txn.unitCost ?? '0')
        .toFixed(4);
      await tx
        .update(salesInvoiceLines)
        .set({ costTotal })
        .where(and(eq(salesInvoiceLines.tenantId, tenantId), eq(salesInvoiceLines.id, line.id)));
    }
  }

  /**
   * The desktop `BindToEntry` journal for sales (Sale/ProcType 1), mirrored for
   * returns and notes:
   *
   * | sale / debit note              | sale_return / credit note          |
   * |--------------------------------|----------------------------------|
   * | Dr receivable — total          | Cr receivable — total              |
   * | Cr sales — gross of discount   | Dr sales return — gross            |
   * | Dr discount given — discount   | Cr discount given — discount       |
   * | Cr VAT output — VAT            | Dr VAT output — VAT                |
   * | Cr excise — extra tax          | Dr excise — extra tax              |
   * | Dr COGS / Cr inventory — cost  | Dr inventory / Cr COGS — cost      |
   *
   * Sales is credited gross of the header discount with a separate discount leg —
   * exactly as the desktop credits `Net − VAT + TotDiscount` and debits 4100003.
   * Line discounts stay netted inside the sales leg (the cloud persists line nets,
   * not grosses). Withholding has no desktop account, so an invoice carrying it
   * must be posted with explicit journal lines.
   */
  private buildAutoJournal(
    locked: {
      kind: string;
      subtotal: string;
      invoiceDiscount: string | null;
      taxTotal: string;
      extraTax: string | null;
      withholding: string | null;
      total: string;
      partyId: string | null;
    },
    mapping: Record<string, string | null | undefined>,
    cogsTotal: Decimal,
    settlement: 'credit' | 'cash' | 'card' | 'bank',
    settlementAccountId?: string,
  ): { accountId: string; debit?: string; credit?: string; partyId?: string }[] {
    const need = (key: string): string => {
      const accountId = mapping[key];
      if (!accountId)
        throw new DomainError('SALES_PROFILE_KEY_MISSING', `Posting profile has no ${key}`, 422, {
          field: key,
        });
      return accountId;
    };
    // Cash and bank sales debit the till/bank account instead of the receivable,
    // exactly like the desktop's payment-method choice at save time. The account
    // must come from a real cash location — never from an unvalidated mapping.
    const settlementAccount =
      settlement === 'credit'
        ? need('receivableAccountId')
        : (settlementAccountId ??
          (() => {
            throw new DomainError(
              'SALES_SETTLEMENT_ACCOUNT_REQUIRED',
              'A cash or bank account is required for immediate settlement',
              422,
              { field: 'settlementAccountId' },
            );
          })());
    if (
      money(locked.withholding ?? '0')
        .abs()
        .gt(0)
    ) {
      throw new DomainError(
        'SALES_WITHHOLDING_MANUAL_POSTING',
        'Invoices with withholding need explicit journal lines',
        422,
      );
    }
    const discount = money(locked.invoiceDiscount ?? '0');
    const extra = money(locked.extraTax ?? '0');
    const tax = money(locked.taxTotal);
    const documentTotal = money(locked.total);
    const gross = money(locked.subtotal).plus(discount);
    const isReturn = locked.kind === 'sale_return' || locked.kind === 'credit_note';
    const lines: { accountId: string; debit?: string; credit?: string; partyId?: string }[] = [];
    const leg = (
      accountId: string,
      debit: Decimal,
      credit: Decimal,
      partyId: string | null = locked.partyId,
    ): void => {
      if (debit.abs().lte(0) && credit.abs().lte(0)) return;
      lines.push({
        accountId,
        debit: debit.toFixed(4),
        credit: credit.toFixed(4),
        partyId: partyId ?? undefined,
      });
    };
    // The till/bank leg carries no party subledger — cash has no customer account.
    const settlementParty = settlement === 'credit' ? locked.partyId : null;

    if (!isReturn) {
      leg(settlementAccount, documentTotal, new Decimal(0), settlementParty);
      leg(need('salesAccountId'), new Decimal(0), gross);
      if (discount.gt(0)) leg(need('discountGivenAccountId'), discount, new Decimal(0));
      if (tax.abs().gt(0)) leg(need('vatOutputAccountId'), new Decimal(0), tax);
      if (extra.abs().gt(0)) leg(need('exciseTaxAccountId'), new Decimal(0), extra);
    } else {
      leg(settlementAccount, new Decimal(0), documentTotal, settlementParty);
      leg(need('salesReturnAccountId'), gross, new Decimal(0));
      if (discount.gt(0)) leg(need('discountGivenAccountId'), new Decimal(0), discount);
      if (tax.abs().gt(0)) leg(need('vatOutputAccountId'), tax, new Decimal(0));
      if (extra.abs().gt(0)) leg(need('exciseTaxAccountId'), extra, new Decimal(0));
    }
    if (cogsTotal.abs().gt(0)) {
      if (!isReturn) {
        leg(need('cogsAccountId'), cogsTotal, new Decimal(0));
        leg(need('inventoryAccountId'), new Decimal(0), cogsTotal);
      } else {
        leg(need('inventoryAccountId'), cogsTotal, new Decimal(0));
        leg(need('cogsAccountId'), new Decimal(0), cogsTotal);
      }
    }
    return lines;
  }

  async void(tenantId: string, id: string, reason: string) {
    if (!reason.trim()) throw new DomainError('SALES_VOID_REASON_REQUIRED', 'A void reason is required', 422);
    const invoice = await this.get(tenantId, id);
    if (invoice.status !== 'posted')
      throw new DomainError('SALES_INVOICE_INVALID_STATUS', 'Only posted invoices can be voided', 409);
    if (
      invoice.zatcaStatus === 'cleared' ||
      invoice.zatcaStatus === 'reported' ||
      invoice.zatcaStatus === 'signed'
    ) {
      throw new DomainError(
        'SALES_VOID_ZATCA_SEALED',
        'A ZATCA-sealed invoice cannot be voided — issue a credit note',
        409,
      );
    }
    if (money(invoice.paidTotal).abs().gt(0)) {
      throw new DomainError(
        'SALES_VOID_HAS_PAYMENTS',
        'Refund or unallocate the payments before voiding',
        409,
      );
    }
    // The desktop only flags the invoice and its entry as deleted and leaves the
    // stock relieved. The cloud reverses all three legs — journal, stock and status —
    // in one transaction, so voiding can never silently unbalance the ledger.
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      await this.assertShiftOpen(tx, tenantId, invoice);
      const today = new Date().toISOString().slice(0, 10);
      const [entry] = await tx
        .select({ id: journalEntries.id })
        .from(journalEntries)
        .where(
          and(
            eq(journalEntries.tenantId, tenantId),
            eq(journalEntries.sourceType, 'sales_invoice'),
            eq(journalEntries.sourceId, id),
          ),
        );
      if (entry) {
        const [existing] = await tx
          .select({ id: journalEntries.id })
          .from(journalEntries)
          .where(eq(journalEntries.reversalOf, entry.id));
        if (!existing) {
          const fiscalPeriodId = await this.accounting.openPeriodForDateInTx(tx, tenantId, today);
          const entryLines = await tx
            .select()
            .from(journalEntryLines)
            .where(eq(journalEntryLines.entryId, entry.id));
          const reversalId = newId();
          await tx.insert(journalEntries).values({
            id: reversalId,
            tenantId,
            branchId: invoice.branchId,
            fiscalPeriodId,
            date: today,
            kind: 'reversal',
            status: 'posted',
            description: `Void ${invoice.number ?? id}: ${reason}`,
            reversalOf: entry.id,
            sourceType: 'sales_invoice',
            sourceId: id,
            postedAt: new Date(),
          });
          await tx.insert(journalEntryLines).values(
            entryLines.map((line) => ({
              entryId: reversalId,
              lineNo: line.lineNo,
              tenantId,
              accountId: line.accountId,
              debit: line.credit,
              credit: line.debit,
              partyId: line.partyId,
              description: line.description,
            })),
          );
          await tx
            .update(journalEntries)
            .set({ status: 'void', updatedAt: new Date() })
            .where(eq(journalEntries.id, entry.id));
        }
      }

      const movements = await tx
        .select()
        .from(inventoryTransactions)
        .where(and(eq(inventoryTransactions.tenantId, tenantId), eq(inventoryTransactions.docId, id)));
      const mirrors: InventoryLine[] = movements.map((movement) => ({
        itemId: movement.itemId,
        warehouseId: movement.warehouseId,
        qty: movement.qty,
        unitCost: movement.unitCost ?? '0',
        direction: movement.direction === 'out' ? 'in' : 'out',
        docType: 'sales_void',
        docId: id,
        lineId: movement.lineId ?? undefined,
        serialId: movement.serialId ?? undefined,
        costing: movement.direction === 'out' ? 'returnAtOriginalCost' : 'outAtAvg',
      }));
      if (mirrors.length) await this.inventory.recordInTx(tx, tenantId, mirrors);

      await tx
        .update(salesInvoices)
        .set({
          status: 'voided',
          voidedAt: new Date(),
          updatedAt: new Date(),
          zatcaStatus: `voided:${reason}`,
        })
        .where(
          and(
            eq(salesInvoices.tenantId, tenantId),
            eq(salesInvoices.id, id),
            eq(salesInvoices.status, 'posted'),
          ),
        );
    });
    return this.get(tenantId, id);
  }

  async addPayment(tenantId: string, invoiceId: string, input: PaymentInput) {
    const invoice = await this.get(tenantId, invoiceId);
    if (invoice.status !== 'posted')
      throw new DomainError('SALES_INVOICE_NOT_POSTED', 'Payments require a posted invoice', 409);
    const paymentValue = money(input.amount);
    if (!paymentValue.isFinite() || paymentValue.lte(0))
      throw new DomainError('PAYMENT_AMOUNT_INVALID', 'Payment amount must be positive', 422);
    if (
      input.method !== 'credit' &&
      ['cash', 'card', 'bank', 'split'].includes(input.method) &&
      input.method === 'cash' &&
      !input.cashLocationId
    )
      throw new DomainError('CASH_LOCATION_REQUIRED', 'Cash payments require a cash location', 422);
    const result = await withTenantTx(this.database.db, tenantId, async (tx) => {
      const [existing] = await tx
        .select()
        .from(invoicePayments)
        .where(
          and(
            eq(invoicePayments.tenantId, tenantId),
            eq(invoicePayments.idempotencyKey, input.idempotencyKey),
          ),
        );
      if (existing) return existing;
      const paidValue = money(invoice.paidTotal).plus(paymentValue);
      if (paidValue.gt(money(invoice.total).plus('0.0001')))
        throw new DomainError('PAYMENT_EXCEEDS_DUE', 'Payment exceeds invoice balance', 422);
      const [payment] = await tx
        .insert(invoicePayments)
        .values({
          id: newId(),
          tenantId,
          invoiceId,
          method: input.method,
          amount: input.amount,
          cashLocationId: input.cashLocationId,
          reference: input.reference,
          idempotencyKey: input.idempotencyKey,
        })
        .returning();
      await tx
        .update(salesInvoices)
        .set({
          paidTotal: paidValue.toFixed(4),
          paymentStatus: paidValue.gte(money(invoice.total)) ? 'paid' : 'partial',
          updatedAt: new Date(),
        })
        .where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, invoiceId)));
      return payment;
    });
    return result;
  }

  async returnFrom(tenantId: string, sourceId: string, input: Omit<SalesInvoiceInput, 'kind'>) {
    const source = await this.get(tenantId, sourceId);
    if (source.status !== 'posted')
      throw new DomainError('SALES_RETURN_SOURCE_INVALID', 'Returns require a posted source invoice', 409);
    if (source.kind === 'sale_return')
      throw new DomainError('SALES_RETURN_SOURCE_INVALID', 'A return cannot reference another return', 422);

    const sourceQuantities = new Map<string, Decimal>();
    const requested = new Map<string, Decimal>();
    for (const line of input.lines) {
      if (!line.itemId)
        throw new DomainError('SALES_RETURN_ITEM_REQUIRED', 'Return lines must reference an item', 422);
      const quantity = money(line.quantity);
      if (!quantity.isFinite() || quantity.lte(0))
        throw new DomainError('SALES_RETURN_QUANTITY_INVALID', 'Return quantities must be positive', 422);
      sourceQuantities.set(line.itemId, sourceQuantities.get(line.itemId) ?? new Decimal(0));
      requested.set(line.itemId, (requested.get(line.itemId) ?? new Decimal(0)).plus(quantity));
    }
    for (const line of source.lines)
      if (line.itemId)
        sourceQuantities.set(
          line.itemId,
          (sourceQuantities.get(line.itemId) ?? new Decimal(0)).plus(line.quantity),
        );

    const returned = await withTenantTx(this.database.db, tenantId, async (tx) => {
      const returnInvoices = await tx
        .select({ id: salesInvoices.id })
        .from(salesInvoices)
        .where(
          and(
            eq(salesInvoices.tenantId, tenantId),
            eq(salesInvoices.referenceInvoiceId, sourceId),
            eq(salesInvoices.kind, 'sale_return'),
          ),
        );
      const totals = new Map<string, Decimal>();
      for (const invoice of returnInvoices) {
        const lines = await tx
          .select()
          .from(salesInvoiceLines)
          .where(and(eq(salesInvoiceLines.tenantId, tenantId), eq(salesInvoiceLines.invoiceId, invoice.id)));
        for (const line of lines)
          if (line.itemId)
            totals.set(line.itemId, (totals.get(line.itemId) ?? new Decimal(0)).plus(line.quantity));
      }
      return totals;
    });

    for (const [itemId, quantity] of requested) {
      const available = (sourceQuantities.get(itemId) ?? new Decimal(0)).minus(
        returned.get(itemId) ?? new Decimal(0),
      );
      if (quantity.gt(available))
        throw new DomainError(
          'SALES_RETURN_QUANTITY_EXCEEDED',
          'Return quantity exceeds the remaining invoice quantity',
          422,
        );
    }
    return this.create(tenantId, {
      ...input,
      kind: 'sale_return',
      partyId: input.partyId ?? source.partyId ?? undefined,
      referenceInvoiceId: sourceId,
    } as SalesInvoiceInput & { referenceInvoiceId: string });
  }

  async evaluateOffer(
    tenantId: string,
    offerId: string,
    input: { itemId: string; quantity: string; value: string },
  ) {
    const [offer] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(offers)
        .where(and(eq(offers.tenantId, tenantId), eq(offers.id, offerId))),
    );
    if (!offer) throw new DomainError('SALES_OFFER_NOT_FOUND', 'Offer was not found', 404);
    const now = Date.now();
    if (offer.status !== 'active' || now < offer.validFrom.getTime() || now > offer.validTo.getTime())
      return { eligible: false, discount: '0', reason: 'OFFER_NOT_VALID' };
    const target = money(offer.targetValue);
    const eligible =
      offer.targetType === 'value' ? money(input.value).gte(target) : money(input.quantity).gte(target);
    if (!eligible) return { eligible: false, discount: '0', reason: 'TARGET_NOT_MET' };
    const discount =
      offer.discountType === 'percent'
        ? money(input.value).mul(offer.discountValue).div(100)
        : Decimal.min(money(offer.discountValue), money(input.value));
    return { eligible: true, discount: discount.toFixed(4), offerId };
  }

  /**
   * Posting allocates the note number from the shared sequence service, the same way an
   * invoice does. It used to mint `AN-<epoch>-<id fragment>`, which is neither sequential
   * nor auditable — a tax authority expects an unbroken series per document type.
   */
  async postAdjustmentNote(tenantId: string, id: string) {
    const [note] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(salesAdjustmentNotes)
        .where(and(eq(salesAdjustmentNotes.tenantId, tenantId), eq(salesAdjustmentNotes.id, id))),
    );
    if (!note) throw new DomainError('SALES_NOTE_NOT_FOUND', 'Adjustment note was not found', 404);
    if (note.status !== 'draft')
      throw new DomainError('SALES_NOTE_INVALID_STATUS', 'Only draft adjustment notes can be posted', 409);
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const allocated = await this.sequences.next(
        { tenantId, branchId: note.branchId, docType: `sales_note_${note.kind}` },
        tx,
        { prefix: note.kind === 'credit' ? 'SCN-' : 'SDN-', padding: 6 },
      );
      const [posted] = await tx
        .update(salesAdjustmentNotes)
        .set({
          status: 'posted',
          number: allocated.display,
          postedAt: new Date(),
          updatedAt: new Date(),
          updatedBy: tryGetAuthContext()?.userId,
        })
        .where(
          and(
            eq(salesAdjustmentNotes.tenantId, tenantId),
            eq(salesAdjustmentNotes.id, id),
            eq(salesAdjustmentNotes.status, 'draft'),
          ),
        )
        .returning();
      if (!posted)
        throw new DomainError('SALES_NOTE_INVALID_STATUS', 'Only draft adjustment notes can be posted', 409);
      return posted;
    });
  }

  async printData(tenantId: string, id: string) {
    return this.get(tenantId, id);
  }

  /**
   * Quotations (عرض سعر) reuse the invoice tables with `kind = 'quotation'`. They are
   * numbered on creation — a customer needs a reference before anything is agreed — but
   * they never touch stock or the ledger; the conversion below does that.
   */
  async createQuotation(tenantId: string, input: SalesInvoiceInput & { validUntil?: string }) {
    const quotation = await this.create(tenantId, { ...input, kind: 'quotation' });
    const [numbered] = await withTenantTx(this.database.db, tenantId, async (tx) => {
      const allocated = await this.sequences.next(
        { tenantId, branchId: quotation.branchId, docType: 'quotation' },
        tx,
        { prefix: 'QT-', padding: 6 },
      );
      return tx
        .update(salesInvoices)
        .set({ number: allocated.display })
        .where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, quotation.id)))
        .returning();
    });
    return { ...quotation, number: numbered?.number ?? quotation.number };
  }

  async listQuotations(tenantId: string) {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(salesInvoices)
        .where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.kind, 'quotation')))
        .orderBy(desc(salesInvoices.createdAt))
        .limit(200),
    );
  }

  /**
   * Turns a quotation into a draft sales invoice, copying the lines as they were quoted.
   * The quotation is stamped `converted` and keeps a pointer to what it became, so the
   * same quote cannot be billed twice.
   */
  async convertQuotation(tenantId: string, id: string, input: { warehouseId?: string } = {}) {
    const quotation = await this.get(tenantId, id);
    if (quotation.kind !== 'quotation')
      throw new DomainError('SALES_QUOTATION_EXPECTED', 'This document is not a quotation', 422);
    if (quotation.status === 'converted')
      throw new DomainError(
        'SALES_QUOTATION_ALREADY_CONVERTED',
        'This quotation was already converted into an invoice',
        409,
      );
    if (quotation.status !== 'draft')
      throw new DomainError('SALES_QUOTATION_INVALID_STATUS', 'Only an open quotation can be converted', 409);
    if (quotation.validUntil && quotation.validUntil < new Date().toISOString().slice(0, 10)) {
      throw new DomainError('SALES_QUOTATION_EXPIRED', 'This quotation expired; issue a new one', 422);
    }

    const invoice = await this.create(tenantId, {
      branchId: quotation.branchId,
      warehouseId: input.warehouseId ?? quotation.warehouseId ?? undefined,
      partyId: quotation.partyId ?? undefined,
      salesmanId: quotation.salesmanId ?? undefined,
      cashCustomerName: quotation.cashCustomerName ?? undefined,
      cashCustomerMobile: quotation.cashCustomerMobile ?? undefined,
      currency: quotation.currency,
      priceIncludesVat: quotation.priceIncludesVat,
      referenceInvoiceId: quotation.id,
      kind: 'sale',
      lines: quotation.lines.map((line) => ({
        itemId: line.itemId ?? undefined,
        description: line.description ?? undefined,
        quantity: line.quantity,
        unitPrice: line.unitPrice,
        discountRate: line.discountRate,
        taxRate: line.taxRate,
        taxGroupId: line.taxGroupId ?? undefined,
      })),
    });

    await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .update(salesInvoices)
        .set({
          status: 'converted',
          convertedInvoiceId: invoice.id,
          updatedAt: new Date(),
          updatedBy: tryGetAuthContext()?.userId,
        })
        .where(
          and(
            eq(salesInvoices.tenantId, tenantId),
            eq(salesInvoices.id, id),
            eq(salesInvoices.status, 'draft'),
          ),
        ),
    );

    return invoice;
  }

  async createAdjustmentNote(
    tenantId: string,
    invoiceId: string,
    input: { branchId: string; kind: string; reason: string; amount: string },
  ) {
    const invoice = await this.get(tenantId, invoiceId);
    if (invoice.status !== 'posted')
      throw new DomainError('SALES_INVOICE_NOT_POSTED', 'Notes require a posted invoice', 409);
    const [note] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .insert(salesAdjustmentNotes)
        .values({
          id: newId(),
          tenantId,
          invoiceId,
          branchId: input.branchId,
          kind: input.kind,
          reason: input.reason,
          amount: input.amount,
        })
        .returning(),
    );
    return note;
  }

  /** Notes issued against posted invoices, newest first — the source of the notes report. */
  async listAdjustmentNotes(tenantId: string, kind?: string) {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select({
          id: salesAdjustmentNotes.id,
          number: salesAdjustmentNotes.number,
          kind: salesAdjustmentNotes.kind,
          status: salesAdjustmentNotes.status,
          reason: salesAdjustmentNotes.reason,
          amount: salesAdjustmentNotes.amount,
          postedAt: salesAdjustmentNotes.postedAt,
          createdAt: salesAdjustmentNotes.createdAt,
          invoiceId: salesAdjustmentNotes.invoiceId,
          invoiceNumber: salesInvoices.number,
          partyId: salesInvoices.partyId,
          invoiceTotal: salesInvoices.total,
        })
        .from(salesAdjustmentNotes)
        .leftJoin(salesInvoices, eq(salesInvoices.id, salesAdjustmentNotes.invoiceId))
        .where(
          and(
            eq(salesAdjustmentNotes.tenantId, tenantId),
            kind ? eq(salesAdjustmentNotes.kind, kind) : undefined,
          ),
        )
        .orderBy(desc(salesAdjustmentNotes.createdAt))
        .limit(200),
    );
  }

  async listOffers(tenantId: string) {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx.select().from(offers).where(eq(offers.tenantId, tenantId)).orderBy(desc(offers.validFrom)),
    );
  }
  /** Validity arrives as ISO strings over HTTP; drizzle timestamps need real `Date`s. */
  async createOffer(
    tenantId: string,
    input: Omit<typeof offers.$inferInsert, 'id' | 'tenantId' | 'validFrom' | 'validTo'> & {
      validFrom: string | Date;
      validTo: string | Date;
    },
  ) {
    const validFrom = input.validFrom instanceof Date ? input.validFrom : new Date(input.validFrom);
    const validTo = input.validTo instanceof Date ? input.validTo : new Date(input.validTo);
    if (Number.isNaN(validFrom.getTime()) || Number.isNaN(validTo.getTime()))
      throw new DomainError('SALES_OFFER_INVALID_PERIOD', 'Offer validity dates are invalid', 422);
    if (validTo < validFrom)
      throw new DomainError('SALES_OFFER_INVALID_PERIOD', 'Offer end date precedes its start date', 422);
    const [offer] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .insert(offers)
        .values({
          ...input,
          validFrom,
          validTo,
          id: newId(),
          tenantId,
          createdBy: tryGetAuthContext()?.userId,
        })
        .returning(),
    );
    return offer;
  }
  /**
   * 👤 عميل نقدي — `Form_WPF/frmCashCustomer.xaml.cs` (`SearchCustomers`).
   *
   * The desktop does **not** keep a table of cash customers: it searches the invoices
   * themselves —
   * `SELECT CashCustomerName, CashCustomerMobile FROM inv WHERE CashCustomerMobile = @Mobile`
   * or `… WHERE CashCustomerName LIKE '%' + @Name + '%'`, both with
   * `CashCustomerName <> ''`. A walk-in is a name and a mobile written **on the sale**,
   * which is why a till can produce one without opening the customer ledger, and why
   * "find the customer" means "find a name the shop has already served".
   *
   * The cloud keeps that source of truth (`sales_invoices`) and groups it so a name is
   * an answer, not a row per visit. One intentional difference: the desktop's grid
   * starts empty and fills only on a keystroke, while a list screen has to show
   * something, so with no search term we return the most recently served names.
   */
  async cashCustomers(tenantId: string, filters: { name?: string; mobile?: string } = {}) {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select({
          name: salesInvoices.cashCustomerName,
          mobile: salesInvoices.cashCustomerMobile,
          /** كم فاتورة بهذا الاسم — `count(*)` over the group. */
          invoices: sql<number>`count(*)::int`,
          lastAt: sql<Date>`max(${salesInvoices.createdAt})`,
        })
        .from(salesInvoices)
        .where(
          and(
            eq(salesInvoices.tenantId, tenantId),
            isNotNull(salesInvoices.cashCustomerName),
            sql`${salesInvoices.cashCustomerName} <> ''`,
            filters.mobile ? eq(salesInvoices.cashCustomerMobile, filters.mobile) : undefined,
            filters.name ? ilike(salesInvoices.cashCustomerName, `%${filters.name}%`) : undefined,
          ),
        )
        .groupBy(salesInvoices.cashCustomerName, salesInvoices.cashCustomerMobile)
        .orderBy(desc(sql`max(${salesInvoices.createdAt})`))
        .limit(100),
    );
  }

  async listSalesmen(tenantId: string) {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx.select().from(salesmen).where(eq(salesmen.tenantId, tenantId)).orderBy(salesmen.name),
    );
  }
  async createSalesman(tenantId: string, input: { name: string; employeeRef?: string; active?: boolean }) {
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .insert(salesmen)
        .values({
          id: newId(),
          tenantId,
          name: input.name,
          employeeRef: input.employeeRef,
          active: input.active ?? true,
        })
        .returning(),
    );
    return row;
  }
  async updateSalesman(
    tenantId: string,
    id: string,
    input: { name?: string; employeeRef?: string | null; active?: boolean },
  ) {
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .update(salesmen)
        .set({
          ...(input.name === undefined ? {} : { name: input.name }),
          ...(input.employeeRef === undefined ? {} : { employeeRef: input.employeeRef }),
          ...(input.active === undefined ? {} : { active: input.active }),
          updatedAt: new Date(),
        })
        .where(and(eq(salesmen.tenantId, tenantId), eq(salesmen.id, id)))
        .returning(),
    );
    if (!row) throw new DomainError('NOT_FOUND', 'Salesman was not found', 404);
    return row;
  }
  /**
   * A salesman who is already named on invoices is deactivated rather than deleted, so
   * commission and performance reports for closed periods keep their subject.
   */
  async deleteSalesman(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const used = await tx.execute(
        sql`SELECT EXISTS (SELECT 1 FROM sales_invoices WHERE tenant_id = ${tenantId} AND salesman_id = ${id}) AS used`,
      );
      if ((used.rows[0] as { used: boolean }).used) {
        const [row] = await tx
          .update(salesmen)
          .set({ active: false, updatedAt: new Date() })
          .where(and(eq(salesmen.tenantId, tenantId), eq(salesmen.id, id)))
          .returning();
        if (!row) throw new DomainError('NOT_FOUND', 'Salesman was not found', 404);
        return { id, archived: true, deleted: false };
      }
      const result = await tx
        .delete(salesmen)
        .where(and(eq(salesmen.tenantId, tenantId), eq(salesmen.id, id)));
      if (!result.rowCount) throw new DomainError('NOT_FOUND', 'Salesman was not found', 404);
      return { id, archived: false, deleted: true };
    });
  }
}

export const salesService = { calculateInvoiceTotals };
