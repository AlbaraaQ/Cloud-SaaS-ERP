import { sql, type SQL } from 'drizzle-orm';

/**
 * Report catalog — one definition per screen in the desktop product's report menus.
 *
 * A definition carries everything a client needs to render the report without knowing
 * anything about it: which filters to offer, which columns to draw, how to format each
 * column and which columns to total. The admin app therefore ships **one** report screen
 * instead of forty hand-written ones, and adding a report here makes it appear there.
 */
export type ReportParamKind = 'date' | 'time' | 'branch' | 'warehouse' | 'party' | 'item' | 'category' | 'salesman' | 'costCenter' | 'select';

export type ReportParam = {
  name: string;
  labelAr: string;
  kind: ReportParamKind;
  options?: Array<{ value: string; labelAr: string }>;
};

export type ReportColumnType = 'text' | 'money' | 'qty' | 'int' | 'date' | 'percent';

/**
 * A column the report **computes** but never shows — the signed twin of a money column,
 * whose only purpose is the one number under the grid. `Form_WPF/frmRptSalesInPeriod`
 * prints «💰 إجمالي المبيعات:» as `المبيعات − المردودات`, while every row of its grid is
 * positive: the hidden column carries the sign, and `grandTotal` sums it.
 */
export type ReportColumn = { key: string; labelAr: string; type: ReportColumnType; hidden?: boolean };

/** One summary card under the grid: the column it sums and the label it prints. */
export type ReportGrandTotal = { key: string; labelAr: string };

export type ReportGroup = 'sales' | 'purchases' | 'inventory' | 'accounting' | 'pos' | 'hrm' | 'projects' | 'marina';

export type ReportFilters = {
  from?: string;
  to?: string;
  /** ⏰ الوقت (HH:mm:ss) — `frmRptSalesInPeriod` builds a datetime from a date box + a time box. */
  fromTime?: string;
  toTime?: string;
  /** 🧾 نوع الفاتورة — «مبيعات نقطة البيع» · «مبيعات عادية» · «مبيعات» (`cmbInvType`). */
  invType?: string;
  /** 🔄 نوع العملية — «مبيعات» · «مرتجع» (`proc_type` 1 · 2). */
  procType?: string;
  /** 💵 حالة الدفع — «مدفوع» · «غير مدفوع» · «مدفوع جزئي» (`PaymentStatus` 1 · 0 · 2). */
  paymentStatus?: string;
  /** 💳 نوع الدفع — «نقدية» · «آجلة» · «شبكة» · «بنك» (`pay_type` 1 · −1 · 2). */
  payType?: string;
  /** 🧾 الضريبة — «مع ضريبة» · «بدون ضريبة». */
  vat?: string;
  /** 📄 نوع الإشعار — «إشعار دائن» · «إشعار مدين» (`inv_type` 21 · 22). */
  notificationType?: string;
  /** 📋 نوع التقرير — البعد الذي يُجمَّع عليه «تحليل المبيعات». */
  dimension?: string;
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
  /**
   * 💰 The number(s) under the grid — the summary cards of the `frmRpt*` windows:
   * «💵 إجمالي صافي البيع» و«📦 إجمالي الكميات» in `frmRptItemsSalesDetails`,
   * «💵 إجمالي صافي البيع» و«💰 إجمالي الربح» in `frmRptItemsProfit`. Summed from the
   * rows, so they always agree with what is on screen, and each may point at a hidden
   * signed column.
   */
  grandTotal?: ReportGrandTotal | ReportGrandTotal[];
  /** What an empty report says — `frmRptSalesInPeriod` says «لا توجد عمليات بالجدول». */
  emptyAr?: string;
  /** «أعده · راجعه · المدير» — the signature strip of `RptSalesInPeriod1/2.repx`. */
  signature?: boolean;
  chart?: 'bar' | 'line';
  build: (tenantId: string, filters: ReportFilters) => SQL;
};

const PERIOD: ReportParam[] = [
  { name: 'from', labelAr: 'من تاريخ', kind: 'date' },
  { name: 'to', labelAr: 'إلى تاريخ', kind: 'date' },
];
/** ⏰ الوقت (HH:mm:ss) — `frmRptSalesInPeriod.xaml` L235 وL246, next to each date box. */
const TIME: ReportParam[] = [
  { name: 'fromTime', labelAr: 'الوقت (HH:mm:ss)', kind: 'time' },
  { name: 'toTime', labelAr: 'الوقت (HH:mm:ss)', kind: 'time' },
];
/**
 * 🧾 نوع الفاتورة — `cmbInvType` L210: «مبيعات نقطة البيع» (index 0) و«مبيعات عادية»
 * (index 1). The desktop reads them as `inv.inv_type = 3` / `= 2`; here a فاتورة نقطة
 * البيع is the cash sale with no عميل (`party_id IS NULL`), which is what every other
 * POS report in this catalogue already reads.
 */
const INVOICE_KIND: ReportParam = {
  name: 'invType',
  labelAr: 'نوع الفاتورة',
  kind: 'select',
  options: [
    { value: 'pos', labelAr: 'مبيعات نقطة البيع' },
    { value: 'sale', labelAr: 'مبيعات عادية' },
  ],
};
/**
 * 🧾 نوع الفاتورة — `frmRptSalesByCategory.xaml.cs` L80 وL81 fill the combo with
 * «مبيعات» (`inv.inv_type = 2`) و«نقطة بيع» (`inv.inv_type = 3`).
 */
const INVOICE_KIND_SALES: ReportParam = {
  name: 'invType',
  labelAr: 'نوع الفاتورة',
  kind: 'select',
  options: [
    { value: 'sale', labelAr: 'مبيعات' },
    { value: 'pos', labelAr: 'نقطة بيع' },
  ],
};
/** 🧾 `frmRptCategorySaleByDay.xaml` L25 وL26 — «فاتورة مبيعات» · «فاتورة نقطة بيع». */
const INVOICE_KIND_DOCS: ReportParam = {
  name: 'invType',
  labelAr: 'نوع الفاتورة',
  kind: 'select',
  options: [
    { value: 'sale', labelAr: 'فاتورة مبيعات' },
    { value: 'pos', labelAr: 'فاتورة نقطة بيع' },
  ],
};
/** ⏰ «وقت البدء (HH:mm)» / «وقت الانتهاء (HH:mm)» — `frmRptItemsSalesDetails.xaml` L397 وL413. */
const TIME_START_END: ReportParam[] = [
  { name: 'fromTime', labelAr: 'وقت البدء (HH:mm)', kind: 'time' },
  { name: 'toTime', labelAr: 'وقت الانتهاء (HH:mm)', kind: 'time' },
];
/** ⏰ «من وقت (HH:mm)» / «إلى وقت (HH:mm)» — `frmRptItemsProfitDetails.xaml` L340 وL356. */
const TIME_FROM_TO: ReportParam[] = [
  { name: 'fromTime', labelAr: 'من وقت (HH:mm)', kind: 'time' },
  { name: 'toTime', labelAr: 'إلى وقت (HH:mm)', kind: 'time' },
];
/**
 * 🔄 نوع العملية — the ثلاثة أزرار `rbAllSales` · `rbSales` · `rbReturn` in every
 * invoice window: `proc_type` 1 (مبيعات) و2 (مرتجع).
 */
const OPERATION_KIND: ReportParam = {
  name: 'procType',
  labelAr: 'نوع العملية',
  kind: 'select',
  options: [
    { value: 'sale', labelAr: 'مبيعات' },
    { value: 'return', labelAr: 'مرتجع' },
  ],
};
/** 💵 حالة الدفع — `PaymentStatus` 1 مدفوع · 0 غير مدفوع · 2 مدفوع جزئي. */
const PAYMENT_STATE: ReportParam = {
  name: 'paymentStatus',
  labelAr: 'حالة الدفع',
  kind: 'select',
  options: [
    { value: 'paid', labelAr: 'مدفوع' },
    { value: 'unpaid', labelAr: 'غير مدفوع' },
    { value: 'partial', labelAr: 'مدفوع جزئي' },
  ],
};
/** 💳 نوع الدفع — «نقدية · آجلة · شبكة · بنك»: `pay_type` 1 · −1 · 2 · 2+bank. */
const PAY_METHOD: ReportParam = {
  name: 'payType',
  labelAr: 'نوع الدفع',
  kind: 'select',
  options: [
    { value: 'cash', labelAr: 'نقدية' },
    { value: 'credit', labelAr: 'آجلة' },
    { value: 'card', labelAr: 'شبكة' },
    { value: 'bank', labelAr: 'بنك' },
  ],
};
/** 🧾 الضريبة — `rbAllVat` · `rbWithVat` · `rbNoVAT`. */
const VAT_FILTER: ReportParam = {
  name: 'vat',
  labelAr: 'الضريبة',
  kind: 'select',
  options: [
    { value: 'with', labelAr: 'مع ضريبة' },
    { value: 'without', labelAr: 'بدون ضريبة' },
  ],
};
/** 📄 نوع الإشعار — `frmRptInvNotfic.xaml.cs` L237 وL239: `inv_type` 21 و22. */
const NOTIFICATION_KIND: ReportParam = {
  name: 'notificationType',
  labelAr: 'نوع الإشعار',
  kind: 'select',
  options: [
    { value: 'credit', labelAr: 'إشعار دائن' },
    { value: 'debit', labelAr: 'إشعار مدين' },
  ],
};
/**
 * 📋 نوع التقرير — the eight radios of `frmRptInvAnalysis.xaml` L254 … L300: المخزن ·
 * العميل · الصنف · مندوب البيع · المستخدم · الأيام · الشهور · مجموعة الصنف.
 */
const ANALYSIS_DIMENSION: ReportParam = {
  name: 'dimension',
  labelAr: 'نوع التقرير',
  kind: 'select',
  options: [
    { value: 'warehouse', labelAr: 'المخزن' },
    { value: 'customer', labelAr: 'العميل' },
    { value: 'item', labelAr: 'الصنف' },
    { value: 'salesman', labelAr: 'مندوب البيع' },
    { value: 'user', labelAr: 'المستخدم' },
    { value: 'day', labelAr: 'الأيام' },
    { value: 'month', labelAr: 'الشهور' },
    { value: 'category', labelAr: 'مجموعة الصنف' },
  ],
};
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

/**
 * 📅 التاريخ + ⏰ الوقت — `BuildDateTime` in `frmRptSalesInPeriod.xaml.cs` glues the date
 * box to the time box, defaulting to `00:00:00` at the start of the range and `23:59:59`
 * at its end. Reports that have no time box keep `onDate`.
 */
const onDateTime = (column: SQL, from?: string, to?: string, fromTime?: string, toTime?: string): SQL =>
  sql`${from ? sql`${column} >= ((${from}::date) + coalesce(${fromTime ?? null}::time, '00:00:00'::time))::timestamptz` : all}
  AND ${to ? sql`${column} <= ((${to}::date) + coalesce(${toTime ?? null}::time, '23:59:59'::time))::timestamptz` : all}`;

/**
 * 🧾 نوع الفاتورة — «مبيعات نقطة البيع» are the cash sales (`party_id IS NULL`);
 * «مبيعات عادية» are the ones with a عميل. No choice means both.
 */
/**
 * 🧾 POS or not — `inv_type=3` at the desktop. The cloud has no such column: a فاتورة
 * نقطة البيع is the cash sale with no عميل, which is what every other POS report here
 * already reads. `null` means "both".
 */
const posScope = (pos: boolean | null): SQL => (pos === null ? all : pos ? sql`si.party_id IS NULL` : sql`si.party_id IS NOT NULL`);

/**
 * 📦 The item-movement scope every تجميعي report shares: posted documents only —
 * `IS_Deleted=0` — and the filters of the 🔧 خيارات البحث panel.
 */
/**
 * The `opts` switches decide which of the 🔧 خيارات البحث boxes this window actually
 * owns — «أرباح المواد تفصيلي» has a مستودع box and a صنف box and neither a فرع nor a
 * مجموعة, so a shared scope has to be able to leave those two out.
 */
const movementLinesScope = (
  tenantId: string,
  f: ReportFilters,
  pos: boolean | null,
  opts: { invType?: boolean; branch?: boolean; category?: boolean } = {},
): SQL => sql`
  si.tenant_id = ${tenantId}
  AND si.status = 'posted'
  AND si.kind IN ('sale', 'sale_return')
  AND ${onDateTime(sql`si.posted_at`, f.from, f.to, f.fromTime, f.toTime)}
  AND ${posScope(pos)}
  AND ${opts.invType ? kindScope(f.invType) : all}
  AND ${opts.branch === false ? all : eqIf(sql`si.branch_id`, f.branchId)}
  AND ${eqIf(sql`si.warehouse_id`, f.warehouseId)}
  AND ${eqIf(sql`line.item_id`, f.itemId)}
  AND ${opts.category === false ? all : eqIf(sql`item.category_id`, f.categoryId)}
  AND ${eqIf(sql`si.party_id`, f.partyId)}
  AND ${eqIf(sql`si.salesman_id`, f.salesmanId)}
`;

/**
 * 🔄 نوع العملية — `proc_type` 1 (مبيعات) و2 (مرتجع) at the desktop, `kind` here: a بيع
 * adds to the 💰 ملخص and a مرتجع subtracts from it, which is also why the summary cards
 * read the signed columns rather than the ones on screen.
 */
const procScope = (saleKind: string, returnKind: string, procType?: string): SQL =>
  procType === 'sale'
    ? sql`si.kind = ${saleKind}`
    : procType === 'return'
      ? sql`si.kind = ${returnKind}`
      : sql`si.kind IN (${saleKind}, ${returnKind})`;
/** 💵 حالة الدفع — `inv.PaymentStatus` 1 · 0 · 2, and `payment_status` is spelled out. */
const paymentScope = (status?: string): SQL =>
  status === 'paid' || status === 'unpaid' || status === 'partial' ? sql`si.payment_status = ${status}` : all;
/** 💳 نوع الدفع — the four legs of `inv.pay_type`; «آجلة» is the فاتورة nothing was paid on. */
const payScope = (payType?: string): SQL =>
  payType === 'cash'
    ? sql`coalesce(pay.cash, 0) > 0`
    : payType === 'card'
      ? sql`coalesce(pay.card, 0) > 0`
      : payType === 'bank'
        ? sql`coalesce(pay.bank, 0) > 0`
        : payType === 'credit'
          ? sql`si.payment_status = 'unpaid'`
          : all;
/** 🧾 الضريبة — «مع ضريبة» و«بدون ضريبة» read the invoice's own tax, main and extra alike. */
const vatScope = (vat?: string, alias: SQL = sql`si`): SQL =>
  vat === 'with'
    ? sql`(${alias}.tax_total + ${alias}.extra_tax) > 0`
    : vat === 'without'
      ? sql`(${alias}.tax_total + ${alias}.extra_tax) = 0`
      : all;
/**
 * 💳 The three payment legs of one فاتورة — `inv.cash` · `inv.visa` · `inv.bank` at the
 * desktop, one row per method in `invoice_payments` here.
 */
const paymentLegs = sql`
  LEFT JOIN LATERAL (
    SELECT sum(CASE WHEN p.method = 'cash' THEN p.amount ELSE 0 END) AS cash,
           sum(CASE WHEN p.method = 'card' THEN p.amount ELSE 0 END) AS card,
           sum(CASE WHEN p.method = 'bank' THEN p.amount ELSE 0 END) AS bank
    FROM invoice_payments p
    WHERE p.tenant_id = si.tenant_id AND p.invoice_id = si.id
  ) pay ON true`;
/** «المجموع» — Σ(quantity × unit_price), the gross the خصم is measured against. */
const lineGross = sql`
  LEFT JOIN LATERAL (
    SELECT sum(l.quantity * l.unit_price) AS gross
    FROM sales_invoice_lines l
    WHERE l.tenant_id = si.tenant_id AND l.invoice_id = si.id
  ) lines ON true`;

/** 🧾 «المجموع» و«الخصم» … the one row of a فاتورة مبيعات, as every invoice window draws it. */
const invoiceSign = sql`(CASE WHEN si.kind IN ('sale', 'debit_note') THEN 1 ELSE -1 END)`;
const invoiceRow = sql`
  si.id::text AS movement_id,
  -- نوع الفاتورة — InvoiceOper.GetInvoiceTypeAr(inv_type, proc_type, pay_type, TaxType):
  -- TaxType is 2 when the عميل carries a رقم ضريبي and 1 when it does not.
  CASE si.kind
    WHEN 'sale' THEN CASE WHEN coalesce(party.tax_no, '') <> '' THEN 'فاتورة ضريبية' ELSE 'فاتورة ضريبية مبسطة' END
    WHEN 'sale_return' THEN CASE WHEN coalesce(party.tax_no, '') <> '' THEN 'إشعار دائن للفاتورة الضريبية' ELSE 'إشعار دائن للفاتورة الضريبية المبسطة' END
    WHEN 'credit_note' THEN 'إشعار دائن'
    WHEN 'debit_note' THEN 'إشعار مدين'
    ELSE si.kind END AS invoice_type,
  coalesce(si.number, '—') AS number,
  -- 🔗 رقم المرجع — inv.Reff_No at the desktop is a free-text box the cloud does
  -- not carry; what it does carry is the link itself (reference_invoice_id), so the
  -- number of the فاتورة this one refers to is what the column prints.
  coalesce((SELECT ref.number FROM sales_invoices ref WHERE ref.id = si.reference_invoice_id), '—') AS reference,
  si.posted_at::date AS day,
  to_char(si.posted_at, 'HH24:MI:SS') AS time,
  -- نوع الدفع — «آجل · نقدي · شبكة · متعدد · ضيافة» in GetPaymentText; ضيافة has no
  -- counterpart in the cloud's payments, so an unpaid فاتورة is «آجل» and a settled one
  -- is named after the leg that settled it, or «متعدد» when several did.
  CASE si.payment_status
    WHEN 'unpaid' THEN 'آجل'
    WHEN 'paid' THEN CASE
      WHEN coalesce(pay.cash, 0) > 0 AND coalesce(pay.card, 0) = 0 AND coalesce(pay.bank, 0) = 0 THEN 'نقدي'
      WHEN coalesce(pay.card, 0) > 0 AND coalesce(pay.cash, 0) = 0 AND coalesce(pay.bank, 0) = 0 THEN 'شبكة'
      ELSE 'متعدد' END
    ELSE 'متعدد' END AS payment_method,
  si.paid_total::text AS paid,
  coalesce(party.name, si.cash_customer_name, '—') AS customer,
  coalesce(pay.cash, 0)::text AS cash,
  coalesce(pay.card, 0)::text AS network,
  round(coalesce(lines.gross, 0), 2)::text AS sum_price,
  round(coalesce(lines.gross, 0) - si.subtotal, 2)::text AS discount,
  si.subtotal::text AS subtotal,
  si.tax_total::text AS tax,
  si.extra_tax::text AS extra_tax,
  (si.tax_total + si.extra_tax)::text AS total_tax,
  si.total::text AS net,
  coalesce(warehouse.name, '—') AS warehouse,
  coalesce(branch.name_ar, '—') AS branch,
  coalesce(salesman.name, '—') AS salesman,
  coalesce("user".full_name, '—') AS user_name,
  -- 📊 ملخص النتائج — the same ten numbers, signed: a مرتجع or an إشعار دائن subtracts.
  (${invoiceSign} * round(coalesce(lines.gross, 0), 2))::text AS s_sum_price,
  (${invoiceSign} * round(coalesce(lines.gross, 0) - si.subtotal, 2))::text AS s_discount,
  (${invoiceSign} * si.subtotal)::text AS s_subtotal,
  (${invoiceSign} * si.tax_total)::text AS s_tax,
  (${invoiceSign} * si.extra_tax)::text AS s_extra_tax,
  (${invoiceSign} * (si.tax_total + si.extra_tax))::text AS s_total_tax,
  (${invoiceSign} * si.total)::text AS s_net,
  (${invoiceSign} * coalesce(pay.cash, 0))::text AS s_cash,
  (${invoiceSign} * coalesce(pay.card, 0))::text AS s_network,
  (${invoiceSign} * si.paid_total)::text AS s_paid
`;

/** 🧾 The 21 columns of an invoice window — «📄 تفاصيل» و«👁️ عرض» and the four technical ones aside. */
const invoiceColumns = (typeLabel: string, numberLabel: string, dateLabel: string): ReportColumn[] => [
  text('invoice_type', typeLabel),
  text('number', numberLabel),
  text('reference', 'رقم المرجع'),
  date('day', dateLabel),
  text('time', 'الوقت'),
  text('payment_method', 'نوع الدفع'),
  money('paid', 'المدفوع'),
  text('customer', 'العميل'),
  money('cash', 'نقدي'),
  money('network', 'شبكة'),
  money('sum_price', 'المجموع'),
  money('discount', 'الخصم'),
  money('subtotal', 'الإجمالي'),
  money('tax', 'الضريبة'),
  money('extra_tax', 'ضريبة إضافية'),
  money('total_tax', 'إجمالي الضريبة'),
  money('net', 'الصافي'),
  text('warehouse', 'المستودع'),
  text('branch', 'الفرع'),
  text('salesman', 'المندوب'),
  text('user_name', 'المستخدم'),
  { key: 's_sum_price', labelAr: 'المجموع', type: 'money', hidden: true },
  { key: 's_discount', labelAr: 'الخصم', type: 'money', hidden: true },
  { key: 's_subtotal', labelAr: 'الإجمالي', type: 'money', hidden: true },
  { key: 's_tax', labelAr: 'الضريبة', type: 'money', hidden: true },
  { key: 's_extra_tax', labelAr: 'ضريبة إضافية', type: 'money', hidden: true },
  { key: 's_total_tax', labelAr: 'إجمالي الضريبة', type: 'money', hidden: true },
  { key: 's_net', labelAr: 'الصافي', type: 'money', hidden: true },
  { key: 's_cash', labelAr: 'نقدي', type: 'money', hidden: true },
  { key: 's_network', labelAr: 'شبكة', type: 'money', hidden: true },
  { key: 's_paid', labelAr: 'المدفوع', type: 'money', hidden: true },
];

/** 📊 ملخص النتائج — «المجموع · الخصم · الإجمالي · الضريبة · ضريبة إضافية · إجمالي الضريبة · الصافي · نقدي · شبكة» و«المدفوع» حيث يكون. */
const summaryCards = (withPaid: boolean): ReportGrandTotal[] => [
  { key: 's_sum_price', labelAr: 'المجموع' },
  { key: 's_discount', labelAr: 'الخصم' },
  { key: 's_subtotal', labelAr: 'الإجمالي' },
  { key: 's_tax', labelAr: 'الضريبة' },
  { key: 's_extra_tax', labelAr: 'ضريبة إضافية' },
  { key: 's_total_tax', labelAr: 'إجمالي الضريبة' },
  { key: 's_net', labelAr: 'الصافي' },
  { key: 's_cash', labelAr: 'نقدي' },
  { key: 's_network', labelAr: 'شبكة' },
  ...(withPaid ? [{ key: 's_paid', labelAr: 'المدفوع' } satisfies ReportGrandTotal] : []),
];

/** 📄 نوع الإشعار — `inv_type` 21 (مدين) و22 (دائن), `credit_note` و`debit_note` here. */
const notificationScope = (kind?: string): SQL =>
  kind === 'credit' ? sql`si.kind = 'credit_note'` : kind === 'debit' ? sql`si.kind = 'debit_note'` : all;

/**
 * 🔍 خيارات البحث of a فاتورة window — `showInvoice()` in `frmRptInvSalesDetails.xaml.cs`
 * L352 … L432, and the same ten boxes in the POS and الإشعارات windows.
 */
const invoiceScope = (
  tenantId: string,
  f: ReportFilters,
  pos: boolean | null,
  opts: { notifications?: boolean } = {},
): SQL => sql`
  si.tenant_id = ${tenantId}
  AND si.status = 'posted'
  AND si.kind IN ('sale', 'sale_return', 'credit_note', 'debit_note')
  AND ${onDateTime(sql`si.posted_at`, f.from, f.to, f.fromTime, f.toTime)}
  AND ${posScope(pos)}
  -- 📄 نوع الفاتورة — «مبيعات» (inv_type = 2) و«نقطة بيع» (inv_type = 3) in cmbInvType;
  -- the POS window hard-codes 3 already, so the box is its own.
  AND ${pos === null ? kindScope(f.invType) : all}
  AND ${opts.notifications ? sql`si.kind IN ('credit_note', 'debit_note')` : sql`si.kind IN ('sale', 'sale_return')`}
  AND ${opts.notifications ? notificationScope(f.notificationType) : procScope('sale', 'sale_return', f.procType)}
  AND ${eqIf(sql`si.branch_id`, f.branchId)}
  AND ${eqIf(sql`si.warehouse_id`, f.warehouseId)}
  AND ${eqIf(sql`si.party_id`, f.partyId)}
  AND ${eqIf(sql`si.salesman_id`, f.salesmanId)}
  AND ${paymentScope(f.paymentStatus)}
  AND ${payScope(f.payType)}
  AND ${vatScope(f.vat)}
`;

/** 📅 اسم اليوم بالعربية — `ToString("ddd", culture ar)` in the two «حسب اليوم» windows. */
const dayName = sql`
  CASE extract(dow FROM si.posted_at::date)::int
    WHEN 0 THEN 'الأحد' WHEN 1 THEN 'الاثنين' WHEN 2 THEN 'الثلاثاء'
    WHEN 3 THEN 'الأربعاء' WHEN 4 THEN 'الخميس' WHEN 5 THEN 'الجمعة'
    ELSE 'السبت' END`;

/** 🔄 `proc_type` 1 شراء و2 مردود شراء في `frmRptInvPurchaseDetails`. */
const purchaseProcScope = (procType?: string): SQL =>
  procType === 'sale' ? sql`pi.kind = 'purchase'` : procType === 'return' ? sql`pi.kind = 'purchase_return'` : all;
const purchaseSign = sql`(CASE WHEN pi.kind = 'purchase' THEN 1 ELSE -1 END)`;
const purchaseInvoiceScope = (tenantId: string, f: ReportFilters): SQL => sql`
  pi.tenant_id = ${tenantId}
  AND pi.status = 'posted'
  AND pi.kind IN ('purchase', 'purchase_return')
  AND ${onDateTime(sql`pi.posted_at`, f.from, f.to, f.fromTime, f.toTime)}
  AND ${eqIf(sql`pi.branch_id`, f.branchId)}
  AND ${eqIf(sql`pi.warehouse_id`, f.warehouseId)}
  AND ${eqIf(sql`pi.party_id`, f.partyId)}
  AND ${purchaseProcScope(f.procType)}
  AND ${vatScope(f.vat, sql`pi`)}
`;

/** 🏢 One arm of «تقرير الحركة اليومية» — `DoProcess(name, type1, type2)` in frmRptDailyProcess. */
const dailyScope = (tenantId: string, f: ReportFilters, kind: string, withParty: boolean): SQL => sql`
  si.tenant_id = ${tenantId}
  AND si.status = 'posted'
  AND si.kind = ${kind}
  AND ${onDateTime(sql`si.posted_at`, f.from, f.to, f.fromTime, f.toTime)}
  AND ${withParty ? sql`si.party_id IS NOT NULL` : sql`si.party_id IS NULL`}
  AND ${eqIf(sql`si.branch_id`, f.branchId)}
`;
const dailyPurchaseScope = (tenantId: string, f: ReportFilters, kind: string): SQL => sql`
  pi.tenant_id = ${tenantId}
  AND pi.status = 'posted'
  AND pi.kind = ${kind}
  AND ${onDateTime(sql`pi.posted_at`, f.from, f.to, f.fromTime, f.toTime)}
  AND ${eqIf(sql`pi.branch_id`, f.branchId)}
`;

/** 📋 نوع التقرير — the dimension every «تحليل المبيعات» row is grouped by. */
const analysisDimension = (dimension?: string): SQL =>
  dimension === 'customer'
    ? sql`coalesce(party.name, si.cash_customer_name, '—')`
    : dimension === 'item'
      ? sql`coalesce(item.name_ar, '—')`
      : dimension === 'salesman'
        ? sql`coalesce(salesman.name, '—')`
        : dimension === 'user'
          ? sql`coalesce("user".full_name, '—')`
          : dimension === 'day'
            ? sql`si.posted_at::date::text`
            : dimension === 'month'
              ? sql`to_char(si.posted_at, 'YYYY-MM')`
              : dimension === 'category'
                ? sql`coalesce(cat.name_ar, '—')`
                : sql`coalesce(warehouse.name, '—')`;

const purchaseLinesScope = (tenantId: string, f: ReportFilters): SQL => sql`
  pi.tenant_id = ${tenantId}
  AND pi.status = 'posted'
  AND pi.kind IN ('purchase', 'purchase_return')
  AND ${onDateTime(sql`pi.posted_at`, f.from, f.to, f.fromTime, f.toTime)}
  AND ${eqIf(sql`pi.branch_id`, f.branchId)}
  AND ${eqIf(sql`pi.warehouse_id`, f.warehouseId)}
  AND ${eqIf(sql`line.item_id`, f.itemId)}
  AND ${eqIf(sql`item.category_id`, f.categoryId)}
`;

const kindScope = (invType?: string): SQL =>
  invType === 'pos' ? sql`si.party_id IS NULL` : invType === 'sale' ? sql`si.party_id IS NOT NULL` : all;
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

/**
 * ⛵ «حركة المبيعات» — `Form_WPF/frmRptSalesInPeriod.xaml.cs`: both tabs read the same
 * documents (`inv_type` 3/2 through `cmbInvType`, `proc_type` 1 بيع / 2 مرتجع,
 * `Proc_Type<>3 AND Proc_Type<>4`, `IS_Buy=0`, `IS_Deleted=0`), only grouped differently.
 */
const movementScope = (tenantId: string, f: ReportFilters): SQL => sql`
  si.tenant_id = ${tenantId}
  AND si.status = 'posted'
  AND si.kind IN ('sale', 'sale_return')
  AND ${onDateTime(sql`si.posted_at`, f.from, f.to, f.fromTime, f.toTime)}
  AND ${eqIf(sql`si.branch_id`, f.branchId)}
  AND ${kindScope(f.invType)}
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

  {
    key: 'sales-movement-items',
    titleAr: 'حركة المبيعات — إجمالي المبيعات',
    group: 'sales',
    hintAr: '«📊 إجمالي المبيعات» من `frmRptSalesInPeriod`: كل صنفٍ بكميّته الصافية وإجماليه الصافي (المبيعات − المرتجع)، وصنفٌ لم يُبع أصلاً لا يظهر.',
    params: [PERIOD[0]!, TIME[0]!, PERIOD[1]!, TIME[1]!, INVOICE_KIND, BRANCH],
    columns: [text('item_code', 'رقم الصنف'), text('item_name', 'الصنف'), qty('quantity', 'الكمية'), money('total', 'الإجمالي')],
    // 💰 إجمالي المبيعات — `txtSumSale` = Σ(الإجمالي الصافي) كما يجمعها `CalcStock`.
    grandTotal: { key: 'total', labelAr: 'إجمالي المبيعات' },
    emptyAr: 'لا توجد عمليات بالجدول',
    signature: true,
    build: (tenantId, f) => sql`
      SELECT coalesce(item.sku, '—') AS item_code, coalesce(item.name_ar, '—') AS item_name,
             sum(CASE WHEN si.kind = 'sale' THEN line.quantity ELSE -line.quantity END)::text AS quantity,
             round(sum(CASE WHEN si.kind = 'sale' THEN line.total ELSE -line.total END), 2)::text AS total
      FROM sales_invoice_lines line
      JOIN sales_invoices si ON si.id = line.invoice_id
      LEFT JOIN items item ON item.id = line.item_id
      WHERE ${movementScope(tenantId, f)}
      GROUP BY item.id, item.sku, item.name_ar
      -- if (qty == 0.0) continue; — an صنف with no بيع at all is not a row, even when it was returned.
      HAVING sum(CASE WHEN si.kind = 'sale' THEN line.quantity ELSE 0 END) <> 0
      ORDER BY item.id LIMIT 2000`,
  },
  {
    key: 'sales-movement-invoices',
    titleAr: 'حركة المبيعات — عرض الفواتير',
    group: 'sales',
    hintAr: '«🧾 عرض الفواتير» من `frmRptSalesInPeriod`: فاتورةٌ بيع أو مرتجع بسطر، بوقتها ونقدها وشبكتها ومجاميعها الأربعة؛ و«آجل» علامة `pay_type = -1`.',
    params: [PERIOD[0]!, TIME[0]!, PERIOD[1]!, TIME[1]!, INVOICE_KIND, BRANCH],
    columns: [
      text('movement_no', 'رقم الحركة'),
      text('number', 'رقم الفاتورة'),
      text('kind_name', 'نوع الفاتورة'),
      date('day', 'التاريخ'),
      text('time', 'الوقت'),
      text('postponed', 'آجل'),
      money('cash', 'نقدي'),
      money('network', 'شبكة'),
      money('subtotal', 'الإجمالي'),
      money('tax', 'الضريبة'),
      money('discount', 'الخصم'),
      money('net', 'الصافي'),
      // المبيعات ناقص المردودات — what «💰 إجمالي المبيعات:» prints (`_sumSale2`).
      { key: 'net_signed', labelAr: 'صافي الحركة', type: 'money', hidden: true },
    ],
    grandTotal: { key: 'net_signed', labelAr: 'إجمالي المبيعات' },
    emptyAr: 'لا توجد عمليات بالجدول',
    signature: true,
    build: (tenantId, f) => sql`
      SELECT si.id::text AS movement_no, coalesce(si.number, '—') AS number,
             CASE si.kind WHEN 'sale' THEN 'بيع' WHEN 'sale_return' THEN 'مرتجع' ELSE si.kind END AS kind_name,
             si.posted_at::date AS day, to_char(si.posted_at, 'HH24:MI:SS') AS time,
             CASE WHEN si.payment_status = 'unpaid' THEN 'نعم' ELSE '—' END AS postponed,
             coalesce(paid.cash, 0)::text AS cash, coalesce(paid.network, 0)::text AS network,
             si.subtotal::text AS subtotal, si.tax_total::text AS tax,
             si.invoice_discount::text AS discount, si.total::text AS net,
             (CASE WHEN si.kind = 'sale' THEN si.total ELSE -si.total END)::text AS net_signed
      FROM sales_invoices si
      LEFT JOIN (
        SELECT p.invoice_id,
               sum(CASE WHEN p.method = 'cash' THEN p.amount ELSE 0 END) AS cash,
               sum(CASE WHEN p.method = 'card' THEN p.amount ELSE 0 END) AS network
        FROM invoice_payments p
        WHERE p.tenant_id = ${tenantId}
        GROUP BY p.invoice_id
      ) paid ON paid.invoice_id = si.id
      WHERE ${movementScope(tenantId, f)}
      ORDER BY si.posted_at, si.number LIMIT 2000`,
  },
  {
    key: 'items-sales-summary',
    titleAr: 'مبيعات الأصناف تجميعي',
    group: 'sales',
    hintAr: 'كل صنفٍ بكميّته الصافية وصافي بيعه (`OperType = 1`): المبيعات والمردودات معاً، نقطة البيع والبيع العادي معاً.',
    // 🏪 المستودع · 🗂️ المجموعة · 📦 الصنف · 🏢 الفرع · 📅 الفترة الزمنية — the order of the
    // 🔧 خيارات البحث panel itself (L263 … L413).
    params: [WAREHOUSE, CATEGORY, ITEM, BRANCH, PERIOD[0]!, TIME_START_END[0]!, PERIOD[1]!, TIME_START_END[1]!],
    columns: [
      text('item_code', 'رمز الصنف'),
      text('item_name', 'الصنف'),
      text('category', 'المجموعة'),
      qty('quantity', 'الكمية'),
      money('net_sales', 'صافي البيع'),
    ],
    totals: ['quantity', 'net_sales'],
    grandTotal: [
      { key: 'net_sales', labelAr: 'إجمالي صافي البيع' },
      { key: 'quantity', labelAr: 'إجمالي الكميات' },
    ],
    signature: true,
    build: (tenantId, f) => sql`
      SELECT coalesce(item.sku, '—') AS item_code, coalesce(item.name_ar, '—') AS item_name,
             coalesce(cat.name_ar, '—') AS category,
             sum(CASE WHEN si.kind = 'sale' THEN line.quantity ELSE -line.quantity END)::text AS quantity,
             round(sum(CASE WHEN si.kind = 'sale' THEN line.total ELSE -line.total END), 2)::text AS net_sales
      FROM sales_invoice_lines line
      JOIN sales_invoices si ON si.id = line.invoice_id
      LEFT JOIN items item ON item.id = line.item_id
      LEFT JOIN item_categories cat ON cat.id = item.category_id
      WHERE ${movementLinesScope(tenantId, f, null)}
      GROUP BY item.id, item.sku, item.name_ar, cat.name_ar
      -- «لا حركة → تجاهل» — if (!hasMovement) continue; in frmRptItemsSalesDetails.xaml.cs L282.
      HAVING sum(line.quantity) <> 0
      ORDER BY item_name LIMIT 2000`,
  },
  {
    key: 'items-pos-sales-summary',
    titleAr: 'مبيعات الأصناف تجميعي - نقطة البيع',
    group: 'sales',
    hintAr: 'النافذة نفسها مقيَّدة بـ`inv.inv_type=3`: فواتير نقطة البيع وحدها (`PosVal − PosRetVal`).',
    // 👤 المستخدم (مؤجَّل) و📅 الفترة الزمنية وحدها — that panel has no مستودع ولا مجموعة.
    params: [PERIOD[0]!, TIME_START_END[0]!, PERIOD[1]!, TIME_START_END[1]!],
    columns: [
      text('item_code', 'رمز الصنف'),
      text('item_name', 'الصنف'),
      text('category', 'المجموعة'),
      qty('quantity', 'الكمية'),
      money('net_sales', 'صافي البيع'),
    ],
    totals: ['quantity', 'net_sales'],
    grandTotal: [
      { key: 'net_sales', labelAr: 'إجمالي صافي البيع' },
      { key: 'quantity', labelAr: 'إجمالي الكميات' },
    ],
    signature: true,
    build: (tenantId, f) => sql`
      SELECT coalesce(item.sku, '—') AS item_code, coalesce(item.name_ar, '—') AS item_name,
             coalesce(cat.name_ar, '—') AS category,
             sum(CASE WHEN si.kind = 'sale' THEN line.quantity ELSE -line.quantity END)::text AS quantity,
             round(sum(CASE WHEN si.kind = 'sale' THEN line.total ELSE -line.total END), 2)::text AS net_sales
      FROM sales_invoice_lines line
      JOIN sales_invoices si ON si.id = line.invoice_id
      LEFT JOIN items item ON item.id = line.item_id
      LEFT JOIN item_categories cat ON cat.id = item.category_id
      WHERE ${movementLinesScope(tenantId, f, true)}
      GROUP BY item.id, item.sku, item.name_ar, cat.name_ar
      -- «لا حركة → تجاهل» — if (!hasMovement) continue; in frmRptItemsSalesDetailsPOS.xaml.cs L223.
      HAVING sum(line.quantity) <> 0
      ORDER BY item_name LIMIT 2000`,
  },
  {
    key: 'items-profit-summary',
    titleAr: 'أرباح المواد تجميعي',
    group: 'sales',
    hintAr: '«صافي البيع» بعد خصم السطر والخصم الموزَّع من رأس الفاتورة، و«الربح» = صافي البيع − إجمالي التكلفة، و«نسبة الربح» = الربح ÷ التكلفة × 100.',
    // 🏪 المستودع · 🗂️ المجموعة · 📦 الصنف · 📅 الفترة الزمنية (من · حتى, no time box).
    params: [WAREHOUSE, CATEGORY, ITEM, { name: 'from', labelAr: 'من', kind: 'date' } satisfies ReportParam, { name: 'to', labelAr: 'حتى', kind: 'date' } satisfies ReportParam],
    columns: [
      text('item_code', 'رمز المادة'),
      text('item_name', 'المادة'),
      qty('quantity', 'الكمية'),
      money('total_cost', 'متوسط التكلفة'),
      money('net_sales', 'صافي البيع'),
      money('profit', 'الربح'),
      percent('profit_ratio', 'نسبة الربح'),
    ],
    totals: ['quantity', 'total_cost', 'net_sales', 'profit'],
    grandTotal: [
      { key: 'net_sales', labelAr: 'إجمالي صافي البيع' },
      { key: 'profit', labelAr: 'إجمالي الربح' },
    ],
    signature: true,
    build: (tenantId, f) => sql`
      SELECT coalesce(item.sku, '—') AS item_code, coalesce(item.name_ar, '—') AS item_name,
             sum(signed.line_qty)::text AS quantity,
             round(sum(signed.line_cost), 2)::text AS total_cost,
             -- «صافي البيع»: صافي السطر بعد خصم السطر وبعد نصيبه من خصم رأس الفاتورة.
             -- The desktop does that distribution in the report itself —
             -- SUM(ROUND((ItemPriceWithoutVAT * minus / NULLIF(InvSum,0)),2)) in
             -- frmRptItemsProfit.GetSaleData — while the cloud distributes it at save
             -- time: calculateInvoiceTotals spreads invoice_discount pro-rata by gross
             -- into every line.net, which is why the report reads line.net as it stands.
             round(sum(signed.line_net), 2)::text AS net_sales,
             round(sum(signed.line_net) - sum(signed.line_cost), 2)::text AS profit,
             round(
               CASE WHEN sum(signed.line_cost) = 0 THEN 0
                    ELSE (sum(signed.line_net) - sum(signed.line_cost)) / sum(signed.line_cost) * 100 END,
               2)::text AS profit_ratio
      FROM sales_invoice_lines line
      JOIN sales_invoices si ON si.id = line.invoice_id
      LEFT JOIN items item ON item.id = line.item_id
      LEFT JOIN item_categories cat ON cat.id = item.category_id
      -- One row per line, already signed: proc_type 1 (بيع) موجب و2 (مرتجع) سالب، لنمطي
      -- البيع ونقطة البيع معاً — r1 − r2 + r3 − r4 in ShowResults.
      CROSS JOIN LATERAL (
        SELECT CASE WHEN si.kind = 'sale' THEN line.quantity ELSE -line.quantity END AS line_qty,
               CASE WHEN si.kind = 'sale' THEN line.net ELSE -line.net END AS line_net,
               CASE WHEN si.kind = 'sale' THEN line.cost_total ELSE -line.cost_total END AS line_cost
      ) signed
      WHERE ${movementLinesScope(tenantId, f, null)}
      GROUP BY item.id, item.sku, item.name_ar
      -- «تجاهل الصنف إذا لم تكن له حركة» — frmRptItemsProfit.xaml.cs L220: a item with any
      -- of the four quantities non-zero is a row, even when the net comes out at zero.
      HAVING sum(line.quantity) <> 0
      ORDER BY item_name LIMIT 2000`,
  },
  {
    key: 'items-purchases-summary',
    titleAr: 'مشتريات الأصناف تجميعي',
    group: 'purchases',
    hintAr: 'النافذة نفسها بـ`OperType = 2`: المشتريات ناقص مردوداتها (`PurchVal − RePurchVal`).',
    params: [WAREHOUSE, CATEGORY, ITEM, BRANCH, PERIOD[0]!, TIME_START_END[0]!, PERIOD[1]!, TIME_START_END[1]!],
    columns: [
      text('item_code', 'رمز الصنف'),
      text('item_name', 'الصنف'),
      text('category', 'المجموعة'),
      qty('quantity', 'الكمية'),
      money('net_purchases', 'صافي الشراء'),
    ],
    totals: ['quantity', 'net_purchases'],
    grandTotal: [
      { key: 'net_purchases', labelAr: 'إجمالي صافي الشراء' },
      { key: 'quantity', labelAr: 'إجمالي الكميات' },
    ],
    signature: true,
    build: (tenantId, f) => sql`
      SELECT coalesce(item.sku, '—') AS item_code, coalesce(item.name_ar, '—') AS item_name,
             coalesce(cat.name_ar, '—') AS category,
             sum(CASE WHEN pi.kind = 'purchase' THEN line.quantity ELSE -line.quantity END)::text AS quantity,
             round(sum(CASE WHEN pi.kind = 'purchase' THEN line.total ELSE -line.total END), 2)::text AS net_purchases
      FROM purchase_invoice_lines line
      JOIN purchase_invoices pi ON pi.id = line.invoice_id
      LEFT JOIN items item ON item.id = line.item_id
      LEFT JOIN item_categories cat ON cat.id = item.category_id
      WHERE ${purchaseLinesScope(tenantId, f)}
      GROUP BY item.id, item.sku, item.name_ar, cat.name_ar
      -- The desktop shares the loop with the sales window, so its hasMovement test still
      -- reads the sale columns even when OperType = 2; the cloud keeps the sane predicate.
      HAVING sum(line.quantity) <> 0
      ORDER BY item_name LIMIT 2000`,
  },
  {
    key: 'items-profit-details',
    titleAr: 'أرباح المواد تفصيلي',
    group: 'sales',
    hintAr: 'سطرٌ لكل حركة: الفاتورة وتاريخها ونوعها ومستودعها، ثم المادة وكميتها وتكلفتها وسعرها ومجموعها وخصمها وربحها ونسبته.',
    // 🏭 المستودع · 📦 الصنف · 📅 الفترة (من تاريخ · من وقت · إلى تاريخ · إلى وقت).
    params: [WAREHOUSE, ITEM, PERIOD[0]!, TIME_FROM_TO[0]!, PERIOD[1]!, TIME_FROM_TO[1]!],
    columns: [
      text('number', 'الرقم'),
      date('day', 'التاريخ'),
      text('operation', 'نوع العملية'),
      text('warehouse', 'المستودع'),
      text('item_code', 'رمز المادة'),
      text('item_name', 'المادة'),
      text('unit', 'الوحدة'),
      qty('quantity', 'الكمية'),
      money('unit_cost', 'متوسط التكلفة'),
      money('total_cost', 'إجمالي التكلفة'),
      money('unit_price', 'السعر'),
      money('gross', 'المجموع'),
      money('net', 'الإجمالي'),
      money('discount', 'الخصم'),
      money('profit', 'الربح'),
      percent('profit_ratio', 'نسبة الربح %'),
    ],
    totals: ['quantity', 'total_cost', 'gross', 'net', 'discount', 'profit'],
    // 📊 ملخص الأرباح — «إجمالي التكلفة · المجموع · الإجمالي · الخصم · 💹 إجمالي الربح»
    // (L655 … L716); «عدد السجلات» هو عدد صفوف الشبكة الذي يطبعه الرأس أصلاً.
    grandTotal: [
      { key: 'total_cost', labelAr: 'إجمالي التكلفة' },
      { key: 'gross', labelAr: 'المجموع' },
      { key: 'net', labelAr: 'الإجمالي' },
      { key: 'discount', labelAr: 'الخصم' },
      { key: 'profit', labelAr: 'إجمالي الربح' },
    ],
    emptyAr: 'لا توجد عمليات بالجدول',
    signature: true,
    build: (tenantId, f) => sql`
      SELECT coalesce(si.number, '—') AS number, si.posted_at::date AS day,
             CASE si.kind WHEN 'sale' THEN 'بيع' WHEN 'sale_return' THEN 'مرتجع' ELSE si.kind END AS operation,
             coalesce(wh.name, '—') AS warehouse,
             coalesce(item.sku, '—') AS item_code, coalesce(item.name_ar, '—') AS item_name,
             coalesce(unit.name_ar, '—') AS unit,
             signed.qty::text AS quantity,
             -- DgvAvgCost / DgvTotAvg — the cost of the حركة divided by its كمية, and the
             -- cost of the whole line (val * AvrgCost at the desktop).
             round(signed.cost / nullif(signed.qty, 0), 2)::text AS unit_cost,
             round(signed.cost, 2)::text AS total_cost,
             line.unit_price::text AS unit_price,
             round(signed.gross, 2)::text AS gross, round(signed.net, 2)::text AS net,
             round(signed.gross - signed.net, 2)::text AS discount,
             round(signed.net - signed.cost, 2)::text AS profit,
             round(CASE WHEN signed.cost = 0 THEN 0 ELSE (signed.net - signed.cost) / signed.cost * 100 END, 2)::text AS profit_ratio
      FROM sales_invoice_lines line
      JOIN sales_invoices si ON si.id = line.invoice_id
      LEFT JOIN items item ON item.id = line.item_id
      LEFT JOIN units_of_measure unit ON unit.id = item.base_unit_id
      LEFT JOIN warehouses wh ON wh.id = si.warehouse_id
      -- proc_type 1 (بيع) موجب و2 (مرتجع) سالب — val1 · val · AvrgCost · exchange_price
      -- and ((val1*exchange_price)/InvSum)*minus in frmRptItemsProfitDetails.xaml.cs.
      CROSS JOIN LATERAL (
        SELECT CASE WHEN si.kind = 'sale' THEN line.quantity ELSE -line.quantity END AS qty,
               CASE WHEN si.kind = 'sale' THEN line.cost_total ELSE -line.cost_total END AS cost,
               CASE WHEN si.kind = 'sale' THEN line.quantity * line.unit_price ELSE -line.quantity * line.unit_price END AS gross,
               CASE WHEN si.kind = 'sale' THEN line.net ELSE -line.net END AS net
      ) signed
      WHERE ${movementLinesScope(tenantId, f, null, { branch: false, category: false })}
        -- AND Inv_Sub.ItemId > 0 — a وصف line with no صنف is not a row of a أرباح المواد report.
        AND line.item_id IS NOT NULL
      ORDER BY si.posted_at DESC, si.number DESC LIMIT 2000`,
  },
  {
    // The cloud already had a `sales-by-category` («مبيعات بحسب الفئة», one row per فئة with
    // a bar chart); this one is the desktop's tree of أصناف under their مجموعات.
    key: 'items-sales-by-category',
    titleAr: 'تقرير مبيعات الأصناف حسب المجموعة',
    group: 'sales',
    hintAr: 'كل صنفٍ تحت مجموعته: إجمالي كميّته وإجماليه وضريبته وصافيه وخصمه؛ ومجاميع المجموعات في البطاقات.',
    // 📄 نوع الفاتورة · 📅 الفترة (من تاريخ / إلى تاريخ) · 📂 المجموعة · 🏬 الفرع — the
    // 👤 المستخدم and 🧑‍💼 المندوب boxes are deferred (§5.5).
    params: [INVOICE_KIND_SALES, PERIOD[0]!, PERIOD[1]!, CATEGORY, BRANCH],
    columns: [
      text('category', 'المجموعة'),
      text('item_name', 'اسم المجموعة / الصنف'),
      text('item_code', 'الرمز'),
      qty('quantity', 'إجمالي الكمية'),
      money('total', 'الإجمالي'),
      money('tax', 'الضريبة'),
      money('net', 'الصافي'),
      money('discount', 'الخصم'),
    ],
    totals: ['quantity', 'total', 'tax', 'net', 'discount'],
    // «إجمالي الكمية · الإجمالي · الضريبة · الصافي» — lblTotQty · lblTotTotal · lblTotVat · lblTotNet.
    grandTotal: [
      { key: 'quantity', labelAr: 'إجمالي الكمية' },
      { key: 'total', labelAr: 'الإجمالي' },
      { key: 'tax', labelAr: 'الضريبة' },
      { key: 'net', labelAr: 'الصافي' },
    ],
    signature: true,
    build: (tenantId, f) => sql`
      SELECT coalesce(cat.name_ar, '—') AS category,
             coalesce(item.name_ar, '—') AS item_name, coalesce(item.sku, '—') AS item_code,
             sum(signed.qty)::text AS quantity,
             round(sum(signed.net), 2)::text AS total,
             -- The desktop hard-codes 15% (total * 0.15 and total * 1.15); the cloud reads
             -- the tax the line was actually posted with.
             round(sum(signed.tax), 2)::text AS tax,
             round(sum(signed.total), 2)::text AS net,
             round(sum(signed.gross - signed.net), 2)::text AS discount
      FROM sales_invoice_lines line
      JOIN sales_invoices si ON si.id = line.invoice_id
      LEFT JOIN items item ON item.id = line.item_id
      LEFT JOIN item_categories cat ON cat.id = item.category_id
      -- proc_type 1 (بيع) minus proc_type 2 (مرتجع) — the two UNION ALL halves of the
      -- item query in frmRptSalesByCategory.xaml.cs L203 … L240.
      CROSS JOIN LATERAL (
        SELECT CASE WHEN si.kind = 'sale' THEN line.quantity ELSE -line.quantity END AS qty,
               CASE WHEN si.kind = 'sale' THEN line.net ELSE -line.net END AS net,
               CASE WHEN si.kind = 'sale' THEN line.tax ELSE -line.tax END AS tax,
               CASE WHEN si.kind = 'sale' THEN line.total ELSE -line.total END AS total,
               CASE WHEN si.kind = 'sale' THEN line.quantity * line.unit_price ELSE -line.quantity * line.unit_price END AS gross
      ) signed
      WHERE ${movementLinesScope(tenantId, f, null, { invType: true })}
        AND line.item_id IS NOT NULL
      GROUP BY cat.id, cat.name_ar, item.id, item.sku, item.name_ar
      HAVING sum(line.quantity) <> 0
      ORDER BY cat.name_ar, item.name_ar LIMIT 2000`,
  },
  {
    key: 'category-sales-by-day',
    titleAr: 'تقرير المبيعات اليومية للمجموعة',
    group: 'sales',
    hintAr: 'كل مجموعةٍ في يوم: رمزها واسمها واسم اليوم وتاريخها وإجمالي مبيعاتها، ثم «إجمالي المبيعات» تحت الشبكة.',
    // 📄 نوع الفاتورة · 🏢 الفرع · 📦 المجموعة · 📅 من / إلى — the order of the panel (L21 … L92).
    params: [INVOICE_KIND_DOCS, BRANCH, CATEGORY, PERIOD[0]!, PERIOD[1]!],
    columns: [
      text('category_code', 'الرمز'),
      text('category', 'المجموعة'),
      text('day_name', 'اليوم'),
      date('day', 'التاريخ'),
      money('total', 'الإجمالي'),
    ],
    totals: ['total'],
    // «إجمالي المبيعات:» — lblTotal in frmRptCategorySaleByDay.xaml L385.
    grandTotal: { key: 'total', labelAr: 'إجمالي المبيعات' },
    emptyAr: 'لا توجد عمليات بالجدول',
    signature: true,
    build: (tenantId, f) => sql`
      SELECT coalesce(cat.code, '—') AS category_code, coalesce(cat.name_ar, '—') AS category,
             -- SaleDay — parsedDate.ToString("ddd", culture ar) in the .xaml.cs L201.
             ${dayName} AS day_name,
             si.posted_at::date AS day,
             round(sum(CASE WHEN si.kind = 'sale' THEN line.total ELSE -line.total END), 2)::text AS total
      FROM sales_invoice_lines line
      JOIN sales_invoices si ON si.id = line.invoice_id
      LEFT JOIN items item ON item.id = line.item_id
      LEFT JOIN item_categories cat ON cat.id = item.category_id
      WHERE ${movementLinesScope(tenantId, f, null, { invType: true })}
        AND line.item_id IS NOT NULL
      GROUP BY cat.id, cat.code, cat.name_ar, si.posted_at::date
      ORDER BY si.posted_at::date DESC, cat.name_ar LIMIT 2000`,
  },
  {
    key: 'sales-invoices-details',
    titleAr: 'تقرير فواتير المبيعات',
    group: 'sales',
    hintAr: 'كل فاتورةٍ بيعٍ أو مرتجع بسطر: نوعها ورقمها وتاريخها ووقتها وطريقة دفعها وعميلها ونقدها وشبكتها ومجاميعها السبعة ومستودعها وفرعها ومندوبها ومستخدمها.',
    // 🔍 خيارات البحث — 📄 نوع الفاتورة · 💳 نوع الدفع · 🏭 المستودع · 🧑‍💼 المندوب ·
    // 👥 العميل · 🏬 الفرع · 📅 الفترة الزمنية · 🧾 الضريبة · 💵 حالة الدفع · 🔄 نوع العملية.
    params: [INVOICE_KIND_SALES, PAY_METHOD, WAREHOUSE, SALESMAN, PARTY, BRANCH, PERIOD[0]!, TIME_FROM_TO[0]!, PERIOD[1]!, TIME_FROM_TO[1]!, VAT_FILTER, PAYMENT_STATE, OPERATION_KIND],
    columns: invoiceColumns('نوع الفاتورة', 'رقم الفاتورة', 'تاريخ الفاتورة'),
    // 📊 ملخص النتائج — «عدد الفواتير · المجموع · الخصم · الإجمالي · الضريبة · ضريبة
    // إضافية · إجمالي الضريبة · الصافي · نقدي · شبكة · المدفوع» (L1023 … L1110); عدد
    // الفواتير هو عدد سطور الشبكة الذي يطبعه رأس الصفحة أصلاً.
    grandTotal: summaryCards(true),
    emptyAr: 'لا توجد عمليات بالجدول',
    signature: true,
    build: (tenantId, f) => sql`
      SELECT ${invoiceRow}
      FROM sales_invoices si
      LEFT JOIN parties party ON party.id = si.party_id
      LEFT JOIN warehouses warehouse ON warehouse.id = si.warehouse_id
      LEFT JOIN branches branch ON branch.id = si.branch_id
      LEFT JOIN salesmen salesman ON salesman.id = si.salesman_id
      LEFT JOIN users "user" ON "user".id = si.created_by
      ${paymentLegs}
      ${lineGross}
      WHERE ${invoiceScope(tenantId, f, null)}
      ORDER BY si.posted_at DESC, si.number DESC LIMIT 2000`,
  },
  {
    key: 'pos-sales-invoices-details',
    titleAr: 'تقرير مبيعات الفواتير',
    group: 'sales',
    hintAr: 'الشبكة نفسها مقيَّدة بـ`inv.inv_type=3`: فواتير نقطة البيع ومردوداتها وحدها.',
    // 🔍 خيارات البحث عند الديسكتوب: 🔄 نوع العملية · 💵 حالة الدفع · 🏭 المستودع ·
    // 👤 المستخدم · 📅 الفترة الزمنية (المستخدم مؤجَّل: لا مرشِّح عضوية بعد).
    params: [OPERATION_KIND, PAYMENT_STATE, WAREHOUSE, PERIOD[0]!, TIME_FROM_TO[0]!, PERIOD[1]!, TIME_FROM_TO[1]!],
    columns: invoiceColumns('نوع الفاتورة', 'رقم الفاتورة', 'تاريخ الفاتورة'),
    grandTotal: summaryCards(false),
    emptyAr: 'لا توجد عمليات بالجدول',
    signature: true,
    build: (tenantId, f) => sql`
      SELECT ${invoiceRow}
      FROM sales_invoices si
      LEFT JOIN parties party ON party.id = si.party_id
      LEFT JOIN warehouses warehouse ON warehouse.id = si.warehouse_id
      LEFT JOIN branches branch ON branch.id = si.branch_id
      LEFT JOIN salesmen salesman ON salesman.id = si.salesman_id
      LEFT JOIN users "user" ON "user".id = si.created_by
      ${paymentLegs}
      ${lineGross}
      WHERE ${invoiceScope(tenantId, f, true)}
      ORDER BY si.posted_at DESC, si.number DESC LIMIT 2000`,
  },
  {
    key: 'sales-notifications',
    titleAr: 'تقرير الإشعارات',
    group: 'sales',
    hintAr: 'كل إشعارٍ دائن أو مدين بسطر: رقمه وتاريخه ووقته وعميله ونقده وشبكته ومجاميعه، ثم «عدد الإشعارات» ومجاميع الأسفل.',
    // 📄 نوع الإشعار · 🔄 نوع العملية · 👤 المستخدم · 👤 المندوب · 🏢 العميل · 📅 من / إلى.
    params: [NOTIFICATION_KIND, OPERATION_KIND, SALESMAN, PARTY, PERIOD[0]!, TIME_FROM_TO[0]!, PERIOD[1]!, TIME_FROM_TO[1]!],
    columns: invoiceColumns('نوع الإشعار', 'رقم الإشعار', 'تاريخ الإشعار'),
    grandTotal: summaryCards(false),
    emptyAr: 'لا توجد عمليات بالجدول',
    signature: true,
    build: (tenantId, f) => sql`
      SELECT ${invoiceRow}
      FROM sales_invoices si
      LEFT JOIN parties party ON party.id = si.party_id
      LEFT JOIN warehouses warehouse ON warehouse.id = si.warehouse_id
      LEFT JOIN branches branch ON branch.id = si.branch_id
      LEFT JOIN salesmen salesman ON salesman.id = si.salesman_id
      LEFT JOIN users "user" ON "user".id = si.created_by
      ${paymentLegs}
      ${lineGross}
      WHERE ${invoiceScope(tenantId, f, null, { notifications: true })}
      ORDER BY si.posted_at DESC, si.number DESC LIMIT 2000`,
  },
  {
    key: 'purchase-invoices-details',
    titleAr: 'تفاصيل فواتير المشتريات',
    group: 'purchases',
    hintAr: 'كل فاتورة شراء أو مردود بسطر: نوعها ورقمها وتاريخها وموردها ومجاميعها الخمسة ومستودعها وفرعها، ثم «المدفوع» و«المتبقي».',
    // 💰 الضريبة · 🔄 نوع العملية · 🏬 الفرع · 🏢 المورد · 🏪 المستودع · 📅 الفترة.
    params: [VAT_FILTER, OPERATION_KIND, BRANCH, PARTY, WAREHOUSE, PERIOD[0]!, TIME_FROM_TO[0]!, PERIOD[1]!, TIME_FROM_TO[1]!],
    columns: [
      text('invoice_type', 'نوع الفاتورة'),
      text('number', 'رقم الفاتورة'),
      text('reference', 'رقم المرجع'),
      date('day', 'التاريخ'),
      text('time', 'الوقت'),
      text('payment_method', 'نوع الدفع'),
      text('supplier', 'المورد'),
      money('sum_price', 'المجموع'),
      money('discount', 'الخصم'),
      money('subtotal', 'الإجمالي'),
      money('tax', 'الضريبة'),
      money('net', 'الصافي'),
      money('paid', 'المدفوع'),
      money('due', 'المتبقي'),
      text('warehouse', 'المستودع'),
      text('branch', 'الفرع'),
      text('user_name', 'المستخدم'),
      // 💰 مجاميع الفواتير — netted the way CalculateSummary nets them (proc_type 1 − 2).
      { key: 's_sum_price', labelAr: 'المجموع', type: 'money', hidden: true },
      { key: 's_discount', labelAr: 'الخصم', type: 'money', hidden: true },
      { key: 's_subtotal', labelAr: 'الإجمالي', type: 'money', hidden: true },
      { key: 's_tax', labelAr: 'الضريبة', type: 'money', hidden: true },
      { key: 's_net', labelAr: 'الصافي', type: 'money', hidden: true },
    ],
    grandTotal: [
      { key: 's_sum_price', labelAr: 'المجموع' },
      { key: 's_discount', labelAr: 'الخصم' },
      { key: 's_subtotal', labelAr: 'الإجمالي' },
      { key: 's_tax', labelAr: 'الضريبة' },
      { key: 's_net', labelAr: 'الصافي' },
    ],
    signature: true,
    build: (tenantId, f) => sql`
      SELECT CASE pi.kind WHEN 'purchase' THEN 'فاتورة مشتريات' WHEN 'purchase_return' THEN 'مردود مشتريات' ELSE pi.kind END AS invoice_type,
             coalesce(pi.number, '—') AS number, pi.posted_at::date AS day, to_char(pi.posted_at, 'HH24:MI:SS') AS time,
             coalesce(pi.supplier_reference_no, (SELECT ref.number FROM purchase_invoices ref WHERE ref.id = pi.reference_invoice_id), '—') AS reference,
             CASE pi.payment_status
               WHEN 'unpaid' THEN 'آجل'
               WHEN 'paid' THEN CASE
                 WHEN coalesce(alloc.cash, 0) > 0 AND coalesce(alloc.card, 0) = 0 AND coalesce(alloc.bank, 0) = 0 THEN 'نقدي'
                 WHEN coalesce(alloc.card, 0) > 0 AND coalesce(alloc.cash, 0) = 0 AND coalesce(alloc.bank, 0) = 0 THEN 'شبكة'
                 ELSE 'متعدد' END
               ELSE 'متعدد' END AS payment_method,
             ${partyName} AS supplier,
             round(coalesce(lines.gross, 0), 2)::text AS sum_price,
             round(coalesce(lines.gross, 0) - pi.subtotal, 2)::text AS discount,
             pi.subtotal::text AS subtotal, pi.tax_total::text AS tax, pi.total::text AS net,
             pi.paid_total::text AS paid, (pi.total - pi.paid_total)::text AS due,
             coalesce(warehouse.name, '—') AS warehouse, coalesce(branch.name_ar, '—') AS branch,
             coalesce(\"user\".full_name, '—') AS user_name,
             (${purchaseSign} * round(coalesce(lines.gross, 0), 2))::text AS s_sum_price,
             (${purchaseSign} * round(coalesce(lines.gross, 0) - pi.subtotal, 2))::text AS s_discount,
             (${purchaseSign} * pi.subtotal)::text AS s_subtotal,
             (${purchaseSign} * pi.tax_total)::text AS s_tax,
             (${purchaseSign} * pi.total)::text AS s_net
      FROM purchase_invoices pi
      LEFT JOIN parties party ON party.id = pi.party_id
      LEFT JOIN warehouses warehouse ON warehouse.id = pi.warehouse_id
      LEFT JOIN branches branch ON branch.id = pi.branch_id
      LEFT JOIN users \"user\" ON \"user\".id = pi.created_by
      LEFT JOIN LATERAL (
        SELECT sum(l.quantity * l.unit_price) AS gross
        FROM purchase_invoice_lines l
        WHERE l.tenant_id = pi.tenant_id AND l.invoice_id = pi.id
      ) lines ON true
      -- 💳 نوع الدفع — inv.pay_type at the desktop is one flag on the فاتورة; the
      -- cloud settles a شراء بسند صرف through payment_allocations, so the legs of
      -- the سند are summed the same way the three of inv.cash/visa/bank are.
      LEFT JOIN LATERAL (
        SELECT sum(CASE WHEN v.method = 'cash' THEN a.amount ELSE 0 END) AS cash,
               sum(CASE WHEN v.method = 'card' THEN a.amount ELSE 0 END) AS card,
               sum(CASE WHEN v.method = 'bank' THEN a.amount ELSE 0 END) AS bank
        FROM payment_allocations a
        LEFT JOIN vouchers v ON v.id = a.voucher_id
        WHERE a.tenant_id = pi.tenant_id AND a.invoice_id = pi.id
      ) alloc ON true
      WHERE ${purchaseInvoiceScope(tenantId, f)}
      ORDER BY pi.posted_at DESC, pi.number DESC LIMIT 2000`,
  },
  {
    key: 'daily-sales',
    titleAr: 'تقرير مبيعات حسب اليوم',
    group: 'sales',
    hintAr: 'كل يومٍ بسطر: الإجمالي قبل الضريبة وضريبته وإجماليه، واسم اليوم بالعربية كما يسمّيه `ToString("ddd", ar)`.',
    // 📄 نوع الفاتورة · 🏢 الفرع · 📅 من / إلى.
    params: [INVOICE_KIND_DOCS, BRANCH, PERIOD[0]!, PERIOD[1]!],
    columns: [
      int('seq', 'الرقم'),
      date('day', 'التاريخ'),
      text('day_name', 'اليوم'),
      money('before_tax', 'الإجمالي قبل الضريبة'),
      money('tax', 'الضريبة'),
      money('total', 'الإجمالي'),
    ],
    totals: ['before_tax', 'tax', 'total'],
    // «إجمالي قبل الضريبة:» · «إجمالي الضريبة:» · «الإجمالي الكلي:»
    grandTotal: [
      { key: 'before_tax', labelAr: 'إجمالي قبل الضريبة' },
      { key: 'tax', labelAr: 'إجمالي الضريبة' },
      { key: 'total', labelAr: 'الإجمالي الكلي' },
    ],
    emptyAr: 'لا توجد بيانات، أدخل الفترة الزمنية الصحيحة',
    signature: true,
    build: (tenantId, f) => sql`
      SELECT row_number() OVER (ORDER BY daily.day)::text AS seq, daily.*
      FROM (
        SELECT si.posted_at::date AS day, ${dayName} AS day_name,
               round(sum(CASE WHEN si.kind = 'sale' THEN si.subtotal ELSE -si.subtotal END), 2)::text AS before_tax,
               round(sum(CASE WHEN si.kind = 'sale' THEN si.tax_total ELSE -si.tax_total END), 2)::text AS tax,
               round(sum(CASE WHEN si.kind = 'sale' THEN si.total ELSE -si.total END), 2)::text AS total
        FROM sales_invoices si
        WHERE ${movementScope(tenantId, f)}
        GROUP BY si.posted_at::date
      ) daily
      ORDER BY day LIMIT 2000`,
  },
  {
    key: 'daily-process',
    titleAr: 'تقرير الحركة اليومية',
    group: 'sales',
    hintAr: 'ستة أنواع حركة بستة أسطر: عدد فواتير كل نوع وإجماليه، وما منه نقديّ وما منه آجل.',
    // 🏢 الفرع · 📅 من / إلى + وقت — 🏦 الصندوق و👤 المستخدم مؤجَّلان (§6.5).
    params: [BRANCH, PERIOD[0]!, TIME_FROM_TO[0]!, PERIOD[1]!, TIME_FROM_TO[1]!],
    columns: [
      text('operation', 'نوع العملية'),
      int('invoices', 'عدد الفواتير'),
      money('total', 'الإجمالي'),
      money('cash', 'نقدي'),
      money('credit', 'آجل'),
      { key: 'sort', labelAr: 'الترتيب', type: 'int', hidden: true },
    ],
    totals: ['invoices', 'total', 'cash', 'credit'],
    signature: true,
    build: (tenantId, f) => sql`
      SELECT operation, invoices, total, cash, credit, sort FROM (
        SELECT 1 AS sort, 'مبيعات' AS operation, count(*)::text AS invoices,
               round(coalesce(sum(si.total), 0), 2)::text AS total,
               round(coalesce(sum(CASE WHEN si.payment_status <> 'unpaid' THEN si.total ELSE 0 END), 0), 2)::text AS cash,
               round(coalesce(sum(CASE WHEN si.payment_status = 'unpaid' THEN si.total ELSE 0 END), 0), 2)::text AS credit
        FROM sales_invoices si WHERE ${dailyScope(tenantId, f, 'sale', true)}
        UNION ALL
        SELECT 2, 'مرتجع مبيعات', count(*)::text,
               round(coalesce(sum(si.total), 0), 2)::text,
               round(coalesce(sum(CASE WHEN si.payment_status <> 'unpaid' THEN si.total ELSE 0 END), 0), 2)::text,
               round(coalesce(sum(CASE WHEN si.payment_status = 'unpaid' THEN si.total ELSE 0 END), 0), 2)::text
        FROM sales_invoices si WHERE ${dailyScope(tenantId, f, 'sale_return', true)}
        UNION ALL
        SELECT 3, 'نقطة البيع', count(*)::text,
               round(coalesce(sum(si.total), 0), 2)::text,
               round(coalesce(sum(CASE WHEN si.payment_status <> 'unpaid' THEN si.total ELSE 0 END), 0), 2)::text,
               round(coalesce(sum(CASE WHEN si.payment_status = 'unpaid' THEN si.total ELSE 0 END), 0), 2)::text
        FROM sales_invoices si WHERE ${dailyScope(tenantId, f, 'sale', false)}
        UNION ALL
        SELECT 4, 'مرتجع نقطة البيع', count(*)::text,
               round(coalesce(sum(si.total), 0), 2)::text,
               round(coalesce(sum(CASE WHEN si.payment_status <> 'unpaid' THEN si.total ELSE 0 END), 0), 2)::text,
               round(coalesce(sum(CASE WHEN si.payment_status = 'unpaid' THEN si.total ELSE 0 END), 0), 2)::text
        FROM sales_invoices si WHERE ${dailyScope(tenantId, f, 'sale_return', false)}
        UNION ALL
        SELECT 5, 'مشتريات', count(*)::text,
               round(coalesce(sum(pi.total), 0), 2)::text,
               round(coalesce(sum(CASE WHEN pi.payment_status <> 'unpaid' THEN pi.total ELSE 0 END), 0), 2)::text,
               round(coalesce(sum(CASE WHEN pi.payment_status = 'unpaid' THEN pi.total ELSE 0 END), 0), 2)::text
        FROM purchase_invoices pi WHERE ${dailyPurchaseScope(tenantId, f, 'purchase')}
        UNION ALL
        SELECT 6, 'مرتجع مشتريات', count(*)::text,
               round(coalesce(sum(pi.total), 0), 2)::text,
               round(coalesce(sum(CASE WHEN pi.payment_status <> 'unpaid' THEN pi.total ELSE 0 END), 0), 2)::text,
               round(coalesce(sum(CASE WHEN pi.payment_status = 'unpaid' THEN pi.total ELSE 0 END), 0), 2)::text
        FROM purchase_invoices pi WHERE ${dailyPurchaseScope(tenantId, f, 'purchase_return')}
      ) movements
      ORDER BY sort LIMIT 2000`,
  },
  {
    // The cloud already had a sales-analysis («أفضل الأصناف مبيعاً», one row per صنف with
    // a share of the total); this one is the desktop's eight-way تحليل of frmRptInvAnalysis.
    key: 'sales-inv-analysis',
    titleAr: 'تقرير تحليل المبيعات',
    group: 'sales',
    hintAr: 'صافي الكمية والإجمالي والخصم والتكلفة والربح مجمَّعةً على البعد الذي تختاره (المخزن · العميل · الصنف · المندوب · المستخدم · اليوم · الشهر · مجموعة الصنف)، ونسبتان من الإجمالي العام.',
    // 📋 نوع التقرير · 👤 المستخدم · 🧑‍💼 العميل · 📦 مجموعة الصنف · 🤝 مندوب البيع ·
    // 🏭 المستودع · 📅 الفترة — 🏦 الصندوق مؤجَّل (§6.5).
    params: [ANALYSIS_DIMENSION, PARTY, CATEGORY, SALESMAN, WAREHOUSE, PERIOD[0]!, PERIOD[1]!],
    columns: [
      text('dimension', 'البعد'),
      int('invoices', 'فواتير'),
      qty('net_quantity', 'صافي الكمية'),
      money('net_total', 'صافي الإجمالي'),
      money('net_discount', 'صافي الخصم'),
      money('net_cost', 'صافي التكلفة'),
      money('net_profit', 'صافي الربح'),
      percent('profit_to_cost', 'نسبة الربح للتكلفة'),
      percent('share_of_sales', 'نسبة الاجمالي لإجمالي البيع'),
      percent('share_of_profit', 'نسبة الربح لإجمالي الربح'),
    ],
    totals: ['invoices', 'net_quantity', 'net_total', 'net_discount', 'net_cost', 'net_profit'],
    // 📦 صافي الكمية · 💰 صافي الإجمالي · 🏷️ صافي الخصم · 💳 صافي التكلفة · 📈 صافي الربح
    grandTotal: [
      { key: 'net_quantity', labelAr: 'صافي الكمية' },
      { key: 'net_total', labelAr: 'صافي الإجمالي' },
      { key: 'net_discount', labelAr: 'صافي الخصم' },
      { key: 'net_cost', labelAr: 'صافي التكلفة' },
      { key: 'net_profit', labelAr: 'صافي الربح' },
    ],
    signature: true,
    build: (tenantId, f) => sql`
      SELECT ${analysisDimension(f.dimension)} AS dimension,
             count(DISTINCT si.id)::text AS invoices,
             sum(signed.qty)::text AS net_quantity,
             round(sum(signed.net), 2)::text AS net_total,
             round(sum(signed.gross - signed.net), 2)::text AS net_discount,
             round(sum(signed.cost), 2)::text AS net_cost,
             round(sum(signed.net) - sum(signed.cost), 2)::text AS net_profit,
             round(CASE WHEN sum(signed.cost) = 0 THEN 0 ELSE (sum(signed.net) - sum(signed.cost)) / sum(signed.cost) * 100 END, 2)::text AS profit_to_cost,
             round(sum(signed.net) / nullif(sum(sum(signed.net)) OVER (), 0) * 100, 2)::text AS share_of_sales,
             round((sum(signed.net) - sum(signed.cost)) / nullif(sum(sum(signed.net) - sum(signed.cost)) OVER (), 0) * 100, 2)::text AS share_of_profit
      FROM sales_invoice_lines line
      JOIN sales_invoices si ON si.id = line.invoice_id
      LEFT JOIN items item ON item.id = line.item_id
      LEFT JOIN item_categories cat ON cat.id = item.category_id
      LEFT JOIN parties party ON party.id = si.party_id
      LEFT JOIN salesmen salesman ON salesman.id = si.salesman_id
      LEFT JOIN warehouses warehouse ON warehouse.id = si.warehouse_id
      LEFT JOIN users "user" ON "user".id = si.created_by
      CROSS JOIN LATERAL (
        SELECT CASE WHEN si.kind = 'sale' THEN line.quantity ELSE -line.quantity END AS qty,
               CASE WHEN si.kind = 'sale' THEN line.net ELSE -line.net END AS net,
               CASE WHEN si.kind = 'sale' THEN line.quantity * line.unit_price ELSE -line.quantity * line.unit_price END AS gross,
               CASE WHEN si.kind = 'sale' THEN line.cost_total ELSE -line.cost_total END AS cost
      ) signed
      WHERE ${movementLinesScope(tenantId, f, null)}
        AND line.item_id IS NOT NULL
      GROUP BY ${analysisDimension(f.dimension)}
      ORDER BY net_total DESC LIMIT 2000`,
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
    key: 'production-orders',
    titleAr: 'تقرير أمر الإنتاج',
    group: 'inventory',
    hintAr: 'أوامر الإنتاج المنفَّذة: تكلفة المكونات المستهلكة وتكلفة وحدة المنتج الناتج. الأمر محايد محاسبياً — القيمة الخارجة من المستودع هي نفسها الداخلة إليه.',
    params: [...PERIOD, WAREHOUSE, ITEM],
    columns: [date('order_date', 'التاريخ'), text('number', 'رقم الأمر'), text('item', 'المنتج'), text('warehouse', 'المستودع'), qty('output_qty', 'الكمية المنتجة'), money('component_cost', 'تكلفة المكونات'), money('unit_cost', 'تكلفة الوحدة'), int('components', 'عدد المكونات'), text('status', 'الحالة')],
    totals: ['output_qty', 'component_cost'],
    build: (tenantId, f) => sql`
      SELECT po.order_date, po.number, ${itemName} AS item, coalesce(wh.name, '—') AS warehouse,
             po.output_qty::text, po.component_cost::text, po.unit_cost::text,
             (SELECT count(*) FROM production_order_components c WHERE c.order_id = po.id)::text AS components,
             po.status
      FROM production_orders po
      LEFT JOIN items item ON item.id = po.output_item_id
      LEFT JOIN warehouses wh ON wh.id = po.warehouse_id
      WHERE po.tenant_id = ${tenantId} AND po.status <> 'cancelled'
        AND ${onDate(sql`po.order_date`, f.from, f.to)}
        AND ${eqIf(sql`po.warehouse_id`, f.warehouseId)}
        AND ${eqIf(sql`po.output_item_id`, f.itemId)}
      ORDER BY po.order_date DESC, po.number DESC LIMIT 1000`,
  },
  {
    key: 'contracting-returns',
    titleAr: 'مرتجعات المقاولات',
    group: 'projects',
    hintAr: 'الأعمال التي أُعيدت من مستخلصات مرحَّلة، وقيمة الإشعار الدائن المقابل بعد ردّ المحتجز.',
    params: [...PERIOD],
    columns: [date('return_date', 'التاريخ'), text('number', 'رقم المرتجع'), text('project', 'المشروع'), text('bill', 'المستخلص'), money('return_value', 'قيمة المرتجع'), money('retention_value', 'المحتجز المردود'), money('net_value', 'صافي الإشعار'), text('status', 'الحالة')],
    totals: ['return_value', 'retention_value', 'net_value'],
    build: (tenantId, f) => sql`
      SELECT r.return_date, r.number, p.name AS project, coalesce(bill.number, '—') AS bill,
             r.return_value::text, r.retention_value::text, r.net_value::text, r.status
      FROM contracting_returns r
      JOIN projects p ON p.id = r.project_id
      LEFT JOIN progress_bills bill ON bill.id = r.bill_id
      WHERE r.tenant_id = ${tenantId} AND r.status <> 'cancelled'
        AND ${onDate(sql`r.return_date`, f.from, f.to)}
      ORDER BY r.return_date DESC LIMIT 1000`,
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
  {
    key: 'purchase-notes',
    titleAr: 'تقرير إشعارات المشتريات',
    group: 'purchases',
    hintAr: 'الإشعارات الدائنة والمدينة الصادرة على فواتير موردين مرحّلة.',
    params: [...PERIOD, BRANCH],
    columns: [date('day', 'التاريخ'), text('number', 'رقم الإشعار'), text('kind', 'النوع'), text('invoice', 'الفاتورة'), text('party', 'المورد'), text('reason', 'السبب'), money('amount', 'المبلغ'), text('status', 'الحالة')],
    totals: ['amount'],
    build: (tenantId, f) => sql`
      SELECT note.created_at::date AS day, coalesce(note.number, '—') AS number,
             CASE note.kind WHEN 'credit' THEN 'إشعار دائن' WHEN 'debit' THEN 'إشعار مدين' ELSE note.kind END AS kind,
             coalesce(pi.number, '—') AS invoice, ${partyName} AS party, note.reason, note.amount::text, note.status
      FROM purchase_adjustment_notes note
      LEFT JOIN purchase_invoices pi ON pi.id = note.invoice_id
      LEFT JOIN parties party ON party.id = pi.party_id
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
