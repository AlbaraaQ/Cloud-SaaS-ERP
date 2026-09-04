# PHASE_03_PROMPT — Tenancy, Identity & Access (RBAC + RLS)

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP replacing a legacy desktop ERP. Phase 03 of 23. `docs/` is
SSOT; `docs/PROJECT_CONTRACT.md` frozen: UUID v7, `tenant_id` + RLS on every business
table, Argon2id passwords, JWT RS256 access (15 min) + rotating refresh (30 d, hashed,
reuse-detection), permission codes `module.entity.action`, problem+json errors.
Legacy users had plaintext passwords (`Users.pwd`) — imported users get forced-reset,
NEVER imported hashes.

## 1. CURRENT PHASE
**#03 — Tenancy & Identity**: the platform module guarding everything after it.
Deliver tenants, users, memberships, roles/permissions, auth endpoints, tenant guard,
RLS policies, and the reusable **tenant isolation test harness**.

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/MULTI_TENANCY.md` (all) 4. `docs/SECURITY_ARCHITECTURE.md` §2–§5
5. `docs/DATABASE_DESIGN.md` §1–§2 6. `docs/API_CONTRACT.md` §1–§2
7. `docs/TESTING_STRATEGY.md` §6. 8. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS (verify; else stop)
Bootable `apps/api`, `packages/database` (client, `withTx`, base columns, RLS setter),
`packages/contracts` (errors, pagination), problem+json filter, health endpoints.

## 4. GOAL & SCOPE
### In scope
- Drizzle schema + migration: `tenants, users, memberships, roles, permissions,
  role_permissions, membership_roles, refresh_tokens` (columns per DATABASE_DESIGN §1–2),
  `tenant_settings`, enabling RLS policies on scoped tables created here.
- Platform module services/controllers: auth (login/refresh/logout/change-password),
  `/me`, tenant read + settings get; memberships CRUD (invite by email, branch scope),
  roles CRUD + permission assignment; permission registry seed from code list.
- Guards: AuthGuard (JWT), TenantGuard + TenantContext (AsyncLocalStorage),
  PermissionsGuard + `@RequiresPermission()`, BranchScope helper filter.
- RLS: policy creation helpers + policies for tables of this phase; API role/`migrator`
  role creation SQL (BYPASSRLS for migrator only).
- `packages/testing/isolation-suite.ts` implementing TESTING_STRATEGY §6 harness.
- Seeds: 3 baseline roles + permission registry; demo tenant factory (fixtures only).
### Out of scope (DO NOT DO)
Tenant self-registration billing flows · MFA TOTP UI/API beyond data columns ·
platform-admin ops plane endpoints (P23 lists only; guard stub exists) · notifications
(P04) · branch module (P05) — branch scope stores ids array only, resolution later.

## 5. EXACT TASKS
1. Schema/migration exactly per contract columns; RLS on `memberships/roles/…/tenant_settings`.
2. Auth flows per API_CONTRACT §1 (login returns access+refresh; rotation + family
   revoke on reuse; Argon2id verify; lockout counters on repeated failures).
3. Guards wired in pipeline position after rate-limit (API_ARCHITECTURE §2 order).
4. TenantGuard asserts membership + tenant status active; attaches TenantContext; sets
   GUC per tx via packages/database helper; optional `X-Branch-Id` validation scope util.
5. Endpoints: auth set, `/me` (permissions expanded), memberships, roles (+permissions),
   tenant settings GET/PUT typed keys from packages/config registry.
6. Isolation harness + apply it to this phase's tenant-scoped resources (4 proofs).
7. Idempotent seeds: run twice-safe.
8. Tests: unit (jwt service, guard logic, password policy), integration (auth flows,
   RBAC allow/deny, settings typed validation, RLS direct-SQL probe).
9. Docs: STATUS.md; SECURITY_ARCHITECTURE §5 matrix = unchanged (verify); API_CONTRACT
   §1–2 already matches — reconcile if deviation found (change process).

## 6. DATABASE IMPACT
Creates 9 platform tables + RLS policies + DB roles script. Migration must be
idempotent and reversible (`down` documented). No tenant business data yet.

## 7. API IMPACT
New endpoints listed in API_CONTRACT §1–2 — implement exactly these shapes; register
permission codes in `permissions` seed; OpenAPI updated.

## 8. SECURITY REQUIREMENTS
SECURITY_ARCHITECTURE §2 §3 §8 (auth rate buckets) enforced & tested; secrets via env;
no password/refresh value ever logged (redaction test).

## 9. TESTING REQUIREMENTS
Per TESTING_STRATEGY: unit ≥80% lines, integration for each endpoint, isolation harness
4-proofs applied, forged-tenant-claim test, suspended-tenant tests, refresh-reuse test.

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md entry; `apps/api/src/modules/platform/README.md` (guards & context usage
guide for later phases); update `DATABASE_DESIGN.md` ONLY if column-level mismatch
found (else none).

## 11. ACCEPTANCE CRITERIA
- All endpoints of API_CONTRACT §1–2 work with contract DTOs.
- Harness demonstrably blocks cross-tenant access attempts incl. raw SQL probe.
- Token rotation + reuse kill-chain passes tests; Argon2id params as specified.

## 12. DEFINITION OF DONE
Verify pipeline green (typegen/tsc/lint/unit/integration/build) · docs updated ·
no open criticals · final report per protocol §8.

## 13. DELIVERABLES
Platform identity module code+schema+tests+seeds+harness+docs updates.

## 14. DO NOT DO
Any business/organization tables · MFA implementation · billing · changing frozen
auth parameters · importing anything from legacy DB.
