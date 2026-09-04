# PHASE_18_PROMPT — Customer UI / Portal (Next.js)

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 18 of 23. `apps/customer` = marketing site +
self-service portal per `docs/CUSTOMER_UI_MASTER_REQUIREMENTS.md`. APIs already exist;
admin panel exists from P17 (reuse shell kit patterns, not pages). RTL-first, mobile
first. No new backend endpoints beyond thin portal-scoped queries IF the master doc
marks them missing (then CR first); default posture: consume existing API.

## 1. CURRENT PHASE
**#18 — Customer UI**: public pages, auth (incl. forced-reset for migrated users),
portal dashboard/invoices/statement/payments/profile-requests/notifications,
optional public invoice verification page, lightweight staff screens where flags on.

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/CUSTOMER_UI_MASTER_REQUIREMENTS.md` (primary spec) 4. `docs/API_CONTRACT.md`
5. `docs/SECURITY_ARCHITECTURE.md` §8 (public endpoints limits)
6. `docs/TESTING_STRATEGY.md` §8 7. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS
API + admin conventions; kits can be re-implemented lightweight or shared via new
`packages/ui` ONLY if P17 shows reuse (ADR-013 note — justify either way in report).

## 4. GOAL & SCOPE
### In scope
Marketing home + pricing placeholder + contact · auth pages · tenant picker · portal
sections §1–§2 of master spec (as flags permit) · verification page §4 with heavy
rate limiting · onboarding wizard §3 · forced-reset flow · profile change REQUEST flow
(creates notification/task, no direct master write).
### Out of scope (DO NOT DO)
E-commerce storefront · payment-gateway checkout · back-office screens (admin's) ·
push-native notifications · SEO tooling beyond metadata basics.

## 5. EXACT TASKS
1. Bootstrap app (i18n RTL/LTR, theme shared tokens).
2. Public pages + verification page (input UUID/hash → masked render; 404 on unknown;
   rate-limit UX friendly).
3. Auth + forced-reset + tenant picker + permission-scoped nav.
4. Portal dashboard (balances, recent docs), invoices list/detail + PDF download &
   QR display, statement date-range + export, payments history, notifications center,
   profile request form.
5. Quick-sale + stock lookup screens behind flags (permission-scoped).
6. Onboarding wizard hitting tenant bootstrap APIs (company → COA choice → first
   branch/warehouse/safe → invite admin) with per-step server validation errors
   surfaced.
7. E2E: portal login→statement→invoice PDF; verification page flow.
8. STATUS.md + coverage markers in CUSTOMER master.

## 6. DATABASE IMPACT
None.

## 7. API IMPACT
Consumption only; needs list documented as CRs (e.g., masked verify payload endpoint).

## 8. SECURITY REQUIREMENTS
Public endpoints rate-limited and data-minimal (verify masks party data); auth pages
protected against brute-force UX; no tokens in localStorage when cookie mode enabled;
CSP strict.

## 9. TESTING REQUIREMENTS
Route tests with MSW; e2e per §7; RTL layout visual smoke (screenshot diff optional).

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md; `apps/customer/README.md`; master coverage markers.

## 11. ACCEPTANCE CRITERIA
- Master §0–§4 functional; flags default-off paths render clear "disabled by tenant"
  empty states; e2e green.

## 12. DEFINITION OF DONE
verify green · e2e green · docs · protocol §8 report.

## 13. DELIVERABLES
`apps/customer` complete per spec + tests + docs.

## 14. DO NOT DO
Admin screens · backend edits without CR · store-to-localStorage of tokens ·
SEO/marketing automation extras.
