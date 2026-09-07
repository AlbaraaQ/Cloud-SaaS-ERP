# Backup, PITR and restore drill — Phase 23

## Policy

- PostgreSQL WAL archiving/PITR must be enabled by the hosting environment.
- Take a logical `pg_dump` before every tenant cutover and every schema migration window.
- Object storage buckets use versioning and lifecycle retention aligned with tenant contract terms.

## Drill evidence template

1. Record source DB identifier, WAL archive location and object-storage bucket version marker.
2. Restore latest base backup to an isolated staging instance.
3. Replay WAL to the selected timestamp.
4. Run `pnpm db:migrate` against the restored instance.
5. Run `pnpm run verify` and tenant reconciliation reports.
6. Record row counts for `tenants`, `memberships`, ledgers, invoices, vouchers, files and audit log.
7. Destroy isolated restore instance after evidence is archived.

## Phase 23 execution note

A code-level drill pack and runbook are committed here. The actual PITR infrastructure is environment-specific, so production launch requires the operations owner to fill the evidence template with provider backup ids and restore timestamps.

## Retention jobs

`apps/api/src/ops/retention.service.ts` defines deterministic cutoffs:

- audit archive before 365 days; audit hard-delete remains forbidden.
- idempotency-key purge before 30 days.
- outbox purge before 90 days.
- pending file orphan purge before 2 days, complementing the existing file orphan collector.
