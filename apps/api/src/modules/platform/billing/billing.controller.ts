import { Controller, Get } from '@nestjs/common';

import { Public } from '../decorators/public.decorator.js';
import { BillingService } from './billing.service.js';

@Controller('billing')
export class BillingController {
  constructor(private readonly billing: BillingService) {}

  @Public()
  @Get('plans')
  async plans() {
    return { data: await this.billing.listActivePlans() };
  }
}
