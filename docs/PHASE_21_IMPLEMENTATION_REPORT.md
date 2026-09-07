# Phase 21 Implementation Report — Installments & Contracting Projects

Date: 2026-09-07
State: COMPLETE

## Delivered

- Added tenant-flag-gated `pack.installments` and `pack.projects` API modules.
- Added database schema and reversible migration `0017_installments_projects.sql` for installment contracts/schedules, projects, stage templates/stages, BOQ terms, progress bills/lines and project requirements.
- All new tenant-scoped tables enable and force PostgreSQL RLS.
- Implemented equal schedule generation for day/month periods with Decimal-safe rounding to the final line.
- Implemented collection flow that creates a treasury receipt voucher and allocates payments to oldest unpaid schedule rows.
- Implemented project/stage/template/BOQ CRUD primitives, user stage accreditation, progress bill computation, posting to sales invoices with no inventory movement and retention release invoice action.
- Added Phase 21 permissions, admin `/installments` and `/projects` screens, calculator tests, module READMEs and OpenAPI regeneration.

## Notes

- Interest/usury engines, subcontractor cycles, Gantt tools and document storage are intentionally out of scope per the phase prompt.
- Progress-bill posting uses standard `sales_invoices.kind = sale`; source linkage is retained on `progress_bills.invoice_id` and service descriptions.
- Retention accounting can use the supplied `retentionReceivableAccountId`, matching the existing posting-profile slot convention until a full slot resolver is introduced.
