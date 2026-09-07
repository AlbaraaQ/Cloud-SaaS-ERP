import { Inject, Injectable } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { and, desc, eq } from 'drizzle-orm';
import { calculateInvoiceTotals, DomainError, newId } from '@erp/contracts';
import {
  invoicePayments,
  offers,
  salesAdjustmentNotes,
  salesInvoiceLines,
  salesInvoices,
  salesmen,
  withTenantTx,
  type DatabaseHandle,
} from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';
import { AccountingService } from '../accounting/accounting.service.js';
import { InventoryService, type InventoryLine } from '../inventory/inventory.service.js';
import { SequencesService } from '../platform-services/index.js';

export type SalesLineInput = { itemId?: string; description?: string; quantity: string; unitPrice: string; discountRate?: string; discountAmount?: string; taxRate?: string; taxGroupId?: string };
export type SalesInvoiceInput = { branchId: string; warehouseId?: string; partyId?: string; salesmanId?: string; referenceInvoiceId?: string; kind?: 'sale' | 'sale_return' | 'credit_note' | 'debit_note'; currency?: string; priceIncludesVat?: boolean; invoiceDiscount?: string; extraTax?: string; withholding?: string; lines: SalesLineInput[]; cashCustomerName?: string; cashCustomerMobile?: string };
export type PaymentInput = { method: 'cash' | 'card' | 'bank' | 'credit' | 'split'; amount: string; idempotencyKey: string; cashLocationId?: string; reference?: string };
export type PostingInput = { fiscalPeriodId?: string; journalLines?: { accountId: string; debit?: string; credit?: string; partyId?: string; description?: string }[]; inventoryLines?: InventoryLine[] };

const money = (value: string) => new Decimal(value);

@Injectable()
export class SalesService {
  constructor(
    @Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle,
    private readonly inventory: InventoryService,
    private readonly accounting: AccountingService,
    private readonly sequences: SequencesService,
  ) {}

  async list(tenantId: string) { return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(salesInvoices).where(eq(salesInvoices.tenantId, tenantId)).orderBy(desc(salesInvoices.createdAt)).limit(100)); }

  async get(tenantId: string, id: string) {
    const [invoice] = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(salesInvoices).where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, id))));
    if (!invoice) throw new DomainError('SALES_INVOICE_NOT_FOUND', 'Sales invoice was not found', 404);
    const lines = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(salesInvoiceLines).where(and(eq(salesInvoiceLines.tenantId, tenantId), eq(salesInvoiceLines.invoiceId, id))));
    const payments = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(invoicePayments).where(and(eq(invoicePayments.tenantId, tenantId), eq(invoicePayments.invoiceId, id))));
    return { ...invoice, lines, payments };
  }

  async create(tenantId: string, input: SalesInvoiceInput) {
    if (!input.lines.length) throw new DomainError('SALES_LINES_REQUIRED', 'At least one invoice line is required', 422);
    if (!input.partyId && !input.cashCustomerName) throw new DomainError('SALES_CUSTOMER_REQUIRED', 'Party or cash customer name is required', 422);
    const totals = calculateInvoiceTotals({ lines: input.lines, priceIncludesVat: input.priceIncludesVat, invoiceDiscount: input.invoiceDiscount, extraTax: input.extraTax, withholding: input.withholding });
    const id = newId();
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      await tx.insert(salesInvoices).values({ id, tenantId, branchId: input.branchId, warehouseId: input.warehouseId, referenceInvoiceId: input.referenceInvoiceId, partyId: input.partyId, salesmanId: input.salesmanId, kind: input.kind ?? 'sale', currency: input.currency ?? 'SAR', priceIncludesVat: input.priceIncludesVat ?? false, cashCustomerName: input.cashCustomerName, cashCustomerMobile: input.cashCustomerMobile, invoiceDiscount: totals.discount, extraTax: totals.extraTax, withholding: totals.withholding, subtotal: totals.subtotal, taxTotal: totals.tax, total: totals.total, status: 'draft' });
      await tx.insert(salesInvoiceLines).values(input.lines.map((line, index) => { const calculated = totals.lines[index]; if (!calculated) throw new DomainError('SALES_TOTALS_INVALID', 'Invoice totals do not match invoice lines', 422); return { id: newId(), tenantId, invoiceId: id, lineNo: index + 1, itemId: line.itemId, description: line.description, quantity: line.quantity, unitPrice: line.unitPrice, discountRate: line.discountRate ?? '0', discountAmount: calculated.discount, taxGroupId: line.taxGroupId, taxRate: line.taxRate ?? '0', net: calculated.net, tax: calculated.tax, total: calculated.total }; }));
    });
    return this.get(tenantId, id);
  }

  async updateDraft(tenantId: string, id: string, input: Partial<SalesInvoiceInput>) {
    const invoice = await this.get(tenantId, id);
    if (invoice.status !== 'draft') throw new DomainError('SALES_INVOICE_IMMUTABLE', 'Only draft invoices can be changed', 409);
    if (input.lines) {
      await withTenantTx(this.database.db, tenantId, async (tx) => {
        await tx.delete(salesInvoiceLines).where(and(eq(salesInvoiceLines.tenantId, tenantId), eq(salesInvoiceLines.invoiceId, id)));
      });
      const replacement = await this.create(tenantId, { ...input, branchId: input.branchId ?? invoice.branchId, lines: input.lines } as SalesInvoiceInput);
      return replacement;
    }
    await withTenantTx(this.database.db, tenantId, (tx) => tx.update(salesInvoices).set({ partyId: input.partyId, salesmanId: input.salesmanId, warehouseId: input.warehouseId, cashCustomerName: input.cashCustomerName, cashCustomerMobile: input.cashCustomerMobile, updatedAt: new Date() }).where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, id))));
    return this.get(tenantId, id);
  }

  async post(tenantId: string, id: string, posting: PostingInput = {}) {
    const invoice = await this.get(tenantId, id);
    if (invoice.status === 'posted') return invoice;
    if (invoice.status !== 'draft') throw new DomainError('SALES_INVOICE_INVALID_STATUS', 'Only draft invoices can be posted', 409);
    if (posting.journalLines?.length && !posting.fiscalPeriodId) throw new DomainError('SALES_FISCAL_PERIOD_REQUIRED', 'A fiscal period is required for accounting posting', 422);

    await withTenantTx(this.database.db, tenantId, async (tx) => {
      const [locked] = await tx.select().from(salesInvoices).where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, id), eq(salesInvoices.status, 'draft')));
      if (!locked) throw new DomainError('SALES_INVOICE_INVALID_STATUS', 'Only draft invoices can be posted', 409);
      const prefix = locked.kind === 'sale_return' ? 'SR-' : locked.kind === 'credit_note' ? 'CN-' : locked.kind === 'debit_note' ? 'DN-' : 'SI-';
      const allocated = await this.sequences.next({ tenantId, branchId: locked.branchId, docType: locked.kind }, tx, { prefix, padding: 6 });
      const number = allocated.display;

      if (posting.inventoryLines?.length) await this.inventory.recordInTx(tx, tenantId, posting.inventoryLines.map((line) => ({ ...line, docType: line.docType || 'sales_invoice', docId: id })));
      if (posting.journalLines?.length) {
        await this.accounting.postJournalInTx(tx, tenantId, {
          branchId: locked.branchId,
          fiscalPeriodId: posting.fiscalPeriodId!,
          date: new Date().toISOString().slice(0, 10),
          description: `Sales invoice ${number}`,
          lines: posting.journalLines,
          sourceType: 'sales_invoice',
          sourceId: id,
        });
      }
      await tx.update(salesInvoices).set({ status: 'posted', number, postedAt: new Date(), paymentStatus: locked.total === '0' ? 'paid' : 'unpaid' }).where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, id), eq(salesInvoices.status, 'draft')));
    });
    return this.get(tenantId, id);
  }

  async void(tenantId: string, id: string, reason: string) {
    if (!reason.trim()) throw new DomainError('SALES_VOID_REASON_REQUIRED', 'A void reason is required', 422);
    const invoice = await this.get(tenantId, id);
    if (invoice.status !== 'posted') throw new DomainError('SALES_INVOICE_INVALID_STATUS', 'Only posted invoices can be voided', 409);
    await withTenantTx(this.database.db, tenantId, (tx) => tx.update(salesInvoices).set({ status: 'voided', voidedAt: new Date(), updatedAt: new Date(), zatcaStatus: `voided:${reason}` }).where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, id), eq(salesInvoices.status, 'posted'))));
    return this.get(tenantId, id);
  }

  async addPayment(tenantId: string, invoiceId: string, input: PaymentInput) {
    const invoice = await this.get(tenantId, invoiceId);
    if (invoice.status !== 'posted') throw new DomainError('SALES_INVOICE_NOT_POSTED', 'Payments require a posted invoice', 409);
    const paymentValue = money(input.amount);
    if (!paymentValue.isFinite() || paymentValue.lte(0)) throw new DomainError('PAYMENT_AMOUNT_INVALID', 'Payment amount must be positive', 422);
    if (input.method !== 'credit' && ['cash', 'card', 'bank', 'split'].includes(input.method) && input.method === 'cash' && !input.cashLocationId) throw new DomainError('CASH_LOCATION_REQUIRED', 'Cash payments require a cash location', 422);
    const result = await withTenantTx(this.database.db, tenantId, async (tx) => {
      const [existing] = await tx.select().from(invoicePayments).where(and(eq(invoicePayments.tenantId, tenantId), eq(invoicePayments.idempotencyKey, input.idempotencyKey)));
      if (existing) return existing;
      const paidValue = money(invoice.paidTotal).plus(paymentValue);
      if (paidValue.gt(money(invoice.total).plus('0.0001'))) throw new DomainError('PAYMENT_EXCEEDS_DUE', 'Payment exceeds invoice balance', 422);
      const [payment] = await tx.insert(invoicePayments).values({ id: newId(), tenantId, invoiceId, method: input.method, amount: input.amount, cashLocationId: input.cashLocationId, reference: input.reference, idempotencyKey: input.idempotencyKey }).returning();
      await tx.update(salesInvoices).set({ paidTotal: paidValue.toFixed(4), paymentStatus: paidValue.gte(money(invoice.total)) ? 'paid' : 'partial', updatedAt: new Date() }).where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, invoiceId)));
      return payment;
    });
    return result;
  }

  async returnFrom(tenantId: string, sourceId: string, input: Omit<SalesInvoiceInput, 'kind'>) {
    const source = await this.get(tenantId, sourceId);
    if (source.status !== 'posted') throw new DomainError('SALES_RETURN_SOURCE_INVALID', 'Returns require a posted source invoice', 409);
    if (source.kind === 'sale_return') throw new DomainError('SALES_RETURN_SOURCE_INVALID', 'A return cannot reference another return', 422);

    const sourceQuantities = new Map<string, Decimal>();
    const requested = new Map<string, Decimal>();
    for (const line of input.lines) {
      if (!line.itemId) throw new DomainError('SALES_RETURN_ITEM_REQUIRED', 'Return lines must reference an item', 422);
      const quantity = money(line.quantity);
      if (!quantity.isFinite() || quantity.lte(0)) throw new DomainError('SALES_RETURN_QUANTITY_INVALID', 'Return quantities must be positive', 422);
      sourceQuantities.set(line.itemId, sourceQuantities.get(line.itemId) ?? new Decimal(0));
      requested.set(line.itemId, (requested.get(line.itemId) ?? new Decimal(0)).plus(quantity));
    }
    for (const line of source.lines) if (line.itemId) sourceQuantities.set(line.itemId, (sourceQuantities.get(line.itemId) ?? new Decimal(0)).plus(line.quantity));

    const returned = await withTenantTx(this.database.db, tenantId, async (tx) => {
      const returnInvoices = await tx.select({ id: salesInvoices.id }).from(salesInvoices).where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.referenceInvoiceId, sourceId), eq(salesInvoices.kind, 'sale_return')));
      const totals = new Map<string, Decimal>();
      for (const invoice of returnInvoices) {
        const lines = await tx.select().from(salesInvoiceLines).where(and(eq(salesInvoiceLines.tenantId, tenantId), eq(salesInvoiceLines.invoiceId, invoice.id)));
        for (const line of lines) if (line.itemId) totals.set(line.itemId, (totals.get(line.itemId) ?? new Decimal(0)).plus(line.quantity));
      }
      return totals;
    });

    for (const [itemId, quantity] of requested) {
      const available = (sourceQuantities.get(itemId) ?? new Decimal(0)).minus(returned.get(itemId) ?? new Decimal(0));
      if (quantity.gt(available)) throw new DomainError('SALES_RETURN_QUANTITY_EXCEEDED', 'Return quantity exceeds the remaining invoice quantity', 422);
    }
    return this.create(tenantId, { ...input, kind: 'sale_return', partyId: input.partyId ?? source.partyId ?? undefined, referenceInvoiceId: sourceId } as SalesInvoiceInput & { referenceInvoiceId: string });
  }

  async evaluateOffer(tenantId: string, offerId: string, input: { itemId: string; quantity: string; value: string }) {
    const [offer] = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(offers).where(and(eq(offers.tenantId, tenantId), eq(offers.id, offerId))));
    if (!offer) throw new DomainError('SALES_OFFER_NOT_FOUND', 'Offer was not found', 404);
    const now = Date.now();
    if (offer.status !== 'active' || now < offer.validFrom.getTime() || now > offer.validTo.getTime()) return { eligible: false, discount: '0', reason: 'OFFER_NOT_VALID' };
    const target = money(offer.targetValue);
    const eligible = offer.targetType === 'value' ? money(input.value).gte(target) : money(input.quantity).gte(target);
    if (!eligible) return { eligible: false, discount: '0', reason: 'TARGET_NOT_MET' };
    const discount = offer.discountType === 'percent' ? money(input.value).mul(offer.discountValue).div(100) : Decimal.min(money(offer.discountValue), money(input.value));
    return { eligible: true, discount: discount.toFixed(4), offerId };
  }

  async postAdjustmentNote(tenantId: string, id: string) {
    const [note] = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(salesAdjustmentNotes).where(and(eq(salesAdjustmentNotes.tenantId, tenantId), eq(salesAdjustmentNotes.id, id))));
    if (!note) throw new DomainError('SALES_NOTE_NOT_FOUND', 'Adjustment note was not found', 404);
    if (note.status !== 'draft') throw new DomainError('SALES_NOTE_INVALID_STATUS', 'Only draft adjustment notes can be posted', 409);
    await withTenantTx(this.database.db, tenantId, (tx) => tx.update(salesAdjustmentNotes).set({ status: 'posted', number: `AN-${Date.now()}-${id.slice(0, 6)}`, postedAt: new Date() }).where(and(eq(salesAdjustmentNotes.tenantId, tenantId), eq(salesAdjustmentNotes.id, id), eq(salesAdjustmentNotes.status, 'draft'))));
    return this.get(tenantId, note.invoiceId ?? '');
  }

  async printData(tenantId: string, id: string) { return this.get(tenantId, id); }

  async createAdjustmentNote(tenantId: string, invoiceId: string, input: { branchId: string; kind: string; reason: string; amount: string }) {
    const invoice = await this.get(tenantId, invoiceId);
    if (invoice.status !== 'posted') throw new DomainError('SALES_INVOICE_NOT_POSTED', 'Notes require a posted invoice', 409);
    const [note] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(salesAdjustmentNotes).values({ id: newId(), tenantId, invoiceId, branchId: input.branchId, kind: input.kind, reason: input.reason, amount: input.amount }).returning());
    return note;
  }

  async listOffers(tenantId: string) { return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(offers).where(eq(offers.tenantId, tenantId)).orderBy(desc(offers.validFrom))); }
  async createOffer(tenantId: string, input: Omit<typeof offers.$inferInsert, 'id' | 'tenantId'>) { const [offer] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(offers).values({ ...input, id: newId(), tenantId }).returning()); return offer; }
  async listSalesmen(tenantId: string) { return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(salesmen).where(eq(salesmen.tenantId, tenantId)).orderBy(salesmen.name)); }
}

export const salesService = { calculateInvoiceTotals };
