import { sql } from 'drizzle-orm';
import { boolean, index, jsonb, numeric, pgTable, primaryKey, text, timestamp, uniqueIndex, uuid } from 'drizzle-orm/pg-core';

import { baseAuditColumns, baseSoftDeleteColumns } from '../columns.js';

import { accounts } from './accounting.js';
import { branches } from './organization.js';
import { tenants } from './platform.js';

const money = { precision: 20, scale: 4, mode: 'string' as const };
export const parties = pgTable('parties', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), code: text('code').notNull(), kind: text('kind').notNull(), name: text('name').notNull(), legalName: text('legal_name'), taxNo: text('tax_no'), nationalId: text('national_id'), address: jsonb('address').$type<Record<string, string | undefined>>(), phone: text('phone'), email: text('email'), receivableAccountId: uuid('receivable_account_id').references(() => accounts.id, { onDelete: 'restrict' }), payableAccountId: uuid('payable_account_id').references(() => accounts.id, { onDelete: 'restrict' }), creditLimit: numeric('credit_limit', money).notNull().default('0'), isOwner: boolean('is_owner').notNull().default(false), isContractor: boolean('is_contractor').notNull().default(false), branchId: uuid('branch_id').references(() => branches.id, { onDelete: 'restrict' }), ...baseAuditColumns(), ...baseSoftDeleteColumns(),
}, (table) => ({ codeKey: uniqueIndex('parties_tenant_code_key').on(table.tenantId, table.code).where(sql`deleted_at IS NULL`), taxIdx: index('parties_tenant_tax_idx').on(table.tenantId, table.taxNo), kindIdx: index('parties_tenant_kind_idx').on(table.tenantId, table.kind) }));
export const partyContacts = pgTable('party_contacts', { partyId: uuid('party_id').notNull().references(() => parties.id, { onDelete: 'cascade' }), id: uuid('id').notNull(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), name: text('name').notNull(), role: text('role'), phone: text('phone'), email: text('email'), isPrimary: boolean('is_primary').notNull().default(false), ...baseAuditColumns(), ...baseSoftDeleteColumns() }, (table) => ({ pk: primaryKey({ columns: [table.partyId, table.id] }), partyIdx: index('party_contacts_tenant_party_idx').on(table.tenantId, table.partyId) }));
export const paymentAllocations = pgTable('payment_allocations', { id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), partyId: uuid('party_id').notNull().references(() => parties.id, { onDelete: 'restrict' }), voucherId: uuid('voucher_id'), invoiceKind: text('invoice_kind').notNull(), invoiceId: uuid('invoice_id').notNull(), amount: numeric('amount', money).notNull(), allocatedAt: timestamp('allocated_at', { withTimezone: true }).notNull().defaultNow(), createdBy: uuid('created_by') }, (table) => ({ invoiceIdx: index('payment_allocations_invoice_idx').on(table.tenantId, table.invoiceKind, table.invoiceId), partyIdx: index('payment_allocations_party_idx').on(table.tenantId, table.partyId) }));
export type Party = typeof parties.$inferSelect;
export type PartyContact = typeof partyContacts.$inferSelect;
export type PaymentAllocation = typeof paymentAllocations.$inferSelect;
export type PartyAddress = NonNullable<Party['address']>;
export const partyAddressKeys = ['country', 'city', 'district', 'street', 'building', 'postalCode', 'additionalNumber'] as const;
