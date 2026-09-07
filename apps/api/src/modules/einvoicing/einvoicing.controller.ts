import { Body, Controller, Get, Param, Post, Put, Query } from '@nestjs/common';

import { getTenantContext } from '../platform/context/tenant-context.js';
import { RequiresPermission } from '../platform/decorators/requires-permission.decorator.js';

import { EinvoicingService, type CredentialInput } from './einvoicing.service.js';

@Controller()
export class EinvoicingController {
  constructor(private readonly einvoicing: EinvoicingService) {}
  @Put('einvoice/credentials') @RequiresPermission('einvoice.credentials.manage') putCredentials(@Body() body: CredentialInput) { return this.einvoicing.upsertCredentials(getTenantContext().tenantId, body); }
  @Get('einvoice/credentials') @RequiresPermission('einvoice.view') credentials() { return this.einvoicing.listCredentials(getTenantContext().tenantId); }
  @Get('einvoice/submissions') @RequiresPermission('einvoice.view') submissions(@Query('status') status?: string) { return this.einvoicing.submissions(getTenantContext().tenantId, status); }
  @Post('einvoice/submissions/:id/retry') @RequiresPermission('einvoice.submit') retry(@Param('id') id: string) { return this.einvoicing.retry(getTenantContext().tenantId, id); }
  @Post('sales-invoices/:id/einvoice/submit') @RequiresPermission('einvoice.submit') submitInvoice(@Param('id') id: string, @Body() body: { authority?: 'zatca' | 'eta'; environment?: 'simulation' | 'production' }) { return this.einvoicing.submitSalesInvoice(getTenantContext().tenantId, id, body.authority, body.environment); }
  @Get('einvoice/health') @RequiresPermission('einvoice.view') health() { return this.einvoicing.health(getTenantContext().tenantId); }
}
