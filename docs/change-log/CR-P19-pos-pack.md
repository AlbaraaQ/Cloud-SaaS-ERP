# CR-P19 — Restaurant POS pack schema

Phase 19 adds the first vertical pack behind tenant feature flag `pack.pos`.

Additive database changes:

- `table_categories`
- `dining_tables`
- `order_events`
- `sales_invoices.order_type`
- `sales_invoices.table_no`
- `sales_invoices.combined_into`
- `sales_invoice_lines.modifiers jsonb`

The change is additive, tenant-scoped by FORCE RLS for new tables, and does not alter core
sales posting rules. POS orders compose normal sales invoices through the public sales
service.
