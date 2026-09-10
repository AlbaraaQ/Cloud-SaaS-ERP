import { apiData } from './api';

export type Account = {
  id: string;
  code: string;
  nameAr?: string;
  nameEn?: string;
  name_ar?: string;
  name_en?: string;
  type?: string;
  accountType?: string;
  parentId?: string | null;
  parent_id?: string | null;
  isPostable?: boolean;
  is_postable?: boolean;
  currencyCode?: string | null;
  level?: number;
};

/** The API has grown both camelCase DTOs and raw snake_case rows; tolerate both. */
export function nameOf(account: Account): string {
  return account.nameAr ?? account.name_ar ?? account.nameEn ?? account.name_en ?? account.code;
}
export function typeOf(account: Account): string {
  return account.type ?? account.accountType ?? '';
}
export function parentOf(account: Account): string | null {
  return account.parentId ?? account.parent_id ?? null;
}
export function postableOf(account: Account): boolean {
  return account.isPostable ?? account.is_postable ?? true;
}

export const ACCOUNT_TYPE_LABELS: Record<string, string> = {
  asset: 'أصول',
  liability: 'خصوم',
  equity: 'حقوق ملكية',
  revenue: 'إيرادات',
  expense: 'مصروفات',
};

export function listAccounts(): Promise<Account[]> {
  return apiData<Account[]>('/accounts');
}

export function accountLabel(account: Account): string {
  return `${account.code} — ${nameOf(account)}`;
}

/** Downloads any array of flat rows as UTF-8 CSV (Excel-friendly BOM). */
export function downloadCsv(fileName: string, headers: string[], rows: Array<Array<string | number>>): void {
  const escape = (value: string | number) => `"${String(value).replace(/"/g, '""')}"`;
  const content = [headers.map(escape).join(','), ...rows.map((row) => row.map(escape).join(','))].join('\n');
  const blob = new Blob(['\uFEFF' + content], { type: 'text/csv;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName;
  anchor.click();
  URL.revokeObjectURL(url);
}
