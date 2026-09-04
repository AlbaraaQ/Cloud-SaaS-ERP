import { z } from 'zod';

/**
 * Environment schema — SECURITY_ARCHITECTURE §9 ("Secrets via env only"),
 * TARGET_ARCHITECTURE §5 ("packages/config zod-validates at boot (fail-fast)").
 *
 * Every variable is optional at *import* time so that type-checking, linting and unit
 * tests never depend on a populated environment. `assertRuntimeEnv()` performs the
 * fail-fast validation that `apps/api` runs during bootstrap.
 */

const booleanish = z
  .union([z.boolean(), z.enum(['true', 'false', '1', '0'])])
  .transform((value) => (typeof value === 'boolean' ? value : value === 'true' || value === '1'));

const csv = z.string().transform((value) =>
  value
    .split(',')
    .map((entry) => entry.trim())
    .filter((entry) => entry.length > 0),
);

const envSchema = z.object({
  NODE_ENV: z.enum(['development', 'test', 'production']).default('development'),
  PORT: z.coerce.number().int().positive().default(3000),
  API_HOST: z.string().default('0.0.0.0'),

  DATABASE_URL: z.string().optional(),
  DATABASE_POOL_MAX: z.coerce.number().int().positive().max(100).default(10),
  DATABASE_APP_ROLE: z.string().default('erp_api'),
  DATABASE_MIGRATOR_ROLE: z.string().default('erp_migrator'),
  DATABASE_MIGRATOR_URL: z.string().optional(),
  /** Used only by `pnpm db:roles`; never read at runtime by the API. */
  DATABASE_APP_PASSWORD: z.string().optional(),
  DATABASE_MIGRATOR_PASSWORD: z.string().optional(),

  REDIS_URL: z.string().optional(),

  /** PEM encoded RS256 key pair. Literal `\n` sequences are normalised to newlines. */
  JWT_PRIVATE_KEY: z.string().optional(),
  JWT_PUBLIC_KEY: z.string().optional(),
  JWT_KEY_ID: z.string().default('dev-key-1'),
  /** Frozen: PROJECT_CONTRACT §9 — access 15 min, refresh 30 d. */
  JWT_ACCESS_TTL_SECONDS: z.coerce.number().int().positive().default(900),
  JWT_REFRESH_TTL_SECONDS: z.coerce.number().int().positive().default(2_592_000),

  /** Frozen: PROJECT_CONTRACT §9 / SECURITY_ARCHITECTURE §2 — Argon2id m=64MiB t=3 p=4. */
  AUTH_ARGON2_MEMORY_KIB: z.coerce.number().int().positive().default(65_536),
  AUTH_ARGON2_TIME_COST: z.coerce.number().int().positive().default(3),
  AUTH_ARGON2_PARALLELISM: z.coerce.number().int().positive().default(4),
  AUTH_ARGON2_OUTPUT_LENGTH: z.coerce.number().int().positive().default(32),
  /** SECURITY_ARCHITECTURE §2 — exponential lockout on repeated failures. */
  AUTH_LOGIN_MAX_FAILURES: z.coerce.number().int().positive().default(5),
  AUTH_LOCKOUT_MINUTES: z.coerce.number().int().positive().default(15),

  /** SECURITY_ARCHITECTURE §8 — token buckets. */
  RATE_LIMIT_DEFAULT_PER_MINUTE: z.coerce.number().int().positive().default(600),
  RATE_LIMIT_LOGIN_PER_MINUTE: z.coerce.number().int().positive().default(10),
  RATE_LIMIT_REGISTER_PER_MINUTE: z.coerce.number().int().positive().default(5),

  CORS_ALLOWED_ORIGINS: csv.default(''),
  OPENAPI_ENABLED: booleanish.default(true),

  S3_ENDPOINT: z.string().optional(),
  S3_BUCKET: z.string().optional(),
  S3_ACCESS_KEY_ID: z.string().optional(),
  S3_SECRET_ACCESS_KEY: z.string().optional(),

  /** AES-256-GCM data-encryption key, base64 (SECURITY_ARCHITECTURE §9). */
  DATA_ENC_KEY: z.string().optional(),
});

const parsed = envSchema.safeParse(process.env);

export const env: AppEnv = parsed.success ? parsed.data : envSchema.parse({});

export type AppEnv = z.infer<typeof envSchema>;

/** Normalises PEM material that arrived through an environment variable. */
export function normalisePem(value: string | undefined): string | undefined {
  if (!value || value.trim().length === 0) return undefined;
  if (value.includes('-----BEGIN')) return value;
  const withNewlines = value.replace(/\\n/g, '\n').trim();
  return withNewlines.includes('-----BEGIN') ? withNewlines : undefined;
}

/**
 * Fail-fast boot validation (PHASE_02 §8). Call once during application bootstrap.
 * Missing values throw with a single aggregated message and never leak the value.
 */
export function assertRuntimeEnv(candidate: AppEnv = env): AppEnv {
  const missing: string[] = [];
  if (!candidate.DATABASE_URL) missing.push('DATABASE_URL');
  if (!normalisePem(candidate.JWT_PRIVATE_KEY)) missing.push('JWT_PRIVATE_KEY');
  if (!normalisePem(candidate.JWT_PUBLIC_KEY)) missing.push('JWT_PUBLIC_KEY');
  if (candidate.NODE_ENV === 'production' && !candidate.DATA_ENC_KEY) missing.push('DATA_ENC_KEY');

  if (missing.length > 0) {
    throw new Error(`Invalid environment: missing required variable(s) ${missing.join(', ')}`);
  }
  return candidate;
}

/**
 * Values that must never reach a log line (PROJECT_CONTRACT §10, PHASE_02 §5.2:
 * "redact password|secret|token|key paths"). Both the bare and the nested form are
 * listed because pino treats `password` and `*.password` as different paths.
 */
export const REDACTED_LOG_PATHS = [
  'req.headers.authorization',
  'req.headers.cookie',
  'req.headers["x-branch-id"]',
  'password',
  '*.password',
  'passwordHash',
  '*.passwordHash',
  'current',
  '*.current',
  'new',
  '*.new',
  'accessToken',
  '*.accessToken',
  'refreshToken',
  '*.refreshToken',
  'token',
  '*.token',
  'tokenHash',
  '*.tokenHash',
  'mfaCode',
  '*.mfaCode',
  'mfaSecretEnc',
  '*.mfaSecretEnc',
  'secret',
  '*.secret',
  'key',
  '*.key',
] as const;
