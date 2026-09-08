import { createCipheriv, createDecipheriv, createHash, randomBytes } from 'node:crypto';

import { Inject, Injectable, Logger } from '@nestjs/common';
import { and, asc, desc, eq, sql } from 'drizzle-orm';
import { DomainError, newId } from '@erp/contracts';
import { einvoiceChain, einvoiceCredentials, einvoiceSubmissions, salesInvoiceLines, salesInvoices, withTenantTx, type DatabaseHandle, type DrizzleTx } from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';

import { buildQrPayload, hashInvoiceXml, signInvoiceHash } from './zatca/qr.js';
import { GENESIS_PIH, buildInvoiceXml, type ZatcaLine, type ZatcaParty } from './zatca/ubl.js';

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

@Injectable()
export class EinvoicingService {
  private readonly logger = new Logger(EinvoicingService.name);

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

  /**
   * Prepares — and, when the tenant is onboarded, files — the ZATCA e-invoice for a posted
   * sales invoice.
   *
   * The document is always produced: UBL 2.1 XML from the real invoice, the hash chained onto
   * the previous one, the invoice counter, and the QR. What varies is how far it gets:
   *
   * | tenant state | submission status | invoice `zatca_status` |
   * |---|---|---|
   * | no credentials uploaded | `prepared` | `prepared` |
   * | key uploaded, no gateway configured | `signed` | `signed` |
   * | gateway configured | `reported` / `cleared` / `failed` | same |
   *
   * The phase-1 QR (tags 1–5) is valid in every one of those states, so an invoice printed
   * from a tenant that has not finished onboarding still carries a scannable code. What it
   * never does is claim to have been accepted by an authority it never spoke to.
   */
  async submitSalesInvoice(tenantId: string, invoiceId: string, authority: 'zatca' | 'eta' = 'zatca', environment: 'simulation' | 'production' = 'simulation') {
    if (authority === 'eta') return this.createEtaStub(tenantId, invoiceId, environment);

    const prepared = await withTenantTx(this.database.db, tenantId, async (tx) => {
      const [invoice] = await tx.select().from(salesInvoices).where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, invoiceId)));
      if (!invoice || invoice.status !== 'posted') throw new DomainError('EINVOICE_INVOICE_NOT_POSTED', 'Only posted invoices can be submitted', 409);

      const document = await this.buildDocument(tx, tenantId, invoice, authority, environment);
      const credential = await this.credentialFor(tx, tenantId, authority, environment);
      const privateKey = credential?.privateKeyEnc ? safeDecrypt(credential.privateKeyEnc) : null;
      const signed = privateKey ? signInvoiceHash(document.invoiceHash, privateKey) : null;
      if (privateKey && !signed) this.logger.warn(`ZATCA private key for tenant ${tenantId} (${environment}) could not sign — filing unsigned`);

      const qrPayload = buildQrPayload({
        sellerName: document.seller.nameAr,
        vatNo: document.seller.vatNo ?? '',
        timestamp: document.issuedAt.toISOString().replace(/\.\d{3}Z$/, 'Z'),
        grandTotal: Number(invoice.total ?? 0).toFixed(2),
        vatTotal: Number(invoice.taxTotal ?? 0).toFixed(2),
        invoiceHash: signed ? document.invoiceHash : undefined,
        signature: signed?.signature,
        publicKeyDer: signed?.publicKeyDer,
      });

      const status = signed ? 'signed' : 'prepared';
      const submissionUuid = newId();
      const [submission] = await tx
        .insert(einvoiceSubmissions)
        .values({
          id: newId(),
          tenantId,
          invoiceId,
          authority,
          environment,
          status,
          uuid: submissionUuid,
          hash: document.invoiceHash,
          previousHash: document.previousHash,
          qrPayload,
          requestPayload: { xml: document.xml, counter: document.counter, profile: document.profile },
          response: {
            submitted: false,
            reason: signed ? 'NO_GATEWAY_CONFIGURED' : 'NO_CREDENTIALS',
            signed: Boolean(signed),
            curve: signed?.curve ?? null,
            phase1Qr: true,
          },
          attempts: '0',
        })
        .returning();
      if (!submission) throw new DomainError('EINVOICE_SUBMISSION_FAILED', 'Submission insert returned no row', 500);

      await tx
        .update(salesInvoices)
        .set({ zatcaUuid: submissionUuid, zatcaHash: document.invoiceHash, zatcaQr: qrPayload, zatcaStatus: status, updatedAt: new Date() })
        .where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, invoiceId)));

      return { submission, xml: document.xml, credential };
    });

    return this.fileWithAuthority(tenantId, prepared.submission, prepared.xml, prepared.credential);
  }

  /**
   * Hands the prepared document to the authority's gateway when one is configured.
   *
   * Nothing here is mocked: with no `ZATCA_API_BASE_URL` and no CSID the method returns the
   * prepared submission untouched, which is the honest description of what happened.
   */
  private async fileWithAuthority(
    tenantId: string,
    submission: typeof einvoiceSubmissions.$inferSelect,
    xml: string,
    credential: typeof einvoiceCredentials.$inferSelect | null,
  ) {
    const baseUrl = (process.env.ZATCA_API_BASE_URL ?? '').replace(/\/$/, '');
    const csid = credential?.csidEnc ? safeDecrypt(credential.csidEnc) : null;
    const secret = credential?.secretEnc ? safeDecrypt(credential.secretEnc) : null;
    if (!baseUrl || !csid || !secret) return submission;

    const clearance = String((credential?.org as Record<string, unknown> | undefined)?.invoiceProfile ?? '') === 'standard';
    const endpoint = `${baseUrl}/invoices/${clearance ? 'clearance' : 'reporting'}/single`;
    const started = Date.now();
    try {
      const response = await fetch(endpoint, {
        method: 'POST',
        headers: {
          'content-type': 'application/json',
          'accept-language': 'en',
          'accept-version': 'V2',
          ...(clearance ? { 'clearance-status': '1' } : {}),
          authorization: `Basic ${Buffer.from(`${csid}:${secret}`).toString('base64')}`,
        },
        body: JSON.stringify({ invoiceHash: submission.hash, uuid: submission.uuid, invoice: Buffer.from(xml, 'utf8').toString('base64') }),
        signal: AbortSignal.timeout(20_000),
      });
      const payload = (await response.json().catch(() => ({}))) as Record<string, unknown>;
      const accepted = response.status >= 200 && response.status < 300;
      const status = accepted ? (clearance ? 'cleared' : 'reported') : 'failed';
      return await this.recordOutcome(tenantId, submission.id, submission.invoiceId, {
        status,
        response: { submitted: true, httpStatus: response.status, endpoint, durationMs: Date.now() - started, payload },
        error: accepted ? null : `ZATCA responded ${response.status}`,
      });
    } catch (error) {
      return await this.recordOutcome(tenantId, submission.id, submission.invoiceId, {
        status: 'failed',
        response: { submitted: true, endpoint, durationMs: Date.now() - started },
        error: error instanceof Error ? error.message : String(error),
      });
    }
  }

  private async recordOutcome(tenantId: string, submissionId: string, invoiceId: string, outcome: { status: string; response: Record<string, unknown>; error: string | null }) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [row] = await tx
        .update(einvoiceSubmissions)
        .set({ status: outcome.status, response: outcome.response, error: outcome.error, attempts: sql`(attempts::int + 1)::text`, submittedAt: new Date(), updatedAt: new Date() })
        .where(and(eq(einvoiceSubmissions.tenantId, tenantId), eq(einvoiceSubmissions.id, submissionId)))
        .returning();
      await tx.update(salesInvoices).set({ zatcaStatus: outcome.status, updatedAt: new Date() }).where(and(eq(salesInvoices.tenantId, tenantId), eq(salesInvoices.id, invoiceId)));
      return row!;
    });
  }

  /** Assembles the UBL document, its place in the hash chain and its counter value. */
  private async buildDocument(
    tx: DrizzleTx,
    tenantId: string,
    invoice: typeof salesInvoices.$inferSelect,
    authority: string,
    environment: string,
  ) {
    const lines = await tx.select().from(salesInvoiceLines).where(and(eq(salesInvoiceLines.tenantId, tenantId), eq(salesInvoiceLines.invoiceId, invoice.id))).orderBy(asc(salesInvoiceLines.lineNo));
    const names = await tx.execute(sql`
      SELECT l.id AS line_id, COALESCE(l.description, i.name_ar) AS label, u.code AS unit_code
        FROM sales_invoice_lines l
        LEFT JOIN items i ON i.id = l.item_id
        LEFT JOIN units_of_measure u ON u.id = i.base_unit_id
       WHERE l.tenant_id = ${tenantId} AND l.invoice_id = ${invoice.id}
    `);
    const labels = new Map<string, { label: string; unitCode: string }>();
    for (const row of rowsOf(names)) labels.set(String(row.line_id), { label: String(row.label ?? 'صنف'), unitCode: String(row.unit_code ?? 'PCE') });

    const seller = await this.sellerParty(tx, tenantId);
    const buyer = await this.buyerParty(tx, tenantId, invoice);
    const { previousHash, counter } = await this.nextChainSlot(tx, tenantId, authority, environment);

    const documentLines: ZatcaLine[] = lines.map((line) => ({
      lineNo: line.lineNo,
      name: labels.get(line.id)?.label ?? line.description ?? 'صنف',
      unitCode: labels.get(line.id)?.unitCode ?? 'PCE',
      quantity: line.quantity,
      unitPrice: line.unitPrice,
      net: line.net,
      discount: line.discountAmount,
      tax: line.tax,
      taxRate: line.taxRate,
    }));

    const profile: 'standard' | 'simplified' = buyer?.vatNo ? 'standard' : 'simplified';
    const issuedAt = invoice.postedAt ?? invoice.createdAt ?? new Date();
    const xml = buildInvoiceXml({
      number: invoice.number ?? invoice.id,
      uuid: invoice.id,
      issuedAt,
      kind: invoice.kind,
      profile,
      currency: invoice.currency || 'SAR',
      seller,
      buyer: buyer ?? undefined,
      lines: documentLines,
      invoiceDiscount: invoice.invoiceDiscount,
      subtotal: invoice.subtotal,
      taxTotal: invoice.taxTotal,
      total: invoice.total,
      paidTotal: invoice.paidTotal,
      paymentMeansCode: Number(invoice.paidTotal ?? 0) > 0 ? '10' : '30',
      counter,
      previousHash,
    });
    const invoiceHash = hashInvoiceXml(xml);
    // The chain moves forward only once the document exists: the next invoice's PIH is this
    // hash, and its ICV is this counter plus one.
    await tx
      .update(einvoiceChain)
      .set({ lastHash: invoiceHash, counter, updatedAt: new Date() })
      .where(and(eq(einvoiceChain.tenantId, tenantId), eq(einvoiceChain.authority, authority), eq(einvoiceChain.environment, environment)));
    return { xml, invoiceHash, previousHash, counter, profile, seller, buyer, issuedAt };
  }

  private async sellerParty(tx: DrizzleTx, tenantId: string): Promise<ZatcaParty> {
    const profile = rowsOf(await tx.execute(sql`SELECT name_ar, name_en, tax_no, cr_no, address FROM company_profiles WHERE tenant_id = ${tenantId}`))[0];
    const tenant = rowsOf(await tx.execute(sql`SELECT name FROM tenants WHERE id = ${tenantId}`))[0];
    const address = (profile?.address ?? {}) as Record<string, string | undefined>;
    return {
      nameAr: String(profile?.name_ar ?? tenant?.name ?? 'المنشأة'),
      nameEn: profile?.name_en ? String(profile.name_en) : undefined,
      vatNo: profile?.tax_no ? String(profile.tax_no) : undefined,
      crNo: profile?.cr_no ? String(profile.cr_no) : undefined,
      ...nationalAddress(address),
    };
  }

  private async buyerParty(tx: DrizzleTx, tenantId: string, invoice: typeof salesInvoices.$inferSelect): Promise<ZatcaParty | null> {
    if (!invoice.partyId) return invoice.cashCustomerName ? { nameAr: invoice.cashCustomerName } : null;
    const party = rowsOf(await tx.execute(sql`SELECT name, tax_no, address FROM parties WHERE tenant_id = ${tenantId} AND id = ${invoice.partyId}`))[0];
    if (!party) return null;
    const address = (party.address ?? {}) as Record<string, string | undefined>;
    return {
      nameAr: String(party.name ?? ''),
      vatNo: party.tax_no ? String(party.tax_no) : undefined,
      ...nationalAddress(address),
    };
  }

  /**
   * Reserves the next link in the chain: the previous invoice's hash and the next counter,
   * taken under `FOR UPDATE` so two invoices filed at once cannot share an ICV.
   */
  private async nextChainSlot(tx: DrizzleTx, tenantId: string, authority: string, environment: string) {
    await tx.execute(sql`INSERT INTO einvoice_chain (tenant_id, authority, environment, last_hash, counter, updated_at) VALUES (${tenantId}, ${authority}, ${environment}, '', 0, now()) ON CONFLICT (tenant_id, authority, environment) DO NOTHING`);
    const [chain] = await tx.select().from(einvoiceChain).where(and(eq(einvoiceChain.tenantId, tenantId), eq(einvoiceChain.authority, authority), eq(einvoiceChain.environment, environment))).for('update');
    return { previousHash: chain?.lastHash || GENESIS_PIH, counter: Number(chain?.counter ?? 0) + 1 };
  }

  /** Re-files a submission that failed. The document is not rebuilt — the hash must not move. */
  async retry(tenantId: string, submissionId: string) {
    const context = await withTenantTx(this.database.db, tenantId, async (tx) => {
      const [row] = await tx.select().from(einvoiceSubmissions).where(and(eq(einvoiceSubmissions.tenantId, tenantId), eq(einvoiceSubmissions.id, submissionId)));
      if (!row) throw new DomainError('EINVOICE_SUBMISSION_NOT_FOUND', 'Submission was not found', 404);
      if (row.authority === 'eta') throw new DomainError('ETA_NOT_IMPLEMENTED', 'ETA retry path is disabled', 501);
      const credential = await this.credentialFor(tx, tenantId, row.authority, row.environment);
      return { row, credential };
    });
    const xml = String((context.row.requestPayload as Record<string, unknown>).xml ?? '');
    return this.fileWithAuthority(tenantId, context.row, xml, context.credential);
  }

  private async createEtaStub(tenantId: string, invoiceId: string, environment: 'simulation' | 'production') { const [row] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(einvoiceSubmissions).values({ id: newId(), tenantId, invoiceId, authority: 'eta', environment, status: 'not_implemented', error: 'ETA adapter is stubbed until certification scope is approved' }).returning()); return row; }
  private async credentialFor(tx: DrizzleTx, tenantId: string, authority: string, environment: string) {
    const [row] = await tx
      .select()
      .from(einvoiceCredentials)
      .where(and(eq(einvoiceCredentials.tenantId, tenantId), eq(einvoiceCredentials.authority, authority), eq(einvoiceCredentials.environment, environment)));
    return row ?? null;
  }

  private maskCredential(row: typeof einvoiceCredentials.$inferSelect) { return { ...row, privateKeyEnc: undefined, csidEnc: undefined, secretEnc: undefined, privateKeyMasked: maskEncrypted(row.privateKeyEnc), csidMasked: maskEncrypted(row.csidEnc), secretMasked: maskEncrypted(row.secretEnc) }; }
}

/**
 * Maps our national-address shape (`plot`, `building`, `street`, `district`, `city`, `postal`)
 * onto the UBL element names ZATCA expects.
 */
function nationalAddress(address: Record<string, string | undefined>) {
  return {
    street: address.street,
    buildingNumber: address.building ?? address.plot,
    district: address.district,
    city: address.city,
    postalZone: address.postal,
    countryCode: address.countryCode ?? 'SA',
  };
}

function rowsOf(result: unknown): Array<Record<string, unknown>> {
  return Array.isArray(result) ? (result as Array<Record<string, unknown>>) : ((result as { rows?: Array<Record<string, unknown>> }).rows ?? []);
}

/** A credential that cannot be decrypted must not stop an invoice from being prepared. */
function safeDecrypt(payload: string): string | null {
  try {
    return decryptSecret(payload);
  } catch {
    return null;
  }
}
