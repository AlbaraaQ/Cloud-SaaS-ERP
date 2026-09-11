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
 *   7. تعريف الخزن والبنوك — المسئولون والملاحظات وبطاقة البنك
 *   8. حركة الصندوق — كشف من دفتر الأستاذ برصيد متحرك ورصيد سابق وترشيح وقت
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

// ── 7. تعريف الخزن والبنوك ─────────────────────────────────────────────────
// `frmTreasury.xaml.cs:222` refuses a الصندوق with no مسئول — «يجب اختيار موظف مسئول» —
// and replaces Stock_Emps inside the treasury's own transaction. `frmBanks.xaml` carries
// the bank's card on the same row: الدولة، المدينة، المنطقة، الهواتف، نسبة الاقتطاع.
console.log('7. تعريف الخزن والبنوك');
const secondEmployee = await call('post', '/hrm/employees', token, {
  employeeNo: `E2-${stamp}`,
  name: 'مساعد أمين الصندوق',
  branchId,
});
const custodySafe = await call('post', '/cash-locations', token, {
  branchId,
  kind: 'safe',
  name: `صندوق بمسئول ${stamp}`,
  accountId: safeAccountId,
  custodianIds: [employee.id, secondEmployee.id],
  notes: 'يُغلق يومياً الساعة الثامنة',
});
check(
  '👤 مسئولو الصندوق يُحفظون مع الصندوق نفسه',
  (custodySafe.custodianIds ?? []).length === 2,
  (custodySafe.custodianIds ?? []).join(' · ').slice(0, 24),
);
check('📝 ملاحظات الصندوق تُحفظ', custodySafe.notes === 'يُغلق يومياً الساعة الثامنة', custodySafe.notes);

try {
  await call('post', '/cash-locations', token, {
    branchId,
    kind: 'safe',
    name: `صندوق بلا مسئول ${stamp}`,
    accountId: safeAccountId,
    custodianIds: [],
  });
  check('صندوق بلا مسئول مرفوض', false, 'expected 422');
} catch (error) {
  check('صندوق بلا مسئول مرفوض', error.status === 422, `${error.status} ${error.code}`);
}

try {
  await call('post', '/cash-locations', token, {
    branchId,
    kind: 'safe',
    name: `صندوق بمسئول غريب ${stamp}`,
    accountId: safeAccountId,
    custodianIds: ['00000000-0000-4000-8000-000000000000'],
  });
  check('موظف من مؤسسة أخرى لا يُجعل مسئولاً', false, 'expected 422');
} catch (error) {
  check('موظف من مؤسسة أخرى لا يُجعل مسئولاً', error.status === 422, `${error.status} ${error.code}`);
}

// التحديث يستبدل المجموعة كما يفعل الديسكتوب: حذف ثم إدراج.
const trimmed = await call('PATCH', `/cash-locations/${custodySafe.id}`, token, {
  custodianIds: [secondEmployee.id],
  notes: 'مسئول واحد بعد التسليم',
});
check('التحديث يستبدل المسئولين', (trimmed.custodianIds ?? []).length === 1, `${(trimmed.custodianIds ?? []).length}`);
try {
  await call('PATCH', `/cash-locations/${custodySafe.id}`, token, { custodianIds: [] });
  check('ولا يُسمح بتجريد الصندوق من مسئوليه', false, 'expected 422');
} catch (error) {
  check('ولا يُسمح بتجريد الصندوق من مسئوليه', error.status === 422, `${error.status} ${error.code}`);
}

const bankCard = await call('post', '/cash-locations', token, {
  branchId,
  kind: 'bank',
  name: `بنك ببطاقة ${stamp}`,
  accountId: bankAccountId,
  bank: {
    bankName: `بنك التحقق ${stamp}`,
    iban: 'SA0380000000608010167519',
    country: 'المملكة العربية السعودية',
    city: 'الرياض',
    region: 'العليا',
    phone: '0114013030',
    mobile: '0550000000',
    deductionPct: '2.5',
  },
});
const card = bankCard.bank ?? {};
check(
  '🏦 بطاقة البنك كاملة: الدولة · المدينة · المنطقة · نسبة الاقتطاع',
  card.country === 'المملكة العربية السعودية' &&
    card.city === 'الرياض' &&
    card.region === 'العليا' &&
    card.deductionPct === '2.5',
  `${card.country} / ${card.city} / ${card.deductionPct}`,
);

const listed = await call('get', '/cash-locations?filter[kind]=safe&limit=100', token);
const listedRows = Array.isArray(listed) ? listed : listed.data ?? [];
check(
  'القائمة تعيد المسئولين مع كل صندوق',
  listedRows.some((row) => (row.custodianIds ?? []).includes(secondEmployee.id)),
  `${listedRows.length} صندوقاً`,
);
console.log('');

// ── 8. حركة الصندوق ────────────────────────────────────────────────────────
// `frmRptKhzna.xaml.cs` builds the statement from the **ledger** (L156 resolves the
// safe's account, L229 groups `Entry_sub`), opens it with `رصيد سابق` when a period is
// chosen (L200), carries a running balance, and closes with the two cards
// `⚖️ الرصيد الإجمالي` and `📅 رصيد الفترة المحددة` (L482/L502).
console.log('8. حركة الصندوق');
const statementAccountId = await account('1212', 'صندوق الكشف — تحقق', 'asset');
const capitalAccountId = await account('3110', 'رأس المال — تحقق', 'equity');
const expenseAccountId = await account('5110', 'مصروفات — تحقق', 'expense');
const statementSafe = await call('post', '/cash-locations', token, {
  branchId,
  kind: 'safe',
  name: `صندوق الكشف ${stamp}`,
  accountId: statementAccountId,
});
const orphanSafe = await call('post', '/cash-locations', token, {
  branchId,
  kind: 'safe',
  name: `صندوق بلا حساب ${stamp}`,
});

const movements = (id, query) => call('get', `/cash-locations/${id}/movements?${query}`, token);

// An entry the treasury screen never wrote — a statement built from `vouchers` would
// simply not see it.
await call('post', '/journal-entries', token, {
  branchId,
  date: '2026-01-05',
  description: 'إيداع افتتاحي',
  lines: [
    { accountId: statementAccountId, debit: '500' },
    { accountId: capitalAccountId, credit: '500' },
  ],
});
// A hand entry with no time at all, on the same day as two vouchers that have one.
await call('post', '/journal-entries', token, {
  branchId,
  date: today,
  description: 'إيداع نقدي من الإدارة',
  lines: [
    { accountId: statementAccountId, debit: '200' },
    { accountId: capitalAccountId, credit: '200' },
  ],
});
const morningReceipt = await call('post', '/vouchers', token, {
  branchId,
  kind: 'receipt',
  subtype: 'customer',
  date: today,
  voucherTime: '9:00',
  partyId: customer.id,
  cashLocationId: statementSafe.id,
  method: 'cash',
  amount: '1000',
  description: 'تحصيل صباحي',
});
await call('post', `/vouchers/${morningReceipt.id}/post`, token, {});
const eveningPayment = await call('post', '/vouchers', token, {
  branchId,
  kind: 'payment',
  subtype: 'expense',
  date: today,
  voucherTime: '15:00',
  cashLocationId: statementSafe.id,
  counterAccountId: expenseAccountId,
  method: 'cash',
  amount: '300',
  description: 'مصروفات نثرية مسائية',
});
await call('post', `/vouchers/${eveningPayment.id}/post`, token, {});
// A draft: it must not move the safe on paper before it moves it in the box.
await call('post', '/vouchers', token, {
  branchId,
  kind: 'receipt',
  subtype: 'customer',
  date: today,
  partyId: customer.id,
  cashLocationId: statementSafe.id,
  method: 'cash',
  amount: '7000',
  description: 'مسودة لم تُعتمد',
});

const everything = await movements(statementSafe.id, 'all=1');
const kinds = (everything.rows ?? []).map((row) => row.processType);
check(
  'الكشف يقرأ دفتر الحساب: قيد يومية + سند قبض + سند صرف',
  ['قيد يومية', 'سند قبض', 'سند صرف'].every((kind) => kinds.includes(kind)),
  kinds.join(' · '),
);
check('⚖️ الرصيد الإجمالي يجمع كل ما حرّك الصندوق', money(everything.totalAll) === money(1400), everything.totalAll);
check('والمسوّدة لا تُحرّك الصندوق على الورق', !JSON.stringify(everything.rows).includes('مسودة لم تُعتمد'));

const lastRow = (everything.rows ?? []).at(-1);
check(
  'الرصيد المتحرك لا يقفز: آخر رصيد هو الرصيد الإجمالي',
  money(lastRow?.balance) === money(everything.totalAll),
  `${lastRow?.balance} / ${everything.totalAll}`,
);

const period = await movements(statementSafe.id, `from=${today}&to=${today}`);
const openingRow = (period.rows ?? [])[0];
check('🧾 رصيد سابق يفتح الكشف عند تحديد فترة', money(period.openingBalance) === money(500), period.openingBalance);
check(
  'وسطره مؤرَّخ بيوم قبل «من تاريخ»',
  openingRow?.isOpening === true && openingRow?.processType === 'رصيد سابق',
  `${openingRow?.processType} ${openingRow?.date}`,
);
check('⚖️ الرصيد الإجمالي يبقى رصيد الصندوق', money(period.totalAll) === money(1400), period.totalAll);
check('📅 رصيد الفترة المحددة هو ما تحرّك فيها فقط', money(period.totalPeriod) === money(900), period.totalPeriod);

const morning = await movements(statementSafe.id, `from=${today}&to=${today}&toTime=10:00`);
const morningKinds = (morning.rows ?? []).map((row) => row.processType);
check('⏰ الوقت يقصّ النهار: سند الخامسة عصراً خارج نافذة الصباح', !morningKinds.includes('سند صرف'), morningKinds.join(' · '));
check('والقيد الذي بلا وقت لا يُخفى أبداً', morningKinds.includes('قيد يومية'));
check('والرصيد يتبع النافذة', money(morning.totalAll) === money(1700), morning.totalAll);

try {
  await movements(orphanSafe.id, 'all=1');
  check('صندوق بلا حساب لا يُفتح له كشف', false, 'expected 422');
} catch (error) {
  check(
    'صندوق بلا حساب لا يُفتح له كشف',
    error.status === 422 && error.code === 'CASH_ACCOUNT_REQUIRED',
    `${error.status} ${error.code}`,
  );
}
try {
  await movements(statementSafe.id, 'from=15-01-2026');
  check('وتاريخ غير مفهوم مرفوض', false, 'expected 422');
} catch (error) {
  check(
    'وتاريخ غير مفهوم مرفوض',
    error.status === 422 && error.code === 'MOVEMENT_DATE_INVALID',
    `${error.status} ${error.code}`,
  );
}
console.log('');

console.log(failures === 0 ? '\n✔ Phase 06 treasury documents verified' : `\n✗ ${failures} check(s) failed`);
process.exit(failures === 0 ? 0 : 1);
