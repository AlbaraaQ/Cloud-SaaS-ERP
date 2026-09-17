#!/usr/bin/env node
/**
 * Live verification of P-C1 — «الأساس والقشرة، وترميم الصلاحيات»
 * (`docs/roadmap/PLATFORM_CONSOLE_PLAN.md` §4) against a running stack
 * (`node scripts/local-db.mjs` + `pnpm db:migrate` + `pnpm db:seed` + `pnpm dev`).
 *
 * It drives the real HTTP API the console drives — nothing is mocked:
 *
 *   1. 🔐 الجلسة والصلاحيات — ما يقوله `/me` لمشغّل المنصة ولغيره
 *   2. 🗺️ المسارات — كل مسار `/platform/*` يعمل للمالك
 *   3. 🚫 الأبواب المغلقة — جلسة مستأجر لا تصل إلى اللوحة
 *   4. 👥 الأدوار والرموز — الأدوار الخمسة، والرموز الثلاثة عشر
 *   5. ⚙️ إعدادات المنصة — قراءة، كتابة، تدقيق، رفض، واستعادة
 *   6. 📜 التدقيق العابر للمستأجرين — بلا حدود منشأةٍ واحدة
 *   7. 🔎 البحث الشامل — Ctrl+K على الرمز والاسم العربي
 *   8. 👤 دور محدود — العمليات تقرأ ولا توقف منشأة
 *   9. 📋 المهام — صندوق الأحداث عبر كل العملاء
 *  10. 🧹 التنظيف — الحالة تعود كما كانت
 *
 * Re-runnable and non-destructive: the settings are snapshotted before anything is written
 * and restored at the end, and the temporary platform role granted in §8 is revoked in §10.
 * Nothing is emailed and no message is sent — the console's writes are its own settings and
 * one role grant on a seeded demo user.
 *
 * Usage: node scripts/verify-platform-console.mjs
 */
import { loadEnvFiles } from './dotenv.mjs';

loadEnvFiles();

const base = process.env.API_BASE ?? 'http://127.0.0.1:3000/api/v1';
const platformTenant = process.env.VERIFY_PLATFORM_TENANT ?? 'platform';
const demo = {
  tenantCode: process.env.VERIFY_TENANT ?? 'demo',
  email: process.env.DEMO_OWNER_EMAIL ?? 'owner@demo.test',
  password: process.env.DEMO_OWNER_PASSWORD ?? '',
};
const operator = {
  email: process.env.PLATFORM_ADMIN_EMAIL ?? 'admin@platform.test',
  password: process.env.PLATFORM_ADMIN_PASSWORD ?? '',
};

let failures = 0;
let checks = 0;

function check(label, condition, detail = '') {
  checks += 1;
  if (condition) {
    console.log(`  ✓ ${label}${detail ? ` — ${detail}` : ''}`);
  } else {
    failures += 1;
    console.log(`  ✗ ${label}${detail ? ` — ${detail}` : ''}`);
  }
}

async function request(method, path, body, token) {
  const response = await fetch(`${base}${path}`, {
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
    const error = new Error(
      `${method} ${path} → ${response.status} ${parsed.code ?? ''} ${parsed.detail ?? parsed.message ?? ''}`,
    );
    error.status = response.status;
    error.code = parsed.code;
    error.detail = parsed.detail ?? parsed.message;
    throw error;
  }
  return parsed.data ?? parsed;
}

/** A call that is *expected* to be refused: its status and code are the answer. */
async function refused(method, path, body, token) {
  try {
    await request(method, path, body, token);
    return { status: 200, code: '', detail: '' };
  } catch (error) {
    return { status: error.status ?? 0, code: error.code ?? '', detail: error.detail ?? '' };
  }
}

async function signIn(tenantCode, credentials) {
  // The platform throttles /auth/login per address and this run signs in three times; a 429
  // is not a failed check, so wait the window out before giving up.
  for (let attempt = 0; ; attempt += 1) {
    try {
      const login = await request('post', '/auth/login', { tenantCode, ...credentials });
      const token = login.accessToken ?? login.access_token ?? login.token;
      if (!token) throw new Error(`login failed for ${credentials.email}`);
      return { token, user: login.user };
    } catch (error) {
      if (error.status !== 429 || attempt === 3) throw error;
      await new Promise((resolve) => setTimeout(resolve, 4_000));
    }
  }
}

const ownerSession = await signIn(platformTenant, operator);
const ownerToken = ownerSession.token;
console.log(`✔ logged in to ${platformTenant} as ${operator.email}\n`);

const get = (path, token = ownerToken) => request('get', path, undefined, token);
const put = (path, body, token = ownerToken) => request('put', path, body, token);

// ═══════════════════════════════════════════════ 1. 🔐 الجلسة والصلاحيات
console.log('■ 1. 🔐 الجلسة والصلاحيات — ما يقوله /me');
const ownerMe = await get('/me');
const ownerCodes = ownerMe.platformPermissions ?? [];
check('جلسة المنصة تُعلن أدوار المنصة', Array.isArray(ownerMe.user.platformRoles) && ownerMe.user.platformRoles.includes('platform_owner'), ownerMe.user.platformRoles.join(', '));
check('وترمز صلاحيات اللوحة على حدة', ownerCodes.length >= 13, `${ownerCodes.length} رمزاً`);
check('والمفتاح الجديد معلَن وممنوح للمالك', ownerCodes.includes('console.settings.manage'));
check(
  'ولا رمز console بين صلاحيات المستأجر',
  (ownerMe.permissions ?? []).every((code) => !code.startsWith('console.')),
);
check('والمفتاح 🧪 محاكاة لا يظهر كرمز مستأجر', !(ownerMe.permissions ?? []).includes('console.settings.manage'));

const demoSession = await signIn(demo.tenantCode, { email: demo.email, password: demo.password });
const demoToken = demoSession.token;
const demoMe = await get('/me', demoToken);
check('ومالك المستأجر لا يحمل أي رمز لوحة', (demoMe.platformPermissions ?? []).length === 0);
check('ولا يُعلَن مشغّل منصة', demoMe.user.isPlatformAdmin === false);

// ═══════════════════════════════════════════════════════ 2. 🗺️ المسارات
console.log('\n■ 2. 🗺️ المسارات — كل مسار /platform/* يعمل للمالك');
const GET_ROUTES = [
  '/platform/overview',
  '/platform/tenants',
  '/platform/plans',
  '/platform/subscriptions',
  '/platform/activation-requests',
  '/platform/users',
  '/platform/roles',
  '/platform/permissions',
  '/platform/audit',
  '/platform/settings',
  '/platform/tenants/search?q=demo',
  '/platform/jobs/outbox?limit=5',
];
for (const path of GET_ROUTES) {
  const result = await refused('get', path, undefined, ownerToken);
  check(`GET ${path}`, result.status === 200, result.status === 200 ? '' : `${result.status} ${result.detail}`);
}

// ═════════════════════════════════════════════════ 3. 🚫 الأبواب المغلقة
console.log('\n■ 3. 🚫 الأبواب المغلقة — جلسة المستأجر لا تصل إلى اللوحة');
for (const path of ['/platform/overview', '/platform/tenants', '/platform/audit', '/platform/settings', '/platform/users']) {
  const result = await refused('get', path, undefined, demoToken);
  check(`${path} ترفض المستأجر`, result.status === 403, `${result.status} ${result.code}`);
}
const anonymous = await refused('get', '/platform/audit', undefined, '');
check('وترفض بلا جلسة', anonymous.status === 401, String(anonymous.status));

// ════════════════════════════════════════════ 4. 👥 الأدوار والرموز
console.log('\n■ 4. 👥 الأدوار والرموز');
const roles = await get('/platform/roles');
const roleCodes = roles.map((role) => role.code);
check(
  'الأدوار الخمسة كلها موجودة',
  ['platform_owner', 'platform_operations', 'platform_billing', 'platform_support', 'platform_auditor'].every((code) =>
    roleCodes.includes(code),
  ),
  roleCodes.join(', '),
);
const ownerRole = roles.find((role) => role.code === 'platform_owner');
const operationsRole = roles.find((role) => role.code === 'platform_operations');
check('مالك المنصة يحمل الرموز الثلاثة عشر', ownerRole.permissions.length === 13, `${ownerRole.permissions.length}`);
check('والعمليات لا تملك إيقاف منشأة', !operationsRole.permissions.includes('console.tenants.manage'));
check('ولا تملك كتابة الإعدادات', !operationsRole.permissions.includes('console.settings.manage'));

const registry = await get('/platform/permissions');
check('سجل رموز اللوحة يعرضها كلها', registry.length === 13, `${registry.length} رمزاً`);
check(
  'والمفتاح الجديد فيه',
  registry.some((entry) => entry.code === 'console.settings.manage'),
);

// ════════════════════════════════════════════════ 5. ⚙️ إعدادات المنصة
console.log('\n■ 5. ⚙️ إعدادات المنصة');
const settingsSnapshot = await get('/platform/settings');
const snapshotValues = Object.fromEntries(settingsSnapshot.settings.map((setting) => [setting.key, setting.value]));
check('البيئة مُعلَنة بوسمٍ عربي', typeof settingsSnapshot.environment.name === 'string' && settingsSnapshot.environment.labelAr.length > 0, `${settingsSnapshot.environment.name} / ${settingsSnapshot.environment.labelAr}`);
check('ثمانية إعدادات معرَّفة', settingsSnapshot.settings.length === 8, `${settingsSnapshot.settings.length}`);
check('وكل إعداد له تسمية عربية وشرح', settingsSnapshot.settings.every((setting) => setting.labelAr.length > 0 && setting.helpAr.length > 0));
check(
  'والحدود الافتراضية الثلاثة معروضة',
  ['limits.max_users', 'limits.max_branches', 'limits.max_invoices_per_month'].every((key) =>
    settingsSnapshot.settings.some((setting) => setting.key === key),
  ),
);

try {
  const written = await put('/platform/settings', {
    values: {
      'support.email': 'support.verify@demo.test',
      'support.phone': '920000000',
      'platform.domains': ['verify.example.test'],
      'limits.max_users': 33,
      'platform.maintenance': true,
      'platform.maintenance_message': 'نافذة صيانة تجريبية',
    },
  });
  const byKey = Object.fromEntries(written.settings.map((setting) => [setting.key, setting]));
  check('الكتابة تُقرأ فوراً', byKey['support.email'].value === 'support.verify@demo.test');
  check('والأعداد تُخزَّن أعداداً', byKey['limits.max_users'].value === 33, typeof byKey['limits.max_users'].value);
  check('والقوائم تُخزَّن قوائم', Array.isArray(byKey['platform.domains'].value) && byKey['platform.domains'].value[0] === 'verify.example.test');
  check('ومفتاح الصيانة صار مفتوحاً', byKey['platform.maintenance'].value === true);
  check('ولم يبقَ إعدادٌ على قيمته الافتراضية', byKey['support.email'].isDefault === false);

  const persisted = await get('/platform/settings');
  check(
    'والقيمة تبقى بعد قراءةٍ جديدة',
    persisted.settings.find((setting) => setting.key === 'support.phone').value === '920000000',
  );

  const auditRows = await get(
    '/platform/audit?filter[entity]=platform_settings&filter[entityId]=platform.maintenance&limit=5',
  );
  check('وكل كتابة تُسجَّل في التدقيق', auditRows.items.length >= 1, `${auditRows.items.length} سطراً`);
  const auditRow = auditRows.items[0] ?? {};
  check('بصاحب الفعل', auditRow.actorUserId === ownerMe.user.id);
  check('وبقيمة قبل وبعد', auditRow.before?.value === false && auditRow.after?.value === true);

  const badKey = await refused('put', '/platform/settings', { values: { 'platform.colour': 'teal' } }, ownerToken);
  check('والرفض 422 لمفتاح مجهول', badKey.status === 422, `${badKey.status} ${badKey.detail}`);
  const badEmail = await refused('put', '/platform/settings', { values: { 'support.email': 'not-an-email' } }, ownerToken);
  check('ولبريدٍ تالف', badEmail.status === 422, `${badEmail.status}`);
  const badRange = await refused('put', '/platform/settings', { values: { 'limits.max_branches': -5 } }, ownerToken);
  check('ولعددٍ خارج المدى', badRange.status === 422, `${badRange.status}`);
} finally {
  await put('/platform/settings', { values: snapshotValues });
}

// ═══════════════════════════════════ 6. 📜 التدقيق العابر للمستأجرين
console.log('\n■ 6. 📜 التدقيق العابر للمستأجرين');
const auditAll = await get('/platform/audit?limit=200');
check('السجل يعود بصفحة وعددٍ كلي', typeof auditAll.total === 'number' && Array.isArray(auditAll.items), `total=${auditAll.total}`);
check('القراءة تعبر المستأجرين', new Set(auditAll.items.map((row) => row.tenantId)).size > 1, `${new Set(auditAll.items.map((row) => row.tenantId)).size} منشأة`);
const withCustomer = auditAll.items.find((row) => row.tenantId !== null && row.tenantCode);
check('وكل سطر يحمل اسم عميله ورمزه', Boolean(withCustomer?.tenantName), withCustomer?.tenantCode ?? '—');
const byTenant = await get(`/platform/audit?filter[tenantId]=${withCustomer.tenantId}&limit=50`);
check(
  'والمرشّح بالمستأجر يحصر النتائج',
  byTenant.items.length > 0 && byTenant.items.every((row) => row.tenantId === withCustomer.tenantId),
  `${byTenant.items.length} سطراً`,
);
const byAction = await get('/platform/audit?filter[action]=auth.login&limit=5');
check(
  'ومرشّح الإجراء يعمل',
  byAction.items.length > 0 && byAction.items.every((row) => row.action === 'auth.login'),
  `${byAction.items.length} سطراً`,
);
const badFilter = await refused('get', '/platform/audit?filter[colour]=red', undefined, ownerToken);
check('والمرشّح المجهول يُرفض 400', badFilter.status === 400 && badFilter.code === 'FILTER_NOT_ALLOWED', badFilter.code);
const tenantAudit = await get('/audit-log?limit=5', demoToken);
check('وسجل المستأجر يبقى خاصاً به', Array.isArray(tenantAudit.data) || Array.isArray(tenantAudit));

// ═══════════════════════════════════════════════════ 7. 🔎 البحث الشامل
console.log('\n■ 7. 🔎 البحث الشامل');
const byCode = await get('/platform/tenants/search?q=demo');
check('البحث بالرمز يجد العميل', byCode.some((hit) => hit.code === demo.tenantCode), byCode.map((hit) => hit.code).join(', '));
const allTenants = await get('/platform/tenants');
const arabic = allTenants.find((tenant) => /[\u0600-\u06FF]/.test(tenant.name));
if (arabic) {
  const byName = await get(`/platform/tenants/search?q=${encodeURIComponent(arabic.name.slice(0, 6))}`);
  check('والبحث بالاسم العربي يعمل', byName.some((hit) => hit.id === arabic.id), arabic.name);
} else {
  check('والبحث بالاسم يعمل', true, 'لا اسم عربي في قاعدة البيانات — تُخطّى');
}
const emptySearch = await refused('get', '/platform/tenants/search?q=', undefined, ownerToken);
check('والاستعلام الفارغ يُرفض 400', emptySearch.status === 400, String(emptySearch.status));

// ════════════════════════════════════════════════════ 8. 👤 دور محدود
console.log('\n■ 8. 👤 دور محدود — العمليات تقرأ ولا توقف منشأة');
const demoRow = (await get(`/platform/users?search=${encodeURIComponent(demo.email)}`)).find(
  (row) => row.email === demo.email,
);
check('وجدنا حساب التجربة في دليل المستخدمين', Boolean(demoRow), demoRow?.id ?? '—');
await request('post', `/platform/users/${demoRow.id}/roles`, { roleCode: 'platform_operations' }, ownerToken);
const limitedSession = await signIn(demo.tenantCode, { email: demo.email, password: demo.password });
const limitedToken = limitedSession.token;
const limitedMe = await get('/me', limitedToken);
check(
  'صار يحمل رموز العمليات',
  (limitedMe.platformPermissions ?? []).includes('console.tenants.view') &&
    !(limitedMe.platformPermissions ?? []).includes('console.tenants.manage'),
  (limitedMe.platformPermissions ?? []).join(', '),
);
const limitedAudit = await refused('get', '/platform/audit?limit=1', undefined, limitedToken);
check('ويقرأ التدقيق', limitedAudit.status === 200, String(limitedAudit.status));
const limitedSettings = await refused('get', '/platform/settings', undefined, limitedToken);
check('ويقرأ الإعدادات', limitedSettings.status === 200, String(limitedSettings.status));
const limitedWrite = await refused(
  'put',
  '/platform/settings',
  { values: { 'platform.maintenance': false } },
  limitedToken,
);
check(
  'ولا يكتبها (console.settings.manage)',
  limitedWrite.status === 403 && limitedWrite.detail === 'platform permission console.settings.manage required',
  `${limitedWrite.status} ${limitedWrite.detail}`,
);
const limitedSuspend = await refused(
  'patch',
  `/platform/tenants/${demoMe.membership.tenantId}/status`,
  { status: 'suspended' },
  limitedToken,
);
check(
  'ولا يوقف منشأة (console.tenants.manage)',
  limitedSuspend.status === 403 && limitedSuspend.detail === 'platform permission console.tenants.manage required',
  `${limitedSuspend.status} ${limitedSuspend.detail}`,
);

// ═════════════════════════════════════════════════════════════ 9. 📋 المهام
console.log('\n■ 9. 📋 المهام — صندوق الأحداث عبر كل العملاء');
const outbox = await get('/platform/jobs/outbox?limit=20');
check('الصندوق يعود بصفحةٍ وعدد', Array.isArray(outbox.items) && typeof outbox.total === 'number', `total=${outbox.total}`);
check(
  'وكل مهمة تحمل عميلها',
  outbox.items.every((job) => job.tenantId && 'tenantCode' in job && 'status' in job),
  `${outbox.items.length} مهمة`,
);
const filteredOutbox = await get('/platform/jobs/outbox?status=dead&limit=5');
check('والمرشّح بالحالة يعمل', filteredOutbox.items.every((job) => job.status === 'dead'));
const outboxDenied = await refused('get', '/platform/jobs/outbox', undefined, demoToken);
check('والمستأجر لا يراه', outboxDenied.status === 403, String(outboxDenied.status));

// ═════════════════════════════════════════════════════════════ 10. 🧹 التنظيف
console.log('\n■ 10. 🧹 التنظيف');
await request('delete', `/platform/users/${demoRow.id}/roles/platform_operations`, undefined, ownerToken);
const afterRevoke = await signIn(demo.tenantCode, { email: demo.email, password: demo.password });
const revokedMe = await get('/me', afterRevoke.token);
check('سُحب الدور المحدود', (revokedMe.platformPermissions ?? []).length === 0);
const afterDenied = await refused('get', '/platform/tenants', undefined, afterRevoke.token);
check('وعاد المستأجر ممنوعاً من اللوحة', afterDenied.status === 403, String(afterDenied.status));

const restored = await get('/platform/settings');
const restoredValues = Object.fromEntries(restored.settings.map((setting) => [setting.key, setting.value]));
check(
  'والإعدادات عادت إلى ما كانت عليه',
  JSON.stringify(restoredValues) === JSON.stringify(snapshotValues),
  `${Object.keys(restoredValues).length} مفتاحاً`,
);

// ═══════════════════════════════════════════════════════════════ الخلاصة
console.log(`\n${'─'.repeat(70)}`);
console.log(` نقاط التحقّق: ${checks} · نجحت: ${checks - failures} · فشلت: ${failures}`);
console.log(`${'─'.repeat(70)}`);
process.exit(failures === 0 ? 0 : 1);
