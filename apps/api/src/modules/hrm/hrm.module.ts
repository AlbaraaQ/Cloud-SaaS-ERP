import { Module } from '@nestjs/common';

import { AccountingModule } from '../accounting/accounting.module.js';
import { TreasuryModule } from '../treasury/treasury.module.js';

import { HrmController } from './hrm.controller.js';
import { HrmService } from './hrm.service.js';

@Module({ imports: [AccountingModule, TreasuryModule], controllers: [HrmController], providers: [HrmService], exports: [HrmService] })
export class HrmModule {}
