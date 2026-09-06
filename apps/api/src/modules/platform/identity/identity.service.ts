import { Inject, Injectable } from '@nestjs/common';
import { and, eq, isNull } from 'drizzle-orm';
import {
  DomainError,
  errorCodes,
  permissionRegistry,
  type MeResponse,
  type PermissionDto,
} from '@erp/contracts';
import {
  membershipRoles,
  memberships,
  permissions,
  rolePermissions,
  roles,
  tenants,
  users,
  withTenantTx,
  withTx,
  type DatabaseHandle,
} from '@erp/database';

import type { AuthContextValue } from '../../../request-context/request-context.js';
import { DATABASE_HANDLE } from '../../../database/database.module.js';
import { toMembershipDto, toUserDto } from '../mappers.js';

/** `GET /me` and `GET /permissions` — API_CONTRACT §1. */
@Injectable()
export class IdentityService {
  constructor(@Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle) {}

  async me(auth: AuthContextValue): Promise<MeResponse> {
    const user = await withTx(this.database.db, async (tx) => {
      const rows = await tx
        .select({
          id: users.id,
          email: users.email,
          fullName: users.fullName,
          phone: users.phone,
          status: users.status,
          isPlatformAdmin: users.isPlatformAdmin,
          mustChangePassword: users.mustChangePassword,
          lastLoginAt: users.lastLoginAt,
        })
        .from(users)
        .where(eq(users.id, auth.userId))
        .limit(1);
      return rows[0];
    });

    if (!user) {
      throw new DomainError(errorCodes.UNAUTHENTICATED, 'User not found', 401);
    }

    return withTenantTx(this.database.db, auth.claimedTenantId, async (tx) => {
      const rows = await tx
        .select({
          id: memberships.id,
          tenantId: memberships.tenantId,
          tenantCode: tenants.code,
          tenantName: tenants.name,
          displayName: memberships.displayName,
          status: memberships.status,
          isOwner: memberships.isOwner,
          branchScope: memberships.branchScope,
        })
        .from(memberships)
        .innerJoin(tenants, eq(tenants.id, memberships.tenantId))
        .where(and(eq(memberships.id, auth.membershipId), isNull(memberships.deletedAt)))
        .limit(1);

      const membership = rows[0];
      if (!membership) {
        throw new DomainError(errorCodes.FORBIDDEN, 'Membership not found', 403);
      }

      const permissionRows = await tx
        .selectDistinct({ code: rolePermissions.permissionCode })
        .from(membershipRoles)
        .innerJoin(roles, eq(roles.id, membershipRoles.roleId))
        .innerJoin(rolePermissions, eq(rolePermissions.roleId, membershipRoles.roleId))
        .where(and(eq(membershipRoles.membershipId, membership.id), isNull(roles.deletedAt)));

      return {
        user: toUserDto(user),
        membership: await toMembershipDto(tx, membership),
        permissions: permissionRows.map((row) => row.code).sort(),
        branchScope: (membership.branchScope as string[] | null) ?? null,
      };
    });
  }

  /** The registry the `permissions` table is seeded from (SECURITY_ARCHITECTURE §3). */
  async listPermissions(): Promise<PermissionDto[]> {
    const rows = await withTx(this.database.db, async (tx) =>
      tx
        .select({ code: permissions.code, module: permissions.module, description: permissions.description })
        .from(permissions),
    );

    const seeded = new Map(rows.map((row) => [row.code, row]));
    // The code list is authoritative; the table only proves the seed ran.
    return permissionRegistry.map((entry) => ({
      code: entry.code,
      module: entry.module,
      description: seeded.get(entry.code)?.description ?? entry.description,
    }));
  }
}
