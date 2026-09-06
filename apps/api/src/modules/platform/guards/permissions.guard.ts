import { Injectable, type CanActivate, type ExecutionContext } from '@nestjs/common';
import { Reflector } from '@nestjs/core';
import { DomainError, errorCodes } from '@erp/contracts';
import { ALL_PERMISSIONS } from '@erp/contracts';

import { getRequestContext } from '../../../request-context/request-context.js';
import { IS_PUBLIC_KEY } from '../decorators/public.decorator.js';
import { REQUIRED_PERMISSION_KEY } from '../decorators/requires-permission.decorator.js';

/**
 * Pipeline position: last guard, after validation-independent checks
 * (API_ARCHITECTURE §2). Effective permission set = UNION(roles) (DATABASE_DESIGN §2).
 */
@Injectable()
export class PermissionsGuard implements CanActivate {
  constructor(private readonly reflector: Reflector) {}

  canActivate(context: ExecutionContext): boolean {
    const isPublic = this.reflector.getAllAndOverride<boolean>(IS_PUBLIC_KEY, [
      context.getHandler(),
      context.getClass(),
    ]);
    if (isPublic) return true;

    const required = this.reflector.getAllAndOverride<string | undefined>(REQUIRED_PERMISSION_KEY, [
      context.getHandler(),
      context.getClass(),
    ]);
    if (!required) return true;

    const granted = getRequestContext().tenant?.permissions ?? [];
    if (granted.includes(ALL_PERMISSIONS) || granted.includes(required)) return true;

    throw new DomainError(errorCodes.FORBIDDEN, `permission ${required} required`, 403, {
      field: 'permission',
      message: required,
    });
  }
}
