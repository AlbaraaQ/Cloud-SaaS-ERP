# SECURITY_ARCHITECTURE

> Level B — CANONICAL (this file doubles as SECURITY_MODEL; the README references it as such).

## 1. Threat Model (ERP SaaS, condensed)

Cross-tenant data theft · privilege escalation inside tenant · financial fraud via
edited history · stolen tokens · brute-force/password spraying · malicious file uploads ·
injection · webhook/e-invoice replay · insider actions without audit · secrets leakage
(config, logs) · DoS on expensive endpoints (reports/migration).

## 2. Authentication

- Argon2id (m=64 MiB, t=3, p=4) password hashing; 12+ char policy, breach-list check.
- Optional TOTP MFA per user (`mfa_secret_enc` AES-GCM); enforced flag per tenant later.
- Access JWT RS256 15 min: claims `sub, tid, mid, scope` + `jti`. Public key rotation via
  `packages/config/keys` (kid). Refresh tokens: random 256-bit, SHA-256 hashed at rest,
  30 d, rotating with **reuse detection → family revocation**.
- Device tokens for compat API: scoped API keys (hashed), per-tenant, revocable.
- Login rate limiting + exponential lockout; audit every auth event.
- Legacy passwords NEVER migrated (`Users.pwd` plaintext) → forced reset flow on first login.

## 3. Authorization Model (RBAC + scope)

- Static permission registry (`permissions` table seeded from code list):
  `<module>.<entity>.<action>` (contract §1 naming). Examples: `sales.invoice.create|
  post|void|pay|view`, `accounting.journal.reverse`, `accounting.period.close`,
  `treasury.cheque.clear`, `migration.run.execute`.
- Roles bundle permissions per tenant; memberships carry roles + `branch_scope`.
- Guard checks permission; service checks scope (branch), data-state (draft), and
  business limits: per-membership `max_discount_pct/amt` (legacy `OperMaxDiscount`) and
  `max_operation_amount` (legacy `OperationPermission.OperVal`) enforced at math time.
- No implicit admin: `platform.*` perms manage tenant config; platform owner plane is
  separate (`is_platform_admin`) and never implied by tenant perms.

## 4. Tenant Isolation (security view)

App guard + Postgres RLS (`MULTI_TENANCY.md` §3) = two independent layers; both tested
per phase (`TESTING_STRATEGY.md` §6 harness). Platform-admin reads route through a
separate Nest module marked `PLATFORM-ONLY` with its own audit channel & reason field.

## 5. Permission Matrix (baseline — extend only forward)

| Module | view | manage | Sensitive extras |
|---|---|---|---|
| platform | tenant.view, notification.view | tenant/membership/role/settings.manage, notification.manage | audit.view, file.upload, job.view |
| organization | org.view | branches/warehouses/cash-locations/currencies.manage | postingprofile.manage |
| catalog | catalog.view | items/categories/units/taxgroups.manage | price.manage, import.execute |
| accounting | accounting.view(+reports.view) | account/costcenter/journal.create | journal.post/reverse, period.close/reopen, opening.manage |
| parties | parties.view | parties.manage | allocations.manage, creditlimit.override |
| inventory | inventory.view | adjust/create, transfer/create | adjust.approve, transfer.receive, negative.override |
| sales | sales.view | invoice.create | invoice.post/void, discount.override, return.create |
| purchases | purchase.view | invoice.create | invoice.post/void |
| treasury | treasury.view | voucher.create | voucher.post/void, cheque.clear, shift.close |
| einvoicing | einvoice.view | submit | credentials.manage |
| reporting | per report key | export.execute | — |
| migration | migration.view | run.execute | run.import (prod) |
| hrm/projects/pos/niche | pack-scoped — defined in their phase prompts, same scheme |

## 6. Input & Output Safety

- zod-validate every body/query/param (contract schemas); unknown keys rejected in writes.
- Drizzle parameterized only; `sql` template builder forbids interpolation of user input
  (lint). No string-built queries anywhere.
- Output encoding: server JSON only; frontends escape by framework; HTML report fields
  (shift html) sanitized server-side before render/store.
- Mass-assignment: DTO whitelists (`strip` mode); server ignores client `id/tenant_id/
  created_by/…`.

## 7. Transport, Headers, CORS/CSRF

- HTTPS only (HSTS), TLS ≥1.2. CORS allow-list per env for admin/customer origins.
- CSRF: Bearer-token API is header-based (not cookie) → CSRF-immune; admin uses
  Authorization header from memory/storage (not cookies) — confirmed pattern frozen.
- helmet defaults + `X-Content-Type-Options`, `Referrer-Policy`, tight CSP on frontends.

## 8. Rate Limiting & Abuse

Token-bucket per (user|ip): default 600/min; strict buckets: login 10/min,
register/forgot 5/min, presign 60/min, reports export 10/min, migration run 1/min per
tenant. 429 + `Retry-After`. Queue-backpressure for einvoice submissions.

## 9. Secrets & Sensitive Data

- Secrets via env only; `.env` gitignored; production uses platform secrets manager.
- **Encrypted-at-rest columns** (AES-256-GCM, key from env `DATA_ENC_KEY`, key rotation
  design noted): e-invoice private keys/CSID secrets, Salla OAuth tokens, SMTP passwords,
  MFA secrets. Never logged (pino redact paths). Legacy plaintext secrets imported
  → encrypted immediately, source discarded.
- PII minimization: customer `national_id`, IDs scans in object storage with private ACLs.
- Audit log access restricted (`platform.audit.view`), immutable (no update/delete grants).

## 10. Audit & Accountability

Mutating endpoint ⇒ audit_log row (entity diff, actor, ip, ua, trace). Financial actions
get domain events as second channel. `audit_log` retention ≥ 7 years (tenant data law);
archival strategy = partition by month (P23).

## 11. Data Lifecycle & Deletion

Soft delete hides; purge job crypto-shreds PII after tenant deletion grace (30 d).
Backups: PITR (managed PG) + daily snapshots tested P23; restore drill documented in
operations runbook (`docs/change-log/ops/…` created in P23).

## 12. Secure SDLC Gates

Every phase: dependency audit (`pnpm audit --prod`, fail on high), lint security rules,
secret scanning (gitleaks) in CI, review checklist: authZ tested, tenant isolation test,
money math test, audit coverage. Pen-test note: external assessment before public
launch (P23 gate, owner action).

## 13. Compliance Notes

- KSA ZATCA Phase-II: credentials & submissions module (P13) follows Fatoora specs;
  certificates lifecycle documented there.
- Egypt ETA: adapter design (P13) keeps secrets encrypted; certification out of scope.
- VAT data retention mirrors audit retention; fiscal reports are reproducible from
  immutable ledgers at any past date (point-in-time statements).
