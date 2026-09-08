import { Body, Controller, Get, Post, Param } from '@nestjs/common';
import { ApiBearerAuth, ApiOperation, ApiTags } from '@nestjs/swagger';

import { Public } from '../decorators/public.decorator.js';
import { BillingService } from './billing.service.js';

@ApiTags('billing')
@ApiBearerAuth()
@Controller('billing')
export class BillingController {
  constructor(private readonly billing: BillingService) {}

  @Public()
  @Get('plans')
  @ApiOperation({ summary: 'List active subscription plans' })
  async plans() {
    return { data: await this.billing.listActivePlans() };
  }

  @Post('activation-requests')
  @ApiOperation({ summary: 'Request manual tenant activation' })
  async requestActivation(@Body() body: { planId: string; notes?: string }) {
    return { data: await this.billing.requestManualActivation(body.planId, body.notes) };
  }

  @Get('subscription')
  @ApiOperation({ summary: 'Read the current tenant subscription' })
  async subscription() {
    return { data: await this.billing.mySubscription() };
  }

  @Get('activation-requests')
  @ApiOperation({ summary: 'List pending activation requests for platform admins' })
  async activationRequests() {
    return { data: await this.billing.listActivationRequests() };
  }

  @Post('activation-requests/:id/review')
  @ApiOperation({ summary: 'Approve or reject a manual activation request' })
  async review(
    @Param('id') id: string,
    @Body() body: { approve: boolean; notes?: string },
  ) {
    return { data: await this.billing.reviewActivation(id, body.approve, body.notes) };
  }
}
