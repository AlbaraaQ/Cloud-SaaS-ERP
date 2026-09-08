/**
 * Cross-module lookups and formatting.
 *
 * Almost every operational screen needs the same reference data (branches, warehouses,
 * items, parties, cash locations, accounts…) to fill a `<select>`. Keeping the fetchers
 * and the display formatting in one module means a screen states *what* it needs, not
 * how the endpoint happens to be shaped.
 */
import { apiList } from './api';

export type Option = { id: string; label: string };

export type Branch = { id: string; code?: string; nameAr?: string; name_ar?: string; nameEn?: string; isDefault?: boolean };
export type Warehouse = { id: string; code?: string; name?: string; nameAr?: string; name_ar?: string; branchId?: string; isDefault?: boolean };
export type Item = { id: string; sku: string; barcode?: string | null; nameAr?: string; name_ar?: string; nameEn?: string; salePrice?: string | null; sale_price?: string | null; purchasePrice?: string | null; purchase_price?: string | null; categoryId?: string; category_id?: string; baseUnitId?: string; base_unit_id?: string; taxGroupId?: string | null; kind?: string };
export type Party = { id: string; code?: string; name: string; paymentMethodId?: string | null; kind?: string; phone?: string | null; taxNo?: string | null; tax_no?: string | null; creditLimit?: string | null };
export type CashLocation = { id: string; name: string; kind?: string; accountId?: string | null; account_id?: string | null; currencyCode?: string; currency_code?: string; isDefault?: boolean; is_default?: boolean; branchId?: string | null; branch_id?: string | null };
export type Category = { id: string; code: string; nameAr?: string; name_ar?: string };
export type Unit = { id: string; code: string; nameAr?: string; name_ar?: string };
export type TaxGroup = { id: string; nameAr?: string; name_ar?: string; rate: string };
export type Salesman = { id: string; name: string; active?: boolean };
export type CostCenter = { id: string; code: string; nameAr?: string; name_ar?: string };
export type FiscalPeriod = { id: string; name: string; status: string; startDate?: string; start_date?: string; endDate?: string; end_date?: string; fiscalYearId?: string; fiscal_year_id?: string };
export type Employee = { id: string; employeeNo?: string; employee_no?: string; name: string; departmentId?: string | null; jobId?: string | null; status?: string; salaryComponents?: Record<string, string>; salary_components?: Record<string, string> };

/** Both spellings exist in the API surface (DTOs vs. raw rows); ask once, here. */
export function arabicName(row: { nameAr?: string | null; name_ar?: string | null; nameEn?: string | null; name?: string | null; code?: string }): string {
  return row.nameAr ?? row.name_ar ?? row.name ?? row.nameEn ?? row.code ?? '—';
}

export const listBranches = () => apiList<Branch>('/branches');
export const listWarehouses = () => apiList<Warehouse>('/warehouses');
export const listItems = (q?: string) => apiList<Item>(`/organization/catalog/items${q ? `?q=${encodeURIComponent(q)}` : ''}`);
export const listCategories = () => apiList<Category>('/organization/catalog/categories');
export const listUnits = () => apiList<Unit>('/organization/catalog/units');
export const listTaxGroups = () => apiList<TaxGroup>('/organization/catalog/tax-groups');
export const listParties = (kind?: string) => apiList<Party>(`/parties${kind ? `?kind=${kind}` : ''}`);
export const listCashLocations = () => apiList<CashLocation>('/cash-locations');
export const listSalesmen = () => apiList<Salesman>('/sales/salesmen');
export const listCostCenters = () => apiList<CostCenter>('/cost-centers');
export const listPeriods = () => apiList<FiscalPeriod>('/fiscal-periods');
export const listEmployees = () => apiList<Employee>('/hrm/employees');

export function branchOptions(rows: Branch[]): Option[] {
  return rows.map((row) => ({ id: row.id, label: `${row.code ? `${row.code} — ` : ''}${arabicName(row)}` }));
}
export function itemLabel(item: Item): string {
  return `${item.sku} — ${arabicName(item)}`;
}
export function partyLabel(party: Party): string {
  return `${party.code ? `${party.code} — ` : ''}${party.name}`;
}
export function cashLocationLabel(location: CashLocation): string {
  return `${location.kind === 'bank' ? '🏦' : '💵'} ${location.name}`;
}

/** The default branch/warehouse is what a single-branch tenant always wants preselected. */
export function defaultOf<T extends { isDefault?: boolean; is_default?: boolean }>(rows: T[]): T | undefined {
  return rows.find((row) => row.isDefault ?? row.is_default) ?? rows[0];
}

// ------------------------------------------------------------------ formatting

const decimal = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const plain = new Intl.NumberFormat('en-US', { maximumFractionDigits: 4 });

/** Money is carried as a string end-to-end; this only affects how it is displayed. */
export function money(value: string | number | null | undefined, currency?: string): string {
  if (value === null || value === undefined || value === '') return '—';
  const parsed = Number(value);
  const text = Number.isFinite(parsed) ? decimal.format(parsed) : String(value);
  return currency ? `${text} ${currency}` : text;
}

/** Start/end of a fiscal period, whichever spelling the endpoint used. */
export function periodRange(period: FiscalPeriod): { from: string; to: string } {
  return { from: period.startDate ?? period.start_date ?? '', to: period.endDate ?? period.end_date ?? '' };
}

export function quantity(value: string | number | null | undefined): string {
  if (value === null || value === undefined || value === '') return '—';
  const parsed = Number(value);
  return Number.isFinite(parsed) ? plain.format(parsed) : String(value);
}

export function percent(value: string | number | null | undefined): string {
  if (value === null || value === undefined || value === '') return '—';
  const parsed = Number(value);
  return Number.isFinite(parsed) ? `${plain.format(parsed * 100)}%` : String(value);
}

export function shortDate(value: string | Date | null | undefined): string {
  if (!value) return '—';
  const date = typeof value === 'string' ? new Date(value) : value;
  if (Number.isNaN(date.getTime())) return String(value);
  return date.toISOString().slice(0, 10);
}

export function dateTime(value: string | Date | null | undefined): string {
  if (!value) return '—';
  const date = typeof value === 'string' ? new Date(value) : value;
  if (Number.isNaN(date.getTime())) return String(value);
  return date.toISOString().slice(0, 16).replace('T', ' ');
}

export function today(): string {
  return new Date().toISOString().slice(0, 10);
}

export const DOC_STATUS_LABELS: Record<string, string> = {
  draft: 'مسودة',
  posted: 'مرحّل',
  paid: 'مدفوع',
  voided: 'ملغي',
  cancelled: 'ملغي',
  reversed: 'معكوس',
  open: 'مفتوح',
  closed: 'مغلق',
  sent: 'مُرسل',
  received: 'مُستلم',
  approved: 'معتمد',
  pending: 'قيد الانتظار',
  active: 'نشط',
};

export function statusLabel(status?: string | null): string {
  if (!status) return '—';
  return DOC_STATUS_LABELS[status] ?? status;
}
