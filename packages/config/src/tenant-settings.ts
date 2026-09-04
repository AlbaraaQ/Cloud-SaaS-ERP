export type TenantSettingKey =
  | 'allow_negative_stock'
  | 'default_currency_code'
  | 'fiscal_year_start_month'
  | 'timezone'
  | 'branding.primary_color';

export const tenantSettingKeys: readonly TenantSettingKey[] = [
  'allow_negative_stock',
  'default_currency_code',
  'fiscal_year_start_month',
  'timezone',
  'branding.primary_color',
] as const;

export type TenantSettingsMap = Record<TenantSettingKey, string | boolean | number | null>;

export const tenantSettingsDefaults: TenantSettingsMap = {
  allow_negative_stock: false,
  default_currency_code: 'SAR',
  fiscal_year_start_month: 1,
  timezone: 'UTC',
  'branding.primary_color': '#0f172a',
};
