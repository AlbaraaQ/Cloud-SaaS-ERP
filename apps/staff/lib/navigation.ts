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
// NOTE: the platform console used to be module 0 here. It moved to the dedicated
// `apps/platform-admin` deployment during the 2026-09 surface separation — the staff
// surface must not route to `/platform/*` at all. Operators reach the console through
// the dashboard link (NEXT_PUBLIC_PLATFORM_URL).
// ---------------------------------------------------------------------------

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
        screen('coa', 'دليل الحسابات', 'Chart of accounts', '/accounting/accounts', 'ready', {
          permission: 'accounting.account.view',
          endpoint: '/accounts',
        }),
        screen('coa-tree', 'شجرة الحسابات', 'Account tree', '/accounting/accounts/tree', 'ready', {
          permission: 'accounting.account.view',
          endpoint: '/accounts',
        }),
        screen('cash-card', 'بطاقة صندوق', 'Cash box card', '/accounting/cash-locations?type=cash', 'ready', {
          permission: 'organization.cashlocation.view',
          endpoint: '/cash-locations',
        }),
        screen('bank-card', 'بطاقة بنك', 'Bank card', '/accounting/cash-locations?type=bank', 'ready', {
          permission: 'organization.cashlocation.view',
          endpoint: '/cash-locations',
        }),
        screen('cost-center', 'بطاقة مركز تكلفة', 'Cost centre card', '/accounting/cost-centers', 'ready', {
          permission: 'accounting.account.view',
          endpoint: '/cost-centers',
        }),
        screen('payment-methods', 'طرق الدفع', 'Payment methods', '/accounting/payment-methods', 'ready', {
          permission: 'parties.view',
          endpoint: '/payment-methods',
        }),
      ],
    },
    {
      key: 'accounting-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen(
          'opening-entry',
          'قيد إفتتاحي',
          'Opening entry',
          '/accounting/journal-entries/new?kind=opening',
          'ready',
          { permission: 'accounting.journal.post', endpoint: 'POST /journal-entries' },
        ),
        screen('journal-voucher', 'سند قيد', 'Journal voucher', '/accounting/journal-entries/new', 'ready', {
          permission: 'accounting.journal.post',
          endpoint: 'POST /journal-entries',
        }),
        screen('periods', 'الفترات المحاسبية', 'Fiscal periods', '/accounting/periods', 'ready', {
          permission: 'accounting.period.view',
          endpoint: '/fiscal-periods',
        }),
      ],
    },
    {
      key: 'accounting-reports',
      labelAr: 'تقارير محاسبية',
      labelEn: 'Accounting reports',
      items: [
        screen(
          'journals-report',
          'القيود اليومية',
          'Journal entries',
          '/accounting/journal-entries',
          'ready',
          { permission: 'accounting.reports.view', endpoint: '/journal-entries' },
        ),
        screen('vouchers-report', 'عرض السندات', 'Vouchers', '/treasury/vouchers', 'ready', {
          permission: 'treasury.view',
          endpoint: '/vouchers',
        }),
        screen('statement', 'كشف حساب', 'Account statement', '/accounting/ledger', 'ready', {
          permission: 'accounting.reports.view',
          endpoint: '/statements/general-ledger/{accountId}',
        }),
        screen(
          'main-statement',
          'كشف حساب رئيسي',
          'Main account statement',
          '/accounting/ledger?rollup=1',
          'ready',
          { permission: 'accounting.reports.view', endpoint: '/statements/general-ledger/{accountId}' },
        ),
        screen('cash-movement', 'حركة الصندوق', 'Cash movement', '/reports/cash-movement', 'ready', {
          permission: 'reporting.view',
          endpoint: '/reports/cash-movement',
        }),
        screen(
          'cc-balances',
          'أرصدة مراكز التكلفة',
          'Cost-centre balances',
          '/reports/cost-center-balances',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen(
          'cc-report',
          'تقرير مركز الكلفة',
          'Cost-centre report',
          '/reports/cost-center-report',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen('daily-movement', 'الحركة اليومية', 'Daily movement', '/reports/general-ledger', 'ready', {
          permission: 'reporting.view',
        }),
        screen('trial-balance', 'ميزان المراجعة', 'Trial balance', '/accounting/trial-balance', 'ready', {
          permission: 'accounting.reports.view',
          endpoint: '/statements/trial-balance',
        }),
        screen(
          'income-statement',
          'قائمة الدخل التحليلية',
          'Analytical income statement',
          '/reports/income-statement',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen(
          'balance-sheet',
          'ميزانية تحليلية',
          'Analytical balance sheet',
          '/reports/balance-sheet',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen('vat-return', 'الإقرار الضريبي', 'VAT return', '/reports/vat-return', 'ready', {
          permission: 'reporting.view',
        }),
        screen('reports-center', 'مركز التقارير (كل التقارير)', 'Report centre', '/reports', 'ready', {
          permission: 'reporting.view',
          endpoint: '/reports',
        }),
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
      key: 'inventory-overview',
      labelAr: 'نظرة عامة',
      labelEn: 'Overview',
      items: [
        screen('inventory-overview', 'لوحة المخزون', 'Inventory overview', '/inventory/overview', 'ready', {
          permission: 'inventory.view',
          endpoint: '/inventory/levels',
        }),
      ],
    },
    {
      key: 'inventory-defs',
      labelAr: 'التعاريف',
      labelEn: 'Definitions',
      items: [
        screen('items', 'دليل المواد وبطاقاتها', 'Item directory and cards', '/inventory/items', 'ready', {
          permission: 'catalog.item.view',
          endpoint: '/organization/catalog/items',
        }),
        screen('warehouse-card', 'بطاقة مستودع', 'Warehouse card', '/inventory/warehouses', 'ready', {
          permission: 'organization.warehouse.view',
          endpoint: '/warehouses',
        }),
        screen('group-card', 'بطاقة مجموعة', 'Category card', '/inventory/categories', 'ready', {
          permission: 'catalog.category.view',
          endpoint: '/organization/catalog/categories',
        }),
        screen('unit-card', 'بطاقة وحدة', 'Unit card', '/inventory/units', 'ready', {
          permission: 'catalog.unit.view',
          endpoint: '/organization/catalog/units',
        }),
        screen(
          'item-units',
          'وحدات الصنف والباركود',
          'Item units and barcodes',
          '/inventory/item-units',
          'ready',
          {
            permission: 'catalog.item.view',
            endpoint: '/organization/catalog/items/:id/units',
          },
        ),
      ],
    },
    {
      key: 'inventory-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen('transfer', 'مناقلة', 'Transfer', '/inventory/transfers', 'ready', {
          permission: 'inventory.view',
          endpoint: '/inventory/transfers',
        }),
        screen(
          'opening-stock',
          'بضاعة أول مدة',
          'Opening stock',
          '/inventory/vouchers?kind=opening',
          'ready',
          { permission: 'inventory.adjust', endpoint: 'POST /inventory/vouchers/:id/post' },
        ),
        screen('goods-in', 'فاتورة إدخال', 'Goods receipt', '/purchases/invoices/new', 'ready', {
          permission: 'purchase.invoice.create',
          endpoint: 'POST /purchase-invoices',
        }),
        screen('goods-out', 'فاتورة إخراج', 'Goods issue', '/sales/invoices/new', 'ready', {
          permission: 'sales.invoice.create',
          endpoint: 'POST /sales/invoices',
        }),
        screen('stock-adjust', 'تسوية مخزنية', 'Stock adjustment', '/inventory/adjustments', 'ready', {
          permission: 'inventory.adjust',
          endpoint: 'POST /inventory/adjustments/:id/post',
        }),
        screen('stock-voucher', 'سند إدخال / إخراج', 'Stock voucher', '/inventory/vouchers', 'ready', {
          permission: 'inventory.adjust',
          endpoint: '/inventory/vouchers',
        }),
        screen('stock-delivery', 'توصيل مخزني', 'Stock delivery', '/inventory/deliveries', 'ready', {
          permission: 'inventory.view',
          endpoint: '/inventory/deliveries',
        }),
        screen('goods-request', 'طلب بضاعة', 'Goods request', '/inventory/requests', 'ready', {
          permission: 'inventory.view',
          endpoint: '/inventory/requests',
        }),
        screen('barcode', 'طباعة الباركود', 'Barcode printing', '/inventory/barcodes', 'ready', {
          permission: 'catalog.item.view',
          endpoint: '/organization/catalog/items',
        }),
      ],
    },
    {
      key: 'inventory-reports',
      labelAr: 'تقارير مستودعية',
      labelEn: 'Inventory reports',
      items: [
        screen('stock-count', 'جرد المواد', 'Stock count', '/inventory/levels', 'ready', {
          permission: 'inventory.view',
          endpoint: '/inventory/levels',
        }),
        screen(
          'below-minimum',
          'أصناف تحت حد الطلب',
          'Below reorder point',
          '/inventory/below-minimum',
          'ready',
          { permission: 'inventory.view', endpoint: '/inventory/below-minimum' },
        ),
        screen('expiry', 'تواريخ الصلاحية', 'Expiry dates', '/inventory/expiry', 'ready', {
          permission: 'inventory.view',
          endpoint: '/inventory/expiry',
        }),
        screen('item-card', 'بطاقة الصنف', 'Item card (stock ledger)', '/inventory/item-card', 'ready', {
          permission: 'inventory.view',
          endpoint: '/inventory/item-card',
        }),
        screen(
          'item-movement',
          'حركة مادة تفصيلي',
          'Item movement (detail)',
          '/inventory/movements',
          'ready',
          { permission: 'inventory.view', endpoint: '/inventory/movements' },
        ),
        screen(
          'items-movement',
          'حركة مواد تجميعي',
          'Item movement (summary)',
          '/reports/item-movement-summary',
          'ready',
          { permission: 'reporting.view', endpoint: '/reports/inventory-movement' },
        ),
        screen('lots', 'صلاحية المواد (الدفعات)', 'Item expiry (lots)', '/inventory/lots', 'ready', {
          permission: 'inventory.view',
          endpoint: '/inventory/lots',
        }),
        screen('sales-analysis', 'تحليل المبيعات', 'Sales analysis', '/reports/sales-analysis', 'ready', {
          permission: 'reporting.view',
        }),
        screen(
          'purchase-sales-total',
          'إجمالي المبيعات والمشتريات',
          'Sales & purchases total',
          '/reports/sales-purchases-total',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen(
          'turnover',
          'معدل الدوران والركود',
          'Turnover & dead stock',
          '/reports/inventory-turnover',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen(
          'inventory-valuation',
          'جرد المواد وتقييم المخزون',
          'Inventory valuation',
          '/reports/inventory-valuation',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen(
          'stock-limits',
          'الأصناف تحت الحد الأدنى',
          'Items below the minimum',
          '/reports/stock-limits',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen(
          'expiry-report',
          'صلاحية المواد',
          'Expiry report',
          '/reports/expiry-report',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen(
          'serial-tracking',
          'تتبّع الأرقام التسلسلية',
          'Serial tracking report',
          '/reports/serial-tracking',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen(
          'production-order',
          'تقرير أمر الإنتاج',
          'Production order report',
          '/inventory/production',
          'ready',
          { permission: 'inventory.view', endpoint: '/inventory/production-orders' },
        ),
        screen(
          'invoices-by-type',
          'الفواتير بحسب النوع',
          'Invoices by type',
          '/reports/invoices-by-type',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen('expired-items', 'انتهاء صلاحية الأصناف', 'Expired items', '/reports/expired-items', 'ready', {
          permission: 'reporting.view',
        }),
        screen('serials', 'تقرير الأرقام التسلسلية', 'Serial numbers report', '/inventory/serials', 'ready', {
          permission: 'inventory.view',
          endpoint: '/inventory/serials',
        }),
      ],
    },
    {
      key: 'inventory-salla',
      labelAr: 'متجر سلة',
      labelEn: 'Salla store',
      items: [
        screen('salla-products', 'المنتجات', 'Products', '/integrations/salla/products', 'ready', {
          permission: 'salla.integration.view',
          endpoint: '/integrations/salla/products',
        }),
        screen('salla-orders', 'إدارة الطلبات', 'Orders', '/integrations/salla/orders', 'ready', {
          permission: 'salla.integration.view',
          endpoint: '/integrations/salla/orders',
        }),
        screen(
          'salla-warehouses',
          'ربط المستودعات',
          'Warehouse mapping',
          '/integrations/salla/warehouses',
          'ready',
          { permission: 'salla.integration.view', endpoint: '/integrations/salla/mappings' },
        ),
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
        screen('purchase-invoice', 'فاتورة المشتريات', 'Purchase invoice', '/purchases/invoices', 'ready', {
          endpoint: '/purchase-invoices',
        }),
        screen(
          'purchase-return',
          'مردود المشتريات',
          'Purchase return',
          '/purchases/invoices/new?kind=purchase_return',
          'ready',
          { permission: 'purchase.invoice.create', endpoint: 'POST /purchase-invoices' },
        ),
      ],
    },
    {
      key: 'purchases-vouchers',
      labelAr: 'السندات',
      labelEn: 'Vouchers',
      items: [
        screen('purchase-credit-note', 'إشعار دائن', 'Credit note', '/purchases/notes/credit', 'ready', {
          permission: 'purchase.view',
          endpoint: '/purchases/adjustment-notes',
        }),
      ],
    },
    {
      key: 'purchases-notes',
      labelAr: 'الإشعارات',
      labelEn: 'Notes',
      items: [
        screen('pn-debit', 'إشعار مدين', 'Debit note', '/purchases/notes/debit', 'ready', {
          permission: 'purchase.view',
          endpoint: '/purchases/adjustment-notes',
        }),
        screen('pn-report', 'تقرير الإشعارات', 'Notes report', '/reports/purchase-notes', 'ready', {
          permission: 'reporting.view',
        }),
      ],
    },
    {
      key: 'purchases-reports',
      labelAr: 'التقارير',
      labelEn: 'Reports',
      items: [
        screen(
          'supplier-statement',
          'كشف مورد',
          'Supplier statement',
          '/sales/statements?kind=supplier',
          'ready',
          { permission: 'parties.view', endpoint: '/parties/{id}/statement' },
        ),
        screen(
          'purchase-invoices-report',
          'تقرير فواتير المشتريات',
          'Purchase invoices',
          '/reports/purchase-invoices',
          'ready',
          { permission: 'reporting.view', endpoint: '/reports/purchases' },
        ),
        screen(
          'purchase-returns-report',
          'تقرير مردود فواتير المشتريات',
          'Purchase returns',
          '/reports/purchase-returns',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen('net-purchases', 'صافي المشتريات', 'Net purchases', '/reports/net-purchases', 'ready', {
          permission: 'reporting.view',
        }),
        screen(
          'purchases-detail',
          'مشتريات تفصيلية',
          'Detailed purchases',
          '/reports/purchases-detail',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen(
          'purchases-items',
          'مشتريات الأصناف تجميعي',
          'Purchases by item',
          '/reports/purchases-by-item',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen(
          'supplier-balances',
          'أرصدة الموردين',
          'Supplier balances',
          '/reports/supplier-balances',
          'ready',
          { permission: 'reporting.view', endpoint: '/reports/party-balances' },
        ),
        screen(
          'supplier-settlements',
          'سداد الموردين',
          'Supplier settlements',
          '/reports/supplier-settlements',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen(
          'employee-purchases',
          'مشتريات موظف',
          'Purchases by employee',
          '/reports/purchases-by-employee',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen(
          'invoices-by-supplier',
          'الفواتير بحسب الموردين',
          'Invoices by supplier',
          '/reports/invoices-by-supplier',
          'ready',
          { permission: 'reporting.view' },
        ),
      ],
    },
    {
      key: 'purchases-other',
      labelAr: 'أخرى',
      labelEn: 'Other',
      items: [
        screen('supplier-card', 'بطاقة مورد', 'Supplier card', '/purchases/suppliers', 'ready', {
          permission: 'parties.view',
          endpoint: '/parties?kind=supplier',
        }),
      ],
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
        screen('pos', 'نقطة البيع', 'Point of sale', '/sales/pos', 'ready', {
          permission: 'sales.invoice.create',
          endpoint: 'POST /sales/invoices',
        }),
        screen('sales-invoice', 'فاتورة مبيعات', 'Sales invoice', '/sales/invoices', 'ready', {
          endpoint: '/sales/invoices',
        }),
        screen('cash-customer', '👤 عميل نقدي', 'Cash customer', '/sales/cash-customers', 'ready', {
          endpoint: '/sales/cash-customers',
        }),
        screen('sales-return', 'مردود المبيعات', 'Sales return', '/sales/returns', 'ready', {
          permission: 'sales.return.create',
          endpoint: 'POST /sales/invoices/{id}/return',
        }),
        screen('quotation', 'عرض سعر', 'Quotation', '/sales/quotations', 'ready', {
          permission: 'sales.view',
          endpoint: '/sales/quotations',
        }),
        screen(
          'contracting-return',
          'مرتجع مقاولات',
          'Contracting return',
          '/projects/contracting-return',
          'ready',
          { permission: 'projects.manage', endpoint: '/contracting/returns' },
        ),
      ],
    },
    {
      key: 'sales-vouchers',
      labelAr: 'السندات',
      labelEn: 'Vouchers',
      items: [
        screen('sales-debit-note', 'إشعار مدين', 'Debit note', '/sales/notes/debit', 'ready', {
          permission: 'sales.view',
          endpoint: '/sales/adjustment-notes',
        }),
      ],
    },
    {
      key: 'sales-notes',
      labelAr: 'الإشعارات',
      labelEn: 'Notes',
      items: [
        screen('sn-credit', 'إشعار دائن', 'Credit note', '/sales/notes/credit', 'ready', {
          permission: 'sales.view',
          endpoint: '/sales/adjustment-notes',
        }),
        screen('sn-report', 'تقرير الإشعارات', 'Notes report', '/reports/sales-notes', 'ready', {
          permission: 'reporting.view',
        }),
      ],
    },
    {
      key: 'sales-reports',
      labelAr: 'التقارير',
      labelEn: 'Reports',
      items: [
        screen(
          'sales-invoices-report',
          'تقرير فواتير المبيعات',
          'Sales invoices',
          '/reports/sales-invoices',
          'ready',
          { permission: 'reporting.view', endpoint: '/reports/sales' },
        ),
        screen(
          'sales-returns-report',
          'تقرير مردود فواتير المبيعات',
          'Sales returns',
          '/reports/sales-returns',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen('net-sales', 'صافي المبيعات', 'Net sales', '/reports/net-sales', 'ready', {
          permission: 'reporting.view',
        }),
        screen('sales-detail', 'مبيعات تفصيلية', 'Detailed sales', '/reports/sales-detail', 'ready', {
          permission: 'reporting.view',
        }),
        screen('sales-items', 'مبيعات الأصناف تجميعي', 'Sales by item', '/reports/sales-by-item', 'ready', {
          permission: 'reporting.view',
        }),
        screen(
          'sales-by-category',
          'مبيعات بحسب الفئة',
          'Sales by category',
          '/reports/sales-by-category',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen(
          'sales-invoice-profit',
          'أرباح الفواتير',
          'Invoice profit',
          '/reports/invoice-profit',
          'ready',
          { permission: 'reporting.view', endpoint: 'GET /reports/invoice-profit' },
        ),
        screen('item-profit', 'أرباح الأصناف', 'Item profit', '/reports/item-profit', 'ready', {
          permission: 'reporting.view',
        }),
        screen(
          'item-profit-detail',
          'تفاصيل أرباح الأصناف',
          'Item profit detail',
          '/reports/item-profit-detail',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen('reps-report', 'تقرير المندوبين', 'Sales reps', '/reports/sales-by-salesman', 'ready', {
          permission: 'reporting.view',
        }),
        screen('employee-sales', 'مبيعات موظف', 'Sales by employee', '/reports/sales-by-employee', 'ready', {
          permission: 'reporting.view',
        }),
        screen('customer-statement', 'كشف عميل', 'Customer statement', '/sales/statements', 'ready', {
          permission: 'parties.view',
          endpoint: '/parties/{id}/statement',
        }),
        screen(
          'customer-balances',
          'أرصدة العملاء',
          'Customer balances',
          '/reports/customer-balances',
          'ready',
          { permission: 'reporting.view', endpoint: '/reports/party-balances' },
        ),
        screen(
          'customer-settlements',
          'سداد العملاء',
          'Customer settlements',
          '/reports/customer-settlements',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen(
          'invoices-by-customer',
          'الفواتير بحسب العملاء',
          'Invoices by customer',
          '/reports/invoices-by-customer',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen('sales-movement', 'حركة المبيعات', 'Sales movement', '/reports/sales-by-day', 'ready', {
          permission: 'reporting.view',
        }),
        screen('sales-chart', 'تقرير بياني', 'Chart report', '/reports/monthly-sales', 'ready', {
          permission: 'reporting.view',
        }),
        screen(
          'contracting-invoices-report',
          'تقرير فواتير المقاولات',
          'Contracting invoices',
          '/reports/project-bills',
          'ready',
          { permission: 'reporting.view' },
        ),
      ],
    },
    {
      key: 'sales-pos-reports',
      labelAr: 'تقارير نقطة البيع',
      labelEn: 'POS reports',
      items: [
        screen('pos-sales', 'تقرير مبيعات POS', 'POS sales', '/reports/pos-sales', 'ready', {
          permission: 'reporting.view',
          endpoint: '/pos/reports/sales',
        }),
        screen(
          'pos-item-detail',
          'تفاصيل أصناف POS',
          'POS item detail',
          '/reports/pos-item-detail',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen(
          'pos-item-summary',
          'أصناف POS تجميعي',
          'POS item summary',
          '/reports/pos-item-summary',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen('pos-daily', 'تقرير المبيعات اليومية', 'Daily sales', '/reports/pos-daily', 'ready', {
          permission: 'reporting.view',
        }),
        screen('pos-group', 'تقرير مبيعات للمجموعة', 'Sales by group', '/reports/pos-by-category', 'ready', {
          permission: 'reporting.view',
        }),
      ],
    },
    {
      key: 'sales-other',
      labelAr: 'أخرى',
      labelEn: 'Other',
      items: [
        screen('customer-card', 'بطاقة عميل', 'Customer card', '/sales/customers', 'ready', {
          permission: 'parties.view',
          endpoint: '/parties?kind=customer',
        }),
        screen('rep-card', 'بطاقة مندوب', 'Sales rep card', '/sales/salesmen', 'ready', {
          permission: 'sales.view',
          endpoint: '/sales/salesmen',
        }),
        screen(
          'customer-portal-access',
          'وصول العملاء للبوابة',
          'Customer portal access',
          '/sales/portal-access',
          'ready',
          { permission: 'parties.view', endpoint: '/portal-access' },
        ),
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
        screen('departments', 'تعريف الإدارات', 'Departments', '/hrm/departments', 'ready', {
          endpoint: '/hrm/departments',
        }),
        screen('sections', 'تعريف الأقسام والوظائف', 'Jobs', '/hrm/jobs', 'ready', { endpoint: '/hrm/jobs' }),
        screen('employee', 'تعريف موظف', 'Employee', '/hrm/employees', 'ready', {
          endpoint: '/hrm/employees',
        }),
      ],
    },
    {
      key: 'hrm-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen('adjustments', 'الحوافز والجزاءات', 'Bonuses & deductions', '/hrm/adjustments', 'ready', {
          endpoint: '/hrm/adjustments',
        }),
        screen('payroll-run', 'إستحقاق وصرف الرواتب', 'Payroll run and payment', '/hrm/payroll', 'ready', {
          endpoint: '/hrm/payroll/runs',
        }),
      ],
    },
    {
      key: 'hrm-reports',
      labelAr: 'التقارير',
      labelEn: 'Reports',
      items: [
        screen(
          'salary-payments-report',
          'دفع الرواتب',
          'Salary payments',
          '/reports/payroll-payments',
          'ready',
          { permission: 'reporting.view' },
        ),
        screen('employee-account', 'حساب موظف', 'Employee account', '/reports/employee-account', 'ready', {
          permission: 'reporting.view',
        }),
        screen('user-logs', 'سجلات المستخدمين', 'User logs', '/settings/audit', 'ready', {
          permission: 'tenant.audit.view',
          endpoint: '/audit-log',
        }),
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
        screen(
          'marina-vessel',
          'بطاقات النماذج والمراكب والملاك',
          'Model, vessel and owner cards',
          '/marina/vessels',
          'ready',
          { endpoint: '/marina' },
        ),
      ],
    },
    {
      key: 'marina-manage',
      labelAr: 'إدارة',
      labelEn: 'Management',
      items: [
        screen('marina-prep', 'تحضير المراكب', 'Vessel preparation', '/marina/preparation', 'ready', {
          permission: 'marina.view',
          endpoint: '/marina/preparations',
        }),
        screen('marina-violations', 'المخالفات', 'Violations', '/marina/violations', 'ready', {
          endpoint: '/marina/violations',
        }),
        screen('marina-rota', 'خطة الدور', 'Rotation plan', '/marina/rota', 'ready', {
          permission: 'marina.view',
          endpoint: '/marina/operation-plans',
        }),
      ],
    },
    {
      key: 'marina-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen('marina-link', 'ربط الفواتير', 'Link invoices', '/marina/link-invoices', 'ready', {
          permission: 'marina.view',
          endpoint: '/marina/rental-invoices',
        }),
        screen(
          'marina-bookings',
          'الحجوزات والإضافات والفواتير',
          'Bookings, additions and invoices',
          '/marina/bookings',
          'ready',
          { endpoint: '/marina/bookings' },
        ),
        screen('marina-day-close', 'إغلاق اليومية', 'Day close', '/marina/day-close', 'ready', {
          permission: 'marina.view',
          endpoint: '/marina/day-close',
        }),
      ],
    },
    {
      key: 'marina-reports',
      labelAr: 'التقارير',
      labelEn: 'Reports',
      items: [
        screen('marina-day-closes', 'إغلاقات اليومية', 'Day closes', '/reports/cashier-shift', 'ready', {
          permission: 'reporting.view',
        }),
        screen(
          'marina-rental-invoices',
          'تقرير فواتير التأجير',
          'Rental invoices',
          '/reports/marina-rentals',
          'ready',
          { permission: 'reporting.view' },
        ),
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
        screen('boq-item', 'بطاقة بند', 'BOQ item card', '/s/projects/boq', 'api', {
          permission: 'projects.manage',
          endpoint: 'POST /projects/{id}/boq',
        }),
        screen('project-stages', 'مراحل مشروع', 'Project stages', '/s/projects/stages', 'api', {
          permission: 'projects.manage',
          endpoint: 'POST /projects/{id}/stages',
        }),
      ],
    },
    {
      key: 'projects-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen('customer-contract', 'عقد عميل', 'Customer contract', '/projects', 'ready', {
          endpoint: '/projects',
        }),
        screen(
          'contractor-contract',
          'عقد مقاول',
          'Contractor contract',
          '/projects/contractor-contract',
          'ready',
          { permission: 'projects.manage', endpoint: '/contracting/contracts' },
        ),
        screen('project-followup', 'متابعة', 'Follow-up', '/projects/followup', 'ready', {
          permission: 'projects.view',
          endpoint: '/projects/{id}',
        }),
        screen('project-offers', 'عروض', 'Offers', '/projects/offers', 'ready', {
          permission: 'projects.manage',
          endpoint: '/contracting/offers',
        }),
        screen(
          'contractor-payment',
          'سند دفع لمقاول',
          'Contractor payment',
          '/projects/contractor-payment',
          'ready',
          { permission: 'projects.contractor.pay', endpoint: 'POST /contracting/contracts/{id}/payments' },
        ),
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
        screen('company-card', 'بطاقة المنشأة', 'Company card', '/settings/company', 'ready', {
          permission: 'tenant.profile.view',
          endpoint: '/company-profile',
        }),
        screen('branch-card', 'بطاقة فرع', 'Branch card', '/settings/branches', 'ready', {
          permission: 'organization.branch.view',
          endpoint: '/branches',
        }),
        screen(
          'posting-profiles',
          'الربط المحاسبي',
          'Posting profiles',
          '/settings/posting-profiles',
          'ready',
          { permission: 'organization.postingprofile.view', endpoint: '/branch-posting-profiles' },
        ),
        screen(
          'zatca-settings',
          'إعدادات الربط مع هيئة الزكاة والضريبة',
          'ZATCA integration',
          '/settings/zatca',
          'ready',
          { permission: 'einvoice.view', endpoint: '/einvoice/credentials' },
        ),
      ],
    },
    {
      key: 'settings-admin',
      labelAr: 'إعدادات إدارية',
      labelEn: 'Administrative',
      items: [
        screen('backup', 'النسخ الإحتياطي', 'Backup', '/settings/backup', 'ready', {
          permission: 'settings.backup.manage',
          endpoint: '/settings/backups',
        }),
        screen('data-rotation', 'تدوير البيانات', 'Data rotation', '/settings/data-rotation', 'ready', {
          permission: 'settings.rotation.manage',
          endpoint: '/settings/data-rotation',
        }),
        screen('new-file', 'إنشاء ملف', 'New company file', '/settings/new-file', 'ready', {
          permission: 'settings.companyfile.create',
          endpoint: '/settings/company-files',
        }),
        screen('import-export', 'إستيراد وتصدير البيانات', 'Import / export', '/migration/runs', 'ready', {
          permission: 'migration.view',
          endpoint: '/migration/runs',
        }),
        screen(
          'invoice-maintenance',
          'صيانة الفواتير',
          'Invoice maintenance',
          '/settings/invoice-maintenance',
          'ready',
          { permission: 'settings.maintenance.manage', endpoint: '/settings/invoice-maintenance' },
        ),
        screen('offers', 'العروض', 'Offers', '/settings/offers', 'ready', {
          permission: 'sales.view',
          endpoint: '/sales/offers',
        }),
        screen('data-sync', 'مزامنة البيانات', 'Data sync', '/settings/sync', 'ready', {
          permission: 'compat.manage',
          endpoint: '/jobs/outbox',
        }),
        screen('restore', 'إستعادة البيانات', 'Restore', '/settings/restore', 'ready', {
          permission: 'settings.restore.manage',
          endpoint: '/settings/restores',
        }),
      ],
    },
    {
      key: 'settings-users',
      labelAr: 'إعدادات المستخدمين',
      labelEn: 'Users',
      items: [
        screen('user-card', 'بطاقة مستخدم', 'User card', '/settings/users', 'ready', {
          permission: 'tenant.users.manage',
          endpoint: '/memberships',
        }),
        screen('user-permissions', 'صلاحيات المستخدمين', 'User permissions', '/settings/roles', 'ready', {
          permission: 'tenant.roles.manage',
          endpoint: '/roles',
        }),
        screen(
          'change-password',
          'تغيير كلمة المرور',
          'Change password',
          '/settings/change-password',
          'ready',
          { endpoint: 'POST /auth/change-password' },
        ),
        screen('two-factor', 'التحقق بخطوتين', 'Two-factor authentication', '/settings/two-factor', 'ready', {
          endpoint: '/auth/mfa',
        }),
      ],
    },
    {
      key: 'settings-general',
      labelAr: 'إعدادات عامة',
      labelEn: 'General',
      items: [
        screen('general-settings', 'إعدادات عامة', 'General settings', '/settings/general', 'ready', {
          permission: 'tenant.settings.manage',
          endpoint: '/settings',
        }),
        screen('language', 'اللغة', 'Language', '/settings/language', 'ready', {
          endpoint: 'client-side preference (localStorage)',
        }),
        screen(
          'prep-device',
          'إعدادات جهاز التحضير',
          'Preparation device',
          '/s/settings/prep-device',
          'planned',
        ),
        screen(
          'salla-settings',
          'إعدادات ربط سلة',
          'Salla integration',
          '/integrations/salla/settings',
          'ready',
          { permission: 'salla.integration.view', endpoint: '/integrations/salla/settings' },
        ),
      ],
    },
    {
      key: 'settings-sync',
      labelAr: 'المزامنة',
      labelEn: 'Synchronisation',
      items: [
        screen('sync-invoices', 'مزامنة الفواتير', 'Invoice sync', '/settings/sync/invoices', 'ready', {
          permission: 'compat.manage',
          endpoint: '/compat/sync/documents?entity=invoices',
        }),
        screen('sync-journals', 'مزامنة القيود', 'Journal sync', '/settings/sync/journals', 'ready', {
          permission: 'compat.manage',
          endpoint: '/compat/sync/documents?entity=journals',
        }),
        screen('sync-vouchers', 'مزامنة السندات', 'Voucher sync', '/settings/sync/vouchers', 'ready', {
          permission: 'compat.manage',
          endpoint: '/compat/sync/documents?entity=vouchers',
        }),
        screen('sync-stock', 'مزامنة المخزون', 'Stock sync', '/settings/sync/stock', 'ready', {
          permission: 'compat.manage',
          endpoint: '/compat/sync/documents?entity=stock',
        }),
        screen('sync-manage', 'إدارة المزامنة', 'Sync management', '/settings/sync/manage', 'ready', {
          permission: 'compat.manage',
          endpoint: '/jobs/queues',
        }),
        screen('sync-prices', 'إعدادات الأسعار', 'Price settings', '/settings/price-lists', 'ready', {
          permission: 'organization.priceList.view',
          endpoint: '/price-lists',
        }),
        screen('sync-zatca', 'مزامنة الفواتير Zatca', 'ZATCA sync', '/settings/sync/zatca', 'ready', {
          permission: 'einvoice.view',
          endpoint: '/einvoice/submissions',
        }),
        screen(
          'android-devices',
          'أجهزة أندرويد المرتبطة',
          'Linked Android devices',
          '/settings/devices',
          'ready',
          { permission: 'compat.manage', endpoint: '/compat/devices' },
        ),
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
        screen('license', 'الترخيص', 'Licence', '/support/license', 'ready', {
          endpoint: '/billing/subscription',
        }),
        screen('about', 'عن البرنامج', 'About', '/support/about', 'ready'),
        screen('update', 'تحديث البرنامج', 'Update', '/support/about#updates', 'ready'),
        screen(
          'report-designer',
          'فتح المصمم لتصميم التقارير',
          'Report designer',
          '/support/report-designer',
          'ready',
          { permission: 'reporting.layout.manage', endpoint: '/reports/layouts' },
        ),
        screen('help', '🆘 إطلب المساعدة', 'Request help', '/support/help', 'ready'),
      ],
    },
  ],
};

// ---------------------------------------------------------------------------
// 2. الخزينة
//
// `frmSandQ` / `frmSandD` (the documents), `frmTreasury` / `frmBanks` (the masters) and
// `frmRptKhzna` (the statement) are one module in the desktop too; the voucher screens
// used to sit under المحاسبة › العمليات because the treasury module had no home. They
// moved here — the routes did not change, and no endpoint was touched.
// ---------------------------------------------------------------------------
const treasury: ModuleNode = {
  key: 'treasury',
  icon: '🏦',
  labelAr: 'الخزينة',
  labelEn: 'Treasury',
  href: '/treasury/vouchers',
  permission: 'treasury.view',
  groups: [
    {
      key: 'treasury-defs',
      labelAr: 'تعاريف',
      labelEn: 'Definitions',
      items: [
        screen('safe-card', '🏦 تعريف الخزينة', 'Safes', '/treasury/safes', 'ready', {
          permission: 'organization.cashlocation.view',
          endpoint: '/cash-locations?filter[kind]=safe',
        }),
        screen('bank-def', '🏦 تعريف البنوك', 'Banks', '/treasury/banks', 'ready', {
          permission: 'organization.cashlocation.view',
          endpoint: '/cash-locations?filter[kind]=bank',
        }),
        screen('expense-card', '📒 بطاقة حساب المصاريف', 'Expense card', '/accounting/expenses', 'ready', {
          permission: 'treasury.view',
          endpoint: '/expense-types',
        }),
      ],
    },
    {
      key: 'treasury-ops',
      labelAr: 'العمليات',
      labelEn: 'Operations',
      items: [
        screen('receipt-voucher', '📄 سند قبض', 'Receipt voucher', '/treasury/vouchers?kind=receipt', 'ready', {
          permission: 'treasury.view',
          endpoint: '/vouchers',
        }),
        screen('payment-voucher', '📄 سند صرف', 'Payment voucher', '/treasury/vouchers?kind=payment', 'ready', {
          permission: 'treasury.view',
          endpoint: '/vouchers',
        }),
        screen('day-close', '📊 إغلاق اليومية', 'Day close', '/treasury/day-close', 'ready', {
          permission: 'treasury.view',
          endpoint: '/shift-closes/day-closes',
        }),
      ],
    },
    {
      key: 'treasury-reports',
      labelAr: 'التقارير',
      labelEn: 'Reports',
      items: [
        screen('safe-movement', '🏦 حركة الصندوق', 'Safe movement', '/treasury/movements', 'ready', {
          permission: 'treasury.view',
          endpoint: '/cash-locations/:id/movements',
        }),
      ],
    },
  ],
};

export const modules: ModuleNode[] = [
  accounting,
  treasury,
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
export const allScreens: Array<
  ScreenItem & { moduleKey: string; moduleLabelAr: string; groupLabelAr: string }
> = modules.flatMap((module) =>
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
