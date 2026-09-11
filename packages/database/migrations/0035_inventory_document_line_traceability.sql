-- 0035_inventory_document_line_traceability.sql
--
-- A commercial document must retain the unit/lot/serial selection that produced its
-- inventory movement.  Keeping this on the invoice line makes sale/purchase posting and
-- reversals unit-safe rather than relying on browser-only metadata.

ALTER TABLE sales_invoice_lines
  ADD COLUMN IF NOT EXISTS unit_id uuid REFERENCES units_of_measure(id) ON DELETE RESTRICT,
  ADD COLUMN IF NOT EXISTS lot_id uuid REFERENCES item_lots(id) ON DELETE RESTRICT,
  ADD COLUMN IF NOT EXISTS serial_ids jsonb NOT NULL DEFAULT '[]';
CREATE INDEX IF NOT EXISTS sales_invoice_lines_unit_idx
  ON sales_invoice_lines (tenant_id, item_id, unit_id);
CREATE INDEX IF NOT EXISTS sales_invoice_lines_lot_idx
  ON sales_invoice_lines (tenant_id, lot_id);

ALTER TABLE purchase_invoice_lines
  ADD COLUMN IF NOT EXISTS unit_id uuid REFERENCES units_of_measure(id) ON DELETE RESTRICT,
  ADD COLUMN IF NOT EXISTS lot_id uuid REFERENCES item_lots(id) ON DELETE RESTRICT,
  ADD COLUMN IF NOT EXISTS serial_ids jsonb NOT NULL DEFAULT '[]';
CREATE INDEX IF NOT EXISTS purchase_invoice_lines_unit_idx
  ON purchase_invoice_lines (tenant_id, item_id, unit_id);
CREATE INDEX IF NOT EXISTS purchase_invoice_lines_lot_idx
  ON purchase_invoice_lines (tenant_id, lot_id);
