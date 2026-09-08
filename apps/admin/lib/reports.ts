/**
 * Report runner plumbing.
 *
 * The API describes every report (title, filters, columns, which columns get a total),
 * so the admin ships one screen that can render all of them instead of forty near-identical
 * pages. This module holds the wire types, the fetchers and the cell formatting.
 */
import { apiData, apiFetch, apiPatch, apiPost } from './api';
import { money, quantity, shortDate } from './lookups';

export type ReportParamKind = 'date' | 'branch' | 'warehouse' | 'party' | 'item' | 'category' | 'salesman' | 'costCenter' | 'select';
export type ReportColumnType = 'text' | 'money' | 'qty' | 'int' | 'date' | 'percent';

export type ReportParam = { name: string; labelAr: string; kind: ReportParamKind; options?: Array<{ value: string; labelAr: string }> };
export type ReportColumn = { key: string; labelAr: string; type: ReportColumnType };

export type ReportEntry = {
  key: string;
  titleAr: string;
  group: string;
  hintAr?: string | null;
  params: ReportParam[];
  columns: ReportColumn[];
  totals: string[];
  chart: 'bar' | 'line' | null;
};

export type ReportResult = {
  key: string;
  titleAr: string;
  group: string;
  hintAr: string | null;
  chart: 'bar' | 'line' | null;
  params: Record<string, string>;
  columns: ReportColumn[];
  rows: Array<Record<string, string>>;
  totals: Record<string, string>;
  rowCount: number;
  generatedAt: string;
};

export const REPORT_GROUP_LABELS: Record<string, string> = {
  sales: 'المبيعات',
  purchases: 'المشتريات',
  inventory: 'المستودعات',
  accounting: 'المحاسبة',
  pos: 'نقطة البيع',
  hrm: 'الموظفين والرواتب',
  marina: 'إدارة المراسي',
  projects: 'إدارة المشاريع',
};

export const REPORT_GROUP_ORDER = ['sales', 'purchases', 'inventory', 'accounting', 'pos', 'hrm', 'marina', 'projects'];

export const fetchReportCatalog = () => apiFetch<ReportEntry[]>('/reports');

/**
 * A saved layout (مصمم التقارير) — presentation only. The report's query lives on the
 * server, so a layout can rename, reorder and hide columns and preload filters, and can
 * never change what the numbers mean.
 */
export type ReportLayoutColumn = { key: string; labelAr?: string; visible: boolean };
export type ReportLayout = {
  id: string;
  reportKey: string;
  name: string;
  titleAr: string | null;
  columns: ReportLayoutColumn[];
  filters: Record<string, string>;
  isDefault: boolean;
};

export const fetchReportLayouts = (reportKey?: string) =>
  apiData<ReportLayout[]>(`/reports/layouts${reportKey ? `?report_key=${encodeURIComponent(reportKey)}` : ''}`);

export const saveReportLayout = (input: Partial<ReportLayout> & { reportKey: string; name: string }) =>
  apiPost<ReportLayout>('/reports/layouts', input);

export const updateReportLayout = (id: string, input: Partial<ReportLayout>) => apiPatch<ReportLayout>(`/reports/layouts/${id}`, input);

export const deleteReportLayout = (id: string) => apiData<unknown>(`/reports/layouts/${id}`, { method: 'DELETE' });

export function runReport(key: string, filters: Record<string, string>): Promise<ReportResult> {
  const query = new URLSearchParams(Object.entries(filters).filter(([, value]) => value !== '')).toString();
  return apiFetch<ReportResult>(`/reports/${encodeURIComponent(key)}${query ? `?${query}` : ''}`);
}

/** Formats a cell for display; the raw value stays untouched for CSV export. */
export function formatCell(value: string, type: ReportColumnType): string {
  if (value === '' || value === '—') return '—';
  switch (type) {
    case 'money':
      return money(value);
    case 'qty':
      return quantity(value);
    case 'int':
      return Number.isFinite(Number(value)) ? String(Number(value)) : value;
    case 'percent':
      return Number.isFinite(Number(value)) ? `${Number(value).toFixed(2)}%` : value;
    case 'date':
      return shortDate(value);
    default:
      return value;
  }
}

export function isNumericColumn(column: ReportColumn): boolean {
  return column.type === 'money' || column.type === 'qty' || column.type === 'int' || column.type === 'percent';
}

/** First and last day of the current month — the default window every report opens with. */
export function currentMonthRange(): { from: string; to: string } {
  const now = new Date();
  const year = now.getUTCFullYear();
  const month = now.getUTCMonth();
  const pad = (value: number) => String(value).padStart(2, '0');
  const lastDay = new Date(Date.UTC(year, month + 1, 0)).getUTCDate();
  return { from: `${year}-${pad(month + 1)}-01`, to: `${year}-${pad(month + 1)}-${pad(lastDay)}` };
}

/** Initial filter values for a report: dates prefilled, lookups left as "all". */
export function initialFilters(params: ReportParam[]): Record<string, string> {
  const range = currentMonthRange();
  const filters: Record<string, string> = {};
  for (const param of params) {
    if (param.kind === 'date') filters[param.name] = param.name === 'to' ? range.to : range.from;
    else filters[param.name] = '';
  }
  return filters;
}
