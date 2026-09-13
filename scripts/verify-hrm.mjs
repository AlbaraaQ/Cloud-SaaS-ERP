#!/usr/bin/env node
/**
 * Live verification of the Phase 08 part one HRM documents against a running stack
 * (`pnpm db:local` + `pnpm db:migrate` + `pnpm db:seed` + `pnpm dev`).
 *
 * It drives the real HTTP API the staff screens drive — nothing is mocked:
 *
 *   1. 🏢 الإدارات والأقسام — إدارة واحدة وأقسامها، والرفضان (`frmManagement` /
 *      `frmDepartments`)
 *   2. 👤 بطاقة الموظف — كل حقول النافذة تُحفظ وتُقرأ (`frmEmployees`)
 *   3. رقم الحساب — حساب باسم الموظف تحت «موظفين الفرع الرئيسي»، ويتغيّر بتغيّر الاسم
 *   4. إجمالي الرواتب والمستحقات — مجموع البدلات السبعة
 *   5. قائمة الموظفين — البحث بالاسم وبالرقم، والتضييق بالإدارة
 *   6. الحذف — «لا يمكن حذف موظف مرتبط بمستخدم»، ثم حذف موظف بلا ارتباط
 *
 * The two other refusals («لا يمكن حذف موظف مرتبط بفواتير» and the payroll one) are in
 * `apps/api/test/employee-card.spec.ts`: they leave an invoice and a payroll run behind,
 * and this script leaves nothing — every row it creates, it deletes, so it can be run
 * twice in a row.
 *
 * Usage: node scripts/verify-hrm.mjs
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
    // Node 22's undici rejects lowercase verbs: `patch` comes back a 405 with an empty
    // body, and `JSON.parse('')` then throws instead of reporting the real problem.
    method: method.toUpperCase(),
    headers: {
      'content-type': 'application/json',
      ...(token ? { authorization: `Bearer ${token}` } : {}),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await response.text();
  const parsed = text ? JSON.parse(text) : {};
  if (!response.ok) {
    const error = new Error(`${method} ${path} → ${response.status} ${parsed.code ?? ''} ${parsed.detail ?? parsed.message ?? ''}`);
    error.status = response.status;
    error.code = parsed.code;
    error.detail = parsed.detail;
    throw error;
  }
  return parsed.data ?? parsed;
}

const login = await call('post', '/auth/login', undefined, { tenantCode, email, password });
const token = login.accessToken ?? login.access_token ?? login.token;
console.log(`✔ logged in to ${tenantCode} as ${email}\n`);

const stamp = Date.now().toString().slice(-6);
const get = (path) => call('get', path, token);
const post = (path, body) => call('post', path, token, body);
const patch = (path, body) => call('patch', path, token, body);
const del = (path) => call('delete', path, token);

/** A refusal is a result, not a crash — the desktop shows the sentence to the operator. */
async function refused(method, path, body) {
  try {
    // Node 22's undici rejects lowercase verbs: `patch` becomes a 405 with an empty body.
    await (method.toUpperCase() === 'DELETE' ? del(path) : method.toUpperCase() === 'PATCH' ? patch(path, body) : post(path, body));
    return { status: 200, code: '', detail: '' };
  } catch (error) {
    return { status: error.status ?? 0, code: error.code ?? '', detail: error.detail ?? '' };
  }
}

// ---------------------------------------------------------------------------
// Cleanup — anything a previous run left behind (`employeeNo` / `code` start with VR).
// ---------------------------------------------------------------------------
const leftoverEmployees = (await get('/hrm/employees')).filter((row) => String(row.employeeNo).startsWith('VR'));
for (const row of leftoverEmployees) await refused('delete', `/hrm/employees/${row.id}`);
const leftoverDepartments = (await get('/hrm/departments')).filter((row) => String(row.code).startsWith('VR'));
for (const row of leftoverDepartments.filter((entry) => entry.kind === 'section')) await refused('delete', `/hrm/departments/${row.id}`);
for (const row of leftoverDepartments.filter((entry) => entry.kind === 'management')) await refused('delete', `/hrm/departments/${row.id}`);

console.log('1. 🏢 الإدارات والأقسام — إدارة واحدة وأقسامها');

const management = await post('/hrm/departments', { code: `VRM${stamp}`, name: `الإدارة المالية ${stamp}` });
check('➕ إضافة إدارة — بلا أب (أي: إدارة لا قسم)', !management.parentId, `${management.code} ${management.name}`);

const section = await post('/hrm/departments', { code: `VRS${stamp}`, name: `قسم الخزينة ${stamp}`, parentId: management.id });
check('➕ إضافة قسم — يتبع الإدارة', section.parentId === management.id);

const listed = await get('/hrm/departments');
const sectionRow = listed.find((row) => row.id === section.id);
check('«اسم الإدارة التابع لها» في القائمة', sectionRow?.parentName === management.name, sectionRow?.parentName);
check(
  'الإدارة في القائمة `management` وتحصي أقسامها',
  listed.find((row) => row.id === management.id)?.kind === 'management' &&
    (listed.find((row) => row.id === management.id)?.sectionCount ?? 0) === 1,
);

const nested = await refused('post', '/hrm/departments', { code: `VRS${stamp}2`, name: 'قسم تحت القسم', parentId: section.id });
check('لا قسم تحت قسم — «لا يمكن إضافة قسم تحت قسم آخر»', nested.status === 422 && nested.detail === 'لا يمكن إضافة قسم تحت قسم آخر', `${nested.status} ${nested.code}`);

const managementWithSections = await refused('delete', `/hrm/departments/${management.id}`);
check('لا يمكن حذف إدارة لها أقسام', managementWithSections.status === 409 && managementWithSections.code === 'DEPARTMENT_HAS_SECTIONS', managementWithSections.detail);

console.log('\n2. 👤 بطاقة الموظف — كل حقول النافذة');

const card = (suffix) => ({
  employeeNo: `VR${stamp}${suffix}`,
  name: `موظف التحقق ${stamp}${suffix}`,
  departmentId: section.id,
  hireDate: '2024-01-15',
  birthDate: '1990-05-02',
  insuranceNo: `INS-${stamp}${suffix}`,
  nationalId: `1099${stamp}${suffix}`,
  maritalStatus: 'متزوج',
  nationality: 'يمني',
  gender: 'male',
  phone: '01-234567',
  mobile: '771234567',
  email: `verify${stamp}${suffix}@example.com`,
  address: 'صنعاء - شارع حدة',
  notes: 'موظف تحقق',
  salaryComponents: { basic: '5000', housing: '1000', transport: '500', food: '300', medical: '200', fixedBonus: '250', other: '150' },
  bank: { iban: 'SA0380000000608010167519', bankNo: '123456', bankName: 'بنك اليمن' },
});

const employee = await post('/hrm/employees', card('1'));
const read = await get(`/hrm/employees/${employee.id}`);

check(
  'بيانات الموظف الأساسية تُقرأ كما كُتبت',
  read.name === employee.name &&
    read.employeeNo === employee.employeeNo &&
    read.hireDate === '2024-01-15' &&
    read.birthDate === '1990-05-02' &&
    read.insuranceNo === employee.insuranceNo &&
    read.maritalStatus === 'متزوج' &&
    read.nationality === 'يمني' &&
    read.gender === 'male' &&
    read.phone === '01-234567' &&
    read.mobile === '771234567' &&
    read.email === employee.email,
  read.employeeNo,
);
check('«النوع» و«الحالة» بأسمائهما العربية', read.genderLabel === 'ذكر' && read.statusLabel === 'نشط', `${read.genderLabel} · ${read.statusLabel}`);
check(
  'البيانات التكميلية — العنوان · رقم الهوية · الحساب البنكي · اسم البنك · ملاحظات',
  read.address === 'صنعاء - شارع حدة' &&
    read.nationalId === employee.nationalId &&
    read.bank?.bankNo === '123456' &&
    read.bank?.bankName === 'بنك اليمن' &&
    read.notes === 'موظف تحقق',
);
check('الآيبان يردّ مقنّعاً', read.bank?.iban === 'SA03********7519', read.bank?.iban);
check('الإدارة تُقرأ من القسم ولا تُخزَّن مرتين', read.managementId === management.id && read.managementName === management.name && read.sectionId === section.id, `${read.managementName} / ${read.sectionName}`);

console.log('\n3. رقم الحساب — حساب باسم الموظف تحت «موظفين الفرع الرئيسي»');

const root = (await get('/accounts?q=2241')).find((row) => row.code === '2241');
check('الشجرة تحمل جذر الموظفين (2241)', Boolean(root), root?.nameAr);

const account = await get(`/accounts/${employee.employeeAccountId}`);
check(
  'SaveAccounts — حساب باسم الموظف تحت الجذر، قابل للترحيل',
  account.parentId === root.id && account.nameAr === employee.name && account.code.startsWith('2241') && account.isPostable === true,
  `${account.code} ${account.nameAr}`,
);
check('رقم الحساب يظهر على البطاقة', read.accountCode === account.code, read.accountCode);

const renamed = await patch(`/hrm/employees/${employee.id}`, { name: `${employee.name} بعد التعديل` });
const renamedAccount = await get(`/accounts/${employee.employeeAccountId}`);
check('تغيير الاسم يغيّر اسم الحساب', renamedAccount.nameAr === renamed.name, renamedAccount.nameAr);

const second = await post('/hrm/employees', card('2'));
check('كل موظف يأخذ الرقم التالي', Number(second.accountCode) === Number(employee.accountCode) + 1, `${employee.accountCode} → ${second.accountCode}`);

console.log('\n4. إجمالي الرواتب والمستحقات');

check('المجموع = الراتب الأساسي + ستة بدلات', money(second.totalSalary) === '7400.00', money(second.totalSalary));
const afterRaise = await patch(`/hrm/employees/${second.id}`, { salaryComponents: { basic: '6000', housing: '1000', transport: '500', food: '300', medical: '200', fixedBonus: '250', other: '150' } });
check('يتغيّر بتغيّر البدلات', money(afterRaise.totalSalary) === '8400.00', money(afterRaise.totalSalary));

console.log('\n5. قائمة الموظفين — البحث بالاسم وبالرقم');

const byName = await get(`/hrm/employees?q=${encodeURIComponent(employee.name)}`);
check('البحث بالاسم', byName.some((row) => row.id === employee.id), `${byName.length} صف`);
const byNumber = await get(`/hrm/employees?q=${encodeURIComponent(second.employeeNo)}`);
check('البحث بالرقم', byNumber.some((row) => row.id === second.id), second.employeeNo);
const inManagement = await get(`/hrm/employees?department_id=${management.id}`);
check(
  'التضييق بالإدارة يجلب موظفي أقسامها',
  inManagement.some((row) => row.id === employee.id) && inManagement.some((row) => row.id === second.id),
  `${inManagement.length} صف`,
);

console.log('\n6. الحذف — «لا يمكن حذف موظف مرتبط بمستخدم»');

const memberships = await get('/memberships');
const membership = (memberships.data ?? memberships)[0];
const linked = await patch(`/hrm/employees/${second.id}`, { membershipId: membership.id });
check('ربط الموظف بمستخدم', linked.id === second.id, membership.displayName ?? membership.id);
const refusedDelete = await refused('delete', `/hrm/employees/${second.id}`);
check('الرفض بعبارة الديسكتوب', refusedDelete.status === 409 && refusedDelete.detail === 'لا يمكن حذف موظف مرتبط بمستخدم', `${refusedDelete.status} ${refusedDelete.code}`);

await patch(`/hrm/employees/${second.id}`, { membershipId: null });
check('فكّ الارتباط', (await get(`/hrm/employees/${second.id}`)).membershipId === null);
const deleted = await del(`/hrm/employees/${second.id}`);
check('حذف موظف بلا ارتباط', deleted.id === second.id && (await refused('get', `/hrm/employees/${second.id}`)).status === 404);

const sectionWithEmployees = await refused('delete', `/hrm/departments/${section.id}`);
check('لا يمكن حذف قسم عليه موظفون', sectionWithEmployees.status === 409 && sectionWithEmployees.code === 'DEPARTMENT_IN_USE', sectionWithEmployees.detail);

// ---------------------------------------------------------------------------
// Cleanup — what this run created, removed again.
// ---------------------------------------------------------------------------
for (const row of (await get('/hrm/employees')).filter((entry) => String(entry.employeeNo).startsWith('VR'))) await refused('delete', `/hrm/employees/${row.id}`);
for (const row of (await get('/hrm/departments')).filter((entry) => String(entry.code).startsWith('VR'))) await refused('delete', `/hrm/departments/${row.id}`);
const remaining = (await get('/hrm/employees')).filter((entry) => String(entry.employeeNo).startsWith('VR'));
check('لا يبقى أثر بعد التشغيل', remaining.length === 0, `${remaining.length} صف`);

console.log(failures === 0 ? '\n✔ Phase 08 part one — 👤 بطاقة الموظف · 🏢 الإدارات والأقسام verified' : `\n✗ ${failures} check(s) failed`);
process.exit(failures === 0 ? 0 : 1);
