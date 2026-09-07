import { Injectable } from '@nestjs/common';
import { sql } from 'drizzle-orm';
import { z } from 'zod';
import { DomainError, newId } from '@erp/contracts';
import { getDatabase, withTenantTx } from '@erp/database';

export const REPORT_KEYS = [
  'sales-by-day', 'sales-by-category', 'sales-by-item', 'sales-by-payment', 'sales-by-ordertype', 'monthly-sales',
  'inventory-valuation', 'item-movement', 'stock-limits', 'expiry-report', 'serial-tracking', 'batch-tracking',
  'ar-aging', 'ap-aging', 'party-statement', 'vat-return', 'trial-balance', 'general-ledger', 'profit-loss', 'balance-sheet', 'cashier-shift',
] as const;
export type ReportKey = typeof REPORT_KEYS[number];

const paramsSchema = z.record(z.string(), z.string().optional()).default({});
const keySet = new Set<string>(REPORT_KEYS);

@Injectable()
export class ReportingService {
  catalog() { return REPORT_KEYS.map((key) => ({ key, asyncExport: true, chart: key.includes('sales') ? 'bar' : 'table' })); }

  async run(tenantId: string, key: string, params: Record<string, string | undefined> = {}) {
    if (!keySet.has(key)) throw new DomainError('REPORT_NOT_FOUND', 'Report key is not registered', 404);
    const parsed = paramsSchema.parse(params);
    const db = getDatabase().db;
    return withTenantTx(db, tenantId, async (tx) => {
      switch (key as ReportKey) {
        case 'sales-by-day':
          return { key, params: parsed, rows: rowsOf(await tx.execute(sql`SELECT date(posted_at) AS day, sum(total)::text AS total FROM sales_invoices WHERE tenant_id = ${tenantId} AND status = 'posted' GROUP BY date(posted_at) ORDER BY day`)) };
        case 'monthly-sales':
          return { key, params: parsed, rows: rowsOf(await tx.execute(sql`SELECT date_trunc('month', posted_at)::date AS month, sum(total)::text AS total FROM sales_invoices WHERE tenant_id = ${tenantId} AND status = 'posted' GROUP BY date_trunc('month', posted_at) ORDER BY month`)) };
        case 'sales-by-payment':
          return { key, params: parsed, rows: rowsOf(await tx.execute(sql`SELECT payment_status, count(*)::text AS count, sum(total)::text AS total FROM sales_invoices WHERE tenant_id = ${tenantId} GROUP BY payment_status ORDER BY payment_status`)) };
        case 'sales-by-ordertype':
          return { key, params: parsed, rows: rowsOf(await tx.execute(sql`SELECT coalesce(order_type, 'standard') AS order_type, count(*)::text AS count, sum(total)::text AS total FROM sales_invoices WHERE tenant_id = ${tenantId} AND status = 'posted' GROUP BY coalesce(order_type, 'standard') ORDER BY order_type`)) };
        case 'inventory-valuation':
          return { key, params: parsed, rows: rowsOf(await tx.execute(sql`SELECT item_id, warehouse_id, quantity::text, value::text, average_cost::text FROM stock_balances WHERE tenant_id = ${tenantId} ORDER BY item_id, warehouse_id`)) };
        case 'item-movement':
          return { key, params: parsed, rows: rowsOf(await tx.execute(sql`SELECT item_id, warehouse_id, direction, qty::text, total_cost::text, occurred_at FROM inventory_transactions WHERE tenant_id = ${tenantId} ORDER BY occurred_at DESC LIMIT 500`)) };
        case 'serial-tracking':
          return { key, params: parsed, rows: rowsOf(await tx.execute(sql`SELECT item_id, serial_no, status, warehouse_id FROM item_serials WHERE tenant_id = ${tenantId} ORDER BY serial_no`)) };
        case 'batch-tracking':
        case 'expiry-report':
          return { key, params: parsed, rows: rowsOf(await tx.execute(sql`SELECT item_id, lot_no, expiry_date FROM item_lots WHERE tenant_id = ${tenantId} ORDER BY expiry_date NULLS LAST`)) };
        case 'trial-balance':
          return { key, params: parsed, rows: rowsOf(await tx.execute(sql`SELECT account_id, sum(debit)::text AS debit, sum(credit)::text AS credit FROM journal_entry_lines WHERE tenant_id = ${tenantId} GROUP BY account_id ORDER BY account_id`)) };
        case 'general-ledger':
        case 'party-statement':
          return { key, params: parsed, rows: rowsOf(await tx.execute(sql`SELECT entry_id, account_id, party_id, debit::text, credit::text FROM journal_entry_lines WHERE tenant_id = ${tenantId} ORDER BY entry_id LIMIT 500`)) };
        case 'ar-aging':
        case 'ap-aging':
          return { key, params: parsed, rows: rowsOf(await tx.execute(sql`SELECT party_id, invoice_kind, sum(amount)::text AS allocated FROM payment_allocations WHERE tenant_id = ${tenantId} GROUP BY party_id, invoice_kind ORDER BY party_id`)) };
        case 'vat-return':
          return { key, params: parsed, rows: rowsOf(await tx.execute(sql`SELECT 'sales' AS bucket, sum(tax_total)::text AS vat FROM sales_invoices WHERE tenant_id = ${tenantId} AND status = 'posted' UNION ALL SELECT 'purchases' AS bucket, sum(tax_total)::text AS vat FROM purchase_invoices WHERE tenant_id = ${tenantId} AND status = 'posted'`)) };
        case 'cashier-shift':
          return { key, params: parsed, rows: rowsOf(await tx.execute(sql`SELECT id, branch_id, user_id, opened_at, closed_at, expected_cash::text, counted_cash::text, diff::text FROM shift_closes WHERE tenant_id = ${tenantId} ORDER BY opened_at DESC LIMIT 100`)) };
        default:
          return { key, params: parsed, rows: [], note: key === 'sales-by-ordertype' ? 'Order type is introduced in Phase 19; empty until then.' : undefined };
      }
    });
  }

  async export(tenantId: string, key: string, params: Record<string, string | undefined> = {}, format: 'csv' | 'xlsx' | 'pdf' = 'csv') {
    const report = await this.run(tenantId, key, params);
    return { exportId: newId(), status: 'queued', queue: 'reports-export', format, reportKey: key, rows: report.rows.length };
  }

  invoicePrintHtml(id: string) { return `<!doctype html><html dir="rtl"><body><h1>فاتورة ${escapeHtml(id)}</h1></body></html>`; }
  shiftPrintHtml(id: string) { return `<!doctype html><html dir="rtl"><body><h1>إغلاق وردية ${escapeHtml(id)}</h1></body></html>`; }
}

function rowsOf(result: unknown): Array<Record<string, unknown>> { return Array.isArray(result) ? result as Array<Record<string, unknown>> : ((result as { rows?: Array<Record<string, unknown>> }).rows ?? []); }
function escapeHtml(value: string): string { return value.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;'); }
