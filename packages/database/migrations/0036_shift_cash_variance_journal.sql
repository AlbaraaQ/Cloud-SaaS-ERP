-- 0036_shift_cash_variance_journal.sql
-- Closing a cashier shift may reveal a physical over/short. Keep the generated
-- accounting entry on the shift itself so the close report can drill into it.
ALTER TABLE shift_closes
  ADD COLUMN IF NOT EXISTS journal_entry_id uuid REFERENCES journal_entries(id) ON DELETE RESTRICT;
CREATE INDEX IF NOT EXISTS shift_closes_journal_idx
  ON shift_closes (tenant_id, journal_entry_id)
  WHERE journal_entry_id IS NOT NULL;
