# PHASE_23_PROMPT — Hardening, Operations & Go-Live

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 23 of 23 — final gate. All phases P01–P22 are
complete per `docs/STATUS.md`. Frozen rules apply. Your job is NOT new features; it is
making the system production-credible: performance, observability, backup/restore
proof, security review, release runbooks, UAT closure.

## 1. CURRENT PHASE
**#23 — Hardening & Go-Live**: the launch checklist executed against a staging
environment with migrated-fixture data.

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/MASTER_PROJECT_PLAN.md` §9 (risks) & §10.2 (owner approvals checklist)
4. `docs/SECURITY_ARCHITECTURE.md` (ALL) 5. `docs/TESTING_STRATEGY.md` §5
6. `docs/MIGRATION_ARCHITECTURE.md` §8/§11 (go-live reconciliation steps)
7. `docs/STATUS.md` (ALL). 8. All ADRs.

## 3. PREVIOUS PHASE OUTPUTS
The whole system. If STATUS.md shows gaps → STOP and report (do not fill here).

## 4. GOAL & SCOPE
### In scope
- Observability: Prometheus metrics endpoint (req/latency/queue depth/einvoice
  failures/migration throughput), log sampling config, health dashboards doc; alerts
  catalogue (runbook docs only — actual alert infra env-specific).
- Performance: run TESTING §5 budgets at 10× fixture scale; fix hot spots ONLY via
  indexes/query fixes (no behavior changes); materialized view exception per ADR-007
  requires CR + measurement proof.
- Data safety: PITR/backup restore drill documented & executed on staging; audit_log
  monthly partition plan implemented (or justified deferral ADR); purge/retention job
  tests.
- Security: dependency audit clean (high=0 or waived w/ ADR), gitleaks clean, RL
  spot-checks, pen-test-friendly endpoint inventory export; platform-admin ops plane
  review (separate audit channel check).
- Release: staging→prod runbook, rollback plan, data-freeze & final delta migration
  checklist per tenant template, feature-flag prod defaults, seed review.
- UAT: scripted UAT scenarios (sales day, purchase cycle, payroll month, migration
  verify) with sign-off sheets as md templates; owner questions list resolved or
  deferred explicitly.
- Docs freeze: ensure all Level A/B docs equal reality (diff audit); CHANGE-REQUESTS
  closed loop.
### Out of scope (DO NOT DO)
New business features · vertical additions · schema redesigns (CRs only) · marketing
site content.

## 5. EXACT TASKS
1. Metrics + `/health/ready` deep checks + dashboards doc `docs/change-log/ops/monitoring.md`.
2. Perf run + report (`perf-report.md` w/ numbers vs budgets; fixes listed).
3. Backup/restore drill doc + evidence; retention job implementation/test.
4. Security sweep report (headers/limits/audit coverage/isolation suite all-green
   attestation).
5. Runbooks: deploy, rollback, tenant onboarding incl. migration cutover, incident
   basics; store under `docs/change-log/ops/`.
6. UAT pack + results capture; program acceptance vs PROJECT_OVERVIEW §8 verified
   item-by-item.
7. Final STATUS.md entry + `RELEASE_NOTES.md` v1.0.0.

## 6. DATABASE IMPACT
Indexes allowed w/ evidence; partitions plan; no structural changes otherwise.

## 7. API IMPACT
`/metrics` + possible hardened config endpoints only.

## 8. SECURITY REQUIREMENTS
This phase IS the security gate — every § of SECURITY_ARCHITECTURE attested with
evidence links in the security sweep report.

## 9. TESTING REQUIREMENTS
Full regression: all suites + isolation + invariants + e2e on staging; load smoke
results recorded.

## 10. DOCUMENTATION REQUIREMENTS
All ops docs listed + final reconciliation of docs↔reality (any mismatch fixed or CR).

## 11. ACCEPTANCE CRITERIA
PROJECT_OVERVIEW §8 definition-of-success items each marked ✅/❌ with evidence; no ❌
without owner waiver recorded in ADR.

## 12. DEFINITION OF DONE
All green on staging + sign-off pack complete + protocol §8 report labeling project READY.

## 13. DELIVERABLES
Ops runbooks, perf/security reports, metrics, release notes, UAT pack, final STATUS.

## 14. DO NOT DO
Feature work · silent fixes of canonical-doc mismatches (document) · scope rescues
of unfinished earlier phases (report instead).
