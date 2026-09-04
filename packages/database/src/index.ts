import { sql } from 'drizzle-orm';
import { drizzle, type NodePgDatabase } from 'drizzle-orm/node-postgres';
import { Pool } from 'pg';

import { env } from '@erp/config';

export type DrizzleDb = NodePgDatabase<Record<string, never>>;

let dbInstance: DrizzleDb | undefined;

export function createDbClient(connectionString = env.DATABASE_URL ?? 'postgres://app:app-dev-password@localhost:5432/app') {
  const pool = new Pool({ connectionString });
  return drizzle(pool, { schema: {} });
}

export function getDb(): DrizzleDb {
  if (!dbInstance) {
    dbInstance = createDbClient();
  }
  return dbInstance;
}

export async function withTx<T>(work: (tx: DrizzleDb) => Promise<T>): Promise<T> {
  const client = getDb();
  return client.transaction(async (tx) => work(tx as unknown as DrizzleDb));
}

export function baseAuditColumns() {
  return {
    createdAt: sql`TIMESTAMPTZ NOT NULL DEFAULT NOW()`,
    createdBy: sql`UUID NULL`,
    updatedAt: sql`TIMESTAMPTZ NULL`,
    updatedBy: sql`UUID NULL`,
    deletedAt: sql`TIMESTAMPTZ NULL`,
    deletedBy: sql`UUID NULL`,
    version: sql`INTEGER NOT NULL DEFAULT 1`,
  };
}

export function baseTenantIdColumn() {
  return {
    tenantId: sql`UUID NOT NULL`,
  };
}

export function setTenantContext(tx: DrizzleDb, tenantId: string) {
  return tx.execute(sql`SELECT set_config('app.tenant_id', ${tenantId}, true)`);
}
