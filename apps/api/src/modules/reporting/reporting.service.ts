import { Injectable } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { z } from 'zod';
import { DomainError, newId } from '@erp/contracts';
import { getDatabase, withTenantTx } from '@erp/database';

import { REPORT_DEFINITIONS, reportByKey, type ReportColumn, type ReportFilters } from './report-catalog.js';

export const REPORT_KEYS = REPORT_DEFINITIONS.map((definition) => definition.key);
export type ReportKey = string;

const uuidish = z.string().uuid().optional();
const dayish = z.string().regex(/^\d{4}-\d{2}-\d{2}$/, 'Expected YYYY-MM-DD').optional();

/** Only these filters reach SQL; anything else in the query string is ignored on purpose. */
const filtersSchema = z
  .object({
    from: dayish,
    to: dayish,
    branchId: uuidish,
    warehouseId: uuidish,
    partyId: uuidish,
    itemId: uuidish,
    categoryId: uuidish,
    salesmanId: uuidish,
    costCenterId: uuidish,
    status: z.string().max(40).optional(),
    kind: z.string().max(40).optional(),
  })
  .partial();

const NUMERIC_TYPES = new Set(['money', 'qty', 'int', 'percent']);

@Injectable()
export class ReportingService {
  /** Everything a client needs to render every report without hard-coding any of them. */
  catalog() {
    return REPORT_DEFINITIONS.map((definition) => ({
      key: definition.key,
      titleAr: definition.titleAr,
      group: definition.group,
      hintAr: definition.hintAr,
      params: definition.params,
      columns: definition.columns,
      totals: definition.totals ?? [],
      chart: definition.chart ?? null,
      asyncExport: false,
    }));
  }

  async run(tenantId: string, key: string, params: Record<string, string | undefined> = {}) {
    const definition = reportByKey.get(key);
    if (!definition) throw new DomainError('REPORT_NOT_FOUND', 'Report key is not registered', 404);
    const filters = parseFilters(params);
    const database = getDatabase().db;
    const rows = await withTenantTx(database, tenantId, async (tx) => rowsOf(await tx.execute(definition.build(tenantId, filters))));
    const normalized = rows.map((row) => normalizeRow(row, definition.columns));
    return {
      key,
      titleAr: definition.titleAr,
      group: definition.group,
      hintAr: definition.hintAr ?? null,
      chart: definition.chart ?? null,
      params: filters,
      columns: definition.columns,
      rows: normalized,
      totals: sumColumns(normalized, definition.totals ?? []),
      rowCount: normalized.length,
      generatedAt: new Date().toISOString(),
    };
  }

  /** Exports are produced inline — the dataset is already capped, so there is nothing to queue. */
  async export(tenantId: string, key: string, params: Record<string, string | undefined> = {}, format: 'csv' | 'xlsx' | 'pdf' = 'csv') {
    const report = await this.run(tenantId, key, params);
    const csv = toCsv(report.columns, report.rows);
    return {
      exportId: newId(),
      status: 'ready' as const,
      format,
      reportKey: key,
      filename: `${key}-${report.generatedAt.slice(0, 10)}.csv`,
      rows: report.rows.length,
      csv,
    };
  }

  invoicePrintHtml(id: string) { return `<!doctype html><html dir="rtl"><body><h1>فاتورة ${escapeHtml(id)}</h1></body></html>`; }
  shiftPrintHtml(id: string) { return `<!doctype html><html dir="rtl"><body><h1>إغلاق وردية ${escapeHtml(id)}</h1></body></html>`; }
}

export function parseFilters(params: Record<string, string | undefined>): ReportFilters {
  const cleaned: Record<string, string> = {};
  for (const [name, value] of Object.entries(params)) {
    if (typeof value === 'string' && value.trim() !== '') cleaned[name] = value.trim();
  }
  const parsed = filtersSchema.safeParse(cleaned);
  if (!parsed.success) throw new DomainError('VALIDATION_FAILED', parsed.error.issues[0]?.message ?? 'Invalid report filter', 422);
  return parsed.data;
}

/** Postgres hands back `Date` objects and nulls; reports must be plain JSON strings. */
function normalizeRow(row: Record<string, unknown>, columns: ReportColumn[]): Record<string, string> {
  const output: Record<string, string> = {};
  for (const column of columns) {
    const raw = row[column.key];
    output[column.key] = raw === null || raw === undefined ? '' : raw instanceof Date ? raw.toISOString().slice(0, 10) : String(raw);
  }
  return output;
}

export function sumColumns(rows: Array<Record<string, string>>, keys: string[]): Record<string, string> {
  const totals: Record<string, string> = {};
  for (const key of keys) {
    let sum = new Decimal(0);
    for (const row of rows) {
      const raw = row[key];
      if (!raw || !/^-?\d+(\.\d+)?$/.test(raw)) continue;
      sum = sum.plus(raw);
    }
    totals[key] = sum.toFixed(sum.decimalPlaces() > 2 ? 4 : 2);
  }
  return totals;
}

export function toCsv(columns: ReportColumn[], rows: Array<Record<string, string>>): string {
  const escape = (value: string) => (/[",\n]/.test(value) ? `"${value.replaceAll('"', '""')}"` : value);
  const header = columns.map((column) => escape(column.labelAr)).join(',');
  const body = rows.map((row) => columns.map((column) => escape(row[column.key] ?? '')).join(','));
  return ['\uFEFF' + header, ...body].join('\n');
}

export function isNumericColumn(column: ReportColumn): boolean { return NUMERIC_TYPES.has(column.type); }

function rowsOf(result: unknown): Array<Record<string, unknown>> {
  return Array.isArray(result) ? (result as Array<Record<string, unknown>>) : ((result as { rows?: Array<Record<string, unknown>> }).rows ?? []);
}
function escapeHtml(value: string): string { return value.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;'); }
