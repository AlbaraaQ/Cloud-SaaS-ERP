import { Body, Controller, Get, Param, Post, Query } from '@nestjs/common';

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
  @Post('transfers/draft') @RequiresPermission('inventory.adjust') createTransfer(@Body() body: { id: string; number: string; fromWarehouseId: string; toWarehouseId: string; lines: Array<{ itemId: string; qty: string; unitCost?: string; lotId?: string; serialIds?: string[] }> }) { return this.inventory.createTransfer(getTenantContext().tenantId, body); }
  @Post('transfers/:id/send') @RequiresPermission('inventory.adjust') sendTransfer(@Param('id') transferId: string) { return this.inventory.sendTransfer(getTenantContext().tenantId, transferId); }
  @Post('transfers/receive') @RequiresPermission('inventory.adjust') receiveTransfer(@Body() body: { transferId: string; received: Array<{ lineNo: number; qty: string }> }) { return this.inventory.receiveTransfer(getTenantContext().tenantId, body.transferId, body.received); }
  @Post('transfers/cancel') @RequiresPermission('inventory.adjust') cancelTransfer(@Body() body: { transferId: string }) { return this.inventory.cancelTransfer(getTenantContext().tenantId, body.transferId); }
  @Post('adjustments/post') @RequiresPermission('inventory.adjust') adjust(@Body() body: { adjustmentId: string; itemId: string; warehouseId: string; countedQty: string; unitCost?: string; approved: boolean; journalEntryId?: string }) { return this.inventory.adjust(getTenantContext().tenantId, body); }
  @Get('lots') @RequiresPermission('inventory.view') lots(@Query('item_id') itemId?: string) { return this.inventory.listLots(getTenantContext().tenantId, itemId); }
  @Post('lots') @RequiresPermission('inventory.adjust') createLot(@Body() body: { itemId: string; lotNo: string; expiryDate?: string; receivedAt?: string }) { return this.inventory.createLot(getTenantContext().tenantId, body); }
  @Get('serials') @RequiresPermission('inventory.view') serials(@Query('item_id') itemId?: string, @Query('status') status?: string) { return this.inventory.listSerials(getTenantContext().tenantId, itemId, status); }
  @Post('serials') @RequiresPermission('inventory.adjust') createSerial(@Body() body: { itemId: string; serialNo: string; lotId?: string; warehouseId?: string; status?: string }) { return this.inventory.createSerial(getTenantContext().tenantId, body); }
  @Post('serials/reserve') @RequiresPermission('inventory.adjust') reserveSerials(@Body() body: { serialIds: string[] }) { return this.inventory.reserveSerials(getTenantContext().tenantId, body.serialIds); }
  @Post('serials/release') @RequiresPermission('inventory.adjust') releaseSerials(@Body() body: { serialIds: string[] }) { return this.inventory.releaseSerials(getTenantContext().tenantId, body.serialIds); }
  @Post('serials/consume') @RequiresPermission('inventory.adjust') consumeSerials(@Body() body: { serialIds: string[] }) { return this.inventory.consumeSerials(getTenantContext().tenantId, body.serialIds); }
  @Post('serials/return') @RequiresPermission('inventory.adjust') returnSerials(@Body() body: { serialIds: string[] }) { return this.inventory.returnSerials(getTenantContext().tenantId, body.serialIds); }
}
