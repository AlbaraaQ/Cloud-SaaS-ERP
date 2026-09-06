import { sql } from 'drizzle-orm';
import { drizzle } from 'drizzle-orm/node-postgres';
import { Pool } from 'pg';
import { env } from '@erp/config';
let dbInstance;
export function createDbClient(connectionString = env.DATABASE_URL ?? 'postgres://app:app-dev-password@localhost:5432/app') {
    const pool = new Pool({ connectionString });
    return drizzle(pool, { schema: {} });
}
export function getDb() {
    if (!dbInstance) {
        dbInstance = createDbClient();
    }
    return dbInstance;
}
// eslint-disable-next-line no-unused-vars
export async function withTx(work) {
    const client = getDb();
    return client.transaction((tx) => work(tx));
}
export function baseAuditColumns() {
    return {
        createdAt: sql `TIMESTAMPTZ NOT NULL DEFAULT NOW()`,
        createdBy: sql `UUID NULL`,
        updatedAt: sql `TIMESTAMPTZ NULL`,
        updatedBy: sql `UUID NULL`,
        deletedAt: sql `TIMESTAMPTZ NULL`,
        deletedBy: sql `UUID NULL`,
        version: sql `INTEGER NOT NULL DEFAULT 1`,
    };
}
export function baseTenantIdColumn() {
    return {
        tenantId: sql `UUID NOT NULL`,
    };
}
export function setTenantContext(tx, tenantId) {
    return tx.execute(sql `SELECT set_config('app.tenant_id', ${tenantId}, true)`);
}
