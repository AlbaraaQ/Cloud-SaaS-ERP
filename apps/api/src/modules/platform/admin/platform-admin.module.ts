import { Module } from '@nestjs/common';

import { OrganizationModule } from '../../organization/organization.module.js';
import { PlatformModule } from '../platform.module.js';

import { PlatformAdminController } from './platform-admin.controller.js';
import { PlatformAdminService } from './platform-admin.service.js';
import { PlatformConsoleController } from './platform-console.controller.js';
import { PlatformConsoleService } from './platform-console.service.js';
import { SignupController } from './signup.controller.js';

/**
 * The SaaS control plane lives in its own module rather than inside `PlatformModule`
 * because it needs `OrgProvisioningService` from `OrganizationModule` — and
 * `OrganizationModule` already depends on the platform guards. Keeping the dependency
 * one-directional here avoids a module cycle.
 */
@Module({
  imports: [PlatformModule, OrganizationModule],
  controllers: [PlatformAdminController, PlatformConsoleController, SignupController],
  providers: [PlatformAdminService, PlatformConsoleService],
  exports: [PlatformAdminService, PlatformConsoleService],
})
export class PlatformAdminModule {}
