import { Body, Controller, Get, Param, Post, Query } from '@nestjs/common';

import { getTenantContext } from '../platform/context/tenant-context.js';
import { RequiresPermission } from '../platform/decorators/requires-permission.decorator.js';

import { PosService, type DiningTableInput, type OrderItemInput, type TableCategoryInput } from './pos.service.js';

@Controller('pos')
export class PosController {
  constructor(private readonly pos: PosService) {}
  @Get('categories') @RequiresPermission('pos.view') categories(@Query('branchId') branchId?: string) { return this.pos.categories(getTenantContext().tenantId, branchId); }
  @Post('categories') @RequiresPermission('pos.config.manage') createCategory(@Body() body: TableCategoryInput) { return this.pos.createCategory(getTenantContext().tenantId, body); }
  @Get('tables') @RequiresPermission('pos.view') tables(@Query('branchId') branchId?: string) { return this.pos.tables(getTenantContext().tenantId, branchId); }
  @Post('tables') @RequiresPermission('pos.tables.manage') createTable(@Body() body: DiningTableInput) { return this.pos.createTable(getTenantContext().tenantId, body); }
  @Post('tables/:id/open') @RequiresPermission('pos.operate') open(@Param('id') id: string, @Body() body: { businessDay?: string }) { return this.pos.openTable(getTenantContext().tenantId, id, body.businessDay); }
  @Post('tables/:id/items') @RequiresPermission('pos.operate') addItem(@Param('id') id: string, @Body() body: OrderItemInput & { businessDay?: string }) { return this.pos.addItem(getTenantContext().tenantId, id, body, body.businessDay); }
  @Post('events/:id/void') @RequiresPermission('pos.operate') voidItem(@Param('id') id: string, @Body() body: { reason: string }) { return this.pos.voidItem(getTenantContext().tenantId, id, body.reason); }
  @Post('tables/:id/send-to-invoice') @RequiresPermission('pos.operate') send(@Param('id') id: string, @Body() body: { cashCustomerName?: string; orderType?: string; businessDay?: string }) { return this.pos.sendToInvoice(getTenantContext().tenantId, id, body.cashCustomerName, body.orderType, body.businessDay); }
  @Post('tables/:id/close') @RequiresPermission('pos.operate') close(@Param('id') id: string) { return this.pos.close(getTenantContext().tenantId, id); }
  @Post('tables/:sourceId/merge/:targetId') @RequiresPermission('pos.operate') merge(@Param('sourceId') sourceId: string, @Param('targetId') targetId: string) { return this.pos.merge(getTenantContext().tenantId, sourceId, targetId); }
  @Post('tables/:id/split') @RequiresPermission('pos.operate') split(@Param('id') id: string) { return this.pos.split(getTenantContext().tenantId, id); }
}
