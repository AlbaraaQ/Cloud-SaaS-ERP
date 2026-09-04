import { sql } from 'drizzle-orm';
import {
  boolean,
  index,
  jsonb,
  pgTable,
  primaryKey,
  text,
  timestamp,
  uniqueIndex,
  uuid,
} from 'drizzle-orm/pg-core';

import { baseAuditColumns, baseSoftDeleteColumns } from '../columns.js';

import { permissions, tenants, users } from './platform.js';

/**
 * Tenancy & access tables — DATABASE_DESIGN §2 and §3.
 * Every table here is tenant-scoped and carries an RLS policy (MULTI_TENANCY §3.5).
 */

export const memberships = pgTable(
  'memberships',
  {
    id: uuid('id').primaryKey(),
    tenantId: uuid('tenant_id')
      .notNull()
      .references(() => tenants.id, { onDelete: 'cascade' }),
    userId: uuid('user_id')
      .notNull()
      .references(() => users.id, { onDelete: 'cascade' }),
    displayName: text('display_name').notNull(),
    /** MULTI_TENANCY §2 — NULL means "all branches"; otherwise an array of branch ids. */
    branchScope: jsonb('branch_scope').$type<string[] | null>(),
    /** CHECK(active,invited,suspended) */
    status: text('status').notNull().default('invited'),
    isOwner: boolean('is_owner').notNull().default(false),
    ...baseAuditColumns(),
    ...baseSoftDeleteColumns(),
  },
  (table) => ({
    membershipsTenantUserUnique: uniqueIndex('memberships_tenant_user_key')
      .on(table.tenantId, table.userId)
      .where(sql`deleted_at IS NULL`),
    membershipsTenantIdx: index('memberships_tenant_id_idx').on(table.tenantId),
    membershipsUserIdx: index('memberships_user_id_idx').on(table.userId),
  }),
);

export const roles = pgTable(
  'roles',
  {
    id: uuid('id').primaryKey(),
    tenantId: uuid('tenant_id')
      .notNull()
      .references(() => tenants.id, { onDelete: 'cascade' }),
    name: text('name').notNull(),
    isSystem: boolean('is_system').notNull().default(false),
    description: text('description'),
    ...baseAuditColumns(),
    ...baseSoftDeleteColumns(),
  },
  (table) => ({
    rolesTenantNameUnique: uniqueIndex('roles_tenant_id_name_key')
      .on(table.tenantId, table.name)
      .where(sql`deleted_at IS NULL`),
    rolesTenantIdx: index('roles_tenant_id_idx').on(table.tenantId),
  }),
);

export const rolePermissions = pgTable(
  'role_permissions',
  {
    roleId: uuid('role_id')
      .notNull()
      .references(() => roles.id, { onDelete: 'cascade' }),
    permissionCode: text('permission_code')
      .notNull()
      .references(() => permissions.code, { onDelete: 'cascade' }),
  },
  (table) => ({
    rolePermissionsPk: primaryKey({ columns: [table.roleId, table.permissionCode] }),
  }),
);

export const membershipRoles = pgTable(
  'membership_roles',
  {
    membershipId: uuid('membership_id')
      .notNull()
      .references(() => memberships.id, { onDelete: 'cascade' }),
    roleId: uuid('role_id')
      .notNull()
      .references(() => roles.id, { onDelete: 'cascade' }),
  },
  (table) => ({
    membershipRolesPk: primaryKey({ columns: [table.membershipId, table.roleId] }),
    membershipRolesRoleIdx: index('membership_roles_role_id_idx').on(table.roleId),
  }),
);

export const tenantSettings = pgTable(
  'tenant_settings',
  {
    tenantId: uuid('tenant_id')
      .notNull()
      .references(() => tenants.id, { onDelete: 'cascade' }),
    key: text('key').notNull(),
    value: jsonb('value').$type<string | boolean | number | null>().notNull(),
    updatedAt: timestamp('updated_at', { withTimezone: true }).notNull().defaultNow(),
  },
  (table) => ({
    tenantSettingsPk: primaryKey({ columns: [table.tenantId, table.key] }),
  }),
);

export type Membership = typeof memberships.$inferSelect;
export type NewMembership = typeof memberships.$inferInsert;
export type Role = typeof roles.$inferSelect;
export type NewRole = typeof roles.$inferInsert;
export type RolePermission = typeof rolePermissions.$inferSelect;
export type MembershipRole = typeof membershipRoles.$inferSelect;
export type TenantSetting = typeof tenantSettings.$inferSelect;
