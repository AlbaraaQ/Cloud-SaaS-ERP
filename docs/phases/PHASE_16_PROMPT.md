# PHASE_16_PROMPT — Legacy Desktop Compatibility API Gateway

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 16 of 23. Direction is frozen: **Desktop → API →
Cloud DB**; the cloud NEVER touches the legacy database (offline migrator excepted).
Legacy sync concepts to honor: device registration, cursor-based pulls
(`SyncEntities.LastSyncId`), push of documents with legacy `GlobalID` as the
idempotency key (`Inv.Sync`, `CloudID` columns prove this existed), per-branch sync
settings (`SettingSync` intervals, sync types, branch distribution).

## 1. CURRENT PHASE
**#16 — Compat Gateway**: lets the existing C# desktop authenticate and exchange data
with the new backend after migration, without any schema coupling.

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/MIGRATION_ARCHITECTURE.md` §9 4. `docs/API_CONTRACT.md` §12 (compat part)
5. `docs/SECURITY_ARCHITECTURE.md` §2 (device keys) 6. `docs/LEGACY_BUSINESS_LOGIC.md`
   BL-1/BL-12 7. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS (verify; else stop)
Core modules + migration engine (legacy_id lookups available).

## 4. GOAL & SCOPE
### In scope
- Tables+RLS: `compat_devices` (tenant_id, name, api_key_hash, branch_id, cursors
  jsonb, status, last_seen_at) — add via change process entry + DATABASE_DESIGN append.
- Endpoints per API_CONTRACT §12: `/compat/auth/device` (key→scoped token),
  `/compat/master/items?since=`, `/compat/master/parties?since=`,
  `/compat/master/accounts?since=`, `/compat/master/tax-groups?since=`,
  `/compat/docs/sales-invoice` POST (legacy-shaped DTO → mapped → sales module),
  `/compat/docs/voucher` POST, `/compat/sync/cursor` GET/POST (checkpoint),
  `/compat/docs/status?legacyId=` lookup.
- Mapping layer `compat-mappers`: legacy request shapes (document them inline from
  legacy columns: Inv fields list) → canonical DTOs; unknown enum ints rejected with
  clear code unless mapping configured per device (config jsonb `enum_maps`).
- Idempotency: header or legacy GlobalID both supported (duplicate → returns original).
- Pull cursors: updated_at-watermark with tie-breaker id column; per-entity cursor store.
- Device revocation + last_seen + per-device branch scope enforcement.
### Out of scope (DO NOT DO)
Modifying the desktop app itself (owner-side), real-time socket channels, sync of
every legacy entity (only listed masters + docs), conflict-resolving UI.

## 5. EXACT TASKS
1. CR + migration for `compat_devices` + RLS; permissions `compat.manage`, `compat.sync`.
2. Device auth guard (hashed key compare, scope injection) + tests (revoked/mismatched).
3. Master pull endpoints (paged, since-cursor correct, deletion tombstones included
   for soft-deleted rows) — tests with edited rows between pulls.
4. Doc push endpoints + mappers + validations; GlobalID idempotency test (double push).
5. Status lookup by legacy id; cursor endpoints.
6. Rate limit per device; audit channel for compat pushes (source=device).
7. Integration test simulating desktop: register → pull → push sale → pull includes it
   pre-numbered with cloud sequence preserved + legacy number stored.
8. STATUS.md; `docs/LEGACY_COMPAT.md` (new Level-C doc describing the wire contract
   for the desktop team — request/response examples included).

## 6. DATABASE IMPACT
+1 table RLS; cursors stored per device.

## 7. API IMPACT
Implements contract §12 compat exactly; new table (CR listed).

## 8. SECURITY REQUIREMENTS
Key hashing (SHA-256+pepper env), rotation endpoint, branch scoping, replay protection
via idempotency, no permission elevation (device token carries device scope only).

## 9. TESTING REQUIREMENTS
Unit mappers (enum map edges), integration flows incl. duplicate push & cursor
paging, isolation (device of tenant A cannot touch B), rate-limit test.

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md; new `docs/LEGACY_COMPAT.md` wire spec; ADMIN §12 devices manager section tick.

## 11. ACCEPTANCE CRITERIA
- Full simulated desktop cycle passes tests incl. duplicate-push dedupe and
  cursor-correct second pull (no re-delivery without changes).
- Mapped sale posts a normal invoice (journal + ledger effects identical to API sale).

## 12. DEFINITION OF DONE
verify green · test classes · docs · protocol §8 report.

## 13. DELIVERABLES
Compat gateway + device management + mappers + wire doc + tests.

## 14. DO NOT DO
Desktop code changes · schema mirroring of legacy tables · broadening entity list ·
long-polling/websocket infra.
