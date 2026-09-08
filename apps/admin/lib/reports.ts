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

export type ExportFormat = 'csv' | 'xlsx' | 'pdf';

export type ExportResult = {
  exportId: string;
  status: 'ready';
  format: ExportFormat;
  reportKey: string;
  titleAr: string;
  filename: string;
  mimeType: string;
  encoding: 'utf-8' | 'base64';
  content: string;
  rows: number;
};

/**
 * Exports are produced by the server, not by the browser.
 *
 * The client used to build its own CSV out of the rows already on screen, which quietly
 * dropped anything the table paged away, ignored the saved layout and could never produce a
 * real workbook. Now the report is re-run server-side with the same filters and comes back as
 * a finished file.
 */
export async function exportReport(key: string, filters: Record<string, string>, format: ExportFormat): Promise<ExportResult> {
  const query = new URLSearchParams(Object.entries(filters).filter(([, value]) => value !== '')).toString();
  return apiFetch<ExportResult>(`/reports/${encodeURIComponent(key)}/export${query ? `?${query}` : ''}`, { method: 'POST', body: JSON.stringify({ format }) });
}

/** Turns an export payload into a downloaded file without a round trip through the server. */
export function saveExport(result: ExportResult): void {
  const bytes =
    result.encoding === 'base64'
      ? Uint8Array.from(atob(result.content), (character) => character.charCodeAt(0))
      : new TextEncoder().encode(result.content);
  const blob = new Blob([bytes], { type: result.mimeType });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = result.filename;
  anchor.click();
  setTimeout(() => URL.revokeObjectURL(url), 4000);
}

/** Opens the print-ready page in its own window and hands it to the printer ("حفظ كـ PDF"). */
export function openPrintable(html: string): boolean {
  const printWindow = window.open('', '_blank', 'width=1100,height=800');
  if (!printWindow) return false;
  printWindow.document.open();
  printWindow.document.write(html);
  printWindow.document.close();
  return true;
}

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
