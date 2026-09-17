import { z } from 'zod';

import { uuidSchema } from '../ids.js';
import { paginationQuerySchema } from '../pagination.js';

/**
 * Platform-console contracts (P-C1 — الأساس والقشرة).
 *
 * The console has no counterpart in `Desktop_ERP` (the desktop serves one company: no
 * tenants, no subscriptions, no operators), so the "source by line" gate of
 * `docs/roadmap/README.md` §4 is satisfied here by naming *this repository's own files* —
 * `apps/api/src/modules/platform/admin/platform-admin.controller.ts` (the route table the
 * console drives), `packages/database/src/schema/platform.ts` (the tables it reads) and
 * `docs/architecture-rbac/` (the plane separation it obeys) — as
 * `PLATFORM_CONSOLE_PLAN.md` §2 requires.
 *
 * What lives here:
 *
 * - the **settings catalogue**: key, Arabic label, kind and default of every platform
 *   setting. It is shared on purpose — the API validates writes with it and the console
 *   renders it, so a key can never exist on one side only.
 * - the **cross-tenant audit query** (`GET /platform/audit`).
 * - the **omnibox query** (`GET /platform/tenants/search`, Ctrl+K).
 */

// --------------------------------------------------------------------------- settings

/**
 * Kinds are deliberately coarse. A "kind" decides two things: how the console renders the
 * field and how the API validates the value. `string-list` is one entry per line in the
 * UI and a JSON array on the wire — the same shape `platform_settings.value` stores.
 */
export const platformSettingKinds = [
  'string',
  'email',
  'string-list',
  'integer',
  'boolean',
  /** `#rrggbb` only (P-C2 branding). */
  'color',
  /** `https://…` or an internal `/…` path (P-C2 branding). */
  'url',
] as const;
export type PlatformSettingKind = (typeof platformSettingKinds)[number];

/**
 * Where a setting may be written (P-C2 added the second scope).
 *
 * - `platform` — one row with `tenant_id IS NULL` decides for every customer. The
 *   maintenance switch is the obvious member: it is not a per-customer idea.
 * - `tenant` — one row per customer, used either as that customer's **override** of a
 *   platform default (`limits.*`) or as data that only a customer can have
 *   (`branding.*`: a logo and a sender name belong to one company).
 */
export const platformSettingScopes = ['platform', 'tenant'] as const;
export type PlatformSettingScope = (typeof platformSettingScopes)[number];

export type PlatformSettingDefinition = {
  key: string;
  /** The label the console shows — «التسمية بالنص» (P-C1 §4 of the plan). */
  labelAr: string;
  labelEn: string;
  kind: PlatformSettingKind;
  /** What the setting is for, in one Arabic sentence — shown under the field. */
  helpAr: string;
  defaultValue: string | string[] | number | boolean;
  /** Inclusive bounds for `integer` kinds. */
  min?: number;
  max?: number;
  /**
   * Writers allowed for this key. Omitted means `['platform']` — P-C1's eight keys keep
   * their original meaning without a line of churn.
   */
  scopes?: readonly PlatformSettingScope[];
};

/**
 * The settings catalogue. Every key is read by a real consumer:
 * `limits.*` are the defaults a newly provisioned tenant inherits (P-C5 enforces them),
 * `platform.maintenance*` is the switch the console flips during an upgrade window, and
 * `support.*` is what the marketing site and the console footer show,
 * `branding.*` is one customer's own look (P-C2), and
 * `billing.*` is what the platform prints on the tax invoice it issues (P-C4).
 */
export const platformSettingDefinitions: readonly PlatformSettingDefinition[] = [
  {
    key: 'support.email',
    labelAr: 'بريد الدعم',
    labelEn: 'Support email',
    kind: 'email',
    helpAr: 'العنوان الذي تُوجَّه إليه رسائل العملاء وتُذكَر في صفحة التواصل.',
    defaultValue: '',
  },
  {
    key: 'support.phone',
    labelAr: 'هاتف الدعم',
    labelEn: 'Support phone',
    kind: 'string',
    helpAr: 'رقم التواصل المعروض للعملاء، بصيغة دولية.',
    defaultValue: '',
  },
  {
    key: 'platform.domains',
    labelAr: 'نطاقات الخدمة',
    labelEn: 'Service domains',
    kind: 'string-list',
    helpAr: 'النطاقات التي يستعملها المشغّل لتشغيل الأسطح (سطر لكل نطاق).',
    defaultValue: [],
  },
  {
    key: 'limits.max_users',
    scopes: ['platform', 'tenant'],
    labelAr: 'حدّ المستخدمين الافتراضي',
    labelEn: 'Default user limit',
    kind: 'integer',
    helpAr: 'عدد المستخدمين الذي ترثه منشأة جديدة قبل أن يُضبط لها حدّ خاص.',
    defaultValue: 5,
    min: 0,
    max: 10_000,
  },
  {
    key: 'limits.max_branches',
    scopes: ['platform', 'tenant'],
    labelAr: 'حدّ الفروع الافتراضي',
    labelEn: 'Default branch limit',
    kind: 'integer',
    helpAr: 'عدد الفروع الذي ترثه منشأة جديدة قبل أن يُضبط لها حدّ خاص.',
    defaultValue: 1,
    min: 0,
    max: 10_000,
  },
  {
    key: 'limits.max_invoices_per_month',
    scopes: ['platform', 'tenant'],
    labelAr: 'حدّ الفواتير الشهرية الافتراضي',
    labelEn: 'Default monthly invoice limit',
    kind: 'integer',
    helpAr: 'عدد الفواتير في الشهر الذي ترثه منشأة جديدة قبل أن يُضبط لها حدّ خاص.',
    defaultValue: 500,
    min: 0,
    max: 10_000_000,
  },
  // --- The five limits P-C5 meters (P-C1 declared the first three) ------------------
  // Each of the eight usage metrics needs a key an operator can set, otherwise "hard at
  // 100%" would only exist for users, branches and invoices. Defaults are the envelope a
  // *new* tenant inherits; P-C5 enforces a limit once it has a source (a platform row or a
  // customer override) — see `platform/usage.ts` decision 2.
  {
    key: 'limits.max_items',
    scopes: ['platform', 'tenant'],
    labelAr: 'حدّ الأصناف الافتراضي',
    labelEn: 'Default item limit',
    kind: 'integer',
    helpAr: 'عدد الأصناف الذي ترثه منشأة جديدة قبل أن يُضبط لها حدّ خاص.',
    defaultValue: 5000,
    min: 0,
    max: 10_000_000,
  },
  {
    key: 'limits.max_storage_mb',
    scopes: ['platform', 'tenant'],
    labelAr: 'حدّ التخزين الافتراضي (م.ب)',
    labelEn: 'Default storage limit (MB)',
    kind: 'integer',
    helpAr: 'حجم الملفات المرفوعة الذي ترثه منشأة جديدة، بالميغابايت.',
    defaultValue: 2048,
    min: 0,
    max: 10_000_000,
  },
  {
    key: 'limits.max_api_calls_per_day',
    scopes: ['platform', 'tenant'],
    labelAr: 'حدّ استدعاءات الـAPI اليومي',
    labelEn: 'Default daily API-call limit',
    kind: 'integer',
    helpAr: 'عدد الطلبات في اليوم الذي ترثه منشأة جديدة؛ يُرفض ما بعده برمز USAGE_LIMIT_REACHED.',
    defaultValue: 50_000,
    min: 0,
    max: 100_000_000,
  },
  {
    key: 'limits.max_whatsapp_per_month',
    scopes: ['platform', 'tenant'],
    labelAr: 'حدّ رسائل واتساب الشهري',
    labelEn: 'Default monthly WhatsApp limit',
    kind: 'integer',
    helpAr: 'عدد الرسائل في الشهر الذي ترثه منشأة جديدة.',
    defaultValue: 1000,
    min: 0,
    max: 10_000_000,
  },
  {
    key: 'limits.max_emails_per_month',
    scopes: ['platform', 'tenant'],
    labelAr: 'حدّ إرسالات البريد الشهري',
    labelEn: 'Default monthly e-mail limit',
    kind: 'integer',
    helpAr: 'عدد رسائل البريد في الشهر؛ يُقاس من P-C6 حين يصير للمنصة مُرسِل.',
    defaultValue: 5000,
    min: 0,
    max: 10_000_000,
  },
  {
    key: 'platform.maintenance',
    labelAr: 'مفتاح الصيانة',
    labelEn: 'Maintenance switch',
    kind: 'boolean',
    helpAr: 'عند تفعيله تُغلق أسطح العمل ولا تُقبل إلا جلسات مشغّلي المنصة.',
    defaultValue: false,
  },
  {
    key: 'platform.maintenance_message',
    labelAr: 'رسالة الصيانة',
    labelEn: 'Maintenance message',
    kind: 'string',
    helpAr: 'النص المعروض للعملاء أثناء نافذة الصيانة.',
    defaultValue: '',
  },

  // --- Per-customer branding (P-C2) --------------------------------------------
  // Three keys, tenant scope only: a logo and a sender name are properties of one
  // company, not defaults. `branding.primary_color` re-uses the key the tenant settings
  // registry already declared (`packages/config/src/tenant-settings.ts`), so a colour
  // written before P-C2 reads back unchanged — the platform console simply becomes the
  // second writer of the same name, with the same `#rrggbb` contract.
  {
    key: 'branding.primary_color',
    labelAr: 'اللون الأساسي',
    labelEn: 'Primary colour',
    kind: 'color',
    helpAr: 'لون واجهة العميل بصيغة #rrggbb.',
    defaultValue: '#0f172a',
    scopes: ['tenant'],
  },
  {
    key: 'branding.logo_url',
    labelAr: 'رابط الشعار',
    labelEn: 'Logo URL',
    kind: 'url',
    helpAr: 'رابط https مباشر لشعار العميل، أو مسار داخلي يبدأ بـ/.',
    defaultValue: '',
    scopes: ['tenant'],
  },
  {
    key: 'branding.sender_name',
    labelAr: 'اسم المُرسِل',
    labelEn: 'Sender name',
    kind: 'string',
    helpAr: 'الاسم الذي تظهر به رسائل العميل وإشعاراته.',
    defaultValue: '',
    max: 60,
    scopes: ['tenant'],
  },

  // --- ما يظهر على فاتورة المنصة (P-C4) ----------------------------------------
  // ستة مفاتيح بنطاق المنصة: الفاتورة الضريبية التي تصدرها المنصة لعملائها يجب أن تحمل
  // **هوية البائع** — الاسم النظامي والرقم الضريبي والعنوان — والرقمين اللذين يشكّلان الورقة
  // (نسبة الضريبة ومهلة السداد). حقنُها في الشيفرة يطبع بائعاً لا يستطيع أحد تصحيحه بلا
  // إصدار جديد، وترحيل `platform_invoices` ينسخ هذه القيم على كل فاتورة، فتغيير مفتاحٍ لا
  // يعيد كتابة مستندٍ صادر.
  {
    key: 'billing.seller_name',
    labelAr: 'اسم البائع (للفاتورة الضريبية)',
    labelEn: 'Seller legal name',
    kind: 'string',
    helpAr: 'الاسم النظامي للمنصة كما يجب أن يظهر على الفاتورة الضريبية التي تصدرها لعملائها.',
    defaultValue: 'منصة ERP السحابية',
    max: 120,
  },
  {
    key: 'billing.seller_tax_number',
    labelAr: 'الرقم الضريبي للبائع',
    labelEn: 'Seller VAT number',
    kind: 'string',
    helpAr: 'الرقم الضريبي المكوَّن من 15 رقماً. القيمة الافتراضية رقمٌ تجريبي للعرض ويجب تغييره قبل الإنتاج.',
    defaultValue: '300000000000003',
    max: 15,
  },
  {
    key: 'billing.seller_address',
    labelAr: 'عنوان البائع',
    labelEn: 'Seller address',
    kind: 'string',
    helpAr: 'عنوان المنصة كما يُطبع تحت اسم البائع في الفاتورة.',
    defaultValue: 'الرياض، المملكة العربية السعودية',
    max: 200,
  },
  {
    key: 'billing.tax_rate',
    labelAr: 'نسبة ضريبة القيمة المضافة',
    labelEn: 'VAT rate',
    kind: 'integer',
    helpAr: 'النسبة المئوية المطبَّقة على فواتير الاشتراكات (15٪ هي النسبة النظامية في السعودية).',
    defaultValue: 15,
    min: 0,
    max: 100,
  },
  {
    key: 'billing.payment_terms_days',
    labelAr: 'مهلة السداد (أيام)',
    labelEn: 'Payment terms (days)',
    kind: 'integer',
    helpAr: 'عدد الأيام من تاريخ الإصدار إلى تاريخ الاستحقاق عند إصدار فاتورة اشتراك.',
    defaultValue: 14,
    min: 0,
    max: 180,
  },
  {
    key: 'billing.dunning_days',
    labelAr: 'أيام متابعة التحصيل',
    labelEn: 'Dunning ladder (days)',
    kind: 'string-list',
    helpAr: 'الأيام التي تُجدول فيها محاولة متابعة بعد الاستحقاق (سطر لكل يوم: 0 ثم 3 ثم 7).',
    defaultValue: ['0', '3', '7'],
  },
] as const;

/**
 * يُقرأ الرقم الضريبي للبائع من الإعدادات — ويُتحقّق منه هنا لا في الشاشة: 15 رقماً كما
 * تشترط هيئة الزكاة والضريبة والجمارك، أو النص الفارغ (منصة لم تُسجَّل بعد).
 */
export function isSellerTaxNumber(value: string): boolean {
  return value.length === 0 || /^\d{15}$/.test(value);
}

/** سلّم المتابعة بالأيام: الأعداد الصحيحة الموجبة فقط، مرتَّبةً ومُزالة التكرار. */
export function billingDunningLadder(value: readonly string[]): number[] {
  const days = value
    .map((entry) => Number(entry.trim()))
    .filter((entry) => Number.isInteger(entry) && entry >= 0)
    .sort((left, right) => left - right);
  return [...new Set(days)];
}

export const platformSettingKeySchema = z.string().refine(
  (key) => platformSettingDefinitions.some((definition) => definition.key === key),
  { message: 'Unknown platform setting key' },
);

export function findPlatformSettingDefinition(
  key: string,
): PlatformSettingDefinition | undefined {
  return platformSettingDefinitions.find((definition) => definition.key === key);
}

/** The writers a key allows. A definition without `scopes` is platform-only (P-C1 rule). */
export function platformSettingScopesOf(key: string): readonly PlatformSettingScope[] {
  return findPlatformSettingDefinition(key)?.scopes ?? ['platform'];
}

export function platformSettingAllowsScope(key: string, scope: PlatformSettingScope): boolean {
  return platformSettingScopesOf(key).includes(scope);
}

/**
 * The catalogue a screen shows for one scope. `GET /platform/settings` renders the
 * platform-scoped half; `GET /platform/tenants/:id/settings` renders the tenant-scoped
 * half — the same definitions, so a key can never be editable in the wrong plane.
 */
export function platformSettingsForScope(scope: PlatformSettingScope): PlatformSettingDefinition[] {
  return platformSettingDefinitions.filter((definition) => platformSettingAllowsScope(definition.key, scope));
}

/** The value a setting takes when it has never been written. */
export function defaultPlatformSettingValue(key: string): string | string[] | number | boolean {
  return findPlatformSettingDefinition(key)?.defaultValue ?? '';
}

export function platformSettingDefaultMap(): Record<string, string | string[] | number | boolean> {
  return Object.fromEntries(
    platformSettingDefinitions.map((definition) => [definition.key, definition.defaultValue]),
  );
}

export const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/**
 * Validates one value against its definition. Returns a human-readable Arabic reason when
 * the value is rejected, so the API can answer 422 with the same words the screen shows.
 */
export function validatePlatformSettingValue(
  key: string,
  value: unknown,
): { ok: true; value: string | string[] | number | boolean } | { ok: false; reason: string } {
  const definition = findPlatformSettingDefinition(key);
  if (!definition) return { ok: false, reason: `مفتاح إعداد غير معروف: ${key}` };

  switch (definition.kind) {
    case 'string': {
      if (typeof value !== 'string') return { ok: false, reason: 'القيمة يجب أن تكون نصاً' };
      const text = value.trim();
      if (definition.max !== undefined && text.length > definition.max) {
        return { ok: false, reason: `أقصى طول مسموح ${definition.max} حرفاً` };
      }
      return { ok: true, value: text };
    }
    case 'color': {
      if (typeof value !== 'string') return { ok: false, reason: 'القيمة يجب أن تكون نصاً' };
      const color = value.trim().toLowerCase();
      if (!/^#[0-9a-f]{6}$/.test(color)) {
        return { ok: false, reason: 'اللون يجب أن يكون بصيغة #rrggbb' };
      }
      return { ok: true, value: color };
    }
    case 'url': {
      if (typeof value !== 'string') return { ok: false, reason: 'القيمة يجب أن تكون نصاً' };
      const url = value.trim();
      // Empty is allowed: a customer with no logo yet keeps the platform's default.
      if (url.length === 0) return { ok: true, value: '' };
      if (url.startsWith('/') && !url.startsWith('//') && !/\s/.test(url)) return { ok: true, value: url };
      if (/^https:\/\/[^\s]+$/i.test(url)) return { ok: true, value: url };
      return { ok: false, reason: 'الرابط يجب أن يبدأ بـhttps:// أو بمسار داخلي /' };
    }
    case 'email': {
      if (typeof value !== 'string') return { ok: false, reason: 'القيمة يجب أن تكون نصاً' };
      const email = value.trim();
      // Empty is allowed: "no support address configured yet" is a real state.
      if (email.length > 0 && !EMAIL_PATTERN.test(email)) {
        return { ok: false, reason: 'صيغة البريد الإلكتروني غير صحيحة' };
      }
      return { ok: true, value: email };
    }
    case 'string-list': {
      if (!Array.isArray(value)) return { ok: false, reason: 'القيمة يجب أن تكون قائمة نصية' };
      const entries = value.map((entry) => String(entry).trim()).filter((entry) => entry.length > 0);
      if (entries.some((entry) => entry.length > 253)) {
        return { ok: false, reason: 'طول النطاق يتجاوز الحد المسموح' };
      }
      return { ok: true, value: [...new Set(entries)] };
    }
    case 'integer': {
      // `null` ليست صفراً: `Number(null) === 0` كان يكتب «حدّ صفر» لمن يرسل قيمةً فارغة.
      // ولمّا صار P-C5 يطبّق الحدود، صار ذلك يعني حجب العميل بدل تجاهل الكتابة — فيُرفض الفراغ.
      if (value === null || value === undefined || value === '') {
        return { ok: false, reason: 'القيمة يجب أن تكون عدداً صحيحاً (صفر يعني حدّاً صفرياً مقصوداً)' };
      }
      const numeric = typeof value === 'number' ? value : Number(value);
      if (!Number.isInteger(numeric)) return { ok: false, reason: 'القيمة يجب أن تكون عدداً صحيحاً' };
      if (definition.min !== undefined && numeric < definition.min) {
        return { ok: false, reason: `أصغر قيمة مسموحة ${definition.min}` };
      }
      if (definition.max !== undefined && numeric > definition.max) {
        return { ok: false, reason: `أكبر قيمة مسموحة ${definition.max}` };
      }
      return { ok: true, value: numeric };
    }
    case 'boolean':
      if (typeof value !== 'boolean') return { ok: false, reason: 'القيمة يجب أن تكون نعم/لا' };
      return { ok: true, value };
    default:
      return { ok: false, reason: 'نوع إعداد غير مدعوم' };
  }
}

/** One row of `GET /platform/settings`: the catalogue entry plus its effective value. */
export const platformSettingViewSchema = z.object({
  key: z.string(),
  labelAr: z.string(),
  labelEn: z.string(),
  kind: z.enum(platformSettingKinds),
  helpAr: z.string(),
  value: z.union([z.string(), z.array(z.string()), z.number(), z.boolean()]),
  /** True when the row comes from the catalogue, not from a `platform_settings` row. */
  isDefault: z.boolean(),
  /**
   * Which row answered (P-C2). `platform` is the inherited value an override replaces;
   * for a platform-scoped read `source` is `default` or `platform` only.
   */
  source: z.enum(['default', 'platform', 'tenant']).default('default'),
  updatedAt: z.string().nullable(),
  updatedBy: z.string().nullable(),
});

export type PlatformSettingView = z.infer<typeof platformSettingViewSchema>;

/** `GET /platform/settings` — the eight rows plus the deployment this API is running as. */
export const platformSettingsResponseSchema = z.object({
  settings: z.array(platformSettingViewSchema),
  environment: z.object({
    name: z.string(),
    labelAr: z.string(),
  }),
});

export type PlatformSettingsResponse = z.infer<typeof platformSettingsResponseSchema>;

/**
 * `PUT /platform/settings` — a partial map of key → value. Partial on purpose: the screen
 * saves the fields the operator touched, and a deployment that already stored a key the
 * console no longer renders keeps working.
 */
export const platformSettingsUpdateSchema = z.object({
  values: z.record(z.unknown()),
});

export type PlatformSettingsUpdate = z.infer<typeof platformSettingsUpdateSchema>;

// --------------------------------------------------------------------------- audit

/** Filters of `GET /platform/audit` — the cross-tenant twin of `GET /audit-log`. */
export const PLATFORM_AUDIT_FILTERS = [
  'tenantId',
  'actorUserId',
  'action',
  'entity',
  'entityId',
  'from',
  'to',
] as const;

export const platformAuditQuerySchema = paginationQuerySchema.extend({
  sort: z.string().trim().max(200).optional(),
  filter: z.record(z.union([z.string(), z.array(z.string())])).optional(),
});

export type PlatformAuditQueryDto = z.infer<typeof platformAuditQuerySchema>;

/** One audit row **plus its customer**, which is the whole point of the cross-tenant read. */
export const platformAuditEntrySchema = z.object({
  id: uuidSchema,
  tenantId: uuidSchema.nullable(),
  tenantCode: z.string().nullable(),
  tenantName: z.string().nullable(),
  actorUserId: uuidSchema.nullable(),
  actorLabel: z.string().nullable(),
  action: z.string(),
  entity: z.string(),
  entityId: z.string().nullable(),
  before: z.unknown().nullable(),
  after: z.unknown().nullable(),
  meta: z.record(z.unknown()),
  createdAt: z.string(),
});

export type PlatformAuditEntry = z.infer<typeof platformAuditEntrySchema>;

// --------------------------------------------------------------------------- omnibox

/** `GET /platform/tenants/search?q=` — the Ctrl+K provider. */
export const platformTenantSearchQuerySchema = z.object({
  q: z.string().trim().min(1).max(120),
});

export type PlatformTenantSearchQueryDto = z.infer<typeof platformTenantSearchQuerySchema>;

export const platformTenantSearchResultSchema = z.object({
  id: uuidSchema,
  code: z.string(),
  name: z.string(),
  status: z.string(),
});

export type PlatformTenantSearchResult = z.infer<typeof platformTenantSearchResultSchema>;
