import { Inject, Injectable } from '@nestjs/common';
import { and, eq, isNull } from 'drizzle-orm';
import {
  accounts,
  fiscalPeriods,
  journalEntries,
  journalEntryLines,
  newId,
  withTenantTx,
  type DatabaseHandle,
} from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';

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
  constructor(@Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle) {}

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
      const lines = await tx.select().from(journalEntryLines).where(eq(journalEntryLines.entryId, entryId));
      const reversalId = newId();
      await tx.insert(journalEntries).values({ id: reversalId, tenantId, branchId: input.branchId, fiscalPeriodId: input.fiscalPeriodId, date: input.date, kind: 'reversal', status: 'posted', description: input.reason, reversalOf: entryId, postedAt: new Date() });
      await tx.insert(journalEntryLines).values(lines.map((line) => ({ entryId: reversalId, lineNo: line.lineNo, tenantId, accountId: line.accountId, debit: line.credit, credit: line.debit, partyId: line.partyId, description: line.description })));
      await tx.update(journalEntries).set({ status: 'void', updatedAt: new Date() }).where(eq(journalEntries.id, entryId));
      return { id: reversalId, reversalOf: entryId };
    });
  }

  async postJournal(tenantId: string, input: { branchId: string; fiscalPeriodId: string; date: string; description?: string; lines: JournalLineInput[] }) {
    if (input.lines.length < 2) throw new Error('A journal entry needs at least two lines');
    const debit = input.lines.reduce((sum, line) => sum + Number(line.debit ?? 0), 0);
    const credit = input.lines.reduce((sum, line) => sum + Number(line.credit ?? 0), 0);
    if (!Number.isFinite(debit) || Math.abs(debit - credit) > 0.00005 || debit <= 0) {
      throw new Error('Journal entry must balance');
    }

    const entryId = newId();
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      const [period] = await tx.select().from(fiscalPeriods).where(and(eq(fiscalPeriods.id, input.fiscalPeriodId), eq(fiscalPeriods.tenantId, tenantId)));
      if (!period || period.status !== 'open') throw new Error('Fiscal period is closed or unavailable');

      await tx.insert(journalEntries).values({
        id: entryId,
        tenantId,
        branchId: input.branchId,
        fiscalPeriodId: input.fiscalPeriodId,
        date: input.date,
        kind: 'manual',
        status: 'posted',
        description: input.description ?? null,
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
    });
    return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(journalEntries).where(eq(journalEntries.id, entryId)));
  }
}
