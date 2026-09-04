import { z } from 'zod';

import { uuidSchema } from '../ids.js';

/**
 * Auth & Identity DTOs — API_CONTRACT §1. Shapes are normative; the server validates
 * every body with these schemas (SECURITY_ARCHITECTURE §6).
 */

/** SECURITY_ARCHITECTURE §2 — 12+ character policy, checked at the boundary and again
 * by the password-policy service (which also runs the breach-list check). */
export const MIN_PASSWORD_LENGTH = 12;
export const MAX_PASSWORD_LENGTH = 200;

export const loginRequestSchema = z
  .object({
    email: z.string().trim().email().max(320),
    password: z.string().min(1).max(MAX_PASSWORD_LENGTH),
    tenantCode: z.string().trim().min(1).max(64),
    mfaCode: z.string().trim().length(6).optional(),
  })
  .strict();

export type LoginRequest = z.infer<typeof loginRequestSchema>;

export const refreshRequestSchema = z
  .object({
    refreshToken: z.string().min(20).max(512),
  })
  .strict();

export type RefreshRequest = z.infer<typeof refreshRequestSchema>;

export const logoutRequestSchema = z.object({}).strict();

export type LogoutRequest = z.infer<typeof logoutRequestSchema>;

export const changePasswordRequestSchema = z
  .object({
    current: z.string().min(1).max(MAX_PASSWORD_LENGTH),
    new: z.string().min(MIN_PASSWORD_LENGTH).max(MAX_PASSWORD_LENGTH),
  })
  .strict();

export type ChangePasswordRequest = z.infer<typeof changePasswordRequestSchema>;

export const userStatusSchema = z.enum(['active', 'invited', 'suspended']);
export type UserStatus = z.infer<typeof userStatusSchema>;

export const userDtoSchema = z.object({
  id: uuidSchema,
  email: z.string(),
  fullName: z.string(),
  phone: z.string().nullable(),
  status: userStatusSchema,
  isPlatformAdmin: z.boolean(),
  mustChangePassword: z.boolean(),
  lastLoginAt: z.string().nullable(),
});

export type UserDto = z.infer<typeof userDtoSchema>;

export const membershipStatusSchema = z.enum(['active', 'invited', 'suspended']);
export type MembershipStatus = z.infer<typeof membershipStatusSchema>;

export const roleDtoSchema = z.object({
  id: uuidSchema,
  name: z.string(),
  description: z.string().nullable(),
  isSystem: z.boolean(),
});

export type RoleDto = z.infer<typeof roleDtoSchema>;

export const membershipDtoSchema = z.object({
  id: uuidSchema,
  tenantId: uuidSchema,
  tenantCode: z.string(),
  tenantName: z.string(),
  displayName: z.string(),
  status: membershipStatusSchema,
  isOwner: z.boolean(),
  branchScope: z.array(uuidSchema).nullable(),
  roles: z.array(roleDtoSchema),
});

export type MembershipDto = z.infer<typeof membershipDtoSchema>;

export const tokenPairSchema = z.object({
  tokenType: z.literal('Bearer'),
  accessToken: z.string(),
  refreshToken: z.string(),
  /** Access-token lifetime in seconds (PROJECT_CONTRACT §9: 900). */
  expiresIn: z.number().int().positive(),
});

export type TokenPair = z.infer<typeof tokenPairSchema>;

export const loginResponseSchema = z.object({
  accessToken: z.string(),
  refreshToken: z.string(),
  tokenType: z.literal('Bearer'),
  expiresIn: z.number().int().positive(),
  user: userDtoSchema,
  memberships: z.array(membershipDtoSchema),
});

export type LoginResponse = z.infer<typeof loginResponseSchema>;

export const meResponseSchema = z.object({
  user: userDtoSchema,
  membership: membershipDtoSchema,
  permissions: z.array(z.string()),
  branchScope: z.array(uuidSchema).nullable(),
});

export type MeResponse = z.infer<typeof meResponseSchema>;

export const permissionDtoSchema = z.object({
  code: z.string(),
  module: z.string(),
  description: z.string(),
});

export type PermissionDto = z.infer<typeof permissionDtoSchema>;
