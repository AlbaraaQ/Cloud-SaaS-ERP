#!/usr/bin/env node
/**
 * Live verification of the Phase 06 treasury documents against a running stack
 * (`pnpm db:local` + `pnpm db:migrate` + `pnpm db:seed` + `pnpm dev`).
 *
 * It drives the real HTTP API the staff screens drive — nothing is mocked:
 *
 *   1. سند قبض عميل — البيان والوقت والمندوب على السطر، ثم القيد
 *   2. سند صرف لمورد — القيد مقلوب، والبنك هو الدائن
 *   3. الرقم — الترقيم من سلسلة المستندات لا من المستخدم
 *   4. شيك — لا يلمس رصيد الصندوق حتى يُحصَّل، ويُقيَّد يوم التحصيل
 *   5. شيك مرتجع — يعيد الدين على العميل
 *   6. 🔍 البحث — بالتاريخ، بالرقم، وبالبيان
 *   7. المسودة تُعدَّل والمرحَّل لا يُعدَّل
 *
 * Every step asserts the *ledger*, not just the balance: the desktop turned a receipt
 * into an entry as it saved it (`Class/ReceiptOper.cs:21`), and a voucher that moves cash
 * without an entry is exactly the bug this phase exists to remove.
 *
 * Usage: node scripts/verify-treasury.mjs
 */
import { loadEnvFiles } from './dotenv.mjs';

loadEnvFiles();

const base = process.env.API_BASE ?? 'http://127.0.0.1:3000/api/v1';
const tenantCode = process.env.VERIFY_TENANT ?? 'demo';
const email = process.env.VERIFY_EMAIL ?? 'owner@demo.test';
const password = process.env.DEMO_OWNER_PASSWORD ?? '';

const money = (value) => Number(value).toFixed(2);
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
const stamp = Date.now().toString().slice(-6);
const today = new Date().toISOString().slice(0, 10);
const year = new Date().getUTCFullYear();

const branches = await call('get', '/branches', token);
const branchId = branches[0].id;

const account = async (code, nameAr, type) =>
  (await call('post', '/accounts', token, { code: `${code}${stamp}`, nameAr, type })).id;
const receivableAccountId = await account('1120', 'العملاء — تحقق', 'asset');
const payableAccountId = await account('2110', 'الموردون — تحقق', 'liability');
const safeAccountId = await account('1211', 'الصندوق — تحقق', 'asset');
const bankAccountId = await account('1221', 'البنك — تحقق', 'asset');
const chequesInHandAccountId = await account('1130', 'أوراق القبض — تحقق', 'asset');

const customer = await call('post', '/parties', token, {
  code: `C-${stamp}`,
  kind: 'customer',
  name: 'عميل التحقق',
  receivableAccountId,
});
const supplier = await call('post', '/parties', token, {
  code: `S-${stamp}`,
  kind: 'supplier',
  name: 'مورد التحقق',
  payableAccountId,
});

const employee = await call('post', '/hrm/employees', token, {
  employeeNo: `E-${stamp}`,
  name: 'مندوب التحقق',
  branchId,
});

// 📋 حالة الصندوق — the safe is an account first (`Class/Treasury.cs:17`).
const safe = await call('post', '/cash-locations', token, {
  branchId,
  kind: 'safe',
  name: `صندوق التحقق ${stamp}`,
  accountId: safeAccountId,
  isDefault: true,
});
const bank = await call('post', '/cash-locations', token, {
  branchId,
  kind: 'bank',
  name: `بنك التحقق ${stamp}`,
  accountId: bankAccountId,
  isDefault: true,
  bank: { bankName: `بنك التحقق ${stamp}`, iban: 'SA0380000000608010167519', accountNo: '608010167519' },
});
console.log(`✔ party, employee, safe and bank ready (${stamp})\n`);

await call('post', '/branch-posting-profiles', token, {
  branchId,
  docType: 'receipt_voucher',
  mapping: { version: 1, chequesInHandAccountId },
});
await call('post', '/branch-posting-profiles', token, {
  branchId,
  docType: 'payment_voucher',
  mapping: { version: 1, chequesInHandAccountId },
});
console.log('✔ أوراق القبض mapped in the posting profile\n');

const balanceOf = async (locationId) => {
  const rows = await call('get', `/cash-locations/${locationId}/balances`, token);
  const list = Array.isArray(rows) ? rows : rows.data ?? [];
  return list.reduce((sum, row) => sum + Number(row.balance ?? 0), 0);
};
const linesOf = async (entryId) => {
  const entry = await call('get', `/journal-entries/${entryId}`, token);
  return entry.lines ?? [];
};

// ── 1. سند قبض عميل ────────────────────────────────────────────────────────
console.log('1. سند قبض عميل');
const receipt = await call('post', '/vouchers', token, {
  branchId,
  kind: 'receipt',
  subtype: 'customer',
  date: today,
  voucherTime: '9:05',
  partyId: customer.id,
  cashLocationId: safe.id,
  method: 'cash',
  amount: '1500',
  description: 'تحصيل فاتورة 2401',
  salesmanId: employee.id,
  referenceNo: `INV-${stamp}`,
  referenceDate: today,
});
check('the voucher keeps its البيان', receipt.description === 'تحصيل فاتورة 2401', receipt.description);
check('⏰ الوقت is kept as a time, not a string', String(receipt.voucherTime).startsWith('09:05'), receipt.voucherTime);
check('👔 المندوب rides on the document', receipt.salesmanId === employee.id);

const beforeBalance = await balanceOf(safe.id);
const postedReceipt = await call('post', `/vouchers/${receipt.id}/post`, token, {});
check('the number comes from the document sequence', String(postedReceipt.number).startsWith('RV-'), postedReceipt.number);
check('posting writes a journal entry', Boolean(postedReceipt.journalEntryId), postedReceipt.journalEntryId);

const receiptLines = await linesOf(postedReceipt.journalEntryId);
const debited = receiptLines.find((line) => Number(line.debit) > 0);
const credited = receiptLines.find((line) => Number(line.credit) > 0);
check('مدين الصندوق — the safe’s own account', debited?.accountId === safeAccountId, debited?.accountId);
check('دائن حساب العميل', credited?.accountId === receivableAccountId, credited?.accountId);
check('the entry is balanced', money(debited?.debit) === money(credited?.credit), `${debited?.debit} / ${credited?.credit}`);
check(
  '📝 البيان is the entry’s narration — BindReceiptToEntry’s entry.Note',
  (await call('get', `/journal-entries/${postedReceipt.journalEntryId}`, token)).description ===
    'تحصيل فاتورة 2401',
);
const afterBalance = await balanceOf(safe.id);
check('the safe’s balance moved with the entry', money(afterBalance - beforeBalance) === money(1500), `${money(beforeBalance)} → ${money(afterBalance)}`);
console.log('');

// ── 2. سند صرف لمورد ───────────────────────────────────────────────────────
console.log('2. سند صرف لمورد');
const payment = await call('post', '/vouchers', token, {
  branchId,
  kind: 'payment',
  subtype: 'supplier',
  date: today,
  partyId: supplier.id,
  cashLocationId: bank.id,
  method: 'bank_transfer',
  amount: '800',
  description: 'سداد للمورد',
});
const postedPayment = await call('post', `/vouchers/${payment.id}/post`, token, {});
check('the payment is numbered from its own series', String(postedPayment.number).startsWith('PV-'), postedPayment.number);
const paymentLines = await linesOf(postedPayment.journalEntryId);
const paymentDebit = paymentLines.find((line) => Number(line.debit) > 0);
const paymentCredit = paymentLines.find((line) => Number(line.credit) > 0);
check('مدين المورد — the mirror of a receipt', paymentDebit?.accountId === payableAccountId);
check('دائن البنك', paymentCredit?.accountId === bankAccountId);
console.log('');

// ── 3. شيك ─────────────────────────────────────────────────────────────────
console.log('3. شيك — وعد لا نقد حتى يُحصَّل');
const cheque = await call('post', '/vouchers', token, {
  branchId,
  kind: 'receipt',
  subtype: 'customer',
  date: today,
  partyId: customer.id,
  cashLocationId: safe.id,
  method: 'cheque',
  chequeNo: `CHK-${stamp}`,
  chequeDate: today,
  amount: '5000',
  description: 'شيك من العميل',
});
const postedCheque = await call('post', `/vouchers/${cheque.id}/post`, token, {});
const pendingBalance = await balanceOf(safe.id);
const chequeLines = await linesOf(postedCheque.journalEntryId);
check(
  'أوراق القبض holds it, not the safe',
  chequeLines.find((line) => Number(line.debit) > 0)?.accountId === chequesInHandAccountId,
);

const cleared = await call('post', `/vouchers/${cheque.id}/cheque`, token, { action: 'clear' });
check('the cheque is محصّل', cleared.chequeState === 'cleared', cleared.chequeState);
const clearedBalance = await balanceOf(safe.id);
check(
  'the safe only moves when the bank honours it',
  money(clearedBalance - pendingBalance) === money(5000),
  `${money(pendingBalance)} → ${money(clearedBalance)}`,
);
const clearedEntries = await call('get', '/journal-entries', token);
const clearedList = (Array.isArray(clearedEntries) ? clearedEntries : clearedEntries.data ?? []).filter(
  (row) => row.sourceType === 'voucher_cheque' && String(row.description ?? '').includes(`CHK-${stamp}`),
);
check('and the clearance is an entry, not just a number', clearedList.length === 1, `${clearedList.length} entry`);
console.log('');

// ── 4. شيك مرتجع ───────────────────────────────────────────────────────────
console.log('4. شيك مرتجع');
const bounced = await call('post', '/vouchers', token, {
  branchId,
  kind: 'receipt',
  subtype: 'customer',
  date: today,
  partyId: customer.id,
  cashLocationId: safe.id,
  method: 'cheque',
  chequeNo: `CHK-B-${stamp}`,
  amount: '1200',
  description: 'شيك مرتجع',
});
await call('post', `/vouchers/${bounced.id}/post`, token, {});
const beforeBounce = await balanceOf(safe.id);
const bouncedState = await call('post', `/vouchers/${bounced.id}/cheque`, token, { action: 'bounce' });
check('the cheque is مرتجع', bouncedState.chequeState === 'bounced', bouncedState.chequeState);
check('a bounced cheque never touched the safe', money((await balanceOf(safe.id)) - beforeBounce) === money(0));
try {
  await call('post', `/vouchers/${bounced.id}/cheque`, token, { action: 'clear' });
  check('a terminal cheque cannot be cleared', false, 'expected 422');
} catch (error) {
  check('a terminal cheque cannot be cleared', error.status === 422 && error.code === 'CHEQUE_INVALID_STATE', `${error.status} ${error.code}`);
}
console.log('');

// ── 5. 🔍 البحث ────────────────────────────────────────────────────────────
console.log('5. 🔍 البحث');
const byCheque = await call('get', `/vouchers?q=CHK-${stamp}`, token);
const byChequeRows = Array.isArray(byCheque) ? byCheque : byCheque.data ?? [];
check('by رقم الشيك', byChequeRows.length === 1, `${byChequeRows.length} found`);
const byDescription = await call('get', '/vouchers?q=تحصيل', token);
const byDescriptionRows = Array.isArray(byDescription) ? byDescription : byDescription.data ?? [];
check('by البيان', byDescriptionRows.length > 0, `${byDescriptionRows.length} found`);
const byDate = await call('get', `/vouchers?from=${today}&to=${today}`, token);
const byDateRows = Array.isArray(byDate) ? byDate : byDate.data ?? [];
check(
  'by 📅 من تاريخ / إلى تاريخ',
  byDateRows.every((row) => String(row.date).slice(0, 10) === today),
  `${byDateRows.length} rows`,
);
console.log('');

// ── 6. المسودة تُعدَّل والمرحَّل لا يُعدَّل ─────────────────────────────────────
console.log('6. المسودة تُعدَّل والمرحَّل لا يُعدَّل');
const draft = await call('post', '/vouchers', token, {
  branchId,
  kind: 'receipt',
  subtype: 'customer',
  date: today,
  partyId: customer.id,
  cashLocationId: safe.id,
  method: 'cash',
  amount: '100',
  description: 'مسودة',
});
const patched = await call('PATCH', `/vouchers/${draft.id}`, token, {
  amount: '250',
  description: 'مبلغ مصحح',
  voucherTime: '14:30',
});
check('a draft can be corrected after saving', money(patched.amount) === money(250), patched.amount);
check('and its البيان with it', patched.description === 'مبلغ مصحح');
await call('post', `/vouchers/${draft.id}/post`, token, {});
try {
  await call('PATCH', `/vouchers/${draft.id}`, token, { amount: '999' });
  check('a posted voucher is sealed', false, 'expected 409');
} catch (error) {
  check('a posted voucher is sealed', error.status === 409 && error.code === 'VOUCHER_IMMUTABLE', `${error.status} ${error.code}`);
}
console.log('');

console.log(failures === 0 ? '\n✔ Phase 06 treasury documents verified' : `\n✗ ${failures} check(s) failed`);
process.exit(failures === 0 ? 0 : 1);
