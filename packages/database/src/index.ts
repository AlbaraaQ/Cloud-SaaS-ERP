/**
 * `@erp/database` — Drizzle schema, client factory, transaction + RLS helpers,
 * migration runner and the platform + demo seeds. Public surface documented in
 * `packages/database/README.md`.
 */
export * from './client.js';
export * from './columns.js';
export * from './ids.js';
export * from './rls.js';
export * from './schema/index.js';

export {
  MIGRATIONS_TABLE,
  checksumOf,
  configureDatabaseRoles,
  migrationsDirectory,
  quoteIdent,
  readMigrationFiles,
  runMigrations,
  runMigrationsDown,
} from './migrate.js';
export type { MigrationFile, MigrationLogger, MigrationOutcome } from './migrate.js';

export { DEMO_TENANT_CODE, seedPermissionRegistry, seedPlatform } from './seed.js';
export type { SeedOptions, SeedReport } from './seed.js';
export { DEMO_CHART_OF_ACCOUNTS, DEMO_PLANS, seedDemoData } from './seed-demo.js';
export type { DemoSeedOptions, DemoSeedReport, DemoUserSpec } from './seed-demo.js';
