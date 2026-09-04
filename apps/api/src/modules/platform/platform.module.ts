import { Module } from '@nestjs/common';

import { AuthController } from './auth/auth.controller.js';
import { AuthService } from './auth/auth.service.js';
import { PasswordService } from './auth/password.service.js';
import { TokenService } from './auth/token.service.js';
import { IdentityController } from './identity/identity.controller.js';
import { IdentityService } from './identity/identity.service.js';
import { RateLimiterService } from './rate-limit/rate-limiter.service.js';
import { MembershipsController } from './tenancy/memberships.controller.js';
import { MembershipsService } from './tenancy/memberships.service.js';
import { RolesController } from './tenancy/roles.controller.js';
import { RolesService } from './tenancy/roles.service.js';
import { SettingsController } from './tenancy/settings.controller.js';
import { SettingsService } from './tenancy/settings.service.js';
import { TenantController } from './tenancy/tenant.controller.js';
import { TenantService } from './tenancy/tenant.service.js';

/**
 * Platform module — tenancy, identity and access (TARGET_ARCHITECTURE §3).
 * Everything later phases need to authorise a request is exported from `./index.js`.
 */
@Module({
  controllers: [
    AuthController,
    IdentityController,
    TenantController,
    MembershipsController,
    RolesController,
    SettingsController,
  ],
  providers: [
    AuthService,
    IdentityService,
    MembershipsService,
    RolesService,
    SettingsService,
    TenantService,
    PasswordService,
    TokenService,
    RateLimiterService,
  ],
  exports: [AuthService, IdentityService, PasswordService, TokenService, RateLimiterService],
})
export class PlatformModule {}
