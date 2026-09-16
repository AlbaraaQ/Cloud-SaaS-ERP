import { Body, Controller, Get, Param, Post, Put, Query } from '@nestjs/common';

import { getTenantContext } from '../platform/context/tenant-context.js';
import { RequiresPermission } from '../platform/decorators/requires-permission.decorator.js';

import { EinvoicingService, type CredentialInput } from './einvoicing.service.js';
import { ZatcaOnboardingService, type SettingsInput } from './zatca-onboarding.service.js';

@Controller()
export class EinvoicingController {
  constructor(
    private readonly einvoicing: EinvoicingService,
    private readonly onboarding: ZatcaOnboardingService,
  ) {}

  // ── ⚙️ إعدادات الربط الضريبي - زاتكا ZATCA (`frmZatcaSetting.xaml`) ──────────────────

  @Get('einvoice/settings') @RequiresPermission('einvoice.view')
  settings() { return this.onboarding.view(getTenantContext().tenantId); }

  /** 💾 حفظ الإعدادات — Save Settings. */
  @Put('einvoice/settings') @RequiresPermission('einvoice.manage')
  saveSettings(@Body() body: SettingsInput) { return this.onboarding.saveSettings(getTenantContext().tenantId, body ?? {}); }

  /** 🔄 تعبئة تلقائي — fills the CSR properties from بطاقة المنشأة. */
  @Post('einvoice/settings/fill-from-company') @RequiresPermission('einvoice.manage')
  fillFromCompany() { return this.onboarding.fillFromCompany(getTenantContext().tenantId); }

  /** ⚡ توليد — Generate. The private key is returned once and stored encrypted. */
  @Post('einvoice/csr/generate') @RequiresPermission('einvoice.credentials.manage')
  generateCsr() { return this.onboarding.generateCsr(getTenantContext().tenantId); }

  /** 🔵 Compliance CSID — needs the 🔑 OTP. */
  @Post('einvoice/onboarding/compliance-csid') @RequiresPermission('einvoice.credentials.manage')
  complianceCsid(@Body() body: { otp?: string }) { return this.onboarding.requestComplianceCsid(getTenantContext().tenantId, body ?? {}); }

  /** 🔐 حفظ مفتاح التشفير — Get PCSID. */
  @Post('einvoice/onboarding/production-csid') @RequiresPermission('einvoice.credentials.manage')
  productionCsid() { return this.onboarding.requestProductionCsid(getTenantContext().tenantId); }

  /** 🔄 Renews CSID — تجديد الشهادة بعد 5 سنوات. */
  @Post('einvoice/onboarding/renew') @RequiresPermission('einvoice.credentials.manage')
  renewCsid() { return this.onboarding.renewCsid(getTenantContext().tenantId); }

  /** 🧪 اختبار الربط — Test Compliance: the six documents. */
  @Post('einvoice/onboarding/compliance-check') @RequiresPermission('einvoice.credentials.manage')
  complianceCheck() { return this.onboarding.complianceCheck(getTenantContext().tenantId); }

  /** ⏸ إيقاف الربط / ▶ تشغيل. */
  @Post('einvoice/link/toggle') @RequiresPermission('einvoice.manage')
  toggleLink() { return this.onboarding.toggleLink(getTenantContext().tenantId); }
  @Put('einvoice/credentials') @RequiresPermission('einvoice.credentials.manage') putCredentials(@Body() body: CredentialInput) { return this.einvoicing.upsertCredentials(getTenantContext().tenantId, body); }
  @Get('einvoice/credentials') @RequiresPermission('einvoice.view') credentials() { return this.einvoicing.listCredentials(getTenantContext().tenantId); }
  @Get('einvoice/submissions') @RequiresPermission('einvoice.view') submissions(@Query('status') status?: string) { return this.einvoicing.submissions(getTenantContext().tenantId, status); }
  @Post('einvoice/submissions/:id/retry') @RequiresPermission('einvoice.submit') retry(@Param('id') id: string) { return this.einvoicing.retry(getTenantContext().tenantId, id); }
  @Post('sales-invoices/:id/einvoice/submit') @RequiresPermission('einvoice.submit') submitInvoice(@Param('id') id: string, @Body() body: { authority?: 'zatca' | 'eta'; environment?: 'simulation' | 'production' }) { return this.einvoicing.submitSalesInvoice(getTenantContext().tenantId, id, body.authority, body.environment); }
  @Get('einvoice/health') @RequiresPermission('einvoice.view') health() { return this.einvoicing.health(getTenantContext().tenantId); }
}
