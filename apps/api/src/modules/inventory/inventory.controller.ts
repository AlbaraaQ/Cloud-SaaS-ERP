import { Body, Controller, Get, Post, Query } from '@nestjs/common';

import { RequiresPermission } from '../platform/decorators/requires-permission.decorator.js';
import { getTenantContext } from '../platform/context/tenant-context.js';

import { InventoryService, type InventoryLine } from './inventory.service.js';

@Controller('inventory')
export class InventoryController {
  constructor(private readonly inventory: InventoryService) {}
  @Get('levels') @RequiresPermission('inventory.view') levels(@Query('warehouse_id') warehouseId?: string, @Query('item_id') itemId?: string) { return this.inventory.levels(getTenantContext().tenantId, warehouseId, itemId); }
  @Get('movements') @RequiresPermission('inventory.view') movements(@Query('item_id') itemId?: string, @Query('warehouse_id') warehouseId?: string) { return this.inventory.movements(getTenantContext().tenantId, itemId, warehouseId); }
  @Post('ledger/record') @RequiresPermission('inventory.adjust') record(@Body() body: { lines: InventoryLine[] }) { return this.inventory.record(getTenantContext().tenantId, body.lines); }
}
