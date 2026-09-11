-- 0037_catalog_scan_code_integrity.down.sql
-- Reverses only the additive source marker and active-primary index. Existing
-- barcode mappings are deliberately retained so a rollback does not lose labels.
DROP INDEX IF EXISTS items_tenant_active_barcode_key;
DROP POLICY IF EXISTS tenant_isolation ON item_components;
DROP POLICY IF EXISTS tenant_isolation ON item_units;
ALTER TABLE item_barcodes
  DROP CONSTRAINT IF EXISTS item_barcodes_source_check,
  DROP COLUMN IF EXISTS source;
