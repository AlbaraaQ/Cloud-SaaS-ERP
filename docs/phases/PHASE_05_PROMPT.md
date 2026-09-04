# PHASE_05_PROMPT — Organization Structure

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 05 of 23. SSOT in `docs/`; frozen: UUID v7,
tenant_id+RLS everywhere, numeric money/decimal.js, soft delete on master data,
audit columns, sequences for numbers, posting profiles per (branch, doc_type)
replacing legacy `SettingGeneral.*Acc` + `Branches.*Acc` mappings.
Legacy reference tables: `Foundation`→company_profiles, `Branches`, `Stocks`→warehouses,
`Safes`+`Banks`+`treasury`→cash_locations, `Currency_Lastprice*`→fx_rates,
`priceTypes`→price_lists.

## 1. CURRENT PHASE
**#05 — Organization**: company profile, branches, warehouses, cash locations with
currency balances view, currencies & FX rates, price lists, branch posting profiles.
Everything downstream (catalog→accounting→sales…) depends on these.

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/DATABASE_DESIGN.md` §5 (+ §3 currencies) 4. `docs/DOMAIN_MODEL.md` §1–§3
5. `docs/API_CONTRACT.md` §3 6. `docs/LEGACY_DATABASE_ANALYSIS.md` §3 (cash/banks)
7. `docs/MULTI_TENANCY.md` §3 8. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS (verify; else stop)
Guards/context, audit/files/sequences/settings/idempotency services ready.

## 4. GOAL & SCOPE
### In scope
- Tables+RLS: `company_profiles, branches, warehouses, cash_locations,
  cash_location_balances, currencies, fx_rates, price_lists, price_list_items(item-id
  FK added P06 — table now, item FK deferred via nullable uuid placeholder)`,
  `branch_posting_profiles`.
- Endpoints per API_CONTRACT §3 (CRUD + activate/default toggles + balances readonly).
- Validation: unique codes per tenant; one default branch/warehouse/safe; IBAN format
  when bank kind; currency ISO codes; posting profile account ids only EXISTING and of
  accepted subtypes (accounts exist from P07 — validate referential shape now via
  uuid + doc note `ValidatedAtRuntime: P07`; JSONB mapping schema zod-typed).
- Seed on tenant bootstrap hook: main branch/warehouse/safe creation API for P03's
  tenant factory (service fn `provisionOrgDefaults(tenantId)` used by later phases).
### Out of scope (DO NOT DO)
Accounts/COA (P07) · items (P06) → price_list_items validation deferred · any posting
logic · UI screens.

## 5. EXACT TASKS
1. Migrations + Drizzle + RLS + indexes per contract (list every index in report).
2. Controllers/services for each resource with soft-delete, default-flag maintenance
   (single default per tenant per kind — partial unique), branch-scope filtering
   respected (list respects membership branch_scope).
3. `company_profiles` 1:1 upsert endpoint; logo wiring to files service (file id only).
4. FX: latest-rate query fn `resolveFx(tenantId, from, to, date)` unit-tested.
5. Posting profiles: zod JSONB schema `PostProfileV1` in @erp/contracts; CRUD +
   `resolvePostProfile(branchId, docType)` with tenant-default fallback (hard fail
   path returns `ACCOUNT_PROFILE_MISSING` error code — add to contracts registry + API_CONTRACT codes list).
6. `cash_location_balances` read endpoints (writer functions arrive in P12; now seeds 0).
7. `provisionOrgDefaults` + test that a new tenant ends with consistent defaults.
8. Isolation tests + soft-delete behavior tests + concurrency test on default flag.
9. STATUS.md; ADMIN master §2 alignment check (extend section if implemented
   capability exceeds doc text).

## 6. DATABASE IMPACT
+9 tables, RLS on all; FKs: warehouses/cash_locations→branches, balances→locations,
profiles→branches; NO FK to accounts yet (validated P07 when accounts exist — note in
migration comment; P07 prompt includes adding the FK).

## 7. API IMPACT
Implements API_CONTRACT §3 exactly; new error code `ACCOUNT_PROFILE_MISSING`;
permissions: `organization.view`, `organization.manage`, `organization.postingprofile.manage`
(register in permissions seed — seed migration).

## 8. SECURITY REQUIREMENTS
Branch-scope enforcement in list/detail; manage perms for writes; IBAN/bank data is
sensitive — masked in list responses; audit changes to profiles & cash locations.

## 9. TESTING REQUIREMENTS
Unit (defaults, fx resolution, profile resolution order, zod schema) · integration
(each endpoint incl. soft delete & default toggling) · isolation 4-proofs · error-path
tests (duplicate code, deleting default, invalid IBAN).

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md; `apps/api/src/modules/organization/README.md` (profile resolution rules);
API_CONTRACT/OpenAPI reconciled if deviations via change process.

## 11. ACCEPTANCE CRITERIA
- All §3 endpoints contract-passing; defaults invariants hold under concurrency.
- `resolvePostProfile` fallback chain unit-verified; missing = hard error.
- Fresh tenant provisioning yields 1 default branch+warehouse+safe with balances 0.

## 12. DEFINITION OF DONE
verify green · isolation tests · docs · protocol §8 report.

## 13. DELIVERABLES
Organization module (schema+service+API+tests) + provision hook + docs.

## 14. DO NOT DO
COA/accounts · inventory/sales objects · cached money totals anywhere · changes to
platform module internals (public APIs only).
