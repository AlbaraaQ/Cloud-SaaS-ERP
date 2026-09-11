import { Body, Controller, Get, Param, Post, Query } from '@nestjs/common';

import { getTenantContext } from '../platform/context/tenant-context.js';
import { RequiresPermission } from '../platform/decorators/requires-permission.decorator.js';

import { InventoryService, type InventoryLine } from './inventory.service.js';

@Controller('inventory')
export class InventoryController {
  constructor(private readonly inventory: InventoryService) {}

  @Get('levels')
  @RequiresPermission('inventory.view')
  levels(@Query('warehouse_id') warehouseId?: string, @Query('item_id') itemId?: string) {
    return this.inventory.levels(getTenantContext().tenantId, warehouseId, itemId);
  }

  @Get('movements')
  @RequiresPermission('inventory.view')
  movements(@Query('item_id') itemId?: string, @Query('warehouse_id') warehouseId?: string) {
    return this.inventory.movements(getTenantContext().tenantId, itemId, warehouseId);
  }

  /** Reorder alert register for the desktop "حد إعادة الطلب" report. */
  @Get('reorder')
  @RequiresPermission('inventory.view')
  reorder(@Query('warehouse_id') warehouseId?: string) {
    return this.inventory.reorderReport(getTenantContext().tenantId, warehouseId);
  }

  @Post('ledger/record')
  @RequiresPermission('inventory.adjust')
  record(@Body() body: { lines: InventoryLine[] }) {
    return this.inventory.record(getTenantContext().tenantId, body.lines);
  }

  @Get('valuation/as-of')
  @RequiresPermission('inventory.view')
  valuationAsOf(
    @Query('as_of') asOf: string,
    @Query('warehouse_id') warehouseId?: string,
    @Query('item_id') itemId?: string,
  ) {
    return this.inventory.valuationAsOf(getTenantContext().tenantId, new Date(asOf), warehouseId, itemId);
  }

  @Post('balances/recompute')
  @RequiresPermission('inventory.negative.override')
  recompute(@Query('warehouse_id') warehouseId?: string, @Query('item_id') itemId?: string) {
    return this.inventory.recomputeBalances(getTenantContext().tenantId, warehouseId, itemId);
  }

  @Get('transfers')
  @RequiresPermission('inventory.view')
  listTransfers(@Query('status') status?: string) {
    return this.inventory.listTransfers(getTenantContext().tenantId, status);
  }

  @Get('transfers/:id')
  @RequiresPermission('inventory.view')
  getTransfer(@Param('id') id: string) {
    return this.inventory.getTransfer(getTenantContext().tenantId, id);
  }

  /** Backward-compatible one-step transfer; new UI should use draft → send → receive. */
  @Post('transfers')
  @RequiresPermission('inventory.transfer')
  transfer(
    @Body()
    body: {
      transferId: string;
      fromWarehouseId: string;
      toWarehouseId: string;
      lines: Array<{
        itemId: string;
        qty: string;
        unitId?: string;
        unitCost?: string;
        lotId?: string;
        serialId?: string;
        serialIds?: string[];
      }>;
    },
  ) {
    return this.inventory.transfer(getTenantContext().tenantId, body);
  }

  @Post('transfers/draft')
  @RequiresPermission('inventory.transfer')
  createTransfer(
    @Body()
    body: {
      id?: string;
      number?: string;
      fromWarehouseId: string;
      toWarehouseId: string;
      lines: Array<{
        itemId: string;
        qty: string;
        unitId?: string;
        unitCost?: string;
        lotId?: string;
        serialIds?: string[];
      }>;
    },
  ) {
    return this.inventory.createTransfer(getTenantContext().tenantId, body);
  }

  @Post('transfers/:id/send')
  @RequiresPermission('inventory.transfer')
  sendTransfer(@Param('id') transferId: string) {
    return this.inventory.sendTransfer(getTenantContext().tenantId, transferId);
  }

  @Post('transfers/:id/receive')
  @RequiresPermission('inventory.transfer.receive')
  receiveTransfer(
    @Param('id') transferId: string,
    @Body() body: { received: Array<{ lineNo: number; qty: string; serialIds?: string[] }> },
  ) {
    return this.inventory.receiveTransfer(getTenantContext().tenantId, transferId, body.received);
  }

  /** Legacy path retained for installed desktop bridge clients. */
  @Post('transfers/receive')
  @RequiresPermission('inventory.transfer.receive')
  receiveTransferLegacy(
    @Body() body: { transferId: string; received: Array<{ lineNo: number; qty: string; serialIds?: string[] }> },
  ) {
    return this.inventory.receiveTransfer(getTenantContext().tenantId, body.transferId, body.received);
  }

  @Post('transfers/:id/cancel')
  @RequiresPermission('inventory.transfer')
  cancelTransfer(@Param('id') transferId: string) {
    return this.inventory.cancelTransfer(getTenantContext().tenantId, transferId);
  }

  @Post('transfers/cancel')
  @RequiresPermission('inventory.transfer')
  cancelTransferLegacy(@Body() body: { transferId: string }) {
    return this.inventory.cancelTransfer(getTenantContext().tenantId, body.transferId);
  }

  /** Legacy direct adjustment endpoint; audited documents live at /inventory/documents. */
  @Post('adjustments/post')
  @RequiresPermission('inventory.adjust.approve')
  adjust(
    @Body()
    body: {
      adjustmentId: string;
      itemId: string;
      warehouseId: string;
      countedQty: string;
      unitId?: string;
      unitCost?: string;
      lotId?: string;
      serialIds?: string[];
      approved: boolean;
      journalEntryId?: string;
    },
  ) {
    return this.inventory.adjust(getTenantContext().tenantId, body);
  }

  @Get('lots')
  @RequiresPermission('inventory.view')
  lots(@Query('item_id') itemId?: string, @Query('warehouse_id') warehouseId?: string) {
    return this.inventory.listLots(getTenantContext().tenantId, itemId, warehouseId);
  }

  @Get('expiry')
  @RequiresPermission('inventory.view')
  expiry(@Query('warehouse_id') warehouseId?: string, @Query('days') days?: string) {
    const parsed = days === undefined ? undefined : Number(days);
    return this.inventory.expiringLots(getTenantContext().tenantId, {
      warehouseId,
      days: Number.isFinite(parsed) ? parsed : undefined,
    });
  }

  @Post('lots')
  @RequiresPermission('inventory.adjust')
  createLot(@Body() body: { itemId: string; lotNo: string; expiryDate?: string; receivedAt?: string }) {
    return this.inventory.createLot(getTenantContext().tenantId, body);
  }

  @Get('serials')
  @RequiresPermission('inventory.view')
  serials(
    @Query('item_id') itemId?: string,
    @Query('status') status?: string,
    @Query('warehouse_id') warehouseId?: string,
  ) {
    return this.inventory.listSerials(getTenantContext().tenantId, itemId, status, warehouseId);
  }

  @Post('serials')
  @RequiresPermission('inventory.adjust')
  createSerial(
    @Body() body: { itemId: string; serialNo: string; lotId?: string; warehouseId?: string; status?: string },
  ) {
    return this.inventory.createSerial(getTenantContext().tenantId, body);
  }

  @Post('serials/reserve')
  @RequiresPermission('inventory.adjust')
  reserveSerials(@Body() body: { serialIds: string[] }) {
    return this.inventory.reserveSerials(getTenantContext().tenantId, body.serialIds);
  }

  @Post('serials/release')
  @RequiresPermission('inventory.adjust')
  releaseSerials(@Body() body: { serialIds: string[] }) {
    return this.inventory.releaseSerials(getTenantContext().tenantId, body.serialIds);
  }

  @Post('serials/consume')
  @RequiresPermission('inventory.adjust')
  consumeSerials(@Body() body: { serialIds: string[] }) {
    return this.inventory.consumeSerials(getTenantContext().tenantId, body.serialIds);
  }

  @Post('serials/return')
  @RequiresPermission('inventory.adjust')
  returnSerials(@Body() body: { serialIds: string[] }) {
    return this.inventory.returnSerials(getTenantContext().tenantId, body.serialIds);
  }

  @Post('serials/scrap')
  @RequiresPermission('inventory.adjust.approve')
  scrapSerials(@Body() body: { serialIds: string[] }) {
    return this.inventory.scrapSerials(getTenantContext().tenantId, body.serialIds);
  }
}
