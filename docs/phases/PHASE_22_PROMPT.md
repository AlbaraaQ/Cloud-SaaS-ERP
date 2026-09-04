# PHASE_22_PROMPT — Niche Verticals & E-commerce Integration Pack

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 22 of 23; flags: `pack.optics`, `pack.tailoring`,
`pack.marina`, `pack.fitment`, `integration.salla`. Legacy coverage (rule §24):
optics `Glasses` (SPH/CYL/AX/ADD/IPD per orientation) + `Other_Column` R1..L5 grid
(store as typed jsonb, RC-21); tailoring `CustomerMeasurements` (per-customer
measurements card; `AtiveCustMeasur` setting); marina `Marine`+`GroupMarine`
(hour/half-hour/offer pricing) + `Booking(+Addition)` → `RentInvoice` (period pricing,
insurance, companions) + `Violation` + `Owners` percent + `OperationPlan/OperPlanSub/
MarineOperPeriod/RentPeriodSub`; fitment `EquipMakes/EquipModels` +
`ItemVehicleFitment` (year ranges) + `ItemAlternativeCodes` (already core);
Salla integration `SallaSettings` OAuth + `Items_Salla_Sync` + `Salla_Export_Log` +
`Branch_safes_salla` + `vw_Items_Salla_Status` diff semantics.

## 1. CURRENT PHASE
**#22 — Niche verticals + Salla**: five bounded modules sharing nothing but core APIs.

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/DATABASE_DESIGN.md` §15 (niche) 4. `docs/LEGACY_DATABASE_ANALYSIS.md` §4/§5/§7
   rows for each niche + §9 secrets 5. `docs/LEGACY_BUSINESS_LOGIC.md` BL-12
6. `docs/ADMIN_PANEL_MASTER_REQUIREMENTS.md` §14 7. `docs/REQUIRES_CONFIRMATION.md`
   RC-21/26 8. `docs/SECURITY_ARCHITECTURE.md` §9 (OAuth tokens encrypted!)
9. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS
Catalog, sales, parties, files, admin shell, reports registry, jobs infra.

## 4. GOAL & SCOPE
### In scope (each gated by its flag)
- **Optics**: `optical_prescriptions` CRUD attached to party + invoice line link;
  grid jsonb schema typed; print section on invoice print template (conditional).
- **Tailoring**: `customer_measurements` per party (versioned by CreatedAt) + display
  in invoice/party context.
- **Marina**: `vessels`, `vessel_groups` + pricing rows, `vessel_owners` (party link +
  percent), `bookings(+additions)` → `rental_invoices` (standalone invoice source
  using sales module service with service-items; insurance/companions fields in
  metadata jsonb: CR allowed additive), `violations`, operation planning list
  (order of vessels per group per day) minimal CRUD.
- **Fitment**: `vehicle_makes/models`, `item_vehicle_fitment`, compatibility lookup
  endpoint used by catalog UI (find items for vehicle & vice versa).
- **Salla**: connection (OAuth flow storing encrypted tokens), item export queue
  (create/update/price/qty) with per-item sync status + diff detection parity with
  legacy view (price/cost/name/qty changed since last export), export log with
  request/response, branch/warehouse mapping table, retry with backoff; inbound
  orders webhook receiver → creates sales invoice (flagged; signature verification
  required) — webhook endpoint documented + secured.
### Out of scope (DO NOT DO)
Real Salla sandbox calls in CI (mock client) · POS optics workflows · rental deposits
accounting beyond insurance field mapping · full operation-planning optimizer.

## 5. EXACT TASKS
1. Migrations+RLS per module tables (+ additive metadata fields via CRs where noted).
2. Services+endpoints namespaces `/optics /tailoring /marina /fitment /integrations/salla`
   appended to API_CONTRACT; flags 404 test.
3. Salla: OAuth endpoints, client wrapper (mockable), outbox-based export workers,
   diff-detector matching legacy view CASE logic, webhook verify+ingest.
4. Admin UI per master §14 niches (forms, pickers, sync monitor).
5. E2E (marina): group pricing → booking → rent invoice, owner % stored; (salla):
   connect(mock)→edit item→diff flag→export→log row.
6. STATUS.md; API_CONTRACT appends; masters markers; RC-21/26 notes to
   `REQUIRES_CONFIRMATION.md` answers column (jsonb/typed rows decision documented).

## 6. DATABASE IMPACT
+~14 tables all RLS; encrypted columns for tokens; additive metadata CRs.

## 7. API IMPACT
Appends only; perms per module seeded (`*.view/manage`, salla `integration.manage`).

## 8. SECURITY REQUIREMENTS
Webhook HMAC verification + rate limit; OAuth state nonce; tokens encrypted; marina
owner PII minimal.

## 9. TESTING REQUIREMENTS
Unit (pricing calc, diff detector truth table, measurements versioning), integration,
webhook tamper tests, isolation, e2e listed.

## 10. DOCUMENTATION REQUIREMENTS
STATUS; module READMEs; masters markers; LEGACY marks for the five legacy families.

## 11. ACCEPTANCE CRITERIA
- All flags off → zero exposure (routing + perms tests).
- All flags on → flows work; Salla diff truth table matches legacy view outcomes on
  fixtures.

## 12. DEFINITION OF DONE
verify green · e2e · docs · protocol §8 report.

## 13. DELIVERABLES
Five niche modules + Salla integration + UI + docs.

## 14. DO NOT DO
Payment gateways for rentals · hardware kiosk flows · core module edits (CR only) ·
scope expansion into CRM/marketing.
