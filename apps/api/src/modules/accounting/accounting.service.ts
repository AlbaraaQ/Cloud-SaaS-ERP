import { Inject, Injectable } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { and, asc, desc, eq, gte, ilike, isNull, lte, or, sql } from 'drizzle-orm';
import { DomainError } from '@erp/contracts';
import {
  accounts,
  branches,
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

export type AccountPatch = Partial<AccountInput> & { allowManual?: boolean };
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
      await tx.insert(journalEntryLines).values(lines.map((line) => ({ entryId: reversalId, lineNo: line.lineNo, tenantId, accountId: line.accountId, debit: line.credit, credit: line.debit, partyId: line.partyId, description: line.description })));
      await tx.update(journalEntries).set({ status: 'void', updatedAt: new Date() }).where(eq(journalEntries.id, entryId));
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

  async generalLedger(tenantId: string, accountId: string) {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx.select({ entryId: journalEntries.id, date: journalEntries.date, number: journalEntries.number, description: journalEntries.description, debit: journalEntryLines.debit, credit: journalEntryLines.credit })
        .from(journalEntryLines)
        .innerJoin(journalEntries, eq(journalEntries.id, journalEntryLines.entryId))
        .where(and(eq(journalEntryLines.tenantId, tenantId), eq(journalEntryLines.accountId, accountId), eq(journalEntries.status, 'posted'))),
    );
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
    },
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

  async postJournalInTx(tx: DrizzleTx, tenantId: string, input: { branchId: string; fiscalPeriodId: string; date: string; description?: string; lines: JournalLineInput[]; sourceType?: string; sourceId?: string; idempotencyKey?: string }) {
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
    await tx.insert(journalEntries).values({
      id: entryId,
      tenantId,
      branchId: input.branchId,
      fiscalPeriodId: input.fiscalPeriodId,
      date: input.date,
      number: allocated.display,
      kind: 'manual',
      status: 'posted',
      description: input.description ?? null,
      sourceType: input.sourceType ?? null,
      sourceId: input.sourceId ?? null,
      idempotencyKey: input.idempotencyKey ?? null,
      postedAt: new Date(),
    });
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
      description: line.description ?? null,
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

  async listCostCenters(tenantId: string) {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(costCenters)
        .where(and(eq(costCenters.tenantId, tenantId), isNull(costCenters.deletedAt)))
        .orderBy(asc(costCenters.code)),
    );
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
