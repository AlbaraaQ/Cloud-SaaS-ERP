/**
 * RBAC role catalogue — 2026-09 architecture/RBAC reorganisation
 * (docs/architecture-rbac/03-roles-permissions-matrix.md).
 *
 * Three independent role families that must never mix:
 *
 *   A. Platform roles — live in `platform_memberships`, grant `console.*`
 *      permissions, have no tenant. Checked by `PlatformAdminGuard` /
 *      `@RequiresPlatformRole`. The legacy `users.is_platform_admin` flag is
 *      equivalent to holding `platform_owner` and is kept only for
 *      compatibility (migration 0032 backfills it into `platform_memberships`).
 *
 *   B. Tenant administration roles — ordinary tenant roles (`roles` table)
 *      whose permission sets cover tenant self-administration (`tenant.*`,
 *      `organization.*`, `settings.*`, …). A `tenant_owner` controls **its own
 *      tenant only**: the `*` wildcard never grants `console.*`, and the
 *      platform console is unreachable without a platform membership.
 *
 *   C. ERP functional roles — ordinary tenant roles for day-to-day ERP work.
 *      A membership may hold several roles; the effective set is their UNION
 *      (DATABASE_DESIGN §2). Per-role scoping (branch / warehouse / cash
 *      location / POS terminal) is stored in `membership_role_scopes` and
 *      published on the request context.
 *
 * This file is the single source of the *default* permission sets. Tenants may
 * customise families B and C freely through the role editor; family A is managed
 * only from the platform console by `console.users.manage` holders.
 */

export type PlatformRoleCode =
  | 'platform_owner'
  | 'platform_operations'
  | 'platform_billing'
  | 'platform_support'
  | 'platform_auditor';

export type TenantAdminRoleCode =
  | 'tenant_owner'
  | 'tenant_admin'
  | 'branch_manager'
  | 'device_manager'
  | 'security_admin'
  | 'tenant_auditor';

export type ErpFunctionalRoleCode =
  | 'accountant'
  | 'sales_manager'
  | 'sales_user'
  | 'purchase_manager'
  | 'purchase_user'
  | 'inventory_manager'
  | 'warehouse_user'
  | 'cashier'
  | 'treasury_user'
  | 'hr_manager'
  | 'project_manager'
  | 'auditor'
  | 'report_viewer';

export type RoleCatalogEntry = {
  readonly code: string;
  readonly nameAr: string;
  readonly nameEn: string;
  readonly description: string;
  /** `console.*` for platform roles, tenant codes (or `*`) otherwise. */
  readonly permissions: readonly string[];
};

/** Family A — platform roles (stored in `platform_memberships`, no tenant). */
export const platformRoleCatalog: readonly RoleCatalogEntry[] = [
  {
    code: 'platform_owner',
    nameAr: 'مالك المنصة',
    nameEn: 'Platform owner',
    description: 'Full control of the SaaS platform: tenants, billing, plans, users, support.',
    permissions: [
      'console.tenants.view',
      'console.tenants.manage',
      'console.subscriptions.manage',
      'console.plans.manage',
      'console.activation.review',
      'console.users.view',
      'console.users.manage',
      'console.audit.view',
      'console.health.view',
      'console.jobs.view',
      'console.billing.manage',
      'console.support.manage',
    ],
  },
  {
    code: 'platform_operations',
    nameAr: 'تشغيل المنصة',
    nameEn: 'Platform operations',
    description: 'Day-to-day platform operations and health monitoring. No billing, no user grants.',
    permissions: [
      'console.tenants.view',
      'console.audit.view',
      'console.health.view',
      'console.jobs.view',
    ],
  },
  {
    code: 'platform_billing',
    nameAr: 'فوترة المنصة',
    nameEn: 'Platform billing',
    description: 'Subscriptions, plans, activation reviews and dunning. No tenant mutation.',
    permissions: [
      'console.tenants.view',
      'console.subscriptions.manage',
      'console.plans.manage',
      'console.activation.review',
      'console.billing.manage',
    ],
  },
  {
    code: 'platform_support',
    nameAr: 'دعم المنصة',
    nameEn: 'Platform support',
    description: 'Customer support with read-only tenant visibility and ticket handling.',
    permissions: ['console.tenants.view', 'console.health.view', 'console.support.manage'],
  },
  {
    code: 'platform_auditor',
    nameAr: 'مدقق المنصة',
    nameEn: 'Platform auditor',
    description: 'Read-only oversight across tenants, audit trail, health and queues.',
    permissions: [
      'console.tenants.view',
      'console.audit.view',
      'console.health.view',
      'console.jobs.view',
    ],
  },
] as const;

/** Family B — tenant administration roles (per-tenant, `roles` table). */
export const tenantAdminRoleCatalog: readonly RoleCatalogEntry[] = [
  {
    code: 'tenant_owner',
    nameAr: 'مالك المنشأة',
    nameEn: 'Tenant owner',
    description:
      'Full control of ONE tenant (branches, users, roles, settings). Has no platform access: `*` never grants `console.*`.',
    permissions: ['*'],
  },
  {
    code: 'tenant_admin',
    nameAr: 'مدير المنشأة',
    nameEn: 'Tenant administrator',
    description: 'Tenant configuration and user management without file-level operations.',
    permissions: [
      'tenant.view',
      'tenant.manage',
      'tenant.membership.manage',
      'tenant.settings.manage',
      'tenant.audit.view',
      'tenant.file.upload',
      'tenant.notification.view',
      'tenant.notification.manage',
      'tenant.job.view',
      'organization.branch.view',
      'organization.branch.manage',
      'organization.warehouse.view',
      'organization.warehouse.manage',
      'organization.cashlocation.view',
      'organization.cashlocation.manage',
      'organization.currency.view',
      'organization.currency.manage',
      'organization.priceList.view',
      'organization.priceList.manage',
      'organization.postingprofile.view',
      'organization.postingprofile.manage',
      'organization.companyprofile.view',
      'organization.companyprofile.manage',
      'einvoice.view',
      'einvoice.manage',
      'einvoice.credentials.manage',
      'reporting.view',
    ],
  },
  {
    code: 'branch_manager',
    nameAr: 'مدير فرع',
    nameEn: 'Branch manager',
    description: 'Manages one branch (scoped via membership branch_scope).',
    permissions: [
      'tenant.view',
      'organization.branch.view',
      'organization.warehouse.view',
      'organization.cashlocation.view',
      'catalog.item.view',
      'parties.view',
      'inventory.view',
      'inventory.transfer',
      'inventory.transfer.receive',
      'sales.view',
      'treasury.view',
      'treasury.shift.close',
      'reporting.view',
      'pos.view',
    ],
  },
  {
    code: 'device_manager',
    nameAr: 'مدير الأجهزة',
    nameEn: 'Device manager',
    description: 'Registers and maintains tenant devices and desktop-compat endpoints.',
    permissions: [
      'tenant.view',
      'tenant.device.view',
      'tenant.device.manage',
      'compat.manage',
      'compat.sync',
      'organization.branch.view',
    ],
  },
  {
    code: 'security_admin',
    nameAr: 'مسؤول الأمن',
    nameEn: 'Security administrator',
    description: 'Roles, memberships and audit review. Cannot touch billing or file operations.',
    permissions: [
      'tenant.view',
      'tenant.membership.manage',
      'tenant.role.manage',
      'tenant.audit.view',
      'tenant.settings.manage',
    ],
  },
  {
    code: 'tenant_auditor',
    nameAr: 'مدقق المنشأة',
    nameEn: 'Tenant auditor',
    description: 'Read-only access to the tenant audit trail, reports and master data.',
    permissions: [
      'tenant.view',
      'tenant.audit.view',
      'tenant.job.view',
      'organization.branch.view',
      'organization.warehouse.view',
      'organization.cashlocation.view',
      'accounting.account.view',
      'accounting.period.view',
      'accounting.reports.view',
      'sales.view',
      'purchase.view',
      'treasury.view',
      'reporting.view',
    ],
  },
] as const;

/** Family C — ERP functional roles (per-tenant, `roles` table). */
export const erpFunctionalRoleCatalog: readonly RoleCatalogEntry[] = [
  {
    code: 'accountant',
    nameAr: 'محاسب',
    nameEn: 'Accountant',
    description: 'Accounting, parties, inventory and document review without tenant administration.',
    permissions: [
      'tenant.view',
      'organization.branch.view',
      'organization.warehouse.view',
      'organization.cashlocation.view',
      'organization.currency.view',
      'catalog.item.view',
      'catalog.category.view',
      'catalog.unit.view',
      'catalog.taxgroup.view',
      'accounting.account.view',
      'accounting.account.manage',
      'accounting.costcenter.manage',
      'accounting.journal.create',
      'accounting.journal.post',
      'accounting.journal.reverse',
      'accounting.period.view',
      'accounting.period.close',
      'accounting.period.reopen',
      'accounting.opening.manage',
      'accounting.reports.view',
      'parties.view',
      'parties.manage',
      'parties.allocate',
      'inventory.view',
      'inventory.adjust',
      'inventory.adjust.approve',
      'inventory.transfer',
      'inventory.transfer.receive',
      'inventory.request.manage',
      'inventory.request.approve',
      'inventory.delivery.manage',
      'inventory.negative.override',
      'sales.view',
      'sales.invoice.create',
      'sales.invoice.post',
      'sales.invoice.void',
      'sales.adjustment.create',
      'purchase.view',
      'purchase.invoice.create',
      'purchase.invoice.post',
      'purchase.invoice.void',
      'purchase.adjustment.create',
      'treasury.view',
      'treasury.voucher.create',
      'treasury.voucher.post',
      'treasury.voucher.void',
      'treasury.cheque.clear',
      'treasury.shift.close',
      'einvoice.view',
      'einvoice.submit',
      'reporting.view',
    ],
  },
  {
    code: 'sales_manager',
    nameAr: 'مدير مبيعات',
    nameEn: 'Sales manager',
    description: 'Full sales cycle including offers, salesmen and discount overrides.',
    permissions: [
      'tenant.view',
      'catalog.item.view',
      'catalog.category.view',
      'catalog.taxgroup.view',
      'catalog.price.manage',
      'parties.view',
      'parties.manage',
      'parties.allocate',
      'parties.creditlimit.override',
      'inventory.view',
      'sales.view',
      'sales.invoice.create',
      'sales.invoice.post',
      'sales.invoice.void',
      'sales.invoice.pay',
      'sales.discount.override',
      'sales.return.create',
      'sales.adjustment.create',
      'sales.offer.manage',
      'sales.salesman.manage',
      'reporting.view',
    ],
  },
  {
    code: 'sales_user',
    nameAr: 'موظف مبيعات',
    nameEn: 'Sales user',
    description: 'Creates draft sales documents; cannot post, void or override.',
    permissions: [
      'tenant.view',
      'catalog.item.view',
      'catalog.taxgroup.view',
      'parties.view',
      'parties.manage',
      'inventory.view',
      'sales.view',
      'sales.invoice.create',
      'reporting.view',
    ],
  },
  {
    code: 'purchase_manager',
    nameAr: 'مدير مشتريات',
    nameEn: 'Purchase manager',
    description: 'Full purchase cycle including landed costs and adjustments.',
    permissions: [
      'tenant.view',
      'catalog.item.view',
      'catalog.category.view',
      'catalog.taxgroup.view',
      'parties.view',
      'parties.manage',
      'inventory.view',
      'purchase.view',
      'purchase.invoice.create',
      'purchase.invoice.post',
      'purchase.invoice.void',
      'purchase.invoice.pay',
      'purchase.cost.manage',
      'purchase.adjustment.create',
      'reporting.view',
    ],
  },
  {
    code: 'purchase_user',
    nameAr: 'موظف مشتريات',
    nameEn: 'Purchase user',
    description: 'Creates draft purchase documents; cannot post or void.',
    permissions: [
      'tenant.view',
      'catalog.item.view',
      'parties.view',
      'inventory.view',
      'purchase.view',
      'purchase.invoice.create',
      'reporting.view',
    ],
  },
  {
    code: 'inventory_manager',
    nameAr: 'مدير مخزون',
    nameEn: 'Inventory manager',
    description: 'Warehouses, adjustments, transfers, production and approvals.',
    permissions: [
      'tenant.view',
      'organization.warehouse.view',
      'catalog.item.view',
      'catalog.item.manage',
      'catalog.category.view',
      'catalog.unit.view',
      'inventory.view',
      'inventory.adjust',
      'inventory.adjust.approve',
      'inventory.transfer',
      'inventory.transfer.receive',
      'inventory.request.manage',
      'inventory.request.approve',
      'inventory.delivery.manage',
      'inventory.production.manage',
      'inventory.production.complete',
      'inventory.negative.override',
      'reporting.view',
    ],
  },
  {
    code: 'warehouse_user',
    nameAr: 'موظف مستودع',
    nameEn: 'Warehouse user',
    description: 'Day-to-day warehouse moves without approvals or negative overrides.',
    permissions: [
      'tenant.view',
      'organization.warehouse.view',
      'catalog.item.view',
      'inventory.view',
      'inventory.transfer',
      'inventory.transfer.receive',
      'inventory.request.manage',
      'inventory.delivery.manage',
      'reporting.view',
    ],
  },
  {
    code: 'cashier',
    nameAr: 'كاشير',
    nameEn: 'Cashier',
    description:
      'Human cashier: point-of-sale and treasury operations. The till hardware itself is a `devices` row, not this role.',
    permissions: [
      'tenant.view',
      'organization.branch.view',
      'organization.cashlocation.view',
      'catalog.item.view',
      'catalog.taxgroup.view',
      'parties.view',
      'parties.manage',
      'inventory.view',
      'sales.view',
      'sales.invoice.create',
      'sales.invoice.pay',
      'treasury.view',
      'treasury.voucher.create',
      'treasury.shift.close',
      'pos.view',
      'pos.operate',
    ],
  },
  {
    code: 'treasury_user',
    nameAr: 'موظف خزينة',
    nameEn: 'Treasury user',
    description: 'Vouchers, cheques and cash transfers.',
    permissions: [
      'tenant.view',
      'organization.cashlocation.view',
      'parties.view',
      'treasury.view',
      'treasury.voucher.create',
      'treasury.voucher.post',
      'treasury.voucher.void',
      'treasury.cheque.clear',
      'treasury.transfer.manage',
      'treasury.expensetype.manage',
      'reporting.view',
    ],
  },
  {
    code: 'hr_manager',
    nameAr: 'مدير موارد بشرية',
    nameEn: 'HR manager',
    description: 'HR directories, attendance, adjustments and payroll.',
    permissions: [
      'tenant.view',
      'hrm.view',
      'hrm.manage',
      'hrm.payroll.post',
      'hrm.adjust.approve',
      'reporting.view',
    ],
  },
  {
    code: 'project_manager',
    nameAr: 'مدير مشاريع',
    nameEn: 'Project manager',
    description: 'Projects, contracting, progress bills and contractor payments.',
    permissions: [
      'tenant.view',
      'projects.view',
      'projects.manage',
      'projects.bill.post',
      'projects.contractor.pay',
      'projects.stage.accredit',
      'reporting.view',
    ],
  },
  {
    code: 'auditor',
    nameAr: 'مراجع',
    nameEn: 'Auditor',
    description: 'Read-only ERP access for external review.',
    permissions: [
      'tenant.view',
      'tenant.audit.view',
      'organization.branch.view',
      'organization.warehouse.view',
      'organization.cashlocation.view',
      'catalog.item.view',
      'accounting.account.view',
      'accounting.period.view',
      'accounting.reports.view',
      'parties.view',
      'inventory.view',
      'sales.view',
      'purchase.view',
      'treasury.view',
      'einvoice.view',
      'reporting.view',
      'hrm.view',
      'projects.view',
    ],
  },
  {
    code: 'report_viewer',
    nameAr: 'مشاهد تقارير',
    nameEn: 'Report viewer',
    description: 'Runs and exports reports; reads the documents behind them.',
    permissions: [
      'tenant.view',
      'sales.view',
      'purchase.view',
      'inventory.view',
      'treasury.view',
      'accounting.reports.view',
      'reporting.view',
      'reporting.export.execute',
    ],
  },
] as const;

const platformByCode = new Map(platformRoleCatalog.map((role) => [role.code, role]));
const tenantAdminByCode = new Map(tenantAdminRoleCatalog.map((role) => [role.code, role]));
const erpByCode = new Map(erpFunctionalRoleCatalog.map((role) => [role.code, role]));

export function findPlatformRole(code: string): RoleCatalogEntry | undefined {
  return platformByCode.get(code);
}

export function findTenantAdminRole(code: string): RoleCatalogEntry | undefined {
  return tenantAdminByCode.get(code);
}

export function findErpFunctionalRole(code: string): RoleCatalogEntry | undefined {
  return erpByCode.get(code);
}

export function isPlatformRoleCode(code: string): boolean {
  return platformByCode.has(code);
}

/**
 * Effective platform permissions of a set of platform roles (UNION semantics,
 * mirroring tenant roles in DATABASE_DESIGN §2).
 */
export function platformPermissionsForRoles(codes: readonly string[]): string[] {
  const out = new Set<string>();
  for (const code of codes) {
    for (const permission of platformByCode.get(code)?.permissions ?? []) out.add(permission);
  }
  return [...out];
}
