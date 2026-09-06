# PHASE 05 IMPLEMENTATION REPORT

Organization Structure — company profile, branches, warehouses, cash locations with
currency balances, currencies & FX rates, price lists, and branch posting profiles.

## Phase Report — PH-05

**Delivered**

- **Schema & migration** — `packages/database/migrations/0002_organization.sql`
  (424 lines) creates the **ten** tables of `DATABASE_DESIGN §5` (+ §3 for
  `currencies`/`fx_rates`): `company_profiles, branches, warehouses, cash_locations,
  cash_location_balances, currencies, fx_rates, price_lists, price_list_items,
  branch_posting_profiles`. `migrations/down/0002_organization.down.sql` (56 lines)
  reverses it. Verified against a real PostgreSQL 16 cluster:
  `up → 26 tables / 21 policies / 21 relations with ENABLE+FORCE RLS / 73 indexes`,
  `up again → 0 applied, 3 skipped`, `down → 16 tables / 11 policies`, `up again → 26
  tables`, with `erp_api` still `NOBYPASSRLS` at every step.
- **Indexes created** (§5.1 asks for the full list):

  | Table | Index | Purpose |
  |---|---|---|
  | `company_profiles` | PK `(tenant_id)` · `company_profiles_logo_file_idx` | one row per tenant; logo lookup |
  | `branches` | `branches_tenant_code_key` (UQ, `WHERE deleted_at IS NULL`) · `branches_tenant_default_key` (UQ, `WHERE is_default AND deleted_at IS NULL`) · `branches_tenant_active_idx` · `branches_tenant_legacy_key` (UQ, `WHERE legacy_id IS NOT NULL`) | unique live code; single default; list filter; migrator re-run safety |
  | `warehouses` | `warehouses_tenant_code_key` (UQ, partial) · `warehouses_tenant_default_key` (UQ, partial) · `warehouses_tenant_branch_idx` · `warehouses_tenant_legacy_key` (UQ, partial) | as above + branch scoping |
  | `cash_locations` | `cash_locations_tenant_kind_default_key` (UQ, `(tenant_id, kind) WHERE is_default AND deleted_at IS NULL`) · `cash_locations_tenant_branch_idx (tenant_id, branch_id, kind)` · `cash_locations_tenant_legacy_key` (UQ, partial) | one default **per kind**; branch/kind lists |
  | `cash_location_balances` | PK `(cash_location_id, currency_code)` · `cash_location_balances_tenant_idx` | one balance per currency; RLS-friendly tenant scan |
  | `currencies` | PK `(tenant_id, code)` · `currencies_tenant_base_key` (UQ `(tenant_id) WHERE is_base`) | enabled codes; exactly one base |
  | `fx_rates` | `fx_rates_tenant_pair_from_key` (UQ `(tenant_id, from_code, to_code, effective_from)`) · `fx_rates_tenant_pair_effective_idx` (`… , effective_from DESC`) | one quote per pair per day; "newest on or before" read |
  | `price_lists` | `price_lists_tenant_name_key` (UQ, partial) · `price_lists_tenant_default_key` (UQ, partial) · `price_lists_tenant_legacy_key` (UQ, partial) | unique live name; single default |
  | `price_list_items` | `price_list_items_scope_key` (UQ `(price_list_id, coalesce(item_id, NIL), min_qty)`) · `price_list_items_list_idx` · `price_list_items_tenant_idx` | upsert key (a quantity break is not a duplicate) |
  | `branch_posting_profiles` | `branch_posting_profiles_scope_key` (UQ `(tenant_id, coalesce(branch_id, NIL), doc_type)`) · `branch_posting_profiles_tenant_doc_type_idx` | one mapping per scope; resolution read |

- **Contracts** (`packages/contracts/src/organization/*`, `contractVersion 0.5.0`) —
  strict zod DTOs for every resource, plus the shared primitives the rest of the system
  will reuse: `moneyStringSchema` / `fxRateStringSchema` (decimal strings, never JSON
  numbers), `orgCodeSchema` (upper-cased master-data code), `nationalAddressSchema`
  (ZATCA), `isValidIban` / `maskIban` (ISO 13616 mod-97, not a length check),
  `docTypeSchema` (18 frozen codes), `postProfileV1Schema` + `POST_PROFILE_ACCOUNT_KEYS`,
  and the new error code `ACCOUNT_PROFILE_MISSING`. Every write schema is `.strict()`, so
  an unknown key is a 400 rather than a silently ignored field.
- **Resources** (`apps/api/src/modules/organization/`) — branches, warehouses, cash
  locations (+ read-only `GET /{id}/balances`), currencies, fx rates, price lists
  (+ `/{id}/items`), branch posting profiles and the 1:1 company profile. Shared rules
  live in `shared/org-support.ts` so ten services express them identically: optimistic
  concurrency (`409 VERSION_CONFLICT`), default-flag switching, branch-scope narrowing,
  unique-violation mapping and the soft-delete stamp.
- **Single-default invariants** — one default branch/warehouse/price list per tenant, one
  **per kind** for cash locations, one base currency. Partial unique indexes make two
  defaults impossible; a transaction-scoped advisory lock (`pg_advisory_xact_lock`) turns
  concurrent promotions into a queue instead of a lost race, which is why eight
  simultaneous `PATCH {isDefault:true}` calls all return 200 and exactly one default
  survives. The first row a tenant creates is automatically the default; clearing the flag
  and deactivating or deleting the default are 422s.
- **Branch scope** (`MULTI_TENANCY §2`) — every list and detail read is narrowed by the
  membership's `branch_scope`, further narrowed by `X-Branch-Id`. A row outside the scope
  is **404, never 403**: the caller must not learn that a branch it cannot see exists.
- **FX** (`§5.4`) — `resolveFx(tenantId, from, to, date?)` answers with the rate **and**
  its provenance: `identity` (no read), `direct`, `inverse` (`1/rate`) or `triangulated`
  (`from→base × base→to`, reporting the pivot in `via` and the **staler** of the two legs
  in `effectiveFrom`). All arithmetic is decimal.js at `FX_SCALE = 10`, `ROUND_HALF_UP`;
  intermediate legs keep full precision so a derived rate never rounds twice. An
  unreachable pair is a 422 — no rate is ever synthesised.
- **Posting profiles** (`§5.5`) — one versioned JSONB `PostProfileV1` per
  `(branch, doc_type)`, replacing the legacy global `SettingGeneral.*Acc` and per-branch
  `Branches.*Acc` columns. `resolvePostProfile` walks branch+docType → branch+`'*'` →
  tenant+docType → tenant+`'*'` and fails hard with `422 ACCOUNT_PROFILE_MISSING`; the
  precedence function is pure and unit-tested away from the database.
- **Company profile & logo** (`§5.3`) — `GET /company-profile` 404s until written, `PUT`
  upserts against `tenant_id`. `logoFileId` must be a **finalised image** file of the same
  tenant, and the module registers the `company_profile` validator in the
  `FileAttachmentRegistry` that PHASE_04 deliberately shipped empty — so a logo can only
  be attached to the caller's own profile (`entityId === tenantId`).
- **Sensitive data & audit** (`§8`) — IBANs are masked in list responses, full only on the
  detail read, and masked **inside the audit row** as well. Cash locations, posting
  profiles and the company profile write their own audit rows inside the same transaction
  as the change, with a real `before`/`after`, and call `markRequestAudited()` so exactly
  one row is produced per request.
- **Provisioning** (`§5.7`) — `OrgProvisioningService.provisionOrgDefaults(tenantId,
  options?)` creates the base currency row, a `MAIN` branch, a warehouse, a safe with a
  **zero balance row** and a default price list. Idempotent (second call returns the same
  ids with `created: false` and writes nothing) and available as `provisionInTx` so
  PHASE_03's tenant factory and PHASE_15's migrator can fold it into their own
  transaction. It writes through the tables, not the CRUD services, because it runs
  outside any HTTP request.
- **PHASE_04 hand-off closed** — the deferred FK `document_sequences.branch_id →
  branches (id) ON DELETE RESTRICT` is added by this migration; the P04 numbering test was
  updated to use real branches, which is precisely the orphan the FK prevents.

**Deviations**

| CR | Deviation | Why |
|---|---|---|
| CR-006 | `cash_locations.account_id`, `warehouses.inventory_account_id`, posting-profile account ids and `price_list_items.item_id` are nullable uuids with **no FK** | `accounts` arrives in PHASE_07 and `items` in PHASE_06; a tenant must be provisionable now. Each column carries a `ValidatedAtRuntime: P0x` comment, and `POST_PROFILE_ACCOUNT_KEYS` is exported for PHASE_07 to iterate. |
| CR-007 | Added `organization.companyprofile.view` and `organization.postingprofile.view`; `SECURITY_ARCHITECTURE §5` row rewritten to the per-entity codes the registry already used | Otherwise reading the company tax number would require the permission to change it. |
| CR-008 | `DELETE` (soft) on branches/warehouses/cash-locations/price-lists, hard on `fx_rates`, `price_list_items`, `branch_posting_profiles`; plus read-only `GET /cash-locations/{id}/balances`, `GET /fx-rates/resolve`, `GET /branch-posting-profiles/resolve` | `API_CONTRACT §3` listed no `DELETE`, but soft delete on master data is frozen in `PROJECT_CONTRACT` and required by `PHASE_05_PROMPT §5.2/§8`; the two resolve routes expose the functions §5.4/§5.5 require. |
| CR-009 | Ten tables, not the "+9" of `PHASE_05_PROMPT §6` | §4 of the same prompt and `DATABASE_DESIGN §5` both name ten; nothing was merged to reach nine. |

Also worth recording, though not a contract deviation: activate/default toggles are PATCH
fields (`isActive`, `isDefault`) rather than dedicated routes, so one optimistic-concurrency
token covers the whole row.

**Files**

- **Created — 40.** Contracts `packages/contracts/src/organization/` (10 files:
  `common, branches, warehouses, cash-locations, currencies, price-lists,
  posting-profiles, company-profile, index` + `organization.spec.ts`);
  `packages/database/src/schema/organization.ts` (372 lines) and the two migration files;
  the API module `apps/api/src/modules/organization/` (21 TS files + `README.md`) covering
  `shared/`, `branches/`, `warehouses/`, `cash-locations/`, `currencies/`,
  `price-lists/`, `posting-profiles/`, `company-profile/`, `provisioning/`; five test
  suites in `apps/api/test/` (`organization-structure`, `organization-money`,
  `organization-profiles`, `organization-provisioning`, `isolation-organization`).
- **Modified — 18.** `packages/contracts/src/{index,errors,permissions}.ts`,
  `packages/database/src/{rls.ts,rls.spec.ts,schema/index.ts}`,
  `apps/api/src/app.module.ts` (registers `OrganizationModule` after
  `PlatformServicesModule`), `apps/api/test/{fixtures,isolation,sequences}.spec.ts`
  (organization permission set, reusable tenant, the new RLS table list, real branches for
  numbering), `packages/contracts/openapi.json` (28 → **48 paths**), `apps/api/package.json`
  + `pnpm-lock.yaml` (decimal.js), and the docs listed below.

**Tests**

| Class | Where | Count |
|---|---|---|
| Unit — contracts | `packages/contracts/src/organization/organization.spec.ts` | 19 |
| Unit — services | `org-support.spec.ts` (FX arithmetic, version, unique violation, timestamps) · `posting-profiles.precedence.spec.ts` (all four rungs + two negatives) | 15 |
| Integration | `organization-structure` (18) · `organization-money` (15) · `organization-profiles` (11) · `organization-provisioning` (5) | 49 |
| Isolation | `isolation-organization.spec.ts` — four proofs on `branches` and on `cash-locations` (incl. the balances sub-resource as the export proof), RLS probe on all ten tables, forged-tenant INSERT/UPDATE, tenant-scoped FX | 5 |
| **New this phase** | | **88** |

`pnpm run verify` → **exit 0**: typegen → `tsc --noEmit` → lint (8 projects) → build
(8 projects) → API smoke → tests (`config 10`, `contracts 34`, `database 7`,
`api 217` = **268**) → OpenAPI export (**48 paths**).

Behaviours specifically proved: duplicate code 422 · unknown key 400 · stale version 409 ·
default cannot be cleared/deactivated/deleted · branch with dependents cannot be deleted ·
soft-deleted row invisible everywhere and its code re-usable · branch-scoped membership
sees one branch and no foreign cash locations · `X-Branch-Id` outside scope 403, inside
scope narrows · IBAN masked in list, full in detail, masked in audit · invalid IBAN
rejected by checksum · safe with bank block 422 · zero balance seeded per cash location ·
FX identity/direct/inverse/triangulated/as-of/duplicate/unknown-currency · only the rate
mutable on an FX row · price-list item upsert by `(item, minQty)` and hard delete ·
currency in use cannot be deactivated · company profile 404→PUT→GET, upsert not duplicate,
logo must be finalised + image + this tenant's profile · posting-profile chain from tenant
wildcard to branch override, other branch never borrows · `ACCOUNT_PROFILE_MISSING` ·
provisioning idempotence and KWD 3-minor-unit base · 8 concurrent default promotions ·
8 concurrent default cash locations across two kinds.

**Docs updated**

`docs/STATUS.md` (Phase-05 row + notes) · `docs/PHASE_05_IMPLEMENTATION_REPORT.md` (this
file) · `docs/API_CONTRACT.md §3` (+ `ACCOUNT_PROFILE_MISSING` in the code list) ·
`docs/SECURITY_ARCHITECTURE.md §5` (organization permission row) ·
`docs/ADMIN_PANEL_MASTER_REQUIREMENTS.md §2` (alignment check — the backend exposes rate
and posting-profile previews, masked IBANs, PATCH toggles and soft delete) ·
`docs/change-log/CHANGE-REQUESTS.md` (CR-006…CR-009) ·
`apps/api/src/modules/organization/README.md` (resolution rules, invariants, provisioning).

**CRs opened**

CR-006, CR-007, CR-008, CR-009 — all APPROVED (applied) above.

**Seeds / env changes**

None. No new environment variable. The two new permission codes are seeded from
`permissionRegistry` by `packages/database/src/seed.ts`, which already upserts the whole
code list, so no data migration is required. One dependency added: `decimal.js` in
`apps/api` (imported as `import { Decimal } from 'decimal.js'` — the default import does
not type-check under `NodeNext` + `verbatimModuleSyntax`).

**Follow-ups**

- **PHASE_06** — add the `price_list_items.item_id → items(id)` FK and drop the nullable
  placeholder.
- **PHASE_07** — add the account FKs (`cash_locations.account_id` NOT NULL,
  `warehouses.inventory_account_id`), and validate posting-profile account ids against
  `accounts` with `ACCOUNT_NOT_POSTABLE`; iterate `POST_PROFILE_ACCOUNT_KEYS`.
- **PHASE_12** — write `cash_location_balances`; this phase only seeds and reads them.
- **Tenant bootstrap** — no code path creates tenants yet (fixtures and the seed do), so
  `provisionOrgDefaults` is currently called explicitly. Whichever phase introduces tenant
  creation must call it in the same transaction.
- **Fiscal calendar viewer** (ADMIN §2) belongs to the accounting phases; nothing in this
  phase blocks it.
