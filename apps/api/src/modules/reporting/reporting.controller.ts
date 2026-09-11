import { Body, Controller, Delete, Get, Param, Patch, Post, Query } from '@nestjs/common';

import { getTenantContext } from '../platform/context/tenant-context.js';
import { RequiresPermission } from '../platform/decorators/requires-permission.decorator.js';

import { PrintTemplatesService } from './print-templates.service.js';
import { ReportLayoutsService, type ReportLayoutInput } from './report-layouts.service.js';
import { ReportingService, type ExportFormat } from './reporting.service.js';

@Controller('reports')
export class ReportingController {
  constructor(private readonly reporting: ReportingService, private readonly layouts: ReportLayoutsService, private readonly print: PrintTemplatesService) {}
  @Get() @RequiresPermission('reporting.view') catalog() { return this.reporting.catalog(); }

  // Declared before `:key` on purpose — otherwise the layout routes would be swallowed by
  // the report runner and `/reports/layouts` would look like a report called "layouts".
  @Get('layouts') @RequiresPermission('reporting.view') async listLayouts(@Query('report_key') reportKey?: string) { return { data: await this.layouts.list(getTenantContext().tenantId, reportKey) }; }
  @Post('layouts') @RequiresPermission('reporting.layout.manage') async createLayout(@Body() body: ReportLayoutInput) { return { data: await this.layouts.create(getTenantContext().tenantId, body) }; }
  @Patch('layouts/:id') @RequiresPermission('reporting.layout.manage') async updateLayout(@Param('id') id: string, @Body() body: Partial<ReportLayoutInput>) { return { data: await this.layouts.update(getTenantContext().tenantId, id, body) }; }
  @Delete('layouts/:id') @RequiresPermission('reporting.layout.manage') async deleteLayout(@Param('id') id: string) { return { data: await this.layouts.remove(getTenantContext().tenantId, id) }; }

  // Printable documents. Each returns `{ html }` — a complete, self-contained A4 page the
  // browser can show in an iframe and send straight to the printer.
  @Get('print/invoices/:id') @RequiresPermission('reporting.view') async invoicePrint(@Param('id') id: string) { return { html: await this.print.salesInvoice(getTenantContext().tenantId, id) }; }
  @Get('print/purchase-invoices/:id') @RequiresPermission('reporting.view') async purchaseInvoicePrint(@Param('id') id: string) { return { html: await this.print.purchaseInvoice(getTenantContext().tenantId, id) }; }
  @Get('print/vouchers/:id') @RequiresPermission('reporting.view') async voucherPrint(@Param('id') id: string) { return { html: await this.print.voucher(getTenantContext().tenantId, id) }; }
  @Get('print/journal-entries/:id') @RequiresPermission('reporting.view') async journalPrint(@Param('id') id: string) { return { html: await this.print.journalEntry(getTenantContext().tenantId, id) }; }
  @Get('print/shifts/:id') @RequiresPermission('reporting.view') async shiftPrint(@Param('id') id: string) { return { html: await this.print.shiftClose(getTenantContext().tenantId, id) }; }
  @Get('print/inventory-documents/:id') @RequiresPermission('inventory.view') async inventoryDocumentPrint(@Param('id') id: string) { return { html: await this.print.inventoryDocument(getTenantContext().tenantId, id) }; }
  @Get(':key') @RequiresPermission('reporting.view') run(@Param('key') key: string, @Query() query: Record<string, string | undefined>) { return this.reporting.run(getTenantContext().tenantId, key, query); }
  @Post(':key/export') @RequiresPermission('reporting.export.execute') export(@Param('key') key: string, @Query() query: Record<string, string | undefined>, @Body() body: { format?: ExportFormat }) { return this.reporting.export(getTenantContext().tenantId, key, query, body.format ?? 'csv'); }
}
