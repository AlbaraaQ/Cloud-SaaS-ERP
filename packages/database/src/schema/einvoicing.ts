import { index, jsonb, pgTable, primaryKey, text, timestamp, uniqueIndex, uuid } from 'drizzle-orm/pg-core';

import { baseAuditColumns } from '../columns.js';

import { salesInvoices } from './sales.js';
import { tenants } from './platform.js';

export const einvoiceCredentials = pgTable('einvoice_credentials', {
  id: uuid('id').primaryKey(),
  tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }),
  authority: text('authority').notNull(),
  environment: text('environment').notNull(),
  csr: text('csr'),
  privateKeyEnc: text('private_key_enc'),
  csidEnc: text('csid_enc'),
  secretEnc: text('secret_enc'),
  org: jsonb('org').$type<Record<string, unknown>>().notNull().default({}),
  validFrom: timestamp('valid_from', { withTimezone: true }),
  validTo: timestamp('valid_to', { withTimezone: true }),
  status: text('status').notNull().default('active'),
  ...baseAuditColumns(),
}, (t) => ({ uniqueAuthority: uniqueIndex('einvoice_credentials_tenant_authority_env_key').on(t.tenantId, t.authority, t.environment) }));

export const einvoiceSubmissions = pgTable('einvoice_submissions', {
  id: uuid('id').primaryKey(),
  tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }),
  invoiceId: uuid('invoice_id').notNull().references(() => salesInvoices.id, { onDelete: 'cascade' }),
  authority: text('authority').notNull(),
  action: text('action').notNull().default('submit'),
  environment: text('environment').notNull().default('simulation'),
  status: text('status').notNull().default('pending'),
  uuid: uuid('uuid'),
  hash: text('hash'),
  previousHash: text('previous_hash'),
  qrPayload: text('qr_payload'),
  requestPayload: jsonb('request_payload').$type<Record<string, unknown>>().notNull().default({}),
  response: jsonb('response').$type<Record<string, unknown>>(),
  error: text('error'),
  attempts: text('attempts').notNull().default('0'),
  submittedAt: timestamp('submitted_at', { withTimezone: true }),
  ...baseAuditColumns(),
}, (t) => ({ invoice: index('einvoice_submissions_invoice_idx').on(t.tenantId, t.invoiceId), status: index('einvoice_submissions_status_idx').on(t.tenantId, t.status) }));

export const einvoiceChain = pgTable('einvoice_chain', {
  tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }),
  authority: text('authority').notNull(),
  environment: text('environment').notNull(),
  lastHash: text('last_hash').notNull().default(''),
  updatedAt: timestamp('updated_at', { withTimezone: true }).notNull().defaultNow(),
}, (t) => ({ pk: primaryKey({ columns: [t.tenantId, t.authority, t.environment] }) }));

export const einvoicingTables = { einvoiceCredentials, einvoiceSubmissions, einvoiceChain };
export type EinvoiceCredential = typeof einvoiceCredentials.$inferSelect;
export type EinvoiceSubmission = typeof einvoiceSubmissions.$inferSelect;
