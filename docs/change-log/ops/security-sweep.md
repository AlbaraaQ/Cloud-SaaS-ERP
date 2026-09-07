# Security sweep — Phase 23

Date: 2026-09-07

## Attestation against SECURITY_ARCHITECTURE

- Authentication and guard pipeline remains ordered in `AppModule`: rate limit → auth → tenant → branch → permissions.
- Tenant isolation remains DB-enforced through tenant-scoped predicates and RLS/FORCE RLS migrations.
- Posted accounting corrections remain reversal-only.
- Secrets use application-layer AES-GCM encryption for e-invoicing and Salla credentials.
- File download uses short-lived signed URLs and tenant-bound object keys.
- Mutating endpoints are covered by the audit interceptor; platform-admin actions remain outside tenant routes.
- Salla webhook ingestion requires HMAC validation before invoice creation.
- Security headers are applied through Helmet; CORS remains explicit via env allow-list.

## Commands/evidence

- `pnpm audit --audit-level high --json`: after upgrading `drizzle-orm` to `0.45.2`, current advisory metadata is `critical=1`, `high=5`, `moderate=10`, `low=0`; remaining high/critical findings are waived by ADR-021 as non-production build/test/dev-server surfaces.
- Secret scan fallback: `git grep -nE '(BEGIN (RSA|OPENSSH|PRIVATE) KEY|AKIA[0-9A-Z]{16}|ghp_[A-Za-z0-9_]{36,}|xox[baprs]-)' -- . ':!pnpm-lock.yaml'` found only documented AWS example fixture strings in `s3-signer.spec.ts`.
- Full regression: `pnpm run verify` final exit code 0.

## Waivers

ADR-021 waives remaining high/critical audit findings because they are limited to disabled/non-production dev server, test runner or build-time CSS/source-map surfaces.
