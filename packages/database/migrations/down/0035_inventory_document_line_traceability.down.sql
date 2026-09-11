-- 0035_inventory_document_line_traceability.down.sql
DROP INDEX IF EXISTS purchase_invoice_lines_lot_idx;
DROP INDEX IF EXISTS purchase_invoice_lines_unit_idx;
ALTER TABLE purchase_invoice_lines
  DROP COLUMN IF EXISTS serial_ids,
  DROP COLUMN IF EXISTS lot_id,
  DROP COLUMN IF EXISTS unit_id;

DROP INDEX IF EXISTS sales_invoice_lines_lot_idx;
DROP INDEX IF EXISTS sales_invoice_lines_unit_idx;
ALTER TABLE sales_invoice_lines
  DROP COLUMN IF EXISTS serial_ids,
  DROP COLUMN IF EXISTS lot_id,
  DROP COLUMN IF EXISTS unit_id;
