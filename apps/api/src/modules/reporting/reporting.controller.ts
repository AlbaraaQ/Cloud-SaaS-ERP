import { Body, Controller, Get, Param, Post, Query } from '@nestjs/common';

import { getTenantContext } from '../platform/context/tenant-context.js';
import { RequiresPermission } from '../platform/decorators/requires-permission.decorator.js';

import { ReportingService } from './reporting.service.js';

@Controller('reports')
export class ReportingController {
  constructor(private readonly reporting: ReportingService) {}
  @Get() @RequiresPermission('reporting.view') catalog() { return this.reporting.catalog(); }
  @Get(':key') @RequiresPermission('reporting.view') run(@Param('key') key: string, @Query() query: Record<string, string | undefined>) { return this.reporting.run(getTenantContext().tenantId, key, query); }
  @Post(':key/export') @RequiresPermission('reporting.export.execute') export(@Param('key') key: string, @Query() query: Record<string, string | undefined>, @Body() body: { format?: 'csv' | 'xlsx' | 'pdf' }) { return this.reporting.export(getTenantContext().tenantId, key, query, body.format); }
  @Get('print/invoices/:id') @RequiresPermission('reporting.view') invoicePrint(@Param('id') id: string) { return { html: this.reporting.invoicePrintHtml(id) }; }
  @Get('print/shifts/:id') @RequiresPermission('reporting.view') shiftPrint(@Param('id') id: string) { return { html: this.reporting.shiftPrintHtml(id) }; }
}
