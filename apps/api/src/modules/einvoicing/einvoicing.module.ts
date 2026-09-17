import { Module } from '@nestjs/common';

import { EinvoicingController } from './einvoicing.controller.js';
import { EinvoicingService } from './einvoicing.service.js';
import { ZatcaOnboardingService } from './zatca-onboarding.service.js';

@Module({
  controllers: [EinvoicingController],
  providers: [EinvoicingService, ZatcaOnboardingService],
  exports: [EinvoicingService, ZatcaOnboardingService],
})
export class EinvoicingModule {}
