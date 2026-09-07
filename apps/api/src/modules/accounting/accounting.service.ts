import { Inject, Injectable } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { and, eq, isNull } from 'drizzle-orm';
import { DomainError } from '@erp/contracts';
import {
  accounts,
  fiscalPeriods,
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

export type AccountInput = {
  code: string;
  nameAr: string;
  nameEn?: string;
  type: 'asset' | 'liability' | 'equity' | 'revenue' | 'expense';
  subtype?: string;
  normalBalance: 'debit' | 'credit';
  parentId?: string;
  isPostable?: boolean;
};

export type JournalLineInput = {
  accountId: string;
  debit?: string;
  credit?: string;
  description?: string;
  partyId?: string;
};

@Injectable()
export class AccountingService {
  constructor(
    @Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle,
    private readonly sequences: SequencesService,
  ) {}

  async listAccounts(tenantId: string) {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx.select().from(accounts).where(and(eq(accounts.tenantId, tenantId), isNull(accounts.deletedAt))),
    );
  }

  async createAccount(tenantId: string, input: AccountInput) {
    const id = newId();
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      await tx.insert(accounts).values({
        id,
        tenantId,
        code: input.code,
        nameAr: input.nameAr,
        nameEn: input.nameEn ?? null,
        type: input.type,
        subtype: input.subtype ?? null,
        normalBalance: input.normalBalance,
        parentId: input.parentId ?? null,
        path: input.parentId ?? id,
        isPostable: input.isPostable ?? true,
      });
    });
    return this.readAccount(tenantId, id);
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

  async postJournal(tenantId: string, input: { branchId: string; fiscalPeriodId: string; date: string; description?: string; lines: JournalLineInput[]; sourceType?: string; sourceId?: string; idempotencyKey?: string }) {
    return withTenantTx(this.database.db, tenantId, (tx) => this.postJournalInTx(tx, tenantId, input));
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
      partyId: line.partyId ?? null,
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

  private async assertModuleUnlocked(tx: DrizzleTx, tenantId: string, periodId: string, module: string): Promise<void> {
    const [lock] = await tx.select().from(periodModuleLocks).where(and(eq(periodModuleLocks.tenantId, tenantId), eq(periodModuleLocks.periodId, periodId), eq(periodModuleLocks.module, module)));
    if (lock?.locked) throw new DomainError('PERIOD_MODULE_LOCKED', `Module ${module} is locked for this fiscal period`, 409);
  }
}
