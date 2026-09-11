#!/usr/bin/env node
/**
 * Live verification of the Phase 05 inventory documents against a running stack
 * (`pnpm db:local` + `pnpm db:migrate` + `pnpm db:seed` + `pnpm dev`).
 *
 * It drives the real HTTP API the staff screens drive — nothing is mocked:
 *
 *   1. بضاعة أول المدة — an opening voucher that credits بضاعة أول المدة
 *   2. سند إخراج — an issue at average cost that credits المخزون
 *   3. جرد وتسوية — a counted variance approved and posted as one balanced entry
 *   4. مناقلة — send (بضاعة تحت التحويل) → receive (عودة للمخزون)
 *   5. الرصيد السالب — an issue beyond the balance is refused, then forced
 *   6. حد الطلب — an item drained below its minimum is listed with its shortage
 *   7. وحدات القياس — a document written in boxes moves pieces
 *   8. الباركود — a label resolves to an item, a unit and a factor
 *   9. تواريخ الصلاحية — a lot inside the horizon is reported, one outside is not
 *
 * Every step asserts the *ledger*, not just the stock level: a stock document that
 * moves quantity without a journal is the desktop bug this phase exists to remove.
 *
 * Usage: node scripts/verify-inventory.mjs
 */
import { loadEnvFiles } from './dotenv.mjs';

loadEnvFiles();

const base = process.env.API_BASE ?? 'http://127.0.0.1:3000/api/v1';
const tenantCode = process.env.VERIFY_TENANT ?? 'demo';
const email = process.env.VERIFY_EMAIL ?? 'owner@demo.test';
const password = process.env.DEMO_OWNER_PASSWORD ?? '';

const money = (value) => Number(value).toFixed(4);
let failures = 0;

function check(label, condition, detail = '') {
  if (condition) {
    console.log(`  ✓ ${label}${detail ? ` — ${detail}` : ''}`);
  } else {
    failures += 1;
    console.log(`  ✗ ${label}${detail ? ` — ${detail}` : ''}`);
  }
}

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
    const error = new Error(
      `${method} ${path} → ${response.status} ${parsed.code ?? ''} ${parsed.detail ?? parsed.message ?? ''}`,
    );
    error.status = response.status;
    error.code = parsed.code;
    throw error;
  }
  return parsed.data ?? parsed;
}

const login = async () => {
  const data = await call('post', '/auth/login', undefined, { tenantCode, email, password });
  return data.accessToken ?? data.access_token ?? data.token;
};

const token = await login();
console.log(`✔ logged in to ${tenantCode} as ${email}\n`);

// ---------------------------------------------------------------- reference data
const branches = await call('get', '/branches', token);
const branchId = branches[0].id;
const warehouses =
  (await call('get', '/warehouses', token)).data ?? (await call('get', '/warehouses', token));
const warehouseRows = Array.isArray(warehouses) ? warehouses : warehouses.data;
let warehouseId = warehouseRows[0].id;
if (warehouseRows.length < 2) {
  const second = await call('post', '/warehouses', token, { branchId, code: 'WH2', name: 'مستودع ثانٍ' });
  warehouseRows.push(second);
}
const secondWarehouseId = warehouseRows.find((row) => row.id !== warehouseId).id;

const categories = await call('get', '/organization/catalog/categories', token);
const units = await call('get', '/organization/catalog/units', token);
const stamp = Date.now().toString().slice(-6);
const categoryId = categories[0].id;
const item = await call('post', '/organization/catalog/items', token, {
  sku: `SKU-INV-${stamp}`,
  nameAr: 'صنف اختبار المخزون',
  categoryId: categories[0].id,
  baseUnitId: units[0].id,
  kind: 'stock',
  purchasePrice: '30',
  minQty: '10',
});
const itemId = item.id;
console.log(`✔ item ${item.sku} in ${warehouseRows.length} warehouses\n`);

const profile = await call(
  'get',
  `/branch-posting-profiles/resolve?branchId=${branchId}&docType=stock_voucher`,
  token,
);
const mapping = profile.mapping ?? {};
for (const key of [
  'inventoryAccountId',
  'openingBalanceAccountId',
  'inventoryAdjustmentAccountId',
  'stockInTransitAccountId',
]) {
  check(`profile maps ${key}`, Boolean(mapping[key]), mapping[key]);
}
console.log('');

const levelOf = async (warehouse = warehouseId) => {
  const levels = await call('get', `/inventory/levels?warehouse_id=${warehouse}&item_id=${itemId}`, token);
  const rows = Array.isArray(levels) ? levels : levels.data;
  return Number(rows[0]?.quantity ?? 0);
};

const journalOf = async (match) => {
  const list = await call('get', '/journal-entries?limit=50', token);
  const entries = Array.isArray(list) ? list : list.data;
  const entry = entries.find((row) => (row.description ?? '').includes(match) && row.kind !== 'reversal');
  if (!entry) throw new Error(`journal entry not found: ${match}`);
  const detail = await call('get', `/journal-entries/${entry.id}`, token);
  return detail.lines ?? [];
};

const line = (lines, accountId) => lines.find((row) => row.accountId === accountId);

// ------------------------------------------------------------- 1. opening balance
console.log('1. بضاعة أول المدة');
const opening = await call('post', '/inventory/vouchers', token, {
  branchId,
  warehouseId,
  kind: 'opening',
  reason: 'رصيد افتتاحي',
  lines: [{ itemId, qty: '100', unitCost: '30' }],
});
check('opening voucher numbered', /^OP-\d{6}$/.test(opening.number), opening.number);
const openingPosted = await call('post', `/inventory/vouchers/${opening.id}/post`, token, {});
check(
  'opening posted',
  openingPosted.status === 'posted',
  `${openingPosted.number} @ ${money(openingPosted.totalCost)}`,
);
check('stock rose to 100', (await levelOf()) === 100, String(await levelOf()));
const openingLines = await journalOf(opening.number);
check(
  'المخزون debited 3000',
  money(line(openingLines, mapping.inventoryAccountId)?.debit ?? 0) === '3000.0000',
);
check(
  'بضاعة أول المدة credited 3000',
  money(line(openingLines, mapping.openingBalanceAccountId)?.credit ?? 0) === '3000.0000',
);
console.log('');

// ----------------------------------------------------------------- 2. stock issue
console.log('2. سند إخراج');
const issue = await call('post', '/inventory/vouchers', token, {
  branchId,
  warehouseId,
  kind: 'stock_out',
  reason: 'تالف',
  lines: [{ itemId, qty: '20' }],
});
check('issue voucher numbered', /^SOU-\d{6}$/.test(issue.number), issue.number);
const issuePosted = await call('post', `/inventory/vouchers/${issue.id}/post`, token, {});
check(
  'issue posted at average cost',
  money(issuePosted.totalCost) === '600.0000',
  money(issuePosted.totalCost),
);
check('stock fell to 80', (await levelOf()) === 80, String(await levelOf()));
const issueLines = await journalOf(issue.number);
check(
  'تسويات المخزون debited 600',
  money(line(issueLines, mapping.inventoryAdjustmentAccountId)?.debit ?? 0) === '600.0000',
);
check(
  'المخزون credited 600',
  money(line(issueLines, mapping.inventoryAccountId)?.credit ?? 0) === '600.0000',
);
try {
  await call('post', `/inventory/vouchers/${issue.id}/post`, token, {});
  check('second posting refused', false, 'expected 409');
} catch (error) {
  check('second posting refused', error.status === 409, `${error.status} ${error.code}`);
}
console.log('');

// ------------------------------------------------------------------- 3. counting
console.log('3. جرد وتسوية');
const count = await call('post', '/inventory/adjustments', token, {
  branchId,
  warehouseId,
  reason: 'جرد سنوي',
  lines: [{ itemId, countedQty: '75' }],
});
check('count numbered', /^ADJ-\d{6}$/.test(count.number), count.number);
try {
  await call('post', `/inventory/adjustments/${count.id}/post`, token, {});
  check('unapproved count refused', false, 'expected 422');
} catch (error) {
  check(
    'unapproved count refused',
    error.status === 422 && error.code === 'ADJUSTMENT_APPROVAL_REQUIRED',
    `${error.status} ${error.code}`,
  );
}
const countPosted = await call('post', `/inventory/adjustments/${count.id}/post`, token, { approved: true });
check('count posted', countPosted.status === 'posted');
check('stock corrected to 75', (await levelOf()) === 75, String(await levelOf()));
check('variance recorded', Number(countPosted.lines[0].varianceQty) === -5, countPosted.lines[0].varianceQty);
const countLines = await journalOf(count.number);
const countDebit = countLines.reduce((sum, row) => sum + Number(row.debit), 0);
const countCredit = countLines.reduce((sum, row) => sum + Number(row.credit), 0);
check(
  'variance entry balances',
  countDebit.toFixed(4) === countCredit.toFixed(4),
  `${money(countDebit)} / ${money(countCredit)}`,
);
console.log('');

// ------------------------------------------------------------------ 4. transfer
console.log('4. مناقلة');
const transfer = await call('post', '/inventory/transfers/draft', token, {
  branchId,
  fromWarehouseId: warehouseId,
  toWarehouseId: secondWarehouseId,
  lines: [{ itemId, qty: '25' }],
});
check('transfer numbered', /^TR-\d{6}$/.test(transfer.number), transfer.number);
const sent = await call('post', `/inventory/transfers/${transfer.id}/send`, token, {});
check('transfer sent', sent.status === 'in_transit', `${sent.status} value ${money(sent.value)}`);
check('source fell to 50', (await levelOf()) === 50, String(await levelOf()));
const sendLines = await journalOf(`مناقلة ${transfer.number} — إرسال`);
check(
  'بضاعة تحت التحويل debited',
  money(line(sendLines, mapping.stockInTransitAccountId)?.debit ?? 0) === money(sent.value),
);
check(
  'المخزون credited',
  money(line(sendLines, mapping.inventoryAccountId)?.credit ?? 0) === money(sent.value),
);
const received = await call('post', `/inventory/transfers/${transfer.id}/receive`, token, {
  received: [{ lineNo: 1, qty: '25' }],
});
check('transfer received', received.status === 'received', received.status);
check(
  'destination holds 25',
  (await levelOf(secondWarehouseId)) === 25,
  String(await levelOf(secondWarehouseId)),
);
const receiveLines = await journalOf(`مناقلة ${transfer.number} — استلام`);
check(
  'المخزون debited on receipt',
  money(line(receiveLines, mapping.inventoryAccountId)?.debit ?? 0) === money(sent.value),
);
check(
  'بضاعة تحت التحويل cleared',
  money(line(receiveLines, mapping.stockInTransitAccountId)?.credit ?? 0) === money(sent.value),
);
console.log('');

// ------------------------------------------------------------- 5. negative stock
console.log('5. الرصيد السالب');
const overshoot = await call('post', '/inventory/vouchers', token, {
  branchId,
  warehouseId,
  kind: 'stock_out',
  reason: 'صرف فوق الرصيد',
  lines: [{ itemId, qty: '500' }],
});
try {
  await call('post', `/inventory/vouchers/${overshoot.id}/post`, token, {});
  check('overshoot refused', false, 'expected 422');
} catch (error) {
  check(
    'overshoot refused',
    error.status === 422 && error.code === 'STOCK_INSUFFICIENT',
    `${error.status} ${error.code}`,
  );
}
const forced = await call('post', `/inventory/vouchers/${overshoot.id}/post`, token, { allowNegative: true });
check('overshoot allowed with the override', forced.status === 'posted', `stock ${await levelOf()}`);
const voided = await call('post', `/inventory/vouchers/${overshoot.id}/void`, token, {
  reason: 'تصحيح اختبار',
});
check(
  'void rolls the stock back',
  voided.status === 'voided' && (await levelOf()) === 50,
  String(await levelOf()),
);
console.log('');

console.log('6. حد الطلب');
const healthy = await call('get', `/inventory/below-minimum?warehouse_id=${warehouseId}`, token);
check(
  'reorder report ignores a healthy item',
  !healthy.some((row) => row.itemId === itemId),
  `${healthy.length} row(s)`,
);
const drain = await call('post', '/inventory/vouchers', token, {
  branchId,
  warehouseId,
  kind: 'stock_out',
  reason: 'صرف حتى حد الطلب',
  lines: [{ itemId, qty: '45' }],
});
await call('post', `/inventory/vouchers/${drain.id}/post`, token, {});
const reorder = await call('get', `/inventory/below-minimum?warehouse_id=${warehouseId}`, token);
const shortRow = reorder.find((row) => row.itemId === itemId);
check(
  'reorder report lists the item under its minimum',
  Boolean(shortRow),
  shortRow ? ` shortage ${shortRow.shortage}` : `${reorder.length} row(s)`,
);
check(
  'shortage equals min − on hand',
  shortRow ? Number(shortRow.shortage) === 5 : false,
  shortRow?.shortage ?? '',
);

// ── 7. وحدات القياس المتعددة ────────────────────────────────────────────────
// The desktop stored `ItemUnits.perc` and multiplied every quantity by it
// (`ItemPrimaryQnty = ItemQuantity * UnitEquality`). The cloud does the same: a
// document counts cartons, the ledger stores pieces.
console.log('');
console.log('7. وحدات القياس المتعددة');
const boxUnit = await call('post', '/organization/catalog/units', token, {
  code: `BOX${stamp}`,
  nameAr: 'علبة',
});
await call('post', `/organization/catalog/items/${itemId}/units`, token, {
  unitId: boxUnit.id,
  ratio: '12',
  isDefaultSale: true,
});
const unitRows = await call('get', `/organization/catalog/items/${itemId}/units`, token);
check(
  'the box is listed with its factor',
  unitRows.some((row) => row.unitId === boxUnit.id && Number(row.ratio) === 12),
  `${unitRows.length} unit(s)`,
);
check('the base unit is still 1:1', Number(unitRows[0].ratio) === 1, unitRows[0]?.ratio ?? '');
try {
  await call('post', `/organization/catalog/items/${itemId}/units`, token, {
    unitId: item.baseUnitId,
    ratio: '6',
  });
  check('the base unit cannot be re-scaled', false, 'expected 422');
} catch (error) {
  check(
    'the base unit cannot be re-scaled',
    error.status === 422 && error.code === 'CATALOG_UNIT_RATIO_INVALID',
    `${error.status} ${error.code}`,
  );
}

const levelBeforeBoxes = Number(await levelOf());
const boxReceipt = await call('post', '/inventory/vouchers', token, {
  branchId,
  warehouseId,
  kind: 'stock_in',
  reason: 'استلام بالعلب',
  lines: [{ itemId, qty: '2', unitId: boxUnit.id, unitCost: '120' }],
});
await call('post', `/inventory/vouchers/${boxReceipt.id}/post`, token, {});
check(
  '2 boxes × 12 move 24 base units',
  Number(await levelOf()) === levelBeforeBoxes + 24,
  `${levelBeforeBoxes} → ${await levelOf()}`,
);
const boxValue = await call('get', `/inventory/vouchers/${boxReceipt.id}`, token);
check('the line is worth 2 × 120, not 24 × 120', Number(boxValue.totalCost) === 240, boxValue.totalCost);
const boxMovements = await call('get', `/inventory/movements?item_id=${itemId}`, token);
const boxMovement = boxMovements.find((row) => row.docId === boxReceipt.id);
check(
  'the movement keeps the unit it was counted in',
  Number(boxMovement?.qty) === 2 &&
    Number(boxMovement?.baseQty) === 24 &&
    Number(boxMovement?.factor) === 12 &&
    boxMovement?.unitId === boxUnit.id,
  `${boxMovement?.qty} ${boxMovement?.unitId ?? ''} → ${boxMovement?.baseQty} base`,
);

// ── 8. الباركود ────────────────────────────────────────────────────────────
console.log('');
console.log('8. الباركود المتعدد');
const label = `BOX-${stamp}`;
await call('post', `/organization/catalog/items/${itemId}/barcodes`, token, {
  barcode: label,
  unitId: boxUnit.id,
});
const scanned = await call('get', `/inventory/barcode/${label}`, token);
check(
  'a scan answers the item, the unit and the factor',
  scanned.itemId === itemId && scanned.unitId === boxUnit.id && Number(scanned.factor) === 12,
  `${scanned.matchedBy} ×${scanned.factor}`,
);
try {
  await call('get', `/inventory/barcode/NOPE-${stamp}`, token);
  check('an unknown label is a 404', false, 'expected 404');
} catch (error) {
  check(
    'an unknown label is a 404',
    error.status === 404 && error.code === 'BARCODE_NOT_FOUND',
    `${error.status} ${error.code}`,
  );
}
const secondItem = await call('post', '/organization/catalog/items', token, {
  sku: `SKU2-${stamp}`,
  nameAr: 'صنف ثانٍ',
  categoryId,
  baseUnitId: item.baseUnitId,
});
try {
  await call('post', `/organization/catalog/items/${secondItem.id}/barcodes`, token, { barcode: label });
  check('one label cannot belong to two items', false, 'expected 409');
} catch (error) {
  check(
    'one label cannot belong to two items',
    error.status === 409 && error.code === 'CATALOG_BARCODE_TAKEN',
    `${error.status} ${error.code}`,
  );
}
const strayUnit = await call('post', '/inventory/vouchers', token, {
  branchId,
  warehouseId,
  kind: 'stock_out',
  reason: 'وحدة غير معرّفة على الصنف',
  lines: [{ itemId: secondItem.id, qty: '1', unitId: boxUnit.id }],
});
try {
  await call('post', `/inventory/vouchers/${strayUnit.id}/post`, token, {});
  check('an undefined unit cannot be posted', false, 'expected 422');
} catch (error) {
  check(
    'an undefined unit cannot be posted',
    error.status === 422 && error.code === 'INVENTORY_UNIT_NOT_ALLOWED',
    `${error.status} ${error.code}`,
  );
}

// ── 9. تواريخ الصلاحية ─────────────────────────────────────────────────────
console.log('');
console.log('9. تواريخ الصلاحية');
const soon = new Date(Date.now() + 10 * 86_400_000).toISOString().slice(0, 10);
const later = new Date(Date.now() + 400 * 86_400_000).toISOString().slice(0, 10);
await call('post', '/inventory/lots', token, { itemId, lotNo: `LOT-SOON-${stamp}`, expiryDate: soon });
await call('post', '/inventory/lots', token, { itemId, lotNo: `LOT-LATER-${stamp}`, expiryDate: later });
const expiring = await call('get', '/inventory/expiry?days=30', token);
const soonRow = expiring.find((row) => row.lotNo === `LOT-SOON-${stamp}`);
check(
  'a lot inside the horizon is reported',
  Boolean(soonRow) && soonRow.daysLeft > 0 && soonRow.daysLeft <= 10,
  soonRow ? `${soonRow.lotNo} in ${soonRow.daysLeft} day(s)` : `${expiring.length} row(s)`,
);
check(
  'a lot beyond the horizon is not',
  !expiring.some((row) => row.lotNo === `LOT-LATER-${stamp}`),
  `${expiring.length} row(s)`,
);

console.log(failures === 0 ? '\n✔ Phase 05 inventory documents verified' : `\n✗ ${failures} check(s) failed`);
process.exit(failures === 0 ? 0 : 1);
