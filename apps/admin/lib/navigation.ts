export type AdminSection = {
  key: string;
  href: string;
  labelAr: string;
  labelEn: string;
  permission: string;
  description: string;
};

export const sections: AdminSection[] = [
  { key: 'dashboard', href: '/', labelAr: 'الرئيسية', labelEn: 'Dashboard', permission: 'reporting.view', description: 'KPIs and module widgets.' },
  { key: 'platform', href: '/platform', labelAr: 'المنصة والإعدادات', labelEn: 'Platform', permission: 'platform.tenant.view', description: 'Tenant, roles, audit, files, jobs.' },
  { key: 'organization', href: '/organization', labelAr: 'المؤسسة', labelEn: 'Organization', permission: 'organization.branch.view', description: 'Branches, warehouses, cash locations, currencies, profiles.' },
  { key: 'catalog', href: '/catalog', labelAr: 'الكتالوج', labelEn: 'Catalog', permission: 'catalog.item.view', description: 'Items, units, categories, tax, prices.' },
  { key: 'accounting', href: '/accounting', labelAr: 'المحاسبة', labelEn: 'Accounting', permission: 'accounting.account.view', description: 'COA, periods, journals, statements.' },
  { key: 'parties', href: '/parties', labelAr: 'العملاء والموردون', labelEn: 'Parties', permission: 'parties.view', description: 'Contacts, balances, allocations.' },
  { key: 'inventory', href: '/inventory', labelAr: 'المخزون', labelEn: 'Inventory', permission: 'inventory.view', description: 'Levels, movements, transfers, lots, serials.' },
  { key: 'sales', href: '/sales', labelAr: 'المبيعات', labelEn: 'Sales', permission: 'sales.view', description: 'Invoices, returns, payments, print.' },
  { key: 'purchases', href: '/purchases', labelAr: 'المشتريات', labelEn: 'Purchases', permission: 'purchase.view', description: 'Supplier invoices, landed cost, payments.' },
  { key: 'treasury', href: '/treasury', labelAr: 'الخزينة', labelEn: 'Treasury', permission: 'treasury.view', description: 'Vouchers, cheques, transfers, shifts.' },
  { key: 'einvoicing', href: '/einvoicing', labelAr: 'الفوترة الإلكترونية', labelEn: 'E-invoicing', permission: 'einvoice.view', description: 'Credentials, submissions, health.' },
  { key: 'reporting', href: '/reporting', labelAr: 'التقارير', labelEn: 'Reporting', permission: 'reporting.view', description: 'Report runner and exports.' },
  { key: 'migration', href: '/migration', labelAr: 'الهجرة والتوافق', labelEn: 'Migration', permission: 'migration.view', description: 'Runs, issues, reconciliation, compat devices.' },
  { key: 'pos', href: '/pos', labelAr: 'مطعم POS', labelEn: 'Restaurant POS', permission: 'pos.view', description: 'Floor map, table orders, kitchen routing and daily counters.' },
  { key: 'hrm', href: '/hrm', labelAr: 'الموارد البشرية', labelEn: 'HRM & Payroll', permission: 'hrm.view', description: 'Employees, attendance, adjustments, payroll runs and payslips.' },
  { key: 'installments', href: '/installments', labelAr: 'الأقساط', labelEn: 'Installments', permission: 'installments.view', description: 'Contracts, schedules, collections and aging.' },
  { key: 'projects', href: '/projects', labelAr: 'المقاولات', labelEn: 'Projects & Contracting', permission: 'projects.view', description: 'Projects, stages, BOQ, progress bills and retention.' },
];
