import { Body, Controller, Delete, Get, Param, Patch, Post, Query } from '@nestjs/common';

import { getTenantContext } from '../platform/context/tenant-context.js';
import { RequiresPermission } from '../platform/decorators/requires-permission.decorator.js';

import { SalesService, type PaymentInput, type PostingInput, type SalesInvoiceInput } from './sales.service.js';

@Controller()
export class SalesController {
  constructor(private readonly sales: SalesService) {}
  @Get('sales/invoices') @RequiresPermission('sales.view') list() { return this.sales.list(getTenantContext().tenantId); }
  @Get('sales/invoices/:id') @RequiresPermission('sales.view') get(@Param('id') id: string) { return this.sales.get(getTenantContext().tenantId, id); }
  @Get('sales/invoices/:id/print-data') @RequiresPermission('sales.view') printData(@Param('id') id: string) { return this.sales.printData(getTenantContext().tenantId, id); }
  @Post('sales/invoices') @RequiresPermission('sales.invoice.create') create(@Body() body: SalesInvoiceInput) { return this.sales.create(getTenantContext().tenantId, body); }
  @Patch('sales/invoices/:id') @RequiresPermission('sales.invoice.create') update(@Param('id') id: string, @Body() body: Partial<SalesInvoiceInput>) { return this.sales.updateDraft(getTenantContext().tenantId, id, body); }
  @Post('sales/invoices/:id/post') @RequiresPermission('sales.invoice.post') post(@Param('id') id: string, @Body() body: PostingInput) { return this.sales.post(getTenantContext().tenantId, id, body); }
  @Post('sales/invoices/:id/void') @RequiresPermission('sales.invoice.void') void(@Param('id') id: string, @Body() body: { reason: string }) { return this.sales.void(getTenantContext().tenantId, id, body.reason); }
  @Post('sales/invoices/:id/payments') @RequiresPermission('sales.invoice.pay') payment(@Param('id') id: string, @Body() body: PaymentInput) { return this.sales.addPayment(getTenantContext().tenantId, id, body); }
  @Post('sales/invoices/:id/return') @RequiresPermission('sales.return.create') returnFrom(@Param('id') id: string, @Body() body: Omit<SalesInvoiceInput, 'kind'>) { return this.sales.returnFrom(getTenantContext().tenantId, id, body); }
  @Post('sales/invoices/:id/adjustment-notes') @RequiresPermission('sales.adjustment.create') note(@Param('id') id: string, @Body() body: { branchId: string; kind: string; reason: string; amount: string }) { return this.sales.createAdjustmentNote(getTenantContext().tenantId, id, body); }
  @Get('sales/adjustment-notes') @RequiresPermission('sales.view') notes(@Query('kind') kind?: string) { return this.sales.listAdjustmentNotes(getTenantContext().tenantId, kind); }
  @Post('sales/adjustment-notes/:id/post') @RequiresPermission('sales.invoice.post') postNote(@Param('id') id: string) { return this.sales.postAdjustmentNote(getTenantContext().tenantId, id); }
  @Post('sales/offers/:id/evaluate') @RequiresPermission('sales.view') evaluateOffer(@Param('id') id: string, @Body() body: { itemId: string; quantity: string; value: string }) { return this.sales.evaluateOffer(getTenantContext().tenantId, id, body); }
  @Get('sales/quotations') @RequiresPermission('sales.view') quotations() { return this.sales.listQuotations(getTenantContext().tenantId); }
  @Post('sales/quotations') @RequiresPermission('sales.invoice.create') createQuotation(@Body() body: Parameters<SalesService['createQuotation']>[1]) { return this.sales.createQuotation(getTenantContext().tenantId, body); }
  @Post('sales/quotations/:id/convert') @RequiresPermission('sales.invoice.create') convertQuotation(@Param('id') id: string, @Body() body: { warehouseId?: string }) { return this.sales.convertQuotation(getTenantContext().tenantId, id, body ?? {}); }
  @Get('sales/offers') @RequiresPermission('sales.view') offers() { return this.sales.listOffers(getTenantContext().tenantId); }
  @Post('sales/offers') @RequiresPermission('sales.offer.manage') createOffer(@Body() body: Parameters<SalesService['createOffer']>[1]) { return this.sales.createOffer(getTenantContext().tenantId, body); }
  @Get('sales/salesmen') @RequiresPermission('sales.view') salesmen() { return this.sales.listSalesmen(getTenantContext().tenantId); }
  @Post('sales/salesmen') @RequiresPermission('sales.salesman.manage') createSalesman(@Body() body: { name: string; employeeRef?: string; active?: boolean }) { return this.sales.createSalesman(getTenantContext().tenantId, body); }
  @Patch('sales/salesmen/:id') @RequiresPermission('sales.salesman.manage') updateSalesman(@Param('id') id: string, @Body() body: { name?: string; employeeRef?: string | null; active?: boolean }) { return this.sales.updateSalesman(getTenantContext().tenantId, id, body); }
  @Delete('sales/salesmen/:id') @RequiresPermission('sales.salesman.manage') deleteSalesman(@Param('id') id: string) { return this.sales.deleteSalesman(getTenantContext().tenantId, id); }
}
