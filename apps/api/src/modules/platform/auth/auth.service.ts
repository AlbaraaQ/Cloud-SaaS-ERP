import { Inject, Injectable } from '@nestjs/common';
import { and, eq, isNull, sql } from 'drizzle-orm';
import { env } from '@erp/config';
import {
  DomainError,
  errorCodes,
  newId,
  type ChangePasswordRequest,
  type LoginRequest,
  type LoginResponse,
  type MembershipDto,
  type RefreshRequest,
} from '@erp/contracts';
import {
  memberships,
  refreshTokens,
  tenants,
  users,
  withTenantTx,
  withTx,
  type DatabaseHandle,
} from '@erp/database';

import type { AuthContextValue } from '../../../request-context/request-context.js';
import { DATABASE_HANDLE } from '../../../database/database.module.js';
import { toMembershipDto, toUserDto, type MembershipRow, type UserRow } from '../mappers.js';

import { PasswordService } from './password.service.js';
import { TokenService } from './token.service.js';

/**
 * Authentication flows — API_CONTRACT §1, SECURITY_ARCHITECTURE §2.
 *
 * Design notes that are deliberate, not accidental:
 *
 * - Login failures return the *same* `UNAUTHENTICATED` response whether the e-mail, the
 *   tenant code or the password was wrong, so the endpoint cannot enumerate accounts.
 * - The membership row is always read **under the RLS GUC of the tenant being logged
 *   into**, so a valid user with no membership in that tenant sees nothing at all.
 * - `memberships` in the response contains the membership of the tenant that was
 *   authenticated. Listing every membership of the user would require a cross-tenant
 *   read that RLS exists to prevent; the login DTO already requires `tenantCode`.
 */

export type RequestMeta = { ip?: string; userAgent?: string };

const USER_COLUMNS = {
  id: users.id,
  email: users.email,
  fullName: users.fullName,
  phone: users.phone,
  status: users.status,
  isPlatformAdmin: users.isPlatformAdmin,
  mustChangePassword: users.mustChangePassword,
  lastLoginAt: users.lastLoginAt,
};

const MEMBERSHIP_COLUMNS = {
  id: memberships.id,
  tenantId: memberships.tenantId,
  tenantCode: tenants.code,
  tenantName: tenants.name,
  displayName: memberships.displayName,
  status: memberships.status,
  isOwner: memberships.isOwner,
  branchScope: memberships.branchScope,
};

@Injectable()
export class AuthService {
  constructor(
    @Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle,
    private readonly tokens: TokenService,
    private readonly passwords: PasswordService,
  ) {}

  async login(input: LoginRequest, meta: RequestMeta = {}): Promise<LoginResponse> {
    const tenant = await withTx(this.database.db, async (tx) => {
      const rows = await tx
        .select({ id: tenants.id, code: tenants.code, name: tenants.name, status: tenants.status })
        .from(tenants)
        .where(sql`lower(${tenants.code}) = lower(${input.tenantCode})`)
        .limit(1);
      return rows[0];
    });

    if (!tenant) {
      throw new DomainError(errorCodes.UNAUTHENTICATED, 'Invalid e-mail, tenant or password', 401);
    }

    const user = await withTx(this.database.db, async (tx) => {
      const rows = await tx
        .select({ ...USER_COLUMNS, passwordHash: users.passwordHash, lockedUntil: users.lockedUntil })
        .from(users)
        .where(eq(users.email, input.email))
        .limit(1);
      return rows[0];
    });

    if (!user) {
      throw new DomainError(errorCodes.UNAUTHENTICATED, 'Invalid e-mail, tenant or password', 401);
    }

    if (user.lockedUntil && user.lockedUntil.getTime() > Date.now()) {
      const retryAfterSeconds = Math.ceil((user.lockedUntil.getTime() - Date.now()) / 1000);
      throw new DomainError(
        errorCodes.RATE_LIMITED,
        `Account temporarily locked after repeated failed logins; retry in ${retryAfterSeconds}s`,
        429,
      );
    }

    const membership = await this.findActiveMembership(tenant.id, user.id);
    if (!membership) {
      throw new DomainError(errorCodes.UNAUTHENTICATED, 'Invalid e-mail, tenant or password', 401);
    }

    if (tenant.status !== 'active') {
      throw new DomainError(errorCodes.TENANT_SUSPENDED, `Tenant '${tenant.code}' is ${tenant.status}`, 423);
    }

    const passwordOk = await this.passwords.verify(user.passwordHash, input.password);
    if (!passwordOk) {
      await this.registerFailedLogin(user.id);
      throw new DomainError(errorCodes.UNAUTHENTICATED, 'Invalid e-mail, tenant or password', 401);
    }

    if (input.mfaCode) {
      // MFA data columns exist (DATABASE_DESIGN §1) but enrolment/verification is out of
      // scope for PHASE_03 (§4 "DO NOT DO"). Rejecting an supplied code is safer than
      // silently ignoring it. TODO(phase:23): TOTP verification.
      throw new DomainError(errorCodes.VALIDATION_FAILED, 'MFA is not enabled for this tenant', 400, {
        field: 'mfaCode',
      });
    }

    await this.registerSuccessfulLogin(user.id);

    return this.issueSession(user, membership, meta);
  }

  async refresh(input: RefreshRequest, meta: RequestMeta = {}): Promise<LoginResponse> {
    const tokenHash = this.tokens.hashRefreshToken(input.refreshToken);

    const stored = await withTx(this.database.db, async (tx) => {
      const rows = await tx
        .select({
          id: refreshTokens.id,
          userId: refreshTokens.userId,
          family: refreshTokens.family,
          tenantId: refreshTokens.tenantId,
          expiresAt: refreshTokens.expiresAt,
          revokedAt: refreshTokens.revokedAt,
        })
        .from(refreshTokens)
        .where(eq(refreshTokens.tokenHash, tokenHash))
        .limit(1);
      return rows[0];
    });

    if (!stored) {
      throw new DomainError(errorCodes.UNAUTHENTICATED, 'Refresh token is not recognised', 401);
    }

    // Reuse detection: presenting an already-rotated token revokes the whole family.
    if (stored.revokedAt) {
      await this.revokeFamily(stored.family);
      throw new DomainError(errorCodes.UNAUTHENTICATED, 'Refresh token reuse detected', 401);
    }

    if (stored.expiresAt.getTime() <= Date.now()) {
      throw new DomainError(errorCodes.UNAUTHENTICATED, 'Refresh token has expired', 401);
    }

    const user = await withTx(this.database.db, async (tx) => {
      const rows = await tx.select(USER_COLUMNS).from(users).where(eq(users.id, stored.userId)).limit(1);
      return rows[0];
    });
    if (!user || user.status !== 'active') {
      throw new DomainError(errorCodes.UNAUTHENTICATED, 'User is not active', 401);
    }

    if (!stored.tenantId) {
      throw new DomainError(errorCodes.UNAUTHENTICATED, 'Refresh token is not bound to a tenant', 401);
    }

    const tenant = await withTx(this.database.db, async (tx) => {
      const rows = await tx
        .select({ id: tenants.id, code: tenants.code, name: tenants.name, status: tenants.status })
        .from(tenants)
        .where(eq(tenants.id, stored.tenantId as string))
        .limit(1);
      return rows[0];
    });
    if (!tenant || tenant.status !== 'active') {
      throw new DomainError(errorCodes.UNAUTHENTICATED, 'Tenant is not active', 401);
    }

    const membership = await this.findActiveMembership(tenant.id, user.id);
    if (!membership) {
      throw new DomainError(errorCodes.UNAUTHENTICATED, 'Membership is no longer active', 401);
    }

    // Rotation: the presented token is marked revoked and points at its replacement, so a
    // later replay of the same value is detectable.
    const rotatedRefreshToken = await withTx(this.database.db, async (tx) => {
      const newTokenId = newId();
      const plaintext = this.tokens.generateRefreshToken();
      await tx
        .update(refreshTokens)
        .set({ revokedAt: new Date(), replacedBy: newTokenId })
        .where(eq(refreshTokens.id, stored.id));
      await tx.insert(refreshTokens).values({
        id: newTokenId,
        userId: user.id,
        tokenHash: this.tokens.hashRefreshToken(plaintext),
        family: stored.family,
        tenantId: tenant.id,
        expiresAt: new Date(Date.now() + this.tokens.refreshTtlSeconds * 1000),
        ip: meta.ip ?? null,
        userAgent: meta.userAgent ?? null,
      });
      return plaintext;
    });

    const accessToken = await this.tokens.signAccessToken({
      sub: user.id,
      tid: tenant.id,
      mid: membership.id,
      scope: ['erp'],
      pam: user.isPlatformAdmin,
    });

    return {
      accessToken: accessToken.token,
      refreshToken: rotatedRefreshToken,
      tokenType: 'Bearer',
      expiresIn: this.tokens.accessTtlSeconds,
      user: toUserDto(user),
      memberships: [await this.toMembershipDto(membership)],
    };
  }

  /** Revokes every still-valid refresh token of the user (API_CONTRACT §1 logout). */
  async logout(auth: AuthContextValue): Promise<void> {
    await withTx(this.database.db, async (tx) => {
      await tx
        .update(refreshTokens)
        .set({ revokedAt: new Date() })
        .where(and(eq(refreshTokens.userId, auth.userId), isNull(refreshTokens.revokedAt)));
    });
  }

  async changePassword(auth: AuthContextValue, input: ChangePasswordRequest): Promise<void> {
    const user = await withTx(this.database.db, async (tx) => {
      const rows = await tx
        .select({ ...USER_COLUMNS, passwordHash: users.passwordHash })
        .from(users)
        .where(eq(users.id, auth.userId))
        .limit(1);
      return rows[0];
    });
    if (!user) {
      throw new DomainError(errorCodes.UNAUTHENTICATED, 'User not found', 401);
    }

    const currentOk = await this.passwords.verify(user.passwordHash, input.current);
    if (!currentOk) {
      throw new DomainError(errorCodes.VALIDATION_FAILED, 'Current password is incorrect', 400, {
        field: 'current',
      });
    }

    this.passwords.assertPolicy(input.new, { email: user.email, fullName: user.fullName });
    const passwordHash = await this.passwords.hash(input.new);

    await withTx(this.database.db, async (tx) => {
      await tx
        .update(users)
        .set({
          passwordHash,
          mustChangePassword: false,
          passwordChangedAt: new Date(),
          failedLoginAttempts: 0,
          lockedUntil: null,
          updatedAt: new Date(),
          updatedBy: user.id,
          version: sql`${users.version} + 1`,
        })
        .where(eq(users.id, user.id));
      // Password change invalidates every existing session (SECURITY_ARCHITECTURE §2).
      await tx
        .update(refreshTokens)
        .set({ revokedAt: new Date() })
        .where(and(eq(refreshTokens.userId, user.id), isNull(refreshTokens.revokedAt)));
    });
  }

  // --- internals ---------------------------------------------------------------

  private async findActiveMembership(tenantId: string, userId: string): Promise<MembershipRow | undefined> {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const rows = await tx
        .select(MEMBERSHIP_COLUMNS)
        .from(memberships)
        .innerJoin(tenants, eq(tenants.id, memberships.tenantId))
        .where(
          and(
            eq(memberships.tenantId, tenantId),
            eq(memberships.userId, userId),
            eq(memberships.status, 'active'),
            isNull(memberships.deletedAt),
          ),
        )
        .limit(1);
      return rows[0];
    });
  }

  private async registerFailedLogin(userId: string): Promise<void> {
    await withTx(this.database.db, async (tx) => {
      await tx
        .update(users)
        .set({
          failedLoginAttempts: sql`${users.failedLoginAttempts} + 1`,
          lockedUntil: sql`CASE
              WHEN ${users.failedLoginAttempts} + 1 >= ${env.AUTH_LOGIN_MAX_FAILURES}
              THEN now() + (${env.AUTH_LOCKOUT_MINUTES} || ' minutes')::interval
              ELSE ${users.lockedUntil}
            END`,
          updatedAt: new Date(),
        })
        .where(eq(users.id, userId));
    });
  }

  private async registerSuccessfulLogin(userId: string): Promise<void> {
    await withTx(this.database.db, async (tx) => {
      await tx
        .update(users)
        .set({ lastLoginAt: new Date(), failedLoginAttempts: 0, lockedUntil: null })
        .where(eq(users.id, userId));
    });
  }

  private async revokeFamily(family: string): Promise<void> {
    await withTx(this.database.db, async (tx) => {
      await tx
        .update(refreshTokens)
        .set({ revokedAt: new Date() })
        .where(and(eq(refreshTokens.family, family), isNull(refreshTokens.revokedAt)));
    });
  }

  private async issueSession(
    user: UserRow,
    membership: MembershipRow,
    meta: RequestMeta,
  ): Promise<LoginResponse> {
    const accessToken = await this.tokens.signAccessToken({
      sub: user.id,
      tid: membership.tenantId,
      mid: membership.id,
      scope: ['erp'],
      pam: user.isPlatformAdmin,
    });

    const refreshToken = this.tokens.generateRefreshToken();
    await withTx(this.database.db, async (tx) => {
      await tx.insert(refreshTokens).values({
        id: newId(),
        userId: user.id,
        tokenHash: this.tokens.hashRefreshToken(refreshToken),
        family: newId(),
        tenantId: membership.tenantId,
        expiresAt: new Date(Date.now() + this.tokens.refreshTtlSeconds * 1000),
        ip: meta.ip ?? null,
        userAgent: meta.userAgent ?? null,
      });
    });

    return {
      accessToken: accessToken.token,
      refreshToken,
      tokenType: 'Bearer',
      expiresIn: this.tokens.accessTtlSeconds,
      user: toUserDto(user),
      memberships: [await this.toMembershipDto(membership)],
    };
  }

  private async toMembershipDto(membership: MembershipRow): Promise<MembershipDto> {
    return withTenantTx(this.database.db, membership.tenantId, async (tx) => toMembershipDto(tx, membership));
  }
}
