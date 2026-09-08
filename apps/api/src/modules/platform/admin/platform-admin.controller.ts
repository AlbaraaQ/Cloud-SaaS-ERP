import { Body, Controller, Get, Param, Patch, Post, Query, UseGuards } from '@nestjs/common';
import { ApiBearerAuth, ApiOperation, ApiTags } from '@nestjs/swagger';

import { OrgProvisioningService } from '../../organization/provisioning/org-provisioning.service.js';
import { PlatformAdminGuard } from '../guards/platform-admin.guard.js';

import {
  PlatformAdminService,
  type CreateTenantInput,
  type GrantSubscriptionInput,
  type PlanInput,
} from './platform-admin.service.js';

/**
 * `/api/v1/platform/*` — the SaaS control plane consumed by the admin console at
 * `/platform`. Every route sits behind `PlatformAdminGuard`, which requires the `pam`
 * claim; a tenant permission can never reach it (SECURITY_ARCHITECTURE §3).
 */
@ApiTags('platform-admin')
@ApiBearerAuth()
@UseGuards(PlatformAdminGuard)
@Controller('platform')
export class PlatformAdminController {
  constructor(
    private readonly admin: PlatformAdminService,
    private readonly provisioning: OrgProvisioningService,
  ) {}

  @Get('overview')
  @ApiOperation({ summary: 'Control-plane KPIs: customers, licences, MRR, pending activations' })
  async overview() {
    return { data: await this.admin.overview() };
  }

  // ------------------------------------------------------------------ tenants

  @Get('tenants')
  @ApiOperation({ summary: 'List every customer with its current licence' })
  async listTenants(@Query('search') search?: string, @Query('status') status?: string) {
    return { data: await this.admin.listTenants(search, status) };
  }

  @Post('tenants')
  @ApiOperation({ summary: 'Create a customer, its owner account and its default branch' })
  async createTenant(@Body() body: CreateTenantInput) {
    const created = await this.admin.createTenant(body);
    // A tenant without a branch cannot number a document or hold stock, so the org
    // defaults are provisioned right away (idempotent).
    const defaults = await this.provisioning.provisionOrgDefaults(created.tenantId, {
      actorUserId: created.ownerUserId,
    });
    return { data: { ...created, defaults } };
  }

  @Patch('tenants/:id/status')
  @ApiOperation({ summary: 'Suspend, reactivate or archive a customer' })
  async setTenantStatus(@Param('id') id: string, @Body() body: { status: 'active' | 'suspended' | 'archived' }) {
    return { data: await this.admin.setTenantStatus(id, body.status) };
  }

  // ------------------------------------------------------------------ plans

  @Get('plans')
  @ApiOperation({ summary: 'List subscription plans including retired ones' })
  async listPlans() {
    return { data: await this.admin.listPlans() };
  }

  @Post('plans')
  @ApiOperation({ summary: 'Create or update a subscription plan (upsert by code)' })
  async createPlan(@Body() body: PlanInput) {
    return { data: await this.admin.createPlan(body) };
  }

  @Patch('plans/:id/active')
  @ApiOperation({ summary: 'Activate or retire a plan' })
  async setPlanActive(@Param('id') id: string, @Body() body: { active: boolean }) {
    return { data: await this.admin.setPlanActive(id, body.active) };
  }

  // ------------------------------------------------------------------ licences

  @Get('subscriptions')
  @ApiOperation({ summary: 'List every licence across all customers' })
  async listSubscriptions(@Query('status') status?: string) {
    return { data: await this.admin.listSubscriptions(status) };
  }

  @Post('subscriptions')
  @ApiOperation({ summary: 'Issue or extend a licence manually' })
  async grantSubscription(@Body() body: GrantSubscriptionInput) {
    return { data: await this.admin.grantSubscription(body) };
  }

  @Post('subscriptions/:id/cancel')
  @ApiOperation({ summary: 'Cancel a licence' })
  async cancelSubscription(@Param('id') id: string) {
    return { data: await this.admin.cancelSubscription(id) };
  }

  // ------------------------------------------------------------------ activation queue

  @Get('activation-requests')
  @ApiOperation({ summary: 'Manual activation queue' })
  async listActivationRequests(@Query('status') status?: string) {
    return { data: await this.admin.listActivationRequests(status ?? 'pending') };
  }

  @Post('activation-requests/:id/review')
  @ApiOperation({ summary: 'Approve or reject an activation request' })
  async reviewActivation(@Param('id') id: string, @Body() body: { approve: boolean; notes?: string }) {
    return { data: await this.admin.reviewActivation(id, body.approve, body.notes) };
  }

  // ------------------------------------------------------------------ users

  @Get('users')
  @ApiOperation({ summary: 'Search platform users across all tenants' })
  async listUsers(@Query('search') search?: string) {
    return { data: await this.admin.listUsers(search) };
  }
}
