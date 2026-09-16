import { describe, expect, it, vi } from 'vitest';
import type { ExecutionContext } from '@nestjs/common';
import { Reflector } from '@nestjs/core';
import { DomainError } from '@erp/contracts';

import {
  requestContextStorage,
  type AuthContextValue,
} from '../../../request-context/request-context.js';
import { IS_PUBLIC_KEY } from '../decorators/public.decorator.js';
import { REQUIRED_PLATFORM_PERMISSION_KEY } from '../decorators/requires-platform-role.decorator.js';

import { PlatformAdminGuard } from './platform-admin.guard.js';

function contextWith(): ExecutionContext {
  return {
    getHandler: () => ({}),
    getClass: () => ({}),
    switchToHttp: () => ({ getRequest: () => ({}) }),
  } as unknown as ExecutionContext;
}

function reflectorFor(metadata: Record<string, unknown>): Reflector {
  return {
    getAllAndOverride: vi.fn((key: string) => metadata[key]),
  } as unknown as Reflector;
}

function withAuth(auth: Partial<AuthContextValue>, run: () => void): void {
  const full: AuthContextValue = {
    userId: 'u1',
    claimedTenantId: 't1',
    membershipId: 'm1',
    scope: ['erp'],
    tokenId: 'jti',
    isPlatformAdmin: false,
    platformRoles: [],
    ...auth,
  };
  requestContextStorage.run({ traceId: 'trace', startTime: 0, auth: full }, run);
}

describe('PlatformAdminGuard', () => {
  const context = contextWith();

  it('denies a tenant user without platform access', () => {
    const guard = new PlatformAdminGuard(reflectorFor({}));
    withAuth({ isPlatformAdmin: false, platformRoles: [] }, () => {
      try {
        guard.canActivate(context);
        throw new Error('expected the guard to deny');
      } catch (error) {
        expect(error).toBeInstanceOf(DomainError);
        expect((error as DomainError).code).toBe('FORBIDDEN');
      }
    });
  });

  it('admits an effective platform administrator on unscoped routes (legacy rule)', () => {
    const guard = new PlatformAdminGuard(reflectorFor({}));
    withAuth({ isPlatformAdmin: true, platformRoles: [] }, () => {
      expect(guard.canActivate(context)).toBe(true);
    });
    withAuth({ isPlatformAdmin: true, platformRoles: ['platform_support'] }, () => {
      expect(guard.canActivate(context)).toBe(true);
    });
  });

  it('enforces @RequiresPlatformRole against the token platform roles', () => {
    const guard = new PlatformAdminGuard(
      reflectorFor({ [REQUIRED_PLATFORM_PERMISSION_KEY]: 'console.users.manage' }),
    );
    withAuth({ isPlatformAdmin: true, platformRoles: ['platform_owner'] }, () => {
      expect(guard.canActivate(context)).toBe(true);
    });
    withAuth({ isPlatformAdmin: true, platformRoles: ['platform_support'] }, () => {
      try {
        guard.canActivate(context);
        throw new Error('expected the guard to deny');
      } catch (error) {
        expect(error).toBeInstanceOf(DomainError);
        expect((error as DomainError).code).toBe('FORBIDDEN');
        expect((error as DomainError).message).toBe('platform permission console.users.manage required');
      }
    });
  });

  it('skips public routes', () => {
    const guard = new PlatformAdminGuard(reflectorFor({ [IS_PUBLIC_KEY]: true }));
    expect(guard.canActivate(context)).toBe(true);
  });
});
