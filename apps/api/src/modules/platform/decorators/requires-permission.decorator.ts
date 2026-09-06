import { SetMetadata } from '@nestjs/common';

/**
 * Declares the permission a route requires (SECURITY_ARCHITECTURE §3). Codes come from
 * the registry in `@erp/contracts` and use the `module.entity.action` shape
 * (PROJECT_CONTRACT §1).
 */
export const REQUIRED_PERMISSION_KEY = 'erp:requiredPermission';

export const RequiresPermission = (code: string) => SetMetadata(REQUIRED_PERMISSION_KEY, code);
