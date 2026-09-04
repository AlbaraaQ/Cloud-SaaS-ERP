import { z } from 'zod';

/**
 * Typed tenant-settings registry.
 *
 * MULTI_TENANCY §5: `tenant_settings(tenant_id, key, value jsonb)` with *typed keys in
 * `packages/config/tenant-settings.ts`*. Every key declares its zod schema, default and
 * owning module, which is what `PUT /settings/{key}` validates against (PHASE_03 §5.5).
 *
 * Keys are dot-namespaced by module. The five flat keys created in Phase 01 are kept
 * (AI_DEVELOPMENT_PROTOCOL §2 forbids silent deletion) and mapped into the registry.
 */

export type TenantSettingDefinition = {
  readonly key: string;
  readonly module: string;
  readonly description: string;
  readonly schema: z.ZodTypeAny;
  readonly defaultValue: string | boolean | number | null;
};

function define(
  key: string,
  module: string,
  description: string,
  schema: z.ZodTypeAny,
  defaultValue: string | boolean | number | null,
): TenantSettingDefinition {
  return { key, module, description, schema, defaultValue };
}

export const tenantSettingsRegistry: readonly TenantSettingDefinition[] = [
  // --- Phase 01 keys (retained) -------------------------------------------------
  define(
    'allow_negative_stock',
    'inventory',
    'Allow posting documents that drive stock below zero.',
    z.boolean(),
    false,
  ),
  define(
    'default_currency_code',
    'organization',
    'ISO-4217 code used when a document omits one.',
    z.string().length(3),
    'SAR',
  ),
  define(
    'fiscal_year_start_month',
    'accounting',
    'Month (1-12) the fiscal year starts in.',
    z.number().int().min(1).max(12),
    1,
  ),
  define(
    'timezone',
    'organization',
    'Legacy flat timezone key; `locale.timezone` is authoritative.',
    z.string(),
    'UTC',
  ),
  define(
    'branding.primary_color',
    'branding',
    'Primary brand colour as #rrggbb.',
    z.string().regex(/^#[0-9a-fA-F]{6}$/),
    '#0f172a',
  ),

  // --- Invoicing / money --------------------------------------------------------
  define(
    'invoice.number_prefix',
    'sales',
    'Prefix for the sales invoice document sequence.',
    z.string().min(1).max(12),
    'INV-',
  ),
  define(
    'invoice.padding',
    'sales',
    'Zero-padding width of the sales invoice sequence.',
    z.number().int().min(1).max(20),
    6,
  ),
  define(
    'money.rounding_digits',
    'accounting',
    'Currency minor units used for rounding (legacy `DigitsNo`).',
    z.number().int().min(0).max(6),
    2,
  ),
  define(
    'pricing.price_includes_vat',
    'sales',
    'Default for `price_includes_vat` on new documents.',
    z.boolean(),
    false,
  ),
  define(
    'vat.default_rate_percent',
    'accounting',
    'Default VAT percentage applied when a line has no tax group.',
    z.number().min(0).max(100),
    15,
  ),

  // --- Locale ------------------------------------------------------------------
  define(
    'locale.timezone',
    'organization',
    'IANA timezone used to resolve business dates (tenants.timezone).',
    z.string(),
    'Asia/Riyadh',
  ),
  define('locale.code', 'organization', 'BCP-47 locale code for rendering.', z.string(), 'ar'),

  // --- Vertical pack feature flags (MULTI_TENANCY §5) ---------------------------
  define('feature.pos', 'pos', 'Restaurant/retail POS pack.', z.boolean(), false),
  define('feature.projects', 'projects', 'Projects & contracting pack.', z.boolean(), false),
  define('feature.hrm', 'hrm', 'Human resources & payroll pack.', z.boolean(), false),
  define('feature.niche', 'niche', 'Niche verticals pack (optical, marine, vehicles).', z.boolean(), false),
] as const;

const registryByKey = new Map(tenantSettingsRegistry.map((entry) => [entry.key, entry]));

export function findTenantSetting(key: string): TenantSettingDefinition | undefined {
  return registryByKey.get(key);
}

export function isTenantSettingKey(key: string): boolean {
  return registryByKey.has(key);
}

/** Parses and validates a raw setting value against the registry. Throws on unknown key. */
export function parseTenantSettingValue(key: string, value: unknown): string | boolean | number | null {
  const definition = registryByKey.get(key);
  if (!definition) {
    throw new Error(`Unknown tenant setting key: ${key}`);
  }
  if (value === null || value === undefined) return definition.defaultValue;
  return definition.schema.parse(value) as string | boolean | number | null;
}

export type TenantSettingKey = (typeof tenantSettingsRegistry)[number]['key'];

export type TenantSettingsMap = Record<string, string | boolean | number | null>;

/** @deprecated use `tenantSettingsRegistry` — kept for Phase-01 API compatibility. */
export const tenantSettingKeys: readonly string[] = tenantSettingsRegistry.map((entry) => entry.key);

/** @deprecated use `tenantSettingsRegistry` — kept for Phase-01 API compatibility. */
export const tenantSettingsDefaults: Record<string, string | boolean | number | null> = Object.fromEntries(
  tenantSettingsRegistry.map((entry) => [entry.key, entry.defaultValue]),
);

/** Effective settings for a tenant: registry defaults overridden by stored rows. */
export function resolveTenantSettings(
  stored:
    ReadonlyMap<string, string | boolean | number | null> | Record<string, string | boolean | number | null>,
): TenantSettingsMap {
  const resolved: TenantSettingsMap = {};
  for (const definition of tenantSettingsRegistry) {
    const override =
      stored instanceof Map
        ? stored.get(definition.key)
        : (stored as Record<string, unknown>)[definition.key];
    resolved[definition.key] = override === undefined ? definition.defaultValue : override;
  }
  return resolved;
}
