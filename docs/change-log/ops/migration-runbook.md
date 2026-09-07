# Migration Runbook — Phase 15

## 1. Preparation

1. Restore the customer SQL Server backup to an isolated staging SQL Server instance.
2. Create a read-only SQL Server login; deny write, DDL, and agent permissions.
3. Freeze owner decisions from `REQUIRES_CONFIRMATION.md`, especially document type maps,
   ParentCode repair policy, multi-year database order, and rounding tolerances.
4. Provision the target tenant, owner membership, base roles, and migration writer secret.

## 2. Analyze

Run `apps/migrator --mode analyze` and attach the JSON output to the migration ticket.
The RC answers dictionary must be reviewed before any load. Unknown legacy enum values are
blockers and must not be guessed.

## 3. Dry run

Run `dry_run` against the restored database. Check issue counts, per-wave staged counts,
R1-R7 preview, and variance lists. Fix mappings or obtain written waivers where allowed.

## 4. Final import window

1. Announce freeze window and stop legacy writes.
2. Take a final SQL Server backup and restore to staging.
3. Re-run analyze and compare dictionaries with the approved dry run.
4. Execute `import`; the worker checkpoints each wave and uses legacy-id idempotency.
5. Execute `reconcile`; tenant activation is blocked unless R1-R4 pass or owner waiver is
   recorded in the run summary.
6. Fast-forward document sequences and verify R7.

## 5. Rollback

Rollback is allowed only before the tenant has non-migration business activity. The worker
deletes tenant data in reverse dependency order and leaves `migration_runs`, issues, and
operator artifacts for audit unless the tenant itself is discarded.

## 6. Security

Never use production legacy DB credentials directly. Never migrate passwords or plaintext
secrets. Attachments are streamed to object storage; real customer data must not be
committed to fixtures.
