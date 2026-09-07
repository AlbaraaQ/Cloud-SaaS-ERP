# Tenant onboarding and migration cutover checklist

## New tenant without migration

1. Create tenant and owner membership.
2. Apply default seeds: roles, SAR/USD, units, tax groups, branch, warehouse, safe and posting profiles.
3. Confirm owner login and forced password reset if required.
4. Validate `/health/ready`, admin dashboard and customer portal tenant picker.
5. Target elapsed time: under 15 minutes.

## Migrated tenant cutover

1. Freeze legacy desktop writes.
2. Take final legacy backup and record checksum.
3. Run migrator `analyze`, resolve blocking RC items, then dry run.
4. Run final delta import.
5. Reconcile trial balance, inventory, party balances, invoices and vouchers.
6. Enable compatibility API only if desktop sync is approved.
7. Enable vertical feature flags per tenant acceptance sheet.
8. Obtain owner sign-off before production DNS switch.
