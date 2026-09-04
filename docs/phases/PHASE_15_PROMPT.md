# PHASE_15_PROMPT — Migration Engine (SQL Server → PostgreSQL)

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 15 of 23 — the reason the whole program exists for
existing customers. You build `apps/migrator`: deterministic, replayable per-tenant
migration with dry-run, reconciliation, rollback. Read `docs/MIGRATION_ARCHITECTURE.md`
completely first — wave plan W1–W14, mapping registry style, run tables, RC blockers.
Frozen: legacy IDs preserved (`legacy_id` columns + `legacy_id_mappings`), sequences
fast-forwarded to legacy max, secrets re-encrypted, passwords never migrated,
`state=1` entries = posted, soft-deleted rows archived not balanced (RC-28 default),
money floats rounded to minor units with variance reporting (RC-31).

## 1. CURRENT PHASE
**#15 — Migration Engine**: the ETL application + run/issue/mapping persistence +
reconciliation contract R1–R7 + fixtures & CI image.

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/MIGRATION_ARCHITECTURE.md` (ALL) 4. `docs/LEGACY_DATABASE_ANALYSIS.md` (ALL)
5. `docs/LEGACY_BUSINESS_LOGIC.md` (ALL) 6. `docs/REQUIRES_CONFIRMATION.md` (ALL 🔴)
7. `docs/DATABASE_DESIGN.md` §14 (+ all domain §§)
8. `docs/ACCOUNTING_ARCHITECTURE.md` §8 9. `docs/API_CONTRACT.md` §12
10. `docs/TESTING_STRATEGY.md` §7 11. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS (verify; else stop)
All core modules P03–P12 live with service APIs (import goes through them), reporting
keys for comparisons (P14), jobs infra.

## 4. GOAL & SCOPE
### In scope
- `apps/migrator` CLI + library: connectors to SQL Server (read-only), staging schema,
  pipeline steps EXTRACT/MAP/TRANSFORM/VALIDATE/STAGE/LOAD/VERIFY per entity with
  checkpoint cursors (resumable) and idempotency via legacy_id.
- Mapping registry files for waves W1–W10 (+W13 attachments-of-record minimal, W11/W12
  flagged behind pack flags) covering: Foundation→company_profiles; SettingGeneral→
  tenant settings + posting profiles; Branches/Stocks/Safes/Banks/treasury; currencies;
  Accounts_Index→accounts (+ParentCode repair report RC-12); AccountingPeriods/locks;
  fiscal years provisional creation (RC-27 multi-db); Customers/Suppliers/Owners/
  PM_Contractor/VATClients→parties + DealPersons; Employees-lite needed for salesman
  refs (map to salesmen stub rows if hrm pack absent — rule written inline);
  Items/Category/units/ItemUnits/Itembarcodes/AltCodes/Details/Components/tax_groups/
  ItemPrices(history); Entry/Entry_sub journals (posted only) per period;
  Inv/Inv_Sub → sales/purchase invoices by RC-resolved kind mapping (config table
  `legacyDocTypeMap` seeded from analyze results + owner overrides); InvoicePayments;
  InvoiceCost; Inventory replay→ledger rebuild→compare (R3); Receipts+Sand*→vouchers
  (RC-19 mapping inline); CreditDeptNotes; CasherClosed*→shift_closes (archive-grade);
  Currency_Lastprice→fx_rates; Currency_SafeBalance→verify-only (balances recomputed);
  ZATCA artifacts→einvoice ledger as `imported` status; attachments (S3 streaming).
- Modes: analyze (distinct-value dictionaries to answer RC files per source),
  dry_run, import, reconcile, rollback per MIGRATION_ARCHITECTURE.
- Reconciliation R1–R7 implementation + JSON/PDF report.
- API per API_CONTRACT §12 (run management, issues, reconciliation fetch).
- CI fixture: anonymized mini legacy DB (scripted SQL Server container in compose
  for dev; tests use generated fixture rows via factory — no real customer data).
### Out of scope (DO NOT DO)
Continuous sync (P16), UI console (P17 uses this API), payroll/verticals data import
beyond flagged waves, modification of legacy DB (read-only claim enforced by using a
read-only login & tests asserting writer absence).

## 5. EXACT TASKS
1. Migrator skeleton + config + logger + run management (status machine per DOMAIN §5).
2. Registry core (map format per MIGRATION doc example) with validation & docs gen.
3. Implement waves W1–W10 loaders through domain services (tx, idempotent).
4. Analyze mode producing `analysis.json` sharing shape of REQUIRES_CONFIRMATION table
   (machine-readable RC answers where data-derived).
5. Reconcile + reports; rollback (tenant-scoped purge respecting FK order).
6. API endpoints + permissions `migration.view/run.execute/run.import` (seed).
7. Test suite per TESTING §7: transforms, idempotent re-run (second run zero changes),
   rollback completeness, reconciliation golden values, legacy-fixture integration.
8. Runbook doc `docs/change-log/ops/migration-runbook.md` (freeze window, final delta
   re-run, cutover checklist).
9. STATUS.md; ADMIN §12 alignment note.

## 6. DATABASE IMPACT
+3 engine tables (already specified §14 DATABASE_DESIGN) — implement now; all data
written by loaders already-designed tables; RLS bypass ONLY via migrator role inside
worker (tests assert API role cannot bypass).

## 7. API IMPACT
API_CONTRACT §12 implemented; no deviations.

## 8. SECURITY REQUIREMENTS
Read-only source credential pattern; secrets handling (encrypt-on-import, never log);
PII anonymized fixtures only; run artifacts access-controlled.

## 9. TESTING REQUIREMENTS
Per strategy §7 classes + property tests for float→numeric rounding variance bounds
(e.g., variance ≤ minor_unit/2 per row) + wave dependency order tests.

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md; registry auto-doc `apps/migrator/docs/registry-index.md`; runbook;
RC table updated where analyze mode answered items.

## 11. ACCEPTANCE CRITERIA
- Fixture legacy import completes; R1–R6 pass on fixture with expected numbers; TB and
  stock value equal legacy fixture truth; re-run idempotent; rollback leaves schema
  pristine; issues list meaningful (deduped, payloaded).
- Analyze mode prints RC answers dictionary for provided sample data.

## 12. DEFINITION OF DONE
verify green · migration test classes green · docs + runbook · protocol §8 report.

## 13. DELIVERABLES
Migrator app + registry + API + fixtures + reconciliation + runbook + docs.

## 14. DO NOT DO
Direct table writes bypassing domain services · importing plaintext secrets/passwords ·
inventing legacy enum meanings not in RC answers/analyze output (block with issue
instead) · UI work.
