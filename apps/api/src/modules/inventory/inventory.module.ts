import { Module } from '@nestjs/common';

import { DatabaseModule } from '../../database/database.module.js';
import { PlatformServicesModule } from '../platform-services/index.js';

import { InventoryController } from './inventory.controller.js';
import { InventoryService } from './inventory.service.js';
import { ProductionOrdersController } from './production-orders.controller.js';
import { ProductionOrdersService } from './production-orders.service.js';
import { WarehouseDocumentsController } from './warehouse-documents.controller.js';
import { WarehouseDocumentsService } from './warehouse-documents.service.js';

@Module({
  imports: [DatabaseModule, PlatformServicesModule],
  controllers: [InventoryController, WarehouseDocumentsController, ProductionOrdersController],
  providers: [InventoryService, WarehouseDocumentsService, ProductionOrdersService],
  exports: [InventoryService, WarehouseDocumentsService, ProductionOrdersService],
})
export class InventoryModule {}
