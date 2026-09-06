import { Body, Controller, Get, Param, Patch, Post } from '@nestjs/common';

import { getTenantContext } from '../platform/context/tenant-context.js';
import { RequiresPermission } from '../platform/decorators/requires-permission.decorator.js';

import { SalesService, type SalesInvoiceInput } from './sales.service.js';

@Controller()
export class SalesController {
  constructor(private readonly sales: SalesService) {}
  @Get('sales/invoices') @RequiresPermission('sales.view') list() { return this.sales.list(getTenantContext().tenantId); }
  @Get('sales/invoices/:id') @RequiresPermission('sales.view') get(@Param('id') id: string) { return this.sales.get(getTenantContext().tenantId, id); }
  @Post('sales/invoices') @RequiresPermission('sales.invoice.create') create(@Body() body: SalesInvoiceInput) { return this.sales.create(getTenantContext().tenantId, body); }
  @Patch('sales/invoices/:id') @RequiresPermission('sales.invoice.create') update(@Param('id') id: string, @Body() body: Partial<SalesInvoiceInput>) { return this.sales.updateDraft(getTenantContext().tenantId, id, body); }
  @Post('sales/invoices/:id/post') @RequiresPermission('sales.invoice.post') post(@Param('id') id: string) { return this.sales.post(getTenantContext().tenantId, id); }
  @Post('sales/invoices/:id/payments') @RequiresPermission('sales.invoice.pay') payment(@Param('id') id: string, @Body() body: { method: string; amount: string; idempotencyKey: string; cashLocationId?: string; reference?: string }) { return this.sales.addPayment(getTenantContext().tenantId, id, body); }
}
