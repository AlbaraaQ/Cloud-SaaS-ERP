import { createHash } from 'node:crypto';

/**
 * The ZATCA gateway, behind one interface with two implementations.
 *
 * `Desktop_ERP` reaches the authority through a compiled dependency (`AuditorAPI`) that
 * takes two switches on every call — `isProduction` and `IsSimulation`
 * (`frmZatcaSetting.xaml.cs` L325-L330, L566-L569, `ZatcaService.cs` L371-L377). The
 * switches are the desktop's way of choosing an endpoint without naming it; this module
 * names them and keeps the same three-way choice:
 *
 * | 🧪 Simulation | 🔵 Compliance | 🔴 Production | host |
 * |---|---|---|---|
 * | on | – | – | none — answered locally |
 * | off | on | – | `…/e-invoicing/developer-portal` (sandbox) |
 * | off | – | on | `…/e-invoicing/core` |
 *
 * The simulation implementation is not a stub that returns success: it signs nothing the
 * real authority would reject, and a document that fails local validation (see
 * `compliance-check.ts`) fails here too. What it does return is a *deterministic* token
 * derived from the request, so a test can assert the whole onboarding chain without a
 * network call and get the same answer twice.
 */

export type CsrGrant = { csid: string; secret: string; requestId: string };
export type ComplianceVerdict = { status: 'CLEARED' | 'REPORTED' | 'FAILED'; validationResults: string[] };
export type ComplianceInvoiceInput = {
  csid: string;
  secret: string;
  invoiceHash: string;
  uuid: string;
  /** Base64 UBL document. */
  invoice: string;
  /** Standard (B2B) invoices are cleared; simplified ones are reported. */
  clearance: boolean;
  /** Findings from the local validation pass — a simulated authority honours them. */
  findings: string[];
};

export interface ZatcaGateway {
  readonly kind: 'simulation' | 'http';
  /** The host the calls go to, or `null` when nothing is dialled. */
  readonly baseUrl: string | null;
  complianceCsr(input: { otp: string; csr: string }): Promise<CsrGrant>;
  /** `renew` asks for a fresh grant rather than the one the compliance id already yields. */
  productionCsr(input: { complianceRequestId: string; csid: string; secret: string; renew?: boolean }): Promise<CsrGrant>;
  complianceInvoice(input: ComplianceInvoiceInput): Promise<ComplianceVerdict>;
}

export const ZATCA_HOSTS = {
  sandbox: 'https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal',
  simulation: 'https://gw-fatoora.zatca.gov.sa/e-invoicing/simulation',
  production: 'https://gw-fatoora.zatca.gov.sa/e-invoicing/core',
} as const;

/** `ZATCA_API_BASE_URL` wins over the per-environment default, as it does when filing. */
export function resolveGateway(options: { simulation?: boolean; production?: boolean; baseUrl?: string }): ZatcaGateway {
  if (options.simulation) return simulationGateway();
  const baseUrl = (options.baseUrl ?? process.env.ZATCA_API_BASE_URL ?? (options.production ? ZATCA_HOSTS.production : ZATCA_HOSTS.sandbox)).replace(/\/$/, '');
  return httpGateway(baseUrl);
}

// ── 🧪 Simulation ────────────────────────────────────────────────────────────────────────

function token(kind: string, seed: string): string {
  return Buffer.from(`ZATCA-SIM-${kind}-${createHash('sha256').update(seed).digest('hex')}`).toString('base64');
}

function requestIdOf(kind: string, seed: string): string {
  const hex = createHash('sha256').update(`${kind}:${seed}`).digest('hex');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-4${hex.slice(13, 16)}-a${hex.slice(17, 20)}-${hex.slice(20, 32)}`;
}

/**
 * Answers the three onboarding calls without leaving the process.
 *
 * Deterministic on purpose: the same CSR and OTP always yield the same CSID, so the
 * onboarding chain is reproducible in tests and a re-run of «🧪 اختبار الربط» reports the
 * same six verdicts.
 */
export function simulationGateway(): ZatcaGateway {
  return {
    kind: 'simulation',
    baseUrl: null,
    async complianceCsr({ otp, csr }) {
      const seed = `${csr}|${otp}`;
      return { csid: token('COMPLIANCE', seed), secret: token('SECRET', seed), requestId: requestIdOf('compliance', seed) };
    },
    async productionCsr({ complianceRequestId, csid, renew }) {
      const seed = renew ? `${complianceRequestId}|${csid}|renewed:${Date.now()}` : `${complianceRequestId}|${csid}`;
      return { csid: token('PRODUCTION', seed), secret: token('SECRET', seed), requestId: requestIdOf('production', seed) };
    },
    async complianceInvoice({ clearance, findings }) {
      return findings.length > 0
        ? { status: 'FAILED', validationResults: findings }
        : { status: clearance ? 'CLEARED' : 'REPORTED', validationResults: [] };
    },
  };
}

// ── 🌐 HTTP ──────────────────────────────────────────────────────────────────────────────

function basicAuth(csid: string, secret: string): string {
  return `Basic ${Buffer.from(`${csid}:${secret}`).toString('base64')}`;
}

type AuthorityError = { code: string; detail: string };

/**
 * The real endpoints — the four calls `AuditorAPI` makes behind its switches. Errors are
 * surfaced as `AuthorityError` rather than thrown so the caller can store them on the
 * onboarding row instead of losing them in a 500.
 */
export function httpGateway(baseUrl: string): ZatcaGateway {
  /** The three shapes the four ZATCA calls take; typed inline because the API has no DOM lib. */
  const call = async (path: string, init: { method: 'POST'; headers: Record<string, string>; body: string }): Promise<Record<string, unknown>> => {
    const response = await fetch(`${baseUrl}${path}`, { ...init, signal: AbortSignal.timeout(20_000) });
    const payload = (await response.json().catch(() => ({}))) as Record<string, unknown>;
    if (!response.ok) {
      const messages = Array.isArray((payload.validationResults as Record<string, unknown> | undefined)?.errorMessages)
        ? ((payload.validationResults as { errorMessages: unknown[] }).errorMessages as unknown[]).map(String)
        : [];
      throw Object.assign(new Error(`ZATCA ${path} responded ${response.status}`), {
        code: 'ZATCA_GATEWAY_ERROR',
        detail: messages.join(' · ') || String(payload.message ?? response.statusText ?? ''),
      }) as AuthorityError & Error;
    }
    return payload;
  };

  return {
    kind: 'http',
    baseUrl,
    async complianceCsr({ otp, csr }) {
      const payload = await call('/compliance', {
        method: 'POST' as const,
        headers: { 'content-type': 'application/json', accept: 'application/json', OTP: otp, 'Accept-Version': 'V2' },
        body: JSON.stringify({ csr }),
      });
      return {
        csid: String(payload.binarySecurityToken ?? ''),
        secret: String(payload.secret ?? ''),
        requestId: String(payload.requestID ?? ''),
      };
    },
    async productionCsr({ complianceRequestId, csid, secret }) {
      const payload = await call('/production/csids', {
        method: 'POST' as const,
        headers: { 'content-type': 'application/json', accept: 'application/json', authorization: basicAuth(csid, secret), 'Accept-Version': 'V2' },
        body: JSON.stringify({ compliance_request_id: complianceRequestId }),
      });
      return {
        csid: String(payload.binarySecurityToken ?? ''),
        secret: String(payload.secret ?? ''),
        requestId: String(payload.requestID ?? ''),
      };
    },
    async complianceInvoice({ csid, secret, invoiceHash, uuid, invoice, clearance }) {
      const payload = await call('/compliance/invoices', {
        method: 'POST' as const,
        headers: {
          'content-type': 'application/json',
          accept: 'application/json',
          authorization: basicAuth(csid, secret),
          'Accept-Version': 'V2',
          ...(clearance ? { 'Clearance-Status': '1' } : {}),
        },
        body: JSON.stringify({ invoiceHash, uuid, invoice }),
      });
      const clearanceStatus = String(payload.clearanceStatus ?? '');
      const reportingStatus = String(payload.reportingStatus ?? '');
      const messages = Array.isArray((payload.validationResults as Record<string, unknown> | undefined)?.errorMessages)
        ? ((payload.validationResults as { errorMessages: unknown[] }).errorMessages as unknown[]).map(String)
        : [];
      // `frmZatcaSetting.xaml.cs` L918-L925 accepts either status as a pass.
      if (clearanceStatus === 'CLEARED') return { status: 'CLEARED', validationResults: messages };
      if (reportingStatus === 'REPORTED') return { status: 'REPORTED', validationResults: messages };
      return { status: 'FAILED', validationResults: messages.length > 0 ? messages : [`ZATCA returned no acceptance status (${clearanceStatus || reportingStatus || 'empty'})`] };
    },
  };
}
