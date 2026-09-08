import { Inject, Injectable } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { sql } from 'drizzle-orm';
import qrcode from 'qrcode-generator';
import { DomainError } from '@erp/contracts';
import { withTenantTx, type DatabaseHandle } from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';

import { amountInArabicWords } from './tafqeet.js';

/**
 * Printable documents.
 *
 * Everything a customer or an auditor ever holds in their hand is produced here: the
 * sales invoice, the purchase invoice, the receipt and payment vouchers, the journal
 * voucher and the daily shift close. They share one A4 stylesheet so a company that
 * prints all six recognises them as one set of papers.
 *
 * The HTML is self-contained on purpose — no external CSS, fonts or images — because it
 * is opened inside an iframe in the browser, saved as a file, or handed to a PDF printer,
 * and none of those can be trusted to fetch anything.
 */
@Injectable()
export class PrintTemplatesService {
  constructor(@Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle) {}

  // ------------------------------------------------------------------ documents

  async salesInvoice(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const company = await this.company(tx, tenantId);
      const invoice = first(
        await tx.execute(sql`
          SELECT i.*, b.name_ar AS branch_name, b.code AS branch_code, b.phone AS branch_phone,
                 p.name AS party_name, p.code AS party_code, p.tax_no AS party_tax_no, p.phone AS party_phone, p.address AS party_address,
                 s.name AS salesman_name, w.name AS warehouse_name
            FROM sales_invoices i
            LEFT JOIN branches b ON b.id = i.branch_id
            LEFT JOIN parties p ON p.id = i.party_id
            LEFT JOIN salesmen s ON s.id = i.salesman_id
            LEFT JOIN warehouses w ON w.id = i.warehouse_id
           WHERE i.tenant_id = ${tenantId} AND i.id = ${id}
        `),
      );
      if (!invoice) throw new DomainError('NOT_FOUND', 'Invoice was not found', 404);

      const lines = rows(
        await tx.execute(sql`
          SELECT l.*, COALESCE(l.description, it.name_ar) AS label, it.sku, u.code AS unit_code
            FROM sales_invoice_lines l
            LEFT JOIN items it ON it.id = l.item_id
            LEFT JOIN units_of_measure u ON u.id = it.base_unit_id
           WHERE l.tenant_id = ${tenantId} AND l.invoice_id = ${id}
           ORDER BY l.line_no
        `),
      );
      const payments = rows(
        await tx.execute(sql`
          SELECT method, amount, reference FROM invoice_payments WHERE tenant_id = ${tenantId} AND invoice_id = ${id} ORDER BY created_at
        `),
      );

      const kindTitle = SALES_KIND_TITLES[str(invoice.kind)] ?? 'فاتورة مبيعات';
      const title = `${kindTitle}${str(invoice.tax_total) !== '0.0000' || company.taxNo ? ' ضريبية' : ''}`;
      return this.page({
        title: `${kindTitle} ${str(invoice.number) || ''}`.trim(),
        body: `
          ${this.header(company, {
            docTitle: title,
            docTitleEn: SALES_KIND_TITLES_EN[str(invoice.kind)] ?? 'Sales Invoice',
            number: str(invoice.number) || '—',
            date: dateText(invoice.posted_at ?? invoice.created_at),
            status: STATUS_LABELS[str(invoice.status)] ?? str(invoice.status),
            extra: [
              ['الفرع', str(invoice.branch_name)],
              ['المستودع', str(invoice.warehouse_name)],
              ['المندوب', str(invoice.salesman_name)],
            ],
          })}
          ${this.partyBlock('بيانات العميل', {
            name: str(invoice.party_name) || str(invoice.cash_customer_name) || 'عميل نقدي',
            code: str(invoice.party_code),
            taxNo: str(invoice.party_tax_no),
            phone: str(invoice.party_phone) || str(invoice.cash_customer_mobile),
            address: invoice.party_address,
          })}
          ${this.linesTable(lines)}
          ${this.totalsBlock({
            currency: str(invoice.currency) || 'SAR',
            subtotal: str(invoice.subtotal),
            discount: str(invoice.invoice_discount),
            tax: str(invoice.tax_total),
            extraTax: str(invoice.extra_tax),
            withholding: str(invoice.withholding),
            total: str(invoice.total),
            paid: str(invoice.paid_total),
            payments: payments.map((row) => ({ method: PAYMENT_METHODS[str(row.method)] ?? str(row.method), amount: str(row.amount), reference: str(row.reference) })),
            qr: str(invoice.zatca_qr),
            zatcaStatus: str(invoice.zatca_status),
          })}
          ${this.signatures(['المستلم', 'المحاسب', 'الختم'])}
        `,
      });
    });
  }

  async purchaseInvoice(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const company = await this.company(tx, tenantId);
      const invoice = first(
        await tx.execute(sql`
          SELECT i.*, b.name_ar AS branch_name, w.name AS warehouse_name,
                 p.name AS party_name, p.code AS party_code, p.tax_no AS party_tax_no, p.phone AS party_phone, p.address AS party_address
            FROM purchase_invoices i
            LEFT JOIN branches b ON b.id = i.branch_id
            LEFT JOIN warehouses w ON w.id = i.warehouse_id
            LEFT JOIN parties p ON p.id = i.party_id
           WHERE i.tenant_id = ${tenantId} AND i.id = ${id}
        `),
      );
      if (!invoice) throw new DomainError('NOT_FOUND', 'Invoice was not found', 404);

      const lines = rows(
        await tx.execute(sql`
          SELECT l.*, COALESCE(l.description, it.name_ar) AS label, it.sku, u.code AS unit_code
            FROM purchase_invoice_lines l
            LEFT JOIN items it ON it.id = l.item_id
            LEFT JOIN units_of_measure u ON u.id = it.base_unit_id
           WHERE l.tenant_id = ${tenantId} AND l.invoice_id = ${id}
           ORDER BY l.line_no
        `),
      );

      const kindTitle = str(invoice.kind) === 'return' ? 'مردود مشتريات' : 'فاتورة مشتريات';
      return this.page({
        title: `${kindTitle} ${str(invoice.number) || ''}`.trim(),
        body: `
          ${this.header(company, {
            docTitle: kindTitle,
            docTitleEn: str(invoice.kind) === 'return' ? 'Purchase Return' : 'Purchase Invoice',
            number: str(invoice.number) || '—',
            date: dateText(invoice.posted_at ?? invoice.created_at),
            status: STATUS_LABELS[str(invoice.status)] ?? str(invoice.status),
            extra: [
              ['الفرع', str(invoice.branch_name)],
              ['المستودع', str(invoice.warehouse_name)],
              ['مرجع المورد', str(invoice.supplier_reference_no)],
            ],
          })}
          ${this.partyBlock('بيانات المورد', {
            name: str(invoice.party_name) || '—',
            code: str(invoice.party_code),
            taxNo: str(invoice.party_tax_no),
            phone: str(invoice.party_phone),
            address: invoice.party_address,
          })}
          ${this.linesTable(lines)}
          ${this.totalsBlock({
            currency: str(invoice.currency) || 'SAR',
            subtotal: str(invoice.subtotal),
            discount: str(invoice.invoice_discount),
            tax: str(invoice.tax_total),
            extraTax: str(invoice.extra_tax),
            withholding: str(invoice.withholding),
            additional: str(invoice.additional_cost_total),
            total: str(invoice.total),
            paid: str(invoice.paid_total),
            payments: [],
          })}
          ${this.signatures(['المورد', 'أمين المستودع', 'المحاسب'])}
        `,
      });
    });
  }

  async voucher(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const company = await this.company(tx, tenantId);
      const voucher = first(
        await tx.execute(sql`
          SELECT v.*, b.name_ar AS branch_name, p.name AS party_name, p.code AS party_code,
                 c.name AS cash_location_name, a.code AS counter_code, a.name_ar AS counter_name,
                 cc.code AS cost_center_code, cc.name_ar AS cost_center_name
            FROM vouchers v
            LEFT JOIN branches b ON b.id = v.branch_id
            LEFT JOIN parties p ON p.id = v.party_id
            LEFT JOIN cash_locations c ON c.id = v.cash_location_id
            LEFT JOIN accounts a ON a.id = v.counter_account_id
            LEFT JOIN cost_centers cc ON cc.id = v.cost_center_id
           WHERE v.tenant_id = ${tenantId} AND v.id = ${id}
        `),
      );
      if (!voucher) throw new DomainError('NOT_FOUND', 'Voucher was not found', 404);

      const isReceipt = str(voucher.kind) === 'receipt';
      const currency = str(voucher.currency) || 'SAR';
      const voucherAmount = str(voucher.amount);
      const label = isReceipt ? 'سند قبض' : 'سند صرف';
      const counterparty = str(voucher.party_name) || str(voucher.recipient) || '—';

      return this.page({
        title: `${label} ${str(voucher.number) || ''}`.trim(),
        body: `
          ${this.header(company, {
            docTitle: label,
            docTitleEn: isReceipt ? 'Receipt Voucher' : 'Payment Voucher',
            number: str(voucher.number) || '—',
            date: dateText(voucher.date),
            status: STATUS_LABELS[str(voucher.status)] ?? str(voucher.status),
            extra: [
              ['الفرع', str(voucher.branch_name)],
              ['الصندوق / البنك', str(voucher.cash_location_name)],
              ['طريقة الدفع', PAYMENT_METHODS[str(voucher.method)] ?? str(voucher.method)],
            ],
          })}
          <section class="panel">
            <div class="kv"><span>${isReceipt ? 'استلمنا من السيد' : 'صرفنا إلى السيد'}</span><b>${escapeHtml(counterparty)}</b></div>
            <div class="kv"><span>مبلغاً وقدره</span><b>${escapeHtml(money(voucherAmount))} ${escapeHtml(currency)}</b></div>
            <div class="kv words"><span>فقط</span><b>${escapeHtml(amountInArabicWords(voucherAmount, currency))}</b></div>
            <div class="kv"><span>وذلك عن</span><b>${escapeHtml(str(voucher.counter_name) || VOUCHER_SUBTYPES[str(voucher.subtype)] || str(voucher.subtype))}</b></div>
            ${str(voucher.reference_no) ? `<div class="kv"><span>المرجع</span><b>${escapeHtml(str(voucher.reference_no))}</b></div>` : ''}
            ${str(voucher.cheque_no) ? `<div class="kv"><span>الشيك</span><b>${escapeHtml(str(voucher.cheque_no))} — ${escapeHtml(str(voucher.bank_name))} — ${escapeHtml(dateText(voucher.cheque_date))}</b></div>` : ''}
            ${str(voucher.cost_center_name) ? `<div class="kv"><span>مركز التكلفة</span><b>${escapeHtml(`${str(voucher.cost_center_code)} — ${str(voucher.cost_center_name)}`)}</b></div>` : ''}
            ${new Decimal(str(voucher.vat_amount) || '0').gt(0) ? `<div class="kv"><span>منها ضريبة القيمة المضافة</span><b>${escapeHtml(money(str(voucher.vat_amount)))}</b></div>` : ''}
          </section>
          ${this.signatures([isReceipt ? 'المستلم' : 'المستفيد', 'أمين الصندوق', 'المدير المالي'])}
        `,
      });
    });
  }

  async journalEntry(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const company = await this.company(tx, tenantId);
      const entry = first(
        await tx.execute(sql`
          SELECT j.*, b.name_ar AS branch_name FROM journal_entries j
            LEFT JOIN branches b ON b.id = j.branch_id
           WHERE j.tenant_id = ${tenantId} AND j.id = ${id}
        `),
      );
      if (!entry) throw new DomainError('NOT_FOUND', 'Journal entry was not found', 404);

      const lines = rows(
        await tx.execute(sql`
          SELECT l.line_no, l.debit, l.credit, l.description, a.code AS account_code, a.name_ar AS account_name,
                 cc.code AS cost_center_code, p.name AS party_name
            FROM journal_entry_lines l
            LEFT JOIN accounts a ON a.id = l.account_id
            LEFT JOIN cost_centers cc ON cc.id = l.cost_center_id
            LEFT JOIN parties p ON p.id = l.party_id
           WHERE l.tenant_id = ${tenantId} AND l.entry_id = ${id}
           ORDER BY l.line_no
        `),
      );
      const debit = lines.reduce((sum, row) => sum.plus(str(row.debit) || '0'), new Decimal(0));
      const credit = lines.reduce((sum, row) => sum.plus(str(row.credit) || '0'), new Decimal(0));

      return this.page({
        title: `سند قيد ${str(entry.number) || ''}`.trim(),
        body: `
          ${this.header(company, {
            docTitle: 'سند قيد',
            docTitleEn: 'Journal Voucher',
            number: str(entry.number) || '—',
            date: dateText(entry.date),
            status: STATUS_LABELS[str(entry.status)] ?? str(entry.status),
            extra: [
              ['الفرع', str(entry.branch_name)],
              ['البيان', str(entry.description)],
            ],
          })}
          <table class="lines">
            <thead>
              <tr><th>#</th><th>الحساب</th><th>البيان</th><th>مركز التكلفة</th><th>الجهة</th><th>مدين</th><th>دائن</th></tr>
            </thead>
            <tbody>
              ${lines
                .map(
                  (row) => `<tr>
                    <td class="num">${escapeHtml(str(row.line_no))}</td>
                    <td>${escapeHtml(`${str(row.account_code)} — ${str(row.account_name)}`)}</td>
                    <td>${escapeHtml(str(row.description))}</td>
                    <td>${escapeHtml(str(row.cost_center_code))}</td>
                    <td>${escapeHtml(str(row.party_name))}</td>
                    <td class="num">${escapeHtml(money(str(row.debit)))}</td>
                    <td class="num">${escapeHtml(money(str(row.credit)))}</td>
                  </tr>`,
                )
                .join('')}
            </tbody>
            <tfoot>
              <tr><th colspan="5">الإجمالي</th><th class="num">${escapeHtml(money(debit.toFixed(2)))}</th><th class="num">${escapeHtml(money(credit.toFixed(2)))}</th></tr>
            </tfoot>
          </table>
          <p class="words">فقط ${escapeHtml(amountInArabicWords(debit.toFixed(2), 'SAR'))}</p>
          ${this.signatures(['المحاسب', 'المراجع', 'المدير المالي'])}
        `,
      });
    });
  }

  async shiftClose(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const company = await this.company(tx, tenantId);
      const shift = first(
        await tx.execute(sql`
          SELECT s.*, b.name_ar AS branch_name FROM shift_closes s
            LEFT JOIN branches b ON b.id = s.branch_id
           WHERE s.tenant_id = ${tenantId} AND s.id = ${id}
        `),
      );
      if (!shift) throw new DomainError('NOT_FOUND', 'Shift was not found', 404);

      const counts = rows(await tx.execute(sql`SELECT currency_code, denomination, count, total FROM cash_count_lines WHERE tenant_id = ${tenantId} AND shift_close_id = ${id} ORDER BY denomination DESC`));
      const totals = rows(await tx.execute(sql`SELECT kind, method, amount FROM shift_close_lines WHERE tenant_id = ${tenantId} AND shift_close_id = ${id} ORDER BY line_no`));
      const diff = new Decimal(str(shift.diff) || '0');

      return this.page({
        title: 'إغلاق اليومية',
        body: `
          ${this.header(company, {
            docTitle: 'إغلاق اليومية',
            docTitleEn: 'Shift Close',
            number: shortId(str(shift.id)),
            date: dateText(shift.closed_at ?? shift.opened_at),
            status: str(shift.status) === 'closed' ? 'مغلقة' : 'مفتوحة',
            extra: [
              ['الفرع', str(shift.branch_name)],
              ['الفتح', dateTimeText(shift.opened_at)],
              ['الإغلاق', dateTimeText(shift.closed_at)],
            ],
          })}
          <section class="panel">
            <div class="kv"><span>النقد المتوقع</span><b>${escapeHtml(money(str(shift.expected_cash)))}</b></div>
            <div class="kv"><span>النقد المعدود</span><b>${escapeHtml(money(str(shift.counted_cash)))}</b></div>
            <div class="kv"><span>الفرق</span><b class="${diff.isZero() ? '' : 'warn'}">${escapeHtml(money(str(shift.diff)))}${diff.isZero() ? ' (مطابق)' : diff.gt(0) ? ' (زيادة)' : ' (عجز)'}</b></div>
          </section>
          ${
            totals.length
              ? `<table class="lines"><thead><tr><th>البند</th><th>طريقة الدفع</th><th>المبلغ</th></tr></thead><tbody>
                  ${totals.map((row) => `<tr><td>${escapeHtml(str(row.kind))}</td><td>${escapeHtml(PAYMENT_METHODS[str(row.method)] ?? str(row.method))}</td><td class="num">${escapeHtml(money(str(row.amount)))}</td></tr>`).join('')}
                 </tbody></table>`
              : ''
          }
          ${
            counts.length
              ? `<table class="lines"><thead><tr><th>الفئة</th><th>العدد</th><th>الإجمالي</th></tr></thead><tbody>
                  ${counts.map((row) => `<tr><td class="num">${escapeHtml(money(str(row.denomination)))}</td><td class="num">${escapeHtml(str(row.count))}</td><td class="num">${escapeHtml(money(str(row.total)))}</td></tr>`).join('')}
                 </tbody></table>`
              : ''
          }
          ${this.signatures(['الكاشير', 'المشرف', 'المحاسب'])}
        `,
      });
    });
  }

  /**
   * A printable page for any report in the catalogue.
   *
   * Reports are wide and unpredictable, so this uses the same letterhead as the documents
   * but lays the table out in landscape and repeats the header band on every printed page
   * (`thead` + `page-break-inside: avoid`), which is what an accountant expects when a trial
   * balance runs to nine pages.
   */
  async reportSheet(
    tenantId: string,
    report: {
      titleAr: string;
      columns: Array<{ key: string; labelAr: string; numeric: boolean }>;
      rows: Array<Record<string, string>>;
      totals: Record<string, string>;
      captions: string[];
      generatedAt: string;
    },
  ) {
    const company = await withTenantTx(this.database.db, tenantId, async (tx) => this.company(tx, tenantId));
    const contact = [company.phones.join(' / '), company.email, addressText(company.address)].filter(Boolean).join(' — ');
    const head = report.columns.map((column) => `<th>${escapeHtml(column.labelAr)}</th>`).join('');
    const body = report.rows.length
      ? report.rows
          .map(
            (row, index) =>
              `<tr><td class="num">${index + 1}</td>${report.columns
                .map((column) => `<td${column.numeric ? ' class="num"' : ''}>${escapeHtml(cellText(row[column.key] ?? '', column.numeric))}</td>`)
                .join('')}</tr>`,
          )
          .join('')
      : `<tr><td class="empty" colspan="${report.columns.length + 1}">لا توجد بيانات ضمن معايير البحث المحددة.</td></tr>`;
    const hasTotals = Object.keys(report.totals).length > 0;
    const footer = hasTotals
      ? `<tfoot><tr><th>الإجمالي</th>${report.columns
          .map((column) => `<th class="num">${report.totals[column.key] ? escapeHtml(money(report.totals[column.key]!)) : ''}</th>`)
          .join('')}</tr></tfoot>`
      : '';

    return this.page({
      title: report.titleAr,
      landscape: true,
      body: `
        <header class="doc-head">
          <div class="company">
            <h1>${escapeHtml(company.nameAr)}</h1>
            ${company.nameEn ? `<div class="en">${escapeHtml(company.nameEn)}</div>` : ''}
            <div class="meta">${company.taxNo ? `<span>الرقم الضريبي: <b dir="ltr">${escapeHtml(company.taxNo)}</b></span>` : ''}${company.crNo ? `<span>السجل التجاري: <b dir="ltr">${escapeHtml(company.crNo)}</b></span>` : ''}</div>
            ${contact ? `<div class="meta">${escapeHtml(contact)}</div>` : ''}
          </div>
          <div class="doc">
            <div class="doc-title">${escapeHtml(report.titleAr)}</div>
            <table class="doc-meta">
              ${report.captions.map((caption) => `<tr><td>${escapeHtml(caption)}</td></tr>`).join('')}
              <tr><td>عدد السجلات: ${report.rows.length}</td></tr>
              <tr><td>طُبع في: ${escapeHtml(dateTimeText(report.generatedAt))}</td></tr>
            </table>
          </div>
        </header>
        <table class="lines report">
          <thead><tr><th>#</th>${head}</tr></thead>
          <tbody>${body}</tbody>
          ${footer}
        </table>
      `,
    });
  }

  // -------------------------------------------------------------------- pieces

  private async company(tx: Tx, tenantId: string) {
    const profile = first(await tx.execute(sql`SELECT name_ar, name_en, tax_no, cr_no, address, phones, email FROM company_profiles WHERE tenant_id = ${tenantId}`));
    const tenant = first(await tx.execute(sql`SELECT name FROM tenants WHERE id = ${tenantId}`));
    return {
      nameAr: str(profile?.name_ar) || str(tenant?.name) || 'المنشأة',
      nameEn: str(profile?.name_en),
      taxNo: str(profile?.tax_no),
      crNo: str(profile?.cr_no),
      email: str(profile?.email),
      phones: Array.isArray(profile?.phones) ? (profile?.phones as string[]) : [],
      address: profile?.address,
    };
  }

  private header(
    company: Awaited<ReturnType<PrintTemplatesService['company']>>,
    doc: { docTitle: string; docTitleEn: string; number: string; date: string; status: string; extra: Array<[string, string]> },
  ) {
    const contact = [company.phones.join(' / '), company.email, addressText(company.address)].filter(Boolean).join(' — ');
    return `
      <header class="doc-head">
        <div class="company">
          <h1>${escapeHtml(company.nameAr)}</h1>
          ${company.nameEn ? `<div class="en">${escapeHtml(company.nameEn)}</div>` : ''}
          <div class="meta">
            ${company.taxNo ? `<span>الرقم الضريبي: <b dir="ltr">${escapeHtml(company.taxNo)}</b></span>` : '<span class="warn">لم يُسجَّل الرقم الضريبي في بطاقة المنشأة</span>'}
            ${company.crNo ? `<span>السجل التجاري: <b dir="ltr">${escapeHtml(company.crNo)}</b></span>` : ''}
          </div>
          ${contact ? `<div class="meta">${escapeHtml(contact)}</div>` : ''}
        </div>
        <div class="doc">
          <div class="doc-title">${escapeHtml(doc.docTitle)}</div>
          <div class="doc-title-en">${escapeHtml(doc.docTitleEn)}</div>
          <table class="doc-meta">
            <tr><th>رقم المستند</th><td dir="ltr">${escapeHtml(doc.number)}</td></tr>
            <tr><th>التاريخ</th><td dir="ltr">${escapeHtml(doc.date)}</td></tr>
            <tr><th>الحالة</th><td>${escapeHtml(doc.status)}</td></tr>
            ${doc.extra.filter(([, value]) => value).map(([label, value]) => `<tr><th>${escapeHtml(label)}</th><td>${escapeHtml(value)}</td></tr>`).join('')}
          </table>
        </div>
      </header>
    `;
  }

  private partyBlock(title: string, party: { name: string; code: string; taxNo: string; phone: string; address: unknown }) {
    return `
      <section class="party">
        <h2>${escapeHtml(title)}</h2>
        <div class="party-grid">
          <div><span>الاسم</span><b>${escapeHtml(party.name)}</b></div>
          ${party.code ? `<div><span>الرمز</span><b dir="ltr">${escapeHtml(party.code)}</b></div>` : ''}
          ${party.taxNo ? `<div><span>الرقم الضريبي</span><b dir="ltr">${escapeHtml(party.taxNo)}</b></div>` : ''}
          ${party.phone ? `<div><span>الهاتف</span><b dir="ltr">${escapeHtml(party.phone)}</b></div>` : ''}
          ${addressText(party.address) ? `<div class="wide"><span>العنوان</span><b>${escapeHtml(addressText(party.address))}</b></div>` : ''}
        </div>
      </section>
    `;
  }

  private linesTable(lines: Array<Record<string, unknown>>) {
    if (!lines.length) return '<p class="empty">لا توجد بنود على هذا المستند.</p>';
    return `
      <table class="lines">
        <thead>
          <tr>
            <th>#</th><th>الصنف</th><th>الوحدة</th><th>الكمية</th><th>السعر</th><th>الخصم</th><th>الصافي</th><th>الضريبة</th><th>الإجمالي</th>
          </tr>
        </thead>
        <tbody>
          ${lines
            .map(
              (line) => `<tr>
                <td class="num">${escapeHtml(str(line.line_no))}</td>
                <td>${escapeHtml(str(line.label))}${str(line.sku) ? `<span class="sku" dir="ltr">${escapeHtml(str(line.sku))}</span>` : ''}</td>
                <td>${escapeHtml(str(line.unit_code))}</td>
                <td class="num">${escapeHtml(qty(str(line.quantity)))}</td>
                <td class="num">${escapeHtml(money(str(line.unit_price)))}</td>
                <td class="num">${escapeHtml(money(str(line.discount_amount)))}</td>
                <td class="num">${escapeHtml(money(str(line.net)))}</td>
                <td class="num">${escapeHtml(money(str(line.tax)))}</td>
                <td class="num">${escapeHtml(money(str(line.total)))}</td>
              </tr>`,
            )
            .join('')}
        </tbody>
      </table>
    `;
  }

  private totalsBlock(totals: {
    currency: string;
    subtotal: string;
    discount: string;
    tax: string;
    extraTax?: string;
    withholding?: string;
    additional?: string;
    total: string;
    paid: string;
    payments: Array<{ method: string; amount: string; reference: string }>;
    qr?: string;
    zatcaStatus?: string;
  }) {
    const due = new Decimal(totals.total || '0').minus(totals.paid || '0');
    const optional = (label: string, value?: string) =>
      value && !new Decimal(value || '0').isZero() ? `<tr><th>${escapeHtml(label)}</th><td class="num">${escapeHtml(money(value))}</td></tr>` : '';

    return `
      <section class="totals">
        <div class="totals-side">
          <p class="words">فقط ${escapeHtml(amountInArabicWords(totals.total, totals.currency))}</p>
          ${
            totals.payments.length
              ? `<table class="pay"><thead><tr><th>الدفعات</th><th>المبلغ</th></tr></thead><tbody>
                  ${totals.payments.map((row) => `<tr><td>${escapeHtml(row.method)}${row.reference ? ` — ${escapeHtml(row.reference)}` : ''}</td><td class="num">${escapeHtml(money(row.amount))}</td></tr>`).join('')}
                 </tbody></table>`
              : ''
          }
          ${this.qrBlock(totals.qr, totals.zatcaStatus)}
        </div>
        <table class="totals-table">
          <tr><th>الإجمالي قبل الضريبة</th><td class="num">${escapeHtml(money(totals.subtotal))}</td></tr>
          ${optional('خصم الفاتورة', totals.discount)}
          ${optional('تكاليف إضافية', totals.additional)}
          <tr><th>ضريبة القيمة المضافة</th><td class="num">${escapeHtml(money(totals.tax))}</td></tr>
          ${optional('ضريبة إضافية', totals.extraTax)}
          ${optional('الاستقطاع', totals.withholding)}
          <tr class="grand"><th>الإجمالي المستحق (${escapeHtml(totals.currency)})</th><td class="num">${escapeHtml(money(totals.total))}</td></tr>
          <tr><th>المدفوع</th><td class="num">${escapeHtml(money(totals.paid))}</td></tr>
          <tr><th>المتبقي</th><td class="num">${escapeHtml(money(due.toFixed(2)))}</td></tr>
        </table>
      </section>
    `;
  }

  /**
   * ZATCA prints the TLV payload as a QR next to the totals. When the invoice has not
   * been reported yet there is nothing honest to draw, so the space carries a note
   * instead of a decorative square that would fail any scan.
   */
  private qrBlock(payload?: string, status?: string) {
    if (!payload) {
      return '<div class="qr-empty">لم تُصدر بعد بيانات الفاتورة الإلكترونية (QR) لهذا المستند.</div>';
    }
    const qr = qrcode(0, 'M');
    qr.addData(payload);
    qr.make();
    return `<div class="qr">${qr.createSvgTag({ cellSize: 3, margin: 0, scalable: true })}<span>${escapeHtml(ZATCA_STATUS_LABELS[status ?? ''] ?? 'رمز الاستجابة السريعة')}</span></div>`;
  }

  private signatures(labels: string[]) {
    return `<section class="signs">${labels.map((label) => `<div><span>${escapeHtml(label)}</span><i></i></div>`).join('')}</section>`;
  }

  /** One A4 stylesheet for every document, plus a print button that hides itself. */
  private page({ title, body, landscape }: { title: string; body: string; landscape?: boolean }) {
    return `<!doctype html>
<html dir="rtl" lang="ar">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>${escapeHtml(title)}</title>
<style>
  @page { size: A4${landscape ? ' landscape' : ''}; margin: 12mm; }
  * { box-sizing: border-box; }
  body { font-family: "Segoe UI", Tahoma, "Noto Naskh Arabic", sans-serif; color: #111; margin: 0; padding: 16px; background: #f4f5f7; font-size: 12px; }
  .sheet { background: #fff; max-width: ${landscape ? '297mm' : '210mm'}; margin: 0 auto; padding: 16mm 14mm; box-shadow: 0 1px 8px rgba(0,0,0,.12); }
  h1 { font-size: 18px; margin: 0 0 2px; }
  h2 { font-size: 13px; margin: 0 0 6px; }
  .doc-head { display: flex; justify-content: space-between; gap: 16px; border-bottom: 2px solid #111; padding-bottom: 10px; margin-bottom: 12px; }
  .company .en { color: #555; font-size: 12px; }
  .company .meta { margin-top: 4px; color: #333; display: flex; gap: 12px; flex-wrap: wrap; }
  .doc { text-align: left; min-width: 210px; }
  .doc-title { font-size: 16px; font-weight: 700; }
  .doc-title-en { color: #666; font-size: 11px; margin-bottom: 6px; }
  .doc-meta { border-collapse: collapse; width: 100%; }
  .doc-meta th { text-align: right; color: #555; font-weight: 500; padding: 1px 6px 1px 0; white-space: nowrap; }
  .doc-meta td { text-align: left; font-weight: 600; padding: 1px 0; }
  .party { border: 1px solid #ddd; border-radius: 6px; padding: 8px 10px; margin-bottom: 10px; }
  .party-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 4px 12px; }
  .party-grid .wide { grid-column: 1 / -1; }
  .party-grid span { color: #666; margin-inline-end: 6px; }
  table.lines { width: 100%; border-collapse: collapse; margin-bottom: 10px; }
  table.lines th, table.lines td { border: 1px solid #ccc; padding: 4px 6px; }
  table.lines thead th { background: #f0f1f4; font-weight: 600; }
  table.lines tfoot th { background: #f0f1f4; }
  .num { text-align: left; font-variant-numeric: tabular-nums; direction: ltr; }
  .sku { display: block; color: #777; font-size: 10px; }
  .totals { display: flex; gap: 16px; align-items: flex-start; justify-content: space-between; }
  .totals-side { flex: 1; }
  .totals-table { border-collapse: collapse; min-width: 260px; }
  .totals-table th { text-align: right; padding: 3px 8px; color: #333; font-weight: 500; }
  .totals-table td { padding: 3px 8px; border-bottom: 1px solid #eee; }
  .totals-table .grand th, .totals-table .grand td { font-size: 14px; font-weight: 700; border-top: 2px solid #111; border-bottom: 2px solid #111; }
  table.pay { border-collapse: collapse; margin-top: 8px; }
  table.pay th, table.pay td { border: 1px solid #ddd; padding: 3px 8px; }
  .words { background: #f6f6f8; border-inline-start: 3px solid #111; padding: 6px 8px; margin: 0 0 8px; }
  .panel { border: 1px solid #ddd; border-radius: 6px; padding: 10px 12px; margin-bottom: 12px; }
  .panel .kv { display: flex; gap: 8px; padding: 4px 0; border-bottom: 1px dashed #e5e5e5; }
  .panel .kv:last-child { border-bottom: 0; }
  .panel .kv span { color: #666; min-width: 150px; }
  .qr { margin-top: 10px; display: inline-flex; flex-direction: column; align-items: center; gap: 2px; }
  .qr svg { width: 110px; height: 110px; }
  .qr span, .qr-empty { font-size: 10px; color: #666; }
  .qr-empty { display: block; margin-top: 10px; }
  .warn { color: #a4400a; }
  .empty { color: #666; padding: 12px; border: 1px dashed #ccc; text-align: center; }
  .signs { display: flex; gap: 24px; margin-top: 28px; }
  .signs div { flex: 1; text-align: center; color: #555; }
  .signs i { display: block; border-top: 1px solid #999; margin-top: 34px; }
  .toolbar { max-width: 210mm; margin: 0 auto 10px; display: flex; gap: 8px; }
  .toolbar button { font: inherit; padding: 6px 14px; border: 1px solid #111; background: #111; color: #fff; border-radius: 6px; cursor: pointer; }
  table.report { font-size: 11px; }
  table.report thead { display: table-header-group; }
  table.report tbody tr { page-break-inside: avoid; }
  table.report tbody tr:nth-child(even) { background: #fafafa; }
  table.report td.empty { text-align: center; color: #666; padding: 18px; border: 1px solid #ccc; }
  @media print { body { background: #fff; padding: 0; } .sheet { box-shadow: none; padding: 0; max-width: none; } .toolbar { display: none; } }
</style>
</head>
<body>
  <div class="toolbar"><button type="button" onclick="window.print()">طباعة</button></div>
  <div class="sheet">${body}</div>
</body>
</html>`;
  }
}

// ---------------------------------------------------------------------- helpers

type Tx = Parameters<Parameters<typeof withTenantTx>[2]>[0];

function rows(result: unknown): Array<Record<string, unknown>> {
  return Array.isArray(result) ? (result as Array<Record<string, unknown>>) : ((result as { rows?: Array<Record<string, unknown>> }).rows ?? []);
}
function first(result: unknown): Record<string, unknown> | undefined {
  return rows(result)[0];
}
function str(value: unknown): string {
  return value === null || value === undefined ? '' : String(value);
}
function money(value: string | number | null | undefined): string {
  const decimal = new Decimal(value === null || value === undefined || value === '' ? 0 : value);
  return decimal.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
}
function qty(value: string): string {
  if (!value) return '0';
  const decimal = new Decimal(value);
  return decimal.eq(decimal.trunc()) ? decimal.toFixed(0) : decimal.toFixed(3);
}
function dateText(value: unknown): string {
  if (!value) return '—';
  const date = value instanceof Date ? value : new Date(String(value));
  return Number.isNaN(date.getTime()) ? String(value) : date.toISOString().slice(0, 10);
}
function dateTimeText(value: unknown): string {
  if (!value) return '—';
  const date = value instanceof Date ? value : new Date(String(value));
  return Number.isNaN(date.getTime()) ? String(value) : date.toISOString().slice(0, 16).replace('T', ' ');
}
function shortId(value: string): string {
  return value ? value.slice(0, 8).toUpperCase() : '—';
}
function addressText(address: unknown): string {
  if (!address || typeof address !== 'object') return '';
  const parts = ['buildingNumber', 'street', 'additionalStreet', 'district', 'city', 'postalCode', 'country']
    .map((key) => (address as Record<string, unknown>)[key])
    .filter((value) => typeof value === 'string' && value.trim() !== '');
  return parts.join('، ');
}
/** Numbers get thousands separators; text is printed as it came out of the query. */
function cellText(value: string, numeric: boolean): string {
  if (!value) return '—';
  return numeric && /^-?\d+(\.\d+)?$/.test(value) ? money(value) : value;
}

export function escapeHtml(value: string): string {
  return value.replace(/[&<>"']/g, (char) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[char] ?? char);
}

/** What the caption under the QR says. A prepared invoice carries a valid phase-1 code. */
const ZATCA_STATUS_LABELS: Record<string, string> = {
  prepared: 'رمز الفوترة الإلكترونية (المرحلة الأولى)',
  signed: 'رمز موقّع — بانتظار الإرسال للهيئة',
  reported: 'مُبلَّغة لهيئة الزكاة والضريبة',
  cleared: 'مُصادق عليها من هيئة الزكاة والضريبة',
  failed: 'تعذّر الإرسال للهيئة',
};

const SALES_KIND_TITLES: Record<string, string> = { sale: 'فاتورة مبيعات', return: 'مردود مبيعات', quotation: 'عرض سعر', contracting: 'فاتورة مقاولات' };
const SALES_KIND_TITLES_EN: Record<string, string> = { sale: 'Sales Invoice', return: 'Sales Return', quotation: 'Quotation', contracting: 'Contracting Invoice' };
const STATUS_LABELS: Record<string, string> = { draft: 'مسودة', posted: 'مرحّلة', void: 'ملغاة', voided: 'ملغاة', paid: 'مدفوعة', open: 'مفتوحة', closed: 'مغلقة' };
const PAYMENT_METHODS: Record<string, string> = { cash: 'نقداً', card: 'شبكة', bank_transfer: 'تحويل بنكي', transfer: 'تحويل بنكي', cheque: 'شيك', credit: 'آجل' };
const VOUCHER_SUBTYPES: Record<string, string> = { customer: 'دفعة من عميل', supplier: 'دفعة لمورد', expense: 'مصروف', salary: 'رواتب', tax: 'ضريبة', other: 'أخرى' };
