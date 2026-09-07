# Phase 18 Implementation Report

## Current status

`COMPLETE` as of 2026-09-07.

## Delivered

- Next.js 15 customer app under `apps/customer`.
- Mobile-first RTL Arabic shell with public navigation, CSP headers, and print-friendly
  portal/invoice detail styling.
- Public marketing pages: home, pricing placeholder, and contact.
- Auth pages: login, forgot password, forced reset for migrated users, and tenant picker.
- Public invoice verification page with UUID/hash form and masked/minimal response copy.
- Portal dashboard, invoices list/detail with PDF and ZATCA QR affordances, statement
  date-range/export UI, payments history, profile-change request form, notifications,
  stock lookup, task inbox, and quick-sale screen behind `portal.quick_sale` flag copy.
- Onboarding wizard page covering company data, COA template, first branch/warehouse/safe,
  admin invite, and migration-console handoff.
- Route metadata with permissions and tenant flags for permission-scoped navigation.
- Customer README and tests for route coverage, money formatting, and PII masking.

## Verification

`pnpm --filter @erp/customer build` passes. Full `pnpm run verify` passes with admin,
customer, migrator, API, packages, smoke checks, tests, and OpenAPI export.

## Notes

Phase 18 is API-consumption only. No backend endpoints or database schema were changed.
Customer-facing payment gateway checkout, e-commerce storefront, and native push
notifications remain out of scope.
