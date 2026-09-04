# MIGRATION_ARCHITECTURE

> Level B — CANONICAL (doubles as MIGRATION_PLAN; README references it as such).
> Direction: `Legacy SQL Server (offline) → Migration Layer → New PostgreSQL`.

## 1. Goals & Non-Goals

Goals: per-tenant, replayable, auditable migration producing **provably equal** books
(TB + stock value + AR/AP) or explained variances; multi-year source DBs merged;
zero impact on other tenants; legacy IDs preserved everywhere.
Non-goals: continuous sync (that's P16 compat gateway, API-based); fixing legacy junk
(junk tables archive-only); importing secrets in plaintext (never).

## 2. Tooling: `apps/migrator` (Node + TypeScript CLI/job worker)

- Reads SQL Server via `mssql` driver (read-only login), or from `.bak`-restored
  staging instance — **never** the production legacy DB during business hours.
- Writes target ONLY via `migration writer services` that call the same domain services
  / repositories as the API (same invariants), inside an explicit `migration_runs` scope,
  using the `migrator` DB role (BYPASSRLS — only role allowed).
- Step pipeline per entity: `EXTRACT → MAP → TRANSFORM → VALIDATE → STAGE → LOAD → VERIFY`.
  Each step emits run artifacts (rows in/out, rejects, timings) into `migration_issues`/
  run `summary jsonb`.

## 3. Modes

- `analyze`: schema diff vs known baseline + distinct-value dictionaries (answers RC-01..).
- `dry_run`: full pipeline minus LOAD; produces preview counts, integrity report,
  rounded-TB preview, variance prediction. Default first step for any customer.
- `import`: transactional per module batch, resumable (`cursor checkpoints` per entity),
  idempotent via `legacy_id` unique keys (re-run = skip/refresh, flag conflicts).
- `reconcile`: post-load checks (see §8) into a signed JSON+PDF report.
- `rollback`: deletes all rows linked to the run per tenant (reverse dependency order)
  or drops a never-activated tenant; blocked once business activity exists outside the run.

## 4. Mapping Registry (data-driven)

`apps/migrator/src/registry/*.map.ts` one file per entity, declarative:

```ts
// parties.map.ts (illustrative, not code deliverable of this phase)
{ legacyTable: 'Customers', target: 'parties', key: 'id',
  columns: { id:'legacy_id', name:'name_ar', tax_no:'tax_no', maxdepit:'credit_limit',
             AccountCode:'__resolveAccount', ISCedit:'is_credit', Pricing:'__priceList' },
  transforms: ['trimNames','nationalAddressJson','defaultSAR'],
  validators: ['uniqueTaxNoWarn','accountMustExist'],
  kind: 'customer' } // Suppliers → kind 'supplier'
```

Every mapping file documents its legacy quirks (float rounding, soft-deleted handling,
default fills) inline; registry index is generated into the run report.

## 5. Legacy ID Preservation (hard rule)

- Every migrated row keeps `legacy_source='data16'` (or db name) + `legacy_id` =
  legacy PK verbatim (`InvGlobalID`, `Entry.GlobalID`, `id` ints as text…).
- Junction `legacy_id_mappings(tenant, entity, legacy-source, legacy_pk → new_id)`
  powers cross-entity rewiring (invoice→lines, invoice→journal, voucher→invoice
  allocations) and future lookups from old paper documents.
- Document numbers resumed: migrator fast-forwards `document_sequences.current_value`
  to legacy max per (branch,type) so new docs never collide with migrated ones (BL-1).

## 6. Run & Engine Tables

`migration_runs`, `legacy_id_mappings`, `migration_issues` (see `DATABASE_DESIGN` §14).
Issue severities: `info | warn | error | block`. Import aborts a module on `block`
(e.g., unbalanced journal with no provable fix); `error` rows quarantined for manual
import via admin re-run.

## 7. Wave Plan (order == dependency, oldest fiscal year first per RC-27)

W1 tenant bootstrap (company/profile/currencies/settings from `Foundation`,`SettingGeneral`)
→ W2 org (branches, warehouses, safes/banks, sequences) → W3 COA + fiscal years/periods
(+ opening balances batch) → W4 parties & contacts → W5 catalog (categories, units,
items, barcodes, prices, tax) → W6 legacy journals per period (state=1 only; drafts as
drafts) → W7 invoices & lines (sales then purchases; computed checks) → W8 inventory
rebuild: replay documents into ledger → balances snapshot **compared** to legacy
functions output → W9 vouchers + allocations → W10 shift closes, FX, petty tables →
W11 payroll & HR (if licensed) → W12 vertical packs data (per enabled packs) →
W13 einvoice artifacts (UUID/hash/QR/status → submissions ledger; certificates encrypted)
→ W14 attachments to S3 (from file paths/image columns). Each wave is independently
re-runnable.

## 8. Reconciliation Contract (the "provable numbers")

Mandatory checks embedded in `reconcile` mode; all must pass or be waived in writing:
R1 TB equality per fiscal year (Σ debit/credit equality per account, tolerance = currency
minor unit per RC-31 with explained rounding variance list).
R2 AR/AP per party totals vs legacy account balances.
R3 Stock qty per (item, warehouse) vs legacy `TotalItemStockInventory` output; stock value
vs `InventoryCostForBalanceSheet` (variance list per item).
R4 Invoice count/totals per (branch, type, year) vs `Inv` aggregates.
R5 Voucher totals per cash location.
R6 VAT report totals vs legacy VAT fields.
R7 Sequence continuation sanity (no duplicate legacy doc number in target).
Output: `reconciliation.json` + human PDF attached to run; tenant activation blocked
until R1–R4 pass or owner signs waiver (recorded in run summary).

## 9. Legacy Desktop Compatibility (post-migration continuity)

`compat` API (P16): device-key auth; pulls master deltas; pushes documents (validated
by the same services). Explicitly **not** a DB bridge. Conflict policy: cloud wins;
desktop submission IDs deduped via `Idempotency-Key = legacy GlobalID`.
Sync cursors mirror `SyncEntities.LastSyncId` concept per entity & device.

## 10. Risks & Containment

Dirty REFERENCES (orphan lines) → quarantine lists. Float drift → rounding variance
report. Unknown enums (RC-*) → analyze mode blocks import. Multi-year merges → per-year
runbooks + opening re-balance between years. Large base64 images → S3 streaming import.
Each risk has owner sign-off line in the run report.

## 11. Operator UX (admin → migration screens are in P17 scope)

Upload/analyze → dry-run dashboard (counts, issues) → confirm import → live progress →
reconciliation report → activate tenant. (Screens spec'd in ADMIN master §14.)
