import { Inject, Injectable } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { sql } from 'drizzle-orm';
import { z } from 'zod';
import { DomainError, newId } from '@erp/contracts';
import { withTenantTx, type DatabaseHandle } from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';

import { PrintTemplatesService } from './print-templates.service.js';
import { REPORT_DEFINITIONS, reportByKey, type ReportColumn, type ReportFilters, type ReportParam } from './report-catalog.js';
import { ReportLayoutsService } from './report-layouts.service.js';
import { buildXlsx } from './xlsx.js';

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

export type ExportFormat = 'csv' | 'xlsx' | 'pdf';
const EXPORT_FORMATS = new Set<ExportFormat>(['csv', 'xlsx', 'pdf']);

/**
 * Which table holds the display name behind each id-shaped filter. A printed report has to say
 * "الفرع: الفرع الرئيسي", never a raw uuid, or nobody can tell two copies of the same report apart.
 */
const FILTER_SOURCES: Record<string, { table: string; column: string } | undefined> = {
  branch: { table: 'branches', column: 'name_ar' },
  warehouse: { table: 'warehouses', column: 'name' },
  party: { table: 'parties', column: 'name' },
  item: { table: 'items', column: 'name_ar' },
  category: { table: 'item_categories', column: 'name_ar' },
  salesman: { table: 'salesmen', column: 'name' },
  costCenter: { table: 'cost_centers', column: 'name_ar' },
};

@Injectable()
export class ReportingService {
  // The handle is injected rather than read from the module-level singleton so a test
  // (or any second connection pool) runs reports against the database it was given.
  constructor(
    @Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle,
    private readonly layouts: ReportLayoutsService,
    private readonly print: PrintTemplatesService,
  ) {}

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

    // A saved layout (مصمم التقارير) contributes default filters and a column presentation.
    // The caller's own filters always win: the layout is a starting point, not a cage.
    const { layout: layoutRef, ...rest } = params;
    const layout = await this.layouts.resolve(tenantId, key, layoutRef);
    const filters = parseFilters({ ...(layout?.filters ?? {}), ...rest });

    const database = this.database.db;
    const rows = await withTenantTx(database, tenantId, async (tx) => rowsOf(await tx.execute(definition.build(tenantId, filters))));
    const normalized = rows.map((row) => normalizeRow(row, definition.columns));
    const columns = applyLayoutColumns(definition.columns, layout?.columns);
    return {
      key,
      titleAr: layout?.titleAr || definition.titleAr,
      group: definition.group,
      hintAr: definition.hintAr ?? null,
      chart: definition.chart ?? null,
      params: filters,
      layout: layout ? { id: layout.id, name: layout.name } : null,
      columns,
      rows: normalized,
      totals: sumColumns(normalized, definition.totals ?? []),
      rowCount: normalized.length,
      generatedAt: new Date().toISOString(),
    };
  }

  /**
   * Exports are produced inline — the dataset is already capped, so there is nothing to queue.
   *
   * Three formats, three real files:
   * - `csv`  — UTF-8 with a BOM so Excel on Windows reads Arabic instead of mojibake.
   * - `xlsx` — a genuine workbook (right-to-left sheet, frozen header, numeric cells that sum).
   * - `pdf`  — a print-ready A4 landscape page on the company letterhead; the browser turns it
   *   into a PDF. Rendering a PDF here would mean shipping a font with Arabic shaping, and the
   *   print dialog already produces a better-looking, selectable document.
   *
   * The payload is base64 for binary formats and plain text otherwise, so one endpoint can
   * serve all three without content negotiation.
   */
  async export(tenantId: string, key: string, params: Record<string, string | undefined> = {}, format: ExportFormat = 'csv') {
    if (!EXPORT_FORMATS.has(format)) throw new DomainError('VALIDATION_FAILED', `Unsupported export format: ${format}`, 422);
    const report = await this.run(tenantId, key, params);
    const definition = reportByKey.get(key)!;
    const stamp = report.generatedAt.slice(0, 10);
    const base = { exportId: newId(), status: 'ready' as const, format, reportKey: key, titleAr: report.titleAr, rows: report.rows.length };
    const captions = await this.filterCaptions(tenantId, definition.params, report.params);

    if (format === 'xlsx') {
      const workbook = buildXlsx({
        name: report.titleAr,
        titleAr: report.titleAr,
        captions: [...captions, `عدد السجلات: ${report.rows.length}`, `طُبع في: ${report.generatedAt.slice(0, 16).replace('T', ' ')}`],
        columns: report.columns.map((column) => ({
          header: column.labelAr,
          kind: isNumericColumn(column) ? (column.type === 'int' ? 'integer' : 'number') : 'text',
          width: column.type === 'text' ? 26 : 15,
        })),
        rows: report.rows.map((row) => report.columns.map((column) => row[column.key] ?? '')),
        totalsRow: Object.keys(report.totals).length
          ? report.columns.map((column, index) => (report.totals[column.key] ? report.totals[column.key]! : index === 0 ? 'الإجمالي' : ''))
          : undefined,
      });
      return {
        ...base,
        filename: `${key}-${stamp}.xlsx`,
        mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        encoding: 'base64' as const,
        content: workbook.toString('base64'),
      };
    }

    if (format === 'pdf') {
      const html = await this.print.reportSheet(tenantId, {
        titleAr: report.titleAr,
        columns: report.columns.map((column) => ({ key: column.key, labelAr: column.labelAr, numeric: isNumericColumn(column) })),
        rows: report.rows,
        totals: report.totals,
        captions,
        generatedAt: report.generatedAt,
      });
      return { ...base, filename: `${key}-${stamp}.html`, mimeType: 'text/html; charset=utf-8', encoding: 'utf-8' as const, content: html, printable: true as const };
    }

    const csv = toCsv(report.columns, report.rows);
    return { ...base, filename: `${key}-${stamp}.csv`, mimeType: 'text/csv; charset=utf-8', encoding: 'utf-8' as const, content: csv, csv };
  }


  /** Turns the applied filters into the caption lines printed under the report title. */
  private async filterCaptions(tenantId: string, params: ReportParam[], applied: Record<string, string>): Promise<string[]> {
    const captions: string[] = [];
    const period = [applied.from, applied.to].filter(Boolean);
    if (period.length === 2) captions.push(`الفترة: من ${applied.from} إلى ${applied.to}`);
    else if (applied.from) captions.push(`من تاريخ: ${applied.from}`);
    else if (applied.to) captions.push(`إلى تاريخ: ${applied.to}`);

    for (const param of params) {
      const value = applied[param.name];
      if (!value || param.kind === 'date') continue;
      if (param.options?.length) {
        captions.push(`${param.labelAr}: ${param.options.find((option) => option.value === value)?.labelAr ?? value}`);
        continue;
      }
      const source = FILTER_SOURCES[param.kind];
      if (!source) {
        captions.push(`${param.labelAr}: ${value}`);
        continue;
      }
      const name = await this.lookupName(tenantId, source, value);
      captions.push(`${param.labelAr}: ${name ?? value}`);
    }
    return captions;
  }

  private async lookupName(tenantId: string, source: { table: string; column: string }, id: string): Promise<string | null> {
    try {
      const found = await withTenantTx(this.database.db, tenantId, async (tx) =>
        rowsOf(await tx.execute(sql`SELECT ${sql.raw(source.column)} AS label FROM ${sql.raw(source.table)} WHERE tenant_id = ${tenantId} AND id = ${id} LIMIT 1`)),
      );
      const label = found[0]?.label;
      return typeof label === 'string' && label ? label : null;
    } catch {
      return null; // a caption is decoration; it must never fail an export
    }
  }
}

/**
 * Reorder, rename and hide columns per a saved layout. Hidden columns are dropped from the
 * response shape only — the rows still carry their values, so a totals row or an export
 * that a user re-enables later does not need the report to be run again.
 */
export function applyLayoutColumns(columns: ReportColumn[], layout?: Array<{ key: string; labelAr?: string; visible: boolean }> | null) {
  if (!layout?.length) return columns;
  const byKey = new Map(columns.map((column) => [column.key, column]));
  const chosen = layout
    .filter((entry) => entry.visible && byKey.has(entry.key))
    .map((entry) => ({ ...byKey.get(entry.key)!, labelAr: entry.labelAr || byKey.get(entry.key)!.labelAr }));
  return chosen.length ? chosen : columns;
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
