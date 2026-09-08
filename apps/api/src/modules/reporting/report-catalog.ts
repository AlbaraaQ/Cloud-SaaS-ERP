import { sql, type SQL } from 'drizzle-orm';

/**
 * Report catalog — one definition per screen in the desktop product's report menus.
 *
 * A definition carries everything a client needs to render the report without knowing
 * anything about it: which filters to offer, which columns to draw, how to format each
 * column and which columns to total. The admin app therefore ships **one** report screen
 * instead of forty hand-written ones, and adding a report here makes it appear there.
 */
export type ReportParamKind = 'date' | 'branch' | 'warehouse' | 'party' | 'item' | 'category' | 'salesman' | 'costCenter' | 'select';

export type ReportParam = {
  name: string;
  labelAr: string;
  kind: ReportParamKind;
  options?: Array<{ value: string; labelAr: string }>;
};

export type ReportColumnType = 'text' | 'money' | 'qty' | 'int' | 'date' | 'percent';

export type ReportColumn = { key: string; labelAr: string; type: ReportColumnType };

export type ReportGroup = 'sales' | 'purchases' | 'inventory' | 'accounting' | 'pos' | 'hrm' | 'projects' | 'marina';

export type ReportFilters = {
  from?: string;
  to?: string;
  branchId?: string;
  warehouseId?: string;
  partyId?: string;
  itemId?: string;
  categoryId?: string;
  salesmanId?: string;
  costCenterId?: string;
  status?: string;
  kind?: string;
};

export type ReportDefinition = {
  key: string;
  titleAr: string;
  group: ReportGroup;
  /** One line explaining what the numbers mean — shown under the report title. */
  hintAr?: string;
  params: ReportParam[];
  columns: ReportColumn[];
  /** Column keys that get a grand total in the footer. */
  totals?: string[];
  chart?: 'bar' | 'line';
  build: (tenantId: string, filters: ReportFilters) => SQL;
};

const PERIOD: ReportParam[] = [
  { name: 'from', labelAr: 'من تاريخ', kind: 'date' },
  { name: 'to', labelAr: 'إلى تاريخ', kind: 'date' },
];
const BRANCH: ReportParam = { name: 'branchId', labelAr: 'الفرع', kind: 'branch' };
const WAREHOUSE: ReportParam = { name: 'warehouseId', labelAr: 'المستودع', kind: 'warehouse' };
const PARTY: ReportParam = { name: 'partyId', labelAr: 'الطرف', kind: 'party' };
const ITEM: ReportParam = { name: 'itemId', labelAr: 'الصنف', kind: 'item' };
const CATEGORY: ReportParam = { name: 'categoryId', labelAr: 'المجموعة', kind: 'category' };
const SALESMAN: ReportParam = { name: 'salesmanId', labelAr: 'المندوب', kind: 'salesman' };
const COST_CENTER: ReportParam = { name: 'costCenterId', labelAr: 'مركز التكلفة', kind: 'costCenter' };

const text = (key: string, labelAr: string): ReportColumn => ({ key, labelAr, type: 'text' });
const money = (key: string, labelAr: string): ReportColumn => ({ key, labelAr, type: 'money' });
const qty = (key: string, labelAr: string): ReportColumn => ({ key, labelAr, type: 'qty' });
const int = (key: string, labelAr: string): ReportColumn => ({ key, labelAr, type: 'int' });
const date = (key: string, labelAr: string): ReportColumn => ({ key, labelAr, type: 'date' });
const percent = (key: string, labelAr: string): ReportColumn => ({ key, labelAr, type: 'percent' });

/** `true` when the filter is absent, so every predicate can be `AND`-ed unconditionally. */
const all = sql`true`;
const onDate = (column: SQL, from?: string, to?: string): SQL =>
  sql`${from ? sql`${column} >= ${from}::date` : all} AND ${to ? sql`${column} <= ${to}::date` : all}`;
const eqIf = (column: SQL, value?: string): SQL => (value ? sql`${column} = ${value}::uuid` : all);

// Party display name, tolerant of the cash-customer case where no party row exists.
const partyName = sql`coalesce(party.name, '—')`;
const branchName = sql`coalesce(branch.name_ar, '—')`;
const itemName = sql`coalesce(item.name_ar, '—')`;

const salesScope = (tenantId: string, f: ReportFilters, kind: string): SQL => sql`
  si.tenant_id = ${tenantId}
  AND si.kind = ${kind}
  AND si.status = 'posted'
  AND ${onDate(sql`si.posted_at::date`, f.from, f.to)}
  AND ${eqIf(sql`si.branch_id`, f.branchId)}
  AND ${eqIf(sql`si.party_id`, f.partyId)}
  AND ${eqIf(sql`si.salesman_id`, f.salesmanId)}
`;

const purchaseScope = (tenantId: string, f: ReportFilters, kind: string): SQL => sql`
  pi.tenant_id = ${tenantId}
  AND pi.kind = ${kind}
  AND pi.status = 'posted'
  AND ${onDate(sql`pi.posted_at::date`, f.from, f.to)}
  AND ${eqIf(sql`pi.branch_id`, f.branchId)}
  AND ${eqIf(sql`pi.party_id`, f.partyId)}
`;

const definitions: ReportDefinition[] = [
  // ---------------------------------------------------------------- sales
  {
    key: 'sales-invoices',
    titleAr: 'تقرير فواتير المبيعات',
    group: 'sales',
    hintAr: 'الفواتير المرحّلة فقط — المسودات لا تدخل أي تقرير مالي.',
    params: [...PERIOD, BRANCH, PARTY, SALESMAN],
    columns: [text('number', 'الرقم'), date('day', 'التاريخ'), text('party', 'العميل'), text('branch', 'الفرع'), money('subtotal', 'الصافي قبل الضريبة'), money('tax_total', 'الضريبة'), money('total', 'الإجمالي'), money('paid_total', 'المدفوع'), money('due', 'المتبقي'), text('payment_status', 'حالة السداد')],
    totals: ['subtotal', 'tax_total', 'total', 'paid_total', 'due'],
    build: (tenantId, f) => sql`
      SELECT si.number, si.posted_at::date AS day, ${partyName} AS party, ${branchName} AS branch,
             si.subtotal::text, si.tax_total::text, si.total::text, si.paid_total::text,
             (si.total - si.paid_total)::text AS due, si.payment_status
      FROM sales_invoices si
      LEFT JOIN parties party ON party.id = si.party_id
      LEFT JOIN branches branch ON branch.id = si.branch_id
      WHERE ${salesScope(tenantId, f, 'sale')}
      ORDER BY si.posted_at DESC LIMIT 1000`,
  },
  {
    key: 'sales-returns',
    titleAr: 'تقرير مردود المبيعات',
    group: 'sales',
    params: [...PERIOD, BRANCH, PARTY],
    columns: [text('number', 'الرقم'), date('day', 'التاريخ'), text('party', 'العميل'), text('branch', 'الفرع'), money('subtotal', 'الصافي'), money('tax_total', 'الضريبة'), money('total', 'الإجمالي')],
    totals: ['subtotal', 'tax_total', 'total'],
    build: (tenantId, f) => sql`
      SELECT si.number, si.posted_at::date AS day, ${partyName} AS party, ${branchName} AS branch,
             si.subtotal::text, si.tax_total::text, si.total::text
      FROM sales_invoices si
      LEFT JOIN parties party ON party.id = si.party_id
      LEFT JOIN branches branch ON branch.id = si.branch_id
      WHERE ${salesScope(tenantId, f, 'sale_return')}
      ORDER BY si.posted_at DESC LIMIT 1000`,
  },
  {
    key: 'net-sales',
    titleAr: 'صافي المبيعات',
    group: 'sales',
    hintAr: 'المبيعات ناقص المردودات لكل يوم.',
    params: [...PERIOD, BRANCH],
    columns: [date('day', 'اليوم'), money('sales', 'المبيعات'), money('returns', 'المردودات'), money('net', 'الصافي')],
    totals: ['sales', 'returns', 'net'],
    chart: 'bar',
    build: (tenantId, f) => sql`
      SELECT si.posted_at::date AS day,
             sum(CASE WHEN si.kind = 'sale' THEN si.total ELSE 0 END)::text AS sales,
             sum(CASE WHEN si.kind = 'sale_return' THEN si.total ELSE 0 END)::text AS returns,
             (sum(CASE WHEN si.kind = 'sale' THEN si.total ELSE -si.total END))::text AS net
      FROM sales_invoices si
      WHERE si.tenant_id = ${tenantId} AND si.status = 'posted' AND si.kind IN ('sale', 'sale_return')
        AND ${onDate(sql`si.posted_at::date`, f.from, f.to)} AND ${eqIf(sql`si.branch_id`, f.branchId)}
      GROUP BY si.posted_at::date ORDER BY day`,
  },
  {
    key: 'sales-detail',
    titleAr: 'مبيعات تفصيلية',
    group: 'sales',
    hintAr: 'سطر لكل صنف في كل فاتورة.',
    params: [...PERIOD, BRANCH, PARTY, ITEM, CATEGORY],
    columns: [text('number', 'الفاتورة'), date('day', 'التاريخ'), text('party', 'العميل'), text('item', 'الصنف'), qty('quantity', 'الكمية'), money('unit_price', 'السعر'), money('discount_amount', 'الخصم'), money('net', 'الصافي'), money('tax', 'الضريبة'), money('total', 'الإجمالي')],
    totals: ['quantity', 'discount_amount', 'net', 'tax', 'total'],
    build: (tenantId, f) => sql`
      SELECT si.number, si.posted_at::date AS day, ${partyName} AS party, ${itemName} AS item,
             line.quantity::text, line.unit_price::text, line.discount_amount::text,
             line.net::text, line.tax::text, line.total::text
      FROM sales_invoice_lines line
      JOIN sales_invoices si ON si.id = line.invoice_id
      LEFT JOIN parties party ON party.id = si.party_id
      LEFT JOIN items item ON item.id = line.item_id
      WHERE ${salesScope(tenantId, f, 'sale')}
        AND ${eqIf(sql`line.item_id`, f.itemId)} AND ${eqIf(sql`item.category_id`, f.categoryId)}
      ORDER BY si.posted_at DESC, line.line_no LIMIT 2000`,
  },
  {
    key: 'sales-by-item',
    titleAr: 'مبيعات الأصناف تجميعي',
    group: 'sales',
    params: [...PERIOD, BRANCH, CATEGORY],
    columns: [text('sku', 'الرمز'), text('item', 'الصنف'), qty('quantity', 'الكمية'), money('net', 'الصافي'), money('tax', 'الضريبة'), money('total', 'الإجمالي'), money('cost_total', 'التكلفة'), money('profit', 'الربح')],
    totals: ['quantity', 'net', 'tax', 'total', 'cost_total', 'profit'],
    build: (tenantId, f) => sql`
      SELECT coalesce(item.sku, '—') AS sku, ${itemName} AS item,
             sum(line.quantity)::text AS quantity, sum(line.net)::text AS net, sum(line.tax)::text AS tax,
             sum(line.total)::text AS total, sum(line.cost_total)::text AS cost_total,
             (sum(line.net) - sum(line.cost_total))::text AS profit
      FROM sales_invoice_lines line
      JOIN sales_invoices si ON si.id = line.invoice_id
      LEFT JOIN items item ON item.id = line.item_id
      WHERE ${salesScope(tenantId, f, 'sale')} AND ${eqIf(sql`item.category_id`, f.categoryId)}
      GROUP BY item.sku, item.name_ar ORDER BY sum(line.total) DESC LIMIT 500`,
  },
  {
    key: 'sales-by-category',
    titleAr: 'مبيعات بحسب الفئة',
    group: 'sales',
    params: [...PERIOD, BRANCH],
    columns: [text('category', 'الفئة'), qty('quantity', 'الكمية'), money('net', 'الصافي'), money('total', 'الإجمالي'), money('profit', 'الربح')],
    totals: ['quantity', 'net', 'total', 'profit'],
    chart: 'bar',
    build: (tenantId, f) => sql`
      SELECT coalesce(cat.name_ar, '—') AS category, sum(line.quantity)::text AS quantity,
             sum(line.net)::text AS net, sum(line.total)::text AS total,
             (sum(line.net) - sum(line.cost_total))::text AS profit
      FROM sales_invoice_lines line
      JOIN sales_invoices si ON si.id = line.invoice_id
      LEFT JOIN items item ON item.id = line.item_id
      LEFT JOIN item_categories cat ON cat.id = item.category_id
      WHERE ${salesScope(tenantId, f, 'sale')}
      GROUP BY cat.name_ar ORDER BY sum(line.total) DESC`,
  },
  {
    key: 'invoice-profit',
    titleAr: 'أرباح الفواتير',
    group: 'sales',
    hintAr: 'التكلفة هي ما قوّم به المخزون الحركة الخارجة وقت الترحيل.',
    params: [...PERIOD, BRANCH, PARTY],
    columns: [text('number', 'الفاتورة'), date('day', 'التاريخ'), text('party', 'العميل'), money('subtotal', 'الصافي'), money('cost_total', 'التكلفة'), money('profit', 'الربح'), percent('margin', 'هامش الربح')],
    totals: ['subtotal', 'cost_total', 'profit'],
    build: (tenantId, f) => sql`
      SELECT si.number, si.posted_at::date AS day, ${partyName} AS party, si.subtotal::text,
             si.cost_total::text, (si.subtotal - si.cost_total)::text AS profit,
             CASE WHEN si.subtotal = 0 THEN '0' ELSE round((si.subtotal - si.cost_total) / si.subtotal * 100, 2)::text END AS margin
      FROM sales_invoices si
      LEFT JOIN parties party ON party.id = si.party_id
      WHERE ${salesScope(tenantId, f, 'sale')}
      ORDER BY si.posted_at DESC LIMIT 1000`,
  },
  {
    key: 'item-profit',
    titleAr: 'أرباح الأصناف',
    group: 'sales',
    params: [...PERIOD, BRANCH, CATEGORY],
    columns: [text('sku', 'الرمز'), text('item', 'الصنف'), qty('quantity', 'الكمية'), money('revenue', 'الإيراد'), money('cost_total', 'التكلفة'), money('profit', 'الربح'), percent('margin', 'الهامش')],
    totals: ['quantity', 'revenue', 'cost_total', 'profit'],
    build: (tenantId, f) => sql`
      SELECT coalesce(item.sku, '—') AS sku, ${itemName} AS item, sum(line.quantity)::text AS quantity,
             sum(line.net)::text AS revenue, sum(line.cost_total)::text AS cost_total,
             (sum(line.net) - sum(line.cost_total))::text AS profit,
             CASE WHEN sum(line.net) = 0 THEN '0' ELSE round((sum(line.net) - sum(line.cost_total)) / sum(line.net) * 100, 2)::text END AS margin
      FROM sales_invoice_lines line
      JOIN sales_invoices si ON si.id = line.invoice_id
      LEFT JOIN items item ON item.id = line.item_id
      WHERE ${salesScope(tenantId, f, 'sale')} AND ${eqIf(sql`item.category_id`, f.categoryId)}
      GROUP BY item.sku, item.name_ar ORDER BY (sum(line.net) - sum(line.cost_total)) DESC LIMIT 500`,
  },
  {
    key: 'item-profit-detail',
    titleAr: 'تفاصيل أرباح الأصناف',
    group: 'sales',
    params: [...PERIOD, BRANCH, ITEM],
    columns: [date('day', 'التاريخ'), text('number', 'الفاتورة'), text('item', 'الصنف'), qty('quantity', 'الكمية'), money('net', 'الإيراد'), money('cost_total', 'التكلفة'), money('profit', 'الربح')],
    totals: ['quantity', 'net', 'cost_total', 'profit'],
    build: (tenantId, f) => sql`
      SELECT si.posted_at::date AS day, si.number, ${itemName} AS item, line.quantity::text,
             line.net::text, line.cost_total::text, (line.net - line.cost_total)::text AS profit
      FROM sales_invoice_lines line
      JOIN sales_invoices si ON si.id = line.invoice_id
      LEFT JOIN items item ON item.id = line.item_id
      WHERE ${salesScope(tenantId, f, 'sale')} AND ${eqIf(sql`line.item_id`, f.itemId)}
      ORDER BY si.posted_at DESC LIMIT 2000`,
  },
  {
    key: 'sales-by-salesman',
    titleAr: 'تقرير المندوبين',
    group: 'sales',
    params: [...PERIOD, BRANCH, SALESMAN],
    columns: [text('salesman', 'المندوب'), int('invoices', 'عدد الفواتير'), money('total', 'الإجمالي'), money('profit', 'الربح')],
    totals: ['invoices', 'total', 'profit'],
    build: (tenantId, f) => sql`
      SELECT coalesce(sm.name, 'بدون مندوب') AS salesman, count(*)::text AS invoices,
             sum(si.total)::text AS total, sum(si.subtotal - si.cost_total)::text AS profit
      FROM sales_invoices si
      LEFT JOIN salesmen sm ON sm.id = si.salesman_id
      WHERE ${salesScope(tenantId, f, 'sale')}
      GROUP BY sm.name ORDER BY sum(si.total) DESC`,
  },
  {
    key: 'invoices-by-customer',
    titleAr: 'الفواتير بحسب العملاء',
    group: 'sales',
    params: [...PERIOD, BRANCH],
    columns: [text('party', 'العميل'), int('invoices', 'عدد الفواتير'), money('total', 'الإجمالي'), money('paid_total', 'المدفوع'), money('due', 'المتبقي')],
    totals: ['invoices', 'total', 'paid_total', 'due'],
    build: (tenantId, f) => sql`
      SELECT ${partyName} AS party, count(*)::text AS invoices, sum(si.total)::text AS total,
             sum(si.paid_total)::text AS paid_total, sum(si.total - si.paid_total)::text AS due
      FROM sales_invoices si
      LEFT JOIN parties party ON party.id = si.party_id
      WHERE ${salesScope(tenantId, f, 'sale')}
      GROUP BY party.name ORDER BY sum(si.total) DESC`,
  },
  {
    key: 'customer-balances',
    titleAr: 'أرصدة العملاء',
    group: 'sales',
    hintAr: 'الرصيد من القيود المرحّلة: مدين ناقص دائن على حركة كل عميل.',
    params: [],
    columns: [text('code', 'الرمز'), text('party', 'العميل'), money('debit', 'مدين'), money('credit', 'دائن'), money('balance', 'الرصيد')],
    totals: ['debit', 'credit', 'balance'],
    build: (tenantId) => sql`
      SELECT party.code, party.name AS party, sum(jel.debit)::text AS debit, sum(jel.credit)::text AS credit,
             (sum(jel.debit) - sum(jel.credit))::text AS balance
      FROM journal_entry_lines jel
      JOIN journal_entries je ON je.id = jel.entry_id
      JOIN parties party ON party.id = jel.party_id
      WHERE jel.tenant_id = ${tenantId} AND je.status = 'posted' AND party.kind IN ('customer', 'both')
      GROUP BY party.code, party.name
      HAVING sum(jel.debit) <> sum(jel.credit) ORDER BY party.code`,
  },
  {
    key: 'customer-settlements',
    titleAr: 'سداد العملاء',
    group: 'sales',
    hintAr: 'كل ما قُبض من العملاء: دفعات الفواتير وسندات القبض المرحّلة.',
    params: [...PERIOD, PARTY],
    columns: [date('day', 'التاريخ'), text('party', 'العميل'), text('source', 'المصدر'), text('method', 'الطريقة'), money('amount', 'المبلغ')],
    totals: ['amount'],
    build: (tenantId, f) => sql`
      SELECT day, party, source, method, amount::text FROM (
        SELECT ip.created_at::date AS day, ${partyName} AS party, 'دفعة فاتورة' AS source, ip.method, ip.amount
        FROM invoice_payments ip
        JOIN sales_invoices si ON si.id = ip.invoice_id
        LEFT JOIN parties party ON party.id = si.party_id
        WHERE ip.tenant_id = ${tenantId} AND ${onDate(sql`ip.created_at::date`, f.from, f.to)} AND ${eqIf(sql`si.party_id`, f.partyId)}
        UNION ALL
        SELECT v.date AS day, coalesce(vp.name, '—') AS party, 'سند قبض' AS source, v.method, v.amount
        FROM vouchers v
        LEFT JOIN parties vp ON vp.id = v.party_id
        WHERE v.tenant_id = ${tenantId} AND v.kind = 'receipt' AND v.status = 'posted'
          AND ${onDate(sql`v.date`, f.from, f.to)} AND ${eqIf(sql`v.party_id`, f.partyId)}
      ) settlements ORDER BY day DESC LIMIT 1000`,
  },
  {
    key: 'sales-by-day',
    titleAr: 'حركة المبيعات اليومية',
    group: 'sales',
    params: [...PERIOD, BRANCH],
    columns: [date('day', 'اليوم'), int('invoices', 'عدد الفواتير'), money('total', 'الإجمالي')],
    totals: ['invoices', 'total'],
    chart: 'bar',
    build: (tenantId, f) => sql`
      SELECT si.posted_at::date AS day, count(*)::text AS invoices, sum(si.total)::text AS total
      FROM sales_invoices si WHERE ${salesScope(tenantId, f, 'sale')}
      GROUP BY si.posted_at::date ORDER BY day`,
  },
  {
    key: 'monthly-sales',
    titleAr: 'المبيعات الشهرية',
    group: 'sales',
    params: [...PERIOD, BRANCH],
    columns: [date('month', 'الشهر'), int('invoices', 'عدد الفواتير'), money('total', 'الإجمالي')],
    totals: ['invoices', 'total'],
    chart: 'bar',
    build: (tenantId, f) => sql`
      SELECT date_trunc('month', si.posted_at)::date AS month, count(*)::text AS invoices, sum(si.total)::text AS total
      FROM sales_invoices si WHERE ${salesScope(tenantId, f, 'sale')}
      GROUP BY date_trunc('month', si.posted_at) ORDER BY month`,
  },
  {
    key: 'sales-by-payment',
    titleAr: 'المبيعات بحسب حالة السداد',
    group: 'sales',
    params: [...PERIOD, BRANCH],
    columns: [text('payment_status', 'حالة السداد'), int('count', 'العدد'), money('total', 'الإجمالي')],
    totals: ['count', 'total'],
    build: (tenantId, f) => sql`
      SELECT si.payment_status, count(*)::text AS count, sum(si.total)::text AS total
      FROM sales_invoices si WHERE ${salesScope(tenantId, f, 'sale')}
      GROUP BY si.payment_status ORDER BY si.payment_status`,
  },

  // ------------------------------------------------------------ purchases
  {
    key: 'purchase-invoices',
    titleAr: 'تقرير فواتير المشتريات',
    group: 'purchases',
    params: [...PERIOD, BRANCH, PARTY],
    columns: [text('number', 'الرقم'), date('day', 'التاريخ'), text('party', 'المورد'), text('branch', 'الفرع'), money('subtotal', 'الصافي'), money('tax_total', 'الضريبة'), money('additional_cost_total', 'مصاريف'), money('total', 'الإجمالي'), money('due', 'المتبقي')],
    totals: ['subtotal', 'tax_total', 'additional_cost_total', 'total', 'due'],
    build: (tenantId, f) => sql`
      SELECT pi.number, pi.posted_at::date AS day, ${partyName} AS party, ${branchName} AS branch,
             pi.subtotal::text, pi.tax_total::text, pi.additional_cost_total::text, pi.total::text,
             (pi.total - pi.paid_total)::text AS due
      FROM purchase_invoices pi
      LEFT JOIN parties party ON party.id = pi.party_id
      LEFT JOIN branches branch ON branch.id = pi.branch_id
      WHERE ${purchaseScope(tenantId, f, 'purchase')}
      ORDER BY pi.posted_at DESC LIMIT 1000`,
  },
  {
    key: 'purchase-returns',
    titleAr: 'تقرير مردود المشتريات',
    group: 'purchases',
    params: [...PERIOD, BRANCH, PARTY],
    columns: [text('number', 'الرقم'), date('day', 'التاريخ'), text('party', 'المورد'), money('subtotal', 'الصافي'), money('tax_total', 'الضريبة'), money('total', 'الإجمالي')],
    totals: ['subtotal', 'tax_total', 'total'],
    build: (tenantId, f) => sql`
      SELECT pi.number, pi.posted_at::date AS day, ${partyName} AS party,
             pi.subtotal::text, pi.tax_total::text, pi.total::text
      FROM purchase_invoices pi
      LEFT JOIN parties party ON party.id = pi.party_id
      WHERE ${purchaseScope(tenantId, f, 'purchase_return')}
      ORDER BY pi.posted_at DESC LIMIT 1000`,
  },
  {
    key: 'net-purchases',
    titleAr: 'صافي المشتريات',
    group: 'purchases',
    params: [...PERIOD, BRANCH],
    columns: [date('day', 'اليوم'), money('purchases', 'المشتريات'), money('returns', 'المردودات'), money('net', 'الصافي')],
    totals: ['purchases', 'returns', 'net'],
    chart: 'bar',
    build: (tenantId, f) => sql`
      SELECT pi.posted_at::date AS day,
             sum(CASE WHEN pi.kind = 'purchase' THEN pi.total ELSE 0 END)::text AS purchases,
             sum(CASE WHEN pi.kind = 'purchase_return' THEN pi.total ELSE 0 END)::text AS returns,
             sum(CASE WHEN pi.kind = 'purchase' THEN pi.total ELSE -pi.total END)::text AS net
      FROM purchase_invoices pi
      WHERE pi.tenant_id = ${tenantId} AND pi.status = 'posted' AND pi.kind IN ('purchase', 'purchase_return')
        AND ${onDate(sql`pi.posted_at::date`, f.from, f.to)} AND ${eqIf(sql`pi.branch_id`, f.branchId)}
      GROUP BY pi.posted_at::date ORDER BY day`,
  },
  {
    key: 'purchases-detail',
    titleAr: 'مشتريات تفصيلية',
    group: 'purchases',
    params: [...PERIOD, BRANCH, PARTY, ITEM],
    columns: [text('number', 'الفاتورة'), date('day', 'التاريخ'), text('party', 'المورد'), text('item', 'الصنف'), qty('quantity', 'الكمية'), money('unit_price', 'السعر'), money('net', 'الصافي'), money('allocated_cost', 'مصاريف موزعة'), money('landed_total', 'التكلفة النهائية')],
    totals: ['quantity', 'net', 'allocated_cost', 'landed_total'],
    build: (tenantId, f) => sql`
      SELECT pi.number, pi.posted_at::date AS day, ${partyName} AS party, ${itemName} AS item,
             line.quantity::text, line.unit_price::text, line.net::text,
             line.allocated_cost::text, line.landed_total::text
      FROM purchase_invoice_lines line
      JOIN purchase_invoices pi ON pi.id = line.invoice_id
      LEFT JOIN parties party ON party.id = pi.party_id
      LEFT JOIN items item ON item.id = line.item_id
      WHERE ${purchaseScope(tenantId, f, 'purchase')} AND ${eqIf(sql`line.item_id`, f.itemId)}
      ORDER BY pi.posted_at DESC, line.line_no LIMIT 2000`,
  },
  {
    key: 'purchases-by-item',
    titleAr: 'مشتريات الأصناف تجميعي',
    group: 'purchases',
    params: [...PERIOD, BRANCH, CATEGORY],
    columns: [text('sku', 'الرمز'), text('item', 'الصنف'), qty('quantity', 'الكمية'), money('net', 'الصافي'), money('landed_total', 'التكلفة النهائية'), money('avg_cost', 'متوسط التكلفة')],
    totals: ['quantity', 'net', 'landed_total'],
    build: (tenantId, f) => sql`
      SELECT coalesce(item.sku, '—') AS sku, ${itemName} AS item, sum(line.quantity)::text AS quantity,
             sum(line.net)::text AS net, sum(line.landed_total)::text AS landed_total,
             CASE WHEN sum(line.quantity) = 0 THEN '0' ELSE round(sum(line.landed_total) / sum(line.quantity), 4)::text END AS avg_cost
      FROM purchase_invoice_lines line
      JOIN purchase_invoices pi ON pi.id = line.invoice_id
      LEFT JOIN items item ON item.id = line.item_id
      WHERE ${purchaseScope(tenantId, f, 'purchase')} AND ${eqIf(sql`item.category_id`, f.categoryId)}
      GROUP BY item.sku, item.name_ar ORDER BY sum(line.landed_total) DESC LIMIT 500`,
  },
  {
    key: 'invoices-by-supplier',
    titleAr: 'الفواتير بحسب الموردين',
    group: 'purchases',
    params: [...PERIOD, BRANCH],
    columns: [text('party', 'المورد'), int('invoices', 'عدد الفواتير'), money('total', 'الإجمالي'), money('paid_total', 'المسدد'), money('due', 'المتبقي')],
    totals: ['invoices', 'total', 'paid_total', 'due'],
    build: (tenantId, f) => sql`
      SELECT ${partyName} AS party, count(*)::text AS invoices, sum(pi.total)::text AS total,
             sum(pi.paid_total)::text AS paid_total, sum(pi.total - pi.paid_total)::text AS due
      FROM purchase_invoices pi
      LEFT JOIN parties party ON party.id = pi.party_id
      WHERE ${purchaseScope(tenantId, f, 'purchase')}
      GROUP BY party.name ORDER BY sum(pi.total) DESC`,
  },
  {
    key: 'supplier-balances',
    titleAr: 'أرصدة الموردين',
    group: 'purchases',
    params: [],
    columns: [text('code', 'الرمز'), text('party', 'المورد'), money('debit', 'مدين'), money('credit', 'دائن'), money('balance', 'الرصيد')],
    totals: ['debit', 'credit', 'balance'],
    build: (tenantId) => sql`
      SELECT party.code, party.name AS party, sum(jel.debit)::text AS debit, sum(jel.credit)::text AS credit,
             (sum(jel.credit) - sum(jel.debit))::text AS balance
      FROM journal_entry_lines jel
      JOIN journal_entries je ON je.id = jel.entry_id
      JOIN parties party ON party.id = jel.party_id
      WHERE jel.tenant_id = ${tenantId} AND je.status = 'posted' AND party.kind IN ('supplier', 'both')
      GROUP BY party.code, party.name
      HAVING sum(jel.debit) <> sum(jel.credit) ORDER BY party.code`,
  },
  {
    key: 'supplier-settlements',
    titleAr: 'سداد الموردين',
    group: 'purchases',
    params: [...PERIOD, PARTY],
    columns: [date('day', 'التاريخ'), text('party', 'المورد'), text('number', 'السند'), text('method', 'الطريقة'), money('amount', 'المبلغ')],
    totals: ['amount'],
    build: (tenantId, f) => sql`
      SELECT v.date AS day, coalesce(party.name, '—') AS party, coalesce(v.number, '—') AS number, v.method, v.amount::text
      FROM vouchers v
      LEFT JOIN parties party ON party.id = v.party_id
      WHERE v.tenant_id = ${tenantId} AND v.kind = 'payment' AND v.status = 'posted'
        AND ${onDate(sql`v.date`, f.from, f.to)} AND ${eqIf(sql`v.party_id`, f.partyId)}
      ORDER BY v.date DESC LIMIT 1000`,
  },

  // ------------------------------------------------------------ inventory
  {
    key: 'inventory-valuation',
    titleAr: 'جرد المواد وتقييم المخزون',
    group: 'inventory',
    params: [WAREHOUSE, CATEGORY, ITEM],
    columns: [text('sku', 'الرمز'), text('item', 'الصنف'), text('warehouse', 'المستودع'), qty('quantity', 'الرصيد'), money('average_cost', 'متوسط التكلفة'), money('value', 'القيمة')],
    totals: ['quantity', 'value'],
    build: (tenantId, f) => sql`
      SELECT coalesce(item.sku, '—') AS sku, ${itemName} AS item, coalesce(wh.name, '—') AS warehouse,
             sb.quantity::text, sb.average_cost::text, sb.value::text
      FROM stock_balances sb
      LEFT JOIN items item ON item.id = sb.item_id
      LEFT JOIN warehouses wh ON wh.id = sb.warehouse_id
      WHERE sb.tenant_id = ${tenantId} AND ${eqIf(sql`sb.warehouse_id`, f.warehouseId)}
        AND ${eqIf(sql`sb.item_id`, f.itemId)} AND ${eqIf(sql`item.category_id`, f.categoryId)}
      ORDER BY item.name_ar LIMIT 1000`,
  },
  {
    key: 'item-movement',
    titleAr: 'حركة مادة تفصيلي',
    group: 'inventory',
    params: [...PERIOD, WAREHOUSE, ITEM],
    columns: [date('day', 'التاريخ'), text('item', 'الصنف'), text('warehouse', 'المستودع'), text('doc_type', 'المستند'), text('direction', 'الاتجاه'), qty('qty', 'الكمية'), money('unit_cost', 'تكلفة الوحدة'), money('total_cost', 'القيمة')],
    totals: ['qty', 'total_cost'],
    build: (tenantId, f) => sql`
      SELECT tx.occurred_at::date AS day, ${itemName} AS item, coalesce(wh.name, '—') AS warehouse,
             tx.doc_type, tx.direction, tx.qty::text, tx.unit_cost::text, tx.total_cost::text
      FROM inventory_transactions tx
      LEFT JOIN items item ON item.id = tx.item_id
      LEFT JOIN warehouses wh ON wh.id = tx.warehouse_id
      WHERE tx.tenant_id = ${tenantId} AND ${onDate(sql`tx.occurred_at::date`, f.from, f.to)}
        AND ${eqIf(sql`tx.warehouse_id`, f.warehouseId)} AND ${eqIf(sql`tx.item_id`, f.itemId)}
      ORDER BY tx.occurred_at DESC LIMIT 2000`,
  },
  {
    key: 'item-movement-summary',
    titleAr: 'حركة مواد تجميعي',
    group: 'inventory',
    params: [...PERIOD, WAREHOUSE, CATEGORY],
    columns: [text('sku', 'الرمز'), text('item', 'الصنف'), qty('in_qty', 'وارد'), qty('out_qty', 'صادر'), qty('net_qty', 'الصافي'), money('in_value', 'قيمة الوارد'), money('out_value', 'قيمة الصادر')],
    totals: ['in_qty', 'out_qty', 'net_qty', 'in_value', 'out_value'],
    build: (tenantId, f) => sql`
      SELECT coalesce(item.sku, '—') AS sku, ${itemName} AS item,
             sum(CASE WHEN tx.direction = 'in' THEN tx.qty ELSE 0 END)::text AS in_qty,
             sum(CASE WHEN tx.direction = 'out' THEN tx.qty ELSE 0 END)::text AS out_qty,
             sum(CASE WHEN tx.direction = 'in' THEN tx.qty ELSE -tx.qty END)::text AS net_qty,
             sum(CASE WHEN tx.direction = 'in' THEN tx.total_cost ELSE 0 END)::text AS in_value,
             sum(CASE WHEN tx.direction = 'out' THEN tx.total_cost ELSE 0 END)::text AS out_value
      FROM inventory_transactions tx
      LEFT JOIN items item ON item.id = tx.item_id
      WHERE tx.tenant_id = ${tenantId} AND ${onDate(sql`tx.occurred_at::date`, f.from, f.to)}
        AND ${eqIf(sql`tx.warehouse_id`, f.warehouseId)} AND ${eqIf(sql`item.category_id`, f.categoryId)}
      GROUP BY item.sku, item.name_ar ORDER BY item.name_ar LIMIT 1000`,
  },
  {
    key: 'inventory-turnover',
    titleAr: 'معدل الدوران والركود',
    group: 'inventory',
    hintAr: 'معدل الدوران = قيمة الصادر خلال الفترة ÷ قيمة المخزون الحالي. الأصناف بلا حركة هي الراكدة.',
    params: [...PERIOD, WAREHOUSE, CATEGORY],
    columns: [text('sku', 'الرمز'), text('item', 'الصنف'), qty('on_hand', 'الرصيد'), money('stock_value', 'قيمة المخزون'), money('out_value', 'قيمة المصروف'), text('turnover', 'معدل الدوران'), int('idle_days', 'أيام الركود')],
    totals: ['on_hand', 'stock_value', 'out_value'],
    build: (tenantId, f) => sql`
      WITH stock AS (
        SELECT sb.item_id, sum(sb.quantity) AS quantity, sum(sb.value) AS value
        FROM stock_balances sb
        WHERE sb.tenant_id = ${tenantId} AND ${eqIf(sql`sb.warehouse_id`, f.warehouseId)}
        GROUP BY sb.item_id
      ), moves AS (
        SELECT tx.item_id,
               sum(CASE WHEN tx.direction = 'out' THEN tx.total_cost ELSE 0 END) AS out_value,
               max(tx.occurred_at) AS last_move
        FROM inventory_transactions tx
        WHERE tx.tenant_id = ${tenantId} AND ${onDate(sql`tx.occurred_at::date`, f.from, f.to)}
          AND ${eqIf(sql`tx.warehouse_id`, f.warehouseId)}
        GROUP BY tx.item_id
      )
      SELECT coalesce(item.sku, '—') AS sku, ${itemName} AS item,
             coalesce(stock.quantity, 0)::text AS on_hand, coalesce(stock.value, 0)::text AS stock_value,
             coalesce(moves.out_value, 0)::text AS out_value,
             CASE WHEN coalesce(stock.value, 0) = 0 THEN '—'
                  ELSE round(coalesce(moves.out_value, 0) / stock.value, 2)::text END AS turnover,
             CASE WHEN moves.last_move IS NULL THEN '—'
                  ELSE extract(day FROM now() - moves.last_move)::int::text END AS idle_days
      FROM items item
      LEFT JOIN stock ON stock.item_id = item.id
      LEFT JOIN moves ON moves.item_id = item.id
      WHERE item.tenant_id = ${tenantId} AND item.deleted_at IS NULL AND ${eqIf(sql`item.category_id`, f.categoryId)}
      ORDER BY coalesce(moves.out_value, 0) ASC, item.name_ar LIMIT 1000`,
  },
  {
    key: 'sales-analysis',
    titleAr: 'تحليل المبيعات',
    group: 'inventory',
    hintAr: 'مساهمة كل صنف في إجمالي مبيعات الفترة.',
    params: [...PERIOD, BRANCH, CATEGORY],
    columns: [text('sku', 'الرمز'), text('item', 'الصنف'), qty('quantity', 'الكمية'), money('revenue', 'الإيراد'), percent('share', 'نسبة المساهمة')],
    totals: ['quantity', 'revenue'],
    build: (tenantId, f) => sql`
      WITH sold AS (
        SELECT line.item_id, sum(line.quantity) AS quantity, sum(line.net) AS revenue
        FROM sales_invoice_lines line
        JOIN sales_invoices si ON si.id = line.invoice_id
        WHERE ${salesScope(tenantId, f, 'sale')}
        GROUP BY line.item_id
      )
      SELECT coalesce(item.sku, '—') AS sku, ${itemName} AS item, sold.quantity::text, sold.revenue::text,
             CASE WHEN (SELECT sum(revenue) FROM sold) = 0 THEN '0'
                  ELSE round(sold.revenue / (SELECT sum(revenue) FROM sold) * 100, 2)::text END AS share
      FROM sold LEFT JOIN items item ON item.id = sold.item_id
      WHERE ${eqIf(sql`item.category_id`, f.categoryId)}
      ORDER BY sold.revenue DESC LIMIT 500`,
  },
  {
    key: 'sales-purchases-total',
    titleAr: 'إجمالي المبيعات والمشتريات',
    group: 'inventory',
    params: [...PERIOD, BRANCH],
    columns: [date('month', 'الشهر'), money('sales', 'المبيعات'), money('purchases', 'المشتريات'), money('gap', 'الفرق')],
    totals: ['sales', 'purchases', 'gap'],
    chart: 'bar',
    build: (tenantId, f) => sql`
      WITH s AS (
        SELECT date_trunc('month', si.posted_at)::date AS month, sum(si.total) AS total
        FROM sales_invoices si WHERE ${salesScope(tenantId, f, 'sale')} GROUP BY 1
      ), p AS (
        SELECT date_trunc('month', pi.posted_at)::date AS month, sum(pi.total) AS total
        FROM purchase_invoices pi WHERE ${purchaseScope(tenantId, f, 'purchase')} GROUP BY 1
      )
      SELECT coalesce(s.month, p.month) AS month, coalesce(s.total, 0)::text AS sales,
             coalesce(p.total, 0)::text AS purchases, (coalesce(s.total, 0) - coalesce(p.total, 0))::text AS gap
      FROM s FULL OUTER JOIN p ON p.month = s.month ORDER BY month`,
  },
  {
    key: 'invoices-by-type',
    titleAr: 'الفواتير بحسب النوع',
    group: 'inventory',
    params: [...PERIOD, BRANCH],
    columns: [text('doc', 'المستند'), int('count', 'العدد'), money('total', 'الإجمالي')],
    totals: ['count', 'total'],
    build: (tenantId, f) => sql`
      SELECT doc, count(*)::text AS count, sum(total)::text AS total FROM (
        SELECT CASE si.kind WHEN 'sale' THEN 'فاتورة مبيعات' ELSE 'مردود مبيعات' END AS doc, si.total
        FROM sales_invoices si
        WHERE si.tenant_id = ${tenantId} AND si.status = 'posted'
          AND ${onDate(sql`si.posted_at::date`, f.from, f.to)} AND ${eqIf(sql`si.branch_id`, f.branchId)}
        UNION ALL
        SELECT CASE pi.kind WHEN 'purchase' THEN 'فاتورة مشتريات' ELSE 'مردود مشتريات' END AS doc, pi.total
        FROM purchase_invoices pi
        WHERE pi.tenant_id = ${tenantId} AND pi.status = 'posted'
          AND ${onDate(sql`pi.posted_at::date`, f.from, f.to)} AND ${eqIf(sql`pi.branch_id`, f.branchId)}
      ) docs GROUP BY doc ORDER BY doc`,
  },
  {
    key: 'expiry-report',
    titleAr: 'صلاحية المواد',
    group: 'inventory',
    hintAr: 'الأيام المتبقية سالبة تعني أن الدفعة منتهية الصلاحية.',
    params: [ITEM],
    columns: [text('sku', 'الرمز'), text('item', 'الصنف'), text('lot_no', 'رقم الدفعة'), date('expiry_date', 'تاريخ الانتهاء'), int('days_left', 'الأيام المتبقية')],
    build: (tenantId, f) => sql`
      SELECT coalesce(item.sku, '—') AS sku, ${itemName} AS item, lot.lot_no, lot.expiry_date,
             CASE WHEN lot.expiry_date IS NULL THEN '—' ELSE (lot.expiry_date - current_date)::text END AS days_left
      FROM item_lots lot
      LEFT JOIN items item ON item.id = lot.item_id
      WHERE lot.tenant_id = ${tenantId} AND lot.deleted_at IS NULL AND ${eqIf(sql`lot.item_id`, f.itemId)}
      ORDER BY lot.expiry_date NULLS LAST LIMIT 1000`,
  },
  {
    key: 'expired-items',
    titleAr: 'انتهاء صلاحية الأصناف',
    group: 'inventory',
    hintAr: 'الدفعات المنتهية أو التي تنتهي خلال 30 يوماً.',
    params: [],
    columns: [text('sku', 'الرمز'), text('item', 'الصنف'), text('lot_no', 'رقم الدفعة'), date('expiry_date', 'تاريخ الانتهاء'), int('days_left', 'الأيام المتبقية')],
    build: (tenantId) => sql`
      SELECT coalesce(item.sku, '—') AS sku, ${itemName} AS item, lot.lot_no, lot.expiry_date,
             (lot.expiry_date - current_date)::text AS days_left
      FROM item_lots lot
      LEFT JOIN items item ON item.id = lot.item_id
      WHERE lot.tenant_id = ${tenantId} AND lot.deleted_at IS NULL
        AND lot.expiry_date IS NOT NULL AND lot.expiry_date <= current_date + 30
      ORDER BY lot.expiry_date LIMIT 1000`,
  },
  {
    key: 'serial-tracking',
    titleAr: 'تقرير الأرقام التسلسلية',
    group: 'inventory',
    params: [ITEM, WAREHOUSE],
    columns: [text('serial_no', 'الرقم التسلسلي'), text('item', 'الصنف'), text('warehouse', 'المستودع'), text('status', 'الحالة')],
    build: (tenantId, f) => sql`
      SELECT s.serial_no, ${itemName} AS item, coalesce(wh.name, '—') AS warehouse, s.status
      FROM item_serials s
      LEFT JOIN items item ON item.id = s.item_id
      LEFT JOIN warehouses wh ON wh.id = s.warehouse_id
      WHERE s.tenant_id = ${tenantId} AND s.deleted_at IS NULL
        AND ${eqIf(sql`s.item_id`, f.itemId)} AND ${eqIf(sql`s.warehouse_id`, f.warehouseId)}
      ORDER BY s.serial_no LIMIT 1000`,
  },
  {
    key: 'stock-limits',
    titleAr: 'الأصناف تحت الحد الأدنى',
    group: 'inventory',
    params: [WAREHOUSE],
    columns: [text('sku', 'الرمز'), text('item', 'الصنف'), qty('on_hand', 'الرصيد'), qty('min_qty', 'الحد الأدنى'), qty('shortfall', 'العجز')],
    totals: ['shortfall'],
    build: (tenantId, f) => sql`
      SELECT item.sku, item.name_ar AS item, coalesce(stock.quantity, 0)::text AS on_hand,
             item.min_qty::text, (item.min_qty - coalesce(stock.quantity, 0))::text AS shortfall
      FROM items item
      LEFT JOIN (
        SELECT sb.item_id, sum(sb.quantity) AS quantity FROM stock_balances sb
        WHERE sb.tenant_id = ${tenantId} AND ${eqIf(sql`sb.warehouse_id`, f.warehouseId)} GROUP BY sb.item_id
      ) stock ON stock.item_id = item.id
      WHERE item.tenant_id = ${tenantId} AND item.deleted_at IS NULL AND item.min_qty > 0
        AND coalesce(stock.quantity, 0) < item.min_qty
      ORDER BY (item.min_qty - coalesce(stock.quantity, 0)) DESC LIMIT 500`,
  },

  // ----------------------------------------------------------- accounting
  {
    key: 'trial-balance',
    titleAr: 'ميزان المراجعة',
    group: 'accounting',
    params: [...PERIOD, BRANCH],
    columns: [text('code', 'رقم الحساب'), text('account', 'الحساب'), money('debit', 'مدين'), money('credit', 'دائن'), money('balance', 'الرصيد')],
    totals: ['debit', 'credit', 'balance'],
    build: (tenantId, f) => sql`
      SELECT acc.code, acc.name_ar AS account, sum(jel.debit)::text AS debit, sum(jel.credit)::text AS credit,
             (sum(jel.debit) - sum(jel.credit))::text AS balance
      FROM journal_entry_lines jel
      JOIN journal_entries je ON je.id = jel.entry_id
      JOIN accounts acc ON acc.id = jel.account_id
      WHERE jel.tenant_id = ${tenantId} AND je.status = 'posted'
        AND ${onDate(sql`je.date`, f.from, f.to)} AND ${eqIf(sql`je.branch_id`, f.branchId)}
      GROUP BY acc.code, acc.name_ar ORDER BY acc.code`,
  },
  {
    key: 'general-ledger',
    titleAr: 'الحركة اليومية (دفتر الأستاذ)',
    group: 'accounting',
    params: [...PERIOD, BRANCH, COST_CENTER],
    columns: [date('day', 'التاريخ'), text('number', 'القيد'), text('code', 'الحساب'), text('account', 'اسم الحساب'), text('description', 'البيان'), money('debit', 'مدين'), money('credit', 'دائن')],
    totals: ['debit', 'credit'],
    build: (tenantId, f) => sql`
      SELECT je.date AS day, coalesce(je.number, '—') AS number, acc.code, acc.name_ar AS account,
             coalesce(jel.description, je.description, '—') AS description, jel.debit::text, jel.credit::text
      FROM journal_entry_lines jel
      JOIN journal_entries je ON je.id = jel.entry_id
      JOIN accounts acc ON acc.id = jel.account_id
      WHERE jel.tenant_id = ${tenantId} AND je.status = 'posted'
        AND ${onDate(sql`je.date`, f.from, f.to)} AND ${eqIf(sql`je.branch_id`, f.branchId)}
        AND ${eqIf(sql`jel.cost_center_id`, f.costCenterId)}
      ORDER BY je.date DESC, je.number DESC, jel.line_no LIMIT 2000`,
  },
  {
    key: 'cash-movement',
    titleAr: 'حركة الصندوق',
    group: 'accounting',
    hintAr: 'القبض والصرف المرحّل لكل صندوق أو بنك.',
    params: [...PERIOD, BRANCH],
    columns: [text('location', 'الصندوق / البنك'), money('receipts', 'قبض'), money('payments', 'صرف'), money('net', 'الصافي')],
    totals: ['receipts', 'payments', 'net'],
    build: (tenantId, f) => sql`
      SELECT coalesce(cl.name, '—') AS location,
             sum(CASE WHEN v.kind = 'receipt' THEN v.amount ELSE 0 END)::text AS receipts,
             sum(CASE WHEN v.kind = 'payment' THEN v.amount ELSE 0 END)::text AS payments,
             sum(CASE WHEN v.kind = 'receipt' THEN v.amount ELSE -v.amount END)::text AS net
      FROM vouchers v
      LEFT JOIN cash_locations cl ON cl.id = v.cash_location_id
      WHERE v.tenant_id = ${tenantId} AND v.status = 'posted'
        AND ${onDate(sql`v.date`, f.from, f.to)} AND ${eqIf(sql`v.branch_id`, f.branchId)}
      GROUP BY cl.name ORDER BY cl.name`,
  },
  {
    key: 'cost-center-balances',
    titleAr: 'أرصدة مراكز التكلفة',
    group: 'accounting',
    params: [...PERIOD],
    columns: [text('code', 'الرمز'), text('cost_center', 'مركز التكلفة'), money('debit', 'مدين'), money('credit', 'دائن'), money('balance', 'الرصيد')],
    totals: ['debit', 'credit', 'balance'],
    build: (tenantId, f) => sql`
      SELECT cc.code, cc.name_ar AS cost_center, sum(jel.debit)::text AS debit, sum(jel.credit)::text AS credit,
             (sum(jel.debit) - sum(jel.credit))::text AS balance
      FROM journal_entry_lines jel
      JOIN journal_entries je ON je.id = jel.entry_id
      JOIN cost_centers cc ON cc.id = jel.cost_center_id
      WHERE jel.tenant_id = ${tenantId} AND je.status = 'posted' AND ${onDate(sql`je.date`, f.from, f.to)}
      GROUP BY cc.code, cc.name_ar ORDER BY cc.code`,
  },
  {
    key: 'cost-center-report',
    titleAr: 'تقرير مركز الكلفة',
    group: 'accounting',
    params: [...PERIOD, COST_CENTER],
    columns: [date('day', 'التاريخ'), text('cost_center', 'مركز التكلفة'), text('account', 'الحساب'), text('description', 'البيان'), money('debit', 'مدين'), money('credit', 'دائن')],
    totals: ['debit', 'credit'],
    build: (tenantId, f) => sql`
      SELECT je.date AS day, cc.name_ar AS cost_center, acc.name_ar AS account,
             coalesce(jel.description, je.description, '—') AS description, jel.debit::text, jel.credit::text
      FROM journal_entry_lines jel
      JOIN journal_entries je ON je.id = jel.entry_id
      JOIN cost_centers cc ON cc.id = jel.cost_center_id
      JOIN accounts acc ON acc.id = jel.account_id
      WHERE jel.tenant_id = ${tenantId} AND je.status = 'posted'
        AND ${onDate(sql`je.date`, f.from, f.to)} AND ${eqIf(sql`jel.cost_center_id`, f.costCenterId)}
      ORDER BY je.date DESC LIMIT 2000`,
  },
  {
    key: 'income-statement',
    titleAr: 'قائمة الدخل التحليلية',
    group: 'accounting',
    hintAr: 'الإيرادات موجبة والمصروفات سالبة؛ المجموع هو صافي الربح.',
    params: [...PERIOD, BRANCH],
    columns: [text('section', 'البند'), text('code', 'رقم الحساب'), text('account', 'الحساب'), money('amount', 'المبلغ')],
    totals: ['amount'],
    build: (tenantId, f) => sql`
      SELECT CASE acc.type WHEN 'revenue' THEN 'الإيرادات' ELSE 'المصروفات' END AS section,
             acc.code, acc.name_ar AS account,
             CASE acc.type WHEN 'revenue' THEN sum(jel.credit) - sum(jel.debit)
                           ELSE -(sum(jel.debit) - sum(jel.credit)) END::text AS amount
      FROM journal_entry_lines jel
      JOIN journal_entries je ON je.id = jel.entry_id
      JOIN accounts acc ON acc.id = jel.account_id
      WHERE jel.tenant_id = ${tenantId} AND je.status = 'posted' AND acc.type IN ('revenue', 'expense')
        AND ${onDate(sql`je.date`, f.from, f.to)} AND ${eqIf(sql`je.branch_id`, f.branchId)}
      GROUP BY acc.type, acc.code, acc.name_ar ORDER BY acc.type DESC, acc.code`,
  },
  {
    key: 'balance-sheet',
    titleAr: 'ميزانية تحليلية',
    group: 'accounting',
    params: [{ name: 'to', labelAr: 'حتى تاريخ', kind: 'date' }, BRANCH],
    columns: [text('section', 'القسم'), text('code', 'رقم الحساب'), text('account', 'الحساب'), money('balance', 'الرصيد')],
    totals: ['balance'],
    build: (tenantId, f) => sql`
      SELECT CASE acc.type WHEN 'asset' THEN 'الأصول' WHEN 'liability' THEN 'الخصوم' ELSE 'حقوق الملكية' END AS section,
             acc.code, acc.name_ar AS account,
             CASE acc.type WHEN 'asset' THEN sum(jel.debit) - sum(jel.credit)
                           ELSE sum(jel.credit) - sum(jel.debit) END::text AS balance
      FROM journal_entry_lines jel
      JOIN journal_entries je ON je.id = jel.entry_id
      JOIN accounts acc ON acc.id = jel.account_id
      WHERE jel.tenant_id = ${tenantId} AND je.status = 'posted' AND acc.type IN ('asset', 'liability', 'equity')
        AND ${onDate(sql`je.date`, undefined, f.to)} AND ${eqIf(sql`je.branch_id`, f.branchId)}
      GROUP BY acc.type, acc.code, acc.name_ar ORDER BY acc.type, acc.code`,
  },
  {
    key: 'vat-return',
    titleAr: 'الإقرار الضريبي',
    group: 'accounting',
    hintAr: 'ضريبة المخرجات من المبيعات ناقص ضريبة المدخلات من المشتريات.',
    params: [...PERIOD, BRANCH],
    columns: [text('bucket', 'البند'), money('base', 'الوعاء'), money('vat', 'الضريبة')],
    totals: ['base', 'vat'],
    build: (tenantId, f) => sql`
      SELECT 'مبيعات خاضعة' AS bucket, coalesce(sum(si.subtotal), 0)::text AS base, coalesce(sum(si.tax_total), 0)::text AS vat
      FROM sales_invoices si WHERE ${salesScope(tenantId, f, 'sale')}
      UNION ALL
      SELECT 'مردود مبيعات', (-coalesce(sum(si.subtotal), 0))::text, (-coalesce(sum(si.tax_total), 0))::text
      FROM sales_invoices si WHERE ${salesScope(tenantId, f, 'sale_return')}
      UNION ALL
      SELECT 'مشتريات خاضعة', coalesce(sum(pi.subtotal), 0)::text, (-coalesce(sum(pi.tax_total), 0))::text
      FROM purchase_invoices pi WHERE ${purchaseScope(tenantId, f, 'purchase')}
      UNION ALL
      SELECT 'مردود مشتريات', (-coalesce(sum(pi.subtotal), 0))::text, coalesce(sum(pi.tax_total), 0)::text
      FROM purchase_invoices pi WHERE ${purchaseScope(tenantId, f, 'purchase_return')}`,
  },
  {
    key: 'ar-aging',
    titleAr: 'أعمار ديون العملاء',
    group: 'accounting',
    hintAr: 'المتبقي على كل فاتورة مبيعات مرحّلة موزّعاً على شرائح العمر.',
    params: [BRANCH],
    columns: [text('party', 'العميل'), money('bucket_0_30', '0-30 يوم'), money('bucket_31_60', '31-60'), money('bucket_61_90', '61-90'), money('bucket_90_plus', 'أكثر من 90'), money('due', 'الإجمالي')],
    totals: ['bucket_0_30', 'bucket_31_60', 'bucket_61_90', 'bucket_90_plus', 'due'],
    build: (tenantId, f) => sql`
      SELECT ${partyName} AS party,
             sum(CASE WHEN now() - si.posted_at <= interval '30 days' THEN si.total - si.paid_total ELSE 0 END)::text AS bucket_0_30,
             sum(CASE WHEN now() - si.posted_at > interval '30 days' AND now() - si.posted_at <= interval '60 days' THEN si.total - si.paid_total ELSE 0 END)::text AS bucket_31_60,
             sum(CASE WHEN now() - si.posted_at > interval '60 days' AND now() - si.posted_at <= interval '90 days' THEN si.total - si.paid_total ELSE 0 END)::text AS bucket_61_90,
             sum(CASE WHEN now() - si.posted_at > interval '90 days' THEN si.total - si.paid_total ELSE 0 END)::text AS bucket_90_plus,
             sum(si.total - si.paid_total)::text AS due
      FROM sales_invoices si
      LEFT JOIN parties party ON party.id = si.party_id
      WHERE si.tenant_id = ${tenantId} AND si.kind = 'sale' AND si.status = 'posted'
        AND si.total > si.paid_total AND ${eqIf(sql`si.branch_id`, f.branchId)}
      GROUP BY party.name ORDER BY sum(si.total - si.paid_total) DESC`,
  },
  {
    key: 'ap-aging',
    titleAr: 'أعمار ديون الموردين',
    group: 'accounting',
    params: [BRANCH],
    columns: [text('party', 'المورد'), money('bucket_0_30', '0-30 يوم'), money('bucket_31_60', '31-60'), money('bucket_61_90', '61-90'), money('bucket_90_plus', 'أكثر من 90'), money('due', 'الإجمالي')],
    totals: ['bucket_0_30', 'bucket_31_60', 'bucket_61_90', 'bucket_90_plus', 'due'],
    build: (tenantId, f) => sql`
      SELECT ${partyName} AS party,
             sum(CASE WHEN now() - pi.posted_at <= interval '30 days' THEN pi.total - pi.paid_total ELSE 0 END)::text AS bucket_0_30,
             sum(CASE WHEN now() - pi.posted_at > interval '30 days' AND now() - pi.posted_at <= interval '60 days' THEN pi.total - pi.paid_total ELSE 0 END)::text AS bucket_31_60,
             sum(CASE WHEN now() - pi.posted_at > interval '60 days' AND now() - pi.posted_at <= interval '90 days' THEN pi.total - pi.paid_total ELSE 0 END)::text AS bucket_61_90,
             sum(CASE WHEN now() - pi.posted_at > interval '90 days' THEN pi.total - pi.paid_total ELSE 0 END)::text AS bucket_90_plus,
             sum(pi.total - pi.paid_total)::text AS due
      FROM purchase_invoices pi
      LEFT JOIN parties party ON party.id = pi.party_id
      WHERE pi.tenant_id = ${tenantId} AND pi.kind = 'purchase' AND pi.status = 'posted'
        AND pi.total > pi.paid_total AND ${eqIf(sql`pi.branch_id`, f.branchId)}
      GROUP BY party.name ORDER BY sum(pi.total - pi.paid_total) DESC`,
  },
  {
    key: 'party-statement',
    titleAr: 'كشف حساب طرف',
    group: 'accounting',
    hintAr: 'اختر العميل أو المورد لعرض حركته من القيود المرحّلة.',
    params: [PARTY, ...PERIOD],
    columns: [date('day', 'التاريخ'), text('number', 'القيد'), text('description', 'البيان'), money('debit', 'مدين'), money('credit', 'دائن'), money('running', 'الرصيد التراكمي')],
    totals: ['debit', 'credit'],
    build: (tenantId, f) => sql`
      SELECT je.date AS day, coalesce(je.number, '—') AS number,
             coalesce(jel.description, je.description, '—') AS description,
             jel.debit::text, jel.credit::text,
             sum(jel.debit - jel.credit) OVER (ORDER BY je.date, je.number, jel.line_no)::text AS running
      FROM journal_entry_lines jel
      JOIN journal_entries je ON je.id = jel.entry_id
      WHERE jel.tenant_id = ${tenantId} AND je.status = 'posted' AND jel.party_id IS NOT NULL
        AND ${eqIf(sql`jel.party_id`, f.partyId)} AND ${onDate(sql`je.date`, f.from, f.to)}
      ORDER BY je.date, je.number, jel.line_no LIMIT 2000`,
  },

  // ------------------------------------------------------------------ pos
  {
    key: 'pos-sales',
    titleAr: 'تقرير مبيعات نقطة البيع',
    group: 'pos',
    hintAr: 'الفواتير النقدية التي لا يقابلها عميل مسجّل هي مبيعات نقطة البيع.',
    params: [...PERIOD, BRANCH],
    columns: [text('number', 'الرقم'), date('day', 'التاريخ'), text('customer', 'العميل النقدي'), money('total', 'الإجمالي'), text('payment_status', 'السداد')],
    totals: ['total'],
    build: (tenantId, f) => sql`
      SELECT si.number, si.posted_at::date AS day, coalesce(si.cash_customer_name, 'نقدي') AS customer,
             si.total::text, si.payment_status
      FROM sales_invoices si
      WHERE ${salesScope(tenantId, f, 'sale')} AND si.party_id IS NULL
      ORDER BY si.posted_at DESC LIMIT 1000`,
  },
  {
    key: 'pos-daily',
    titleAr: 'المبيعات اليومية لنقطة البيع',
    group: 'pos',
    params: [...PERIOD, BRANCH],
    columns: [date('day', 'اليوم'), int('invoices', 'عدد الفواتير'), money('total', 'الإجمالي')],
    totals: ['invoices', 'total'],
    chart: 'bar',
    build: (tenantId, f) => sql`
      SELECT si.posted_at::date AS day, count(*)::text AS invoices, sum(si.total)::text AS total
      FROM sales_invoices si WHERE ${salesScope(tenantId, f, 'sale')} AND si.party_id IS NULL
      GROUP BY si.posted_at::date ORDER BY day DESC`,
  },
  {
    key: 'pos-item-summary',
    titleAr: 'أصناف نقطة البيع تجميعي',
    group: 'pos',
    params: [...PERIOD, BRANCH],
    columns: [text('item', 'الصنف'), qty('quantity', 'الكمية'), money('total', 'الإجمالي')],
    totals: ['quantity', 'total'],
    build: (tenantId, f) => sql`
      SELECT ${itemName} AS item, sum(line.quantity)::text AS quantity, sum(line.total)::text AS total
      FROM sales_invoice_lines line
      JOIN sales_invoices si ON si.id = line.invoice_id
      LEFT JOIN items item ON item.id = line.item_id
      WHERE ${salesScope(tenantId, f, 'sale')} AND si.party_id IS NULL
      GROUP BY item.name_ar ORDER BY sum(line.total) DESC LIMIT 500`,
  },
  {
    key: 'cashier-shift',
    titleAr: 'إغلاقات اليومية',
    group: 'pos',
    params: [...PERIOD, BRANCH],
    columns: [date('opened', 'الفتح'), date('closed', 'الإغلاق'), text('branch', 'الفرع'), text('status', 'الحالة'), money('expected_cash', 'النقد المتوقع'), money('counted_cash', 'النقد المعدود'), money('diff', 'الفرق')],
    totals: ['expected_cash', 'counted_cash', 'diff'],
    build: (tenantId, f) => sql`
      SELECT sc.opened_at::date AS opened, sc.closed_at::date AS closed, ${branchName} AS branch, sc.status,
             sc.expected_cash::text, sc.counted_cash::text, sc.diff::text
      FROM shift_closes sc
      LEFT JOIN branches branch ON branch.id = sc.branch_id
      WHERE sc.tenant_id = ${tenantId} AND ${onDate(sql`sc.opened_at::date`, f.from, f.to)}
        AND ${eqIf(sql`sc.branch_id`, f.branchId)}
      ORDER BY sc.opened_at DESC LIMIT 500`,
  },

  // ------------------------------------------------------------------ hrm
  {
    key: 'payroll-payments',
    titleAr: 'دفع الرواتب',
    group: 'hrm',
    params: [],
    columns: [text('year_month', 'الشهر'), text('status', 'الحالة'), int('employees', 'عدد الموظفين'), money('net', 'صافي الرواتب'), date('posted', 'تاريخ الترحيل'), date('paid', 'تاريخ الصرف')],
    totals: ['employees', 'net'],
    build: (tenantId) => sql`
      SELECT run.year_month, run.status, count(line.employee_id)::text AS employees,
             coalesce(sum(line.net), 0)::text AS net, run.posted_at::date AS posted, run.paid_at::date AS paid
      FROM payroll_runs run
      LEFT JOIN payroll_run_lines line ON line.run_id = run.id
      WHERE run.tenant_id = ${tenantId}
      GROUP BY run.id, run.year_month, run.status, run.posted_at, run.paid_at
      ORDER BY run.year_month DESC LIMIT 200`,
  },
  {
    key: 'employee-account',
    titleAr: 'حساب موظف',
    group: 'hrm',
    hintAr: 'كل ما استحقه الموظف شهراً بشهر.',
    params: [],
    columns: [text('employee_no', 'الرقم'), text('employee', 'الموظف'), text('year_month', 'الشهر'), money('gross', 'الإجمالي'), money('additions', 'حوافز'), money('deductions', 'جزاءات'), money('net', 'الصافي'), text('status', 'الحالة')],
    totals: ['gross', 'additions', 'deductions', 'net'],
    build: (tenantId) => sql`
      SELECT emp.employee_no, emp.name AS employee, run.year_month, line.gross::text, line.additions::text,
             line.deductions::text, line.net::text, run.status
      FROM payroll_run_lines line
      JOIN payroll_runs run ON run.id = line.run_id
      JOIN employees emp ON emp.id = line.employee_id
      WHERE line.tenant_id = ${tenantId}
      ORDER BY run.year_month DESC, emp.employee_no LIMIT 1000`,
  },

  // -------------------------------------------------------- marina/projects
  {
    key: 'marina-rentals',
    titleAr: 'تقرير فواتير التأجير',
    group: 'marina',
    params: [...PERIOD, BRANCH],
    columns: [date('day', 'التاريخ'), text('vessel', 'المركب'), text('party', 'العميل'), int('companions', 'المرافقون'), money('period_amount', 'قيمة الفترة'), money('additions_amount', 'الإضافات'), money('insurance_amount', 'التأمين'), money('total', 'الإجمالي'), text('number', 'الفاتورة')],
    totals: ['period_amount', 'additions_amount', 'insurance_amount', 'total'],
    build: (tenantId, f) => sql`
      SELECT b.starts_at::date AS day, coalesce(v.name, '—') AS vessel, ${partyName} AS party,
             b.companions::text, rental.period_amount::text, rental.additions_amount::text,
             rental.insurance_amount::text, rental.total::text, coalesce(si.number, '—') AS number
      FROM rental_invoices rental
      JOIN marina_bookings b ON b.id = rental.booking_id
      LEFT JOIN vessels v ON v.id = b.vessel_id
      LEFT JOIN parties party ON party.id = b.party_id
      LEFT JOIN sales_invoices si ON si.id = rental.sales_invoice_id
      WHERE rental.tenant_id = ${tenantId}
        AND ${onDate(sql`b.starts_at::date`, f.from, f.to)} AND ${eqIf(sql`b.branch_id`, f.branchId)}
      ORDER BY b.starts_at DESC LIMIT 1000`,
  },
  {
    key: 'project-bills',
    titleAr: 'تقرير فواتير المقاولات',
    group: 'projects',
    params: [...PERIOD],
    columns: [text('project', 'المشروع'), text('number', 'المستخلص'), date('bill_date', 'التاريخ'), money('work_value', 'قيمة الأعمال'), money('previous_value', 'سابق'), money('retention_value', 'المحتجز'), money('net_due', 'المستحق'), text('status', 'الحالة')],
    totals: ['work_value', 'retention_value', 'net_due'],
    build: (tenantId, f) => sql`
      SELECT p.name AS project, coalesce(bill.number, '—') AS number, bill.bill_date,
             bill.work_value::text, bill.previous_value::text, bill.retention_value::text,
             bill.net_due::text, bill.status
      FROM progress_bills bill
      JOIN projects p ON p.id = bill.project_id
      WHERE bill.tenant_id = ${tenantId} AND ${onDate(sql`bill.bill_date`, f.from, f.to)}
      ORDER BY bill.bill_date DESC LIMIT 1000`,
  },
  {
    key: 'pos-item-detail',
    titleAr: 'تفاصيل أصناف نقطة البيع',
    group: 'pos',
    hintAr: 'سطر لكل صنف في كل فاتورة نقدية.',
    params: [...PERIOD, BRANCH, ITEM],
    columns: [date('day', 'التاريخ'), text('number', 'الفاتورة'), text('item', 'الصنف'), qty('quantity', 'الكمية'), money('unit_price', 'السعر'), money('discount_amount', 'الخصم'), money('total', 'الإجمالي')],
    totals: ['quantity', 'discount_amount', 'total'],
    build: (tenantId, f) => sql`
      SELECT si.posted_at::date AS day, si.number, ${itemName} AS item, line.quantity::text,
             line.unit_price::text, line.discount_amount::text, line.total::text
      FROM sales_invoice_lines line
      JOIN sales_invoices si ON si.id = line.invoice_id
      LEFT JOIN items item ON item.id = line.item_id
      WHERE ${salesScope(tenantId, f, 'sale')} AND si.party_id IS NULL AND ${eqIf(sql`line.item_id`, f.itemId)}
      ORDER BY si.posted_at DESC, line.line_no LIMIT 2000`,
  },
  {
    key: 'pos-by-category',
    titleAr: 'مبيعات نقطة البيع بحسب المجموعة',
    group: 'pos',
    params: [...PERIOD, BRANCH],
    columns: [text('category', 'المجموعة'), int('invoices', 'عدد الفواتير'), qty('quantity', 'الكمية'), money('total', 'الإجمالي')],
    totals: ['quantity', 'total'],
    chart: 'bar',
    build: (tenantId, f) => sql`
      SELECT coalesce(cat.name_ar, '—') AS category, count(DISTINCT si.id)::text AS invoices,
             sum(line.quantity)::text AS quantity, sum(line.total)::text AS total
      FROM sales_invoice_lines line
      JOIN sales_invoices si ON si.id = line.invoice_id
      LEFT JOIN items item ON item.id = line.item_id
      LEFT JOIN item_categories cat ON cat.id = item.category_id
      WHERE ${salesScope(tenantId, f, 'sale')} AND si.party_id IS NULL
      GROUP BY cat.name_ar ORDER BY sum(line.total) DESC`,
  },
  {
    key: 'sales-by-employee',
    titleAr: 'مبيعات موظف',
    group: 'sales',
    hintAr: 'حسب المستخدم الذي أنشأ الفاتورة. الفواتير التي أُنشئت قبل تفعيل التتبّع تظهر كـ«غير محدد».',
    params: [...PERIOD, BRANCH],
    columns: [text('employee', 'الموظف'), int('invoices', 'عدد الفواتير'), money('total', 'الإجمالي'), money('profit', 'الربح')],
    totals: ['invoices', 'total', 'profit'],
    build: (tenantId, f) => sql`
      SELECT coalesce(u.full_name, u.email, 'غير محدد') AS employee, count(*)::text AS invoices,
             sum(si.total)::text AS total, sum(si.subtotal - si.cost_total)::text AS profit
      FROM sales_invoices si
      LEFT JOIN users u ON u.id = si.created_by
      WHERE ${salesScope(tenantId, f, 'sale')}
      GROUP BY u.full_name, u.email ORDER BY sum(si.total) DESC`,
  },
  {
    key: 'purchases-by-employee',
    titleAr: 'مشتريات موظف',
    group: 'purchases',
    hintAr: 'حسب المستخدم الذي أنشأ فاتورة الشراء.',
    params: [...PERIOD, BRANCH],
    columns: [text('employee', 'الموظف'), int('invoices', 'عدد الفواتير'), money('total', 'الإجمالي')],
    totals: ['invoices', 'total'],
    build: (tenantId, f) => sql`
      SELECT coalesce(u.full_name, u.email, 'غير محدد') AS employee, count(*)::text AS invoices,
             sum(pi.total)::text AS total
      FROM purchase_invoices pi
      LEFT JOIN users u ON u.id = pi.created_by
      WHERE ${purchaseScope(tenantId, f, 'purchase')}
      GROUP BY u.full_name, u.email ORDER BY sum(pi.total) DESC`,
  },
  {
    key: 'sales-notes',
    titleAr: 'تقرير إشعارات المبيعات',
    group: 'sales',
    hintAr: 'الإشعارات الدائنة والمدينة الصادرة على فواتير مرحّلة.',
    params: [...PERIOD, BRANCH],
    columns: [date('day', 'التاريخ'), text('number', 'رقم الإشعار'), text('kind', 'النوع'), text('invoice', 'الفاتورة'), text('party', 'العميل'), text('reason', 'السبب'), money('amount', 'المبلغ'), text('status', 'الحالة')],
    totals: ['amount'],
    build: (tenantId, f) => sql`
      SELECT note.created_at::date AS day, coalesce(note.number, '—') AS number,
             CASE note.kind WHEN 'credit' THEN 'إشعار دائن' WHEN 'debit' THEN 'إشعار مدين' ELSE note.kind END AS kind,
             coalesce(si.number, '—') AS invoice, ${partyName} AS party, note.reason, note.amount::text, note.status
      FROM sales_adjustment_notes note
      LEFT JOIN sales_invoices si ON si.id = note.invoice_id
      LEFT JOIN parties party ON party.id = si.party_id
      WHERE note.tenant_id = ${tenantId} AND ${onDate(sql`note.created_at::date`, f.from, f.to)}
        AND ${eqIf(sql`note.branch_id`, f.branchId)}
      ORDER BY note.created_at DESC LIMIT 1000`,
  },
  // ----------------------------------------------------- long-lived keys
  // Registered since the first release; kept so saved links and the legacy
  // desktop client keep resolving.
  {
    key: 'sales-by-ordertype',
    titleAr: 'المبيعات بحسب نوع الطلب',
    group: 'sales',
    params: [...PERIOD, BRANCH],
    columns: [text('order_type', 'نوع الطلب'), int('count', 'العدد'), money('total', 'الإجمالي')],
    totals: ['count', 'total'],
    build: (tenantId, f) => sql`
      SELECT coalesce(si.order_type, 'عادي') AS order_type, count(*)::text AS count, sum(si.total)::text AS total
      FROM sales_invoices si WHERE ${salesScope(tenantId, f, 'sale')}
      GROUP BY coalesce(si.order_type, 'عادي') ORDER BY order_type`,
  },
  {
    key: 'batch-tracking',
    titleAr: 'تتبع الدفعات',
    group: 'inventory',
    params: [ITEM],
    columns: [text('sku', 'الرمز'), text('item', 'الصنف'), text('lot_no', 'رقم الدفعة'), date('received_at', 'تاريخ الاستلام'), date('expiry_date', 'تاريخ الانتهاء')],
    build: (tenantId, f) => sql`
      SELECT coalesce(item.sku, '—') AS sku, ${itemName} AS item, lot.lot_no,
             lot.received_at::date AS received_at, lot.expiry_date
      FROM item_lots lot
      LEFT JOIN items item ON item.id = lot.item_id
      WHERE lot.tenant_id = ${tenantId} AND lot.deleted_at IS NULL AND ${eqIf(sql`lot.item_id`, f.itemId)}
      ORDER BY lot.received_at DESC NULLS LAST LIMIT 1000`,
  },
  {
    key: 'profit-loss',
    titleAr: 'الأرباح والخسائر',
    group: 'accounting',
    hintAr: 'ملخص الإيرادات والمصروفات وصافي النتيجة.',
    params: [...PERIOD, BRANCH],
    columns: [text('section', 'البند'), money('amount', 'المبلغ')],
    build: (tenantId, f) => sql`
      WITH movement AS (
        SELECT acc.type, sum(jel.credit) - sum(jel.debit) AS net
        FROM journal_entry_lines jel
        JOIN journal_entries je ON je.id = jel.entry_id
        JOIN accounts acc ON acc.id = jel.account_id
        WHERE jel.tenant_id = ${tenantId} AND je.status = 'posted' AND acc.type IN ('revenue', 'expense')
          AND ${onDate(sql`je.date`, f.from, f.to)} AND ${eqIf(sql`je.branch_id`, f.branchId)}
        GROUP BY acc.type
      )
      SELECT 'إجمالي الإيرادات' AS section, coalesce((SELECT net FROM movement WHERE type = 'revenue'), 0)::text AS amount
      UNION ALL
      SELECT 'إجمالي المصروفات', coalesce(-(SELECT net FROM movement WHERE type = 'expense'), 0)::text
      UNION ALL
      SELECT 'صافي الربح', (coalesce((SELECT net FROM movement WHERE type = 'revenue'), 0)
                            + coalesce((SELECT net FROM movement WHERE type = 'expense'), 0))::text`,
  },
];

export const REPORT_DEFINITIONS: ReportDefinition[] = definitions;
export const REPORT_KEYS = definitions.map((definition) => definition.key);
export const reportByKey = new Map(definitions.map((definition) => [definition.key, definition]));
