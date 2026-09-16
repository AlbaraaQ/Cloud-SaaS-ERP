import { apiFetch } from './api';

/**
 * ⚙️ إعدادات الربط الضريبي - زاتكا ZATCA — the client behind
 * `Desktop_ERP/SmartAuditERP/Form_WPF/frmZatcaSetting.xaml`.
 *
 * Every function here is one button of that window; the shape of each payload is the
 * shape of the `SettingZatca` / `CSRProperties` / `ZatcaCredential` rows it wrote.
 */

export type CsrProperties = {
  commonName: string;
  serialNumber: string;
  organizationIdentifier: string;
  organizationUnitName: string;
  organizationName: string;
  countryName: string;
  invoiceType: string;
  address: string;
  industry: string;
};

export type ComplianceCheckRow = {
  key: string;
  labelEn: string;
  labelAr: string;
  invoiceTypeCode: string;
  typeName: string;
  profile: 'standard' | 'simplified';
  clearance: boolean;
  success: boolean;
  status: string;
  hash: string;
  messages: string[];
};

export type ChecklistRow = { key: string; labelAr: string; done: boolean; at: string | null };

export type ZatcaView = {
  authority: string;
  settings: { environment: 'compliance' | 'production'; simulation: boolean; active: boolean; syncManual: boolean; startDate: string; endDate: string };
  csr: CsrProperties;
  credential: {
    environment: string;
    hasCsr: boolean;
    csr: string | null;
    hasPrivateKey: boolean;
    privateKeyMasked: string | null;
    csidMasked: string | null;
    secretMasked: string | null;
    requestId: string | null;
    productionCsidMasked: string | null;
    productionSecretMasked: string | null;
    productionRequestId: string | null;
    validFrom: string | null;
    validTo: string | null;
    updatedAt: string | null;
  };
  onboarding: {
    csrGeneratedAt: string | null;
    complianceCsidAt: string | null;
    productionCsidAt: string | null;
    complianceCheckedAt: string | null;
    renewedAt: string | null;
    lastComplianceCheck: { passed: boolean; checkedAt: string; checks: ComplianceCheckRow[] } | null;
  };
  checklist: ChecklistRow[];
  link: { active: boolean; canToggle: boolean };
};

export type SettingsInput = {
  environment?: 'compliance' | 'production';
  simulation?: boolean;
  active?: boolean;
  syncManual?: boolean;
  startDate?: string;
  csr?: Partial<CsrProperties>;
};

export type GeneratedCsr = {
  csr: string;
  privateKey: string;
  publicKey: string;
  serialNumber: string;
  fingerprint: string;
  revokedCredentials: boolean;
  message: string;
};

export type CsrGrant = {
  requestId: string;
  csid: string;
  secret: string;
  gateway: 'simulation' | 'http';
  environment: string;
  renewedAt?: string;
  message: string;
};

export type ComplianceRun = {
  passed: boolean;
  checkedAt: string;
  gateway: 'simulation' | 'http';
  checks: ComplianceCheckRow[];
  message: string;
};

export const zatcaView = () => apiFetch<ZatcaView>('/einvoice/settings');

export const saveZatcaSettings = (input: SettingsInput) =>
  apiFetch<{ settings: ZatcaView['settings']; message: string }>('/einvoice/settings', { method: 'PUT', body: JSON.stringify(input) });

export const fillCsrFromCompany = () =>
  apiFetch<{ settings: ZatcaView & { csr: CsrProperties }; warnings: string[]; message: string }>('/einvoice/settings/fill-from-company', { method: 'POST', body: '{}' });

export const generateCsr = () => apiFetch<GeneratedCsr>('/einvoice/csr/generate', { method: 'POST', body: '{}' });

export const requestComplianceCsid = (otp: string) =>
  apiFetch<CsrGrant>('/einvoice/onboarding/compliance-csid', { method: 'POST', body: JSON.stringify({ otp }) });

export const requestProductionCsid = () => apiFetch<CsrGrant>('/einvoice/onboarding/production-csid', { method: 'POST', body: '{}' });

export const renewCsid = () => apiFetch<CsrGrant>('/einvoice/onboarding/renew', { method: 'POST', body: '{}' });

export const runComplianceCheck = () => apiFetch<ComplianceRun>('/einvoice/onboarding/compliance-check', { method: 'POST', body: '{}' });

export const toggleZatcaLink = () =>
  apiFetch<{ active: boolean; message: string }>('/einvoice/link/toggle', { method: 'POST', body: '{}' });
