import { sql } from 'drizzle-orm';
import { boolean, index, integer, jsonb, numeric, pgTable, text, timestamp, uniqueIndex, uuid } from 'drizzle-orm/pg-core';

import { baseAuditColumns, baseLegacyColumns } from '../columns.js';

import { branches, warehouses } from './organization.js';
import { items, taxGroups } from './catalog.js';
import { parties } from './parties.js';
import { tenants } from './platform.js';

const money = { precision: 20, scale: 4, mode: 'string' as const };
const qty = { precision: 20, scale: 4, mode: 'string' as const };

export const salesInvoices = pgTable('sales_invoices', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }),
  branchId: uuid('branch_id').notNull().references(() => branches.id), warehouseId: uuid('warehouse_id').references(() => warehouses.id),
  partyId: uuid('party_id').references(() => parties.id), salesmanId: uuid('salesman_id'), kind: text('kind').notNull().default('sale'),
  status: text('status').notNull().default('draft'), number: text('number'), currency: text('currency').notNull().default('SAR'),
  priceIncludesVat: boolean('price_includes_vat').notNull().default(false), cashCustomerName: text('cash_customer_name'), cashCustomerMobile: text('cash_customer_mobile'),
  invoiceDiscount: numeric('invoice_discount', money).notNull().default('0'), extraTax: numeric('extra_tax', money).notNull().default('0'), withholding: numeric('withholding', money).notNull().default('0'),
  subtotal: numeric('subtotal', money).notNull().default('0'), taxTotal: numeric('tax_total', money).notNull().default('0'), total: numeric('total', money).notNull().default('0'), paidTotal: numeric('paid_total', money).notNull().default('0'),
  paymentStatus: text('payment_status').notNull().default('unpaid'), costTotal: numeric('cost_total', money).notNull().default('0'), profit: numeric('profit', money).notNull().default('0'),
  zatcaUuid: uuid('zatca_uuid'), zatcaHash: text('zatca_hash'), zatcaQr: text('zatca_qr'), zatcaStatus: text('zatca_status'), postedAt: timestamp('posted_at', { withTimezone: true }), voidedAt: timestamp('voided_at', { withTimezone: true }),
  ...baseAuditColumns(), ...baseLegacyColumns(),
}, (t) => ({ number: uniqueIndex('sales_invoices_tenant_number_key').on(t.tenantId, t.number).where(sql`number IS NOT NULL`), scope: index('sales_invoices_scope_idx').on(t.tenantId, t.branchId, t.status), party: index('sales_invoices_party_idx').on(t.tenantId, t.partyId) }));

export const salesInvoiceLines = pgTable('sales_invoice_lines', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), invoiceId: uuid('invoice_id').notNull().references(() => salesInvoices.id, { onDelete: 'cascade' }), lineNo: integer('line_no').notNull(), itemId: uuid('item_id').references(() => items.id), taxGroupId: uuid('tax_group_id').references(() => taxGroups.id), description: text('description'), quantity: numeric('quantity', qty).notNull(), unitPrice: numeric('unit_price', money).notNull(), discountRate: numeric('discount_rate', money).notNull().default('0'), discountAmount: numeric('discount_amount', money).notNull().default('0'), taxRate: numeric('tax_rate', money).notNull().default('0'), net: numeric('net', money).notNull().default('0'), tax: numeric('tax', money).notNull().default('0'), total: numeric('total', money).notNull().default('0'), costTotal: numeric('cost_total', money).notNull().default('0'), metadata: jsonb('metadata').$type<Record<string, unknown>>().notNull().default({}), ...baseAuditColumns(),
}, (t) => ({ number: uniqueIndex('sales_invoice_lines_invoice_line_key').on(t.invoiceId, t.lineNo), scope: index('sales_invoice_lines_scope_idx').on(t.tenantId, t.itemId) }));

export const invoicePayments = pgTable('invoice_payments', { id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), invoiceId: uuid('invoice_id').notNull().references(() => salesInvoices.id, { onDelete: 'cascade' }), method: text('method').notNull(), amount: numeric('amount', money).notNull(), cashLocationId: uuid('cash_location_id'), reference: text('reference'), idempotencyKey: text('idempotency_key').notNull(), createdAt: timestamp('created_at', { withTimezone: true }).notNull().defaultNow(), createdBy: uuid('created_by') }, (t) => ({ idem: uniqueIndex('invoice_payments_tenant_idempotency_key').on(t.tenantId, t.idempotencyKey), invoice: index('invoice_payments_invoice_idx').on(t.tenantId, t.invoiceId) }));
export const salesAdjustmentNotes = pgTable('sales_adjustment_notes', { id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), invoiceId: uuid('invoice_id').references(() => salesInvoices.id), branchId: uuid('branch_id').notNull().references(() => branches.id), kind: text('kind').notNull(), status: text('status').notNull().default('draft'), number: text('number'), reason: text('reason').notNull(), amount: numeric('amount', money).notNull(), postedAt: timestamp('posted_at', { withTimezone: true }), ...baseAuditColumns() }, (t) => ({ number: uniqueIndex('sales_adjustment_notes_tenant_number_key').on(t.tenantId, t.number).where(sql`number IS NOT NULL`) }));
export const offers = pgTable('offers', { id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), code: text('code').notNull(), name: text('name').notNull(), validFrom: timestamp('valid_from', { withTimezone: true }).notNull(), validTo: timestamp('valid_to', { withTimezone: true }).notNull(), status: text('status').notNull().default('active'), targetType: text('target_type').notNull().default('quantity'), targetValue: numeric('target_value', qty).notNull().default('0'), discountType: text('discount_type').notNull().default('percent'), discountValue: numeric('discount_value', money).notNull().default('0'), ...baseAuditColumns() }, (t) => ({ code: uniqueIndex('offers_tenant_code_key').on(t.tenantId, t.code), scope: index('offers_validity_idx').on(t.tenantId, t.validFrom, t.validTo) }));
export const offerItems = pgTable('offer_items', { offerId: uuid('offer_id').notNull().references(() => offers.id, { onDelete: 'cascade' }), itemId: uuid('item_id').notNull().references(() => items.id), ...baseAuditColumns() }, (t) => ({ key: uniqueIndex('offer_items_offer_item_key').on(t.offerId, t.itemId) }));
export const offerParties = pgTable('offer_parties', { offerId: uuid('offer_id').notNull().references(() => offers.id, { onDelete: 'cascade' }), partyId: uuid('party_id').notNull().references(() => parties.id), ...baseAuditColumns() }, (t) => ({ key: uniqueIndex('offer_parties_offer_party_key').on(t.offerId, t.partyId) }));
export const salesmen = pgTable('salesmen', { id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), name: text('name').notNull(), employeeRef: text('employee_ref'), active: boolean('active').notNull().default(true), ...baseAuditColumns() }, (t) => ({ name: uniqueIndex('salesmen_tenant_name_key').on(t.tenantId, t.name) }));

export const salesTables = { salesInvoices, salesInvoiceLines, invoicePayments, salesAdjustmentNotes, offers, offerItems, offerParties, salesmen };
export type SalesInvoice = typeof salesInvoices.$inferSelect;
export type SalesInvoiceLine = typeof salesInvoiceLines.$inferSelect;
