import { createCipheriv, createDecipheriv, createHash, randomBytes } from 'node:crypto';

import { Inject, Injectable } from '@nestjs/common';
import { and, desc, eq, sql } from 'drizzle-orm';
import { DomainError, newId } from '@erp/contracts';
import { einvoiceChain, einvoiceCredentials, einvoiceSubmissions, salesInvoices, withTenantTx, type DatabaseHandle, type DrizzleTx } from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';

export type CredentialInput = { authority: 'zatca' | 'eta'; environment: 'simulation' | 'production'; csr?: string; privateKey?: string; csid?: string; secret?: string; org?: Record<string, unknown>; validFrom?: string; validTo?: string };

function keyBytes(secret = process.env.DATA_ENC_KEY ?? 'local-development-data-key'): Buffer {
  return createHash('sha256').update(secret).digest();
}

export function encryptSecret(plain: string, key = process.env.DATA_ENC_KEY): string {
  const iv = randomBytes(12);
  const cipher = createCipheriv('aes-256-gcm', keyBytes(key), iv);
  const encrypted = Buffer.concat([cipher.update(plain, 'utf8'), cipher.final()]);
  const tag = cipher.getAuthTag();
  return `v1:${iv.toString('base64')}:${tag.toString('base64')}:${encrypted.toString('base64')}`;
}

export function decryptSecret(payload: string, key = process.env.DATA_ENC_KEY): string {
  const [, iv64, tag64, data64] = payload.split(':');
  if (!iv64 || !tag64 || !data64) throw new DomainError('SECRET_DECRYPT_FAILED', 'Encrypted secret payload is invalid', 500);
  const decipher = createDecipheriv('aes-256-gcm', keyBytes(key), Buffer.from(iv64, 'base64'));
  decipher.setAuthTag(Buffer.from(tag64, 'base64'));
  return Buffer.concat([decipher.update(Buffer.from(data64, 'base64')), decipher.final()]).toString('utf8');
}

function maskEncrypted(value?: string | null): string | null {
  if (!value) return null;
  let plain = '';
  try { plain = decryptSecret(value); } catch { return '****'; }
  return `****${plain.slice(-4)}`;
}

function escapeXml(value: unknown): string {
  return String(value ?? '').replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;').replaceAll("'", '&apos;');
}

export function buildZatcaUbl(invoice: { id: string; number: string | null; kind: string; total: string; taxTotal: string; postedAt: Date | null }): string {
  return `<Invoice><ID>${escapeXml(invoice.number ?? invoice.id)}</ID><UUID>${escapeXml(invoice.id)}</UUID><InvoiceType>${escapeXml(invoice.kind)}</InvoiceType><IssueDate>${escapeXml(invoice.postedAt?.toISOString().slice(0, 10) ?? new Date().toISOString().slice(0, 10))}</IssueDate><TaxTotal>${escapeXml(invoice.taxTotal)}</TaxTotal><PayableAmount>${escapeXml(invoice.total)}</PayableAmount></Invoice>`;
}

export function buildQrPayload(input: { seller: string; vatNo: string; timestamp: string; total: string; vat: string }): string {
  const values = [input.seller, input.vatNo, input.timestamp, input.total, input.vat];
  return Buffer.concat(values.map((value, index) => Buffer.concat([Buffer.from([index + 1, Buffer.byteLength(value)]), Buffer.from(value)]))).toString('base64');
}

@Injectable()
export class EinvoicingService {
  constructor(@Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle) {}

  async upsertCredentials(tenantId: string, input: CredentialInput) {
    if (input.authority === 'eta') throw new DomainError('ETA_NOT_IMPLEMENTED', 'Egypt ETA adapter is configured as a disabled stub in this phase', 501);
    const values = { id: newId(), tenantId, authority: input.authority, environment: input.environment, csr: input.csr, privateKeyEnc: input.privateKey ? encryptSecret(input.privateKey) : null, csidEnc: input.csid ? encryptSecret(input.csid) : null, secretEnc: input.secret ? encryptSecret(input.secret) : null, org: input.org ?? {}, validFrom: input.validFrom ? new Date(input.validFrom) : null, validTo: input.validTo ? new Date(input.validTo) : null, updatedAt: new Date() };
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(einvoiceCredentials).values(values).onConflictDoUpdate({ target: [einvoiceCredentials.tenantId, einvoiceCredentials.authority, einvoiceCredentials.environment], set: { csr: values.csr, privateKeyEnc: values.privateKeyEnc, csidEnc: values.csidEnc, secretEnc: values.secretEnc, org: values.org, validFrom: values.validFrom, validTo: values.validTo, updatedAt: new Date() } }).returning());
    if (!row) throw new DomainError('EINVOICE_CREDENTIAL_SAVE_FAILED', 'Credential save returned no row', 500);
    return this.maskCredential(row);
  }

  listCredentials(tenantId: string) { return withTenantTx(this.database.db, tenantId, async (tx) => (await tx.select().from(einvoiceCredentials).where(eq(einvoiceCredentials.tenantId, tenantId))).map((row) => this.maskCredential(row))); }
  submissions(tenantId: string, status?: string) { return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(einvoiceSubmissions).where(and(eq(einvoiceSubmissions.tenantId, tenantId), status ? eq(einvoiceSubmissions.status, status) : undefined)).orderBy(desc(einvoiceSubmissions.createdAt)).limit(100)); }
  health(tenantId: string) { return withTenantTx(this.database.db, tenantId, async (tx) => ({ status: 'ok', credentials: (await tx.select().from(einvoiceCredentials).where(eq(einvoiceCredentials.tenantId, tenantId))).length })); }

  async submitSalesInvoice(tenantId: string, invoiceId: string, authority: 'zatca' | 'eta' = 'zatca', environment: 'simulation' | 'production' = 'simulation') {
    if (authority === 'eta') return this.createEtaStub(tenantId, invoiceId, environment);
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [invoice] = await tx.select().from(salesInvoices).where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, invoiceId)));
      if (!invoice || invoice.status !== 'posted') throw new DomainError('EINVOICE_INVOICE_NOT_POSTED', 'Only posted invoices can be submitted', 409);
      const ubl = buildZatcaUbl(invoice);
      const { hash, previousHash } = await this.nextHash(tx, tenantId, authority, environment, ubl);
      const qrPayload = buildQrPayload({ seller: 'Tenant seller', vatNo: '000000000000000', timestamp: new Date().toISOString(), total: invoice.total, vat: invoice.taxTotal });
      const submissionId = newId();
      const [submission] = await tx.insert(einvoiceSubmissions).values({ id: submissionId, tenantId, invoiceId, authority, environment, status: 'reported', uuid: newId(), hash, previousHash, qrPayload, requestPayload: { ubl }, response: { accepted: true, mode: 'mock-sandbox' }, attempts: '1', submittedAt: new Date() }).returning();
      if (!submission) throw new DomainError('EINVOICE_SUBMISSION_FAILED', 'Submission insert returned no row', 500);
      await tx.update(salesInvoices).set({ zatcaUuid: submission.uuid, zatcaHash: hash, zatcaQr: qrPayload, zatcaStatus: 'reported', updatedAt: new Date() }).where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, invoiceId)));
      return submission;
    });
  }

  retry(tenantId: string, submissionId: string) { return withTenantTx(this.database.db, tenantId, async (tx) => { const [row] = await tx.select().from(einvoiceSubmissions).where(and(eq(einvoiceSubmissions.tenantId, tenantId), eq(einvoiceSubmissions.id, submissionId))); if (!row) throw new DomainError('EINVOICE_SUBMISSION_NOT_FOUND', 'Submission was not found', 404); if (row.authority === 'eta') throw new DomainError('ETA_NOT_IMPLEMENTED', 'ETA retry path is disabled', 501); const [updated] = await tx.update(einvoiceSubmissions).set({ status: 'reported', error: null, attempts: String(Number(row.attempts) + 1), response: { accepted: true, retry: true }, submittedAt: new Date(), updatedAt: new Date() }).where(and(eq(einvoiceSubmissions.tenantId, tenantId), eq(einvoiceSubmissions.id, submissionId))).returning(); return updated; }); }

  private async createEtaStub(tenantId: string, invoiceId: string, environment: 'simulation' | 'production') { const [row] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(einvoiceSubmissions).values({ id: newId(), tenantId, invoiceId, authority: 'eta', environment, status: 'not_implemented', error: 'ETA adapter is stubbed until certification scope is approved' }).returning()); return row; }
  private async nextHash(tx: DrizzleTx, tenantId: string, authority: string, environment: string, ubl: string) { await tx.execute(sql`INSERT INTO einvoice_chain (tenant_id, authority, environment, last_hash, updated_at) VALUES (${tenantId}, ${authority}, ${environment}, '', now()) ON CONFLICT (tenant_id, authority, environment) DO NOTHING`); const [chain] = await tx.select().from(einvoiceChain).where(and(eq(einvoiceChain.tenantId, tenantId), eq(einvoiceChain.authority, authority), eq(einvoiceChain.environment, environment))).for('update'); const previousHash = chain?.lastHash ?? ''; const hash = createHash('sha256').update(`${previousHash}:${ubl}`).digest('hex'); await tx.update(einvoiceChain).set({ lastHash: hash, updatedAt: new Date() }).where(and(eq(einvoiceChain.tenantId, tenantId), eq(einvoiceChain.authority, authority), eq(einvoiceChain.environment, environment))); return { hash, previousHash }; }
  private maskCredential(row: typeof einvoiceCredentials.$inferSelect) { return { ...row, privateKeyEnc: undefined, csidEnc: undefined, secretEnc: undefined, privateKeyMasked: maskEncrypted(row.privateKeyEnc), csidMasked: maskEncrypted(row.csidEnc), secretMasked: maskEncrypted(row.secretEnc) }; }
}
