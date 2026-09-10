'use client';

import { apiData, apiList, ApiError } from './api';
import { listPeriods, periodRange, type FiscalPeriod } from './lookups';

/**
 * Posting helpers shared by the sales, purchase, POS and voucher screens.
 *
 * The document services deliberately refuse to invent accounts: they post exactly the
 * journal lines they are handed. The mapping between a document and its accounts lives
 * in `branch_posting_profiles` (الربط المحاسبي), so every screen that posts resolves the
 * profile first and builds its lines from it. When no profile resolves, the API answers
 * `ACCOUNT_PROFILE_MISSING` and the UI sends the user to the mapping screen instead of
 * guessing an account — a wrong posting account is a silently corrupted ledger.
 */

export type PostingProfile = {
  version: 1;
  salesAccountId?: string | null;
  salesReturnAccountId?: string | null;
  purchasesAccountId?: string | null;
  purchaseReturnAccountId?: string | null;
  discountGivenAccountId?: string | null;
  discountReceivedAccountId?: string | null;
  vatOutputAccountId?: string | null;
  vatInputAccountId?: string | null;
  inventoryAccountId?: string | null;
  cogsAccountId?: string | null;
  cashAccountId?: string | null;
  bankAccountId?: string | null;
  receivableAccountId?: string | null;
  payableAccountId?: string | null;
  costCenterId?: string | null;
};

export type PostingResolution = {
  branchId: string;
  docType: string;
  mapping: PostingProfile;
  matchedBranchId: string | null;
  matchedDocType: string;
};

export type JournalLine = {
  accountId: string;
  debit?: string;
  credit?: string;
  partyId?: string;
  costCenterId?: string;
  description?: string;
};

export const POSTING_ACCOUNT_LABELS: Record<string, string> = {
  salesAccountId: 'إيرادات المبيعات',
  salesReturnAccountId: 'مردودات المبيعات',
  purchasesAccountId: 'المشتريات / المخزون',
  purchaseReturnAccountId: 'مردودات المشتريات',
  discountGivenAccountId: 'خصم مسموح به',
  discountReceivedAccountId: 'خصم مكتسب',
  vatOutputAccountId: 'ضريبة المخرجات',
  vatInputAccountId: 'ضريبة المدخلات',
  inventoryAccountId: 'المخزون',
  cogsAccountId: 'تكلفة البضاعة المباعة',
  cashAccountId: 'الصندوق',
  bankAccountId: 'البنك',
  receivableAccountId: 'العملاء (ذمم مدينة)',
  payableAccountId: 'الموردون (ذمم دائنة)',
};

export const PROFILE_MISSING_HINT =
  'لا يوجد ربط محاسبي لهذا الفرع/المستند. افتح «الإعدادات ← الربط المحاسبي» وحدّد الحسابات ثم أعد المحاولة.';

export async function resolvePostingProfile(branchId: string, docType: string): Promise<PostingResolution> {
  return apiData<PostingResolution>(`/branch-posting-profiles/resolve?branchId=${branchId}&docType=${docType}`);
}

/** Reads the mapping, translating the API's 422 into a message the user can act on. */
export async function loadPostingProfile(branchId: string, docType: string): Promise<PostingProfile> {
  try {
    const resolution = await resolvePostingProfile(branchId, docType);
    return resolution.mapping;
  } catch (error) {
    if (error instanceof ApiError && error.status === 422) throw new ApiError(422, error.code, PROFILE_MISSING_HINT);
    throw error;
  }
}

export function requireAccount(profile: PostingProfile, key: keyof PostingProfile): string {
  const accountId = profile[key];
  if (!accountId || typeof accountId !== 'string') {
    throw new ApiError(422, 'ACCOUNT_PROFILE_MISSING', `الربط المحاسبي ناقص: لم يُحدَّد حساب «${POSTING_ACCOUNT_LABELS[key] ?? key}».`);
  }
  return accountId;
}

/** The open period that contains `date`, which is what every posting call needs. */
export async function periodForDate(date: string): Promise<FiscalPeriod | undefined> {
  const periods = await listPeriods();
  const covering = periods.filter((period) => {
    const range = periodRange(period);
    return range.from <= date && range.to >= date;
  });
  return covering.find((period) => period.status !== 'closed') ?? covering[0];
}

const round = (value: number) => value.toFixed(4);

export type InvoiceTotals = { subtotal: string; taxTotal: string; total: string; withholding?: string };

/**
 * Sales invoice journal: debit what we receive (cash, bank or the customer), credit the
 * revenue and the output VAT. `sale_return` mirrors it through the returns account.
 */
export function salesJournalLines(
  profile: PostingProfile,
  totals: InvoiceTotals,
  options: { settlement: 'credit' | 'cash' | 'bank'; partyId?: string; isReturn?: boolean; description?: string },
): JournalLine[] {
  const net = Number(totals.subtotal);
  const tax = Number(totals.taxTotal);
  const grand = Number(totals.total);
  if (grand <= 0) return [];

  const settlementAccount =
    options.settlement === 'cash'
      ? requireAccount(profile, 'cashAccountId')
      : options.settlement === 'bank'
        ? requireAccount(profile, 'bankAccountId')
        : requireAccount(profile, 'receivableAccountId');
  const revenueAccount = options.isReturn
    ? (profile.salesReturnAccountId ?? requireAccount(profile, 'salesAccountId'))
    : requireAccount(profile, 'salesAccountId');
  const vatAccount = tax > 0 ? requireAccount(profile, 'vatOutputAccountId') : undefined;

  const description = options.description;
  const lines: JournalLine[] = options.isReturn
    ? [
        { accountId: revenueAccount, debit: round(net), description },
        ...(vatAccount ? [{ accountId: vatAccount, debit: round(tax), description }] : []),
        { accountId: settlementAccount, credit: round(grand), partyId: options.settlement === 'credit' ? options.partyId : undefined, description },
      ]
    : [
        { accountId: settlementAccount, debit: round(grand), partyId: options.settlement === 'credit' ? options.partyId : undefined, description },
        { accountId: revenueAccount, credit: round(net), description },
        ...(vatAccount ? [{ accountId: vatAccount, credit: round(tax), description }] : []),
      ];

  return lines.filter((line) => Number(line.debit ?? line.credit) > 0);
}

/** Purchase invoice journal: debit the goods and the input VAT, credit the supplier. */
export function purchaseJournalLines(
  profile: PostingProfile,
  totals: InvoiceTotals,
  options: { settlement: 'credit' | 'cash' | 'bank'; partyId?: string; isReturn?: boolean; description?: string },
): JournalLine[] {
  const net = Number(totals.subtotal);
  const tax = Number(totals.taxTotal);
  const grand = Number(totals.total);
  if (grand <= 0) return [];

  const settlementAccount =
    options.settlement === 'cash'
      ? requireAccount(profile, 'cashAccountId')
      : options.settlement === 'bank'
        ? requireAccount(profile, 'bankAccountId')
        : requireAccount(profile, 'payableAccountId');
  const goodsAccount = options.isReturn
    ? (profile.purchaseReturnAccountId ?? requireAccount(profile, 'purchasesAccountId'))
    : requireAccount(profile, 'purchasesAccountId');
  const vatAccount = tax > 0 ? requireAccount(profile, 'vatInputAccountId') : undefined;

  const description = options.description;
  const lines: JournalLine[] = options.isReturn
    ? [
        { accountId: settlementAccount, debit: round(grand), partyId: options.settlement === 'credit' ? options.partyId : undefined, description },
        { accountId: goodsAccount, credit: round(net), description },
        ...(vatAccount ? [{ accountId: vatAccount, credit: round(tax), description }] : []),
      ]
    : [
        { accountId: goodsAccount, debit: round(net), description },
        ...(vatAccount ? [{ accountId: vatAccount, debit: round(tax), description }] : []),
        { accountId: settlementAccount, credit: round(grand), partyId: options.settlement === 'credit' ? options.partyId : undefined, description },
      ];

  return lines.filter((line) => Number(line.debit ?? line.credit) > 0);
}

export type StockLineDraft = { itemId?: string; quantity: string };

/** Inventory movements for a document, valued by the moving average on the way out. */
export function inventoryLinesFor(
  lines: StockLineDraft[],
  warehouseId: string | undefined,
  direction: 'in' | 'out',
  docType: string,
  unitCostOf?: (itemId: string) => string | undefined,
) {
  if (!warehouseId) return [];
  return lines
    .filter((line) => line.itemId && Number(line.quantity) > 0)
    .map((line) => ({
      itemId: line.itemId as string,
      warehouseId,
      qty: line.quantity,
      unitCost: direction === 'in' ? (unitCostOf?.(line.itemId as string) ?? '0') : undefined,
      direction,
      docType,
      lineId: crypto.randomUUID(),
      costing: direction === 'in' ? 'inWithCost' : 'outAtAvg',
    }));
}

type Movement = { docId: string; direction: 'in' | 'out'; totalCost: string; warehouseId: string };

/**
 * Cost of goods sold, taken from what inventory actually valued the outgoing movements at
 * rather than from a client-side guess — the two must agree or the stock account drifts.
 * Returns `0` when the document moved no stock (a service invoice).
 */
export async function cogsAmountFor(docId: string, warehouseId?: string): Promise<number> {
  const movements = await apiList<Movement>(`/inventory/movements${warehouseId ? `?warehouse_id=${warehouseId}` : ''}`);
  return movements
    .filter((movement) => movement.docId === docId && movement.direction === 'out')
    .reduce((sum, movement) => sum + Number(movement.totalCost), 0);
}
