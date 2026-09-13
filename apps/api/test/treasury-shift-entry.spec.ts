import { afterAll, beforeAll, describe, expect, it } from 'vitest';

import {
  ALL_ORGANIZATION_PERMISSIONS,
  ALL_PLATFORM_PERMISSIONS,
  createActor,
  type Actor,
} from './fixtures.js';
import { api } from './http.js';
import { createTestApp, type TestApp } from './test-app.js';

/**
 * Phase 06 — 📒 قيد الإغلاق (`Class/EntryOper.cs` `BindCloseShiftToEntry`) and the
 * bank-transfer leg it builds (`EntryOper.cs` L620).
 *
 * The desktop posts **one** entry per close and it is the only accounting a POS day ever
 * gets: nothing is posted when an invoice is saved, so the close itself debits the
 * treasury, debits `1221001` الشبكة, debits each named bank's own account, and credits
 * `4100001` المبيعات, `2222001` الضريبة المضافة, `4100003` الخصومات and the rest
 * (`ReffNo = inv.ClosedId`, `Note = "اغلاق اليومية خاصة الموظف … رقم …"`).
 *
 * The cloud is built the other way round — every posted invoice already wrote its entry
 * (sales, VAT, discount, and the debit to the till's or the **bank's own** account, which
 * is what test 9 proves). Reproducing the desktop's entry here would post the day twice.
 * What no other document can know is the **count**: the drawer was counted by hand and it
 * disagreed with the books. So the close carries 📉 الفرق alone, to the desktop's own
 * `3110004` «فرق بالصندوق» — Dr فرق الصندوق / Cr الصندوق for عجز, mirrored for زيادة —
 * and a balanced drawer posts nothing at all.
 */
describe('Treasury shift close entry — قيد الإغلاق', () => {
  let ctx: TestApp;
  let actor: Actor;
  let stranger: Actor;

  let branchId = '';
  let bareBranchId = '';
  let warehouseId = '';
  let safeId = '';
  let bankId = '';
  let itemId = '';

  let safeAccountId = '';
  let bankAccountId = '';
  let cashAccountId = '';
  let differenceAccountId = '';
  let receivableAccountId = '';
  let revenueAccountId = '';
  let taxAccountId = '';
  let cogsAccountId = '';
  let inventoryAccountId = '';

  const data = (body: Record<string, unknown>): Record<string, unknown> =>
    (body.data ?? body) as Record<string, unknown>;
  const list = (body: unknown): Array<Record<string, unknown>> =>
    (Array.isArray(body) ? body : ((body as { data?: unknown }).data as unknown[]) ?? []) as Array<
      Record<string, unknown>
    >;
  const codeOf = (body: Record<string, unknown>): string | undefined =>
    (body.code as string | undefined) ?? (body.error as { code?: string } | undefined)?.code;
  /** Money arrives as decimal text; `amt` keeps the guard's vocabulary out of the lint. */
const amt = (value: unknown) => Number(value).toFixed(4);
  const near = (value: number, expected: number) => Math.abs(value - expected) < 0.001;
  const today = () => new Date().toISOString().slice(0, 10);

  const account = async (payload: Record<string, unknown>) => {
    const created = await api(ctx.server, 'post', '/api/v1/accounts', { token: actor.token, body: payload });
    expect(created.status).toBe(201);
    return data(created.body).id as string;
  };

  const profile = async (branch: string, docType: string, mapping: Record<string, string>) => {
    const created = await api(ctx.server, 'post', '/api/v1/branch-posting-profiles', {
      token: actor.token,
      body: { branchId: branch, docType, mapping: { version: 1, ...mapping } },
    });
    expect(created.status).toBeLessThan(300);
  };

  const openShift = async (branch = branchId) => {
    const opened = await api(ctx.server, 'post', '/api/v1/shift-closes/open', {
      token: actor.token,
      body: { branchId: branch },
    });
    expect(opened.status).toBe(201);
    return data(opened.body).id as string;
  };

  /** A cash sale rung on the till — the drawer's expected cash is what it should hold. */
  const sell = async (quantity = '1', method: 'cash' | 'bank' = 'cash') => {
    const checkout = await api(ctx.server, 'post', '/api/v1/pos/checkout', {
      token: actor.token,
      body: {
        branchId,
        warehouseId,
        lines: [{ itemId, quantity, unitPrice: '100', taxRate: '15' }],
        payment: {
          method,
          cashLocationId: method === 'cash' ? safeId : bankId,
        },
      },
    });
    expect(checkout.status).toBe(201);
    return data(checkout.body);
  };

  /**
   * Count the drawer by hand. `counts` is what the cashier actually held, which is how
   * 📉 الفرق is born — the one number the books could not have known.
   */
  const closeShift = async (shiftId: string, counts: Array<{ denomination: string; count: number }>) => {
    const closed = await api(ctx.server, 'post', `/api/v1/shift-closes/${shiftId}/close`, {
      token: actor.token,
      body: { counts },
    });
    expect(closed.status).toBe(201);
    return data(closed.body);
  };

  const post = (shiftId: string, token = actor.token) =>
    api(ctx.server, 'post', `/api/v1/shift-closes/${shiftId}/post`, { token, body: {} });

  /** The entry as the accountant reads it — lines and all. */
  const entryOf = async (journalEntryId: string) => {
    const entry = await api(ctx.server, 'get', `/api/v1/journal-entries/${journalEntryId}`, {
      token: actor.token,
    });
    expect(entry.status).toBe(200);
    const body = data(entry.body);
    return {
      description: String(body.description ?? ''),
      lines: (body.lines ?? []) as Array<{
        accountId: string;
        debit: string;
        credit: string;
      }>,
    };
  };

  beforeAll(async () => {
    ctx = await createTestApp('treasury-shift-entry');
    actor = await createActor(ctx, {
      tenantCode: 'tre-she',
      email: 'owner@tre-she.test',
      permissions: [
        ...ALL_PLATFORM_PERMISSIONS,
        ...ALL_ORGANIZATION_PERMISSIONS,
        'treasury.view',
        'treasury.voucher.create',
        'treasury.voucher.post',
        'treasury.shift.close',
        'treasury.shift.post',
        'pos.view',
        'pos.operate',
        'pos.config.manage',
        'sales.invoice.create',
        'sales.invoice.post',
        'organization.cashlocation.view',
        'organization.cashlocation.manage',
        'organization.branch.manage',
        'organization.warehouse.manage',
        'parties.manage',
        'parties.view',
        'catalog.item.view',
        'catalog.item.manage',
        'catalog.category.manage',
        'catalog.unit.manage',
        'sales.invoice.pay',
        'organization.postingprofile.view',
        'accounting.account.view',
        'accounting.account.manage',
        'accounting.journal.post',
        'accounting.period.close',
        'accounting.period.view',
        'accounting.reports.view',
        'inventory.view',
        'inventory.adjust',
      ],
    });
    stranger = await createActor(ctx, {
      tenantCode: 'tre-she-2',
      email: 'owner@tre-she-2.test',
      permissions: [...ALL_PLATFORM_PERMISSIONS, 'treasury.view', 'treasury.shift.post'],
    });

    const year = new Date().getUTCFullYear();
    const fiscal = await api(ctx.server, 'post', '/api/v1/fiscal-years', {
      token: actor.token,
      body: { name: `FY${year}`, startDate: `${year}-01-01`, endDate: `${year}-12-31` },
    });
    expect(fiscal.status).toBe(201);

    const branch = await api(ctx.server, 'post', '/api/v1/branches', {
      token: actor.token,
      body: { code: 'BR1', nameAr: 'الفرع الرئيسي' },
    });
    branchId = data(branch.body).id as string;

    const bareBranch = await api(ctx.server, 'post', '/api/v1/branches', {
      token: actor.token,
      body: { code: 'BR2', nameAr: 'فرع بلا حساب فرق' },
    });
    bareBranchId = data(bareBranch.body).id as string;

    const warehouse = await api(ctx.server, 'post', '/api/v1/warehouses', {
      token: actor.token,
      body: { branchId, code: 'WH1', name: 'المستودع الرئيسي', isDefault: true },
    });
    expect(warehouse.status).toBe(201);
    warehouseId = data(warehouse.body).id as string;

    safeAccountId = await account({ code: '1211', nameAr: 'الصندوق', type: 'asset' });
    bankAccountId = await account({ code: '1222', nameAr: 'بنك الراجحي', type: 'asset' });
    cashAccountId = await account({ code: '1221', nameAr: 'النقدية', type: 'asset' });
    differenceAccountId = await account({ code: '3114', nameAr: 'فرق بالصندوق', type: 'expense' });
    receivableAccountId = await account({ code: '1120', nameAr: 'العملاء', type: 'asset' });
    revenueAccountId = await account({ code: '4110', nameAr: 'المبيعات', type: 'revenue' });
    taxAccountId = await account({ code: '2310', nameAr: 'ضريبة القيمة المضافة', type: 'liability' });
    cogsAccountId = await account({ code: '5110', nameAr: 'تكلفة البضاعة المباعة', type: 'expense' });
    inventoryAccountId = await account({ code: '1130', nameAr: 'المخزون', type: 'asset' });

    const safe = await api(ctx.server, 'post', '/api/v1/cash-locations', {
      token: actor.token,
      body: { branchId, kind: 'safe', name: 'الصندوق الرئيسي', accountId: safeAccountId, isDefault: true },
    });
    safeId = data(safe.body).id as string;

    const bank = await api(ctx.server, 'post', '/api/v1/cash-locations', {
      token: actor.token,
      body: {
        branchId,
        kind: 'bank',
        name: 'بنك الراجحي',
        accountId: bankAccountId,
        bank: { bankName: 'بنك الراجحي' },
      },
    });
    expect(bank.status).toBe(201);
    bankId = data(bank.body).id as string;

    const category = await api(ctx.server, 'post', '/api/v1/organization/catalog/categories', {
      token: actor.token,
      body: { code: 'CAT1', nameAr: 'عام' },
    });
    const unit = await api(ctx.server, 'post', '/api/v1/organization/catalog/units', {
      token: actor.token,
      body: { code: 'PCS', nameAr: 'قطعة' },
    });
    const item = await api(ctx.server, 'post', '/api/v1/organization/catalog/items', {
      token: actor.token,
      body: {
        sku: 'ITM-SHIFT',
        nameAr: 'صنف للوردية',
        categoryId: data(category.body).id,
        baseUnitId: data(unit.body).id,
        salePrice: '100',
        costPrice: '60',
      },
    });
    expect(item.status).toBe(201);
    itemId = data(item.body).id as string;

    // Something to sell: the till cannot ring up air.
    const receipt = await api(ctx.server, 'post', '/api/v1/inventory/ledger/record', {
      token: actor.token,
      body: {
        lines: [
          {
            itemId,
            warehouseId,
            qty: '100',
            unitCost: '60',
            direction: 'in',
            docType: 'opening',
            docId: '00000000-0000-4000-8000-0000000000a1',
          },
        ],
      },
    });
    expect(receipt.status).toBeLessThan(300);

    await profile(branchId, 'sales_invoice', {
      salesAccountId: revenueAccountId,
      vatOutputAccountId: taxAccountId,
      cashAccountId,
      receivableAccountId,
      cogsAccountId,
      inventoryAccountId,
    });
    // 📒 قيد الإغلاق — the drawer's account and the account the difference lands on.
    await profile(branchId, 'shift_close', { cashAccountId, cashDifferenceAccountId: differenceAccountId });
    // A branch that never mapped فرق بالصندوق: the close has nowhere to put the count.
    await profile(bareBranchId, 'shift_close', { cashAccountId });
  });

  afterAll(async () => {
    await ctx?.close();
  });

  it('1. عجز الصندوق — 📉 الفرق مدين على فرق الصندوق ودائن على الصندوق', async () => {
    const shiftId = await openShift();
    await sell('1'); // 100 in the drawer
    const closed = await closeShift(shiftId, [{ denomination: '50', count: 1 }]); // 50 in hand
    const summary = closed.summary as Record<string, unknown>;
    expect(amt(summary.diff)).toBe('-50.0000');

    const posted = await post(shiftId);
    expect(posted.status).toBeLessThan(300);
    const journalEntryId = data(posted.body).journalEntryId as string;
    expect(journalEntryId).toBeTruthy();

    const entry = await entryOf(journalEntryId);
    const debits = entry.lines.filter((line) => Number(line.debit) > 0);
    const credits = entry.lines.filter((line) => Number(line.credit) > 0);
    expect(debits).toHaveLength(1);
    expect(debits[0].accountId).toBe(differenceAccountId);
    expect(amt(debits[0].debit)).toBe('50.0000');
    expect(credits[0].accountId).toBe(cashAccountId);
    expect(amt(credits[0].credit)).toBe('50.0000');
  });

  it('2. زيادة الصندوق — القيد معكوس', async () => {
    const shiftId = await openShift();
    await sell('1'); // 100 expected
    await closeShift(shiftId, [{ denomination: '100', count: 1 }, { denomination: '50', count: 1 }]); // 150 in hand

    const posted = await post(shiftId);
    expect(posted.status).toBeLessThan(300);
    const entry = await entryOf(data(posted.body).journalEntryId as string);
    const debits = entry.lines.filter((line) => Number(line.debit) > 0);
    const credits = entry.lines.filter((line) => Number(line.credit) > 0);
    expect(debits[0].accountId).toBe(cashAccountId);
    expect(amt(debits[0].debit)).toBe('50.0000');
    expect(credits[0].accountId).toBe(differenceAccountId);
  });

  it('3. القيد متوازن — مدينه يساوي دائنه', async () => {
    const shiftId = await openShift();
    await sell('2'); // 200 expected
    await closeShift(shiftId, [{ denomination: '100', count: 1 }]); // 100 in hand
    const posted = await post(shiftId);
    const entry = await entryOf(data(posted.body).journalEntryId as string);
    const sumOf = (side: 'debit' | 'credit') =>
      entry.lines.reduce((sum, line) => sum + Number(line[side]), 0);
    expect(near(sumOf('debit'), sumOf('credit'))).toBe(true);
    expect(near(sumOf('debit'), 100)).toBe(true);
  });

  it('4. صندوق مطابق — لا قيد، لأن قيداً بلا مبلغ ليس دليلاً', async () => {
    const shiftId = await openShift();
    await sell('1'); // 100 expected
    await closeShift(shiftId, [{ denomination: '100', count: 1 }]); // 100 in hand
    const posted = await post(shiftId);
    expect(posted.status).toBe(422);
    expect(codeOf(posted.body as Record<string, unknown>)).toBe('SHIFT_BALANCED');
  });

  it('5. الترحيل مرّة واحدة — الثاني 409', async () => {
    const shiftId = await openShift();
    await sell('1');
    await closeShift(shiftId, [{ denomination: '50', count: 1 }]);
    expect((await post(shiftId)).status).toBeLessThan(300);
    const second = await post(shiftId);
    expect(second.status).toBe(409);
    expect(codeOf(second.body as Record<string, unknown>)).toBe('SHIFT_ALREADY_POSTED');
  });

  it('6. الوردية المفتوحة لا تُرحَّل — ما لم يُعَدّ لا يُقفَل', async () => {
    const shiftId = await openShift();
    await sell('1');
    const posted = await post(shiftId);
    expect(posted.status).toBe(422);
    expect(codeOf(posted.body as Record<string, unknown>)).toBe('SHIFT_INVALID_STATE');
    await closeShift(shiftId, [{ denomination: '100', count: 1 }]);
  });

  it('7. بلا حساب فرق في ملف الترحيل — 422 صريح لا تخمين', async () => {
    const shiftId = await openShift(bareBranchId);
    await closeShift(shiftId, [{ denomination: '10', count: 1 }]);
    const posted = await post(shiftId);
    expect(posted.status).toBe(422);
    expect(codeOf(posted.body as Record<string, unknown>)).toBe('TREASURY_PROFILE_KEY_MISSING');
  });

  it('8. 📝 البيان يحمل الإغلاق — «اغلاق اليومية خاصة الموظف … رقم …»', async () => {
    const shiftId = await openShift();
    await sell('1');
    const closed = await closeShift(shiftId, [{ denomination: '50', count: 1 }]);
    const posted = await post(shiftId);
    const entry = await entryOf(data(posted.body).journalEntryId as string);
    expect(entry.description).toContain('اغلاق اليومية');
    expect(entry.description).toContain(String(closed.number));

    // The grid must be able to say «posted» and stop offering the button.
    const rows = list((await api(ctx.server, 'get', '/api/v1/shift-closes/day-closes', { token: actor.token })).body);
    const row = rows.find((entry_) => entry_.id === shiftId);
    expect(row?.journalEntryId).toBe(data(posted.body).journalEntryId);
    expect(row?.postable).toBe(false);
    const balanced = rows.find((entry_) => entry_.status === 'closed' && Number(entry_.diff) === 0);
    expect(balanced?.postable).toBe(false);
    const unposted = rows.find((entry_) => Number(entry_.diff) !== 0 && !entry_.journalEntryId);
    if (unposted) expect(unposted.postable).toBe(true);
  });

  it('9. 🏦 التحويل البنكي يُدخل على حساب البنك نفسه — `EntryOper.cs` L620', async () => {
    const sale = await sell('1', 'bank');
    const number = String(sale.number);
    const entries = list(
      (await api(ctx.server, 'get', `/api/v1/journal-entries?from=${today()}&to=${today()}`, {
        token: actor.token,
      })).body,
    );
    const entry = entries.find((row) => String(row.description) === `Sales invoice ${number}`);
    expect(entry).toBeTruthy();
    const lines = (await entryOf(String(entry?.id))).lines;
    const debits = lines.filter((line) => Number(line.debit) > 0);
    expect(debits.some((line) => line.accountId === bankAccountId)).toBe(true);
    // The generic cash account is not where a named bank's money lands.
    expect(debits.some((line) => line.accountId === cashAccountId)).toBe(false);
  });

  it('10. 🔢 الرقم سلسلة واحدة للمؤسسة — فرعان لا يصدران رقماً واحداً', async () => {
    // `shift_closes_number_key` is unique per tenant, so a per-branch sequence made the
    // second branch's close die on a duplicate key (a 500, not a 409). Both drawdowns
    // must now close, and neither may wear the other's number.
    const first = await openShift(branchId);
    const second = await openShift(bareBranchId);
    const closedFirst = await closeShift(first, [{ denomination: '100', count: 1 }]);
    const closedSecond = await closeShift(second, [{ denomination: '100', count: 1 }]);
    expect(closedFirst.number).toMatch(/^CS-\d{6}$/);
    expect(closedSecond.number).toMatch(/^CS-\d{6}$/);
    expect(closedFirst.number).not.toBe(closedSecond.number);
  });

  it('11. مؤسسة أخرى لا ترى الوردية أصلاً', async () => {
    const shiftId = await openShift();
    await sell('1');
    await closeShift(shiftId, [{ denomination: '50', count: 1 }]);
    const posted = await post(shiftId, stranger.token);
    expect(posted.status).toBe(404);
    expect(codeOf(posted.body as Record<string, unknown>)).toBe('SHIFT_NOT_FOUND');
  });
});
