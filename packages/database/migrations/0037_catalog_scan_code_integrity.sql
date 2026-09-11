-- 0037_catalog_scan_code_integrity.sql
--
-- A scanner must resolve one tenant-local code to one concrete item unit.  The
-- original schema keeps scanner-visible values in four places (`items.sku`,
-- `items.barcode`, `item_units.barcode`, and `item_alternative_codes`) while
-- `item_barcodes` already has the tenant-scoped primary key needed to be the
-- canonical register for primary and unit barcodes.  This migration records the
-- managed source of each register row and backfills the two mirrored legacy fields.
--
-- It deliberately stops on ambiguous historic data rather than choosing an item
-- silently.  An operator must resolve such labels before migrating, which is safer
-- than making a POS scanner sell the wrong item.

ALTER TABLE item_barcodes
  ADD COLUMN IF NOT EXISTS source text NOT NULL DEFAULT 'alternate';

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'item_barcodes_source_check'
  ) THEN
    ALTER TABLE item_barcodes
      ADD CONSTRAINT item_barcodes_source_check
      CHECK (source IN ('primary', 'unit', 'alternate'));
  END IF;
END
$$;

-- `item_units` and `item_components` are tenant-derived tables. Migration 0003
-- enabled RLS on them but did not add a policy, which made every API write fail
-- closed with a 500. Keep the tenant boundary derived from the parent item so a
-- caller cannot attach another tenant's unit/component to its own item.
ALTER TABLE item_units ENABLE ROW LEVEL SECURITY;
ALTER TABLE item_units FORCE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON item_units;
CREATE POLICY tenant_isolation ON item_units
  USING (EXISTS (
    SELECT 1 FROM items i
    WHERE i.id = item_units.item_id
      AND i.tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid
  ))
  WITH CHECK (EXISTS (
    SELECT 1 FROM items i
    WHERE i.id = item_units.item_id
      AND i.tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid
  ));

ALTER TABLE item_components ENABLE ROW LEVEL SECURITY;
ALTER TABLE item_components FORCE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON item_components;
CREATE POLICY tenant_isolation ON item_components
  USING (EXISTS (
    SELECT 1 FROM items i
    WHERE i.id = item_components.item_id
      AND i.tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid
  ))
  WITH CHECK (EXISTS (
    SELECT 1 FROM items i
    WHERE i.id = item_components.item_id
      AND i.tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid
  ));

-- Treat every scanner-visible value as a unit mapping while checking historic
-- data.  Repeated rows that resolve to the *same* item/unit are valid mirrors;
-- one code resolving to different item/unit pairs is not.
DO $$
DECLARE collision record;
BEGIN
  WITH scan_codes AS (
    SELECT i.tenant_id, btrim(i.sku) AS code, i.id AS item_id, i.base_unit_id AS unit_id
    FROM items i
    WHERE i.deleted_at IS NULL AND btrim(i.sku) <> ''

    UNION ALL

    SELECT i.tenant_id, btrim(i.barcode) AS code, i.id AS item_id, i.base_unit_id AS unit_id
    FROM items i
    WHERE i.deleted_at IS NULL AND i.barcode IS NOT NULL AND btrim(i.barcode) <> ''

    UNION ALL

    SELECT i.tenant_id, btrim(iu.barcode) AS code, iu.item_id, iu.unit_id
    FROM item_units iu
    INNER JOIN items i ON i.id = iu.item_id
    WHERE i.deleted_at IS NULL AND iu.barcode IS NOT NULL AND btrim(iu.barcode) <> ''

    UNION ALL

    SELECT i.tenant_id, btrim(ib.barcode) AS code, ib.item_id,
      COALESCE(ib.unit_id, i.base_unit_id) AS unit_id
    FROM item_barcodes ib
    INNER JOIN items i ON i.id = ib.item_id
    WHERE i.deleted_at IS NULL AND btrim(ib.barcode) <> ''

    UNION ALL

    SELECT i.tenant_id, btrim(iac.code) AS code, iac.item_id, i.base_unit_id AS unit_id
    FROM item_alternative_codes iac
    INNER JOIN items i ON i.id = iac.item_id
    WHERE i.deleted_at IS NULL AND iac.deleted_at IS NULL AND btrim(iac.code) <> ''
  ), conflicts AS (
    SELECT tenant_id, code,
      array_agg(DISTINCT item_id::text || ':' || unit_id::text ORDER BY item_id::text || ':' || unit_id::text) AS mappings
    FROM scan_codes
    GROUP BY tenant_id, code
    HAVING count(DISTINCT item_id::text || ':' || unit_id::text) > 1
  )
  SELECT tenant_id, code, mappings
  INTO collision
  FROM conflicts
  ORDER BY tenant_id, code
  LIMIT 1;

  IF FOUND THEN
    RAISE EXCEPTION USING
      ERRCODE = '23505',
      MESSAGE = format(
        'Cannot migrate catalog scanner code %s in tenant %s: it maps to multiple item units (%s). Resolve the duplicate before retrying.',
        collision.code,
        collision.tenant_id,
        array_to_string(collision.mappings, ', ')
      );
  END IF;
END
$$;

-- Primary item barcode: `items.barcode` remains the backward-compatible display
-- field; its canonical mapping lives in the register with source=primary.
INSERT INTO item_barcodes (tenant_id, barcode, item_id, unit_id, source)
SELECT i.tenant_id, i.barcode, i.id, i.base_unit_id, 'primary'
FROM items i
WHERE i.deleted_at IS NULL AND i.barcode IS NOT NULL AND btrim(i.barcode) <> ''
ON CONFLICT (tenant_id, barcode) DO UPDATE
SET item_id = EXCLUDED.item_id,
    unit_id = EXCLUDED.unit_id,
    source = 'primary';

-- Alternate-unit barcode: `item_units.barcode` is still retained for legacy
-- readers, but its canonical mapping is also registered.  A primary source wins
-- only when the existing entry is exactly the same mapping.
INSERT INTO item_barcodes (tenant_id, barcode, item_id, unit_id, source)
SELECT i.tenant_id, iu.barcode, iu.item_id, iu.unit_id, 'unit'
FROM item_units iu
INNER JOIN items i ON i.id = iu.item_id
WHERE i.deleted_at IS NULL AND iu.barcode IS NOT NULL AND btrim(iu.barcode) <> ''
ON CONFLICT (tenant_id, barcode) DO UPDATE
SET item_id = EXCLUDED.item_id,
    unit_id = EXCLUDED.unit_id,
    source = CASE
      WHEN item_barcodes.source = 'primary' THEN 'primary'
      ELSE 'unit'
    END;

-- The canonical register protects all newly written primary barcodes at the DB
-- level.  This additional index keeps legacy/direct SQL writes from duplicating
-- active primary values outside the service layer.
CREATE UNIQUE INDEX IF NOT EXISTS items_tenant_active_barcode_key
  ON items (tenant_id, barcode)
  WHERE deleted_at IS NULL AND barcode IS NOT NULL;
