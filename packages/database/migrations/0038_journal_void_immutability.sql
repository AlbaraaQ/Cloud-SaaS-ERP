-- 0038_journal_void_immutability.sql
--
-- Migration 0004 intended to allow the one auditable transition from a posted journal
-- to `void`, but returned OLD from its BEFORE UPDATE trigger. PostgreSQL therefore kept
-- the old row and silently discarded the allowed status change. Return NEW only for that
-- narrow transition; all accounting values and identity fields remain immutable.

CREATE OR REPLACE FUNCTION prevent_posted_journal_mutation() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF OLD.status = 'posted' THEN
    IF TG_OP <> 'UPDATE' OR NEW.status <> 'void' THEN
      RAISE EXCEPTION 'posted journal entries are immutable' USING ERRCODE = '42501';
    END IF;

    -- A void may stamp audit columns, but cannot rewrite a posted journal while changing
    -- its status. `version` is retained as an allowed conventional audit field.
    IF (to_jsonb(NEW) - ARRAY['status', 'updated_at', 'updated_by', 'version'])
       IS DISTINCT FROM
       (to_jsonb(OLD) - ARRAY['status', 'updated_at', 'updated_by', 'version']) THEN
      RAISE EXCEPTION 'posted journal entries are immutable' USING ERRCODE = '42501';
    END IF;
  END IF;
  RETURN NEW;
END
$$;
