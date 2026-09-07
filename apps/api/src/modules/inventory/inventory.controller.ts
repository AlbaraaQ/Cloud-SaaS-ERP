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
  @Get('valuation/as-of') @RequiresPermission('inventory.view') valuationAsOf(@Query('as_of') asOf: string, @Query('warehouse_id') warehouseId?: string, @Query('item_id') itemId?: string) { return this.inventory.valuationAsOf(getTenantContext().tenantId, new Date(asOf), warehouseId, itemId); }
  @Post('balances/recompute') @RequiresPermission('inventory.negative.override') recompute(@Query('warehouse_id') warehouseId?: string, @Query('item_id') itemId?: string) { return this.inventory.recomputeBalances(getTenantContext().tenantId, warehouseId, itemId); }
  @Post('transfers') @RequiresPermission('inventory.adjust') transfer(@Body() body: { transferId: string; fromWarehouseId: string; toWarehouseId: string; lines: Array<{ itemId: string; qty: string; unitCost?: string; lotId?: string; serialId?: string }> }) { return this.inventory.transfer(getTenantContext().tenantId, body); }
  @Post('adjustments/post') @RequiresPermission('inventory.adjust') adjust(@Body() body: { adjustmentId: string; itemId: string; warehouseId: string; countedQty: string; unitCost?: string; approved: boolean; journalEntryId?: string }) { return this.inventory.adjust(getTenantContext().tenantId, body); }
  @Post('serials/reserve') @RequiresPermission('inventory.adjust') reserveSerials(@Body() body: { serialIds: string[] }) { return this.inventory.reserveSerials(getTenantContext().tenantId, body.serialIds); }
}
