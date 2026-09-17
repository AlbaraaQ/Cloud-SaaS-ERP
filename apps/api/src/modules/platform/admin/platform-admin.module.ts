import { Module } from '@nestjs/common';

import { OrganizationModule } from '../../organization/organization.module.js';
import { PlatformModule } from '../platform.module.js';

import { PlatformAdminController } from './platform-admin.controller.js';
import { PlatformAdminService } from './platform-admin.service.js';
import { PlatformBillingController } from './platform-billing.controller.js';
import { PlatformBillingService } from './platform-billing.service.js';
import { PlatformConsoleController } from './platform-console.controller.js';
import { PlatformIdentityController } from './platform-identity.controller.js';
import { PlatformIdentityService } from './platform-identity.service.js';
import { PlatformConsoleService } from './platform-console.service.js';
import { PlatformTenantsController } from './platform-tenants.controller.js';
import { PlatformUsageController } from './platform-usage.controller.js';
import { PlatformTenantsService } from './platform-tenants.service.js';
import { SignupController } from './signup.controller.js';

/**
 * The SaaS control plane lives in its own module rather than inside `PlatformModule`
 * because it needs `OrgProvisioningService` from `OrganizationModule` — and
 * `OrganizationModule` already depends on the platform guards. Keeping the dependency
 * one-directional here avoids a module cycle.
 */
@Module({
  imports: [PlatformModule, OrganizationModule],
  controllers: [
    PlatformAdminController,
    PlatformBillingController,
    PlatformConsoleController,
    PlatformIdentityController,
    PlatformTenantsController,
    PlatformUsageController,
    SignupController,
  ],
  providers: [
    PlatformAdminService,
    PlatformBillingService,
    PlatformConsoleService,
    PlatformIdentityService,
    PlatformTenantsService,
  ],
  exports: [
    PlatformAdminService,
    PlatformBillingService,
    PlatformConsoleService,
    PlatformIdentityService,
    PlatformTenantsService,
  ],
})
export class PlatformAdminModule {}
