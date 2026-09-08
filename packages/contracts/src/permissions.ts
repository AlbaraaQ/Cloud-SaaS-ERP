/**
 * Permission registry — SECURITY_ARCHITECTURE §3 ("Static permission registry
 * (`permissions` table seeded from code list)") and PROJECT_CONTRACT §1 naming
 * (`module.entity.action`).
 *
 * This code list is the single source that the `permissions` table is seeded from
 * (PHASE_03 §5.7). The matrix in SECURITY_ARCHITECTURE §5 is a summary of it.
 * Extend only forward — never rename or remove a code without an ADR.
 */

export type PermissionDefinition = {
  readonly code: string;
  readonly module: string;
  readonly description: string;
};

function perm(code: string, description: string): PermissionDefinition {
  return { code, module: code.split('.')[0] as string, description };
}

export const permissionRegistry: readonly PermissionDefinition[] = [
  // platform (PHASE_03)
  perm('platform.tenant.view', 'Read the own tenant record and its effective settings.'),
  perm('platform.tenant.manage', 'Update the own tenant record and typed settings in bulk.'),
  perm('platform.membership.manage', 'Invite, update and remove tenant memberships.'),
  perm('platform.role.manage', 'Create and maintain roles and their permission sets.'),
  perm('platform.settings.manage', 'Read and write individual typed tenant settings.'),
  perm('platform.audit.view', 'Read the tenant audit log.'),
  perm('platform.file.upload', 'Request pre-signed uploads, attach and download files.'),

  // platform services (PHASE_04)
  perm('platform.notification.view', 'Read own in-app notifications and mark them read.'),
  perm('platform.notification.manage', 'Create notifications for other memberships of the tenant.'),
  perm('platform.job.view', 'Read the transactional outbox and background-queue health.'),

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

const registryByCode = new Map(permissionRegistry.map((entry) => [entry.code, entry]));

/** Wildcard granted to the baseline `owner` role (see packages/config/src/seeds/roles.ts). */
export const ALL_PERMISSIONS = '*';

export function isKnownPermissionCode(code: string): boolean {
  return registryByCode.has(code);
}

export function findPermission(code: string): PermissionDefinition | undefined {
  return registryByCode.get(code);
}

export function permissionsForModule(moduleName: string): PermissionDefinition[] {
  return permissionRegistry.filter((entry) => entry.module === moduleName);
}

export const permissionModules: readonly string[] = [
  ...new Set(permissionRegistry.map((entry) => entry.module)),
];
