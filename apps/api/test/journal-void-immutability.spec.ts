import crypto from 'node:crypto';

import { Client } from 'pg';
import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { runMigrations, runMigrationsDown } from '@erp/database';

import { OrgProvisioningService } from '../src/modules/organization/index.js';

import { createActor, type Actor } from './fixtures.js';
import { createTestApp, type TestApp } from './test-app.js';

/**
 * The posted-journal trigger lives in SQL rather than the accounting service, so this
 * round trip deliberately talks to PostgreSQL directly. It protects the very narrow
 * transition used by `AccountingService.voidJournalInTx`: status may become `void`, but
 * the posted entry cannot be rewritten as part of that transition.
 */
describe('journal void immutability migration (0038)', () => {
  let ctx: TestApp;
  let actor: Actor;
  let branchId = '';
  let fiscalPeriodId = '';

  beforeAll(async () => {
    ctx = await createTestApp('journal-void-immutability');
    actor = await createActor(ctx, {
      tenantCode: 'journal-void',
      email: 'admin@journal-void.test',
    });

    const defaults = await ctx.app.get(OrgProvisioningService).provisionOrgDefaults(actor.tenantId);
    branchId = defaults.branchId;

    const fiscalYearId = crypto.randomUUID();
    fiscalPeriodId = crypto.randomUUID();
    const client = new Client({ connectionString: ctx.db.ownerUrl });
    await client.connect();
    try {
      await client.query(
        `INSERT INTO fiscal_years (id, tenant_id, name, start_date, end_date, status)
         VALUES ($1, $2, 'FY 2026', '2026-01-01', '2026-12-31', 'open')`,
        [fiscalYearId, actor.tenantId],
      );
      await client.query(
        `INSERT INTO fiscal_periods (id, tenant_id, fiscal_year_id, name, start_date, end_date, status)
         VALUES ($1, $2, $3, 'January 2026', '2026-01-01', '2026-01-31', 'open')`,
        [fiscalPeriodId, actor.tenantId, fiscalYearId],
      );
    } finally {
      await client.end();
    }
  }, 240_000);

  afterAll(async () => {
    await ctx.close();
  });

  async function insertPostedJournal(client: Client): Promise<string> {
    const id = crypto.randomUUID();
    await client.query(
      `INSERT INTO journal_entries (
         id, tenant_id, branch_id, fiscal_period_id, date, number, kind, status,
         description, source_type, source_id, posted_at, posted_by, created_by
       ) VALUES ($1, $2, $3, $4, '2026-01-15', 'JV-IMMUTABLE', 'auto', 'posted',
                 'قيد مرحّل للاختبار', 'inventory_document', $5, now(), $6, $6)`,
      [id, actor.tenantId, branchId, fiscalPeriodId, crypto.randomUUID(), actor.userId],
    );
    return id;
  }

  it('rolls 0038 down and up, then allows only an untampered posted-to-void transition', async () => {
    const down = await runMigrationsDown(ctx.db.ownerUrl);
    expect(down.applied).toEqual(['0038_journal_void_immutability.down.sql']);

    const client = new Client({ connectionString: ctx.db.ownerUrl });
    await client.connect();
    try {
      // The legacy function returned OLD, which made an apparently-successful void a
      // no-op. This is the exact production defect migration 0038 repairs.
      const legacyId = await insertPostedJournal(client);
      await client.query(`UPDATE journal_entries SET status = 'void' WHERE id = $1`, [legacyId]);
      const legacyStatus = await client.query<{ status: string }>('SELECT status FROM journal_entries WHERE id = $1', [legacyId]);
      expect(legacyStatus.rows[0]?.status).toBe('posted');

      const up = await runMigrations(ctx.db.ownerUrl);
      expect(up.applied).toEqual(['0038_journal_void_immutability.sql']);

      const protectedId = await insertPostedJournal(client);
      await expect(
        client.query(`UPDATE journal_entries SET status = 'void', date = '2026-01-16' WHERE id = $1`, [protectedId]),
      ).rejects.toMatchObject({ code: '42501' });

      const afterRejectedMutation = await client.query<{ status: string; date: string }>(
        'SELECT status, date::text AS date FROM journal_entries WHERE id = $1',
        [protectedId],
      );
      expect(afterRejectedMutation.rows[0]).toMatchObject({ status: 'posted', date: '2026-01-15' });

      await client.query(
        `UPDATE journal_entries
            SET status = 'void', updated_at = now(), updated_by = $2, version = version + 1
          WHERE id = $1`,
        [protectedId, actor.userId],
      );
      const voided = await client.query<{ status: string; version: number }>(
        'SELECT status, version FROM journal_entries WHERE id = $1',
        [protectedId],
      );
      expect(voided.rows[0]).toMatchObject({ status: 'void', version: 2 });
    } finally {
      await client.end();
    }
  });
});
