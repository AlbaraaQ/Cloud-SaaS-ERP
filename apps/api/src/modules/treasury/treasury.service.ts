/* eslint-disable no-restricted-syntax */
import { Inject, Injectable } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { and, desc, eq, isNull, sql } from 'drizzle-orm';
import { DomainError, newId } from '@erp/contracts';
import {
  cashLocationBalances,
  cashTransfers,
  expenseTypes,
  paymentAllocations,
  shiftCloseLines,
  shiftCloses,
  cashCountLines,
  vouchers,
  withTenantTx,
  type DatabaseHandle,
  type DrizzleTx,
} from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';
import { AccountingService, type JournalLineInput } from '../accounting/accounting.service.js';
import { SequencesService } from '../platform-services/index.js';

export type VoucherInput = { branchId: string; kind: 'receipt' | 'payment'; subtype: 'customer' | 'supplier' | 'expense' | 'account' | 'salary' | 'vat' | 'other'; date: string; partyId?: string; counterAccountId?: string; cashLocationId: string; method: 'cash' | 'cheque' | 'bank_transfer' | 'card'; amount: string; vatAmount?: string; netAmount?: string; currency?: string; chequeNo?: string; chequeDate?: string; bankName?: string; costCenterId?: string; referenceNo?: string; referenceDate?: string; recipient?: string; fiscalPeriodId?: string; idempotencyKey?: string };
export type VoucherPostInput = { fiscalPeriodId?: string; journalLines?: JournalLineInput[]; allocations?: Array<{ partyId: string; invoiceKind: string; invoiceId: string; amount: string }> };
export type TransferInput = { branchId: string; fromCashLocationId: string; toCashLocationId: string; amount: string; currency?: string };
export type ShiftCount = { currencyCode?: string; denomination: string; count: number };

const money = (value: string) => new Decimal(value);

@Injectable()
export class TreasuryService {
  constructor(@Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle, private readonly accounting: AccountingService, private readonly sequences: SequencesService) {}

  vouchers(tenantId: string) { return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(vouchers).where(eq(vouchers.tenantId, tenantId)).orderBy(desc(vouchers.createdAt)).limit(100)); }
  async getVoucher(tenantId: string, id: string) { const [row] = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(vouchers).where(and(eq(vouchers.tenantId, tenantId), eq(vouchers.id, id)))); if (!row) throw new DomainError('VOUCHER_NOT_FOUND', 'Voucher was not found', 404); return row; }

  async createVoucher(tenantId: string, input: VoucherInput) {
    const amount = money(input.amount);
    if (!amount.isFinite() || amount.lte(0)) throw new DomainError('VOUCHER_AMOUNT_INVALID', 'Voucher amount must be positive', 422);
    if (input.method === 'cheque' && !input.chequeNo) throw new DomainError('CHEQUE_NO_REQUIRED', 'Cheque vouchers require a cheque number', 422);
    const id = newId();
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(vouchers).values({ id, tenantId, branchId: input.branchId, kind: input.kind, subtype: input.subtype, date: input.date, partyId: input.partyId, counterAccountId: input.counterAccountId, cashLocationId: input.cashLocationId, method: input.method, amount: input.amount, vatAmount: input.vatAmount ?? '0', netAmount: input.netAmount ?? input.amount, currency: input.currency ?? 'SAR', chequeNo: input.chequeNo, chequeDate: input.chequeDate, bankName: input.bankName, chequeState: input.method === 'cheque' ? 'pending' : null, costCenterId: input.costCenterId, referenceNo: input.referenceNo, referenceDate: input.referenceDate, recipient: input.recipient, fiscalPeriodId: input.fiscalPeriodId, idempotencyKey: input.idempotencyKey }).returning());
    return row;
  }

  async updateDraftVoucher(tenantId: string, id: string, input: Partial<VoucherInput>) {
    const row = await this.getVoucher(tenantId, id);
    if (row.status !== 'draft') throw new DomainError('VOUCHER_IMMUTABLE', 'Only draft vouchers can be changed', 409);
    await withTenantTx(this.database.db, tenantId, (tx) => tx.update(vouchers).set({ partyId: input.partyId, counterAccountId: input.counterAccountId, amount: input.amount, vatAmount: input.vatAmount, netAmount: input.netAmount, updatedAt: new Date() }).where(and(eq(vouchers.tenantId, tenantId), eq(vouchers.id, id))));
    return this.getVoucher(tenantId, id);
  }

  async postVoucher(tenantId: string, id: string, input: VoucherPostInput = {}) {
    const voucher = await this.getVoucher(tenantId, id);
    if (voucher.status === 'posted') return voucher;
    if (voucher.status !== 'draft') throw new DomainError('VOUCHER_INVALID_STATUS', 'Only draft vouchers can be posted', 409);
    if (input.journalLines?.length && !input.fiscalPeriodId && !voucher.fiscalPeriodId) throw new DomainError('VOUCHER_FISCAL_PERIOD_REQUIRED', 'A fiscal period is required for voucher journals', 422);
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      const [locked] = await tx.select().from(vouchers).where(and(eq(vouchers.tenantId, tenantId), eq(vouchers.id, id), eq(vouchers.status, 'draft')));
      if (!locked) throw new DomainError('VOUCHER_INVALID_STATUS', 'Only draft vouchers can be posted', 409);
      const allocated = await this.sequences.next({ tenantId, branchId: locked.branchId, docType: `${locked.kind}_voucher` }, tx, { prefix: locked.kind === 'receipt' ? 'RV-' : 'PV-', padding: 6 });
      let journalEntryId: string | null = null;
      if (input.journalLines?.length) {
        const journal = await this.accounting.postJournalInTx(tx, tenantId, { branchId: locked.branchId, fiscalPeriodId: input.fiscalPeriodId ?? locked.fiscalPeriodId!, date: locked.date, description: `${locked.kind} voucher ${allocated.display}`, lines: input.journalLines, sourceType: 'voucher', sourceId: id });
        journalEntryId = journal?.id ?? null;
      }
      if (locked.method !== 'cheque') await this.bumpBalance(tx, tenantId, locked.cashLocationId, locked.currency, locked.kind === 'receipt' ? locked.amount : `-${locked.amount}`);
      for (const allocation of input.allocations ?? []) {
        if (money(allocation.amount).lte(0) || money(allocation.amount).gt(money(locked.amount))) throw new DomainError('ALLOCATION_AMOUNT_INVALID', 'Allocation amount is outside voucher bounds', 422);
        await tx.insert(paymentAllocations).values({ id: newId(), tenantId, partyId: allocation.partyId, voucherId: id, invoiceKind: allocation.invoiceKind, invoiceId: allocation.invoiceId, amount: allocation.amount });
      }
      await tx.update(vouchers).set({ status: 'posted', number: allocated.display, journalEntryId, postedAt: new Date(), updatedAt: new Date() }).where(and(eq(vouchers.tenantId, tenantId), eq(vouchers.id, id)));
    });
    return this.getVoucher(tenantId, id);
  }

  async voidVoucher(tenantId: string, id: string, reason: string) {
    if (!reason.trim()) throw new DomainError('VOUCHER_VOID_REASON_REQUIRED', 'Void reason is required', 422);
    const row = await this.getVoucher(tenantId, id);
    if (row.status !== 'posted') throw new DomainError('VOUCHER_INVALID_STATUS', 'Only posted vouchers can be voided', 409);
    await withTenantTx(this.database.db, tenantId, async (tx) => { if (row.method !== 'cheque') await this.bumpBalance(tx, tenantId, row.cashLocationId, row.currency, row.kind === 'receipt' ? `-${row.amount}` : row.amount); await tx.update(vouchers).set({ status: 'voided', voidedAt: new Date(), updatedAt: new Date() }).where(and(eq(vouchers.tenantId, tenantId), eq(vouchers.id, id))); });
    return this.getVoucher(tenantId, id);
  }

  async transitionCheque(tenantId: string, id: string, action: 'clear' | 'bounce' | 'collect') {
    const target = action === 'clear' ? 'cleared' : action === 'bounce' ? 'bounced' : 'collected';
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [row] = await tx.select().from(vouchers).where(and(eq(vouchers.tenantId, tenantId), eq(vouchers.id, id)));
      if (!row || row.method !== 'cheque' || row.status !== 'posted') throw new DomainError('CHEQUE_INVALID_STATE', 'Cheque voucher is not posted', 422);
      if (row.chequeState !== 'pending') throw new DomainError('CHEQUE_INVALID_STATE', 'Cheque transition is terminal', 422);
      if (target === 'cleared' || target === 'collected') await this.bumpBalance(tx, tenantId, row.cashLocationId, row.currency, row.kind === 'receipt' ? row.amount : `-${row.amount}`);
      await tx.update(vouchers).set({ chequeState: target, updatedAt: new Date() }).where(and(eq(vouchers.tenantId, tenantId), eq(vouchers.id, id), eq(vouchers.chequeState, 'pending')));
      return { id, chequeState: target };
    });
  }

  async createTransfer(tenantId: string, input: TransferInput) {
    if (input.fromCashLocationId === input.toCashLocationId) throw new DomainError('CASH_TRANSFER_INVALID', 'Transfer requires distinct locations', 422);
    const amount = money(input.amount); if (!amount.isFinite() || amount.lte(0)) throw new DomainError('CASH_TRANSFER_AMOUNT_INVALID', 'Transfer amount must be positive', 422);
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(cashTransfers).values({ id: newId(), tenantId, branchId: input.branchId, fromCashLocationId: input.fromCashLocationId, toCashLocationId: input.toCashLocationId, amount: input.amount, currency: input.currency ?? 'SAR' }).returning());
    return row;
  }
  transfers(tenantId: string) { return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(cashTransfers).where(eq(cashTransfers.tenantId, tenantId)).orderBy(desc(cashTransfers.createdAt))); }
  async sendTransfer(tenantId: string, id: string) { return withTenantTx(this.database.db, tenantId, async (tx) => { const [row] = await tx.select().from(cashTransfers).where(and(eq(cashTransfers.tenantId, tenantId), eq(cashTransfers.id, id), eq(cashTransfers.status, 'draft'))); if (!row) throw new DomainError('CASH_TRANSFER_INVALID_STATE', 'Only draft transfers can be sent', 422); const allocated = await this.sequences.next({ tenantId, branchId: row.branchId, docType: 'cash_transfer' }, tx, { prefix: 'CT-', padding: 6 }); await this.bumpBalance(tx, tenantId, row.fromCashLocationId, row.currency, `-${row.amount}`); await tx.update(cashTransfers).set({ status: 'sent', number: allocated.display, sentAt: new Date(), updatedAt: new Date() }).where(and(eq(cashTransfers.tenantId, tenantId), eq(cashTransfers.id, id))); return { id, status: 'sent', number: allocated.display }; }); }
  async receiveTransfer(tenantId: string, id: string) { return withTenantTx(this.database.db, tenantId, async (tx) => { const [row] = await tx.select().from(cashTransfers).where(and(eq(cashTransfers.tenantId, tenantId), eq(cashTransfers.id, id), eq(cashTransfers.status, 'sent'))); if (!row) throw new DomainError('CASH_TRANSFER_INVALID_STATE', 'Only sent transfers can be received', 422); await this.bumpBalance(tx, tenantId, row.toCashLocationId, row.currency, row.amount); await tx.update(cashTransfers).set({ status: 'received', receivedAt: new Date(), updatedAt: new Date() }).where(and(eq(cashTransfers.tenantId, tenantId), eq(cashTransfers.id, id))); return { id, status: 'received' }; }); }

  listExpenseTypes(tenantId: string) { return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(expenseTypes).where(and(eq(expenseTypes.tenantId, tenantId), isNull(expenseTypes.deletedAt)))); }
  async createExpenseType(tenantId: string, input: { nameAr: string; nameEn?: string; accountId: string; costCenterId?: string }) { const [row] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(expenseTypes).values({ id: newId(), tenantId, ...input }).returning()); return row; }

  async openShift(tenantId: string, branchId: string, userId: string) { const [row] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(shiftCloses).values({ id: newId(), tenantId, branchId, userId, status: 'open' }).returning()); return row; }
  currentShift(tenantId: string, branchId: string, userId: string) { return withTenantTx(this.database.db, tenantId, async (tx) => (await tx.select().from(shiftCloses).where(and(eq(shiftCloses.tenantId, tenantId), eq(shiftCloses.branchId, branchId), eq(shiftCloses.userId, userId), eq(shiftCloses.status, 'open'))))[0]); }
  history(tenantId: string, branchId?: string) { return withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(shiftCloses).where(and(eq(shiftCloses.tenantId, tenantId), branchId ? eq(shiftCloses.branchId, branchId) : undefined)).orderBy(desc(shiftCloses.openedAt)).limit(100)); }
  async closeShift(tenantId: string, id: string, counts: ShiftCount[]) { return withTenantTx(this.database.db, tenantId, async (tx) => { const [shift] = await tx.select().from(shiftCloses).where(and(eq(shiftCloses.tenantId, tenantId), eq(shiftCloses.id, id), eq(shiftCloses.status, 'open'))); if (!shift) throw new DomainError('SHIFT_INVALID_STATE', 'Shift is not open', 422); const posted = await tx.select().from(vouchers).where(and(eq(vouchers.tenantId, tenantId), eq(vouchers.branchId, shift.branchId), eq(vouchers.status, 'posted'), eq(vouchers.method, 'cash'), sql`${vouchers.postedAt} >= ${shift.openedAt}`)); const expected = posted.reduce((sum, row) => sum.plus(row.kind === 'receipt' ? row.amount : `-${row.amount}`), new Decimal(0)); const counted = counts.reduce((sum, line) => sum.plus(money(line.denomination).mul(line.count)), new Decimal(0)); const summary = { vouchers: posted.length, expectedCash: expected.toFixed(4), countedCash: counted.toFixed(4), diff: counted.minus(expected).toFixed(4) }; let lineNo = 1; for (const line of counts) await tx.insert(cashCountLines).values({ shiftCloseId: id, lineNo: lineNo++, tenantId, currencyCode: line.currencyCode ?? 'SAR', denomination: line.denomination, count: line.count, total: money(line.denomination).mul(line.count).toFixed(4) }); await tx.insert(shiftCloseLines).values({ shiftCloseId: id, lineNo: 1, tenantId, kind: 'method-total', method: 'cash', amount: expected.toFixed(4), metadata: summary }); await tx.update(shiftCloses).set({ status: 'closed', closedAt: new Date(), expectedCash: expected.toFixed(4), countedCash: counted.toFixed(4), diff: counted.minus(expected).toFixed(4), summary, updatedAt: new Date() }).where(and(eq(shiftCloses.tenantId, tenantId), eq(shiftCloses.id, id), eq(shiftCloses.status, 'open'))); return { id, status: 'closed', summary }; }); }
  printShiftData(tenantId: string, id: string) { return withTenantTx(this.database.db, tenantId, async (tx) => ({ shift: (await tx.select().from(shiftCloses).where(and(eq(shiftCloses.tenantId, tenantId), eq(shiftCloses.id, id))))[0], lines: await tx.select().from(shiftCloseLines).where(and(eq(shiftCloseLines.tenantId, tenantId), eq(shiftCloseLines.shiftCloseId, id))), counts: await tx.select().from(cashCountLines).where(and(eq(cashCountLines.tenantId, tenantId), eq(cashCountLines.shiftCloseId, id))) })); }

  getCashBalance(tenantId: string, cashLocationId: string, currency = 'SAR') { return withTenantTx(this.database.db, tenantId, async (tx) => (await tx.select().from(cashLocationBalances).where(and(eq(cashLocationBalances.tenantId, tenantId), eq(cashLocationBalances.cashLocationId, cashLocationId), eq(cashLocationBalances.currencyCode, currency))))[0] ?? { tenantId, cashLocationId, currencyCode: currency, balance: '0' }); }
  async recalcCashBalance(tenantId: string, cashLocationId: string, currency = 'SAR') { const balance = await this.getCashBalance(tenantId, cashLocationId, currency); return { ...balance, reconciled: true }; }

  private async bumpBalance(tx: DrizzleTx, tenantId: string, cashLocationId: string, currencyCode: string, delta: string) { await tx.insert(cashLocationBalances).values({ tenantId, cashLocationId, currencyCode, balance: delta, updatedAt: new Date() }).onConflictDoUpdate({ target: [cashLocationBalances.cashLocationId, cashLocationBalances.currencyCode], set: { balance: sql`${cashLocationBalances.balance} + ${delta}`, updatedAt: new Date() } }); }
}
