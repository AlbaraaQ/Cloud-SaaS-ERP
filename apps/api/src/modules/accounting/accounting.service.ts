import { Inject, Injectable } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { and, asc, desc, eq, gte, ilike, inArray, isNull, lt, lte, or, sql } from 'drizzle-orm';
import { DomainError } from '@erp/contracts';
import {
  accounts,
  branches,
  vouchers,
  costCenters,
  fiscalPeriods,
  fiscalYears,
  journalEntries,
  journalEntryLines,
  newId,
  periodModuleLocks,
  withTenantTx,
  type DatabaseHandle,
  type DrizzleTx,
} from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';
import { SequencesService } from '../platform-services/index.js';

export type AccountType = 'asset' | 'liability' | 'equity' | 'revenue' | 'expense';

export type AccountInput = {
  code: string;
  nameAr: string;
  nameEn?: string;
  type: AccountType;
  subtype?: string;
  /** Optional: derived from `type` when the caller omits it (assets/expenses are debit). */
  normalBalance?: 'debit' | 'credit';
  parentId?: string;
  isPostable?: boolean;
  /** 📅 تاريخ فتح الحساب — `frmAccountsTree`. */
  openedAt?: string | null;
  /** 💰 الرصيد الافتتاحي — `frmAccountsTree`. */
  openingBalance?: string | null;
  /** 📊 مركز التكلفة — the card's default centre (`frmAccountsTree`). */
  costCenterId?: string | null;
};

/**
 * What `GET /accounts?withBalances=1` adds to every row: the account's own movement and
 * the rolled-up total that the desktop shows on each node of 📂 شجرة الحسابات
 * (`frmAccountsDirectory.xaml` binds `trBalance`).
 */
export type AccountBalance = {
  /** حركة الحساب نفسه فقط — بدون الأبناء. */
  ownDebit: string;
  ownCredit: string;
  /** الرصيد = مدين − دائن (موقّع بحسب حركة الحساب لا بطبيعته). */
  ownBalance: string;
  /** الرصيد مضافاً إليه أبناءه — ما يعرضه الديسكتوب على العقدة. */
  debit: string;
  credit: string;
  balance: string;
  /** كم حساباً فرعياً دخل في هذا الرصيد. */
  descendants: number;
};

export type AccountQuery = {
  q?: string;
  type?: string;
  branchId?: string;
  withBalances?: boolean;
};

/**
 * 📄 كشف الحساب — the filters of `Form_WPF/frmAccountBalance.xaml` («كشف حساب تفصيلي»)
 * and `frmAccountsStatement.xaml` («كشف حساب رئيسي»). Every default here is the one the
 * window opens with, except `fullPeriod`, which defaults to *no date filter at all* so
 * that a caller that sends nothing gets exactly the ledger the old endpoint returned.
 */
export type StatementQuery = {
  from?: string;
  to?: string;
  /** `الفرع` — absent means `كل الفروع`, as the checked `chkAllBranches` does. */
  branchId?: string;
  /** كشف حساب رئيسي: the account *and* everything beneath it, as the SP's `AccountHierarchy` does. */
  withDescendants?: boolean;
  /** 📊 طريقة العرض — `تجميعي (ملخص)` groups the lines of one entry into one row. */
  summary?: boolean;
  /** `فترة كاملة (من البداية)` — no `from`, and therefore no رصيد سابق row. */
  fullPeriod?: boolean;
  /** `عدم إظهار الرصيد السابق`. */
  hidePreviousBalance?: boolean;
};

/**
 * One row of the statement. `rank` 0 is the `رصيد سابق` row the desktop prepends
 * (`AddPreviousBalanceRow`); `الرصيد` and `الحالة` are only filled for a single account,
 * because `كشف حساب رئيسي` has no running-balance column in the desktop either — a
 * running total over accounts of different natures is not a number anyone can read.
 */
export type StatementRow = {
  rank: number;
  date: string;
  /** الرقم العام — `Entry.GlobalID` in the desktop's grid. */
  entryId: string | null;
  /** رقم السند. */
  number: string | null;
  /** الفرع — named, as the desktop resolves `Entry.branch` through `Branches`. */
  branchName: string | null;
  /** النوع — `EntryTypes` (قيد مبيعات، سند قبض، …). */
  entryType: string;
  /** البيان — the line's own note when تفصيلي, the entry's when تجميعي. */
  description: string | null;
  debit: string;
  credit: string;
  /** الرصيد — running, signed by the account's nature. */
  runningBalance: string | null;
  /** الحالة — `مدين` / `دائن`. */
  balanceStatus: string | null;
  /** رمز الحساب / الحساب — the columns of كشف حساب رئيسي. */
  accountCode: string | null;
  accountName: string | null;
};

export type StatementTotals = {
  /** إجمالي مدين / إجمالي دائن — over the period's movements, not the رصيد سابق row. */
  debit: string;
  credit: string;
  /** رصيد الفترة (مدين) / رصيد الفترة (دائن) — the two windows split the balance by side. */
  periodDebit: string;
  periodCredit: string;
  /** الرصيد النهائي — what the account closes the period on (`الرصيد` of the last row). */
  closing: string;
  closingStatus: string | null;
};

/**
 * النوع of an entry, in the words of the desktop's own `EntryTypes` table
 * (`CrystalLiteDB.txt` L3341–L3360): قيد إفتتاحي · قيد مشتريات · قيد مبيعات ·
 * قيد نقطة بيع · تسوية جردية · سند قبض من عميل · سند صرف لمورد · سند قبض · سند صرف ·
 * قيد اليومية · بضاعة أول مدة · قيد إضافات · إغلاق اليومية · مرتجع مبيعات · …
 */
const ENTRY_TYPE_LABELS: Record<string, string> = {
  opening: 'قيد إفتتاحي',
  purchase_invoice: 'قيد مشتريات',
  sales_invoice: 'قيد مبيعات',
  pos_sale: 'قيد نقطة بيع',
  inventory_adjust: 'تسوية جردية',
  shift_close: 'إغلاق اليومية',
  return_sale: 'مرتجع مبيعات',
  return_purchase: 'مرتجع مشتريات',
  contract_invoice: 'قيد فاتورة عقد',
};

function entryTypeOf(row: {
  kind: string;
  sourceType: string | null;
  voucherKind: string | null;
}): string {
  if (row.kind === 'reversal') return 'قيد عكسي';
  if (row.voucherKind === 'receipt') return 'سند قبض';
  if (row.voucherKind === 'payment') return 'سند صرف';
  return ENTRY_TYPE_LABELS[row.sourceType ?? ''] ?? 'قيد اليومية';
}

export type AccountPatch = Partial<AccountInput> & { allowManual?: boolean };
/**
 * 🌳 شجرة مراكز التكلفة — `Form_WPF/frmCostCenter.xaml` shows every centre in a tree
 * (`LoadTree` / `BuildTreeNodes`, joining `ParentCode`) and `frmCostCenterBalance.xaml`
 * («تقرير مركز كلفة») reports one of them with its balance. `withBalances` adds the
 * figure the cloud's list never carried: the centre's own movement plus its children's,
 * from **posted** entries only.
 */
export type CostCenterQuery = {
  q?: string;
  branchId?: string;
  withBalances?: boolean;
};

export type CostCenterBalance = {
  ownDebit: string;
  ownCredit: string;
  /** رصيد المركز وحده دون أبنائه. */
  ownBalance: string;
  /** الرصيد مضافاً إليه أبناؤه — ما تُظهره الشجرة على العقدة الأب. */
  debit: string;
  credit: string;
  balance: string;
  children: number;
};

/** `📑 نوع التقرير` and the period filters of `frmCostCenterBalance`. */
export type CostCenterStatementQuery = {
  from?: string;
  to?: string;
  branchId?: string;
  /** `اسم الحساب` — narrow the centre's report to one account. */
  accountId?: string;
  /** `تجميعي` collapses the lines of one entry; the default is `تفصيلي`. */
  summary?: boolean;
  fullPeriod?: boolean;
  hidePreviousBalance?: boolean;
};

/** A cost centre as the list returns it — and, with `withBalances`, its node's balance. */
export type CostCenterRow = {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string | null;
  parentId: string | null;
  branchId: string | null;
  parentName?: string | null;
  level?: number;
  /** 🏷️ النوع — `🟢 رئيسي` / `🔵 فرعي`. */
  kind?: 'main' | 'sub';
  balance?: CostCenterBalance;
};

export type CostCenterPatch = Partial<CostCenterInput>;

export type CostCenterInput = {
  code: string;
  nameAr: string;
  nameEn?: string;
  parentId?: string;
  branchId?: string;
};

export type FiscalYearInput = {
  name: string;
  startDate: string;
  endDate: string;
  /** When true (default) twelve monthly periods are generated inside the year. */
  generateMonthlyPeriods?: boolean;
};

export type JournalQuery = {
  from?: string;
  to?: string;
  branchId?: string;
  fiscalPeriodId?: string;
  status?: string;
  limit?: number;
};

/** Money in this module is a decimal string end to end; `money` is the one conversion. */
const money = (value: string | number | null | undefined): Decimal =>
  new Decimal(value === null || value === undefined || value === '' ? '0' : String(value));

/**
 * الحالة — which side the money is on: `مدين` / `دائن`, and `رصيد متوازن` when the
 * account is square (the wording of `frmAccountsStatement.xaml.cs` L346–L352).
 *
 * The desktop derives this from a nature-signed running total
 * (`balanceStatus = runningBalance >= 0 ? "مدين" : "دائن"`), which inverts the answer for
 * every credit-natured account: a liability sitting on its own credit side is reported
 * `مدين`. The side is a fact about the balance, not about the account, so here it is read
 * from the balance itself.
 */
function statusOf(natureSigned: Decimal): string {
  if (natureSigned.isZero()) return 'رصيد متوازن';
  return natureSigned.isPositive() ? 'مدين' : 'دائن';
}

/** ACCOUNTING_ARCHITECTURE §2 — the natural side of each account class. */
export function defaultNormalBalance(type: AccountType): 'debit' | 'credit' {
  return type === 'asset' || type === 'expense' ? 'debit' : 'credit';
}

export type JournalLineInput = {
  accountId: string;
  debit?: string;
  credit?: string;
  description?: string;
  partyId?: string;
  costCenterId?: string;
  branchId?: string;
  /** المندوب — `FrmNewEntry.xaml` `colSalesman`. */
  salesmanId?: string;
};

/**
 * 📒 إنشاء قيد يومية — the card of `Form_WPF/FrmNewEntry.xaml`: `رقم القيد` (allocated
 * on save) · `📅 التاريخ` · `⏰ الوقت` · `🔑 الرقم العام` (the id) · `✅ قيد ضريبي` ·
 * `📝 الملاحظة`. Every field is optional here, because every entry written by a sale, a
 * voucher or a shift close is posted through this same call without ever seeing the
 * window.
 */
export type JournalCardInput = {
  time?: string | null;
  isVat?: boolean;
};

@Injectable()
export class AccountingService {
  constructor(
    @Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle,
    private readonly sequences: SequencesService,
  ) {}

  /**
   * 📂 شجرة الحسابات و 📋 تفاصيل الحسابات — `frmAccountsDirectory.xaml(.cs)`.
   *
   * The desktop's directory is two things at once: a tree whose every node carries
   * `trBalance`, and a details grid (الحساب الرئيسي · رمز الحساب · اسم الحساب · الفرع ·
   * الرصيد · كشف حساب · تعديل) filled from the selected node. So the list has to be able
   * to search (`🔍` → `frmAccountSrch`), to narrow by الفرع, and — the part that was
   * missing — to return **balances**.
   *
   * A balance is read from **posted** entries only: `المسوّدة ليست مالاً`, and a tree
   * that counts drafts shows an accountant numbers that vanish. The parent's figure is
   * the sum of its children plus its own movement, rolled up here rather than in the
   * browser, so a node and the ميزان that contains it can never disagree.
   */
  async listAccounts(tenantId: string, query: AccountQuery = {}) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const rows = await tx
        .select()
        .from(accounts)
        .where(
          and(
            eq(accounts.tenantId, tenantId),
            isNull(accounts.deletedAt),
            query.type ? eq(accounts.type, query.type) : undefined,
            query.branchId ? eq(accounts.branchId, query.branchId) : undefined,
            query.q
              ? or(ilike(accounts.code, `%${query.q}%`), ilike(accounts.nameAr, `%${query.q}%`))
              : undefined,
          ),
        )
        .orderBy(asc(accounts.code));
      if (!query.withBalances) return rows;

      const movements = await tx
        .select({
          accountId: journalEntryLines.accountId,
          debit: sql<string>`COALESCE(SUM(${journalEntryLines.debit}), 0)::text`,
          credit: sql<string>`COALESCE(SUM(${journalEntryLines.credit}), 0)::text`,
        })
        .from(journalEntryLines)
        .innerJoin(journalEntries, eq(journalEntries.id, journalEntryLines.entryId))
        .where(
          and(eq(journalEntryLines.tenantId, tenantId), eq(journalEntries.status, 'posted')),
        )
        .groupBy(journalEntryLines.accountId);

      const own = new Map<string, { debit: Decimal; credit: Decimal }>();
      for (const row of movements)
        own.set(row.accountId, { debit: money(row.debit), credit: money(row.credit) });

      const byId = new Map(rows.map((row) => [row.id, row]));
      const rolled = new Map<string, { debit: Decimal; credit: Decimal; descendants: number }>();
      for (const row of rows) {
        const self = own.get(row.id) ?? { debit: new Decimal(0), credit: new Decimal(0) };
        rolled.set(row.id, { debit: self.debit, credit: self.credit, descendants: 0 });
      }
      // `path` is an ltree of ids, root first (`root.mid.leaf`), so every ancestor of a
      // row is simply a prefix of its own path.
      for (const row of rows) {
        const self = rolled.get(row.id)!;
        const ancestors = row.path.split('.').slice(0, -1);
        for (const ancestorId of ancestors) {
          const target = rolled.get(ancestorId);
          if (!target) continue;
          target.debit = target.debit.plus(self.debit);
          target.credit = target.credit.plus(self.credit);
          target.descendants += 1;
        }
      }
      /**
       * 💰 الرصيد الافتتاحي (`frmAccountsTree`) belongs to the account before any entry
       * was written, so it is added to the account's own row and rolls up from there.
       */
      for (const row of rows) {
        const opening = money(row.openingBalance ?? '0');
        if (opening.isZero()) continue;
        const self = rolled.get(row.id)!;
        const side = row.normalBalance === 'credit' ? 'credit' : 'debit';
        self[side] = self[side].plus(opening);
        for (const ancestorId of row.path.split('.').slice(0, -1)) {
          const target = rolled.get(ancestorId);
          if (!target) continue;
          target[side] = target[side].plus(opening);
        }
      }

      return rows.map((row) => {
        const self = own.get(row.id) ?? { debit: new Decimal(0), credit: new Decimal(0) };
        const subtree = rolled.get(row.id)!;
        const totals: AccountBalance = {
          ownDebit: self.debit.toFixed(4),
          ownCredit: self.credit.toFixed(4),
          ownBalance: self.debit.minus(self.credit).toFixed(4),
          debit: subtree.debit.toFixed(4),
          credit: subtree.credit.toFixed(4),
          balance: subtree.debit.minus(subtree.credit).toFixed(4),
          descendants: subtree.descendants,
        };
        return { ...row, parentName: row.parentId ? (byId.get(row.parentId)?.nameAr ?? null) : null, balance: totals };
      });
    });
  }

  async createAccount(tenantId: string, input: AccountInput) {
    const id = newId();
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      // `accounts.path` is an ltree of account ids, root first. A child hangs off its
      // parent's path (`<parent path>.<own id>`) and sits one level deeper — that is what
      // makes subtree queries (`path <@ ancestor`) and the depth column agree, both here
      // and in the seeded chart of accounts.
      const parent = input.parentId ? await this.readAccountRow(tx, tenantId, input.parentId) : undefined;
      if (input.parentId && !parent) {
        throw new DomainError('NOT_FOUND', 'Parent account not found', 404);
      }

      await tx.insert(accounts).values({
        id,
        tenantId,
        code: input.code,
        nameAr: input.nameAr,
        nameEn: input.nameEn ?? null,
        type: input.type,
        subtype: input.subtype ?? null,
        normalBalance: input.normalBalance ?? defaultNormalBalance(input.type),
        parentId: input.parentId ?? null,
        level: parent ? parent.level + 1 : 0,
        path: parent ? `${parent.path}.${id}` : id,
        isPostable: input.isPostable ?? true,
        openedAt: input.openedAt ?? null,
        openingBalance: input.openingBalance ?? '0',
        costCenterId: input.costCenterId ?? null,
      });
    });
    return this.readAccount(tenantId, id);
  }

  /**
   * Edit an account card. What is editable depends on whether the account has ever been
   * posted to: names and flags always are, but `code`, `type` and `normalBalance` are
   * frozen once journal lines exist — those three define how every existing balance is
   * read, so changing them would retroactively re-state closed periods.
   */
  async updateAccount(tenantId: string, id: string, patch: AccountPatch) {
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      const [current] = await tx.select().from(accounts).where(and(eq(accounts.id, id), eq(accounts.tenantId, tenantId), isNull(accounts.deletedAt)));
      if (!current) throw new DomainError('NOT_FOUND', 'Account not found', 404);

      const posted = await this.accountHasLines(tx, tenantId, id);
      const changesIdentity =
        (patch.code !== undefined && patch.code !== current.code) ||
        (patch.type !== undefined && patch.type !== current.type) ||
        (patch.normalBalance !== undefined && patch.normalBalance !== current.normalBalance);
      if (posted && changesIdentity) {
        throw new DomainError('ACCOUNT_POSTED', 'This account already carries journal entries; its number, nature and side can no longer be changed', 409);
      }

      // Only an explicit request to make the account postable is checked against its
      // children: an older chart may already contain a parent that was left postable, and
      // refusing to rename it would make that history impossible to clean up.
      if (patch.isPostable === true && (await this.countAccountChildren(tx, tenantId, id)) > 0) {
        throw new DomainError('ACCOUNT_HAS_CHILDREN', 'A parent account cannot be a posting account', 422);
      }
      const isPostable = patch.isPostable ?? current.isPostable;

      let { level, path, parentId } = current;
      const reparent = patch.parentId !== undefined && (patch.parentId ?? null) !== current.parentId;
      if (reparent) {
        const nextParent = patch.parentId ? await this.readAccountRow(tx, tenantId, patch.parentId) : undefined;
        if (patch.parentId && !nextParent) throw new DomainError('NOT_FOUND', 'Parent account not found', 404);
        if (nextParent && (nextParent.id === id || nextParent.path.split('.').includes(id))) {
          throw new DomainError('ACCOUNT_CYCLE', 'An account cannot be moved under one of its own branches', 422);
        }
        parentId = patch.parentId ?? null;
        level = nextParent ? nextParent.level + 1 : 0;
        path = nextParent ? `${nextParent.path}.${id}` : id;

        // The whole branch travels with the account: every descendant keeps its relative
        // position but is re-rooted, otherwise `path <@ ancestor` queries would lose them.
        const depthShift = level - current.level;
        await tx.execute(sql`
          UPDATE accounts
             SET path = ${`${path}`}::ltree || subpath(path, nlevel(${current.path}::ltree)),
                 level = level + ${depthShift},
                 updated_at = now()
           WHERE tenant_id = ${tenantId}
             AND path <@ ${current.path}::ltree
             AND id <> ${id}
        `);
      }

      await tx
        .update(accounts)
        .set({
          code: patch.code ?? current.code,
          nameAr: patch.nameAr ?? current.nameAr,
          nameEn: patch.nameEn === undefined ? current.nameEn : patch.nameEn,
          type: patch.type ?? current.type,
          subtype: patch.subtype === undefined ? current.subtype : patch.subtype,
          normalBalance: patch.normalBalance ?? current.normalBalance,
          isPostable,
          allowManual: patch.allowManual ?? current.allowManual,
          /**
           * 📅 تاريخ فتح الحساب · 💰 الرصيد الافتتاحي · 📊 مركز التكلفة —
           * `frmAccountsTree`. The opening balance is editable only while the account
           * carries no posted line: it is the balance *before* the ledger starts, so
           * changing it after the fact re-states every period that already closed.
           */
          openedAt: patch.openedAt === undefined ? current.openedAt : patch.openedAt,
          openingBalance:
            patch.openingBalance === undefined
              ? current.openingBalance
              : (() => {
                  if (posted && money(patch.openingBalance).cmp(money(current.openingBalance)) !== 0)
                    throw new DomainError(
                      'ACCOUNT_POSTED',
                      'This account already carries journal entries; its opening balance can no longer be changed',
                      409,
                    );
                  return money(patch.openingBalance).toFixed(4);
                })(),
          costCenterId: patch.costCenterId === undefined ? current.costCenterId : patch.costCenterId,
          parentId,
          level,
          path,
          updatedAt: new Date(),
        })
        .where(and(eq(accounts.id, id), eq(accounts.tenantId, tenantId)));
    });
    return this.readAccount(tenantId, id);
  }

  /**
   * Deleting an account is only ever allowed while it is still empty — no journal lines,
   * no opening balances, no children. Anything else is refused rather than hidden,
   * because a chart of accounts with a hole in it stops reconciling.
   */
  async deleteAccount(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [current] = await tx.select({ id: accounts.id }).from(accounts).where(and(eq(accounts.id, id), eq(accounts.tenantId, tenantId), isNull(accounts.deletedAt)));
      if (!current) throw new DomainError('NOT_FOUND', 'Account not found', 404);
      if (await this.accountHasLines(tx, tenantId, id)) {
        throw new DomainError('ACCOUNT_POSTED', 'This account carries journal entries and cannot be deleted', 409);
      }
      if ((await this.countAccountChildren(tx, tenantId, id)) > 0) {
        throw new DomainError('ACCOUNT_HAS_CHILDREN', 'Delete or move the sub-accounts first', 409);
      }
      await tx.update(accounts).set({ deletedAt: new Date(), updatedAt: new Date() }).where(and(eq(accounts.id, id), eq(accounts.tenantId, tenantId)));
      return { id, deleted: true };
    });
  }

  private async accountHasLines(tx: DrizzleTx, tenantId: string, id: string) {
    const result = await tx.execute(sql`
      SELECT EXISTS (SELECT 1 FROM journal_entry_lines WHERE tenant_id = ${tenantId} AND account_id = ${id})
          OR EXISTS (SELECT 1 FROM opening_balances WHERE tenant_id = ${tenantId} AND account_id = ${id}) AS used
    `);
    return Boolean((result.rows[0] as { used: boolean }).used);
  }

  private async countAccountChildren(tx: DrizzleTx, tenantId: string, id: string) {
    const result = await tx.execute(sql`SELECT count(*)::int AS total FROM accounts WHERE tenant_id = ${tenantId} AND parent_id = ${id} AND deleted_at IS NULL`);
    return (result.rows[0] as { total: number }).total;
  }

  private async readAccountRow(tx: DrizzleTx, tenantId: string, id: string) {
    const [row] = await tx
      .select({ id: accounts.id, path: accounts.path, level: accounts.level })
      .from(accounts)
      .where(and(eq(accounts.id, id), eq(accounts.tenantId, tenantId), isNull(accounts.deletedAt)));
    return row;
  }

  async readAccount(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [row] = await tx.select().from(accounts).where(and(eq(accounts.id, id), eq(accounts.tenantId, tenantId), isNull(accounts.deletedAt)));
      if (!row) throw new Error('Account not found');
      return row;
    });
  }

  async listPeriods(tenantId: string) {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx.select().from(fiscalPeriods).where(eq(fiscalPeriods.tenantId, tenantId)),
    );
  }

  async closePeriod(tenantId: string, periodId: string) {
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      const [period] = await tx.select().from(fiscalPeriods).where(and(eq(fiscalPeriods.id, periodId), eq(fiscalPeriods.tenantId, tenantId)));
      if (!period) throw new Error('Fiscal period not found');
      if (period.status === 'closed') return;
      const [draft] = await tx.select({ id: journalEntries.id }).from(journalEntries).where(and(eq(journalEntries.tenantId, tenantId), eq(journalEntries.fiscalPeriodId, periodId), eq(journalEntries.status, 'draft'))).limit(1);
      if (draft) throw new Error('Fiscal period contains draft journals');
      await tx.update(fiscalPeriods).set({ status: 'closed', closedAt: new Date() }).where(eq(fiscalPeriods.id, periodId));
    });
  }

  async reopenPeriod(tenantId: string, periodId: string, reason: string) {
    if (!reason.trim()) throw new Error('Reopen reason is required');
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      const [period] = await tx.select().from(fiscalPeriods).where(and(eq(fiscalPeriods.id, periodId), eq(fiscalPeriods.tenantId, tenantId)));
      if (!period) throw new Error('Fiscal period not found');
      await tx.update(fiscalPeriods).set({ status: 'open', closedAt: null, closedBy: null }).where(eq(fiscalPeriods.id, periodId));
    });
  }

  async reverseJournal(tenantId: string, entryId: string, input: { branchId: string; fiscalPeriodId: string; date: string; reason: string }) {
    if (!input.reason.trim()) throw new Error('Reversal reason is required');
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [original] = await tx.select().from(journalEntries).where(and(eq(journalEntries.id, entryId), eq(journalEntries.tenantId, tenantId)));
      if (!original || original.status !== 'posted') throw new Error('Only posted journals can be reversed');
      const [existing] = await tx.select({ id: journalEntries.id }).from(journalEntries).where(eq(journalEntries.reversalOf, entryId));
      if (existing) throw new Error('Journal was already reversed');
      const [period] = await tx.select().from(fiscalPeriods).where(and(eq(fiscalPeriods.id, input.fiscalPeriodId), eq(fiscalPeriods.tenantId, tenantId)));
      if (!period || period.status !== 'open') throw new Error('Reversal period is closed or unavailable');
      await this.assertModuleUnlocked(tx, tenantId, input.fiscalPeriodId, 'accounting');
      const lines = await tx.select().from(journalEntryLines).where(eq(journalEntryLines.entryId, entryId));
      const reversalId = newId();
      await tx.insert(journalEntries).values({ id: reversalId, tenantId, branchId: input.branchId, fiscalPeriodId: input.fiscalPeriodId, date: input.date, kind: 'reversal', status: 'posted', description: input.reason, reversalOf: entryId, postedAt: new Date() });
      /**
       * The mirror of every line — amounts swapped *and every dimension carried*. A
       * reversal that drops the cost centre, the branch or the salesman leaves those
       * reports holding money the ledger no longer has: 🌳 شجرة مراكز التكلفة would keep
       * spending that was reversed, which is exactly what it did until migration `0047`
       * made the `void` below stick.
       */
      await tx.insert(journalEntryLines).values(lines.map((line) => ({
        entryId: reversalId,
        lineNo: line.lineNo,
        tenantId,
        accountId: line.accountId,
        debit: line.credit,
        credit: line.debit,
        partyId: line.partyId,
        costCenterId: line.costCenterId,
        salesmanId: line.salesmanId,
        branchId: line.branchId,
        currencyCode: line.currencyCode,
        currencyAmount: line.currencyAmount,
        fxRate: line.fxRate,
        description: line.description,
      })));
      /**
       * The original keeps its status, on purpose. The reversal is the mirror of its
       * lines — debit for credit — so the two together already net to nothing; marking
       * the original `void` as well would subtract the amount twice, since every balance
       * in this module counts `status = 'posted'` only. What the reversal leaves behind
       * is `reversalOf`, which is how the register knows an entry was undone.
       *
       * (Until migration `0047` this line ran and did nothing: the immutability trigger
       * allowed the `void` and then returned `OLD`, discarding it. Which is the only
       * reason a reversal ever balanced.)
       */
      return { id: reversalId, reversalOf: entryId };
    });
  }

  async trialBalance(tenantId: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const rows = await tx.select({ accountId: journalEntryLines.accountId, debit: journalEntryLines.debit, credit: journalEntryLines.credit })
        .from(journalEntryLines)
        .innerJoin(journalEntries, eq(journalEntries.id, journalEntryLines.entryId))
        .where(and(eq(journalEntryLines.tenantId, tenantId), eq(journalEntries.status, 'posted')));
      const totals = new Map<string, { debit: Decimal; credit: Decimal }>();
      for (const row of rows) {
        const current = totals.get(row.accountId) ?? { debit: new Decimal(0), credit: new Decimal(0) };
        current.debit = current.debit.plus(row.debit);
        current.credit = current.credit.plus(row.credit);
        totals.set(row.accountId, current);
      }
      // eslint-disable-next-line no-restricted-syntax
      return [...totals.entries()].map(([accountId, total]) => ({ accountId, debit: total.debit.toFixed(4), credit: total.credit.toFixed(4), balance: total.debit.minus(total.credit).toFixed(4) }));
    });
  }

  /**
   * 📄 كشف الحساب — the two statement windows of the desktop in one endpoint.
   *
   * `Form_WPF/frmAccountBalance.xaml` («كشف حساب تفصيلي») is the model: الرصيد is a
   * running total (`runningBalance += dept - credit`, L216), the opening row is prepended
   * as `رصيد سابق` / «رصيد مرحل من فترة سابقة» (`AddPreviousBalanceRow`), and
   * `تجميعي (ملخص)` collapses the lines of one entry into one row while `تفصيلي (كامل)`
   * keeps every line (`GetAccountMovements`, L307). `frmAccountsStatement.xaml`
   * («كشف حساب رئيسي») is the same report over an account **and its descendants** —
   * the stored procedure `GetAccountStatement` walks `AccountHierarchy` — and it shows
   * `رمز الحساب` and `الحساب` instead of a running balance.
   *
   * Only **posted** entries count (`Entry.state = 1` in the desktop). The caller that
   * sends nothing still gets exactly the ledger this endpoint always returned; every new
   * column and every new key in the envelope is additive.
   */
  async accountStatement(tenantId: string, accountId: string, query: StatementQuery = {}) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [account] = await tx
        .select()
        .from(accounts)
        .where(and(eq(accounts.id, accountId), eq(accounts.tenantId, tenantId), isNull(accounts.deletedAt)))
        .limit(1);
      if (!account) throw new DomainError('NOT_FOUND', 'Account not found', 404);

      const debitNature = (account.normalBalance ?? defaultNormalBalance(account.type as AccountType)) !== 'credit';
      /**
       * كشف حساب رئيسي walks the whole branch; `path` is `<root>.<…>.<own id>`, so a
       * descendant is any account whose path *starts with* this account's path.
       */
      const inBranch = query.withDescendants
        ? sql`${accounts.path} <@ ${account.path}::ltree`
        : eq(accounts.id, accountId);

      const period = query.fullPeriod
        ? []
        : [query.from ? gte(journalEntries.date, query.from) : undefined, query.to ? lte(journalEntries.date, query.to) : undefined].filter(
            (clause) => clause !== undefined,
          );
      const where = and(
        eq(journalEntryLines.tenantId, tenantId),
        eq(journalEntries.status, 'posted'),
        inBranch,
        query.branchId ? eq(journalEntries.branchId, query.branchId) : undefined,
        ...period,
      );

      // ── 1. الرصيد السابق — everything posted before `from`, plus 💰 الرصيد الافتتاحي
      //      (migration 0045), which is money on the books before any entry at all.
      /**
       * With no `from` there is no "before" — `فترة كاملة (من البداية)` starts at zero,
       * and counting the whole ledger as an opening would count it twice: once here and
       * once again as the period's movements.
       */
      const before = query.from
        ? await tx
            .select({
              debit: sql<string>`COALESCE(SUM(${journalEntryLines.debit}), 0)::text`,
              credit: sql<string>`COALESCE(SUM(${journalEntryLines.credit}), 0)::text`,
            })
            .from(journalEntryLines)
            .innerJoin(journalEntries, eq(journalEntries.id, journalEntryLines.entryId))
            .innerJoin(accounts, eq(accounts.id, journalEntryLines.accountId))
            .where(
              and(
                eq(journalEntryLines.tenantId, tenantId),
                eq(journalEntries.status, 'posted'),
                inBranch,
                query.branchId ? eq(journalEntries.branchId, query.branchId) : undefined,
                lt(journalEntries.date, query.from),
              ),
            )
            .then((rows) => rows[0])
        : undefined;
      const movedBefore = money(before?.debit).minus(money(before?.credit));
      const openingSigned = query.withDescendants ? new Decimal(0) : money(account.openingBalance);
      // Nature-signed: how much the account holds *on its own side* before the period.
      const opening = debitNature ? movedBefore.plus(openingSigned) : movedBefore.negated().plus(openingSigned);

      // ── 2. حركات الفترة
      const select = {
        entryId: journalEntries.id,
        date: journalEntries.date,
        number: journalEntries.number,
        entryDescription: journalEntries.description,
        // In تجميعي mode one row covers many lines, so the note is aggregated; the row
        // shows the entry's own البيان anyway.
        lineDescription: sql<string | null>`MIN(${journalEntryLines.description})`,
        debit: sql<string>`COALESCE(SUM(${journalEntryLines.debit}), 0)::text`,
        credit: sql<string>`COALESCE(SUM(${journalEntryLines.credit}), 0)::text`,
        accountCode: accounts.code,
        accountNameAr: accounts.nameAr,
        branchName: branches.nameAr,
        kind: journalEntries.kind,
        sourceType: journalEntries.sourceType,
        voucherKind: vouchers.kind,
      };
      // 📊 تجميعي (ملخص) — one row per entry (per account); تفصيلي — one row per line.
      const grouped = query.summary
        ? [
            journalEntries.id,
            journalEntries.date,
            journalEntries.number,
            journalEntries.description,
            accounts.code,
            accounts.nameAr,
            branches.nameAr,
            journalEntries.kind,
            journalEntries.sourceType,
            vouchers.kind,
          ]
        : [
            journalEntries.id,
            journalEntryLines.lineNo,
            journalEntries.date,
            journalEntries.number,
            journalEntries.description,
            journalEntryLines.description,
            journalEntryLines.debit,
            journalEntryLines.credit,
            accounts.code,
            accounts.nameAr,
            branches.nameAr,
            journalEntries.kind,
            journalEntries.sourceType,
            vouchers.kind,
          ];
      const movements = await tx
        .select(select)
        .from(journalEntryLines)
        .innerJoin(journalEntries, eq(journalEntries.id, journalEntryLines.entryId))
        .innerJoin(accounts, eq(accounts.id, journalEntryLines.accountId))
        .leftJoin(branches, eq(branches.id, journalEntries.branchId))
        .leftJoin(vouchers, eq(vouchers.journalEntryId, journalEntries.id))
        .where(where)
        .groupBy(...grouped)
        .orderBy(
          asc(journalEntries.date),
          asc(journalEntries.number),
          ...(query.summary ? [] : [asc(journalEntryLines.lineNo)]),
        );

      // ── 3. الرصيد المتراكم — signed by the account's nature, as the window does.
      const rows: StatementRow[] = [];
      const periodTotals = { debit: new Decimal(0), credit: new Decimal(0) };
      let running = opening;

      /**
       * `running` is kept nature-signed so that الرصيد grows on the account's own side,
       * but `مدين`/`دائن` and the debit/credit columns are facts about the balance, so
       * the value is flipped back into debit space before it is named or placed.
       */
      const toDebitSpace = (natureSigned: Decimal) => (debitNature ? natureSigned : natureSigned.negated());

      const showOpeningRow = !query.fullPeriod && Boolean(query.from) && !query.hidePreviousBalance;
      if (showOpeningRow) {
        const side = toDebitSpace(opening);
        const placed = side.isPositive()
          ? { debit: side, credit: new Decimal(0) }
          : { debit: new Decimal(0), credit: side.negated() };
        rows.push({
          rank: 0,
          date: query.from!,
          entryId: null,
          number: null,
          branchName: null,
          entryType: 'رصيد سابق',
          description: 'رصيد مرحل من فترة سابقة',
          debit: placed.debit.toFixed(4),
          credit: placed.credit.toFixed(4),
          // كشف حساب رئيسي reports a branch, and a branch has no single running total.
          runningBalance: query.withDescendants ? null : opening.abs().toFixed(4),
          balanceStatus: query.withDescendants ? null : statusOf(side),
          accountCode: account.code,
          accountName: account.nameAr,
        });
      }

      let rank = 1;
      for (const row of movements) {
        const debit = money(row.debit);
        const credit = money(row.credit);
        periodTotals.debit = periodTotals.debit.plus(debit);
        periodTotals.credit = periodTotals.credit.plus(credit);
        running = running.plus(debitNature ? debit.minus(credit) : credit.minus(debit));
        rows.push({
          rank: rank++,
          date: row.date,
          entryId: row.entryId,
          number: row.number,
          branchName: row.branchName ?? null,
          entryType: entryTypeOf(row),
          description: (query.summary ? row.entryDescription : row.lineDescription ?? row.entryDescription) ?? null,
          debit: debit.toFixed(4),
          credit: credit.toFixed(4),
          runningBalance: query.withDescendants ? null : running.abs().toFixed(4),
          balanceStatus: query.withDescendants ? null : statusOf(toDebitSpace(running)),
          accountCode: row.accountCode,
          accountName: row.accountNameAr,
        });
      }

      const totals: StatementTotals = {
        debit: periodTotals.debit.toFixed(4),
        credit: periodTotals.credit.toFixed(4),
        periodDebit: periodTotals.debit.greaterThan(periodTotals.credit) ? periodTotals.debit.minus(periodTotals.credit).toFixed(4) : '0.0000',
        periodCredit: periodTotals.credit.greaterThan(periodTotals.debit) ? periodTotals.credit.minus(periodTotals.debit).toFixed(4) : '0.0000',
        closing: running.abs().toFixed(4),
        closingStatus: statusOf(toDebitSpace(running)),
      };

      return {
        account: {
          id: account.id,
          code: account.code,
          nameAr: account.nameAr,
          type: account.type,
          normalBalance: account.normalBalance,
          openingBalance: account.openingBalance,
          branchId: account.branchId,
        },
        totals,
        rows,
      };
    });
  }

  /**
   * The ledger as this endpoint has always returned it: one row per posted line, no
   * filters. It is `accountStatement` with the desktop's own defaults removed, so the
   * shape cannot drift between the two.
   */
  async generalLedger(tenantId: string, accountId: string) {
    const statement = await this.accountStatement(tenantId, accountId);
    return statement.rows;
  }

  async postJournal(
    tenantId: string,
    input: {
      branchId?: string;
      fiscalPeriodId?: string;
      date: string;
      description?: string;
      lines: JournalLineInput[];
      sourceType?: string;
      sourceId?: string;
      idempotencyKey?: string;
    } & JournalCardInput,
  ) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const resolved = await this.resolvePostingContext(tx, tenantId, input.date, input.branchId, input.fiscalPeriodId);
      return this.postJournalInTx(tx, tenantId, { ...input, ...resolved });
    });
  }

  /**
   * A voucher screen sends a date; it should not have to know the internal id of the
   * fiscal period or of the default branch. Both are derived here, and the errors are
   * explicit so the operator learns what is actually missing (no fiscal year yet, the
   * month is closed, no branch defined) instead of a generic 409.
   */
  async resolvePostingContext(
    tx: DrizzleTx,
    tenantId: string,
    date: string,
    branchId?: string,
    fiscalPeriodId?: string,
  ): Promise<{ branchId: string; fiscalPeriodId: string }> {
    let resolvedBranchId = branchId;
    if (!resolvedBranchId) {
      const [branch] = await tx
        .select({ id: branches.id })
        .from(branches)
        .where(and(eq(branches.tenantId, tenantId), isNull(branches.deletedAt)))
        .orderBy(desc(branches.isDefault), asc(branches.code))
        .limit(1);
      if (!branch) {
        throw new DomainError('BRANCH_REQUIRED', 'This tenant has no branch yet — create one in الإعدادات ← بطاقة فرع', 422);
      }
      resolvedBranchId = branch.id;
    }

    let resolvedPeriodId = fiscalPeriodId;
    if (!resolvedPeriodId) {
      const [period] = await tx
        .select({ id: fiscalPeriods.id, status: fiscalPeriods.status })
        .from(fiscalPeriods)
        .where(
          and(
            eq(fiscalPeriods.tenantId, tenantId),
            lte(fiscalPeriods.startDate, date),
            gte(fiscalPeriods.endDate, date),
          ),
        )
        .limit(1);
      if (!period) {
        throw new DomainError(
          'FISCAL_PERIOD_NOT_FOUND',
          `No fiscal period covers ${date} — create the fiscal year first (المحاسبة ← الفترات المحاسبية)`,
          422,
        );
      }
      resolvedPeriodId = period.id;
    }

    return { branchId: resolvedBranchId, fiscalPeriodId: resolvedPeriodId };
  }

  /**
   * Resolves the open fiscal period containing `date` (every auto-posting engine
   * calls this when the caller names no period). Throws `FISCAL_PERIOD_CLOSED`
   * when the date falls in no open period — back-dating into a closed period is
   * never silently re-routed, the accountant reopens or picks a period explicitly.
   */
  async openPeriodForDateInTx(tx: DrizzleTx, tenantId: string, date: string): Promise<string> {
    const [period] = await tx
      .select({ id: fiscalPeriods.id })
      .from(fiscalPeriods)
      .where(
        and(
          eq(fiscalPeriods.tenantId, tenantId),
          eq(fiscalPeriods.status, 'open'),
          lte(fiscalPeriods.startDate, date),
          gte(fiscalPeriods.endDate, date),
        ),
      )
      .limit(1);
    if (!period) throw new DomainError('FISCAL_PERIOD_CLOSED', `No open fiscal period contains ${date}`, 409);
    return period.id;
  }

  async postJournalInTx(tx: DrizzleTx, tenantId: string, input: { branchId: string; fiscalPeriodId: string; date: string; description?: string; lines: JournalLineInput[]; sourceType?: string; sourceId?: string; idempotencyKey?: string } & JournalCardInput) {
    if (input.lines.length < 2) throw new DomainError('JOURNAL_LINES_REQUIRED', 'A journal entry needs at least two lines', 422);
    const debit = input.lines.reduce((sum, line) => sum.plus(line.debit ?? '0'), new Decimal(0));
    const credit = input.lines.reduce((sum, line) => sum.plus(line.credit ?? '0'), new Decimal(0));
    if (!debit.isFinite() || !credit.isFinite() || debit.minus(credit).abs().gt('0.00005') || debit.lte(0)) {
      throw new DomainError('JOURNAL_NOT_BALANCED', 'Journal entry must balance', 422);
    }

    const [period] = await tx.select().from(fiscalPeriods).where(and(eq(fiscalPeriods.id, input.fiscalPeriodId), eq(fiscalPeriods.tenantId, tenantId)));
    if (!period || period.status !== 'open') throw new DomainError('FISCAL_PERIOD_CLOSED', 'Fiscal period is closed or unavailable', 409);
    await this.assertModuleUnlocked(tx, tenantId, input.fiscalPeriodId, 'accounting');

    const entryId = newId();
    const allocated = await this.sequences.next({ tenantId, branchId: input.branchId, docType: 'journal_entry', fiscalYearId: period.fiscalYearId }, tx, { prefix: 'JE-', padding: 6 });
    /**
     * 📝 الملاحظة — `FrmNewEntry.xaml.cs` L745: when the clerk leaves the note empty the
     * window writes `سند قيد يومية رقم: {EntryNo} بتاريخ {date}`. An unnamed entry is
     * unfindable a year later, and the number is only known once the sequence has run.
     */
    const defaultNote = `سند قيد يومية رقم: ${allocated.display} بتاريخ ${input.date}`;
    await tx.insert(journalEntries).values({
      id: entryId,
      tenantId,
      branchId: input.branchId,
      fiscalPeriodId: input.fiscalPeriodId,
      date: input.date,
      entryTime: input.time ?? null,
      isVat: input.isVat ?? false,
      number: allocated.display,
      kind: 'manual',
      status: 'posted',
      description: input.description?.trim() || defaultNote,
      sourceType: input.sourceType ?? null,
      sourceId: input.sourceId ?? null,
      idempotencyKey: input.idempotencyKey ?? null,
      postedAt: new Date(),
    });
    /**
     * الشرح — `FrmNewEntry.xaml.cs` L761 names the line after the account it settles when
     * the clerk leaves it empty, so a ledger line always says what it was for.
     */
    const names = new Map(
      (
        await tx
          .select({ id: accounts.id, nameAr: accounts.nameAr })
          .from(accounts)
          .where(and(eq(accounts.tenantId, tenantId), inArray(accounts.id, input.lines.map((line) => line.accountId))))
      ).map((row) => [row.id, row.nameAr]),
    );
    await tx.insert(journalEntryLines).values(input.lines.map((line, index) => ({
      entryId,
      lineNo: index + 1,
      tenantId,
      accountId: line.accountId,
      debit: line.debit ?? '0',
      credit: line.credit ?? '0',
      costCenterId: line.costCenterId ?? null,
      partyId: line.partyId ?? null,
      branchId: line.branchId ?? input.branchId,
      salesmanId: line.salesmanId ?? null,
      description:
        line.description?.trim() ||
        `سند قيد يومية رقم: ${allocated.display} - سداد دفعة من حساب: ${names.get(line.accountId) ?? ''}`.trim(),
    })));
    const [entry] = await tx.select().from(journalEntries).where(eq(journalEntries.id, entryId));
    return entry;
  }

  async lockModule(tenantId: string, periodId: string, module: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      await tx.insert(periodModuleLocks).values({ tenantId, periodId, module, locked: true, lockedAt: new Date() }).onConflictDoUpdate({ target: [periodModuleLocks.periodId, periodModuleLocks.module], set: { locked: true, lockedAt: new Date() } });
      return { periodId, module, locked: true };
    });
  }

  async unlockModule(tenantId: string, periodId: string, module: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      await tx.insert(periodModuleLocks).values({ tenantId, periodId, module, locked: false, lockedAt: null }).onConflictDoUpdate({ target: [periodModuleLocks.periodId, periodModuleLocks.module], set: { locked: false, lockedAt: null, lockedBy: null } });
      return { periodId, module, locked: false };
    });
  }

  // -------------------------------------------------------------- cost centres

  async listCostCenters(tenantId: string, query: CostCenterQuery = {}): Promise<CostCenterRow[]> {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const rows = await tx
        .select()
        .from(costCenters)
        .where(
          and(
            eq(costCenters.tenantId, tenantId),
            isNull(costCenters.deletedAt),
            query.branchId ? eq(costCenters.branchId, query.branchId) : undefined,
            query.q
              ? or(ilike(costCenters.code, `%${query.q}%`), ilike(costCenters.nameAr, `%${query.q}%`))
              : undefined,
          ),
        )
        .orderBy(asc(costCenters.code));
      if (!query.withBalances) return rows;

      const movements = await tx
        .select({
          costCenterId: journalEntryLines.costCenterId,
          debit: sql<string>`COALESCE(SUM(${journalEntryLines.debit}), 0)::text`,
          credit: sql<string>`COALESCE(SUM(${journalEntryLines.credit}), 0)::text`,
        })
        .from(journalEntryLines)
        .innerJoin(journalEntries, eq(journalEntries.id, journalEntryLines.entryId))
        .where(
          and(
            eq(journalEntryLines.tenantId, tenantId),
            eq(journalEntries.status, 'posted'),
            sql`${journalEntryLines.costCenterId} IS NOT NULL`,
          ),
        )
        .groupBy(journalEntryLines.costCenterId);

      const own = new Map<string, { debit: Decimal; credit: Decimal }>();
      for (const row of movements) {
        if (!row.costCenterId) continue;
        own.set(row.costCenterId, { debit: money(row.debit), credit: money(row.credit) });
      }

      // A cost centre's children are reached through `parent_id`; every ancestor of a row
      // is found by walking up, and the balance follows the same path downwards.
      const parentOf = new Map(rows.map((row) => [row.id, row.parentId ?? null]));
      const rolled = new Map<string, { debit: Decimal; credit: Decimal; children: number }>();
      for (const row of rows) {
        const self = own.get(row.id) ?? { debit: new Decimal(0), credit: new Decimal(0) };
        rolled.set(row.id, { debit: self.debit, credit: self.credit, children: 0 });
      }
      for (const row of rows) {
        const self = rolled.get(row.id)!;
        let parentId = parentOf.get(row.id) ?? null;
        const seen = new Set<string>([row.id]);
        while (parentId && !seen.has(parentId)) {
          seen.add(parentId);
          const target = rolled.get(parentId);
          if (!target) break;
          target.debit = target.debit.plus(self.debit);
          target.credit = target.credit.plus(self.credit);
          target.children += 1;
          parentId = parentOf.get(parentId) ?? null;
        }
      }

      const byId = new Map(rows.map((row) => [row.id, row]));
      const levelOf = (row: (typeof rows)[number]): number => {
        let level = 0;
        let parentId = row.parentId ?? null;
        const seen = new Set<string>([row.id]);
        while (parentId && !seen.has(parentId)) {
          seen.add(parentId);
          const parent = byId.get(parentId);
          if (!parent) break;
          level += 1;
          parentId = parent.parentId ?? null;
        }
        return level;
      };

      return rows.map((row) => {
        const self = own.get(row.id) ?? { debit: new Decimal(0), credit: new Decimal(0) };
        const subtree = rolled.get(row.id)!;
        const totals: CostCenterBalance = {
          ownDebit: self.debit.toFixed(4),
          ownCredit: self.credit.toFixed(4),
          ownBalance: self.debit.minus(self.credit).toFixed(4),
          debit: subtree.debit.toFixed(4),
          credit: subtree.credit.toFixed(4),
          balance: subtree.debit.minus(subtree.credit).toFixed(4),
          children: subtree.children,
        };
        return {
          ...row,
          parentName: row.parentId ? (byId.get(row.parentId)?.nameAr ?? null) : null,
          level: levelOf(row),
          /** 🏷️ النوع — `🟢 رئيسي` / `🔵 فرعي`, exactly as the window's radio buttons read. */
          kind: row.parentId ? 'sub' : 'main',
          balance: totals,
        };
      });
    });
  }

  /**
   * 📊 كشف مركز الكلفة — `Form_WPF/frmCostCenterBalance.xaml` («تقرير مركز كلفة»).
   *
   * The report is the account statement pointed at a cost centre instead of an account:
   * `م · 💸 مدين · 💰 دائن · ⚖️ الرصيد · 📌 الحالة · 🔢 الرقم العام · 📄 رقم السند`, with
   * الفرع · النوع · التاريخ · البيان as well (`BuildDataTable`), the same `رصيد سابق`
   * row when a period is set, and the same `تجميعي`/`تفصيلي` choice (`deptExpr` vs
   * `GROUP BY`, L245–L255).
   *
   * One difference is deliberate: the window guesses the centre's nature from the first
   * character of its code (`costCenterCode.Substring(0, 1)`, L236), the same trick it
   * uses for accounts. A cost centre accumulates costs, so here الرصيد grows on the
   * debit side and `📌 الحالة` names the side the money is actually on.
   */
  async costCenterStatement(tenantId: string, centerId: string, query: CostCenterStatementQuery = {}) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const rows = await this.listCostCenters(tenantId, { withBalances: true });
      const center = rows.find((row) => row.id === centerId);
      if (!center) throw new DomainError('NOT_FOUND', 'Cost centre not found', 404);

      // The centre and everything beneath it — the window reports the centre itself,
      // but a balance the tree shows rolled up must agree with what this report says.
      const branch = new Set<string>([centerId]);
      let grew = true;
      while (grew) {
        grew = false;
        for (const row of rows) {
          if (row.parentId && branch.has(row.parentId) && !branch.has(row.id)) {
            branch.add(row.id);
            grew = true;
          }
        }
      }
      const scope = [...branch];

      const period = query.fullPeriod
        ? []
        : [query.from ? gte(journalEntries.date, query.from) : undefined, query.to ? lte(journalEntries.date, query.to) : undefined].filter(
            (clause) => clause !== undefined,
          );

      const before = query.from
        ? await tx
            .select({
              debit: sql<string>`COALESCE(SUM(${journalEntryLines.debit}), 0)::text`,
              credit: sql<string>`COALESCE(SUM(${journalEntryLines.credit}), 0)::text`,
            })
            .from(journalEntryLines)
            .innerJoin(journalEntries, eq(journalEntries.id, journalEntryLines.entryId))
            .where(
              and(
                eq(journalEntryLines.tenantId, tenantId),
                eq(journalEntries.status, 'posted'),
                inArray(journalEntryLines.costCenterId, scope),
                query.branchId ? eq(journalEntries.branchId, query.branchId) : undefined,
                query.accountId ? eq(journalEntryLines.accountId, query.accountId) : undefined,
                lt(journalEntries.date, query.from),
              ),
            )
            .then((result) => result[0])
        : undefined;
      let running = money(before?.debit).minus(money(before?.credit));

      const select = {
        entryId: journalEntries.id,
        date: journalEntries.date,
        number: journalEntries.number,
        entryDescription: journalEntries.description,
        lineDescription: sql<string | null>`MIN(${journalEntryLines.description})`,
        debit: sql<string>`COALESCE(SUM(${journalEntryLines.debit}), 0)::text`,
        credit: sql<string>`COALESCE(SUM(${journalEntryLines.credit}), 0)::text`,
        accountCode: accounts.code,
        accountNameAr: accounts.nameAr,
        branchName: branches.nameAr,
        kind: journalEntries.kind,
        sourceType: journalEntries.sourceType,
        voucherKind: vouchers.kind,
      };
      const movements = await tx
        .select(select)
        .from(journalEntryLines)
        .innerJoin(journalEntries, eq(journalEntries.id, journalEntryLines.entryId))
        .innerJoin(accounts, eq(accounts.id, journalEntryLines.accountId))
        .leftJoin(branches, eq(branches.id, journalEntries.branchId))
        .leftJoin(vouchers, eq(vouchers.journalEntryId, journalEntries.id))
        .where(
          and(
            eq(journalEntryLines.tenantId, tenantId),
            eq(journalEntries.status, 'posted'),
            inArray(journalEntryLines.costCenterId, scope),
            query.branchId ? eq(journalEntries.branchId, query.branchId) : undefined,
            query.accountId ? eq(journalEntryLines.accountId, query.accountId) : undefined,
            ...period,
          ),
        )
        .groupBy(
          ...(query.summary
            ? [
                journalEntries.id,
                journalEntries.date,
                journalEntries.number,
                journalEntries.description,
                accounts.code,
                accounts.nameAr,
                branches.nameAr,
                journalEntries.kind,
                journalEntries.sourceType,
                vouchers.kind,
              ]
            : [
                journalEntries.id,
                journalEntryLines.lineNo,
                journalEntries.date,
                journalEntries.number,
                journalEntries.description,
                journalEntryLines.description,
                journalEntryLines.debit,
                journalEntryLines.credit,
                accounts.code,
                accounts.nameAr,
                branches.nameAr,
                journalEntries.kind,
                journalEntries.sourceType,
                vouchers.kind,
              ]),
        )
        .orderBy(
          asc(journalEntries.date),
          asc(journalEntries.number),
          ...(query.summary ? [] : [asc(journalEntryLines.lineNo)]),
        );

      const statement: StatementRow[] = [];
      const periodTotals = { debit: new Decimal(0), credit: new Decimal(0) };

      if (!query.fullPeriod && query.from && !query.hidePreviousBalance) {
        const side = running.isPositive() ? { debit: running, credit: new Decimal(0) } : { debit: new Decimal(0), credit: running.negated() };
        statement.push({
          rank: 0,
          date: query.from,
          entryId: null,
          number: null,
          branchName: null,
          entryType: 'رصيد سابق',
          description: 'رصيد مرحل من فترة سابقة',
          debit: side.debit.toFixed(4),
          credit: side.credit.toFixed(4),
          runningBalance: running.abs().toFixed(4),
          balanceStatus: statusOf(running),
          accountCode: null,
          accountName: null,
        });
      }

      let rank = 1;
      for (const row of movements) {
        const debit = money(row.debit);
        const credit = money(row.credit);
        periodTotals.debit = periodTotals.debit.plus(debit);
        periodTotals.credit = periodTotals.credit.plus(credit);
        running = running.plus(debit.minus(credit));
        statement.push({
          rank: rank++,
          date: row.date,
          entryId: row.entryId,
          number: row.number,
          branchName: row.branchName ?? null,
          entryType: entryTypeOf(row),
          description: (query.summary ? row.entryDescription : row.lineDescription ?? row.entryDescription) ?? null,
          debit: debit.toFixed(4),
          credit: credit.toFixed(4),
          runningBalance: running.abs().toFixed(4),
          balanceStatus: statusOf(running),
          accountCode: row.accountCode,
          accountName: row.accountNameAr,
        });
      }

      const totals: StatementTotals = {
        debit: periodTotals.debit.toFixed(4),
        credit: periodTotals.credit.toFixed(4),
        periodDebit: periodTotals.debit.greaterThan(periodTotals.credit)
          ? periodTotals.debit.minus(periodTotals.credit).toFixed(4)
          : '0.0000',
        periodCredit: periodTotals.credit.greaterThan(periodTotals.debit)
          ? periodTotals.credit.minus(periodTotals.debit).toFixed(4)
          : '0.0000',
        closing: running.abs().toFixed(4),
        closingStatus: statusOf(running),
      };

      return {
        costCenter: {
          id: center.id,
          code: center.code,
          nameAr: center.nameAr,
          parentId: center.parentId ?? null,
          branchId: center.branchId ?? null,
          balance: center.balance,
        },
        totals,
        rows: statement,
      };
    });
  }

  async createCostCenter(tenantId: string, input: CostCenterInput) {
    const id = newId();
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      await tx.insert(costCenters).values({
        id,
        tenantId,
        code: input.code,
        nameAr: input.nameAr,
        nameEn: input.nameEn ?? null,
        parentId: input.parentId ?? null,
        branchId: input.branchId ?? null,
      });
      const [row] = await tx.select().from(costCenters).where(eq(costCenters.id, id));
      return row;
    });
  }

  async updateCostCenter(tenantId: string, id: string, patch: CostCenterPatch) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [current] = await tx.select().from(costCenters).where(and(eq(costCenters.id, id), eq(costCenters.tenantId, tenantId), isNull(costCenters.deletedAt)));
      if (!current) throw new DomainError('NOT_FOUND', 'Cost centre not found', 404);
      if (patch.parentId === id) throw new DomainError('COST_CENTER_CYCLE', 'A cost centre cannot be its own parent', 422);
      const [row] = await tx
        .update(costCenters)
        .set({
          code: patch.code ?? current.code,
          nameAr: patch.nameAr ?? current.nameAr,
          nameEn: patch.nameEn === undefined ? current.nameEn : patch.nameEn,
          parentId: patch.parentId === undefined ? current.parentId : patch.parentId,
          branchId: patch.branchId === undefined ? current.branchId : patch.branchId,
          updatedAt: new Date(),
        })
        .where(and(eq(costCenters.id, id), eq(costCenters.tenantId, tenantId)))
        .returning();
      return row;
    });
  }

  /** Cost centres carry analysis, so one that already appears on journal lines stays. */
  async deleteCostCenter(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const used = await tx.execute(sql`
        SELECT EXISTS (SELECT 1 FROM journal_entry_lines WHERE tenant_id = ${tenantId} AND cost_center_id = ${id})
            OR EXISTS (SELECT 1 FROM cost_centers WHERE tenant_id = ${tenantId} AND parent_id = ${id} AND deleted_at IS NULL) AS used
      `);
      if ((used.rows[0] as { used: boolean }).used) {
        throw new DomainError('COST_CENTER_IN_USE', 'This cost centre is used on journal entries or has sub-centres', 409);
      }
      const result = await tx.update(costCenters).set({ deletedAt: new Date(), updatedAt: new Date() }).where(and(eq(costCenters.id, id), eq(costCenters.tenantId, tenantId), isNull(costCenters.deletedAt)));
      if (!result.rowCount) throw new DomainError('NOT_FOUND', 'Cost centre not found', 404);
      return { id, deleted: true };
    });
  }

  // -------------------------------------------------------------- fiscal calendar

  async listFiscalYears(tenantId: string) {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx.select().from(fiscalYears).where(eq(fiscalYears.tenantId, tenantId)).orderBy(desc(fiscalYears.startDate)),
    );
  }

  /**
   * Creates the fiscal year and, unless told otherwise, the twelve monthly periods
   * inside it. Without at least one open period nothing in the system can be posted,
   * so this is the first screen a new tenant needs.
   */
  async createFiscalYear(tenantId: string, input: FiscalYearInput) {
    const start = new Date(`${input.startDate}T00:00:00Z`);
    const end = new Date(`${input.endDate}T00:00:00Z`);
    if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime()) || end <= start) {
      throw new DomainError('VALIDATION_FAILED', 'Fiscal year end date must be after the start date', 422);
    }

    const yearId = newId();
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const overlapping = await tx
        .select({ id: fiscalYears.id })
        .from(fiscalYears)
        .where(
          and(
            eq(fiscalYears.tenantId, tenantId),
            lte(fiscalYears.startDate, input.endDate),
            gte(fiscalYears.endDate, input.startDate),
          ),
        )
        .limit(1);
      if (overlapping.length > 0) {
        throw new DomainError('FISCAL_YEAR_OVERLAP', 'Another fiscal year already covers this range', 409);
      }

      await tx.insert(fiscalYears).values({
        id: yearId,
        tenantId,
        name: input.name,
        startDate: input.startDate,
        endDate: input.endDate,
        status: 'open',
      });

      if (input.generateMonthlyPeriods !== false) {
        const periods: Array<{ id: string; tenantId: string; fiscalYearId: string; name: string; startDate: string; endDate: string; status: string }> = [];
        const cursor = new Date(start);
        while (cursor <= end) {
          const periodStart = new Date(cursor);
          const periodEnd = new Date(Date.UTC(cursor.getUTCFullYear(), cursor.getUTCMonth() + 1, 0));
          const clampedEnd = periodEnd > end ? end : periodEnd;
          periods.push({
            id: newId(),
            tenantId,
            fiscalYearId: yearId,
            name: `${periodStart.getUTCFullYear()}-${String(periodStart.getUTCMonth() + 1).padStart(2, '0')}`,
            startDate: periodStart.toISOString().slice(0, 10),
            endDate: clampedEnd.toISOString().slice(0, 10),
            status: 'open',
          });
          cursor.setUTCMonth(cursor.getUTCMonth() + 1, 1);
        }
        if (periods.length > 0) await tx.insert(fiscalPeriods).values(periods);
      }

      const [row] = await tx.select().from(fiscalYears).where(eq(fiscalYears.id, yearId));
      return row;
    });
  }

  // -------------------------------------------------------------- journal reads

  /** `القيود اليومية` — the posted-journal register with its per-entry totals. */
  async listJournalEntries(tenantId: string, query: JournalQuery = {}) {
    const limit = Math.min(Math.max(query.limit ?? 200, 1), 1_000);
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const filters = [eq(journalEntries.tenantId, tenantId)];
      if (query.from) filters.push(gte(journalEntries.date, query.from));
      if (query.to) filters.push(lte(journalEntries.date, query.to));
      if (query.branchId) filters.push(eq(journalEntries.branchId, query.branchId));
      if (query.fiscalPeriodId) filters.push(eq(journalEntries.fiscalPeriodId, query.fiscalPeriodId));
      if (query.status) filters.push(eq(journalEntries.status, query.status));

      return tx
        .select({
          id: journalEntries.id,
          number: journalEntries.number,
          date: journalEntries.date,
          kind: journalEntries.kind,
          status: journalEntries.status,
          description: journalEntries.description,
          branchId: journalEntries.branchId,
          fiscalPeriodId: journalEntries.fiscalPeriodId,
          sourceType: journalEntries.sourceType,
          reversalOf: journalEntries.reversalOf,
          postedAt: journalEntries.postedAt,
          totalDebit: sql<string>`COALESCE((SELECT SUM(l.debit) FROM journal_entry_lines l WHERE l.entry_id = ${journalEntries.id}), 0)::text`,
          totalCredit: sql<string>`COALESCE((SELECT SUM(l.credit) FROM journal_entry_lines l WHERE l.entry_id = ${journalEntries.id}), 0)::text`,
          lineCount: sql<number>`(SELECT COUNT(*) FROM journal_entry_lines l WHERE l.entry_id = ${journalEntries.id})::int`,
        })
        .from(journalEntries)
        .where(and(...filters))
        .orderBy(desc(journalEntries.date), desc(journalEntries.number))
        .limit(limit);
    });
  }

  async readJournalEntry(tenantId: string, entryId: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [entry] = await tx
        .select()
        .from(journalEntries)
        .where(and(eq(journalEntries.id, entryId), eq(journalEntries.tenantId, tenantId)));
      if (!entry) throw new DomainError('NOT_FOUND', 'Journal entry not found', 404);

      const lines = await tx
        .select({
          lineNo: journalEntryLines.lineNo,
          accountId: journalEntryLines.accountId,
          accountCode: accounts.code,
          accountNameAr: accounts.nameAr,
          debit: journalEntryLines.debit,
          credit: journalEntryLines.credit,
          costCenterId: journalEntryLines.costCenterId,
          partyId: journalEntryLines.partyId,
          salesmanId: journalEntryLines.salesmanId,
          description: journalEntryLines.description,
        })
        .from(journalEntryLines)
        .innerJoin(accounts, eq(accounts.id, journalEntryLines.accountId))
        .where(eq(journalEntryLines.entryId, entryId))
        .orderBy(asc(journalEntryLines.lineNo));

      return { ...entry, lines };
    });
  }

  private async assertModuleUnlocked(tx: DrizzleTx, tenantId: string, periodId: string, module: string): Promise<void> {
    const [lock] = await tx.select().from(periodModuleLocks).where(and(eq(periodModuleLocks.tenantId, tenantId), eq(periodModuleLocks.periodId, periodId), eq(periodModuleLocks.module, module)));
    if (lock?.locked) throw new DomainError('PERIOD_MODULE_LOCKED', `Module ${module} is locked for this fiscal period`, 409);
  }
}
