import { Body, Controller, Get, Param, Patch, Post, Query } from '@nestjs/common';

import { getTenantContext } from '../platform/context/tenant-context.js';
import { RequiresPermission } from '../platform/decorators/requires-permission.decorator.js';

import {
  InventoryDocumentsService,
  type InventoryDocumentInput,
} from './inventory-documents.service.js';

/** Desktop-equivalent opening, receipt, issue and inventory-adjustment documents. */
@Controller('inventory/documents')
export class InventoryDocumentsController {
  constructor(private readonly documents: InventoryDocumentsService) {}

  @Get()
  @RequiresPermission('inventory.view')
  list(
    @Query('kind') kind?: string,
    @Query('status') status?: string,
    @Query('warehouse_id') warehouseId?: string,
  ) {
    return this.documents.list(getTenantContext().tenantId, { kind, status, warehouseId });
  }

  @Get(':id')
  @RequiresPermission('inventory.view')
  get(@Param('id') id: string) {
    return this.documents.get(getTenantContext().tenantId, id);
  }

  @Post()
  @RequiresPermission('inventory.adjust')
  create(@Body() body: InventoryDocumentInput) {
    return this.documents.create(getTenantContext().tenantId, body);
  }

  @Patch(':id')
  @RequiresPermission('inventory.adjust')
  updateDraft(@Param('id') id: string, @Body() body: InventoryDocumentInput) {
    return this.documents.updateDraft(getTenantContext().tenantId, id, body);
  }

  @Post(':id/post')
  @RequiresPermission('inventory.adjust.approve')
  post(@Param('id') id: string) {
    return this.documents.post(getTenantContext().tenantId, id);
  }

  @Post(':id/cancel')
  @RequiresPermission('inventory.adjust')
  cancel(@Param('id') id: string) {
    return this.documents.cancel(getTenantContext().tenantId, id);
  }

  @Post(':id/void')
  @RequiresPermission('inventory.adjust.approve')
  void(@Param('id') id: string, @Body() body: { reason: string }) {
    return this.documents.void(getTenantContext().tenantId, id, body.reason ?? '');
  }
}
