import { Module } from '@nestjs/common';

import { AccountingModule } from '../accounting/accounting.module.js';
import { InventoryModule } from '../inventory/inventory.module.js';
import { OrganizationModule } from '../organization/organization.module.js';

import { SalesController } from './sales.controller.js';
import { SalesService } from './sales.service.js';

@Module({ imports: [AccountingModule, InventoryModule, OrganizationModule], controllers: [SalesController], providers: [SalesService], exports: [SalesService] })
export class SalesModule {}
