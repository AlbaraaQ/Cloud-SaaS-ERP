/**
 * Navigation tree — a 1:1 mirror of the desktop product's menu (المحاسبة، المستودعات،
 * المشتريات، المبيعات، الموظفين والرواتب، المراسي، المشاريع، الإعدادات، الدعم الفني)
 * plus the SaaS platform console that the desktop edition never had.
 *
 * `status` is deliberately part of the data model and is rendered in the UI:
 *   'ready'   — the screen is implemented and talks to a real endpoint.
 *   'api'     — the API endpoint exists, the screen is still a scaffold.
 *   'planned' — neither side exists yet.
 * Nothing here pretends to work; a scaffold screen says so and names its endpoint.
 */

export type ScreenStatus = 'ready' | 'api' | 'planned';

export type ScreenItem = {
  key: string;
  labelAr: string;
  labelEn: string;
  href: string;
  /** Permission code from @erp/contracts; when absent the item is always visible. */
  permission?: string;
  status: ScreenStatus;
  /** API path the screen uses (or will use) — shown on scaffold screens. */
  endpoint?: string;
  description?: string;
};

export type ScreenGroup = {
  key: string;
  labelAr: string;
  labelEn: string;
  items: ScreenItem[];
};

export type ModuleNode = {
  key: string;
  icon: string;
  labelAr: string;
  labelEn: string;
  href: string;
  permission?: string;
  /** Only rendered for users whose token carries `pam` (platform administrators). */
  platformAdminOnly?: boolean;
  groups: ScreenGroup[];
};

const screen = (
  key: string,
  labelAr: string,
  labelEn: string,
  href: string,
  status: ScreenStatus,
  extra: Partial<ScreenItem> = {},
): ScreenItem => ({ key, labelAr, labelEn, href, status, ...extra });

// ---------------------------------------------------------------------------
// 0. Platform console (SaaS control plane) — customers, licences, subscriptions.
// ---------------------------------------------------------------------------
const platformConsole: ModuleNode = {
  key: 'console',
  icon: '🛡️',
  labelAr: 'لوحة تحكم المنصة',
  labelEn: 'Platform console',
  href: '/platform',
  platformAdminOnly: true,
  groups: [
    {
      key: 'console-overview',
      labelAr: 'نظرة عامة',
      labelEn: 'Overview',
      items: [
        screen('console-home', 'مؤشرات المنصة', 'Platform KPIs', '/platform', 'ready', { endpoint: '/platform/overview' }),
      ],
    },
    {
      key: 'console-customers',
      labelAr: 'العملاء والتراخيص',
      labelEn: 'Customers & licences',
      items: [
        screen('console-tenants', 'العملاء (المستأجرون)', 'Tenants', '/platform/tenants', 'ready', { endpoint: '/platform/tenants' }),
        screen('console-tenant-new', 'إنشاء عميل جديد', 'Create tenant', '/platform/tenants/new', 'ready', { endpoint: 'POST /platform/tenants' }),
        screen('console-subscriptions', 'الاشتراكات والتراخيص', 'Subscriptions', '/platform/subscriptions', 'ready', { endpoint: '/platform/subscriptions' }),
        screen('console-plans', 'الباقات والأسعار', 'Plans', '/platform/plans', 'ready', { endpoint: '/platform/plans' }),
        screen('console-activation', 'طلبات التفعيل', 'Activation requests', '/platform/activation-requests', 'ready', { endpoint: '/billing/activation-requests' }),
      ],
    },
    {
      key: 'console-ops',
      labelAr: 'التشغيل',
      labelEn: 'Operations',
      items: [
        screen('console-users', 'مستخدمو المنصة', 'Platform users', '/platform/users', 'ready', { endpoint: '/platform/users' }),
        screen('console-audit', 'سجل التدقيق', 'Audit log', '/platform/audit', 'ready', { endpoint: '/audit-log' }),
        screen('console-jobs', 'المهام والطوابير', 'Jobs & queues', '/platform/jobs', 'ready', { endpoint: '/jobs/outbox' }),
        screen('console-health', 'صحة النظام', 'System health', '/platform/health', 'ready', { endpoint: '/health/ready' }),
      ],
    },
  ],
};

// ---------------------------------------------------------------------------
// 1. المحاسبة
// ---------------------------------------------------------------------------
const accounting: ModuleNode = {
  key: 'accounting',
  icon: '📊',
  labelAr: 'المحاسبة',
  labelEn: 'Accounting',
  href: '/accounting/accounts',
  permission: 'accounting.account.view',
  groups: [
    {
      key: 'accounting-defs',
      labelAr: 'تعاريف',
      labelEn: 'Definitions',
      items: [
        screen('coa', 'دليل الحسابات', 'Chart of accounts', '/accounting/accounts', 'ready', { permission: 'accounting.account.view', endpoint: '/accounts' }),
        screen('coa-tree', 'شجرة الحسابات', 'Account tree', '/accounting/accounts/tree', 'ready', { permission: 'accounting.account.view', endpoint: '/accounts' }),
        screen('cash-card', 'بطاقة صندوق', 'Cash box card', '/accounting/cash-locations?type=cash', 'ready', { permission: 'organization.cashlocation.view', endpoint: '/cash-locations' }),
        screen('bank-card', 'بطاقة بنك', 'Bank card', '/accounting/cash-locations?type=bank', 'ready', { permission: 'organization.cashlocation.view', endpoint: '/cash-locations' }),
        screen('cost-center', 'بطاقة مركز تكلفة', 'Cost centre card', '/accounting/cost-centers', 'ready', { permission: 'accounting.account.view', endpoint: '/cost-centers' }),
        screen('customer-payment-method', 'طريقة دفع عميل', 'Customer payment method', '/s/accounting/payment-methods', 'planned'),
        screen('expense-card', 'بطاقة المصاريف', 'Expense card', '/s/accounting/expenses', 'planned'),
      ],
    },
    {
      key: 'accounting-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen('opening-entry', 'قيد إفتتاحي', 'Opening entry', '/accounting/journal-entries/new?kind=opening', 'ready', { permission: 'accounting.journal.post', endpoint: 'POST /journal-entries' }),
        screen('journal-voucher', 'سند قيد', 'Journal voucher', '/accounting/journal-entries/new', 'ready', { permission: 'accounting.journal.post', endpoint: 'POST /journal-entries' }),
        screen('receipt-voucher', 'سند قبض', 'Receipt voucher', '/s/treasury/receipt-voucher', 'api', { permission: 'treasury.view', endpoint: '/treasury/receipts' }),
        screen('payment-voucher', 'سند صرف', 'Payment voucher', '/s/treasury/payment-voucher', 'api', { permission: 'treasury.view', endpoint: '/treasury/payments' }),
        screen('tax-payment-voucher', 'سند صرف الضريبة', 'Tax payment voucher', '/s/treasury/tax-payment', 'planned'),
        screen('periods', 'الفترات المحاسبية', 'Fiscal periods', '/accounting/periods', 'ready', { permission: 'accounting.period.view', endpoint: '/fiscal-periods' }),
      ],
    },
    {
      key: 'accounting-reports',
      labelAr: 'تقارير محاسبية',
      labelEn: 'Accounting reports',
      items: [
        screen('journals-report', 'القيود اليومية', 'Journal entries', '/accounting/journal-entries', 'ready', { permission: 'accounting.reports.view', endpoint: '/journal-entries' }),
        screen('vouchers-report', 'عرض السندات', 'Vouchers', '/s/accounting/vouchers', 'api', { endpoint: '/treasury/vouchers' }),
        screen('statement', 'كشف حساب', 'Account statement', '/accounting/ledger', 'ready', { permission: 'accounting.reports.view', endpoint: '/statements/general-ledger/{accountId}' }),
        screen('main-statement', 'كشف حساب رئيسي', 'Main account statement', '/accounting/ledger?rollup=1', 'ready', { permission: 'accounting.reports.view', endpoint: '/statements/general-ledger/{accountId}' }),
        screen('cash-movement', 'حركة الصندوق', 'Cash movement', '/s/accounting/cash-movement', 'api', { endpoint: '/reports/cash-movement' }),
        screen('account-balances', 'أرصدة الحسابات', 'Account balances', '/accounting/trial-balance', 'ready', { permission: 'accounting.reports.view', endpoint: '/statements/trial-balance' }),
        screen('cc-balances', 'أرصدة مراكز التكلفة', 'Cost-centre balances', '/s/accounting/cost-center-balances', 'planned'),
        screen('cc-report', 'تقرير مركز الكلفة', 'Cost-centre report', '/s/accounting/cost-center-report', 'planned'),
        screen('daily-movement', 'الحركة اليومية', 'Daily movement', '/s/accounting/daily-movement', 'planned'),
        screen('trial-balance', 'ميزان المراجعة رئيسي', 'Trial balance', '/accounting/trial-balance', 'ready', { permission: 'accounting.reports.view', endpoint: '/statements/trial-balance' }),
        screen('income-statement', 'قائمة الدخل التحليلية', 'Analytical income statement', '/s/accounting/income-statement', 'planned'),
        screen('balance-sheet', 'ميزانية تحليلية', 'Analytical balance sheet', '/s/accounting/balance-sheet', 'planned'),
        screen('vat-return', 'الإقرار الضريبي', 'VAT return', '/s/accounting/vat-return', 'planned'),
      ],
    },
  ],
};

// ---------------------------------------------------------------------------
// 2. المستودعات
// ---------------------------------------------------------------------------
const inventory: ModuleNode = {
  key: 'inventory',
  icon: '🏭',
  labelAr: 'المستودعات',
  labelEn: 'Inventory',
  href: '/s/inventory/items',
  permission: 'inventory.view',
  groups: [
    {
      key: 'inventory-defs',
      labelAr: 'التعاريف',
      labelEn: 'Definitions',
      items: [
        screen('items', 'دليل المواد', 'Item directory', '/s/inventory/items', 'api', { permission: 'catalog.item.view', endpoint: '/organization/catalog/items' }),
        screen('warehouse-card', 'بطاقة مستودع', 'Warehouse card', '/s/inventory/warehouses', 'api', { permission: 'organization.warehouse.view', endpoint: '/warehouses' }),
        screen('group-card', 'بطاقة مجموعة', 'Category card', '/s/inventory/categories', 'api', { permission: 'catalog.item.view', endpoint: '/organization/catalog/categories' }),
        screen('unit-card', 'بطاقة وحدة', 'Unit card', '/s/inventory/units', 'api', { permission: 'catalog.item.view', endpoint: '/organization/catalog/units' }),
        screen('item-card', 'بطاقة مادة', 'Item card', '/s/inventory/items/new', 'api', { permission: 'catalog.item.manage', endpoint: 'POST /organization/catalog/items' }),
      ],
    },
    {
      key: 'inventory-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen('transfer', 'مناقلة', 'Transfer', '/s/inventory/transfers', 'api', { endpoint: '/inventory/transfers' }),
        screen('opening-stock', 'بضاعة أول مدة', 'Opening stock', '/s/inventory/opening', 'api', { endpoint: '/inventory/opening-balances' }),
        screen('goods-in', 'فاتورة إدخال', 'Goods receipt', '/s/inventory/receipts', 'api', { endpoint: '/inventory/receipts' }),
        screen('goods-out', 'فاتورة إخراج', 'Goods issue', '/s/inventory/issues', 'api', { endpoint: '/inventory/issues' }),
        screen('stock-adjust', 'تسوية مخزنية', 'Stock adjustment', '/s/inventory/adjustments', 'api', { endpoint: '/inventory/adjustments' }),
        screen('stock-delivery', 'توصيل مخزني', 'Stock delivery', '/s/inventory/deliveries', 'planned'),
        screen('goods-request', 'طلب بضاعة', 'Goods request', '/s/inventory/requests', 'planned'),
        screen('barcode', 'طباعة الباركود', 'Barcode printing', '/s/inventory/barcodes', 'planned'),
      ],
    },
    {
      key: 'inventory-reports',
      labelAr: 'تقارير مستودعية',
      labelEn: 'Inventory reports',
      items: [
        screen('stock-count', 'جرد المواد', 'Stock count', '/s/inventory/reports/levels', 'api', { endpoint: '/inventory/levels' }),
        screen('item-movement', 'حركة مادة تفصيلي', 'Item movement (detail)', '/s/inventory/reports/movement-detail', 'api', { endpoint: '/inventory/movements' }),
        screen('items-movement', 'حركة مواد تجميعي', 'Item movement (summary)', '/s/inventory/reports/movement-summary', 'api', { endpoint: '/reports/inventory-movement' }),
        screen('expiry', 'صلاحية المواد', 'Item expiry', '/s/inventory/reports/expiry', 'api', { endpoint: '/inventory/lots' }),
        screen('sales-analysis', 'تحليل المبيعات', 'Sales analysis', '/s/inventory/reports/sales-analysis', 'planned'),
        screen('purchase-sales-total', 'إجمالي المبيعات والمشتريات', 'Sales & purchases total', '/s/inventory/reports/totals', 'planned'),
        screen('invoice-profit', 'أرباح الفواتير', 'Invoice profit', '/s/inventory/reports/invoice-profit', 'planned'),
        screen('turnover', 'معدل الدوران والركود', 'Turnover & dead stock', '/s/inventory/reports/turnover', 'planned'),
        screen('production-order', 'تقرير أمر الإنتاج', 'Production order report', '/s/inventory/reports/production', 'planned'),
        screen('invoices-by-type', 'الفواتير بحسب النوع', 'Invoices by type', '/s/inventory/reports/invoices-by-type', 'planned'),
        screen('expired-items', 'انتهاء صلاحية الأصناف', 'Expired items', '/s/inventory/reports/expired', 'planned'),
        screen('serials', 'تقرير الأرقام التسلسلية', 'Serial numbers report', '/s/inventory/reports/serials', 'api', { endpoint: '/inventory/serials' }),
      ],
    },
    {
      key: 'inventory-salla',
      labelAr: 'متجر سلة',
      labelEn: 'Salla store',
      items: [
        screen('salla-products', 'المنتجات', 'Products', '/s/integrations/salla/products', 'api', { permission: 'salla.integration.view', endpoint: '/integrations/salla/products' }),
        screen('salla-orders', 'إدارة الطلبات', 'Orders', '/s/integrations/salla/orders', 'api', { permission: 'salla.integration.view', endpoint: '/integrations/salla/orders' }),
        screen('salla-warehouses', 'ربط المستودعات', 'Warehouse mapping', '/s/integrations/salla/warehouses', 'api', { permission: 'salla.integration.manage', endpoint: '/integrations/salla/mappings' }),
      ],
    },
  ],
};

// ---------------------------------------------------------------------------
// 3. المشتريات
// ---------------------------------------------------------------------------
const purchases: ModuleNode = {
  key: 'purchases',
  icon: '🛒',
  labelAr: 'المشتريات',
  labelEn: 'Purchases',
  href: '/s/purchases/invoices',
  permission: 'purchase.view',
  groups: [
    {
      key: 'purchases-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen('purchase-invoice', 'فاتورة المشتريات', 'Purchase invoice', '/s/purchases/invoices', 'api', { endpoint: '/purchase-invoices' }),
        screen('purchase-return', 'مردود المشتريات', 'Purchase return', '/s/purchases/returns', 'api', { endpoint: '/purchase-returns' }),
      ],
    },
    {
      key: 'purchases-vouchers',
      labelAr: 'السندات',
      labelEn: 'Vouchers',
      items: [
        screen('supplier-payment', 'سند صرف لمورد', 'Supplier payment', '/s/purchases/payments', 'api', { endpoint: '/treasury/payments' }),
        screen('purchase-vouchers', 'عرض السندات', 'Vouchers', '/s/purchases/vouchers', 'api', { endpoint: '/treasury/vouchers' }),
        screen('purchase-credit-note', 'إشعار دائن', 'Credit note', '/s/purchases/credit-notes', 'planned'),
      ],
    },
    {
      key: 'purchases-notes',
      labelAr: 'الإشعارات',
      labelEn: 'Notes',
      items: [
        screen('pn-credit', 'إشعار دائن', 'Credit note', '/s/purchases/notes/credit', 'planned'),
        screen('pn-debit', 'إشعار مدين', 'Debit note', '/s/purchases/notes/debit', 'planned'),
        screen('pn-report', 'تقرير الإشعارات', 'Notes report', '/s/purchases/notes/report', 'planned'),
      ],
    },
    {
      key: 'purchases-reports',
      labelAr: 'التقارير',
      labelEn: 'Reports',
      items: [
        screen('supplier-statement', 'كشف مورد', 'Supplier statement', '/s/purchases/reports/supplier-statement', 'api', { endpoint: '/parties/{id}/statement' }),
        screen('purchase-invoices-report', 'تقرير فواتير المشتريات', 'Purchase invoices', '/s/purchases/reports/invoices', 'api', { endpoint: '/reports/purchases' }),
        screen('purchase-returns-report', 'تقرير مردود فواتير المشتريات', 'Purchase returns', '/s/purchases/reports/returns', 'planned'),
        screen('net-purchases', 'صافي المشتريات', 'Net purchases', '/s/purchases/reports/net', 'planned'),
        screen('purchases-detail', 'مشتريات تفصيلية', 'Detailed purchases', '/s/purchases/reports/detail', 'planned'),
        screen('purchases-items', 'مشتريات الأصناف تجميعي', 'Purchases by item', '/s/purchases/reports/items', 'planned'),
        screen('supplier-balances', 'أرصدة الموردين', 'Supplier balances', '/s/purchases/reports/balances', 'api', { endpoint: '/reports/party-balances' }),
        screen('supplier-settlements', 'سداد الموردين', 'Supplier settlements', '/s/purchases/reports/settlements', 'planned'),
        screen('employee-purchases', 'مشتريات موظف', 'Purchases by employee', '/s/purchases/reports/by-employee', 'planned'),
        screen('invoices-by-supplier', 'الفواتير بحسب الموردين', 'Invoices by supplier', '/s/purchases/reports/by-supplier', 'planned'),
      ],
    },
    {
      key: 'purchases-other',
      labelAr: 'أخرى',
      labelEn: 'Other',
      items: [screen('supplier-card', 'بطاقة مورد', 'Supplier card', '/s/parties/suppliers', 'api', { permission: 'parties.view', endpoint: '/parties?type=supplier' })],
    },
  ],
};

// ---------------------------------------------------------------------------
// 4. المبيعات
// ---------------------------------------------------------------------------
const sales: ModuleNode = {
  key: 'sales',
  icon: '🧾',
  labelAr: 'المبيعات',
  labelEn: 'Sales',
  href: '/s/sales/invoices',
  permission: 'sales.view',
  groups: [
    {
      key: 'sales-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen('pos', 'نقطة البيع', 'Point of sale', '/s/pos/terminal', 'api', { permission: 'pos.view', endpoint: '/pos/orders' }),
        screen('sales-invoice', 'فاتورة مبيعات', 'Sales invoice', '/s/sales/invoices', 'api', { endpoint: '/sales-invoices' }),
        screen('sales-return', 'مردود المبيعات', 'Sales return', '/s/sales/returns', 'api', { endpoint: '/sales-returns' }),
        screen('quotation', 'عرض سعر', 'Quotation', '/s/sales/quotations', 'planned'),
        screen('day-close', 'إغلاق اليومية', 'Day close', '/s/sales/day-close', 'api', { endpoint: '/pos/shifts/close' }),
        screen('contracting-invoice', 'فاتورة المقاولات', 'Contracting invoice', '/s/projects/progress-bills', 'api', { permission: 'projects.view', endpoint: '/projects/progress-bills' }),
        screen('contracting-return', 'مرتجع مقاولات', 'Contracting return', '/s/projects/returns', 'planned'),
      ],
    },
    {
      key: 'sales-vouchers',
      labelAr: 'السندات',
      labelEn: 'Vouchers',
      items: [
        screen('customer-receipt', 'سند قبض عميل', 'Customer receipt', '/s/sales/receipts', 'api', { endpoint: '/treasury/receipts' }),
        screen('sales-vouchers', 'عرض السندات', 'Vouchers', '/s/sales/vouchers', 'api', { endpoint: '/treasury/vouchers' }),
        screen('sales-debit-note', 'إشعار مدين', 'Debit note', '/s/sales/debit-notes', 'planned'),
      ],
    },
    {
      key: 'sales-notes',
      labelAr: 'الإشعارات',
      labelEn: 'Notes',
      items: [
        screen('sn-credit', 'إشعار دائن', 'Credit note', '/s/sales/notes/credit', 'planned'),
        screen('sn-debit', 'إشعار مدين', 'Debit note', '/s/sales/notes/debit', 'planned'),
        screen('sn-report', 'تقرير الإشعارات', 'Notes report', '/s/sales/notes/report', 'planned'),
      ],
    },
    {
      key: 'sales-reports',
      labelAr: 'التقارير',
      labelEn: 'Reports',
      items: [
        screen('day-closes', 'إغلاقات اليومية', 'Day closes', '/s/sales/reports/day-closes', 'api', { endpoint: '/pos/shifts' }),
        screen('sales-invoices-report', 'تقرير فواتير المبيعات', 'Sales invoices', '/s/sales/reports/invoices', 'api', { endpoint: '/reports/sales' }),
        screen('sales-returns-report', 'تقرير مردود فواتير المبيعات', 'Sales returns', '/s/sales/reports/returns', 'planned'),
        screen('net-sales', 'صافي المبيعات', 'Net sales', '/s/sales/reports/net', 'planned'),
        screen('sales-detail', 'مبيعات تفصيلية', 'Detailed sales', '/s/sales/reports/detail', 'planned'),
        screen('sales-items', 'مبيعات الأصناف تجميعي', 'Sales by item', '/s/sales/reports/items', 'planned'),
        screen('sales-by-category', 'مبيعات بحسب الفئة', 'Sales by category', '/s/sales/reports/by-category', 'planned'),
        screen('sales-invoice-profit', 'أرباح الفواتير', 'Invoice profit', '/s/sales/reports/invoice-profit', 'planned'),
        screen('item-profit', 'أرباح الأصناف', 'Item profit', '/s/sales/reports/item-profit', 'planned'),
        screen('item-profit-detail', 'تفاصيل أرباح الأصناف', 'Item profit detail', '/s/sales/reports/item-profit-detail', 'planned'),
        screen('reps-report', 'تقرير المندوبين', 'Sales reps', '/s/sales/reports/reps', 'planned'),
        screen('employee-sales', 'مبيعات موظف', 'Sales by employee', '/s/sales/reports/by-employee', 'planned'),
        screen('customer-statement', 'كشف عميل', 'Customer statement', '/s/sales/reports/customer-statement', 'api', { endpoint: '/parties/{id}/statement' }),
        screen('customer-balances', 'أرصدة العملاء', 'Customer balances', '/s/sales/reports/balances', 'api', { endpoint: '/reports/party-balances' }),
        screen('customer-settlements', 'سداد العملاء', 'Customer settlements', '/s/sales/reports/settlements', 'planned'),
        screen('invoices-by-customer', 'الفواتير بحسب العملاء', 'Invoices by customer', '/s/sales/reports/by-customer', 'planned'),
        screen('sales-movement', 'حركة المبيعات', 'Sales movement', '/s/sales/reports/movement', 'planned'),
        screen('sales-chart', 'تقرير بياني', 'Chart report', '/s/sales/reports/chart', 'planned'),
        screen('contracting-invoices-report', 'تقرير فواتير المقاولات', 'Contracting invoices', '/s/projects/reports/invoices', 'planned'),
      ],
    },
    {
      key: 'sales-pos-reports',
      labelAr: 'تقارير نقطة البيع',
      labelEn: 'POS reports',
      items: [
        screen('pos-sales', 'تقرير مبيعات POS', 'POS sales', '/s/pos/reports/sales', 'api', { permission: 'pos.view', endpoint: '/pos/reports/sales' }),
        screen('pos-item-detail', 'تفاصيل أصناف POS', 'POS item detail', '/s/pos/reports/item-detail', 'planned'),
        screen('pos-item-summary', 'أصناف POS تجميعي', 'POS item summary', '/s/pos/reports/item-summary', 'planned'),
        screen('pos-daily', 'تقرير المبيعات اليومية', 'Daily sales', '/s/pos/reports/daily', 'planned'),
        screen('pos-group', 'تقرير مبيعات للمجموعة', 'Sales by group', '/s/pos/reports/group', 'planned'),
      ],
    },
    {
      key: 'sales-other',
      labelAr: 'أخرى',
      labelEn: 'Other',
      items: [
        screen('customer-card', 'بطاقة عميل', 'Customer card', '/s/parties/customers', 'api', { permission: 'parties.view', endpoint: '/parties?type=customer' }),
        screen('rep-card', 'بطاقة مندوب', 'Sales rep card', '/s/parties/reps', 'planned'),
      ],
    },
  ],
};

// ---------------------------------------------------------------------------
// 5. الموظفين والرواتب
// ---------------------------------------------------------------------------
const hrm: ModuleNode = {
  key: 'hrm',
  icon: '👥',
  labelAr: 'الموظفين والرواتب',
  labelEn: 'HR & Payroll',
  href: '/s/hrm/employees',
  permission: 'hrm.view',
  groups: [
    {
      key: 'hrm-defs',
      labelAr: 'التعاريف',
      labelEn: 'Definitions',
      items: [
        screen('departments', 'تعريف الإدارات', 'Departments', '/s/hrm/departments', 'api', { endpoint: '/hrm/departments' }),
        screen('sections', 'تعريف الأقسام', 'Sections', '/s/hrm/sections', 'api', { endpoint: '/hrm/sections' }),
        screen('employee', 'تعريف موظف', 'Employee', '/s/hrm/employees', 'api', { endpoint: '/hrm/employees' }),
      ],
    },
    {
      key: 'hrm-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen('adjustments', 'الحوافز والجزاءات', 'Bonuses & deductions', '/s/hrm/adjustments', 'api', { endpoint: '/hrm/adjustments' }),
        screen('payroll-run', 'إستحقاق راتب', 'Payroll run', '/s/hrm/payroll-runs', 'api', { endpoint: '/hrm/payroll-runs' }),
        screen('salary-payment', 'سند صرف راتب', 'Salary payment voucher', '/s/hrm/salary-payments', 'api', { endpoint: '/hrm/payroll-runs/{id}/pay' }),
      ],
    },
    {
      key: 'hrm-reports',
      labelAr: 'التقارير',
      labelEn: 'Reports',
      items: [
        screen('accrual-report', 'تقرير الإستحقاق', 'Accrual report', '/s/hrm/reports/accrual', 'api', { endpoint: '/hrm/payroll-runs/{id}/payslips' }),
        screen('salary-payments-report', 'دفع الرواتب', 'Salary payments', '/s/hrm/reports/payments', 'planned'),
        screen('employee-account', 'حساب موظف', 'Employee account', '/s/hrm/reports/employee-account', 'planned'),
        screen('user-logs', 'سجلات المستخدمين', 'User logs', '/platform/audit', 'ready', { permission: 'platform.audit.view', endpoint: '/audit-log' }),
      ],
    },
  ],
};

// ---------------------------------------------------------------------------
// 6. إدارة المراسي
// ---------------------------------------------------------------------------
const marina: ModuleNode = {
  key: 'marina',
  icon: '⛵',
  labelAr: 'إدارة المراسي',
  labelEn: 'Marina',
  href: '/s/marina/vessels',
  permission: 'marina.view',
  groups: [
    {
      key: 'marina-defs',
      labelAr: 'التعاريف',
      labelEn: 'Definitions',
      items: [
        screen('marina-model', 'بطاقة نموذج', 'Model card', '/s/marina/models', 'api', { endpoint: '/marina/models' }),
        screen('marina-vessel', 'بطاقة مركب', 'Vessel card', '/s/marina/vessels', 'api', { endpoint: '/marina/vessels' }),
        screen('marina-addons', 'بطاقة إضافات', 'Add-ons card', '/s/marina/addons', 'api', { endpoint: '/marina/addons' }),
        screen('marina-owner', 'بطاقة مالك', 'Owner card', '/s/marina/owners', 'api', { endpoint: '/marina/owners' }),
        screen('marina-customer', 'بطاقة عميل', 'Customer card', '/s/parties/customers', 'api', { permission: 'parties.view', endpoint: '/parties?type=customer' }),
      ],
    },
    {
      key: 'marina-manage',
      labelAr: 'إدارة',
      labelEn: 'Management',
      items: [
        screen('marina-prep', 'تحضير المراكب', 'Vessel preparation', '/s/marina/preparation', 'api', { endpoint: '/marina/preparation' }),
        screen('marina-violations', 'المخالفات', 'Violations', '/s/marina/violations', 'planned'),
        screen('marina-rota', 'خطة الدور', 'Rotation plan', '/s/marina/rota', 'api', { endpoint: '/marina/operation-plans' }),
      ],
    },
    {
      key: 'marina-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen('marina-invoice', 'فاتورة', 'Invoice', '/s/marina/invoices', 'api', { endpoint: '/marina/invoices' }),
        screen('marina-link', 'ربط الفواتير', 'Link invoices', '/s/marina/link-invoices', 'planned'),
        screen('marina-bookings', 'حجوزات', 'Bookings', '/s/marina/bookings', 'api', { endpoint: '/marina/bookings' }),
        screen('marina-day-close', 'إغلاق اليومية', 'Day close', '/s/marina/day-close', 'planned'),
      ],
    },
    {
      key: 'marina-reports',
      labelAr: 'التقارير',
      labelEn: 'Reports',
      items: [
        screen('marina-day-closes', 'إغلاقات اليومية', 'Day closes', '/s/marina/reports/day-closes', 'planned'),
        screen('marina-rental-invoices', 'تقرير فواتير التأجير', 'Rental invoices', '/s/marina/reports/rentals', 'planned'),
      ],
    },
  ],
};

// ---------------------------------------------------------------------------
// 7. إدارة المشاريع
// ---------------------------------------------------------------------------
const projects: ModuleNode = {
  key: 'projects',
  icon: '🏗️',
  labelAr: 'إدارة المشاريع',
  labelEn: 'Projects',
  href: '/s/projects/list',
  permission: 'projects.view',
  groups: [
    {
      key: 'projects-defs',
      labelAr: 'التعاريف',
      labelEn: 'Definitions',
      items: [
        screen('boq-item', 'بطاقة بند', 'BOQ item card', '/s/projects/boq-items', 'api', { endpoint: '/projects/boq' }),
        screen('contractor-card', 'بطاقة مقاول', 'Contractor card', '/s/projects/contractors', 'api', { endpoint: '/parties?type=contractor' }),
        screen('project-customer', 'بطاقة عميل', 'Customer card', '/s/parties/customers', 'api', { permission: 'parties.view', endpoint: '/parties?type=customer' }),
        screen('project-stages', 'مراحل مشروع', 'Project stages', '/s/projects/stages', 'api', { endpoint: '/projects/{id}/stages' }),
      ],
    },
    {
      key: 'projects-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen('customer-contract', 'عقد عميل', 'Customer contract', '/s/projects/list', 'api', { endpoint: '/projects' }),
        screen('contractor-contract', 'عقد مقاول', 'Contractor contract', '/s/projects/contractor-contracts', 'planned'),
        screen('project-followup', 'متابعة', 'Follow-up', '/s/projects/followup', 'planned'),
        screen('project-offers', 'عروض', 'Offers', '/s/projects/offers', 'planned'),
        screen('project-receipt', 'سند قبض عميل', 'Customer receipt', '/s/sales/receipts', 'api', { endpoint: '/treasury/receipts' }),
        screen('contractor-payment', 'سند دفع لمقاول', 'Contractor payment', '/s/projects/contractor-payments', 'planned'),
      ],
    },
  ],
};

// ---------------------------------------------------------------------------
// 8. الإعدادات
// ---------------------------------------------------------------------------
const settings: ModuleNode = {
  key: 'settings',
  icon: '⚙️',
  labelAr: 'الإعدادات',
  labelEn: 'Settings',
  href: '/settings/company',
  groups: [
    {
      key: 'settings-org',
      labelAr: 'تعاريف المنشأة',
      labelEn: 'Organisation',
      items: [
        screen('company-card', 'بطاقة المنشأة', 'Company card', '/settings/company', 'ready', { permission: 'platform.tenant.view', endpoint: '/company-profile' }),
        screen('branch-card', 'بطاقة فرع', 'Branch card', '/settings/branches', 'ready', { permission: 'organization.branch.view', endpoint: '/branches' }),
        screen('zatca-settings', 'إعدادات الربط مع هيئة الزكاة والضريبة', 'ZATCA integration', '/s/settings/zatca', 'api', { permission: 'einvoice.view', endpoint: '/einvoicing/credentials' }),
      ],
    },
    {
      key: 'settings-admin',
      labelAr: 'إعدادات إدارية',
      labelEn: 'Administrative',
      items: [
        screen('backup', 'النسخ الإحتياطي', 'Backup', '/s/settings/backup', 'planned'),
        screen('data-rotation', 'تدوير البيانات', 'Data rotation', '/s/settings/rotation', 'planned'),
        screen('new-file', 'إنشاء ملف', 'New company file', '/s/settings/new-file', 'planned'),
        screen('import-export', 'إستيراد وتصدير البيانات', 'Import / export', '/s/migration/runs', 'api', { permission: 'migration.view', endpoint: '/migration/runs' }),
        screen('invoice-maintenance', 'صيانة الفواتير', 'Invoice maintenance', '/s/settings/invoice-maintenance', 'planned'),
        screen('offers', 'العروض', 'Offers', '/s/settings/offers', 'planned'),
        screen('data-sync', 'مزامنة البيانات', 'Data sync', '/s/settings/sync', 'api', { endpoint: '/jobs/outbox' }),
        screen('fiscal-periods-settings', 'الفترات المحاسبية', 'Fiscal periods', '/accounting/periods', 'ready', { permission: 'accounting.period.view', endpoint: '/fiscal-periods' }),
        screen('restore', 'إستعادة البيانات', 'Restore', '/s/settings/restore', 'planned'),
      ],
    },
    {
      key: 'settings-users',
      labelAr: 'إعدادات المستخدمين',
      labelEn: 'Users',
      items: [
        screen('user-card', 'بطاقة مستخدم', 'User card', '/settings/users', 'ready', { permission: 'platform.membership.manage', endpoint: '/memberships' }),
        screen('user-permissions', 'صلاحيات المستخدمين', 'User permissions', '/settings/roles', 'ready', { permission: 'platform.role.manage', endpoint: '/roles' }),
        screen('change-password', 'تغيير كلمة المرور', 'Change password', '/settings/change-password', 'ready', { endpoint: 'POST /auth/change-password' }),
      ],
    },
    {
      key: 'settings-general',
      labelAr: 'إعدادات عامة',
      labelEn: 'General',
      items: [
        screen('general-settings', 'إعدادات عامة', 'General settings', '/settings/general', 'ready', { permission: 'platform.settings.manage', endpoint: '/settings' }),
        screen('prep-device', 'إعدادات جهاز التحضير', 'Preparation device', '/s/settings/prep-device', 'planned'),
        screen('salla-settings', 'إعدادات ربط سلة', 'Salla integration', '/s/integrations/salla/settings', 'api', { permission: 'salla.integration.manage', endpoint: '/integrations/salla/settings' }),
      ],
    },
    {
      key: 'settings-sync',
      labelAr: 'المزامنة',
      labelEn: 'Synchronisation',
      items: [
        screen('sync-invoices', 'مزامنة الفواتير', 'Invoice sync', '/s/settings/sync/invoices', 'planned'),
        screen('sync-journals', 'مزامنة القيود', 'Journal sync', '/s/settings/sync/journals', 'planned'),
        screen('sync-vouchers', 'مزامنة السندات', 'Voucher sync', '/s/settings/sync/vouchers', 'planned'),
        screen('sync-stock', 'مزامنة المخزون', 'Stock sync', '/s/settings/sync/stock', 'planned'),
        screen('sync-manage', 'إدارة المزامنة', 'Sync management', '/s/settings/sync/manage', 'api', { endpoint: '/jobs/queues' }),
        screen('sync-payment-methods', 'إعدادات طريقة الدفع', 'Payment methods', '/s/settings/payment-methods', 'planned'),
        screen('sync-prices', 'إعدادات الأسعار', 'Price settings', '/s/settings/price-lists', 'api', { endpoint: '/price-lists' }),
        screen('sync-zatca', 'مزامنة الفواتير Zatca', 'ZATCA sync', '/s/settings/sync/zatca', 'api', { permission: 'einvoice.view', endpoint: '/einvoicing/submissions' }),
        screen('android-devices', 'أجهزة أندرويد المرتبطة', 'Linked Android devices', '/s/settings/devices', 'api', { endpoint: '/compat/devices' }),
      ],
    },
  ],
};

// ---------------------------------------------------------------------------
// 9. الدعم الفني
// ---------------------------------------------------------------------------
const support: ModuleNode = {
  key: 'support',
  icon: '🎧',
  labelAr: 'الدعم الفني',
  labelEn: 'Support',
  href: '/support/license',
  groups: [
    {
      key: 'support-main',
      labelAr: 'الدعم',
      labelEn: 'Support',
      items: [
        screen('license', 'الترخيص', 'Licence', '/support/license', 'ready', { endpoint: '/billing/subscription' }),
        screen('about', 'عن البرنامج', 'About', '/support/about', 'ready'),
        screen('update', 'تحديث البرنامج', 'Update', '/support/about#updates', 'ready'),
        screen('report-designer', 'فتح المصمم لتصميم التقارير', 'Report designer', '/s/support/report-designer', 'planned'),
        screen('help', '🆘 إطلب المساعدة', 'Request help', '/support/help', 'ready'),
      ],
    },
  ],
};

export const modules: ModuleNode[] = [
  platformConsole,
  accounting,
  inventory,
  purchases,
  sales,
  hrm,
  marina,
  projects,
  settings,
  support,
];

/** Flat index of every screen, used by the scaffold route and by the search box. */
export const allScreens: Array<ScreenItem & { moduleKey: string; moduleLabelAr: string; groupLabelAr: string }> =
  modules.flatMap((module) =>
    module.groups.flatMap((group) =>
      group.items.map((item) => ({
        ...item,
        moduleKey: module.key,
        moduleLabelAr: module.labelAr,
        groupLabelAr: group.labelAr,
      })),
    ),
  );

export function findScreenByHref(href: string) {
  const normalised = href.split('?')[0];
  return allScreens.find((item) => item.href.split('?')[0] === normalised);
}

export function screenCounts() {
  return allScreens.reduce(
    (accumulator, item) => {
      accumulator[item.status] += 1;
      accumulator.total += 1;
      return accumulator;
    },
    { ready: 0, api: 0, planned: 0, total: 0 },
  );
}

/**
 * Filters the tree for the signed-in user. `permissions` is the effective list from
 * `GET /me`; the owner role receives `*`.
 */
export function visibleModules(permissions: string[], isPlatformAdmin: boolean): ModuleNode[] {
  const allows = (permission?: string) =>
    !permission || permissions.includes('*') || permissions.includes(permission);

  return modules
    .filter((module) => (module.platformAdminOnly ? isPlatformAdmin : allows(module.permission)))
    .map((module) => ({
      ...module,
      groups: module.groups
        .map((group) => ({ ...group, items: group.items.filter((item) => allows(item.permission)) }))
        .filter((group) => group.items.length > 0),
    }))
    .filter((module) => module.groups.length > 0);
}
