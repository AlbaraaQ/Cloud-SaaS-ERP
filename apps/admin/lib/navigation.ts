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
        screen('customer-payment-method', 'طريقة دفع عميل', 'Customer payment method', '/accounting/payment-methods', 'ready', { permission: 'parties.view', endpoint: '/payment-methods' }),
        screen('expense-card', 'بطاقة المصاريف', 'Expense card', '/accounting/expenses', 'ready', { permission: 'treasury.view', endpoint: '/expense-types' }),
      ],
    },
    {
      key: 'accounting-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen('opening-entry', 'قيد إفتتاحي', 'Opening entry', '/accounting/journal-entries/new?kind=opening', 'ready', { permission: 'accounting.journal.post', endpoint: 'POST /journal-entries' }),
        screen('journal-voucher', 'سند قيد', 'Journal voucher', '/accounting/journal-entries/new', 'ready', { permission: 'accounting.journal.post', endpoint: 'POST /journal-entries' }),
        screen('receipt-voucher', 'سند قبض', 'Receipt voucher', '/treasury/vouchers?kind=receipt', 'ready', { permission: 'treasury.view', endpoint: '/vouchers' }),
        screen('payment-voucher', 'سند صرف', 'Payment voucher', '/treasury/vouchers?kind=payment', 'ready', { permission: 'treasury.view', endpoint: '/vouchers' }),
        screen('tax-payment-voucher', 'سند صرف الضريبة', 'Tax payment voucher', '/treasury/vouchers?kind=payment', 'ready', { permission: 'treasury.view', endpoint: '/vouchers' }),
        screen('periods', 'الفترات المحاسبية', 'Fiscal periods', '/accounting/periods', 'ready', { permission: 'accounting.period.view', endpoint: '/fiscal-periods' }),
      ],
    },
    {
      key: 'accounting-reports',
      labelAr: 'تقارير محاسبية',
      labelEn: 'Accounting reports',
      items: [
        screen('journals-report', 'القيود اليومية', 'Journal entries', '/accounting/journal-entries', 'ready', { permission: 'accounting.reports.view', endpoint: '/journal-entries' }),
        screen('vouchers-report', 'عرض السندات', 'Vouchers', '/treasury/vouchers', 'ready', { permission: 'treasury.view', endpoint: '/vouchers' }),
        screen('statement', 'كشف حساب', 'Account statement', '/accounting/ledger', 'ready', { permission: 'accounting.reports.view', endpoint: '/statements/general-ledger/{accountId}' }),
        screen('main-statement', 'كشف حساب رئيسي', 'Main account statement', '/accounting/ledger?rollup=1', 'ready', { permission: 'accounting.reports.view', endpoint: '/statements/general-ledger/{accountId}' }),
        screen('cash-movement', 'حركة الصندوق', 'Cash movement', '/reports/cash-movement', 'ready', { permission: 'reporting.view', endpoint: '/reports/cash-movement' }),
        screen('account-balances', 'أرصدة الحسابات', 'Account balances', '/accounting/trial-balance', 'ready', { permission: 'accounting.reports.view', endpoint: '/statements/trial-balance' }),
        screen('cc-balances', 'أرصدة مراكز التكلفة', 'Cost-centre balances', '/reports/cost-center-balances', 'ready', { permission: 'reporting.view' }),
        screen('cc-report', 'تقرير مركز الكلفة', 'Cost-centre report', '/reports/cost-center-report', 'ready', { permission: 'reporting.view' }),
        screen('daily-movement', 'الحركة اليومية', 'Daily movement', '/reports/general-ledger', 'ready', { permission: 'reporting.view' }),
        screen('trial-balance', 'ميزان المراجعة رئيسي', 'Trial balance', '/accounting/trial-balance', 'ready', { permission: 'accounting.reports.view', endpoint: '/statements/trial-balance' }),
        screen('income-statement', 'قائمة الدخل التحليلية', 'Analytical income statement', '/reports/income-statement', 'ready', { permission: 'reporting.view' }),
        screen('balance-sheet', 'ميزانية تحليلية', 'Analytical balance sheet', '/reports/balance-sheet', 'ready', { permission: 'reporting.view' }),
        screen('vat-return', 'الإقرار الضريبي', 'VAT return', '/reports/vat-return', 'ready', { permission: 'reporting.view' }),
        screen('reports-center', 'مركز التقارير (كل التقارير)', 'Report centre', '/reports', 'ready', { permission: 'reporting.view', endpoint: '/reports' }),
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
        screen('items', 'دليل المواد', 'Item directory', '/inventory/items', 'ready', { permission: 'catalog.item.view', endpoint: '/organization/catalog/items' }),
        screen('warehouse-card', 'بطاقة مستودع', 'Warehouse card', '/inventory/warehouses', 'ready', { permission: 'organization.warehouse.view', endpoint: '/warehouses' }),
        screen('group-card', 'بطاقة مجموعة', 'Category card', '/inventory/categories', 'ready', { permission: 'catalog.category.view', endpoint: '/organization/catalog/categories' }),
        screen('unit-card', 'بطاقة وحدة', 'Unit card', '/inventory/units', 'ready', { permission: 'catalog.unit.view', endpoint: '/organization/catalog/units' }),
        screen('item-card', 'بطاقة مادة', 'Item card', '/inventory/items', 'ready', { permission: 'catalog.item.manage', endpoint: 'POST /organization/catalog/items' }),
      ],
    },
    {
      key: 'inventory-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen('transfer', 'مناقلة', 'Transfer', '/inventory/transfers', 'ready', { permission: 'inventory.view', endpoint: '/inventory/transfers' }),
        screen('opening-stock', 'بضاعة أول مدة', 'Opening stock', '/inventory/adjustments', 'ready', { permission: 'inventory.adjust', endpoint: 'POST /inventory/adjustments/post' }),
        screen('goods-in', 'فاتورة إدخال', 'Goods receipt', '/purchases/invoices/new', 'ready', { permission: 'purchase.invoice.create', endpoint: 'POST /purchase-invoices' }),
        screen('goods-out', 'فاتورة إخراج', 'Goods issue', '/sales/invoices/new', 'ready', { permission: 'sales.invoice.create', endpoint: 'POST /sales/invoices' }),
        screen('stock-adjust', 'تسوية مخزنية', 'Stock adjustment', '/inventory/adjustments', 'ready', { permission: 'inventory.adjust', endpoint: 'POST /inventory/adjustments/post' }),
        screen('stock-delivery', 'توصيل مخزني', 'Stock delivery', '/inventory/deliveries', 'ready', { permission: 'inventory.view', endpoint: '/inventory/deliveries' }),
        screen('goods-request', 'طلب بضاعة', 'Goods request', '/inventory/requests', 'ready', { permission: 'inventory.view', endpoint: '/inventory/requests' }),
        screen('barcode', 'طباعة الباركود', 'Barcode printing', '/inventory/barcodes', 'ready', { permission: 'catalog.item.view', endpoint: '/organization/catalog/items' }),
      ],
    },
    {
      key: 'inventory-reports',
      labelAr: 'تقارير مستودعية',
      labelEn: 'Inventory reports',
      items: [
        screen('stock-count', 'جرد المواد', 'Stock count', '/inventory/levels', 'ready', { permission: 'inventory.view', endpoint: '/inventory/levels' }),
        screen('item-movement', 'حركة مادة تفصيلي', 'Item movement (detail)', '/inventory/movements', 'ready', { permission: 'inventory.view', endpoint: '/inventory/movements' }),
        screen('items-movement', 'حركة مواد تجميعي', 'Item movement (summary)', '/reports/item-movement-summary', 'ready', { permission: 'reporting.view', endpoint: '/reports/inventory-movement' }),
        screen('expiry', 'صلاحية المواد', 'Item expiry', '/inventory/lots', 'ready', { permission: 'inventory.view', endpoint: '/inventory/lots' }),
        screen('sales-analysis', 'تحليل المبيعات', 'Sales analysis', '/reports/sales-analysis', 'ready', { permission: 'reporting.view' }),
        screen('purchase-sales-total', 'إجمالي المبيعات والمشتريات', 'Sales & purchases total', '/reports/sales-purchases-total', 'ready', { permission: 'reporting.view' }),
        screen('invoice-profit', 'أرباح الفواتير', 'Invoice profit', '/reports/invoice-profit', 'ready', { permission: 'reporting.view' }),
        screen('turnover', 'معدل الدوران والركود', 'Turnover & dead stock', '/reports/inventory-turnover', 'ready', { permission: 'reporting.view' }),
        screen('production-order', 'تقرير أمر الإنتاج', 'Production order report', '/s/inventory/reports/production', 'planned'),
        screen('invoices-by-type', 'الفواتير بحسب النوع', 'Invoices by type', '/reports/invoices-by-type', 'ready', { permission: 'reporting.view' }),
        screen('expired-items', 'انتهاء صلاحية الأصناف', 'Expired items', '/reports/expired-items', 'ready', { permission: 'reporting.view' }),
        screen('serials', 'تقرير الأرقام التسلسلية', 'Serial numbers report', '/inventory/serials', 'ready', { permission: 'inventory.view', endpoint: '/inventory/serials' }),
      ],
    },
    {
      key: 'inventory-salla',
      labelAr: 'متجر سلة',
      labelEn: 'Salla store',
      items: [
        screen('salla-products', 'المنتجات', 'Products', '/integrations/salla/products', 'ready', { permission: 'salla.integration.view', endpoint: '/integrations/salla/products' }),
        screen('salla-orders', 'إدارة الطلبات', 'Orders', '/integrations/salla/orders', 'ready', { permission: 'salla.integration.view', endpoint: '/integrations/salla/orders' }),
        screen('salla-warehouses', 'ربط المستودعات', 'Warehouse mapping', '/integrations/salla/warehouses', 'ready', { permission: 'salla.integration.view', endpoint: '/integrations/salla/mappings' }),
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
  href: '/purchases/invoices',
  permission: 'purchase.view',
  groups: [
    {
      key: 'purchases-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen('purchase-invoice', 'فاتورة المشتريات', 'Purchase invoice', '/purchases/invoices', 'ready', { endpoint: '/purchase-invoices' }),
        screen('purchase-return', 'مردود المشتريات', 'Purchase return', '/purchases/invoices/new?kind=purchase_return', 'ready', { permission: 'purchase.invoice.create', endpoint: 'POST /purchase-invoices' }),
      ],
    },
    {
      key: 'purchases-vouchers',
      labelAr: 'السندات',
      labelEn: 'Vouchers',
      items: [
        screen('supplier-payment', 'سند صرف لمورد', 'Supplier payment', '/treasury/vouchers?kind=payment', 'ready', { permission: 'treasury.view', endpoint: '/vouchers' }),
        screen('purchase-vouchers', 'عرض السندات', 'Vouchers', '/treasury/vouchers', 'ready', { permission: 'treasury.view', endpoint: '/vouchers' }),
        screen('purchase-credit-note', 'إشعار دائن', 'Credit note', '/purchases/notes/credit', 'ready', { permission: 'purchase.view', endpoint: '/purchases/adjustment-notes' }),
      ],
    },
    {
      key: 'purchases-notes',
      labelAr: 'الإشعارات',
      labelEn: 'Notes',
      items: [
        screen('pn-credit', 'إشعار دائن', 'Credit note', '/purchases/notes/credit', 'ready', { permission: 'purchase.view', endpoint: '/purchases/adjustment-notes' }),
        screen('pn-debit', 'إشعار مدين', 'Debit note', '/purchases/notes/debit', 'ready', { permission: 'purchase.view', endpoint: '/purchases/adjustment-notes' }),
        screen('pn-report', 'تقرير الإشعارات', 'Notes report', '/reports/purchase-notes', 'ready', { permission: 'reporting.view' }),
      ],
    },
    {
      key: 'purchases-reports',
      labelAr: 'التقارير',
      labelEn: 'Reports',
      items: [
        screen('supplier-statement', 'كشف مورد', 'Supplier statement', '/sales/statements?kind=supplier', 'ready', { permission: 'parties.view', endpoint: '/parties/{id}/statement' }),
        screen('purchase-invoices-report', 'تقرير فواتير المشتريات', 'Purchase invoices', '/reports/purchase-invoices', 'ready', { permission: 'reporting.view', endpoint: '/reports/purchases' }),
        screen('purchase-returns-report', 'تقرير مردود فواتير المشتريات', 'Purchase returns', '/reports/purchase-returns', 'ready', { permission: 'reporting.view' }),
        screen('net-purchases', 'صافي المشتريات', 'Net purchases', '/reports/net-purchases', 'ready', { permission: 'reporting.view' }),
        screen('purchases-detail', 'مشتريات تفصيلية', 'Detailed purchases', '/reports/purchases-detail', 'ready', { permission: 'reporting.view' }),
        screen('purchases-items', 'مشتريات الأصناف تجميعي', 'Purchases by item', '/reports/purchases-by-item', 'ready', { permission: 'reporting.view' }),
        screen('supplier-balances', 'أرصدة الموردين', 'Supplier balances', '/reports/supplier-balances', 'ready', { permission: 'reporting.view', endpoint: '/reports/party-balances' }),
        screen('supplier-settlements', 'سداد الموردين', 'Supplier settlements', '/reports/supplier-settlements', 'ready', { permission: 'reporting.view' }),
        screen('employee-purchases', 'مشتريات موظف', 'Purchases by employee', '/reports/purchases-by-employee', 'ready', { permission: 'reporting.view' }),
        screen('invoices-by-supplier', 'الفواتير بحسب الموردين', 'Invoices by supplier', '/reports/invoices-by-supplier', 'ready', { permission: 'reporting.view' }),
      ],
    },
    {
      key: 'purchases-other',
      labelAr: 'أخرى',
      labelEn: 'Other',
      items: [screen('supplier-card', 'بطاقة مورد', 'Supplier card', '/purchases/suppliers', 'ready', { permission: 'parties.view', endpoint: '/parties?kind=supplier' })],
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
  href: '/sales/invoices',
  permission: 'sales.view',
  groups: [
    {
      key: 'sales-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen('pos', 'نقطة البيع', 'Point of sale', '/sales/pos', 'ready', { permission: 'sales.invoice.create', endpoint: 'POST /sales/invoices' }),
        screen('sales-invoice', 'فاتورة مبيعات', 'Sales invoice', '/sales/invoices', 'ready', { endpoint: '/sales/invoices' }),
        screen('sales-return', 'مردود المبيعات', 'Sales return', '/sales/returns', 'ready', { permission: 'sales.return.create', endpoint: 'POST /sales/invoices/{id}/return' }),
        screen('quotation', 'عرض سعر', 'Quotation', '/sales/quotations', 'ready', { permission: 'sales.view', endpoint: '/sales/quotations' }),
        screen('day-close', 'إغلاق اليومية', 'Day close', '/sales/shifts', 'ready', { permission: 'treasury.view', endpoint: '/shift-closes' }),
        screen('contracting-invoice', 'فاتورة المقاولات', 'Contracting invoice', '/projects', 'ready', { permission: 'projects.view', endpoint: '/projects/{id}/progress-bills' }),
        screen('contracting-return', 'مرتجع مقاولات', 'Contracting return', '/s/projects/returns', 'planned'),
      ],
    },
    {
      key: 'sales-vouchers',
      labelAr: 'السندات',
      labelEn: 'Vouchers',
      items: [
        screen('customer-receipt', 'سند قبض عميل', 'Customer receipt', '/treasury/vouchers?kind=receipt', 'ready', { permission: 'treasury.view', endpoint: '/vouchers' }),
        screen('sales-vouchers', 'عرض السندات', 'Vouchers', '/treasury/vouchers', 'ready', { permission: 'treasury.view', endpoint: '/vouchers' }),
        screen('sales-debit-note', 'إشعار مدين', 'Debit note', '/sales/notes/debit', 'ready', { permission: 'sales.view', endpoint: '/sales/adjustment-notes' }),
      ],
    },
    {
      key: 'sales-notes',
      labelAr: 'الإشعارات',
      labelEn: 'Notes',
      items: [
        screen('sn-credit', 'إشعار دائن', 'Credit note', '/sales/notes/credit', 'ready', { permission: 'sales.view', endpoint: '/sales/adjustment-notes' }),
        screen('sn-debit', 'إشعار مدين', 'Debit note', '/sales/notes/debit', 'ready', { permission: 'sales.view', endpoint: '/sales/adjustment-notes' }),
        screen('sn-report', 'تقرير الإشعارات', 'Notes report', '/reports/sales-notes', 'ready', { permission: 'reporting.view' }),
      ],
    },
    {
      key: 'sales-reports',
      labelAr: 'التقارير',
      labelEn: 'Reports',
      items: [
        screen('day-closes', 'إغلاقات اليومية', 'Day closes', '/sales/shifts', 'ready', { permission: 'treasury.view', endpoint: '/shift-closes' }),
        screen('sales-invoices-report', 'تقرير فواتير المبيعات', 'Sales invoices', '/reports/sales-invoices', 'ready', { permission: 'reporting.view', endpoint: '/reports/sales' }),
        screen('sales-returns-report', 'تقرير مردود فواتير المبيعات', 'Sales returns', '/reports/sales-returns', 'ready', { permission: 'reporting.view' }),
        screen('net-sales', 'صافي المبيعات', 'Net sales', '/reports/net-sales', 'ready', { permission: 'reporting.view' }),
        screen('sales-detail', 'مبيعات تفصيلية', 'Detailed sales', '/reports/sales-detail', 'ready', { permission: 'reporting.view' }),
        screen('sales-items', 'مبيعات الأصناف تجميعي', 'Sales by item', '/reports/sales-by-item', 'ready', { permission: 'reporting.view' }),
        screen('sales-by-category', 'مبيعات بحسب الفئة', 'Sales by category', '/reports/sales-by-category', 'ready', { permission: 'reporting.view' }),
        screen('sales-invoice-profit', 'أرباح الفواتير', 'Invoice profit', '/reports/invoice-profit', 'ready', { permission: 'reporting.view' }),
        screen('item-profit', 'أرباح الأصناف', 'Item profit', '/reports/item-profit', 'ready', { permission: 'reporting.view' }),
        screen('item-profit-detail', 'تفاصيل أرباح الأصناف', 'Item profit detail', '/reports/item-profit-detail', 'ready', { permission: 'reporting.view' }),
        screen('reps-report', 'تقرير المندوبين', 'Sales reps', '/reports/sales-by-salesman', 'ready', { permission: 'reporting.view' }),
        screen('employee-sales', 'مبيعات موظف', 'Sales by employee', '/reports/sales-by-employee', 'ready', { permission: 'reporting.view' }),
        screen('customer-statement', 'كشف عميل', 'Customer statement', '/sales/statements', 'ready', { permission: 'parties.view', endpoint: '/parties/{id}/statement' }),
        screen('customer-balances', 'أرصدة العملاء', 'Customer balances', '/reports/customer-balances', 'ready', { permission: 'reporting.view', endpoint: '/reports/party-balances' }),
        screen('customer-settlements', 'سداد العملاء', 'Customer settlements', '/reports/customer-settlements', 'ready', { permission: 'reporting.view' }),
        screen('invoices-by-customer', 'الفواتير بحسب العملاء', 'Invoices by customer', '/reports/invoices-by-customer', 'ready', { permission: 'reporting.view' }),
        screen('sales-movement', 'حركة المبيعات', 'Sales movement', '/reports/sales-by-day', 'ready', { permission: 'reporting.view' }),
        screen('sales-chart', 'تقرير بياني', 'Chart report', '/reports/monthly-sales', 'ready', { permission: 'reporting.view' }),
        screen('contracting-invoices-report', 'تقرير فواتير المقاولات', 'Contracting invoices', '/reports/project-bills', 'ready', { permission: 'reporting.view' }),
      ],
    },
    {
      key: 'sales-pos-reports',
      labelAr: 'تقارير نقطة البيع',
      labelEn: 'POS reports',
      items: [
        screen('pos-sales', 'تقرير مبيعات POS', 'POS sales', '/reports/pos-sales', 'ready', { permission: 'reporting.view', endpoint: '/pos/reports/sales' }),
        screen('pos-item-detail', 'تفاصيل أصناف POS', 'POS item detail', '/reports/pos-item-detail', 'ready', { permission: 'reporting.view' }),
        screen('pos-item-summary', 'أصناف POS تجميعي', 'POS item summary', '/reports/pos-item-summary', 'ready', { permission: 'reporting.view' }),
        screen('pos-daily', 'تقرير المبيعات اليومية', 'Daily sales', '/reports/pos-daily', 'ready', { permission: 'reporting.view' }),
        screen('pos-group', 'تقرير مبيعات للمجموعة', 'Sales by group', '/reports/pos-by-category', 'ready', { permission: 'reporting.view' }),
      ],
    },
    {
      key: 'sales-other',
      labelAr: 'أخرى',
      labelEn: 'Other',
      items: [
        screen('customer-card', 'بطاقة عميل', 'Customer card', '/sales/customers', 'ready', { permission: 'parties.view', endpoint: '/parties?kind=customer' }),
        screen('rep-card', 'بطاقة مندوب', 'Sales rep card', '/sales/salesmen', 'ready', { permission: 'sales.view', endpoint: '/sales/salesmen' }),
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
  href: '/hrm/employees',
  permission: 'hrm.view',
  groups: [
    {
      key: 'hrm-defs',
      labelAr: 'التعاريف',
      labelEn: 'Definitions',
      items: [
        screen('departments', 'تعريف الإدارات', 'Departments', '/hrm/departments', 'ready', { endpoint: '/hrm/departments' }),
        screen('sections', 'تعريف الأقسام والوظائف', 'Jobs', '/hrm/jobs', 'ready', { endpoint: '/hrm/jobs' }),
        screen('employee', 'تعريف موظف', 'Employee', '/hrm/employees', 'ready', { endpoint: '/hrm/employees' }),
      ],
    },
    {
      key: 'hrm-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen('adjustments', 'الحوافز والجزاءات', 'Bonuses & deductions', '/hrm/adjustments', 'ready', { endpoint: '/hrm/adjustments' }),
        screen('payroll-run', 'إستحقاق راتب', 'Payroll run', '/hrm/payroll', 'ready', { endpoint: '/hrm/payroll/runs' }),
        screen('salary-payment', 'سند صرف راتب', 'Salary payment voucher', '/hrm/payroll', 'ready', { permission: 'hrm.payroll.post', endpoint: 'POST /hrm/payroll/runs/{id}/pay' }),
      ],
    },
    {
      key: 'hrm-reports',
      labelAr: 'التقارير',
      labelEn: 'Reports',
      items: [
        screen('accrual-report', 'تقرير الإستحقاق', 'Accrual report', '/hrm/payroll', 'ready', { endpoint: 'POST /hrm/payroll/preview' }),
        screen('salary-payments-report', 'دفع الرواتب', 'Salary payments', '/reports/payroll-payments', 'ready', { permission: 'reporting.view' }),
        screen('employee-account', 'حساب موظف', 'Employee account', '/reports/employee-account', 'ready', { permission: 'reporting.view' }),
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
  href: '/marina/vessels',
  permission: 'marina.view',
  groups: [
    {
      key: 'marina-defs',
      labelAr: 'التعاريف',
      labelEn: 'Definitions',
      items: [
        screen('marina-model', 'بطاقة نموذج', 'Model card', '/marina/vessels', 'ready', { endpoint: 'POST /marina/groups' }),
        screen('marina-vessel', 'بطاقة مركب', 'Vessel card', '/marina/vessels', 'ready', { endpoint: '/marina' }),
        screen('marina-addons', 'بطاقة إضافات', 'Add-ons card', '/marina/bookings', 'ready', { permission: 'marina.manage', endpoint: 'POST /marina/bookings/{id}/additions' }),
        screen('marina-owner', 'بطاقة مالك', 'Owner card', '/marina/vessels', 'ready', { permission: 'marina.manage', endpoint: 'POST /marina/vessels/{id}/owners' }),
        screen('marina-customer', 'بطاقة عميل', 'Customer card', '/sales/customers', 'ready', { permission: 'parties.view', endpoint: '/parties?kind=customer' }),
      ],
    },
    {
      key: 'marina-manage',
      labelAr: 'إدارة',
      labelEn: 'Management',
      items: [
        screen('marina-prep', 'تحضير المراكب', 'Vessel preparation', '/marina/preparation', 'ready', { permission: 'marina.view', endpoint: '/marina/preparations' }),
        screen('marina-violations', 'المخالفات', 'Violations', '/marina/violations', 'ready', { endpoint: '/marina/violations' }),
        screen('marina-rota', 'خطة الدور', 'Rotation plan', '/marina/rota', 'ready', { permission: 'marina.view', endpoint: '/marina/operation-plans' }),
      ],
    },
    {
      key: 'marina-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen('marina-invoice', 'فاتورة', 'Invoice', '/marina/bookings', 'ready', { permission: 'marina.invoice', endpoint: 'POST /marina/bookings/{id}/rental-invoice' }),
        screen('marina-link', 'ربط الفواتير', 'Link invoices', '/marina/link-invoices', 'ready', { permission: 'marina.view', endpoint: '/marina/rental-invoices' }),
        screen('marina-bookings', 'حجوزات', 'Bookings', '/marina/bookings', 'ready', { endpoint: '/marina/bookings' }),
        screen('marina-day-close', 'إغلاق اليومية', 'Day close', '/marina/day-close', 'ready', { permission: 'marina.view', endpoint: '/marina/day-close' }),
      ],
    },
    {
      key: 'marina-reports',
      labelAr: 'التقارير',
      labelEn: 'Reports',
      items: [
        screen('marina-day-closes', 'إغلاقات اليومية', 'Day closes', '/reports/cashier-shift', 'ready', { permission: 'reporting.view' }),
        screen('marina-rental-invoices', 'تقرير فواتير التأجير', 'Rental invoices', '/reports/marina-rentals', 'ready', { permission: 'reporting.view' }),
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
  href: '/projects',
  permission: 'projects.view',
  groups: [
    {
      key: 'projects-defs',
      labelAr: 'التعاريف',
      labelEn: 'Definitions',
      items: [
        screen('boq-item', 'بطاقة بند', 'BOQ item card', '/projects', 'ready', { permission: 'projects.manage', endpoint: 'POST /projects/{id}/boq' }),
        screen('contractor-card', 'بطاقة مقاول', 'Contractor card', '/purchases/suppliers', 'ready', { permission: 'parties.view', endpoint: '/parties?kind=supplier' }),
        screen('project-customer', 'بطاقة عميل', 'Customer card', '/sales/customers', 'ready', { permission: 'parties.view', endpoint: '/parties?kind=customer' }),
        screen('project-stages', 'مراحل مشروع', 'Project stages', '/projects', 'ready', { permission: 'projects.manage', endpoint: 'POST /projects/{id}/stages' }),
      ],
    },
    {
      key: 'projects-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen('customer-contract', 'عقد عميل', 'Customer contract', '/projects', 'ready', { endpoint: '/projects' }),
        screen('contractor-contract', 'عقد مقاول', 'Contractor contract', '/s/projects/contractor-contracts', 'planned'),
        screen('project-followup', 'متابعة', 'Follow-up', '/projects/followup', 'ready', { permission: 'projects.view', endpoint: '/projects/{id}' }),
        screen('project-offers', 'عروض', 'Offers', '/s/projects/offers', 'planned'),
        screen('project-receipt', 'سند قبض عميل', 'Customer receipt', '/treasury/vouchers?kind=receipt', 'ready', { permission: 'treasury.view', endpoint: '/vouchers' }),
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
        screen('posting-profiles', 'الربط المحاسبي', 'Posting profiles', '/settings/posting-profiles', 'ready', { permission: 'organization.postingprofile.view', endpoint: '/branch-posting-profiles' }),
        screen('zatca-settings', 'إعدادات الربط مع هيئة الزكاة والضريبة', 'ZATCA integration', '/settings/zatca', 'ready', { permission: 'einvoice.view', endpoint: '/einvoice/credentials' }),
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
        screen('import-export', 'إستيراد وتصدير البيانات', 'Import / export', '/migration/runs', 'ready', { permission: 'migration.view', endpoint: '/migration/runs' }),
        screen('invoice-maintenance', 'صيانة الفواتير', 'Invoice maintenance', '/s/settings/invoice-maintenance', 'planned'),
        screen('offers', 'العروض', 'Offers', '/settings/offers', 'ready', { permission: 'sales.view', endpoint: '/sales/offers' }),
        screen('data-sync', 'مزامنة البيانات', 'Data sync', '/settings/sync', 'ready', { permission: 'compat.manage', endpoint: '/jobs/outbox' }),
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
        screen('salla-settings', 'إعدادات ربط سلة', 'Salla integration', '/integrations/salla/settings', 'ready', { permission: 'salla.integration.view', endpoint: '/integrations/salla/settings' }),
      ],
    },
    {
      key: 'settings-sync',
      labelAr: 'المزامنة',
      labelEn: 'Synchronisation',
      items: [
        screen('sync-invoices', 'مزامنة الفواتير', 'Invoice sync', '/settings/sync/invoices', 'ready', { permission: 'compat.manage', endpoint: '/compat/sync/documents?entity=invoices' }),
        screen('sync-journals', 'مزامنة القيود', 'Journal sync', '/settings/sync/journals', 'ready', { permission: 'compat.manage', endpoint: '/compat/sync/documents?entity=journals' }),
        screen('sync-vouchers', 'مزامنة السندات', 'Voucher sync', '/settings/sync/vouchers', 'ready', { permission: 'compat.manage', endpoint: '/compat/sync/documents?entity=vouchers' }),
        screen('sync-stock', 'مزامنة المخزون', 'Stock sync', '/settings/sync/stock', 'ready', { permission: 'compat.manage', endpoint: '/compat/sync/documents?entity=stock' }),
        screen('sync-manage', 'إدارة المزامنة', 'Sync management', '/settings/sync/manage', 'ready', { permission: 'compat.manage', endpoint: '/jobs/queues' }),
        screen('sync-payment-methods', 'إعدادات طريقة الدفع', 'Payment methods', '/accounting/payment-methods', 'ready', { permission: 'parties.view', endpoint: '/payment-methods' }),
        screen('sync-prices', 'إعدادات الأسعار', 'Price settings', '/settings/price-lists', 'ready', { permission: 'organization.priceList.view', endpoint: '/price-lists' }),
        screen('sync-zatca', 'مزامنة الفواتير Zatca', 'ZATCA sync', '/settings/sync/zatca', 'ready', { permission: 'einvoice.view', endpoint: '/einvoice/submissions' }),
        screen('android-devices', 'أجهزة أندرويد المرتبطة', 'Linked Android devices', '/settings/devices', 'ready', { permission: 'compat.manage', endpoint: '/compat/devices' }),
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
