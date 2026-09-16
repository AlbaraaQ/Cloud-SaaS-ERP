import { Module } from '@nestjs/common';

import { AccountingModule } from '../accounting/accounting.module.js';
import { InventoryModule } from '../inventory/inventory.module.js';
import { OrganizationModule } from '../organization/organization.module.js';

import { PurchaseNotesController } from './purchase-notes.controller.js';
import { PurchasesController } from './purchases.controller.js';
import { PurchasesService } from './purchases.service.js';

@Module({ imports: [AccountingModule, InventoryModule, OrganizationModule], controllers: [PurchasesController, PurchaseNotesController], providers: [PurchasesService], exports: [PurchasesService] })
export class PurchasesModule {}
