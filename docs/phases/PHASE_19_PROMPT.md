# PHASE_19_PROMPT — Restaurant POS Pack (vertical)

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 19 of 23 — first vertical pack; gated per tenant by
feature flag `pack.pos`. Legacy coverage being re-platformed (must not shrink,
rule §24): tables & categories (`Tables`, `Cat_Table`), open-table ↔ invoice link,
order item events (`Table_Order`), order types (`SettingOrderMethods`: local/takeaway/
family/car/table/hosting + default), daily order numbering (`DailyOrderCounter`/
`OrderNumbers`), kitchen printing categories (ItemsCategory.printer, PrintAllItems/
PrintItemsSeparately), POS display/payment settings (`SettingsPOSDisplay`,
`SettingPayMethods`, `SettingPayForm`), additions/modifiers (`Additions`,
`CategoryNotes.AddPrice`), shift close already core (P12) with POS totals buckets.
Core sales/posting/ledger services are reused through public APIs ONLY.

## 1. CURRENT PHASE
**#19 — Restaurant POS**: backend pack + admin POS screens (floor map, order flow).

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/DATABASE_DESIGN.md` §15 (pos) 4. `docs/LEGACY_DATABASE_ANALYSIS.md` §1.4, §7
5. `docs/LEGACY_BUSINESS_LOGIC.md` BL-12 6. `docs/ADMIN_PANEL_MASTER_REQUIREMENTS.md` §14
7. `docs/API_CONTRACT.md` (extension style §2) 8. `docs/STATUS.md`.

## 3. PREVIOUS PHASE OUTPUTS (verify; else stop)
Sales/treasury/inventory modules + admin shell + reporting registry.

## 4. GOAL & SCOPE
### In scope
- Tables+RLS: `dining_tables`, `table_categories`, `order_events` (lifecycle of items
  fired/voided per table before invoice), pos config via tenant_settings typed keys
  (order methods, pay methods visible, kitchen routing, display layout numbers).
- Endpoints (new sub-resource namespace `/pos/…`, documented into API_CONTRACT via
  its own section in this change): floors/tables CRUD; open table → draft order state;
  add/void items events; **send-to-invoice** composes sales invoice draft (kind sale,
  `order_type`, `table_no` set — columns already exist from P10) with daily order
  number sequence `pos_order` per branch+day; merge/split by `combined_into`
  (pre-existing column); close via pay → post path (P10 service).
- Additions/modifiers: `Additions` items (priced) + per-category notes-with-price;
  line linkage stored in `sales_invoice_lines.description`-extended jsonb column
  `modifiers jsonb` (CR to DATABASE_DESIGN §10 — additive nullable column).
- Admin POS UI: floor map grid with table status/balance chips; order screen (item
  grid by category, additions pop-up, notes, qty/price edit w/ caps); pay & close
  dialog (multi-tender); kitchen print config page (printer names per category);
  daily numbers reset view; shift close reuses P12 screen (POS buckets visible).
- Reporting hooks: sales-by-ordertype now populated (P14 key already existed).
### Out of scope (DO NOT DO)
Kitchen display hardware integration (print config data only) · delivery apps ·
loyalty/points · waiter app (mobile) · changes to core posting/totals logic (CR only).

## 5. EXACT TASKS
1. CR + migrations + RLS for 3 tables + modifiers column; sequences `pos_order`.
2. POS module (depends sales only via public services) + endpoints + flag gating
   (disabled → 404) tests.
3. Order events lifecycle + firing rules + audit.
4. Compose-invoice service + merge/split tests + payment close path via treasury/P10.
5. Admin UI screens (per master §14) wired end-to-end incl. states kit reuse.
6. E2E: open table → add items w/ additions → merge second table → pay cash+card →
   shift close shows buckets.
7. STATUS.md; ADMIN master §14 coverage; API_CONTRACT POS section; LEGACY_COMPAT note
   that desktop POS later maps via mappers (phase 16 extension note only).

## 6. DATABASE IMPACT
+3 tables, +1 nullable jsonb column on `sales_invoice_lines` (CR), RLS everywhere.

## 7. API IMPACT
New `/pos/*` section appended to API_CONTRACT (this phase owns append; no edits to
existing paths).

## 8. SECURITY REQUIREMENTS
Perms `pos.view/operate/priceoverride/tables.manage/config.manage` seeded; void-item
requires reason audit; price edits respect caps.

## 9. TESTING REQUIREMENTS
Unit (compose/merge/split, numbering per day/branch), integration endpoints, gating
tests, e2e flow, isolation proofs.

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md; API_CONTRACT append; ADMIN master markers; module README (event model).

## 11. ACCEPTANCE CRITERIA
- E2E flow green; daily order numbers continuous per day; composed invoice identical
  in effects to native sale (journal/ledger checks).

## 12. DEFINITION OF DONE
verify green · e2e · docs (incl. CRs listed) · protocol §8 report.

## 13. DELIVERABLES
POS backend pack + admin POS UI + docs appendices.

## 14. DO NOT DO
Core module internals edits (services' public APIs only) · hardware drivers ·
delivery integrations · unflagged behavior changes for non-POS tenants.
