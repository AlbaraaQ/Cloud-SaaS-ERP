/**
 * Permission registry — SECURITY_ARCHITECTURE §3 ("Static permission registry
 * (`permissions` table seeded from code list)") and PROJECT_CONTRACT §1 naming
 * (`module.entity.action`).
 *
 * This code list is the single source that the `permissions` table is seeded from
 * (PHASE_03 §5.7). The matrix in SECURITY_ARCHITECTURE §5 is a summary of it.
 * Extend only forward — never rename or remove a code without an ADR.
 *
 * ---------------------------------------------------------------------------
 * 2026-09 architecture/RBAC reorganisation — namespace fix (docs/architecture-rbac/).
 *
 * The historic `platform.*` prefix was semantically wrong: those ten codes never
 * described the SaaS *platform* — they describe a tenant **administering itself**
 * (own tenant record, own memberships, own roles, own audit log…). Their canonical
 * home is now the `tenant.*` namespace. The old codes stay in this registry,
 * flagged `deprecated`, and keep working forever through the alias map below:
 *
 *   - reads (`PermissionsGuard`, `GET /me`) accept either spelling;
 *   - writes (`POST /roles`, seed scripts) normalise legacy → canonical, so the
 *     database converges on the canonical spelling without a lossy migration.
 *
 * The `console.*` namespace is the opposite side of the same fix: permissions of
 * the *platform console* (apps/platform-admin). They live in
 * `platformPermissionRegistry` — deliberately NOT in `permissionRegistry` — so no
 * tenant flow (role editor, owner expansion, `*` wildcard) can ever grant them.
 * `RolesService` additionally rejects them with 422 if they are submitted.
 */

export type PermissionDefinition = {
  readonly code: string;
  readonly module: string;
  readonly description: string;
  /**
   * Legacy spelling kept for compatibility. New grants must use `canonical`
   * instead; seed and role-write paths normalise automatically.
   */
  readonly deprecated?: boolean;
  /** Canonical replacement of a deprecated code. */
  readonly canonical?: string;
};

function perm(code: string, description: string): PermissionDefinition {
  return { code, module: code.split('.')[0] as string, description };
}

function legacy(code: string, canonical: string, description: string): PermissionDefinition {
  return { code, module: code.split('.')[0] as string, description, deprecated: true, canonical };
}

/**
 * Legacy → canonical reclassification. A tenant permission granted under either
 * spelling authorises the same operation (see `permissionGrants`).
 */
export const permissionAliases: Readonly<Record<string, string>> = {
  'platform.tenant.view': 'tenant.view',
  'platform.tenant.manage': 'tenant.manage',
  'platform.membership.manage': 'tenant.membership.manage',
  'platform.role.manage': 'tenant.role.manage',
  'platform.settings.manage': 'tenant.settings.manage',
  'platform.audit.view': 'tenant.audit.view',
  'platform.file.upload': 'tenant.file.upload',
  'platform.notification.view': 'tenant.notification.view',
  'platform.notification.manage': 'tenant.notification.manage',
  'platform.job.view': 'tenant.job.view',
} as const;

/** Reverse lookup: canonical → legacy spelling (kept for audit display of old rows). */
export const canonicalToLegacy: Readonly<Record<string, string>> = Object.fromEntries(
  Object.entries(permissionAliases).map(([oldCode, newCode]) => [newCode, oldCode]),
);

export const permissionRegistry: readonly PermissionDefinition[] = [
  // tenant self-administration — canonical `tenant.*` spelling (2026-09).
  perm('tenant.view', 'Read the own tenant record and its effective settings.'),
  perm('tenant.manage', 'Update the own tenant record and typed settings in bulk.'),
  perm('tenant.membership.manage', 'Invite, update and remove tenant memberships.'),
  perm('tenant.role.manage', 'Create and maintain roles and their permission sets.'),
  perm('tenant.settings.manage', 'Read and write individual typed tenant settings.'),
  perm('tenant.audit.view', 'Read the tenant audit log.'),
  perm('tenant.file.upload', 'Request pre-signed uploads, attach and download files.'),
  perm('tenant.notification.view', 'Read own in-app notifications and mark them read.'),
  perm('tenant.notification.manage', 'Create notifications for other memberships of the tenant.'),
  perm('tenant.job.view', 'Read the transactional outbox and background-queue health.'),
  // tenant device registry (2026-09) — canonical Device entity, see `devices` table.
  perm('tenant.device.view', 'List and read registered tenant devices.'),
  perm('tenant.device.manage', 'Register, activate, suspend and rotate credentials of tenant devices.'),

  // tenant self-administration — legacy `platform.*` spelling (deprecated, still honoured).
  legacy('platform.tenant.view', 'tenant.view', 'Read the own tenant record and its effective settings.'),
  legacy('platform.tenant.manage', 'tenant.manage', 'Update the own tenant record and typed settings in bulk.'),
  legacy('platform.membership.manage', 'tenant.membership.manage', 'Invite, update and remove tenant memberships.'),
  legacy('platform.role.manage', 'tenant.role.manage', 'Create and maintain roles and their permission sets.'),
  legacy('platform.settings.manage', 'tenant.settings.manage', 'Read and write individual typed tenant settings.'),
  legacy('platform.audit.view', 'tenant.audit.view', 'Read the tenant audit log.'),
  legacy('platform.file.upload', 'tenant.file.upload', 'Request pre-signed uploads, attach and download files.'),
  legacy(
    'platform.notification.view',
    'tenant.notification.view',
    'Read own in-app notifications and mark them read.',
  ),
  legacy(
    'platform.notification.manage',
    'tenant.notification.manage',
    'Create notifications for other memberships of the tenant.',
  ),
  legacy(
    'platform.job.view',
    'tenant.job.view',
    'Read the transactional outbox and background-queue health.',
  ),

  // organization (PHASE_05)
  perm('organization.branch.view', 'List and read branches.'),
  perm('organization.branch.manage', 'Create and update branches.'),
  perm('organization.warehouse.view', 'List and read warehouses.'),
  perm('organization.warehouse.manage', 'Create and update warehouses.'),
  perm('organization.cashlocation.view', 'List and read safes and bank accounts.'),
  perm('organization.cashlocation.manage', 'Create and update safes and bank accounts.'),
  perm('organization.currency.view', 'List and read currencies and FX rates.'),
  perm('organization.currency.manage', 'Create and update currencies and FX rates.'),
  perm('organization.priceList.view', 'List and read price lists.'),
  perm('organization.priceList.manage', 'Create and update price lists.'),
  perm('organization.postingprofile.manage', 'Maintain branch posting profiles.'),
  perm('organization.companyprofile.manage', 'Maintain the company profile.'),
  // Added by PHASE_05 (CR-007): the registry shipped write codes for these two
  // resources but no read code, which would have forced a reader to hold `manage`.
  perm('organization.companyprofile.view', 'Read the company profile.'),
  perm('organization.postingprofile.view', 'Read branch posting profiles and resolve them.'),

  // catalog (PHASE_06/07)
  perm('catalog.item.view', 'List and read items.'),
  perm('catalog.item.manage', 'Create and update items.'),
  perm('catalog.category.view', 'List and read item categories.'),
  perm('catalog.category.manage', 'Create and update item categories.'),
  perm('catalog.unit.view', 'List and read units of measure.'),
  perm('catalog.unit.manage', 'Create and update units of measure.'),
  perm('catalog.taxgroup.view', 'List and read tax groups.'),
  perm('catalog.taxgroup.manage', 'Create and update tax groups.'),
  perm('catalog.price.manage', 'Maintain item prices and price history.'),
  perm('catalog.import.execute', 'Run catalog CSV imports.'),

  // accounting (PHASE_08..10)
  perm('accounting.account.view', 'Read the chart of accounts.'),
  perm('accounting.account.manage', 'Create and update accounts.'),
  perm('accounting.costcenter.manage', 'Maintain cost centers.'),
  perm('accounting.journal.create', 'Create draft journal entries.'),
  perm('accounting.journal.post', 'Post journal entries.'),
  perm('accounting.journal.reverse', 'Reverse posted journal entries.'),
  perm('accounting.period.view', 'Read fiscal years and periods.'),
  perm('accounting.period.close', 'Close fiscal periods.'),
  perm('accounting.period.reopen', 'Reopen closed fiscal periods.'),
  perm('accounting.opening.manage', 'Import and post opening balances.'),
  perm('accounting.reports.view', 'Read trial balance, general ledger and statements.'),

  // parties (PHASE_11)
  perm('parties.view', 'List and read customers and suppliers.'),
  perm('parties.manage', 'Create and update customers and suppliers.'),
  perm('parties.allocate', 'Allocate payments to invoices.'),
  perm('parties.creditlimit.override', 'Override the party credit limit.'),

  // inventory (PHASE_12)
  perm('inventory.view', 'Read stock levels and movements.'),
  perm('inventory.adjust', 'Create stock adjustments and transfers.'),
  perm('inventory.adjust.approve', 'Approve stock adjustments (posts ledger and journal).'),
  perm('inventory.transfer', 'Create stock transfers.'),
  perm('inventory.transfer.receive', 'Receive stock transfers.'),
  perm('inventory.request.manage', 'Raise and submit goods requests.'),
  perm('inventory.request.approve', 'Approve, reject or fulfil goods requests.'),
  perm('inventory.delivery.manage', 'Record stock deliveries against posted sales invoices.'),
  perm('inventory.production.manage', 'Create, edit and cancel production orders.'),
  perm('inventory.production.complete', 'Complete production orders: consume components and receive the finished item.'),
  perm('inventory.negative.override', 'Allow negative stock movements.'),

  // sales / purchases (PHASE_13)
  perm('sales.view', 'List and read sales documents.'),
  perm('sales.invoice.create', 'Create draft sales invoices.'),
  perm('sales.invoice.post', 'Post sales invoices.'),
  perm('sales.invoice.void', 'Void posted sales invoices.'),
  perm('sales.invoice.pay', 'Record payments on sales invoices.'),
  perm('sales.discount.override', 'Exceed the membership discount limits.'),
  perm('sales.return.create', 'Create sales returns and credit notes.'),
  perm('sales.adjustment.create', 'Issue credit and debit notes against posted sales invoices.'),
  perm('sales.offer.manage', 'Maintain sales offers and promotional discount rules.'),
  perm('sales.salesman.manage', 'Maintain salesman cards.'),
  perm('purchase.view', 'List and read purchase documents.'),
  perm('purchase.invoice.create', 'Create draft purchase invoices.'),
  perm('purchase.invoice.post', 'Post purchase invoices.'),
  perm('purchase.invoice.void', 'Void posted purchase invoices.'),
  perm('purchase.invoice.pay', 'Record supplier payment hooks on purchase invoices.'),
  perm('purchase.cost.manage', 'Create, update and allocate purchase landed costs.'),
  perm('purchase.adjustment.create', 'Issue credit and debit notes against posted purchase invoices.'),

  // treasury (PHASE_13)
  perm('treasury.view', 'List and read vouchers and shifts.'),
  perm('treasury.voucher.create', 'Create draft vouchers.'),
  perm('treasury.voucher.post', 'Post vouchers.'),
  perm('treasury.voucher.void', 'Void posted vouchers.'),
  perm('treasury.cheque.clear', 'Clear or bounce cheques.'),
  perm('treasury.transfer.manage', 'Create, send and receive cash transfers.'),
  perm('treasury.expensetype.manage', 'Maintain treasury expense types.'),
  perm('treasury.shift.close', 'Open and close cashier shifts.'),

  // e-invoicing (PHASE_13)
  perm('einvoice.view', 'Read e-invoice credentials and submissions.'),
  perm('einvoice.manage', 'Maintain e-invoicing configuration.'),
  perm('einvoice.submit', 'Sign and submit e-invoices.'),
  perm('einvoice.credentials.manage', 'Maintain e-invoicing credentials.'),

  // reporting (PHASE_14)
  perm('reporting.view', 'Read the reporting catalogue.'),
  perm('reporting.export.execute', 'Run asynchronous report exports.'),
  perm('reporting.layout.manage', 'Create and maintain saved report layouts (مصمم التقارير).'),

  // file-level operations (الإعدادات: النسخ الإحتياطي، الإستعادة، التدوير، الصيانة، إنشاء ملف)
  perm('settings.backup.manage', 'Take and download logical backups of the company file.'),
  perm('settings.restore.manage', 'Dry-run and apply additive restores from a backup.'),
  perm('settings.rotation.manage', 'Purge operational logs older than a cutoff (تدوير البيانات).'),
  perm('settings.maintenance.manage', 'Scan and repair invoice inconsistencies (صيانة الفواتير).'),
  perm('settings.companyfile.create', 'Create a sibling company file for the same owner (إنشاء ملف).'),

  // migration (PHASE_15)
  perm('migration.view', 'Read migration runs, issues and reconciliation.'),
  perm('migration.run.execute', 'Start dry-run and import migration runs.'),
  perm('migration.run.import', 'Execute production data imports.'),

  // legacy compat gateway (PHASE_16)
  perm('compat.manage', 'Register, rotate and revoke legacy desktop compatibility devices.'),
  perm('compat.sync', 'Use legacy desktop compatibility pull and push endpoints.'),

  // restaurant POS pack (PHASE_19)
  perm('pos.view', 'Read POS floor maps, tables and open order state.'),
  perm('pos.operate', 'Open tables, add or void order items, merge/split, send and close POS orders.'),
  perm('pos.priceoverride', 'Override POS item prices where tenant caps permit it.'),
  perm('pos.tables.manage', 'Maintain dining tables and table categories.'),
  perm('pos.config.manage', 'Maintain POS order methods, payment visibility and kitchen print routing.'),

  // HRM and payroll pack (PHASE_20)
  perm('hrm.view', 'Read HR directories, attendance summaries, payroll previews and payslips.'),
  perm('hrm.manage', 'Maintain departments, jobs, employees, attendance imports and payroll drafts.'),
  perm('hrm.payroll.post', 'Post, pay and reverse payroll runs.'),
  perm('hrm.adjust.approve', 'Approve salary additions and deductions.'),

  // installments and contracting/projects packs (PHASE_21)
  perm('installments.view', 'Read installment contracts, schedules, overdue aging and contract statements.'),
  perm('installments.manage', 'Create and maintain installment contracts and schedule templates.'),
  perm('installments.collect', 'Collect installment receipts and allocate them to due schedule rows.'),
  perm('projects.view', 'Read projects, stages, BOQ terms, progress bills and requirements.'),
  perm('projects.manage', 'Create and maintain projects, stage templates, BOQ terms and requirement registers.'),
  perm('projects.bill.post', 'Post progress bills and release retention invoices.'),
  perm('projects.contractor.pay', 'Approve and pay contractor payment certificates.'),
  perm('projects.stage.accredit', 'Accredit or reject project stages assigned to a user.'),

  // niche verticals and Salla integration pack (PHASE_22)
  perm('optics.view', 'Read optical prescriptions and invoice print sections.'),
  perm('optics.manage', 'Create and maintain optical prescriptions.'),
  perm('tailoring.view', 'Read customer measurement cards and latest measurements.'),
  perm('tailoring.manage', 'Create and maintain customer measurements.'),
  perm('marina.view', 'Read marina groups, vessels, bookings and operation plans.'),
  perm('marina.manage', 'Create and maintain marina vessels, owners, bookings, pricing, violations and plans.'),
  perm('marina.invoice', 'Create rental invoices from marina bookings.'),
  perm('fitment.view', 'Read vehicle compatibility lookups.'),
  perm('fitment.manage', 'Maintain vehicle makes, models and item fitment rows.'),
  perm('salla.integration.view', 'Read Salla synchronization status and export logs.'),
  perm('salla.integration.manage', 'Manage Salla OAuth connections, mappings, export queues and webhooks.'),
] as const;

/**
 * Platform-console permissions (2026-09). Granted only through
 * `platform_memberships` — never through tenant `role_permissions`, never through
 * the `*` wildcard. Kept out of `permissionRegistry` on purpose: every tenant
 * flow that enumerates that registry (role editor, owner expansion, `GET
 * /permissions`) stays blind to these codes by construction.
 */
export const platformPermissionRegistry: readonly PermissionDefinition[] = [
  perm('console.tenants.view', 'List and read tenants in the platform console.'),
  perm('console.tenants.manage', 'Create, suspend and reactivate tenants.'),
  perm('console.subscriptions.manage', 'Create, renew and cancel tenant subscriptions.'),
  perm('console.plans.manage', 'Create and retire billing plans.'),
  perm('console.activation.review', 'Approve or reject tenant activation requests.'),
  perm('console.users.view', 'List platform users.'),
  perm('console.users.manage', 'Grant and revoke platform roles.'),
  perm('console.audit.view', 'Read the cross-tenant audit trail.'),
  perm('console.health.view', 'Read system health and readiness.'),
  perm('console.jobs.view', 'Read background-queue and outbox health.'),
  perm('console.billing.manage', 'Manage billing operations and dunning.'),
  perm('console.support.manage', 'Handle platform support tickets and break-glass access.'),
] as const;

const registryByCode = new Map(permissionRegistry.map((entry) => [entry.code, entry]));
const platformRegistryByCode = new Map(platformPermissionRegistry.map((entry) => [entry.code, entry]));

/** Wildcard granted to the baseline `owner` role (see packages/config/src/seeds/roles.ts). */
export const ALL_PERMISSIONS = '*';

/**
 * Tenant-grantable codes minus deprecated spellings. Seed scripts and the owner
 * `*` expansion use this list so new databases converge on canonical codes while
 * old rows keep working through the alias map.
 */
export const canonicalPermissionCodes: readonly string[] = permissionRegistry
  .filter((entry) => !entry.deprecated)
  .map((entry) => entry.code);

/** Every code the `permissions` table is seeded from: tenant + platform registries. */
export const seedablePermissionCodes: readonly string[] = [
  ...permissionRegistry.map((entry) => entry.code),
  ...platformPermissionRegistry.map((entry) => entry.code),
];

/** Normalises a legacy spelling to its canonical code; unknown codes pass through. */
export function canonicalizePermissionCode(code: string): string {
  return permissionAliases[code] ?? code;
}

/** True when the code belongs to the platform console (`console.*` namespace). */
export function isConsolePermissionCode(code: string): boolean {
  return code === 'console' || code.startsWith('console.') || platformRegistryByCode.has(code);
}

/**
 * True when the code may be stored in a tenant `role_permissions` row: any tenant
 * registry code (canonical or deprecated). Console codes and `*` are rejected —
 * `*` is a seed-time macro, not a storable code.
 */
export function isTenantGrantablePermissionCode(code: string): boolean {
  if (code === ALL_PERMISSIONS) return false;
  return registryByCode.has(code) && !isConsolePermissionCode(code);
}

export function isKnownPermissionCode(code: string): boolean {
  return registryByCode.has(code) || platformRegistryByCode.has(code);
}

export function findPermission(code: string): PermissionDefinition | undefined {
  return registryByCode.get(code) ?? platformRegistryByCode.get(code);
}

/**
 * Alias-aware authorisation check shared by `PermissionsGuard` and unit tests.
 *
 * A granted code satisfies the required code when it is identical, when it is the
 * legacy spelling of the required canonical code, or when it is the canonical
 * spelling of a required legacy code (controllers migrate at their own pace).
 * The `*` wildcard satisfies every *tenant* code and never a `console.*` code.
 */
export function permissionGrants(granted: readonly string[], required: string): boolean {
  if (granted.includes(required)) return true;
  if (isConsolePermissionCode(required)) return false;
  const grantedSet = new Set(granted);
  if (grantedSet.has(ALL_PERMISSIONS)) return true;
  const requiredCanonical = canonicalizePermissionCode(required);
  if (grantedSet.has(requiredCanonical)) return true;
  const legacySpelling = canonicalToLegacy[requiredCanonical];
  if (legacySpelling && grantedSet.has(legacySpelling)) return true;
  for (const code of grantedSet) {
    if (canonicalizePermissionCode(code) === requiredCanonical) return true;
  }
  return false;
}

export function permissionsForModule(moduleName: string): PermissionDefinition[] {
  return permissionRegistry.filter((entry) => entry.module === moduleName);
}

export const permissionModules: readonly string[] = [
  ...new Set(permissionRegistry.map((entry) => entry.module)),
];
