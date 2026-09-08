/* eslint-disable no-restricted-syntax */
import { Inject, Injectable } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { and, desc, eq } from 'drizzle-orm';
import { allocateLandedCost, calculateInvoiceTotals, DomainError, newId } from '@erp/contracts';
import {
  parties,
  paymentAllocations,
  purchaseAdjustmentNotes,
  purchaseInvoiceCosts,
  purchaseInvoiceLines,
  purchaseInvoices,
  withTenantTx,
  type DatabaseHandle,
} from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';
import { tryGetAuthContext } from '../platform/context/tenant-context.js';
import { AccountingService, type JournalLineInput } from '../accounting/accounting.service.js';
import { InventoryService, type InventoryLine } from '../inventory/inventory.service.js';
import { SequencesService } from '../platform-services/index.js';

export type PurchaseLineInput = { itemId: string; description?: string; quantity: string; unitPrice: string; discountRate?: string; discountAmount?: string; taxRate?: string; taxGroupId?: string };
export type PurchaseInvoiceInput = { branchId: string; warehouseId?: string; partyId: string; referenceInvoiceId?: string; kind?: 'purchase' | 'purchase_return'; supplierReferenceNo?: string; supplierReferenceDate?: string; currency?: string; priceIncludesVat?: boolean; invoiceDiscount?: string; extraTax?: string; withholding?: string; landedCostAlloc?: 'qty' | 'value'; lines: PurchaseLineInput[] };
export type PurchaseCostInput = { costName: string; amount: string; allocationTarget?: 'inventory' | 'expense'; costCenterId?: string; accountId?: string };
export type PurchasePostingInput = { fiscalPeriodId?: string; journalLines?: JournalLineInput[] };
export type PurchasePaymentInput = { amount: string; idempotencyKey?: string; voucherId?: string; reference?: string };

const money = (value: string) => new Decimal(value);
const landedCostMethod = (value: string | null | undefined): 'qty' | 'value' => value === 'qty' ? 'qty' : 'value';

@Injectable()
export class PurchasesService {
  constructor(
    @Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle,
    private readonly inventory: InventoryService,
    private readonly accounting: AccountingService,
    private readonly sequences: SequencesService,
  ) {}

  list(tenantId: string) { return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(purchaseInvoices).where(eq(purchaseInvoices.tenantId, tenantId)).orderBy(desc(purchaseInvoices.createdAt)).limit(100)); }

  async get(tenantId: string, id: string) {
    const [invoice] = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(purchaseInvoices).where(and(eq(purchaseInvoices.tenantId, tenantId), eq(purchaseInvoices.id, id))));
    if (!invoice) throw new DomainError('PURCHASE_INVOICE_NOT_FOUND', 'Purchase invoice was not found', 404);
    const lines = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(purchaseInvoiceLines).where(and(eq(purchaseInvoiceLines.tenantId, tenantId), eq(purchaseInvoiceLines.invoiceId, id))));
    const costs = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(purchaseInvoiceCosts).where(and(eq(purchaseInvoiceCosts.tenantId, tenantId), eq(purchaseInvoiceCosts.invoiceId, id))));
    return { ...invoice, lines, costs };
  }

  async create(tenantId: string, input: PurchaseInvoiceInput) {
    if (!input.lines.length) throw new DomainError('PURCHASE_LINES_REQUIRED', 'At least one purchase line is required', 422);
    const id = newId();
    const totals = calculateInvoiceTotals({ lines: input.lines, priceIncludesVat: input.priceIncludesVat, invoiceDiscount: input.invoiceDiscount, extraTax: input.extraTax, withholding: input.withholding });
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      const [supplier] = await tx.select().from(parties).where(and(eq(parties.tenantId, tenantId), eq(parties.id, input.partyId)));
      if (!supplier || !['supplier', 'both'].includes(supplier.kind)) throw new DomainError('PURCHASE_SUPPLIER_REQUIRED', 'Purchase invoices require a supplier party', 422);
      await tx.insert(purchaseInvoices).values({ id, tenantId, createdBy: tryGetAuthContext()?.userId, branchId: input.branchId, warehouseId: input.warehouseId, partyId: input.partyId, referenceInvoiceId: input.referenceInvoiceId, kind: input.kind ?? 'purchase', supplierReferenceNo: input.supplierReferenceNo, supplierReferenceDate: input.supplierReferenceDate, currency: input.currency ?? 'SAR', priceIncludesVat: input.priceIncludesVat ?? false, landedCostAlloc: input.landedCostAlloc ?? 'value', invoiceDiscount: totals.discount, extraTax: totals.extraTax, withholding: totals.withholding, subtotal: totals.subtotal, taxTotal: totals.tax, total: totals.total, status: 'draft' });
      await tx.insert(purchaseInvoiceLines).values(input.lines.map((line, index) => { const calculated = totals.lines[index]; if (!calculated) throw new DomainError('PURCHASE_TOTALS_INVALID', 'Purchase totals do not match invoice lines', 422); return { id: newId(), tenantId, invoiceId: id, lineNo: index + 1, itemId: line.itemId, description: line.description, quantity: line.quantity, unitPrice: line.unitPrice, discountRate: line.discountRate ?? '0', discountAmount: calculated.discount, taxGroupId: line.taxGroupId, taxRate: line.taxRate ?? '0', net: calculated.net, tax: calculated.tax, total: calculated.total }; }));
    });
    return this.get(tenantId, id);
  }

  async updateDraft(tenantId: string, id: string, input: Partial<PurchaseInvoiceInput>) {
    const invoice = await this.get(tenantId, id);
    if (invoice.status !== 'draft') throw new DomainError('PURCHASE_INVOICE_IMMUTABLE', 'Only draft purchase invoices can be changed', 409);
    if (input.lines) {
      await withTenantTx(this.database.db, tenantId, (tx) => tx.delete(purchaseInvoiceLines).where(and(eq(purchaseInvoiceLines.tenantId, tenantId), eq(purchaseInvoiceLines.invoiceId, id))));
      return this.create(tenantId, { ...input, branchId: input.branchId ?? invoice.branchId, partyId: input.partyId ?? invoice.partyId, warehouseId: input.warehouseId ?? invoice.warehouseId ?? undefined, lines: input.lines } as PurchaseInvoiceInput);
    }
    await withTenantTx(this.database.db, tenantId, (tx) => tx.update(purchaseInvoices).set({ warehouseId: input.warehouseId, supplierReferenceNo: input.supplierReferenceNo, supplierReferenceDate: input.supplierReferenceDate, landedCostAlloc: input.landedCostAlloc, updatedAt: new Date() }).where(and(eq(purchaseInvoices.tenantId, tenantId), eq(purchaseInvoices.id, id))));
    return this.get(tenantId, id);
  }

  async addCost(tenantId: string, invoiceId: string, input: PurchaseCostInput) {
    const invoice = await this.get(tenantId, invoiceId);
    if (invoice.status !== 'draft') throw new DomainError('PURCHASE_COST_IMMUTABLE', 'Costs can be edited only while invoice is draft', 409);
    const amount = money(input.amount);
    if (!amount.isFinite() || amount.lt(0)) throw new DomainError('PURCHASE_COST_INVALID', 'Cost amount must be non-negative', 422);
    const [cost] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(purchaseInvoiceCosts).values({ id: newId(), tenantId, invoiceId, costName: input.costName, amount: input.amount, allocationTarget: input.allocationTarget ?? 'inventory', costCenterId: input.costCenterId, accountId: input.accountId }).returning());
    return cost;
  }

  async deleteCost(tenantId: string, invoiceId: string, costId: string) {
    const invoice = await this.get(tenantId, invoiceId);
    if (invoice.status !== 'draft') throw new DomainError('PURCHASE_COST_IMMUTABLE', 'Costs can be edited only while invoice is draft', 409);
    await withTenantTx(this.database.db, tenantId, (tx) => tx.delete(purchaseInvoiceCosts).where(and(eq(purchaseInvoiceCosts.tenantId, tenantId), eq(purchaseInvoiceCosts.invoiceId, invoiceId), eq(purchaseInvoiceCosts.id, costId))));
    return { id: costId, deleted: true };
  }

  previewLandedCost(input: { lines: Array<{ lineId?: string; itemId?: string; quantity: string; net: string; unitCost?: string }>; costs: Array<{ amount: string }>; method: 'qty' | 'value' }) {
    return allocateLandedCost(input);
  }

  async previewInvoiceLandedCost(tenantId: string, invoiceId: string) {
    const invoice = await this.get(tenantId, invoiceId);
    return this.previewLandedCost({ method: landedCostMethod(invoice.landedCostAlloc), lines: invoice.lines.map((line) => ({ lineId: line.id, itemId: line.itemId, quantity: line.quantity, net: line.net })), costs: invoice.costs.filter((cost) => cost.allocationTarget === 'inventory').map((cost) => ({ amount: cost.amount })) });
  }

  async post(tenantId: string, id: string, posting: PurchasePostingInput = {}) {
    const invoice = await this.get(tenantId, id);
    if (invoice.status === 'posted') return invoice;
    if (invoice.status !== 'draft') throw new DomainError('PURCHASE_INVOICE_INVALID_STATUS', 'Only draft purchases can be posted', 409);
    if (posting.journalLines?.length && !posting.fiscalPeriodId) throw new DomainError('PURCHASE_FISCAL_PERIOD_REQUIRED', 'A fiscal period is required for accounting posting', 422);

    await withTenantTx(this.database.db, tenantId, async (tx) => {
      const [locked] = await tx.select().from(purchaseInvoices).where(and(eq(purchaseInvoices.tenantId, tenantId), eq(purchaseInvoices.id, id), eq(purchaseInvoices.status, 'draft')));
      if (!locked) throw new DomainError('PURCHASE_INVOICE_INVALID_STATUS', 'Only draft purchases can be posted', 409);
      const lines = await tx.select().from(purchaseInvoiceLines).where(and(eq(purchaseInvoiceLines.tenantId, tenantId), eq(purchaseInvoiceLines.invoiceId, id)));
      const costs = await tx.select().from(purchaseInvoiceCosts).where(and(eq(purchaseInvoiceCosts.tenantId, tenantId), eq(purchaseInvoiceCosts.invoiceId, id)));
      const allocation = allocateLandedCost({ method: landedCostMethod(locked.landedCostAlloc), lines: lines.map((line) => ({ lineId: line.id, itemId: line.itemId, quantity: line.quantity, net: line.net })), costs: costs.filter((cost) => cost.allocationTarget === 'inventory').map((cost) => ({ amount: cost.amount })) });
      const byLine = new Map(allocation.lines.map((line) => [line.lineId, line]));
      const docType = locked.kind === 'purchase_return' ? 'purchase_return' : 'purchase_invoice';
      const inventoryLines: InventoryLine[] = lines.map((line) => { const allocated = byLine.get(line.id); return { itemId: line.itemId, warehouseId: locked.warehouseId ?? '', qty: line.quantity, unitCost: allocated?.effectiveUnitCost ?? line.unitPrice, direction: locked.kind === 'purchase_return' ? 'out' : 'in', docType, docId: id, lineId: line.id, costing: locked.kind === 'purchase_return' ? 'outAtAvg' : 'inWithCost' }; });
      if (inventoryLines.some((line) => !line.warehouseId)) throw new DomainError('PURCHASE_WAREHOUSE_REQUIRED', 'Posting purchases requires a warehouse', 422);
      await this.inventory.recordInTx(tx, tenantId, inventoryLines);
      for (const line of lines) {
        const allocated = byLine.get(line.id);
        await tx.update(purchaseInvoiceLines).set({ allocatedCost: allocated?.allocatedCost ?? '0', landedTotal: allocated?.landedTotal ?? line.net, unitCostAtPost: allocated?.effectiveUnitCost ?? line.unitPrice, updatedAt: new Date() }).where(and(eq(purchaseInvoiceLines.tenantId, tenantId), eq(purchaseInvoiceLines.id, line.id)));
      }
      let journalEntryId: string | null = null;
      if (posting.journalLines?.length) {
        const journal = await this.accounting.postJournalInTx(tx, tenantId, { branchId: locked.branchId, fiscalPeriodId: posting.fiscalPeriodId!, date: new Date().toISOString().slice(0, 10), description: `Purchase invoice ${id}`, lines: posting.journalLines, sourceType: docType, sourceId: id });
        journalEntryId = journal?.id ?? null;
      }
      const allocated = await this.sequences.next({ tenantId, branchId: locked.branchId, docType }, tx, { prefix: locked.kind === 'purchase_return' ? 'PR-' : 'PI-', padding: 6 });
      const additionalCostTotal = costs.reduce((sum, cost) => sum.plus(cost.amount), new Decimal(0)).toFixed(4);
      await tx.update(purchaseInvoices).set({ status: 'posted', number: allocated.display, additionalCostTotal, total: money(locked.total).plus(additionalCostTotal).toFixed(4), journalEntryId, postedAt: new Date(), paymentStatus: 'unpaid' }).where(and(eq(purchaseInvoices.tenantId, tenantId), eq(purchaseInvoices.id, id), eq(purchaseInvoices.status, 'draft')));
    });
    return this.get(tenantId, id);
  }

  async void(tenantId: string, id: string, reason: string) {
    if (!reason.trim()) throw new DomainError('PURCHASE_VOID_REASON_REQUIRED', 'A void reason is required', 422);
    const invoice = await this.get(tenantId, id);
    if (invoice.status !== 'posted') throw new DomainError('PURCHASE_INVOICE_INVALID_STATUS', 'Only posted purchases can be voided', 409);
    await withTenantTx(this.database.db, tenantId, (tx) => tx.update(purchaseInvoices).set({ status: 'voided', voidedAt: new Date(), updatedAt: new Date() }).where(and(eq(purchaseInvoices.tenantId, tenantId), eq(purchaseInvoices.id, id), eq(purchaseInvoices.status, 'posted'))));
    return this.get(tenantId, id);
  }

  async addPayment(tenantId: string, invoiceId: string, input: PurchasePaymentInput) {
    const invoice = await this.get(tenantId, invoiceId);
    if (invoice.status !== 'posted') throw new DomainError('PURCHASE_INVOICE_NOT_POSTED', 'Payments require a posted purchase invoice', 409);
    const amount = money(input.amount);
    if (!amount.isFinite() || amount.lte(0)) throw new DomainError('PAYMENT_AMOUNT_INVALID', 'Payment amount must be positive', 422);
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const paidTotal = money(invoice.paidTotal).plus(amount);
      if (paidTotal.gt(money(invoice.total).plus('0.0001'))) throw new DomainError('PAYMENT_EXCEEDS_DUE', 'Payment exceeds purchase invoice balance', 422);
      const [allocation] = await tx.insert(paymentAllocations).values({ id: newId(), tenantId, partyId: invoice.partyId, voucherId: input.voucherId, invoiceKind: invoice.kind, invoiceId, amount: input.amount }).returning();
      await tx.update(purchaseInvoices).set({ paidTotal: paidTotal.toFixed(4), paymentStatus: paidTotal.gte(money(invoice.total)) ? 'paid' : 'partial', updatedAt: new Date() }).where(and(eq(purchaseInvoices.tenantId, tenantId), eq(purchaseInvoices.id, invoiceId)));
      return allocation;
    });
  }

  /** Supplier notes are raised against a posted invoice only — the mirror of the sales side. */
  async createAdjustmentNote(tenantId: string, invoiceId: string, input: { branchId: string; kind: string; reason: string; amount: string }) {
    const invoice = await this.get(tenantId, invoiceId);
    if (invoice.status !== 'posted') throw new DomainError('PURCHASE_INVOICE_NOT_POSTED', 'Notes require a posted purchase invoice', 409);
    if (input.kind !== 'credit' && input.kind !== 'debit') throw new DomainError('PURCHASE_NOTE_KIND_INVALID', 'A note is either credit or debit', 422);
    const amount = money(input.amount);
    if (!amount.isFinite() || amount.lte(0)) throw new DomainError('PURCHASE_NOTE_AMOUNT_INVALID', 'Note amount must be positive', 422);
    const [note] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx.insert(purchaseAdjustmentNotes).values({ id: newId(), tenantId, invoiceId, branchId: input.branchId, kind: input.kind, reason: input.reason, amount: amount.toFixed(4), createdBy: tryGetAuthContext()?.userId }).returning());
    return note;
  }

  /** Notes raised on supplier invoices, newest first — the source of the notes report. */
  async listAdjustmentNotes(tenantId: string, kind?: string) {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select({ id: purchaseAdjustmentNotes.id, number: purchaseAdjustmentNotes.number, kind: purchaseAdjustmentNotes.kind, status: purchaseAdjustmentNotes.status, reason: purchaseAdjustmentNotes.reason, amount: purchaseAdjustmentNotes.amount, postedAt: purchaseAdjustmentNotes.postedAt, createdAt: purchaseAdjustmentNotes.createdAt, invoiceId: purchaseAdjustmentNotes.invoiceId, invoiceNumber: purchaseInvoices.number, partyId: purchaseInvoices.partyId, invoiceTotal: purchaseInvoices.total })
        .from(purchaseAdjustmentNotes)
        .leftJoin(purchaseInvoices, eq(purchaseInvoices.id, purchaseAdjustmentNotes.invoiceId))
        .where(and(eq(purchaseAdjustmentNotes.tenantId, tenantId), kind ? eq(purchaseAdjustmentNotes.kind, kind) : undefined))
        .orderBy(desc(purchaseAdjustmentNotes.createdAt))
        .limit(200));
  }

  /** Posting allocates the note number through the shared sequence service. */
  async postAdjustmentNote(tenantId: string, id: string) {
    const [note] = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(purchaseAdjustmentNotes).where(and(eq(purchaseAdjustmentNotes.tenantId, tenantId), eq(purchaseAdjustmentNotes.id, id))));
    if (!note) throw new DomainError('PURCHASE_NOTE_NOT_FOUND', 'Adjustment note was not found', 404);
    if (note.status !== 'draft') throw new DomainError('PURCHASE_NOTE_INVALID_STATUS', 'Only draft adjustment notes can be posted', 409);
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const allocated = await this.sequences.next({ tenantId, branchId: note.branchId, docType: `purchase_note_${note.kind}` }, tx, { prefix: note.kind === 'credit' ? 'PCN-' : 'PDN-', padding: 6 });
      const [posted] = await tx
        .update(purchaseAdjustmentNotes)
        .set({ status: 'posted', number: allocated.display, postedAt: new Date(), updatedAt: new Date(), updatedBy: tryGetAuthContext()?.userId })
        .where(and(eq(purchaseAdjustmentNotes.tenantId, tenantId), eq(purchaseAdjustmentNotes.id, id), eq(purchaseAdjustmentNotes.status, 'draft')))
        .returning();
      if (!posted) throw new DomainError('PURCHASE_NOTE_INVALID_STATUS', 'Only draft adjustment notes can be posted', 409);
      return posted;
    });
  }
}
