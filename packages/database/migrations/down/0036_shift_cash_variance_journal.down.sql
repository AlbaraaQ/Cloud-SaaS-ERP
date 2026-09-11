-- 0036_shift_cash_variance_journal.down.sql
DROP INDEX IF EXISTS shift_closes_journal_idx;
ALTER TABLE shift_closes DROP COLUMN IF EXISTS journal_entry_id;
