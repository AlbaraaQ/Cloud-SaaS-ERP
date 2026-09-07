import { Module } from '@nestjs/common';

import { AccountingModule } from '../accounting/accounting.module.js';

import { TreasuryController } from './treasury.controller.js';
import { TreasuryService } from './treasury.service.js';

@Module({ imports: [AccountingModule], controllers: [TreasuryController], providers: [TreasuryService], exports: [TreasuryService] })
export class TreasuryModule {}
