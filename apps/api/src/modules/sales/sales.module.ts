import { Module } from '@nestjs/common';

import { AccountingModule } from '../accounting/accounting.module.js';
import { InventoryModule } from '../inventory/inventory.module.js';

import { SalesController } from './sales.controller.js';
import { SalesService } from './sales.service.js';

@Module({ imports: [AccountingModule, InventoryModule], controllers: [SalesController], providers: [SalesService], exports: [SalesService] })
export class SalesModule {}
