import { permissionRegistry } from '@erp/contracts';
import {
  assignRoleFixture,
  createMembershipFixture,
  createRoleFixture,
  createTenantFixture,
  createUserFixture,
  setMembershipStatusFixture,
  setTenantStatusFixture,
  setTenantSettingFixture,
} from '@erp/testing';

import { PasswordService, TokenService } from '../src/modules/platform/index.js';

import type { TestApp } from './test-app.js';

export const ALL_PLATFORM_PERMISSIONS = permissionRegistry
  .filter((entry) => entry.module === 'platform')
  .map((entry) => entry.code);

/** Every PHASE_05 permission — the organization suites need all of them (PHASE_05 §7). */
export const ALL_ORGANIZATION_PERMISSIONS = permissionRegistry
  .filter((entry) => entry.module === 'organization')
  .map((entry) => entry.code);

export type ActorOptions = {
  tenantCode: string;
  /**
   * Reuses an existing tenant instead of creating one. This is what lets a suite put two
   * memberships — e.g. an unrestricted admin and a branch-scoped user — inside the same
   * tenant, which is the only way to test `branch_scope` filtering.
   */
  tenantId?: string;
  tenantName?: string;
  email: string;
  fullName?: string;
  password?: string;
  permissions?: readonly string[];
  roleNames?: readonly string[];
  tenantStatus?: 'active' | 'suspended' | 'archived';
  membershipStatus?: 'active' | 'invited' | 'suspended';
  branchScope?: string[] | null;
  isOwner?: boolean;
  isPlatformAdmin?: boolean;
};

export type Actor = {
  label: string;
  tenantId: string;
  tenantCode: string;
  userId: string;
  email: string;
  password?: string;
  membershipId: string;
  roleIds: string[];
  token: string;
  branchScope: string[] | null;
};

/** Hashes with the frozen Argon2id parameters (PROJECT_CONTRACT §9). */
export function hashPassword(password: string): Promise<string> {
  // A standalone instance: fixtures run outside the DI container.
  return new PasswordService().hash(password);
}

/**
 * Creates a tenant + user + membership + role and returns an actor with a valid access
 * token signed by the application's own TokenService.
 */
export async function createActor(ctx: TestApp, options: ActorOptions): Promise<Actor> {
  const tenant = options.tenantId
    ? { id: options.tenantId, code: options.tenantCode }
    : await createTenantFixture(ctx.db.ownerUrl, {
        code: options.tenantCode,
        name: options.tenantName,
        status: options.tenantStatus ?? 'active',
      });

  const permissionCodes = options.permissions ?? ALL_PLATFORM_PERMISSIONS;
  const roleIds: string[] = [];
  for (const [index, roleName] of (options.roleNames ?? ['Admin']).entries()) {
    const role = await createRoleFixture(ctx.db.ownerUrl, {
      tenantId: tenant.id,
      name: roleName,
      permissionCodes: index === 0 ? permissionCodes : [],
    });
    roleIds.push(role.id);
  }

  const passwordHash = options.password ? await new PasswordService().hash(options.password) : null;
  const user = await createUserFixture(ctx.db.ownerUrl, {
    email: options.email,
    fullName: options.fullName ?? options.email.split('@')[0],
    passwordHash,
    status: options.password ? 'active' : 'invited',
  });

  const membership = await createMembershipFixture(ctx.db.ownerUrl, {
    tenantId: tenant.id,
    userId: user.id,
    displayName: options.fullName ?? options.email.split('@')[0] ?? 'Actor',
    status: options.membershipStatus ?? 'active',
    isOwner: options.isOwner ?? true,
    branchScope: options.branchScope === undefined ? null : options.branchScope,
  });

  for (const roleId of roleIds) {
    await assignRoleFixture(ctx.db.ownerUrl, membership.id, roleId);
  }

  const tokens = ctx.app.get(TokenService);
  const { token } = await tokens.signAccessToken({
    sub: user.id,
    tid: tenant.id,
    mid: membership.id,
    scope: ['erp'],
    pam: options.isPlatformAdmin ?? false,
  });

  return {
    label: options.tenantCode,
    tenantId: tenant.id,
    tenantCode: tenant.code,
    userId: user.id,
    email: user.email,
    password: options.password,
    membershipId: membership.id,
    roleIds,
    token,
    branchScope: options.branchScope === undefined ? null : options.branchScope,
  };
}

export {
  assignRoleFixture,
  createMembershipFixture,
  createRoleFixture,
  createTenantFixture,
  createUserFixture,
  setMembershipStatusFixture,
  setTenantSettingFixture,
  setTenantStatusFixture,
};
