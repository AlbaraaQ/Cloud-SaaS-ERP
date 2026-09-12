#!/usr/bin/env node
/**
 * Live verification of the Phase 07 accounting documents against a running stack
 * (`pnpm db:local` + `pnpm db:migrate` + `pnpm db:seed` + `pnpm dev`).
 *
 * It drives the real HTTP API the staff screens drive — nothing is mocked:
 *
 *   1. 📂 شجرة الحسابات — الرصيد على كل عقدة (`frmAccountsDirectory.xaml` binds
 *      `trBalance` on every node, and a parent is the sum of its branch)
 *   2. الرصيد من القيود المرحَّلة فقط — وما عُكس لا يُحصى
 *   3. 🔍 البحث بالكود وبالاسم، وتضييق النوع والفرع
 *   4. 📋 تفاصيل الحسابات — الحساب الرئيسي مسمّى لا مُعرّفاً
 *   5. بطاقة الحساب — 📅 تاريخ فتح الحساب · 💰 الرصيد الافتتاحي · 📊 مركز التكلفة
 *
 * Usage: node scripts/verify-accounting.mjs
 */
import { loadEnvFiles } from './dotenv.mjs';

loadEnvFiles();

const base = process.env.API_BASE ?? 'http://127.0.0.1:3000/api/v1';
const tenantCode = process.env.VERIFY_TENANT ?? 'demo';
const email = process.env.VERIFY_EMAIL ?? 'owner@demo.test';
const password = process.env.DEMO_OWNER_PASSWORD ?? '';

const money = (value) => Number(value).toFixed(2);
const near = (value, expected) => Math.abs(Number(value) - expected) < 0.001;
const today = () => new Date().toISOString().slice(0, 10);
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

const token = await (
  async () => {
    const data = await call('post', '/auth/login', undefined, { tenantCode, email, password });
    return data.accessToken ?? data.access_token ?? data.token;
  }
)();
console.log(`✔ logged in to ${tenantCode} as ${email}\n`);

const stamp = Date.now().toString().slice(-6);
const account = async (code, nameAr, type, extra = {}) =>
  call('post', '/accounts', token, { code, nameAr, type, ...extra });

const directory = (query = '') => call('get', `/accounts${query ? `?${query}` : ''}`, token);

console.log('1. 📂 شجرة الحسابات — الرصيد على كل عقدة');

const costCenter = await call('post', '/cost-centers', token, {
  code: `CC${stamp}`,
  nameAr: `مركز تكلفة ${stamp}`,
});

const rootId = (await account(`9${stamp}`, `أصول التحقق ${stamp}`, 'asset', {
  isPostable: false,
  openedAt: '2024-01-01',
  costCenterId: costCenter.id,
})).id;
const midId = (await account(`9${stamp}1`, `نقدية التحقق ${stamp}`, 'asset', { parentId: rootId })).id;
const leafId = (await account(`9${stamp}11`, `صندوق التحقق ${stamp}`, 'asset', { parentId: midId })).id;
const sisterId = (await account(`9${stamp}2`, `عملاء التحقق ${stamp}`, 'asset', { parentId: rootId })).id;
const contraId = (await account(`8${stamp}`, `رأس مال التحقق ${stamp}`, 'equity')).id;

const post = (lines) => call('post', '/journal-entries', token, { date: today(), description: `قيد تحقق ${stamp}`, lines });

const balanceOf = async (id) => {
  const rows = await directory('with_balances=1');
  return rows.find((row) => row.id === id)?.balance;
};

const first = await post([
  { accountId: leafId, debit: '500' },
  { accountId: contraId, credit: '500' },
]);
await post([
  { accountId: sisterId, debit: '300' },
  { accountId: contraId, credit: '300' },
]);

const leaf = await balanceOf(leafId);
check('رصيد الورقة 500', near(Number(leaf?.balance), 500), money(leaf?.balance));
const root = await balanceOf(rootId);
check('رصيد الأب يجمع فرعه (800)', near(Number(root?.balance), 800), money(root?.balance));
check('الأب بلا حركة خاصة به', near(Number(root?.ownBalance), 0), money(root?.ownBalance));
check('عدد الأبناء في الرصيد', Number(root?.descendants) === 3, String(root?.descendants));

console.log('\n2. وما عُكس ليس مالاً');
const periods = await call('get', '/fiscal-periods', token);
const period = periods.find((row) => row.status === 'open');
const extra = await post([
  { accountId: leafId, debit: '120' },
  { accountId: contraId, credit: '120' },
]);
check('القيد الثاني يحرّك الرصيد', near(Number((await balanceOf(leafId))?.balance), 620), money((await balanceOf(leafId))?.balance));
await call('post', `/journal-entries/${extra.id}/reverse`, token, {
  branchId: (await call('get', '/branches', token))[0].id,
  fiscalPeriodId: period?.id,
  date: today(),
  reason: `عكس للتحقق ${stamp}`,
});
check('العكس يعيد الرصيد (500)', near(Number((await balanceOf(leafId))?.balance), 500), money((await balanceOf(leafId))?.balance));
void first;

console.log('\n3. 🔍 البحث بالكود وبالاسم، وتضييق النوع والفرع');
const byCode = await directory(`q=9${stamp}11`);
check('🔍 بالكود', byCode.some((row) => row.id === leafId), `${byCode.length} صف`);
const byName = await directory(`q=${encodeURIComponent('صندوق التحقق')}`);
check('🔍 بالاسم', byName.some((row) => row.id === leafId), `${byName.length} صف`);
const byType = await directory('type=equity');
check('تضييق النوع', byType.some((row) => row.id === contraId) && !byType.some((row) => row.id === leafId), `${byType.length} صف`);
const nothing = await directory('q=لا-يوجد-بهذا-الاسم');
check('بحث بلا نتيجة لا يردّ كل شيء', nothing.length === 0, `${nothing.length} صف`);

console.log('\n4. 📋 تفاصيل الحسابات — الحساب الرئيسي مسمّى');
const rows = await directory('with_balances=1');
check(
  '📋 الحساب الرئيسي',
  rows.find((row) => row.id === leafId)?.parentName === `نقدية التحقق ${stamp}`,
  String(rows.find((row) => row.id === leafId)?.parentName),
);
check('الجذر بلا أب', (rows.find((row) => row.id === rootId)?.parentName ?? null) === null, '—');

console.log('\n5. بطاقة الحساب — 📅 تاريخ فتح الحساب · 💰 الرصيد الافتتاحي · 📊 مركز التكلفة');
const openingId = (await account(`9${stamp}3`, `افتتاحي التحقق ${stamp}`, 'asset', {
  parentId: rootId,
  openingBalance: '250',
  openedAt: '2024-06-01',
})).id;
check('💰 الرصيد الافتتاحي داخل الرصيد', near(Number((await balanceOf(openingId))?.balance), 250), money((await balanceOf(openingId))?.balance));
const card = await call('get', `/accounts/${rootId}`, token);
check('📅 تاريخ فتح الحساب محفوظ', String(card.openedAt ?? '').slice(0, 10) === '2024-01-01', String(card.openedAt));
check('📊 مركز التكلفة محفوظ', card.costCenterId === costCenter.id, String(card.costCenterId));
try {
  await call('PATCH', `/accounts/${openingId}`, token, { openingBalance: '900' });
  // The opening balance is editable while nothing is posted against the account.
  check('💰 الافتتاحي قابل للتعديل قبل الترحيل', true, '250 → 900');
} catch (error) {
  check('💰 الافتتاحي قابل للتعديل قبل الترحيل', false, `${error.status} ${error.code}`);
}

// التوافق: بلا `with_balances` يبقى الردّ كما كان.
const plain = await directory();
check('النهاية القائمة بلا أرصدة كما كانت', plain.length > 0 && plain[0].balance === undefined, `${plain.length} حساب`);

console.log('');
console.log(failures === 0 ? '\n✔ Phase 07 accounting directory verified' : `\n✗ ${failures} check(s) failed`);
process.exit(failures === 0 ? 0 : 1);
