import { describe, expect, it, vi } from 'vitest';
import type { ExecutionContext } from '@nestjs/common';
import { Reflector } from '@nestjs/core';
import { DomainError } from '@erp/contracts';

import { requestContextStorage, type TenantContextValue } from '../../../request-context/request-context.js';
import { IS_PUBLIC_KEY } from '../decorators/public.decorator.js';
import { REQUIRED_PERMISSION_KEY } from '../decorators/requires-permission.decorator.js';

import { PermissionsGuard } from './permissions.guard.js';

function contextWith(metadata: Record<string, unknown>): ExecutionContext {
  return {
    getHandler: () => ({}),
    getClass: () => ({}),
    switchToHttp: () => ({ getRequest: () => ({}) }),
    ...metadata,
  } as unknown as ExecutionContext;
}

function reflectorFor(metadata: Record<string, unknown>): Reflector {
  return {
    getAllAndOverride: vi.fn((key: string) => metadata[key]),
  } as unknown as Reflector;
}

function withTenant(permissions: string[], run: () => void): void {
  const tenant: TenantContextValue = {
    tenantId: 't1',
    tenantCode: 'demo',
    tenantStatus: 'active',
    membershipId: 'm1',
    userId: 'u1',
    permissions,
    branchScope: null,
    isOwner: false,
  };
  requestContextStorage.run({ traceId: 'trace', startTime: 0, tenant }, run);
}

describe('PermissionsGuard', () => {
  const context = contextWith({});

  it('allows a route that declares no permission', () => {
    const guard = new PermissionsGuard(reflectorFor({}));
    withTenant([], () => {
      expect(guard.canActivate(context)).toBe(true);
    });
  });

  it('skips public routes', () => {
    const guard = new PermissionsGuard(reflectorFor({ [IS_PUBLIC_KEY]: true }));
    expect(guard.canActivate(context)).toBe(true);
  });

  it('allows when the effective permission set contains the code', () => {
    const guard = new PermissionsGuard(reflectorFor({ [REQUIRED_PERMISSION_KEY]: 'platform.role.manage' }));
    withTenant(['platform.tenant.view', 'platform.role.manage'], () => {
      expect(guard.canActivate(context)).toBe(true);
    });
  });

  it('denies with FORBIDDEN and names the missing permission', () => {
    const guard = new PermissionsGuard(reflectorFor({ [REQUIRED_PERMISSION_KEY]: 'platform.role.manage' }));
    withTenant(['platform.tenant.view'], () => {
      try {
        guard.canActivate(context);
        throw new Error('expected the guard to deny');
      } catch (error) {
        expect(error).toBeInstanceOf(DomainError);
        expect((error as DomainError).code).toBe('FORBIDDEN');
        expect((error as DomainError).status).toBe(403);
        expect((error as DomainError).message).toBe('permission platform.role.manage required');
      }
    });
  });

  it('treats the owner wildcard as every permission', () => {
    const guard = new PermissionsGuard(
      reflectorFor({ [REQUIRED_PERMISSION_KEY]: 'accounting.journal.post' }),
    );
    withTenant(['*'], () => {
      expect(guard.canActivate(context)).toBe(true);
    });
  });
});
