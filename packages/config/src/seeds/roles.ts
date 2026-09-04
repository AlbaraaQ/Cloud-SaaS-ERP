/**
 * Baseline role seed (DATABASE_DESIGN §17: "3 roles (owner/accountant/cashier)").
 * Seed lists live in `packages/config/seeds/*.ts` per DATABASE_DESIGN §17.
 *
 * Permission codes must exist in the registry exported by `@erp/contracts`
 * (`permissionRegistry`); `apps/api/src/modules/platform/seeds/permission-registry.ts`
 * asserts this at seed time.
 */

export type BaselineRoleSeed = {
  readonly code: string;
  readonly name: string;
  readonly description: string;
  readonly isSystem: boolean;
  readonly permissions: readonly string[];
};

export const baselineRoles: readonly BaselineRoleSeed[] = [
  {
    code: 'owner',
    name: 'Owner',
    description: 'Full control of the tenant, including configuration and access management.',
    isSystem: true,
    permissions: ['*'],
  },
  {
    code: 'accountant',
    name: 'Accountant',
    description: 'Accounting, parties, inventory and document review without tenant administration.',
    isSystem: true,
    permissions: [
      'platform.tenant.view',
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
      'accounting.period.close',
      'accounting.period.reopen',
      'accounting.opening.manage',
      'accounting.reports.view',
      'parties.view',
      'parties.manage',
      'parties.allocate',
      'inventory.view',
      'inventory.adjust',
      'inventory.transfer',
      'sales.view',
      'sales.invoice.create',
      'sales.invoice.post',
      'sales.invoice.void',
      'purchase.view',
      'purchase.invoice.create',
      'purchase.invoice.post',
      'purchase.invoice.void',
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
    code: 'cashier',
    name: 'Cashier',
    description: 'Point-of-sale and treasury operations; read-only catalog and party lookup.',
    isSystem: true,
    permissions: [
      'platform.tenant.view',
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
    ],
  },
] as const;
