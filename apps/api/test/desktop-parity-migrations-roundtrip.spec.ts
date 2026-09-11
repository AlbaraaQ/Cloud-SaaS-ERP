import crypto from 'node:crypto';

import { Client } from 'pg';
import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { runMigrations, runMigrationsDown } from '@erp/database';

import { OrgProvisioningService } from '../src/modules/organization/index.js';

import {
  ALL_ORGANIZATION_PERMISSIONS,
  ALL_PLATFORM_PERMISSIONS,
  createActor,
  type Actor,
} from './fixtures.js';
import { api } from './http.js';
import { createTestApp, type TestApp } from './test-app.js';

/**
 * 0034–0037 add deliberately additive structures, but their down files still need to
 * remove only those structures and allow the exact same database to be migrated again.
 * The migration runner can only roll back consecutive files, so 0038 is rolled back and
 * re-applied as the harmless top-of-stack companion; its trigger semantics have their
 * own focused test in journal-void-immutability.spec.ts.
 */
describe('desktop-parity migration round trip (0034–0037)', () => {
  let ctx: TestApp;
  let actor: Actor;
  let branchId = '';
  let warehouseId = '';

  const data = <T>(body: Record<string, unknown>) => (body.data ?? body) as T;

  beforeAll(async () => {
    ctx = await createTestApp('desktop-parity-migration-roundtrip');
    actor = await createActor(ctx, {
      tenantCode: 'migration-roundtrip',
      email: 'admin@migration-roundtrip.test',
      permissions: [
        ...ALL_PLATFORM_PERMISSIONS,
        ...ALL_ORGANIZATION_PERMISSIONS,
        'catalog.item.view',
        'catalog.item.manage',
        'catalog.category.manage',
        'catalog.unit.manage',
      ],
    });
    const defaults = await ctx.app.get(OrgProvisioningService).provisionOrgDefaults(actor.tenantId);
    branchId = defaults.branchId;
    warehouseId = defaults.warehouseId;
  }, 240_000);

  afterAll(async () => {
    await ctx.close();
  });

  it('rolls the additive schemas down and back up while preserving prior tenant data', async () => {
    const down = await runMigrationsDown(ctx.db.ownerUrl, { steps: 5 });
    expect(down.applied).toEqual([
      '0038_journal_void_immutability.down.sql',
      '0037_catalog_scan_code_integrity.down.sql',
      '0036_shift_cash_variance_journal.down.sql',
      '0035_inventory_document_line_traceability.down.sql',
      '0034_desktop_parity_followups_and_inventory.down.sql',
    ]);

    const beforeUp = new Client({ connectionString: ctx.db.ownerUrl });
    await beforeUp.connect();
    try {
      const relations = await beforeUp.query<{
        inventory_documents: string | null;
        inventory_document_lines: string | null;
        pos_held_tickets: string | null;
        pos_shortcut_items: string | null;
      }>(`
        SELECT to_regclass('public.inventory_documents')::text AS inventory_documents,
               to_regclass('public.inventory_document_lines')::text AS inventory_document_lines,
               to_regclass('public.pos_held_tickets')::text AS pos_held_tickets,
               to_regclass('public.pos_shortcut_items')::text AS pos_shortcut_items
      `);
      expect(relations.rows[0]).toEqual({
        inventory_documents: null,
        inventory_document_lines: null,
        pos_held_tickets: null,
        pos_shortcut_items: null,
      });

      const removedColumns = await beforeUp.query<{ table_name: string; column_name: string }>(`
        SELECT table_name, column_name
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND (table_name, column_name) IN (
            ('sales_invoices', 'cashier_id'),
            ('sales_invoices', 'tax_type'),
            ('inventory_transactions', 'unit_id'),
            ('stock_transfer_lines', 'unit_id'),
            ('sales_invoice_lines', 'serial_ids'),
            ('purchase_invoice_lines', 'serial_ids'),
            ('shift_closes', 'journal_entry_id'),
            ('item_barcodes', 'source')
          )
      `);
      expect(removedColumns.rows).toEqual([]);

      // This row predates every migration under test. A rollback of additive structures
      // must not disturb the tenant, its default branch, or its default warehouse.
      const tenant = await beforeUp.query<{ code: string }>('SELECT code FROM tenants WHERE id = $1', [actor.tenantId]);
      expect(tenant.rows).toEqual([{ code: 'migration-roundtrip' }]);
      const defaults = await beforeUp.query<{ branch: string; warehouse: string }>(
        `SELECT (SELECT id::text FROM branches WHERE id = $1) AS branch,
                (SELECT id::text FROM warehouses WHERE id = $2) AS warehouse`,
        [branchId, warehouseId],
      );
      expect(defaults.rows[0]).toEqual({ branch: branchId, warehouse: warehouseId });
    } finally {
      await beforeUp.end();
    }

    const up = await runMigrations(ctx.db.ownerUrl);
    expect(up.applied).toEqual([
      '0034_desktop_parity_followups_and_inventory.sql',
      '0035_inventory_document_line_traceability.sql',
      '0036_shift_cash_variance_journal.sql',
      '0037_catalog_scan_code_integrity.sql',
      '0038_journal_void_immutability.sql',
    ]);

    const category = await api(ctx.server, 'post', '/api/v1/organization/catalog/categories', {
      token: actor.token,
      body: { code: 'RT-0034', nameAr: 'فئة اختبار round trip' },
    });
    expect(category.status).toBe(201);
    const unit = await api(ctx.server, 'post', '/api/v1/organization/catalog/units', {
      token: actor.token,
      body: { code: 'RT-UNIT', nameAr: 'قطعة اختبار' },
    });
    expect(unit.status).toBe(201);
    const categoryId = data<{ id: string }>(category.body).id;
    const unitId = data<{ id: string }>(unit.body).id;

    // The post-round-trip service write verifies the recreated 0037 source column,
    // its RLS policy, and its register backfill path together rather than only looking
    // at information_schema metadata.
    const item = await api(ctx.server, 'post', '/api/v1/organization/catalog/items', {
      token: actor.token,
      body: {
        sku: 'RT-ITEM-0037',
        barcode: 'RT-PRIMARY-0037',
        nameAr: 'صنف اختبار round trip',
        categoryId,
        baseUnitId: unitId,
      },
    });
    expect(item.status).toBe(201);
    const itemId = data<{ id: string }>(item.body).id;

    const afterUp = new Client({ connectionString: ctx.db.ownerUrl });
    await afterUp.connect();
    try {
      const requiredColumns = await afterUp.query<{ table_name: string; column_name: string }>(`
        SELECT table_name, column_name
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND (table_name, column_name) IN (
            ('sales_invoices', 'cashier_id'),
            ('sales_invoices', 'tax_type'),
            ('inventory_transactions', 'unit_id'),
            ('stock_transfer_lines', 'unit_id'),
            ('sales_invoice_lines', 'unit_id'),
            ('sales_invoice_lines', 'lot_id'),
            ('sales_invoice_lines', 'serial_ids'),
            ('purchase_invoice_lines', 'unit_id'),
            ('purchase_invoice_lines', 'lot_id'),
            ('purchase_invoice_lines', 'serial_ids'),
            ('shift_closes', 'journal_entry_id'),
            ('item_barcodes', 'source')
          )
        ORDER BY table_name, column_name
      `);
      expect(requiredColumns.rows).toHaveLength(12);

      const migrationObjects = await afterUp.query<{
        source_constraint: boolean;
        active_barcode_index: boolean;
        unit_policy: boolean;
        component_policy: boolean;
      }>(`
        SELECT
          EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'item_barcodes_source_check') AS source_constraint,
          EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'public' AND indexname = 'items_tenant_active_barcode_key') AS active_barcode_index,
          EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = 'public' AND tablename = 'item_units' AND policyname = 'tenant_isolation') AS unit_policy,
          EXISTS (SELECT 1 FROM pg_policies WHERE schemaname = 'public' AND tablename = 'item_components' AND policyname = 'tenant_isolation') AS component_policy
      `);
      expect(migrationObjects.rows[0]).toEqual({
        source_constraint: true,
        active_barcode_index: true,
        unit_policy: true,
        component_policy: true,
      });

      const registry = await afterUp.query<{ source: string; item_id: string; unit_id: string }>(
        `SELECT source, item_id::text AS item_id, unit_id::text AS unit_id
           FROM item_barcodes
          WHERE tenant_id = $1 AND barcode = 'RT-PRIMARY-0037'`,
        [actor.tenantId],
      );
      expect(registry.rows).toEqual([{ source: 'primary', item_id: itemId, unit_id: unitId }]);

      // A small real 0034 document fixture proves the recreated tables, keys/defaults
      // and tenant columns can hold an operational draft again after the round trip.
      const documentId = crypto.randomUUID();
      await afterUp.query(
        `INSERT INTO inventory_documents (
           id, tenant_id, branch_id, warehouse_id, number, kind, status, document_date, created_by
         ) VALUES ($1, $2, $3, $4, 'RT-OPEN-0034', 'opening', 'draft', '2026-01-01', $5)`,
        [documentId, actor.tenantId, branchId, warehouseId, actor.userId],
      );
      await afterUp.query(
        `INSERT INTO inventory_document_lines (
           id, document_id, tenant_id, line_no, item_id, unit_id, quantity, base_quantity, unit_cost, created_by
         ) VALUES ($1, $2, $3, 1, $4, $5, '2.0000', '2.0000', '0.0000', $6)`,
        [crypto.randomUUID(), documentId, actor.tenantId, itemId, unitId, actor.userId],
      );
      const documentFixture = await afterUp.query<{ status: string; serial_ids: string }>(
        `SELECT d.status, l.serial_ids::text AS serial_ids
           FROM inventory_documents d
           INNER JOIN inventory_document_lines l ON l.document_id = d.id
          WHERE d.id = $1`,
        [documentId],
      );
      expect(documentFixture.rows).toEqual([{ status: 'draft', serial_ids: '[]' }]);
    } finally {
      await afterUp.end();
    }
  }, 240_000);
});

export {};
