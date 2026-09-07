import { Module } from '@nestjs/common';

import { EinvoicingController } from './einvoicing.controller.js';
import { EinvoicingService } from './einvoicing.service.js';

@Module({ controllers: [EinvoicingController], providers: [EinvoicingService], exports: [EinvoicingService] })
export class EinvoicingModule {}
