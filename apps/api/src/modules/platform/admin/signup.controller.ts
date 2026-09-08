import { Body, Controller, Get, HttpCode, Post } from '@nestjs/common';
import { ApiOperation, ApiResponse, ApiTags } from '@nestjs/swagger';
import { env } from '@erp/config';
import { DomainError, errorCodes } from '@erp/contracts';

import { OrgProvisioningService } from '../../organization/provisioning/org-provisioning.service.js';
import { Public } from '../decorators/public.decorator.js';
import { RateLimit } from '../decorators/rate-limit.decorator.js';
import { BillingService } from '../billing/billing.service.js';

import { PlatformAdminService, type SignupInput } from './platform-admin.service.js';

/**
 * `/api/v1/signup` — public self-service onboarding (نافذة الاشتراك).
 *
 * The product had a login window but no way to *become* a customer: every tenant had to be
 * inserted by the seed script. This controller closes that gap. It is deliberately the only
 * public write in the platform plane, so it is rate limited on the same bucket as login and
 * it can never grant a licence — it files a pending activation request that a platform
 * operator approves from the console.
 */
@ApiTags('signup')
@Controller()
export class SignupController {
  constructor(
    private readonly admin: PlatformAdminService,
    private readonly billing: BillingService,
    private readonly provisioning: OrgProvisioningService,
  ) {}

  @Public()
  @Get('signup/plans')
  @RateLimit({ name: 'signup', limit: env.RATE_LIMIT_DEFAULT_PER_MINUTE, windowMs: 60_000 })
  @ApiOperation({ summary: 'Plans a prospect can pick while signing up' })
  async plans() {
    return { data: await this.billing.listActivePlans() };
  }

  @Public()
  @Post('signup')
  @HttpCode(201)
  @RateLimit({ name: 'signup', limit: env.RATE_LIMIT_LOGIN_PER_MINUTE, windowMs: 60_000 })
  @ApiOperation({ summary: 'Create a new tenant, its owner account and a pending activation request' })
  @ApiResponse({ status: 201, description: 'Tenant created; owner can log in immediately' })
  @ApiResponse({ status: 409, description: 'Tenant code already taken (VERSION_CONFLICT)' })
  @ApiResponse({ status: 422, description: 'Invalid payload or weak password (VALIDATION_FAILED)' })
  async signup(@Body() body: SignupInput) {
    if (env.SIGNUP_ENABLED === false) {
      throw new DomainError(errorCodes.FORBIDDEN, 'Self-service signup is disabled on this deployment', 403);
    }

    const created = await this.admin.signup(body);
    const defaults = await this.provisioning.provisionOrgDefaults(created.tenantId, {
      actorUserId: created.ownerUserId,
    });

    return {
      data: {
        tenantCode: created.tenantCode,
        ownerEmail: body.ownerEmail.trim().toLowerCase(),
        subscriptionStatus: created.subscriptionStatus,
        activationRequestId: created.activationRequestId,
        defaults,
      },
    };
  }
}
