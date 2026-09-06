import { Inject, Injectable } from '@nestjs/common';
import { and, desc, eq } from 'drizzle-orm';
import { calculateInvoiceTotals, DomainError, newId } from '@erp/contracts';
import { invoicePayments, salesInvoiceLines, salesInvoices, withTenantTx, type DatabaseHandle } from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';

export type SalesLineInput = { itemId?: string; description?: string; quantity: string; unitPrice: string; discountRate?: string; discountAmount?: string; taxRate?: string; taxGroupId?: string };
export type SalesInvoiceInput = { branchId: string; warehouseId?: string; partyId?: string; kind?: 'sale' | 'sale_return' | 'credit_note' | 'debit_note'; currency?: string; priceIncludesVat?: boolean; invoiceDiscount?: string; extraTax?: string; withholding?: string; lines: SalesLineInput[]; cashCustomerName?: string; cashCustomerMobile?: string };

@Injectable()
export class SalesService {
  constructor(@Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle) {}

  async list(tenantId: string) { return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(salesInvoices).where(eq(salesInvoices.tenantId, tenantId)).orderBy(desc(salesInvoices.createdAt)).limit(100)); }

  async get(tenantId: string, id: string) {
    const [invoice] = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(salesInvoices).where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, id))));
    if (!invoice) throw new DomainError('SALES_INVOICE_NOT_FOUND', 'Sales invoice was not found', 404);
    const lines = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(salesInvoiceLines).where(and(eq(salesInvoiceLines.tenantId, tenantId), eq(salesInvoiceLines.invoiceId, id))));
    return { ...invoice, lines };
  }

  async create(tenantId: string, input: SalesInvoiceInput) {
    if (!input.lines.length) throw new DomainError('SALES_LINES_REQUIRED', 'At least one invoice line is required', 422);
    const totals = calculateInvoiceTotals({ lines: input.lines, priceIncludesVat: input.priceIncludesVat, invoiceDiscount: input.invoiceDiscount, extraTax: input.extraTax, withholding: input.withholding });
    const id = newId();
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      await tx.insert(salesInvoices).values({ id, tenantId, branchId: input.branchId, warehouseId: input.warehouseId, partyId: input.partyId, kind: input.kind ?? 'sale', currency: input.currency ?? 'SAR', priceIncludesVat: input.priceIncludesVat ?? false, cashCustomerName: input.cashCustomerName, cashCustomerMobile: input.cashCustomerMobile, invoiceDiscount: totals.discount, extraTax: totals.extraTax, withholding: totals.withholding, subtotal: totals.subtotal, taxTotal: totals.tax, total: totals.total, status: 'draft' });
      await tx.insert(salesInvoiceLines).values(input.lines.map((line, index) => {
        const calculated = totals.lines[index];
        if (!calculated) throw new DomainError('SALES_TOTALS_INVALID', 'Invoice totals do not match invoice lines', 422);
        return { id: newId(), tenantId, invoiceId: id, lineNo: index + 1, itemId: line.itemId, description: line.description, quantity: line.quantity, unitPrice: line.unitPrice, discountRate: line.discountRate ?? '0', discountAmount: calculated.discount, taxRate: line.taxRate ?? '0', net: calculated.net, tax: calculated.tax, total: calculated.total };
      }));
    });
    return this.get(tenantId, id);
  }

  async updateDraft(tenantId: string, id: string, input: Partial<SalesInvoiceInput>) {
    const invoice = await this.get(tenantId, id);
    if (invoice.status !== 'draft') throw new DomainError('SALES_INVOICE_IMMUTABLE', 'Only draft invoices can be changed', 409);
    if (input.lines) return this.create(tenantId, { ...input, branchId: input.branchId ?? invoice.branchId, lines: input.lines } as SalesInvoiceInput);
    await withTenantTx(this.database.db, tenantId, (tx) => tx.update(salesInvoices).set({ partyId: input.partyId, warehouseId: input.warehouseId, updatedAt: new Date() }).where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, id))));
    return this.get(tenantId, id);
  }

  async post(tenantId: string, id: string) {
    const invoice = await this.get(tenantId, id);
    if (invoice.status === 'posted') return invoice;
    if (invoice.status !== 'draft') throw new DomainError('SALES_INVOICE_INVALID_STATUS', 'Only draft invoices can be posted', 409);
    const number = `SI-${Date.now()}-${id.slice(0, 6)}`;
    await withTenantTx(this.database.db, tenantId, (tx) => tx.update(salesInvoices).set({ status: 'posted', number, postedAt: new Date(), paymentStatus: invoice.total === '0' ? 'paid' : 'unpaid' }).where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, id), eq(salesInvoices.status, 'draft'))));
    return this.get(tenantId, id);
  }

  async addPayment(tenantId: string, invoiceId: string, input: { method: string; amount: string; idempotencyKey: string; cashLocationId?: string; reference?: string }) {
    const invoice = await this.get(tenantId, invoiceId);
    if (invoice.status !== 'posted') throw new DomainError('SALES_INVOICE_NOT_POSTED', 'Payments require a posted invoice', 409);
    const result = await withTenantTx(this.database.db, tenantId, async (tx) => {
      const [existing] = await tx.select().from(invoicePayments).where(and(eq(invoicePayments.tenantId, tenantId), eq(invoicePayments.idempotencyKey, input.idempotencyKey)));
      if (existing) return existing;
      const [payment] = await tx.insert(invoicePayments).values({ id: newId(), tenantId, invoiceId, ...input }).returning();
      return payment;
    });
    return result;
  }
}

export const salesService = { calculateInvoiceTotals };
