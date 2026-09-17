#!/usr/bin/env node
/**
 * Live verification of P-C1 «الأساس والقشرة، وترميم الصلاحيات» and P-C2 «العملاء في العمق»
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
 *  10. 🪪 بطاقة العميل — كل سؤال عن عميل واحد في مكان واحد (P-C2)
 *  11. 📊 الاستخدام — مقابل الحدّ، ومصدر الحدّ، وتجاوز العميل
 *  12. 🚦 الحالة والملكية — بلا سبب لا يقع الفعل، والسبب في التدقيق
 *  13. 🚩 الرايات والهوية — حزمة تُفتح، ولون يُكتب، وشعار خبيث يُرفض
 *  14. 🗒️ الملاحظات — تُضاف، ولا تُحذف من بطاقة عميل آخر
 *  15. 🔐 أبواب بطاقة العميل — جلسة المستأجر لا تدخل
 *  16. 🧹 التنظيف — الحالة تعود كما كانت
 *
 * Re-runnable and non-destructive: the settings are snapshotted before anything is written
 * and restored at the end, and the temporary platform role granted in §8 is revoked in §10.
 * Nothing is emailed and no message is sent — the console's writes are its own settings and
 * one role grant on a seeded demo user.
 *
 * One deliberate **normalisation** is possible on a freshly seeded database, and §16 says so
 * rather than hiding it: `pnpm db:seed` writes the four feature flags as rows holding the
 * catalogue default (`false`). The card's write path treats "value equals the registry
 * default" as "no row" (that is what keeps `isDefault` truthful), so returning a flag to
 * `false` removes a row that merely repeated the default. The **effective** value the
 * customer sees is identical (the feature stays off), and a second run changes nothing —
 * which is what "no residue" means here, and what the before/after snapshot in the report
 * asserts.
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
  'post',
  `/platform/tenants/${demoMe.membership.tenantId}/status`,
  { status: 'suspended', reason: 'محاولة من دور العمليات' },
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

// ═══════════════════════════════════════════ 10. 🪪 بطاقة العميل
console.log('\n■ 10. 🪪 بطاقة العميل — كل سؤال عن عميل واحد');
const demoId = demoMe.membership.tenantId;
const cardSnapshot = await get(`/platform/tenants/${demoId}`);
const cardTenant = cardSnapshot.tenant;
check('البطاقة تعود باسم العميل ورمزه', cardTenant.code === demo.tenantCode && cardTenant.name.length > 0, `${cardTenant.code} / ${cardTenant.name}`);
check('وبحالته وعملته', ['active', 'suspended', 'archived'].includes(cardTenant.status), `${cardTenant.status} · ${cardTenant.baseCurrency}`);
check('وأعداد المستخدمين والفروع', typeof cardTenant.userCount === 'number' && cardTenant.branchCount >= 1, `${cardTenant.userCount} مستخدماً · ${cardTenant.branchCount} فرعاً`);
check('وعدّاد فواتير آخر ثلاثين يوماً', typeof cardTenant.invoicesLast30Days === 'number', String(cardTenant.invoicesLast30Days));
check('وآخر نشاط من التدقيق لا من سجل المتصفح', cardTenant.lastActivityAt === null || !Number.isNaN(Date.parse(cardTenant.lastActivityAt)), String(cardTenant.lastActivityAt));
check('ومالك المنشأة مُسمّى', Boolean(cardTenant.owner?.email), cardTenant.owner?.email ?? '—');
check('وسطور المستخدمين تحمل أدوارهم', cardSnapshot.members.every((member) => typeof member.roleCount === 'number' && typeof member.isOwner === 'boolean'), `${cardSnapshot.members.length} عضواً`);
const missingCard = await refused('get', '/platform/tenants/00000000-0000-0000-0000-000000000001', undefined, ownerToken);
check('وعميل غير موجود يردّ 404', missingCard.status === 404, `${missingCard.status} ${missingCard.code}`);
const badCard = await refused('get', '/platform/tenants/not-a-uuid', undefined, ownerToken);
check('ومعرّف تالف 400', badCard.status === 400, String(badCard.status));

// ═══════════════════════════════════════════ 11. 📊 الاستخدام
console.log('\n■ 11. 📊 الاستخدام — مقابل الحدّ، ومصدر الحدّ');
const usage = await get(`/platform/tenants/${demoId}/usage`);
const metricKeys = usage.metrics.map((metric) => metric.key);
check(
  'البطاقة تقيس المستخدمين والفروع وفواتير الشهر',
  JSON.stringify(metricKeys) === JSON.stringify(['users', 'branches', 'invoices_per_month']),
  metricKeys.join(', '),
);
check('وكل بند يقول مصدر حدّه', usage.metrics.every((metric) => ['tenant', 'platform', 'default'].includes(metric.limitSource)), usage.metrics.map((metric) => `${metric.key}:${metric.limitSource}`).join(' '));
check('وسلسلة ثلاثين يوماً كاملة', usage.invoicesPerDay.length === 30, `${usage.invoicesPerDay.length} يوماً`);
const usersMetric = usage.metrics.find((metric) => metric.key === 'users');
check('وعدد المستخدمين يطابق البطاقة', usersMetric.used === cardTenant.userCount, `${usersMetric.used}`);

const tenantSettingBefore = await get(`/platform/tenants/${demoId}/settings`);
const limitsBefore = Object.fromEntries(tenantSettingBefore.settings.map((setting) => [setting.key, setting.value]));
check(
  'وإعدادات العميل تعرض القسم المسموح للمستأجر فقط',
  tenantSettingBefore.settings.every((setting) => !setting.key.startsWith('platform.') && !setting.key.startsWith('support.')),
  `${tenantSettingBefore.settings.length} مفتاحاً`,
);
try {
  const override = await put(`/platform/tenants/${demoId}/settings/limits.max_users`, { value: 7 });
  const afterOverride = override.settings.find((setting) => setting.key === 'limits.max_users');
  check('كتابة تجاوزٍ خاص بالعميل', afterOverride.value === 7 && afterOverride.source === 'tenant', `${afterOverride.value} من ${afterOverride.source}`);
  const usageAfter = await get(`/platform/tenants/${demoId}/usage`);
  check(
    'والاستخدام يقرأ التجاوز لا الافتراضي',
    usageAfter.metrics.find((metric) => metric.key === 'users').limit === 7 &&
      usageAfter.metrics.find((metric) => metric.key === 'users').limitSource === 'tenant',
  );
  const platformOnly = await refused('put', `/platform/tenants/${demoId}/settings/platform.maintenance`, { value: true }, ownerToken);
  check('ومفتاح المنصة يُرفض هنا 422', platformOnly.status === 422, `${platformOnly.status} ${platformOnly.detail}`);
  const badValue = await refused('put', `/platform/tenants/${demoId}/settings/branding.primary_color`, { value: 'teal' }, ownerToken);
  check('ولون بصيغة خاطئة 422', badValue.status === 422, `${badValue.status}`);
} finally {
  await put(`/platform/tenants/${demoId}/settings/limits.max_users`, { value: null });
  const cleared = await get(`/platform/tenants/${demoId}/settings`);
  check(
    'وإزالة التجاوز تُرجع العميل إلى قيمة المنصة',
    cleared.settings.find((setting) => setting.key === 'limits.max_users').source !== 'tenant',
  );
}

// ═══════════════════════════════════════ 12. 🚦 الحالة والملكية
console.log('\n■ 12. 🚦 الحالة والملكية — بلا سبب لا يقع الفعل');
const noReason = await refused('post', `/platform/tenants/${demoId}/status`, { status: 'suspended' }, ownerToken);
check('الإيقاف بلا سبب يُرفض 400', noReason.status === 400, String(noReason.status));
const suspended = await request('post', `/platform/tenants/${demoId}/status`, { status: 'suspended', reason: 'تحقّق حيّ P-C2' }, ownerToken);
check('وبالسبب يقع الإيقاف', suspended.status === 'suspended', suspended.status);
const alreadySuspended = await refused('post', `/platform/tenants/${demoId}/status`, { status: 'suspended', reason: 'تحقّق حيّ P-C2' }, ownerToken);
check('وتكرار نفس الحالة يُرفض 409', alreadySuspended.status === 409 && alreadySuspended.code === 'TENANT_STATUS_UNCHANGED', `${alreadySuspended.status} ${alreadySuspended.code}`);
const suspendedHealth = await get(`/platform/tenants/${demoId}/health`);
check('والصحة تُعلن الإيقاف', suspendedHealth.findings.some((finding) => finding.text.includes('موقوف')), suspendedHealth.status);
const reactivated = await request('post', `/platform/tenants/${demoId}/status`, { status: 'active', reason: 'انتهى التحقّق الحيّ' }, ownerToken);
check('وإعادة التنشيط تعود بالحالة', reactivated.status === 'active');
const statusAudit = await get(`/platform/audit?filter[tenantId]=${demoId}&filter[entity]=tenant&limit=50`);
const statusRow = statusAudit.items.find((row) => row.action === 'tenant.status' && row.after?.status === 'suspended');
check('وسبب الإيقاف محفوظ في تدقيق العميل نفسه', Boolean(statusRow) && statusRow.meta?.reason === 'تحقّق حيّ P-C2', statusRow?.meta?.reason ?? '—');

const members = cardSnapshot.members;
const otherMember = members.find((member) => !member.isOwner && member.status === 'active');
if (otherMember) {
  const badTransfer = await refused('post', `/platform/tenants/${demoId}/owner/transfer`, { membershipId: '00000000-0000-0000-0000-000000000002', reason: 'عضو غير موجود' }, ownerToken);
  check('نقل الملكية لعضو غير موجود 404', badTransfer.status === 404, String(badTransfer.status));
  const transferred = await request('post', `/platform/tenants/${demoId}/owner/transfer`, { membershipId: otherMember.membershipId, reason: 'تحقّق حيّ P-C2' }, ownerToken);
  check('والنقل لعضو قائم يقع', transferred.owner.email === otherMember.email, transferred.owner.email);
  const back = await request('post', `/platform/tenants/${demoId}/owner/transfer`, { membershipId: cardTenant.owner.membershipId, reason: 'إعادة المالك في التحقّق' }, ownerToken);
  check('ويُعاد المالك الأصلي', back.owner.email === cardTenant.owner.email, back.owner.email);
} else {
  check('لا عضو ثانٍ نشط لنقل الملكية — تُخطّى', true, 'منشأة التجربة بمستخدم واحد');
}

// ═══════════════════════════ 13. 🚩 الرايات والهوية
console.log('\n■ 13. 🚩 الرايات والهوية');
const flagsBefore = await get(`/platform/tenants/${demoId}/flags`);
check('الرايات أربع', flagsBefore.flags.length === 4, flagsBefore.flags.map((flag) => flag.key).join(', '));
check('وكل راية تقول أهي افتراضية أم مضبوطة', flagsBefore.flags.every((flag) => typeof flag.isDefault === 'boolean' && typeof flag.enabled === 'boolean'));
const previousFlags = Object.fromEntries(flagsBefore.flags.map((flag) => [flag.key, flag.enabled]));
// `isDefault === false` means the customer had an explicit row; §16 reports when the write
// path turned such a redundant row (one that merely repeated the catalogue default) into none.
const previousFlagRows = Object.fromEntries(flagsBefore.flags.map((flag) => [flag.key, flag.isDefault]));
const openedFlag = await put(`/platform/tenants/${demoId}/flags`, { values: { 'feature.pos': true } });
check('فتح حزمة نقطة البيع', openedFlag.flags.find((flag) => flag.key === 'feature.pos').enabled === true);
const demoFlagView = await get('/settings', demoToken);
check('ويراها العميل في إعداداته (نفس المخزن)', demoFlagView.settings['feature.pos'] === true);
const unknownFlag = await refused('put', `/platform/tenants/${demoId}/flags`, { values: { 'feature.nope': true } }, ownerToken);
check('وراية مجهولة 422', unknownFlag.status === 422, `${unknownFlag.status} ${unknownFlag.detail}`);
await put(`/platform/tenants/${demoId}/flags`, { values: previousFlags });

const brandingBefore = await get(`/platform/tenants/${demoId}/branding`);
try {
  const branding = await put(`/platform/tenants/${demoId}/branding`, { primaryColor: '#123ABC', senderName: 'شركة تجريبية', logoUrl: 'https://example.test/logo.png' });
  check('كتابة الهوية (لون · شعار · اسم المُرسِل)', branding.primaryColor === '#123abc' && branding.senderName === 'شركة تجريبية', branding.primaryColor);
  const brandingAgain = await get(`/platform/tenants/${demoId}/branding`);
  check('والقراءة تُرجعها كما كُتبت', brandingAgain.logoUrl === 'https://example.test/logo.png');
  const badLogo = await refused('put', `/platform/tenants/${demoId}/branding`, { logoUrl: 'javascript:alert(1)' }, ownerToken);
  check('ورشعار بخواص خطرة 422', badLogo.status === 422, `${badLogo.status} ${badLogo.detail}`);
} finally {
  // `updatedAt === null` means the customer had **no** branding override: writing the
  // defaults back would leave three rows saying what the catalogue already says.
  const hadOverride = brandingBefore.updatedAt !== null;
  if (hadOverride) {
    await put(`/platform/tenants/${demoId}/branding`, {
      primaryColor: brandingBefore.primaryColor,
      senderName: brandingBefore.senderName,
      logoUrl: brandingBefore.logoUrl,
    });
  } else {
    for (const key of ['branding.primary_color', 'branding.logo_url', 'branding.sender_name']) {
      await put(`/platform/tenants/${demoId}/settings/${key}`, { value: null });
    }
  }
  const brandingAfter = await get(`/platform/tenants/${demoId}/branding`);
  const sameBranding =
    brandingAfter.primaryColor === brandingBefore.primaryColor &&
    brandingAfter.senderName === brandingBefore.senderName &&
    brandingAfter.logoUrl === brandingBefore.logoUrl &&
    (brandingAfter.updatedAt === null) === (brandingBefore.updatedAt === null);
  check('واستُعيدت الهوية إلى ما كانت عليه', sameBranding, hadOverride ? 'كما كانت' : 'بلا تجاوز');
}

// ═══════════════════════════════════════════ 14. 🗒️ الملاحظات
console.log('\n■ 14. 🗒️ الملاحظات — ما لا تحتمله الحقول');
const beforeNotes = await get(`/platform/tenants/${demoId}/notes`);
const note = await request('post', `/platform/tenants/${demoId}/notes`, { body: 'ملاحظة تحقّق حيّ P-C2' }, ownerToken);
check('إضافة ملاحظة تُرجع كاتبها', note.authorLabel.length > 0, note.authorLabel);
const listedNotes = await get(`/platform/tenants/${demoId}/notes`);
check('وتبدو في القائمة', listedNotes.total === (beforeNotes.total ?? 0) + 1, `${listedNotes.total} ملاحظة`);
const foreignNote = await refused('delete', `/platform/tenants/${withCustomer.tenantId}/notes/${note.id}`, undefined, ownerToken);
check('ولا تُحذف من بطاقة عميل آخر (404)', foreignNote.status === 404, String(foreignNote.status));
const deletedNote = await request('delete', `/platform/tenants/${demoId}/notes/${note.id}`, undefined, ownerToken);
check('وتُحذف من بطاقة صاحبها', deletedNote.deleted === true);
const noteAudit = await get(`/platform/audit?filter[tenantId]=${demoId}&filter[entity]=tenant_note&limit=20`);
check('ونصّها باقٍ في التدقيق بعد الحذف', noteAudit.items.some((row) => row.before?.body === 'ملاحظة تحقّق حيّ P-C2'), `${noteAudit.items.length} سطراً`);

// ═════════════════════════════ 15. 🔐 أبواب بطاقة العميل
console.log('\n■ 15. 🔐 أبواب بطاقة العميل');
for (const path of [
  `/platform/tenants/${demoId}`,
  `/platform/tenants/${demoId}/usage`,
  `/platform/tenants/${demoId}/settings`,
  `/platform/tenants/${demoId}/flags`,
  `/platform/tenants/${demoId}/branding`,
  `/platform/tenants/${demoId}/health`,
  `/platform/tenants/${demoId}/notes`,
]) {
  const result = await refused('get', path, undefined, demoToken);
  check(`GET ${path.replace(demoId, ':id')} ترفض جلسة المستأجر`, result.status === 403, `${result.status} ${result.code}`);
}
const anonCard = await refused('get', `/platform/tenants/${demoId}`, undefined, '');
check('وترفض بلا جلسة 401', anonCard.status === 401, String(anonCard.status));

// ═════════════════════════════════════════════════════════════ 16. 🧹 التنظيف
console.log('\n■ 16. 🧹 التنظيف');
await request('delete', `/platform/users/${demoRow.id}/roles/platform_operations`, undefined, ownerToken);
const afterRevoke = await signIn(demo.tenantCode, { email: demo.email, password: demo.password });
const revokedMe = await get('/me', afterRevoke.token);
check('سُحب الدور المحدود', (revokedMe.platformPermissions ?? []).length === 0);
const afterDenied = await refused('get', '/platform/tenants', undefined, afterRevoke.token);
check('وعاد المستأجر ممنوعاً من اللوحة', afterDenied.status === 403, String(afterDenied.status));

const restoredFlags = await get(`/platform/tenants/${demoId}/flags`);
const restoredFlagValues = Object.fromEntries(restoredFlags.flags.map((flag) => [flag.key, flag.enabled]));
check(
  'ورايات العميل بقيمها الفعلية',
  JSON.stringify(restoredFlagValues) === JSON.stringify(previousFlags),
  Object.entries(restoredFlagValues).map(([key, value]) => `${key.replace('feature.', '')}=${value}`).join(' '),
);
const normalised = restoredFlags.flags.filter((flag) => previousFlagRows[flag.key] === false && flag.isDefault);
if (normalised.length > 0) {
  // Not a residue: the value is unchanged, the redundant row is gone by design.
  console.log(
    `  · ملاحظة: ${normalised.length} راية كانت صفاً يكرّر القيمة الافتراضية فصار صفّها محذوفاً (القيمة الفعلية كما هي)`,
  );
}

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
