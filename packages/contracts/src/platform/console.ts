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
export const platformSettingKinds = ['string', 'email', 'string-list', 'integer', 'boolean'] as const;
export type PlatformSettingKind = (typeof platformSettingKinds)[number];

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
};

/**
 * The eight settings P-C1 declares. Every key is read by a real consumer:
 * `limits.*` are the defaults a newly provisioned tenant inherits (P-C5 enforces them),
 * `platform.maintenance*` is the switch the console flips during an upgrade window, and
 * `support.*` is what the marketing site and the console footer show.
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
    labelAr: 'حدّ الفواتير الشهرية الافتراضي',
    labelEn: 'Default monthly invoice limit',
    kind: 'integer',
    helpAr: 'عدد الفواتير في الشهر الذي ترثه منشأة جديدة قبل أن يُضبط لها حدّ خاص.',
    defaultValue: 500,
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
] as const;

export const platformSettingKeySchema = z.string().refine(
  (key) => platformSettingDefinitions.some((definition) => definition.key === key),
  { message: 'Unknown platform setting key' },
);

export function findPlatformSettingDefinition(
  key: string,
): PlatformSettingDefinition | undefined {
  return platformSettingDefinitions.find((definition) => definition.key === key);
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
    case 'string':
      if (typeof value !== 'string') return { ok: false, reason: 'القيمة يجب أن تكون نصاً' };
      return { ok: true, value: value.trim() };
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
