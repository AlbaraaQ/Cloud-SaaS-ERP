import { Body, Controller, Delete, Get, Param, Patch, Post, Query, UseGuards } from '@nestjs/common';
import { ApiBearerAuth, ApiOperation, ApiTags } from '@nestjs/swagger';

import { OrgProvisioningService } from '../../organization/provisioning/org-provisioning.service.js';
import { RequiresPlatformRole } from '../decorators/requires-platform-role.decorator.js';
import { PlatformAdminGuard } from '../guards/platform-admin.guard.js';

import {
  PlatformAdminService,
  type CreateTenantInput,
  type GrantSubscriptionInput,
  type PlanInput,
} from './platform-admin.service.js';

/**
 * `/api/v1/platform/*` — the SaaS control plane consumed by the admin console at
 * `/platform`. Every route sits behind `PlatformAdminGuard`.
 *
 * **P-C1 (2026-09-17) — the permission repair.** Until this part, two routes carried a
 * `console.*` code and the other eleven were reachable by *any* effective platform
 * administrator: the `pam` claim alone was enough to suspend a customer, retire a plan or
 * cancel a licence (INCOMPLETE_INVENTORY §4.2 measured it: 10 of 12 codes declared but
 * unused). Every route below now names the code it needs, so the five Family-A roles in
 * `@erp/contracts`' `platformRoleCatalog` mean what their descriptions say:
 *
 * | Route | Code | Owner | Operations | Billing | Support | Auditor |
 * |---|---|---|---|---|---|---|
 * | `GET overview` | `console.tenants.view` | ✓ | ✓ | ✓ | ✓ | ✓ |
 * | `GET tenants` | `console.tenants.view` | ✓ | ✓ | ✓ | ✓ | ✓ |
 * | `POST tenants` | `console.tenants.manage` | ✓ | | | | |
 * | `POST tenants/:id/status` (P-C2, in `PlatformTenantsController`) | `console.tenants.manage` | ✓ | | | | |
 * | `GET plans` | `console.plans.manage` | ✓ | | ✓ | | |
 * | `POST plans` | `console.plans.manage` | ✓ | | ✓ | | |
 * | `PATCH plans/:id/active` | `console.plans.manage` | ✓ | | ✓ | | |
 * | `GET subscriptions` | `console.subscriptions.manage` | ✓ | | ✓ | | |
 * | `POST subscriptions` | `console.subscriptions.manage` | ✓ | | ✓ | | |
 * | `POST subscriptions/:id/cancel` | `console.subscriptions.manage` | ✓ | | ✓ | | |
 * | `GET activation-requests` | `console.activation.review` | ✓ | | ✓ | | |
 * | `POST activation-requests/:id/review` | `console.activation.review` | ✓ | | ✓ | | |
 * | `GET users` | `console.users.view` | ✓ | | | | |
 * | `GET roles` | `console.users.view` | ✓ | | | | |
 * | `GET permissions` | `console.users.view` | ✓ | | | | |
 * | `POST users/:id/roles` | `console.users.manage` | ✓ | | | | |
 * | `DELETE users/:id/roles/:roleCode` | `console.users.manage` | ✓ | | | | |
 *
 * Read routes are mapped to the code that owns the *area* (plans/subscriptions/activation
 * queue/identity) rather than to a read-only twin, because no `console.*.view` twin exists
 * for them in the registry and inventing four codes to read four lists is not a smaller
 * surface — it is a bigger one. The console sidebar hides exactly what these codes deny
 * (`apps/platform-admin/lib/navigation.ts`), so the operator sees no dead links.
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
  @RequiresPlatformRole('console.tenants.view')
  @ApiOperation({ summary: 'Control-plane KPIs: customers, licences, MRR, pending activations' })
  async overview() {
    return { data: await this.admin.overview() };
  }

  // ------------------------------------------------------------------ tenants

  @Get('tenants')
  @RequiresPlatformRole('console.tenants.view')
  @ApiOperation({ summary: 'List every customer with its current licence' })
  async listTenants(@Query('search') search?: string, @Query('status') status?: string) {
    return { data: await this.admin.listTenants(search, status) };
  }

  @Post('tenants')
  @RequiresPlatformRole('console.tenants.manage')
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

  // `PATCH tenants/:id/status` lived here and took a status with no reason. P-C2 replaced it
  // with `POST /platform/tenants/:id/status` (`PlatformTenantsController`) which requires
  // «السبب»: suspending a customer must be explainable a month later, and two routes for one
  // decision — one of them reason-less — means the rule is only as strong as the caller.

  // ------------------------------------------------------------------ plans

  @Get('plans')
  @RequiresPlatformRole('console.plans.manage')
  @ApiOperation({ summary: 'List subscription plans including retired ones' })
  async listPlans() {
    return { data: await this.admin.listPlans() };
  }

  @Post('plans')
  @RequiresPlatformRole('console.plans.manage')
  @ApiOperation({ summary: 'Create or update a subscription plan (upsert by code)' })
  async createPlan(@Body() body: PlanInput) {
    return { data: await this.admin.createPlan(body) };
  }

  @Patch('plans/:id/active')
  @RequiresPlatformRole('console.plans.manage')
  @ApiOperation({ summary: 'Activate or retire a plan' })
  async setPlanActive(@Param('id') id: string, @Body() body: { active: boolean }) {
    return { data: await this.admin.setPlanActive(id, body.active) };
  }

  // ------------------------------------------------------------------ licences

  @Get('subscriptions')
  @RequiresPlatformRole('console.subscriptions.manage')
  @ApiOperation({ summary: 'List every licence across all customers' })
  async listSubscriptions(@Query('status') status?: string) {
    return { data: await this.admin.listSubscriptions(status) };
  }

  @Post('subscriptions')
  @RequiresPlatformRole('console.subscriptions.manage')
  @ApiOperation({ summary: 'Issue or extend a licence manually' })
  async grantSubscription(@Body() body: GrantSubscriptionInput) {
    return { data: await this.admin.grantSubscription(body) };
  }

  @Post('subscriptions/:id/cancel')
  @RequiresPlatformRole('console.subscriptions.manage')
  @ApiOperation({ summary: 'Cancel a licence' })
  async cancelSubscription(@Param('id') id: string) {
    return { data: await this.admin.cancelSubscription(id) };
  }

  // ------------------------------------------------------------------ activation queue

  @Get('activation-requests')
  @RequiresPlatformRole('console.activation.review')
  @ApiOperation({ summary: 'Manual activation queue' })
  async listActivationRequests(@Query('status') status?: string) {
    return { data: await this.admin.listActivationRequests(status ?? 'pending') };
  }

  @Post('activation-requests/:id/review')
  @RequiresPlatformRole('console.activation.review')
  @ApiOperation({ summary: 'Approve or reject an activation request' })
  async reviewActivation(@Param('id') id: string, @Body() body: { approve: boolean; notes?: string }) {
    return { data: await this.admin.reviewActivation(id, body.approve, body.notes) };
  }

  // ------------------------------------------------------------------ users

  @Get('users')
  @RequiresPlatformRole('console.users.view')
  @ApiOperation({ summary: 'Search platform users across all tenants' })
  async listUsers(@Query('search') search?: string) {
    return { data: await this.admin.listUsers(search) };
  }

  // ------------------------------------------------------------------ platform roles (2026-09)

  @Get('roles')
  @RequiresPlatformRole('console.users.view')
  @ApiOperation({ summary: 'Family-A platform role catalogue with holder counts' })
  async listPlatformRoles() {
    return { data: await this.admin.listPlatformRoles() };
  }

  @Get('permissions')
  @RequiresPlatformRole('console.users.view')
  @ApiOperation({ summary: 'Platform-console (console.*) permission registry' })
  async listPlatformPermissions() {
    return { data: this.admin.listPlatformPermissions() };
  }

  @Post('users/:id/roles')
  @RequiresPlatformRole('console.users.manage')
  @ApiOperation({ summary: 'Grant a platform role to a user' })
  async grantPlatformRole(@Param('id') id: string, @Body() body: { roleCode: string }) {
    return { data: await this.admin.grantPlatformRole(id, body.roleCode) };
  }

  @Delete('users/:id/roles/:roleCode')
  @RequiresPlatformRole('console.users.manage')
  @ApiOperation({ summary: 'Revoke a platform role from a user' })
  async revokePlatformRole(@Param('id') id: string, @Param('roleCode') roleCode: string) {
    return { data: await this.admin.revokePlatformRole(id, roleCode) };
  }
}
