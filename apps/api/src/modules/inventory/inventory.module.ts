import { Module } from '@nestjs/common';

import { DatabaseModule } from '../../database/database.module.js';
import { AccountingModule } from '../accounting/accounting.module.js';
import { OrganizationModule } from '../organization/organization.module.js';
import { PlatformServicesModule } from '../platform-services/index.js';

import { InventoryController } from './inventory.controller.js';
import { InventoryDocumentsController } from './inventory-documents.controller.js';
import { InventoryDocumentsService } from './inventory-documents.service.js';
import { InventoryService } from './inventory.service.js';
import { ProductionOrdersController } from './production-orders.controller.js';
import { ProductionOrdersService } from './production-orders.service.js';
import { WarehouseDocumentsController } from './warehouse-documents.controller.js';
import { WarehouseDocumentsService } from './warehouse-documents.service.js';

@Module({
  imports: [DatabaseModule, PlatformServicesModule, AccountingModule, OrganizationModule],
  controllers: [InventoryController, InventoryDocumentsController, WarehouseDocumentsController, ProductionOrdersController],
  providers: [InventoryService, InventoryDocumentsService, WarehouseDocumentsService, ProductionOrdersService],
  exports: [InventoryService, InventoryDocumentsService, WarehouseDocumentsService, ProductionOrdersService],
})
export class InventoryModule {}
