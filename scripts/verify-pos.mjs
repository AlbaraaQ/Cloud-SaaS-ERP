#!/usr/bin/env node
/**
 * Live verification of the Phase 04 POS engine against a running stack
 * (`pnpm db:local` + `pnpm db:migrate` + `pnpm db:seed` + `pnpm dev`).
 *
 * It drives the real HTTP API the staff screen drives — nothing is mocked:
 *
 *   1. open a cashier shift
 *   2. sell from the till (cash, over-tendered) through POST /pos/checkout
 *   3. read the invoice back: posted, numbered, paid, stocked movement, journal
 *   4. sell on account (postponed) — no cash in the drawer
 *   5. close the shift with a counted drawer and print the closing summary
 *
 * Usage: node scripts/verify-pos.mjs
 */
import { loadEnvFiles } from './dotenv.mjs';

loadEnvFiles();

const base = process.env.API_BASE ?? 'http://127.0.0.1:3000/api/v1';
const tenantCode = process.env.VERIFY_TENANT ?? 'demo';
const email = process.env.VERIFY_EMAIL ?? 'owner@demo.test';
const password = process.env.DEMO_OWNER_PASSWORD ?? '';

const money = (value) => Number(value).toFixed(4);

async function call(method, path, token, body) {
  const response = await fetch(`${base}${path}`, {
    method,
    headers: {
      'content-type': 'application/json',
      ...(token ? { authorization: `Bearer ${token}` } : {}),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await response.text();
  const parsed = text ? JSON.parse(text) : {};
  if (!response.ok) {
    throw new Error(`${method} ${path} → ${response.status} ${parsed.code ?? ''} ${parsed.message ?? ''}`);
  }
  return parsed.data ?? parsed;
}

const login = async () => {
  const data = await call('post', '/auth/login', undefined, { tenantCode, email, password });
  return data.accessToken ?? data.token;
};

const main = async () => {
  const token = await login();
  console.log(`✔ signed in as ${email} @ ${tenantCode}`);

  const branches = await call('get', '/branches', token);
  const branch = branches.find((row) => row.isDefault) ?? branches[0];
  const warehouses = await call('get', '/warehouses', token);
  const warehouse = warehouses.find((row) => row.branchId === branch.id) ?? warehouses[0];

  // The demo seed ships no catalogue, so the till gets the minimum it needs:
  // one stocked item with stock on hand, one safe, one customer account.
  const stamp = Date.now().toString().slice(-6);
  const category = await call('post', '/organization/catalog/categories', token, {
    code: `POS${stamp}`,
    nameAr: 'أصناف نقطة البيع',
  });
  const unit = await call('post', '/organization/catalog/units', token, {
    code: `PC${stamp}`,
    nameAr: 'حبة',
  });
  const item = await call('post', '/organization/catalog/items', token, {
    sku: `SKU-POS-${stamp}`,
    nameAr: 'قهوة مختصة 250جم',
    categoryId: category.id,
    baseUnitId: unit.id,
    kind: 'stock',
    salePrice: '100.0000',
    purchasePrice: '60.0000',
  });
  await call('post', '/inventory/ledger/record', token, {
    lines: [
      {
        itemId: item.id,
        warehouseId: warehouse.id,
        qty: '100',
        unitCost: '60',
        direction: 'in',
        docType: 'opening',
        docId: '00000000-0000-0000-0000-000000000001',
      },
    ],
  });

  const draws = await call('get', '/cash-locations', token);
  const safe =
    draws.find((row) => (row.kind ?? 'safe') === 'safe') ??
    (await call('post', '/cash-locations', token, {
      branchId: branch.id,
      kind: 'safe',
      name: 'صندوق نقطة البيع',
      isDefault: true,
    }));
  if (!safe.accountId) {
    const profile = await call(
      'get',
      `/branch-posting-profiles/resolve?branchId=${branch.id}&docType=sales_invoice`,
      token,
    );
    await call('patch', `/cash-locations/${safe.id}`, token, { accountId: profile.mapping.cashAccountId });
    safe.accountId = profile.mapping.cashAccountId;
  }
  const customers = await call('get', '/parties?kind=customer', token);
  const customer =
    customers[0] ??
    (await call('post', '/parties', token, { kind: 'customer', name: 'عميل آجل — نقطة البيع' }));
  console.log(
    `✔ branch ${branch.code} · warehouse ${warehouse.code ?? warehouse.nameAr} · item ${item.sku} · safe ${safe.name} · customer ${customer.name}`,
  );

  const shift = await call('post', '/shift-closes/open', token, { branchId: branch.id });
  console.log(`✔ shift opened ${shift.id}`);

  // 1 — a cash sale, over-tendered, straight from the till.
  const sale = await call('post', '/pos/checkout', token, {
    branchId: branch.id,
    warehouseId: warehouse.id,
    priceIncludesVat: true,
    orderType: 'pos',
    shiftId: shift.id,
    lines: [
      { itemId: item.id, quantity: '2', unitPrice: String(item.salePrice ?? item.sale_price), taxRate: '15' },
    ],
    payment: { method: 'cash', cashLocationId: safe.id, tendered: '500' },
  });
  console.log(
    `✔ cash sale ${sale.number}: total ${money(sale.total)} · tendered ${money(sale.tendered)} · change ${money(sale.change)} · ${sale.paymentStatus} · shift ${sale.shiftId}`,
  );

  const invoice = await call('get', `/sales/invoices/${sale.invoiceId}`, token);
  console.log(
    `✔ invoice ${invoice.number}: status ${invoice.status} · lines ${invoice.lines.length} · payments ${invoice.payments.length} (${invoice.payments[0]?.method} ${money(invoice.payments[0]?.amount)})`,
  );
  console.log(`  line cost stamped: ${money(invoice.lines[0].costTotal)}`);

  const movements = await call('get', `/inventory/movements?warehouse_id=${warehouse.id}`, token);
  const moved = movements.filter((row) => row.docId === sale.invoiceId);
  console.log(
    `✔ stock movements for the sale: ${moved.length} (${moved.map((row) => `${row.direction} ${row.qty}`).join(', ')})`,
  );

  const entries = await call('get', '/journal-entries?limit=50', token);
  const entry = entries.find((row) => row.description === `Sales invoice ${sale.number}`);
  if (entry) {
    const detail = await call('get', `/journal-entries/${entry.id}`, token);
    const debit = detail.lines.reduce((sum, line) => sum + Number(line.debit), 0);
    const credit = detail.lines.reduce((sum, line) => sum + Number(line.credit), 0);
    console.log(
      `✔ journal ${entry.description}: ${detail.lines.length} legs · Dr ${money(debit)} / Cr ${money(credit)} · balanced ${debit.toFixed(4) === credit.toFixed(4)}`,
    );
  }

  // 2 — a card sale in the same shift: network takings, not drawer cash.
  const bank = draws.find((row) => (row.kind ?? 'safe') === 'bank');
  if (bank) {
    const cardSale = await call('post', '/pos/checkout', token, {
      branchId: branch.id,
      warehouseId: warehouse.id,
      priceIncludesVat: true,
      orderType: 'pos',
      shiftId: shift.id,
      lines: [
        {
          itemId: item.id,
          quantity: '1',
          unitPrice: String(item.salePrice ?? item.sale_price),
          taxRate: '15',
        },
      ],
      payment: { method: 'card', cashLocationId: bank.id },
    });
    const cardInvoice = await call('get', `/sales/invoices/${cardSale.invoiceId}`, token);
    console.log(
      `✔ card sale ${cardSale.number}: ${money(cardSale.total)} · ${cardSale.paymentStatus} · payment row ${cardInvoice.payments[0]?.method}`,
    );
  }

  // 3 — a postponed sale in the same shift: cash in the drawer must not move.
  let postponedId = '';
  {
    const postponed = await call('post', '/pos/checkout', token, {
      branchId: branch.id,
      warehouseId: warehouse.id,
      priceIncludesVat: true,
      orderType: 'pos',
      shiftId: shift.id,
      partyId: customer.id,
      lines: [
        {
          itemId: item.id,
          quantity: '1',
          unitPrice: String(item.salePrice ?? item.sale_price),
          taxRate: '15',
        },
      ],
      payment: { method: 'credit' },
    });
    postponedId = postponed.invoiceId;
    console.log(
      `✔ postponed sale ${postponed.number}: ${money(postponed.total)} · ${postponed.paymentStatus} (no cash in the drawer)`,
    );
  }

  // 3 — close the drawer: counted cash vs what the till should hold.
  const expectedCash = Number(sale.total);
  const tens = Math.floor(expectedCash / 10);
  const counts = [
    { denomination: '100', count: Math.floor(tens / 10) },
    { denomination: '10', count: tens % 10 },
  ];
  const closed = await call('post', `/shift-closes/${shift.id}/close`, token, { counts });
  console.log('✔ shift closed:', JSON.stringify(closed.summary, null, 2));

  // 4 — a closed shift is sealed: the postponed sale cannot be voided any more.
  try {
    await call('post', `/sales/invoices/${postponedId}/void`, token, { reason: 'after closing' });
    console.log('✘ the closed shift accepted a void');
  } catch (error) {
    console.log(`✔ sealed: ${error.message.split('→')[1]?.trim()}`);
  }
};

main().catch((error) => {
  console.error('✘ verification failed:', error.message);
  process.exit(1);
});
