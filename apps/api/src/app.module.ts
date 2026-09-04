import { Module } from '@nestjs/common';
import { ConfigModule } from '@nestjs/config';
import { APP_FILTER, APP_GUARD, APP_INTERCEPTOR } from '@nestjs/core';
import { LoggerModule } from 'nestjs-pino';
import { REDACTED_LOG_PATHS, env } from '@erp/config';

import { AllExceptionsFilter } from './common/filters/all-exceptions.filter.js';
import { IdempotencyInterceptor } from './common/interceptors/idempotency.interceptor.js';
import { RequestContextInterceptor } from './common/interceptors/request-context.interceptor.js';
import { DatabaseModule } from './database/database.module.js';
import { DomainEventsModule } from './events/domain-events.module.js';
import { HealthController } from './health/health.controller.js';
import {
  AuthGuard,
  BranchScopeGuard,
  PermissionsGuard,
  PlatformModule,
  RateLimitGuard,
  TenantGuard,
} from './modules/platform/index.js';
import { AuditInterceptor, PlatformServicesModule } from './modules/platform-services/index.js';

/**
 * Guard order is frozen by API_ARCHITECTURE §2:
 * `rate limit → AuthGuard → TenantGuard (+RLS GUC) → BranchScopeGuard → PermissionsGuard`.
 * `APP_GUARD` providers are applied in declaration order, so the array below *is* the
 * pipeline; reordering it is a contract change, not a refactor.
 *
 * Interceptor order matters just as much: `RequestContext` establishes the ALS store the
 * other two read, `Idempotency` may short-circuit with a stored response *before* the
 * handler (and before an audit row would be written for a request that never ran), and
 * `Audit` wraps the handler last so it observes the real outcome
 * (SECURITY_ARCHITECTURE §10: every mutating endpoint writes an audit row).
 */
@Module({
  imports: [
    ConfigModule.forRoot({
      isGlobal: true,
      cache: true,
      envFilePath: ['.env', '.env.local'],
      validate: () => env,
    }),
    LoggerModule.forRoot({
      pinoHttp: {
        level: env.NODE_ENV === 'production' ? 'info' : 'debug',
        customProps: () => ({ service: 'erp-api' }),
        redact: { paths: [...REDACTED_LOG_PATHS], censor: '[redacted]' },
        autoLogging: env.NODE_ENV !== 'test',
      },
    }),
    DatabaseModule,
    DomainEventsModule,
    PlatformModule,
    PlatformServicesModule,
  ],
  controllers: [HealthController],
  providers: [
    { provide: APP_FILTER, useClass: AllExceptionsFilter },
    { provide: APP_INTERCEPTOR, useClass: RequestContextInterceptor },
    { provide: APP_INTERCEPTOR, useClass: IdempotencyInterceptor },
    { provide: APP_INTERCEPTOR, useClass: AuditInterceptor },
    { provide: APP_GUARD, useClass: RateLimitGuard },
    { provide: APP_GUARD, useClass: AuthGuard },
    { provide: APP_GUARD, useClass: TenantGuard },
    { provide: APP_GUARD, useClass: BranchScopeGuard },
    { provide: APP_GUARD, useClass: PermissionsGuard },
  ],
})
export class AppModule {}
