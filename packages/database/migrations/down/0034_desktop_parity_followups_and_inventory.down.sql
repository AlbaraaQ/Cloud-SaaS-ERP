-- Reverse migration 0034. Applied only by an operator who intentionally removes the
-- additive desktop-parity features; dependent documents must be removed first.

DROP TABLE IF EXISTS inventory_document_lines;
DROP TABLE IF EXISTS inventory_documents;
DROP TABLE IF EXISTS pos_shortcut_items;
DROP TABLE IF EXISTS pos_held_tickets;

ALTER TABLE stock_transfer_lines DROP COLUMN IF EXISTS unit_id;
DROP INDEX IF EXISTS inventory_transactions_unit_idx;
ALTER TABLE inventory_transactions DROP COLUMN IF EXISTS unit_id;

DROP INDEX IF EXISTS sales_invoices_cashier_idx;
ALTER TABLE sales_invoices DROP COLUMN IF EXISTS tax_type;
ALTER TABLE sales_invoices DROP COLUMN IF EXISTS cashier_id;
