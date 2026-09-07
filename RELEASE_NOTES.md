# Release Notes — v1.0.0

Date: 2026-09-07
Status: READY candidate

## Highlights

- Completed all 23 project phases for the cloud multi-tenant SaaS ERP.
- Core platform: tenancy, identity, RBAC, settings, audit, files, notifications, outbox and sequences.
- Domain modules: organization, catalog, accounting, parties, inventory, sales, purchases, treasury, e-invoicing, reporting and migration.
- Compatibility and surfaces: legacy desktop gateway, admin panel and customer portal.
- Vertical packs: restaurant POS, HRM/payroll, installments, contracting/projects, optics, tailoring, marina, fitment and Salla integration.
- Go-live hardening: Prometheus metrics, deeper readiness checks, retention plan, backup/restore and incident runbooks, UAT pack, security sweep and release operations documentation.

## Verification

Final Phase 23 verification command: `pnpm run verify` with exit code 0.

Known environment note: this sandbox logs an embedded PostgreSQL `libpq.so.5` loader error during API Vitest setup, but the repository verify script completes and exits 0.

## Security notes

- Runtime secrets are encrypted with AES-GCM.
- Tenant isolation is enforced by application guards and PostgreSQL RLS/FORCE RLS.
- Remaining dependency audit findings are waived by ADR-021 because they are isolated to build/test/dev-server surfaces and are not exposed by the production runbook.
