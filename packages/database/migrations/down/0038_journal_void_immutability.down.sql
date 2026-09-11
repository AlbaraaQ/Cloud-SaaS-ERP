-- Restore the migration-0004 trigger body when rolling back this additive repair.
CREATE OR REPLACE FUNCTION prevent_posted_journal_mutation() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF OLD.status = 'posted' AND NOT (TG_OP = 'UPDATE' AND NEW.status = 'void') THEN
    RAISE EXCEPTION 'posted journal entries are immutable' USING ERRCODE = '42501';
  END IF;
  RETURN OLD;
END $$;
