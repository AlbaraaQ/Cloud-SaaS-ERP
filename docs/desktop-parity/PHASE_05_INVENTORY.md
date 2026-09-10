# Phase 05 — المخزون (stock documents, transfers, counts, traceability)

Date: 2026-09 · Status: ✅ done
Sources: `Desktop_ERP/SmartAuditERP/Form_WPF/frmInvInOutput.xaml.cs` (3,120 lines),
`frmInventoryTransfer*`, `frmReGenerateEntries.xaml.cs`, `Class/Inventory.cs`
(`UpdateItemStock`), `Class/ItemOper.cs`.

---

## 1. What the desktop did — and what the cloud refuses to copy

| Desktop behaviour | Where | Cloud before Phase 05 | Cloud after Phase 05 |
|---|---|---|---|
| إدخال مخزني (invType 4) / إخراج مخزني (invType 5) written straight into `Inv`/`InvDetails` **with `Entry entry = null`** | `frmInvInOutput` save path | no document at all — only raw `POST /inventory/ledger/record` | `stock_vouchers` + `POST /inventory/vouchers/:id/post` |
| The ledger is repaired afterwards by a special tool that retro-fits an entry (invType 4 → account `4200003`) | `frmReGenerateEntries` | n/a | not needed: **every** stock document posts its own balanced journal |
| بضاعة أول المدة (invType 9) | `frmInvInOutput` with `OpeningFlag` | raw `docType:'opening'` movement, no journal | voucher kind `opening`, numbered `OP-…`, credits بضاعة أول المدة |
| مناقلة between branches/warehouses (invType 8) | `frmInventoryTransfer` | `stock_transfers` moved quantity only; **no journal, and the send leg crashed** (`in_transit` violated a 0006 CHECK) | send → Dr بضاعة تحت التحويل / Cr المخزون; receive → the mirror |
| جرد وتسوية — one item at a time, approval in the same dialog | `frmInvInOutput` | multi-line rows existed but had to be posted with a journal the browser built first | `POST /inventory/adjustments/:id/post` builds the entry server-side |
| أرقام تسلسلية / دفعات / تواريخ صلاحية | `ItemOper`, `frmItems*` | lists only; the master-data flags were not even settable | item card carries `minQty`/`maxQty`/`trackLot`/`trackSerial`; documents enforce them |

**The rule this phase adds:** a stock document that moves quantity *must* move value in
the same transaction. Sales and purchases already post to `inventoryAccountId`; a
quantity-only document would leave the ledger permanently disagreeing with the stock
balance — the exact drift `POST /inventory/balances/recompute` exists to detect.

---

## 2. Data (migration `0034_inventory_vouchers.sql`)

| New object | Why |
|---|---|
| `stock_vouchers` | numbered (`SIN-` / `SOU-` / `OP-`), branch-scoped, `kind` ∈ `stock_in`\|`stock_out`\|`opening`, `status` ∈ `draft`\|`posted`\|`voided`, carries `journal_entry_id`, `total_cost`, `reason`, optional `counter_account_id`; RLS `tenant_isolation` |
| `stock_voucher_lines` | `(voucher_id, line_no)` PK — item, qty, `unit_cost`, `line_cost`, `lot_id`, `serial_id`, note |
| `stock_transfers.sent_journal_entry_id` / `.received_journal_entry_id` / `.branch_id` | a transfer needs a branch to resolve a posting profile, and both legs must be traceable to their entries |
| `stock_adjustment_lines.variance_qty` / `.variance_value` | a count remembers what the difference was *worth*, not only its quantity |
| widened `stock_transfers_status_check` | 0006 allowed `('draft','sent','partially_received','received','cancelled')` while the service always wrote `in_transit` — every send had been failing on a check constraint |
| widened `stock_adjustments_status_check` | same frozen-vocabulary problem for `posted` |

Every statement is additive or *widening*: no table dropped, no column narrowed, no row
rewritten.

### Chart of accounts and posting profile

Two leaves were appended to the desktop chart (`packages/database/src/seed-demo.ts` —
115 accounts) and three keys to `POST_PROFILE_ACCOUNT_KEYS`:

| Key | Account (code) | Meaning |
|---|---|---|
| `openingBalanceAccountId` | `1270002` بضاعة أول المدة | the contra account of an opening voucher |
| `inventoryAdjustmentAccountId` | `3121004` تسويات المخزون (new, expense) | the contra account of an issue or a count variance |
| `stockInTransitAccountId` | `1270003` بضاعة تحت التحويل (new, asset) | where goods live while on the road |

`OrgProvisioningService` now **completes** an existing chart and profile instead of
leaving them alone: later phases add leaves and keys, and a tenant provisioned before
them would otherwise post every new document into `ACCOUNT_PROFILE_MISSING` forever.
Only *missing* codes/keys are added — an accountant's own edits are never overwritten.

---

## 3. The engine (`apps/api/src/modules/inventory/inventory.service.ts`)

Every method below runs inside one `withTenantTx`: movements and journal or nothing.

### 3.1 Stock vouchers

```
POST /inventory/vouchers                 → draft (SIN-000001 / SOU-… / OP-…)
POST /inventory/vouchers/:id/post        → movements + balanced journal
POST /inventory/vouchers/:id/void        → mirror movements + reversal entry
GET  /inventory/vouchers?kind=&status=   → register
GET  /inventory/vouchers/:id             → document with lines
```

| kind | movement | journal |
|---|---|---|
| `stock_in` | in @ the cost on the line | Dr المخزون / Cr الحساب المقابل |
| `stock_out` | out @ **average cost** | Dr الحساب المقابل / Cr المخزون |
| `opening` | in @ the cost on the line | Dr المخزون / Cr بضاعة أول المدة |

The contra account is, in order: the voucher's own `counter_account_id` (validated to
belong to the tenant) → `openingBalanceAccountId` for `opening` →
`inventoryAdjustmentAccountId`. It is never inferred from the free-text `reason`.

Gates: `INVENTORY_LINES_REQUIRED`, `INVENTORY_VOUCHER_KIND_INVALID`,
`INVENTORY_ITEM_NOT_FOUND` (404), `INVENTORY_ITEM_NOT_STOCKED` (a service item cannot
appear on a stock document), `INVENTORY_LOT_REQUIRED` / `INVENTORY_SERIAL_REQUIRED`
(when the item card says it is tracked), `INVENTORY_ACCOUNT_INVALID`,
`INVENTORY_PROFILE_KEY_MISSING`, `INVENTORY_VOUCHER_INVALID_STATUS` (409 on a second
post), `STOCK_INSUFFICIENT`, `INVENTORY_VOID_REASON_REQUIRED`.

### 3.2 Counts (`جرد وتسوية`)

```
POST /inventory/adjustments            → draft (ADJ-000001), multi-line
POST /inventory/adjustments/:id/post   → { approved: true } → variance + one entry
GET  /inventory/adjustments[?status=]  → register
GET  /inventory/adjustments/:id        → lines with expected / counted / variance
```

Each line is brought from its book quantity to the counted one; the line keeps
`variance_qty` (signed) and `variance_value`. The **net** variance is posted as a single
balanced entry — Dr المخزون / Cr تسويات المخزون for a net overage, the mirror for a net
shortage — because a count is one decision, not one decision per line. Posting without
`approved: true` is refused with `ADJUSTMENT_APPROVAL_REQUIRED`.

The old single-line `POST /inventory/adjustments/post` (browser-built journal) is kept
untouched for compatibility.

### 3.3 Transfers (`مناقلة`)

```
POST /inventory/transfers/draft          → draft (TR-000001), branch optional
POST /inventory/transfers/:id/send       → out movements + Dr in-transit / Cr المخزون
POST /inventory/transfers/:id/receive    → in movements  + Dr المخزون / Cr in-transit
POST /inventory/transfers/:id/cancel     → draft: stop; in-transit: goods come home
GET  /inventory/transfers/:id
```

* The branch comes from the call, else from the destination warehouse, else the source
  (`TRANSFER_BRANCH_REQUIRED` when none of them has one).
* On send the line's `unit_cost` is **overwritten with the cost the stock ledger
  actually used**, so the transfer is value-neutral: what leaves the source arrives at
  the destination with the same value, and بضاعة تحت التحويل always clears.
* Cancelling an in-transit transfer mirrors the goods back into the source warehouse,
  reverses the send entry and releases the serials — otherwise the stock simply
  disappeared.
* Serials ride the transfer: `available → reserved` on send, `available` **at the
  destination warehouse** on receipt.
* `INVALID_STOCK_TRANSFER`, `TRANSFER_QUANTITY_INVALID`, `TRANSFER_WAREHOUSE_INVALID`,
  `TRANSFER_LINE_NOT_FOUND`, `TRANSFER_RECEIPT_INVALID`, `TRANSFER_INVALID_STATE`.

### 3.4 Reorder point

`GET /inventory/below-minimum[?warehouse_id=]` joins `stock_balances` with
`items.min_qty` and returns the shortage — the desktop's `CalcItemsStockLimits`.

### 3.5 Negative stock

`recordInTx` gained `{ allowNegative }`. It is only honoured when the caller holds
`inventory.negative.override`, read from the request context — a service cannot decide
that for itself.

---

## 4. Screens (`apps/staff`)

| Screen | Notes |
|---|---|
| `/inventory/vouchers` (new) | three kinds as tabs (`?kind=opening` opens بضاعة أول المدة), draft→post→void, lot/serial columns, running total, detail panel with per-line value |
| `/inventory/adjustments` (rewritten) | multi-line count against the book quantity, live variance per line, one-click approve & post, detail panel with expected/counted/variance |
| `/inventory/transfers` (updated) | branch selector, REST send/receive/cancel, cancel-in-transit with a reason, value column |
| `/inventory/below-minimum` (new) | shortage per item/warehouse, “سند إدخال بالعجز” creates a covering draft |
| `/inventory/items` (updated) | the card now carries حد الطلب, الحد الأقصى, تتبع بدفعات, تتبع بأرقام تسلسلية |

Navigation (`apps/staff/lib/navigation.ts`): `stock-voucher`, `below-minimum` added;
`opening-stock` flipped from `api` to a real screen; no screen is left marked `api`
while its endpoint is missing.

---

## 5. Verification

* `apps/api/test/inventory-documents.spec.ts` — 14 tests: numbering per kind, the
  journal legs of each kind, double-post 409, service/lot-tracked refusals, void
  rollback, count approval + net variance, transfer send→receive legs,
  cancel-in-transit, below-minimum, negative override, tenant isolation.
* `node scripts/verify-inventory.mjs` — the same journey against a live stack
  (opening → issue → count → transfer → negative → reorder), asserting the **ledger**
  at every step, not only the stock level.
* Suite: API **76 files / 436 tests** green (14 new), `@erp/database` **17/17** green.
* `pnpm db:seed` re-run on an existing tenant: 2 chart leaves added, posting profile
  extended, everything else untouched.

---

## 6. Deliberately deferred

* Production orders / item assembly (`frmProductionOrder*`) already have their own
  service; they are not re-modelled here.
* Multi-barcode and multi-unit conversion on the item card.
* Expiry alerts (lots and `expiry_date` are captured and shown; a dated alert report is
  a reporting-phase item).
* Printing the stock documents (phase 10).
