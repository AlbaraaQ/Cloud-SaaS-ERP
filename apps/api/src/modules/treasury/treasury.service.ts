/* eslint-disable no-restricted-syntax */
import { Inject, Injectable } from '@nestjs/common';
import { Decimal } from 'decimal.js';
import { and, desc, eq, gte, ilike, isNull, lte, ne, or, sql } from 'drizzle-orm';
import { DomainError, newId } from '@erp/contracts';
import {
  cashLocationBalances,
  cashLocations,
  cashTransfers,
  employees,
  expenseTypes,
  parties,
  paymentAllocations,
  shiftCloseLines,
  shiftCloses,
  cashCountLines,
  invoicePayments,
  salesInvoices,
  vouchers,
  withTenantTx,
  type DatabaseHandle,
  type DrizzleTx,
} from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';
import { AccountingService, type JournalLineInput } from '../accounting/accounting.service.js';
import { PostingProfilesService } from '../organization/posting-profiles/posting-profiles.service.js';
import { SequencesService } from '../platform-services/index.js';

export type VoucherInput = {
  branchId: string;
  kind: 'receipt' | 'payment';
  subtype: 'customer' | 'supplier' | 'expense' | 'account' | 'salary' | 'vat' | 'other';
  date: string;
  partyId?: string;
  counterAccountId?: string;
  cashLocationId: string;
  method: 'cash' | 'cheque' | 'bank_transfer' | 'card';
  amount: string;
  vatAmount?: string;
  netAmount?: string;
  currency?: string;
  chequeNo?: string;
  chequeDate?: string;
  bankName?: string;
  costCenterId?: string;
  referenceNo?: string;
  referenceDate?: string;
  recipient?: string;
  /**
   * 📝 البيان — the desktop's `Receipts.Notes`, which `BindReceiptToEntry` copies onto
   * the journal entry (`entry.Note = Receipt.Notes`). Without it a posted receipt reads
   * `receipt voucher RV-000004` in the ledger, which tells an auditor nothing.
   */
  description?: string;
  /** ⏰ الوقت — `HH:mm`. The desktop stores date *and* time, and حركة الصندوق filters by both. */
  voucherTime?: string;
  /** 👔 المندوب — `Receipts.SalesManID`, which the desktop carries onto the journal line. */
  salesmanId?: string;
  /** 💲 قيمة السند كما في عملتها الأجنبية; `amount` stays in the base currency. */
  foreignAmount?: string;
  fiscalPeriodId?: string;
  idempotencyKey?: string;
};
export type VoucherPostInput = {
  fiscalPeriodId?: string;
  journalLines?: JournalLineInput[];
  allocations?: Array<{ partyId: string; invoiceKind: string; invoiceId: string; amount: string }>;
};
export type TransferInput = {
  branchId: string;
  fromCashLocationId: string;
  toCashLocationId: string;
  amount: string;
  currency?: string;
};
export type ShiftCount = { currencyCode?: string; denomination: string; count: number };

const money = (value: string) => new Decimal(value);

/**
 * ⏰ الوقت — `Receipts.ReceiptDate` carries a time as well as a date, and حركة الصندوق is
 * filtered من وقت / إلى وقت. A clerk types `9:05`, not `09:05:00`, so both are accepted and
 * the stored value is always `HH:mm:ss`.
 */
function normaliseTime(value?: string | null): string | null {
  if (value === undefined || value === null || String(value).trim() === '') return null;
  const match = /^(\d{1,2}):(\d{2})(?::(\d{2}))?$/.exec(String(value).trim());
  const pad = (part: string) => part.padStart(2, '0');
  if (!match)
    throw new DomainError('VOUCHER_TIME_INVALID', 'Voucher time must look like 09:05', 422, {
      field: 'voucherTime',
    });
  const [hour, minute, second] = [Number(match[1]), Number(match[2]), Number(match[3] ?? '0')];
  if (hour > 23 || minute > 59 || second > 59)
    throw new DomainError('VOUCHER_TIME_INVALID', 'Voucher time must look like 09:05', 422, {
      field: 'voucherTime',
    });
  return `${pad(String(hour))}:${pad(String(minute))}:${pad(String(second))}`;
}

@Injectable()
export class TreasuryService {
  constructor(
    @Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle,
    private readonly accounting: AccountingService,
    private readonly sequences: SequencesService,
    private readonly profiles: PostingProfilesService,
  ) {}

  /**
   * `frmSandQ`'s 🔍 panel: 📅 من تاريخ / 📅 إلى تاريخ, 📋 كل الفترة, and a search by
   * 🔢 الرقم or رقم المرجع (`ReceiptOper.LoadReceipts`, `Class/ReceiptOper.cs:496`).
   */
  vouchers(tenantId: string, filters: { from?: string; to?: string; q?: string } = {}) {
    const q = filters.q?.trim();
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(vouchers)
        .where(
          and(
            eq(vouchers.tenantId, tenantId),
            filters.from ? gte(vouchers.date, filters.from) : undefined,
            filters.to ? lte(vouchers.date, filters.to) : undefined,
            q
              ? or(
                  ilike(vouchers.number, `%${q}%`),
                  ilike(vouchers.referenceNo, `%${q}%`),
                  ilike(vouchers.chequeNo, `%${q}%`),
                  ilike(vouchers.description, `%${q}%`),
                  ilike(vouchers.recipient, `%${q}%`),
                )
              : undefined,
          ),
        )
        .orderBy(desc(vouchers.date), desc(vouchers.createdAt))
        .limit(500),
    );
  }
  async getVoucher(tenantId: string, id: string) {
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(vouchers)
        .where(and(eq(vouchers.tenantId, tenantId), eq(vouchers.id, id))),
    );
    if (!row) throw new DomainError('VOUCHER_NOT_FOUND', 'Voucher was not found', 404);
    return row;
  }

  async createVoucher(tenantId: string, input: VoucherInput) {
    const amount = money(input.amount);
    if (!amount.isFinite() || amount.lte(0))
      throw new DomainError('VOUCHER_AMOUNT_INVALID', 'Voucher amount must be positive', 422);
    if (input.method === 'cheque' && !input.chequeNo)
      throw new DomainError('CHEQUE_NO_REQUIRED', 'Cheque vouchers require a cheque number', 422);
    const voucherTime = normaliseTime(input.voucherTime);
    if (input.foreignAmount !== undefined && input.foreignAmount !== '') {
      const foreign = new Decimal(input.foreignAmount);
      if (!foreign.isFinite() || foreign.lte(0))
        throw new DomainError('VOUCHER_FOREIGN_AMOUNT_INVALID', 'Foreign amount must be positive', 422, {
          field: 'foreignAmount',
        });
    }
    if (input.salesmanId) await this.assertSalesman(tenantId, input.salesmanId);
    const id = newId();
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .insert(vouchers)
        .values({
          id,
          tenantId,
          branchId: input.branchId,
          kind: input.kind,
          subtype: input.subtype,
          date: input.date,
          partyId: input.partyId,
          counterAccountId: input.counterAccountId,
          cashLocationId: input.cashLocationId,
          method: input.method,
          amount: input.amount,
          vatAmount: input.vatAmount ?? '0',
          netAmount: input.netAmount ?? input.amount,
          currency: input.currency ?? 'SAR',
          chequeNo: input.chequeNo,
          chequeDate: input.chequeDate,
          bankName: input.bankName,
          chequeState: input.method === 'cheque' ? 'pending' : null,
          costCenterId: input.costCenterId,
          referenceNo: input.referenceNo,
          referenceDate: input.referenceDate,
          recipient: input.recipient,
          description: input.description?.trim() || null,
          voucherTime,
          salesmanId: input.salesmanId ?? null,
          foreignAmount: input.foreignAmount ?? null,
          fiscalPeriodId: input.fiscalPeriodId,
          idempotencyKey: input.idempotencyKey,
        })
        .returning(),
    );
    return row;
  }

  /**
   * `frmSandQ.xaml.cs:903` updates the whole receipt, not just its money: date, reference,
   * treasury, salesman, payment type, the cheque block and the cost centre. A draft is a
   * draft — the one thing that cannot change is what the document *is* (`kind`/`subtype`),
   * because the numbering series and the journal shape are chosen from it.
   */
  async updateDraftVoucher(tenantId: string, id: string, input: Partial<VoucherInput>) {
    const row = await this.getVoucher(tenantId, id);
    if (row.status !== 'draft')
      throw new DomainError('VOUCHER_IMMUTABLE', 'Only draft vouchers can be changed', 409);
    if (input.salesmanId) await this.assertSalesman(tenantId, input.salesmanId);
    if (input.foreignAmount !== undefined && input.foreignAmount !== '') {
      const foreign = new Decimal(input.foreignAmount);
      if (!foreign.isFinite() || foreign.lte(0))
        throw new DomainError('VOUCHER_FOREIGN_AMOUNT_INVALID', 'Foreign amount must be positive', 422, {
          field: 'foreignAmount',
        });
    }
    const patch: Record<string, unknown> = { updatedAt: new Date() };
    const assignments = {
      plain: <T>(value: T) => value,
      money: (value: string) => value,
      trimmed: (value: string) => value.trim() || null,
      time: (value: string) => normaliseTime(value),
    } as const;
    const copy = <K extends keyof typeof assignments>(key: string, field: K, value: unknown) => {
      if (value !== undefined) patch[key] = assignments[field](value as never);
    };
    copy('branchId', 'plain', input.branchId);
    copy('date', 'plain', input.date);
    copy('voucherTime', 'time', input.voucherTime);
    copy('partyId', 'plain', input.partyId);
    copy('counterAccountId', 'plain', input.counterAccountId);
    copy('cashLocationId', 'plain', input.cashLocationId);
    copy('method', 'plain', input.method);
    copy('amount', 'money', input.amount);
    copy('vatAmount', 'money', input.vatAmount);
    copy('netAmount', 'money', input.netAmount);
    copy('currency', 'plain', input.currency);
    copy('foreignAmount', 'money', input.foreignAmount);
    copy('chequeNo', 'plain', input.chequeNo);
    copy('chequeDate', 'plain', input.chequeDate);
    copy('bankName', 'plain', input.bankName);
    copy('costCenterId', 'plain', input.costCenterId);
    copy('referenceNo', 'plain', input.referenceNo);
    copy('referenceDate', 'plain', input.referenceDate);
    copy('recipient', 'plain', input.recipient);
    copy('description', 'trimmed', input.description);
    copy('salesmanId', 'plain', input.salesmanId);
    copy('fiscalPeriodId', 'plain', input.fiscalPeriodId);
    await withTenantTx(this.database.db, tenantId, (tx) =>
      tx.update(vouchers).set(patch).where(and(eq(vouchers.tenantId, tenantId), eq(vouchers.id, id))),
    );
    return this.getVoucher(tenantId, id);
  }

  /** 👔 المندوب has to be one of *this* tenant's employees, or the commission is fiction. */
  private async assertSalesman(tenantId: string, salesmanId: string): Promise<void> {
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select({ id: employees.id })
        .from(employees)
        .where(and(eq(employees.tenantId, tenantId), eq(employees.id, salesmanId), isNull(employees.deletedAt)))
        .limit(1),
    );
    if (!row)
      throw new DomainError('SALESMAN_NOT_FOUND', 'That employee does not belong to this tenant', 422, {
        field: 'salesmanId',
      });
  }

  /**
   * The two accounts a voucher moves money between.
   *
   * `BindReceiptToEntry` (`Class/ReceiptOper.cs:21`) always built them: debit the treasury
   * account (`Receipt.DebitAcc`) and credit the client's account (`Receipt.CreditAcc`), with
   * the cost centre on the client line. The cloud left it to the caller, and the callers who
   * forgot — HRM's `payRun`, for one — paid a salary out of the safe with **no entry at all**:
   * the cash left and the ledger never heard of it. From here the entry is built unless the
   * caller deliberately overrides it.
   */
  private async voucherAccounts(
    tx: DrizzleTx,
    tenantId: string,
    voucher: typeof vouchers.$inferSelect,
  ): Promise<{ cashAccountId: string; counterAccountId: string }> {
    const docType = voucher.kind === 'receipt' ? 'receipt_voucher' : 'payment_voucher';
    const [location] = await tx
      .select()
      .from(cashLocations)
      .where(and(eq(cashLocations.tenantId, tenantId), eq(cashLocations.id, voucher.cashLocationId)))
      .limit(1);
    if (!location)
      throw new DomainError('CASH_LOCATION_NOT_FOUND', 'That cash location does not belong to this tenant', 422, {
        field: 'cashLocationId',
      });
    const cashAccountId =
      location.accountId ??
      (await this.profileAccount(tx, tenantId, voucher.branchId, docType, location.kind === 'bank' ? 'bankAccountId' : 'cashAccountId', { required: false }));
    if (!cashAccountId)
      throw new DomainError(
        'CASH_ACCOUNT_REQUIRED',
        'This cash location has no linked account, and the branch posting profile names no cash account',
        422,
        { field: 'cashLocationId' },
      );

    let counterAccountId = voucher.counterAccountId ?? undefined;
    if (!counterAccountId && voucher.partyId) {
      const [party] = await tx
        .select()
        .from(parties)
        .where(and(eq(parties.tenantId, tenantId), eq(parties.id, voucher.partyId)))
        .limit(1);
      counterAccountId =
        voucher.kind === 'receipt'
          ? (party?.receivableAccountId ?? undefined)
          : (party?.payableAccountId ?? undefined);
    }
    if (!counterAccountId)
      counterAccountId = await this.profileAccount(
        tx,
        tenantId,
        voucher.branchId,
        docType,
        voucher.kind === 'receipt' ? 'receivableAccountId' : 'payableAccountId',
        { required: false },
      );
    if (!counterAccountId)
      throw new DomainError(
        'COUNTER_ACCOUNT_REQUIRED',
        'A voucher needs somewhere for the money to come from: set the party, the counter account, or the branch posting profile',
        422,
        { field: 'counterAccountId' },
      );
    return { cashAccountId, counterAccountId };
  }

  private async resolveProfile(
    tx: DrizzleTx,
    tenantId: string,
    branchId: string,
    docType: string,
  ): Promise<{ mapping: unknown } | undefined> {
    try {
      return await this.profiles.resolvePostProfileInTx(tx, tenantId, branchId, docType);
    } catch (error) {
      if (error instanceof DomainError && error.code === 'ACCOUNT_PROFILE_MISSING') return undefined;
      throw error;
    }
  }

  private async profileAccount(
    tx: DrizzleTx,
    tenantId: string,
    branchId: string,
    docType: string,
    key: string,
    options: { required: boolean } = { required: true },
  ): Promise<string | undefined> {
    /**
     * A tenant that has never opened Settings › Posting profiles has no profile row at all,
     * and `ACCOUNT_PROFILE_MISSING` is the answer. That is a hard stop for an account the
     * engine *must* have, but not for one it is only *hoping* to find: the cash location's
     * own account, or the party's own account, is the better answer when it exists.
     */
    const profile = await this.resolveProfile(tx, tenantId, branchId, docType);
    const accountId = profile
      ? (profile.mapping as unknown as Record<string, string | null | undefined>)[key]
      : undefined;
    if (!accountId && options.required)
      throw new DomainError(
        'TREASURY_PROFILE_KEY_MISSING',
        `Posting profile has no ${key} — map it in Settings › Posting profiles`,
        422,
        { field: key },
      );
    return accountId ?? undefined;
  }

  /**
   * The entry a voucher writes, if the caller did not supply one. A pending cheque does not
   * touch the treasury: it sits in أوراق القبض (`chequesInHandAccountId`) until it clears,
   * which is why `bumpBalance` skips it and why the two have to agree.
   */
  private async defaultJournalLines(
    tx: DrizzleTx,
    tenantId: string,
    voucher: typeof vouchers.$inferSelect,
  ): Promise<JournalLineInput[]> {
    const { cashAccountId, counterAccountId } = await this.voucherAccounts(tx, tenantId, voucher);
    const treasuryAccountId =
      voucher.method === 'cheque'
        ? ((await this.profileAccount(tx, tenantId, voucher.branchId, voucher.kind === 'receipt' ? 'receipt_voucher' : 'payment_voucher', 'chequesInHandAccountId', { required: false })) ??
          cashAccountId)
        : cashAccountId;
    const amount = voucher.netAmount && Number(voucher.netAmount) > 0 ? voucher.netAmount : voucher.amount;
    const statement = voucher.description ?? (voucher.kind === 'receipt' ? 'سند قبض' : 'سند صرف');
    const counterLine: JournalLineInput = {
      accountId: counterAccountId,
      costCenterId: voucher.costCenterId ?? undefined,
      partyId: voucher.partyId ?? undefined,
      description: statement,
      ...(voucher.kind === 'receipt' ? { credit: amount } : { debit: amount }),
    };
    const treasuryLine: JournalLineInput = {
      accountId: treasuryAccountId,
      description: statement,
      ...(voucher.kind === 'receipt' ? { debit: amount } : { credit: amount }),
    };
    return [treasuryLine, counterLine];
  }

  async postVoucher(tenantId: string, id: string, input: VoucherPostInput = {}) {
    const voucher = await this.getVoucher(tenantId, id);
    if (voucher.status === 'posted') return voucher;
    if (voucher.status !== 'draft')
      throw new DomainError('VOUCHER_INVALID_STATUS', 'Only draft vouchers can be posted', 409);
    if (!input.journalLines?.length && !input.fiscalPeriodId && !voucher.fiscalPeriodId) {
      // Nothing to post into until we know the period — checked here so the failure is a
      // 422 on the request, not a half-written transaction.
      await withTenantTx(this.database.db, tenantId, (tx) =>
        this.accounting.openPeriodForDateInTx(tx, tenantId, voucher.date),
      );
    }
    if (input.journalLines?.length && !input.fiscalPeriodId && !voucher.fiscalPeriodId)
      throw new DomainError(
        'VOUCHER_FISCAL_PERIOD_REQUIRED',
        'A fiscal period is required for voucher journals',
        422,
      );
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      const [locked] = await tx
        .select()
        .from(vouchers)
        .where(and(eq(vouchers.tenantId, tenantId), eq(vouchers.id, id), eq(vouchers.status, 'draft')));
      if (!locked) throw new DomainError('VOUCHER_INVALID_STATUS', 'Only draft vouchers can be posted', 409);
      const allocated = await this.sequences.next(
        { tenantId, branchId: locked.branchId, docType: `${locked.kind}_voucher` },
        tx,
        { prefix: locked.kind === 'receipt' ? 'RV-' : 'PV-', padding: 6 },
      );
      let journalEntryId: string | null = null;
      const lines = input.journalLines?.length
        ? input.journalLines
        : await this.defaultJournalLines(tx, tenantId, locked);
      if (lines.length) {
        const journal = await this.accounting.postJournalInTx(tx, tenantId, {
          branchId: locked.branchId,
          fiscalPeriodId:
            input.fiscalPeriodId ??
            locked.fiscalPeriodId ??
            (await this.accounting.openPeriodForDateInTx(tx, tenantId, locked.date)),
          date: locked.date,
          description:
            locked.description?.trim() ||
            `${locked.kind === 'receipt' ? 'سند قبض' : 'سند صرف'} ${allocated.display}`,
          lines,
          sourceType: 'voucher',
          sourceId: id,
        });
        journalEntryId = journal?.id ?? null;
      }
      if (locked.method !== 'cheque')
        await this.bumpBalance(
          tx,
          tenantId,
          locked.cashLocationId,
          locked.currency,
          locked.kind === 'receipt' ? locked.amount : `-${locked.amount}`,
        );
      for (const allocation of input.allocations ?? []) {
        if (money(allocation.amount).lte(0) || money(allocation.amount).gt(money(locked.amount)))
          throw new DomainError(
            'ALLOCATION_AMOUNT_INVALID',
            'Allocation amount is outside voucher bounds',
            422,
          );
        await tx
          .insert(paymentAllocations)
          .values({
            id: newId(),
            tenantId,
            partyId: allocation.partyId,
            voucherId: id,
            invoiceKind: allocation.invoiceKind,
            invoiceId: allocation.invoiceId,
            amount: allocation.amount,
          });
      }
      await tx
        .update(vouchers)
        .set({
          status: 'posted',
          number: allocated.display,
          journalEntryId,
          postedAt: new Date(),
          updatedAt: new Date(),
        })
        .where(and(eq(vouchers.tenantId, tenantId), eq(vouchers.id, id)));
    });
    return this.getVoucher(tenantId, id);
  }

  async voidVoucher(tenantId: string, id: string, reason: string) {
    if (!reason.trim()) throw new DomainError('VOUCHER_VOID_REASON_REQUIRED', 'Void reason is required', 422);
    const row = await this.getVoucher(tenantId, id);
    if (row.status !== 'posted')
      throw new DomainError('VOUCHER_INVALID_STATUS', 'Only posted vouchers can be voided', 409);
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      if (row.method !== 'cheque')
        await this.bumpBalance(
          tx,
          tenantId,
          row.cashLocationId,
          row.currency,
          row.kind === 'receipt' ? `-${row.amount}` : row.amount,
        );
      await tx
        .update(vouchers)
        .set({ status: 'voided', voidedAt: new Date(), updatedAt: new Date() })
        .where(and(eq(vouchers.tenantId, tenantId), eq(vouchers.id, id)));
    });
    return this.getVoucher(tenantId, id);
  }

  async transitionCheque(tenantId: string, id: string, action: 'clear' | 'bounce' | 'collect') {
    const target = action === 'clear' ? 'cleared' : action === 'bounce' ? 'bounced' : 'collected';
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      /** `typeof vouchers.$inferSelect` — the full row, so the entry can read its البيان. */
      const [row] = await tx
        .select()
        .from(vouchers)
        .where(and(eq(vouchers.tenantId, tenantId), eq(vouchers.id, id)));
      if (!row || row.method !== 'cheque' || row.status !== 'posted')
        throw new DomainError('CHEQUE_INVALID_STATE', 'Cheque voucher is not posted', 422);
      if (row.chequeState !== 'pending')
        throw new DomainError('CHEQUE_INVALID_STATE', 'Cheque transition is terminal', 422);
      /**
       * A pending cheque is a promise, not money: أوراق القبض holds it until the bank
       * honours it. Clearing is what turns the promise into cash — Dr البنك / Cr أوراق
       * القبض — and bouncing is what takes it back: the customer owes the money again.
       * The cloud used to move the balance here with no entry at all, so a cleared cheque
       * was invisible to the ledger the bank statement has to agree with.
       */
      if (target === 'cleared' || target === 'collected') {
        await this.bumpBalance(
          tx,
          tenantId,
          row.cashLocationId,
          row.currency,
          row.kind === 'receipt' ? row.amount : `-${row.amount}`,
        );
        const { cashAccountId } = await this.voucherAccounts(tx, tenantId, row);
        const docType = row.kind === 'receipt' ? 'receipt_voucher' : 'payment_voucher';
        const chequesAccountId =
          (await this.profileAccount(tx, tenantId, row.branchId, docType, 'chequesInHandAccountId', {
            required: false,
          })) ?? cashAccountId;
        const amount = row.netAmount && Number(row.netAmount) > 0 ? row.netAmount : row.amount;
        const statement = row.description?.trim() || `${row.kind === 'receipt' ? 'سند قبض' : 'سند صرف'} ${row.number ?? ''}`.trim();
        await this.accounting.postJournalInTx(tx, tenantId, {
          branchId: row.branchId,
          fiscalPeriodId:
            row.fiscalPeriodId ?? (await this.accounting.openPeriodForDateInTx(tx, tenantId, row.date)),
          date: row.date,
          description: `${target === 'collected' ? 'تحصيل' : 'تحصيل'} شيك ${row.chequeNo ?? ''}`.trim(),
          lines:
            row.kind === 'receipt'
              ? [
                  { accountId: cashAccountId, debit: amount, description: statement },
                  { accountId: chequesAccountId, credit: amount, description: statement },
                ]
              : [
                  { accountId: chequesAccountId, debit: amount, description: statement },
                  { accountId: cashAccountId, credit: amount, description: statement },
                ],
          sourceType: 'voucher_cheque',
          sourceId: id,
          idempotencyKey: `voucher-cheque:${id}:${target}`,
        });
      }
      if (target === 'bounced') {
        const { cashAccountId, counterAccountId } = await this.voucherAccounts(tx, tenantId, row);
        const docType = row.kind === 'receipt' ? 'receipt_voucher' : 'payment_voucher';
        const chequesAccountId =
          (await this.profileAccount(tx, tenantId, row.branchId, docType, 'chequesInHandAccountId', {
            required: false,
          })) ?? cashAccountId;
        const amount = row.netAmount && Number(row.netAmount) > 0 ? row.netAmount : row.amount;
        const statement = row.description?.trim() || 'شيك مرتجع';
        // مرتجع: the cheque never became money, so the debt it was meant to settle returns.
        await this.accounting.postJournalInTx(tx, tenantId, {
          branchId: row.branchId,
          fiscalPeriodId:
            row.fiscalPeriodId ?? (await this.accounting.openPeriodForDateInTx(tx, tenantId, row.date)),
          date: row.date,
          description: `ارتجاع شيك ${row.chequeNo ?? ''}`.trim(),
          lines:
            row.kind === 'receipt'
              ? [
                  { accountId: counterAccountId, debit: amount, description: statement },
                  { accountId: chequesAccountId, credit: amount, description: statement },
                ]
              : [
                  { accountId: chequesAccountId, debit: amount, description: statement },
                  { accountId: counterAccountId, credit: amount, description: statement },
                ],
          sourceType: 'voucher_cheque',
          sourceId: id,
          idempotencyKey: `voucher-cheque:${id}:bounced`,
        });
      }
      await tx
        .update(vouchers)
        .set({ chequeState: target, updatedAt: new Date() })
        .where(
          and(eq(vouchers.tenantId, tenantId), eq(vouchers.id, id), eq(vouchers.chequeState, 'pending')),
        );
      return { id, chequeState: target };
    });
  }

  async createTransfer(tenantId: string, input: TransferInput) {
    if (input.fromCashLocationId === input.toCashLocationId)
      throw new DomainError('CASH_TRANSFER_INVALID', 'Transfer requires distinct locations', 422);
    const amount = money(input.amount);
    if (!amount.isFinite() || amount.lte(0))
      throw new DomainError('CASH_TRANSFER_AMOUNT_INVALID', 'Transfer amount must be positive', 422);
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .insert(cashTransfers)
        .values({
          id: newId(),
          tenantId,
          branchId: input.branchId,
          fromCashLocationId: input.fromCashLocationId,
          toCashLocationId: input.toCashLocationId,
          amount: input.amount,
          currency: input.currency ?? 'SAR',
        })
        .returning(),
    );
    return row;
  }
  transfers(tenantId: string) {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(cashTransfers)
        .where(eq(cashTransfers.tenantId, tenantId))
        .orderBy(desc(cashTransfers.createdAt)),
    );
  }
  async sendTransfer(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [row] = await tx
        .select()
        .from(cashTransfers)
        .where(
          and(
            eq(cashTransfers.tenantId, tenantId),
            eq(cashTransfers.id, id),
            eq(cashTransfers.status, 'draft'),
          ),
        );
      if (!row) throw new DomainError('CASH_TRANSFER_INVALID_STATE', 'Only draft transfers can be sent', 422);
      const allocated = await this.sequences.next(
        { tenantId, branchId: row.branchId, docType: 'cash_transfer' },
        tx,
        { prefix: 'CT-', padding: 6 },
      );
      await this.bumpBalance(tx, tenantId, row.fromCashLocationId, row.currency, `-${row.amount}`);
      await tx
        .update(cashTransfers)
        .set({ status: 'sent', number: allocated.display, sentAt: new Date(), updatedAt: new Date() })
        .where(and(eq(cashTransfers.tenantId, tenantId), eq(cashTransfers.id, id)));
      return { id, status: 'sent', number: allocated.display };
    });
  }
  async receiveTransfer(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [row] = await tx
        .select()
        .from(cashTransfers)
        .where(
          and(
            eq(cashTransfers.tenantId, tenantId),
            eq(cashTransfers.id, id),
            eq(cashTransfers.status, 'sent'),
          ),
        );
      if (!row)
        throw new DomainError('CASH_TRANSFER_INVALID_STATE', 'Only sent transfers can be received', 422);
      await this.bumpBalance(tx, tenantId, row.toCashLocationId, row.currency, row.amount);
      await tx
        .update(cashTransfers)
        .set({ status: 'received', receivedAt: new Date(), updatedAt: new Date() })
        .where(and(eq(cashTransfers.tenantId, tenantId), eq(cashTransfers.id, id)));
      return { id, status: 'received' };
    });
  }

  listExpenseTypes(tenantId: string) {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(expenseTypes)
        .where(and(eq(expenseTypes.tenantId, tenantId), isNull(expenseTypes.deletedAt))),
    );
  }
  async createExpenseType(
    tenantId: string,
    input: { nameAr: string; nameEn?: string; accountId: string; costCenterId?: string },
  ) {
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .insert(expenseTypes)
        .values({ id: newId(), tenantId, ...input })
        .returning(),
    );
    return row;
  }
  async updateExpenseType(
    tenantId: string,
    id: string,
    input: { nameAr?: string; nameEn?: string | null; accountId?: string; costCenterId?: string | null },
  ) {
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .update(expenseTypes)
        .set({
          ...(input.nameAr === undefined ? {} : { nameAr: input.nameAr }),
          ...(input.nameEn === undefined ? {} : { nameEn: input.nameEn }),
          ...(input.accountId === undefined ? {} : { accountId: input.accountId }),
          ...(input.costCenterId === undefined ? {} : { costCenterId: input.costCenterId }),
          updatedAt: new Date(),
        })
        .where(
          and(eq(expenseTypes.tenantId, tenantId), eq(expenseTypes.id, id), isNull(expenseTypes.deletedAt)),
        )
        .returning(),
    );
    if (!row) throw new DomainError('NOT_FOUND', 'Expense card was not found', 404);
    return row;
  }
  /** Expense cards are only labels over an account, so removing one never touches the ledger. */
  async deleteExpenseType(tenantId: string, id: string) {
    const result = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .update(expenseTypes)
        .set({ deletedAt: new Date(), updatedAt: new Date() })
        .where(
          and(eq(expenseTypes.tenantId, tenantId), eq(expenseTypes.id, id), isNull(expenseTypes.deletedAt)),
        ),
    );
    if (!result.rowCount) throw new DomainError('NOT_FOUND', 'Expense card was not found', 404);
    return { id, deleted: true };
  }

  async openShift(tenantId: string, branchId: string, userId: string) {
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx.insert(shiftCloses).values({ id: newId(), tenantId, branchId, userId, status: 'open' }).returning(),
    );
    return row;
  }
  /**
   * The caller's open shift, with its takings *so far* attached as `live` — the
   * same numbers `closeShift` will freeze, so a cashier can see what the drawer
   * should hold before counting it.
   */
  async currentShift(tenantId: string, branchId: string, userId: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [shift] = await tx
        .select()
        .from(shiftCloses)
        .where(
          and(
            eq(shiftCloses.tenantId, tenantId),
            eq(shiftCloses.branchId, branchId),
            eq(shiftCloses.userId, userId),
            eq(shiftCloses.status, 'open'),
          ),
        );
      if (!shift) return null;
      return { ...shift, live: await this.shiftTakings(tx, tenantId, shift) };
    });
  }

  /**
   * What the till has taken since `shift` opened: manual cash vouchers plus the
   * payments the sales engine wrote for this shift's invoices (or, for invoices
   * captured with no shift, for this branch during the window). Returns are
   * separated so a refund never inflates the drawer.
   */
  private async shiftTakings(
    tx: DrizzleTx,
    tenantId: string,
    shift: { id: string; branchId: string; openedAt: Date },
  ) {
    const posted = await tx
      .select()
      .from(vouchers)
      .where(
        and(
          eq(vouchers.tenantId, tenantId),
          eq(vouchers.branchId, shift.branchId),
          eq(vouchers.status, 'posted'),
          eq(vouchers.method, 'cash'),
          sql`${vouchers.postedAt} >= ${shift.openedAt}`,
        ),
      );
    const voucherCash = posted.reduce(
      (sum, row) => sum.plus(row.kind === 'receipt' ? row.amount : `-${row.amount}`),
      new Decimal(0),
    );

    const settled = await tx
      .select({ method: invoicePayments.method, amount: invoicePayments.amount, kind: salesInvoices.kind })
      .from(invoicePayments)
      .innerJoin(salesInvoices, eq(salesInvoices.id, invoicePayments.invoiceId))
      .where(
        and(
          eq(invoicePayments.tenantId, tenantId),
          eq(salesInvoices.tenantId, tenantId),
          eq(salesInvoices.branchId, shift.branchId),
          gte(invoicePayments.createdAt, shift.openedAt),
          ne(salesInvoices.status, 'voided'),
          or(eq(salesInvoices.shiftId, shift.id), isNull(salesInvoices.shiftId)),
        ),
      );

    const zero = () => ({
      cash: new Decimal(0),
      card: new Decimal(0),
      bank: new Decimal(0),
      credit: new Decimal(0),
      other: new Decimal(0),
    });
    const sales = zero();
    const returns = zero();
    const bucket = (method: string) =>
      method === 'cash' || method === 'card' || method === 'bank' || method === 'credit' ? method : 'other';
    for (const row of settled) {
      const target = row.kind === 'sale_return' || row.kind === 'credit_note' ? returns : sales;
      target[bucket(row.method)] = target[bucket(row.method)].plus(row.amount);
    }

    return {
      vouchers: posted.length,
      vouchersCash: voucherCash.toFixed(4),
      invoices: settled.length,
      sales: {
        cash: sales.cash.toFixed(4),
        card: sales.card.toFixed(4),
        bank: sales.bank.toFixed(4),
        credit: sales.credit.toFixed(4),
      },
      returns: {
        cash: returns.cash.toFixed(4),
        card: returns.card.toFixed(4),
        bank: returns.bank.toFixed(4),
        credit: returns.credit.toFixed(4),
      },
      expectedCash: voucherCash.plus(sales.cash).minus(returns.cash).toFixed(4),
    };
  }
  history(tenantId: string, branchId?: string) {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(shiftCloses)
        .where(
          and(eq(shiftCloses.tenantId, tenantId), branchId ? eq(shiftCloses.branchId, branchId) : undefined),
        )
        .orderBy(desc(shiftCloses.openedAt))
        .limit(100),
    );
  }
  /**
   * Closes a cashier shift: counts the drawer and compares it against what the
   * till should hold.
   *
   * Before Phase 04 the "expected" side came from cash vouchers alone, so a shift
   * made entirely of POS sales closed with an expected cash of zero and every
   * counted note looked like a surplus. The desktop (`frmCloseShift`) summarised
   * the cashier's own takings — cash, network and postponed — and this now does
   * the same: manual vouchers *plus* the payments the sales engine wrote for
   * invoices captured by this shift (or, for invoices with no shift, posted at
   * this branch during the shift window). Returns are subtracted per method, so
   * a cash refund shrinks the expected cash instead of inflating it.
   */
  async closeShift(tenantId: string, id: string, counts: ShiftCount[]) {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const [shift] = await tx
        .select()
        .from(shiftCloses)
        .where(
          and(eq(shiftCloses.tenantId, tenantId), eq(shiftCloses.id, id), eq(shiftCloses.status, 'open')),
        );
      if (!shift) throw new DomainError('SHIFT_INVALID_STATE', 'Shift is not open', 422);

      const takings = await this.shiftTakings(tx, tenantId, shift);
      const expected = money(takings.expectedCash);
      const counted = counts.reduce(
        (sum, line) => sum.plus(money(line.denomination).mul(line.count)),
        new Decimal(0),
      );
      const summary = {
        ...takings,
        countedCash: counted.toFixed(4),
        diff: counted.minus(expected).toFixed(4),
      };

      let lineNo = 1;
      for (const line of counts)
        await tx
          .insert(cashCountLines)
          .values({
            shiftCloseId: id,
            lineNo: lineNo++,
            tenantId,
            currencyCode: line.currencyCode ?? 'SAR',
            denomination: line.denomination,
            count: line.count,
            total: money(line.denomination).mul(line.count).toFixed(4),
          });
      let summaryLine = 1;
      const totals: Array<{
        kind: string;
        method: string;
        amount: Decimal;
        metadata?: Record<string, unknown>;
      }> = [
        { kind: 'method-total', method: 'cash', amount: expected, metadata: summary },
        {
          kind: 'method-total',
          method: 'card',
          amount: money(takings.sales.card).minus(money(takings.returns.card)),
        },
        {
          kind: 'method-total',
          method: 'bank',
          amount: money(takings.sales.bank).minus(money(takings.returns.bank)),
        },
        {
          kind: 'method-total',
          method: 'credit',
          amount: money(takings.sales.credit).minus(money(takings.returns.credit)),
        },
        { kind: 'voucher-cash', method: 'cash', amount: money(takings.vouchersCash) },
      ];
      for (const total of totals)
        await tx
          .insert(shiftCloseLines)
          .values({
            shiftCloseId: id,
            lineNo: summaryLine++,
            tenantId,
            kind: total.kind,
            method: total.method,
            amount: total.amount.toFixed(4),
            metadata: total.metadata ?? {},
          });
      await tx
        .update(shiftCloses)
        .set({
          status: 'closed',
          closedAt: new Date(),
          expectedCash: expected.toFixed(4),
          countedCash: counted.toFixed(4),
          diff: counted.minus(expected).toFixed(4),
          summary,
          updatedAt: new Date(),
        })
        .where(
          and(eq(shiftCloses.tenantId, tenantId), eq(shiftCloses.id, id), eq(shiftCloses.status, 'open')),
        );
      return { id, status: 'closed', summary };
    });
  }
  printShiftData(tenantId: string, id: string) {
    return withTenantTx(this.database.db, tenantId, async (tx) => ({
      shift: (
        await tx
          .select()
          .from(shiftCloses)
          .where(and(eq(shiftCloses.tenantId, tenantId), eq(shiftCloses.id, id)))
      )[0],
      lines: await tx
        .select()
        .from(shiftCloseLines)
        .where(and(eq(shiftCloseLines.tenantId, tenantId), eq(shiftCloseLines.shiftCloseId, id))),
      counts: await tx
        .select()
        .from(cashCountLines)
        .where(and(eq(cashCountLines.tenantId, tenantId), eq(cashCountLines.shiftCloseId, id))),
    }));
  }

  getCashBalance(tenantId: string, cashLocationId: string, currency = 'SAR') {
    return withTenantTx(
      this.database.db,
      tenantId,
      async (tx) =>
        (
          await tx
            .select()
            .from(cashLocationBalances)
            .where(
              and(
                eq(cashLocationBalances.tenantId, tenantId),
                eq(cashLocationBalances.cashLocationId, cashLocationId),
                eq(cashLocationBalances.currencyCode, currency),
              ),
            )
        )[0] ?? { tenantId, cashLocationId, currencyCode: currency, balance: '0' },
    );
  }
  async recalcCashBalance(tenantId: string, cashLocationId: string, currency = 'SAR') {
    const balance = await this.getCashBalance(tenantId, cashLocationId, currency);
    return { ...balance, reconciled: true };
  }

  private async bumpBalance(
    tx: DrizzleTx,
    tenantId: string,
    cashLocationId: string,
    currencyCode: string,
    delta: string,
  ) {
    await tx
      .insert(cashLocationBalances)
      .values({ tenantId, cashLocationId, currencyCode, balance: delta, updatedAt: new Date() })
      .onConflictDoUpdate({
        target: [cashLocationBalances.cashLocationId, cashLocationBalances.currencyCode],
        set: { balance: sql`${cashLocationBalances.balance} + ${delta}`, updatedAt: new Date() },
      });
  }
}
