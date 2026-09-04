import { Injectable, type CanActivate, type ExecutionContext } from '@nestjs/common';
import { DomainError, errorCodes } from '@erp/contracts';

import { getAuthContext } from '../../../request-context/request-context.js';

/**
 * Guard stub for the platform-admin (ops) plane — PHASE_03 §4 explicitly keeps the
 * plane's endpoints out of scope ("P23 lists only; guard stub exists") while fixing its
 * entry rule now, so no tenant route can ever be reachable through it.
 *
 * MULTI_TENANCY §4: platform admin access is a separate plane, always audited, never
 * implied by tenant permissions (SECURITY_ARCHITECTURE §3).
 */
@Injectable()
export class PlatformAdminGuard implements CanActivate {
  canActivate(_context: ExecutionContext): boolean {
    const auth = getAuthContext();
    if (!auth.isPlatformAdmin) {
      throw new DomainError(errorCodes.FORBIDDEN, 'platform-admin plane requires is_platform_admin', 403);
    }
    // TODO(phase:23): write the break-glass audit row (actor, reason, target tenant).
    return true;
  }
}
