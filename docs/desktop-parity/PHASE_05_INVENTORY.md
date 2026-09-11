# Phase 05 — المخزون (stock documents, transfers, counts, multi-unit, barcodes, expiry, in-transit, item card)

Date: 2026-09 · Status: ✅ done
Sources: `Desktop_ERP/SmartAuditERP/Form_WPF/frmInvInOutput.xaml.cs` (3,120 lines),
`frmInventoryTransfer*`, `frmReGenerateEntries.xaml.cs`, `Class/Inventory.cs`
(`UpdateItemStock`), `Class/ItemOper.cs`, `Class/ListItemunit.cs`,
`Class/InvoiceOper.cs` (L5031 `ItemPrimaryQnty = ItemQuantity * UnitEquality`).

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
| وحدات القياس المتعددة — `ItemUnits.perc` per item/unit, with its own barcode and prices | `ListItemunit.cs`, `ItemOper.cs` L744/L2523/L2713 → `ProductUnit.UnitEquality` | the table existed but was **unusable**: `FORCE ROW LEVEL SECURITY` with no policy and no `tenant_id` | `item_units` repaired by migration 0035; every document line can be written in any unit the card defines |
| `ItemPrimaryQnty = ItemQuantity * UnitEquality` — the ledger always stores base units | `InvoiceOper.cs` L5031, stock update L4275-4295 | every quantity was assumed to be base units | `base_qty = qty × factor`; the entered `unit_id` and its `factor` are snapshotted on `inventory_transactions` |
| باركود متعدد (`ItemBarcodes`) + a barcode per unit | `ItemOper.cs` barcode lookup | `item_barcodes` existed with no endpoint; nothing resolved a scan | `GET /inventory/barcode/:code` answers item + unit + factor, searching `items.barcode`, `item_barcodes` and `item_units.barcode` |
| تنبيه انتهاء الصلاحية | `frmItems*` expiry column | lots carried `expiry_date` but nothing ever read it | `GET /inventory/expiry?days=…` + the `/inventory/expiry` screen |
| مناقلة مرسلة ولم تُستلم كاملة | `frmInventoryTransfer` | no ending at all: the remainder stayed in بضاعة تحت التحويل for ever | `GET /inventory/in-transit` + `POST /inventory/transfers/:id/close` with `return` or `shortage` |
| رصيد الصنف حتى تاريخ (`TotalItemStock(branch, date)`, `Inventorybalance()`) | `Class/Inventory.cs` | a flat movement list with no period and no running balance | `GET /inventory/item-card` with opening, running balance, totals and closing |

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

## 5. Verification (part one)

* `apps/api/test/inventory-documents.spec.ts` — 14 tests: numbering per kind, the
  journal legs of each kind, double-post 409, service/lot-tracked refusals, void
  rollback, count approval + net variance, transfer send→receive legs,
  cancel-in-transit, below-minimum, negative override, tenant isolation.
* `pnpm db:seed` re-run on an existing tenant: 2 chart leaves added, posting profile
  extended, everything else untouched.
* (Part two and the full suite numbers are in §7.)

---

## 6. Part two — وحدات القياس، الباركود، تواريخ الصلاحية

### 6.1 Migration `0035_item_units_barcodes.sql`

`item_units` and `item_components` were created in 0003 and could never be used: they
were created with `ENABLE` **and `FORCE ROW LEVEL SECURITY`**, with no policy and no
`tenant_id` column — under `FORCE`, a table with no policy denies every command, and
without a tenant column no policy could have been written. 0035 repairs them in place,
without destroying anything:

1. `tenant_id` is added, orphan rows (an item that no longer exists) are deleted, the
   column is back-filled from `items`, and only then made `NOT NULL`;
2. a real `tenant_isolation` policy is created on both tables, plus the tenant-scoped
   unique indexes `(tenant_id, item_id, unit_id)` and
   `(tenant_id, item_id, component_item_id)` that RLS scans actually use;
3. `inventory_transactions` gains `unit_id` and `factor numeric(20,6) NOT NULL DEFAULT 1`,
   commented on the column: *the balance moves by `base_qty = qty × factor`, never by
   `qty`*;
4. the three stock-document line tables (`stock_voucher_lines`,
   `stock_adjustment_lines`, `stock_transfer_lines`) gain the same `unit_id`, so a
   document records what the clerk counted as well as what the ledger stored.

Nothing is dropped, and every statement is idempotent.

### 6.2 The conversion rule

| Rule | Where |
|---|---|
| `baseQty = qty × factor`, and the balance/value move by `baseQty` only | `InventoryService.recordInTx` |
| `factor` is read from `item_units.ratio` and **snapshotted** onto the movement, so a ratio edited later cannot rewrite history | `resolveUnit()` + `inventory_transactions.factor` |
| `unitCost` is converted the other way: the entered cost is per *entered* unit, the average is per *base* unit (`entered ÷ factor`) | `recordInTx` |
| A unit the item card does not define is refused at posting time | `422 INVENTORY_UNIT_NOT_ALLOWED` |
| The base unit always converts 1:1 — it is the unit every ratio is expressed against | `422 CATALOG_UNIT_RATIO_INVALID` |

Two boxes of twelve at 120 each therefore land as **24 base units worth 240**, and the
moving average becomes 10 per piece, not 120.

Counts are compared in the unit they were taken in: the book quantity (base units) is
divided by the factor before the variance is computed, so the variance on the screen is
the variance that is posted.

### 6.3 Endpoints

| Endpoint | Permission | Notes |
|---|---|---|
| `GET/POST/DELETE /organization/catalog/items/:id/units` | `catalog.item.view` / `.manage` | base unit first, then every packing unit; ratio, unit barcode, unit prices, default-for-sale |
| `GET/POST /organization/catalog/items/:id/barcodes`, `DELETE …/:barcode` | `catalog.item.view` / `.manage` | extra labels; a label owned by another item is `409 CATALOG_BARCODE_TAKEN` |
| `GET /inventory/barcode/:code` | `inventory.view` | resolves `items.barcode` → `item_barcodes` → `item_units.barcode`; answers item, unit, factor and which table matched |
| `GET /inventory/expiry?days=30&warehouse_id=…` | `inventory.view` | lots at or inside the horizon, `daysLeft` and `expired` per row, soonest first |

### 6.4 Screens

| Screen | Notes |
|---|---|
| `/inventory/item-units` (new) | pick an item → its units with the base-unit equivalent, add/remove a unit (ratio, unit barcode, unit prices, default for sale), and manage extra barcodes per unit |
| `/inventory/expiry` (new) | horizon selector (أسبوع / شهر / ثلاثة أشهر / سنة), منتهية vs قاربت counters, days left per lot |
| `/inventory/vouchers` (updated) | a unit column per line with the base-unit equivalent beside the quantity, and a barcode box: one scan appends the item with its unit and factor |
| `/inventory/adjustments` (updated) | the count can be taken in any unit; the book column and the variance follow the chosen unit |
| `/inventory/transfers` (updated) | the same unit column when the transfer line is written in cartons |

---

## 7. Part three — بضاعة في الطريق، إقفال المناقلة، وبطاقة الصنف

### 7.1 بضاعة في الطريق

A مناقلة that is sent but never fully received had no ending at all — on the desktop
either. The source warehouse loses the goods at send time, the destination only books
what arrives, and the difference sits in بضاعة تحت التحويل with nothing to clear it.

`GET /inventory/in-transit` lists exactly that: every transfer line still outstanding,
with the item, the quantity, its value, and `daysInTransit` — because an old one is the
one nobody has looked at.

`POST /inventory/transfers/:id/close` settles it, and there are only two honest endings:

| Mode | Stock | Journal |
|---|---|---|
| `return` | the remainder moves back into the **source** warehouse at the cost it left at, so value is conserved | Dr المخزون / Cr بضاعة تحت التحويل |
| `shortage` | nothing moves — the source already lost the goods when it sent them | Dr عجز (the count-variance account) / Cr بضاعة تحت التحويل |

Either way the transit account ends at zero for that transfer. The settled quantity is
recorded in a new `closed_qty` column, never in `received_qty`: a document must not be
made to look fully received when the goods went home or went missing. Closing twice is
`409 TRANSFER_ALREADY_CLOSED`, and a transfer that is not on the road cannot be closed.

### 7.2 بطاقة الصنف

`GET /inventory/item-card?item_id=&warehouse_id=&from=&to=` — the desktop's
`Inventorybalance()` and `TotalItemStock(branch, date)`, read the way a storekeeper
reads them:

* an opening balance at `from` (zero when no period is asked for — otherwise everything
  would be counted twice);
* every movement inside the period with a **running** quantity and value beside it;
* in/out totals, and a closing balance that is asserted against `stock_balances`.

The value column matters as much as the quantity: a card that only counts units cannot
answer what the stock is worth. `GET /inventory/movements` gained the same `from`/`to`
filters, the joined names (SKU, item, warehouse, unit) and a sane limit, so a screen can
print a name instead of a uuid.

### 7.3 Migration `0036_transfer_closure.sql`

Additive: the transfer status check is widened with `'closed'`, `stock_transfers` gains
`closed_at` / `closed_journal_entry_id` / `closure_mode` / `closure_reason`, and
`stock_transfer_lines` gains `closed_qty`. No row is rewritten.

### 7.4 Screens

| Screen | Notes |
|---|---|
| `/inventory/in-transit` (new) | what is still on the road, its value, how many days it has been there, and the two closures — إعادة للمصدر / إقفال كعجز — with a reason |
| `/inventory/item-card` (new) | item + warehouse + period, four tiles (افتتاحي · وارد · صادر · ختامي) each with quantity **and** value, then the ledger with a running balance and the unit the line was counted in |
| `/inventory/movements` (updated) | the same period filters, and document types named in Arabic |

---

## 8. Part four — إعادة تصميم شاشات المخزون (مخطَّط: لم يُنفَّذ بعد)

> حالة هذا القسم: **خطة عمل لجلسة قادمة**. نُفِّذت الأجزاء 1–3 (الكومِتات `0b4578c` و`a6d99e1` و`55d407c`)؛ الجزء الرابع تغيير في **طبقة العرض فقط** ولا يمس أي نقطة نهاية.

### 8.1 Why — measured, not impression

الخلفية سليمة ومُختبَرة، لكن التصميم لم يتطور:

- كل شاشة ترث نفس القالب `components/screen.tsx` (مسار + عنوان + وصف + أزرار) ثم بطاقات وجداول `DataTable`؛ لا لوحة مؤشرات ولا خطوات ولا تخطيط مقسوم ولا تبويبات.
- أصناف معرّفة في `globals.css` ولا تُستخدم في `app/inventory/`: `.kpi` **0**، `.state` **0**، `.skeleton` **0**، `.section-title` **0**، `.grid.cols` **1**.
- **8 شاشات** ما زالت تكتب `<table>` يدوياً بدل `DataTable`.
- النتيجة: شاشات صحيحة وظيفياً بشكل «نموذج موحّد بسيط» لا يختلف عن بقية النظام.

### 8.2 The shared component layer to build in `apps/staff/components/`

`PageHeader` · `FilterBar` (لاصق) · `StatTiles` (القيمة + الكمية) · `DataTable` (رأس لاصق، صفوف مخططة، ترتيب، توسّع، تذييل مجاميع) · `StatusTrack` (خطوات حالة المستند) · `EmptyState` / `LoadingState` / `ErrorState` · `ActionBar` (تأكيد قبل الإجراء المدمّر) · `Tabs`.

### 8.3 `Desktop_ERP` references — التصميم يُستخرج منها، لا يُخترع

| الشاشة السحابية | الواجهة `Form_WPF/` | المستخلص |
|---|---|---|
| بطاقة الصنف `items` | `frmItems.xaml` | التبويبات الأربعة: **عام / وحدات / بضاعة أول المدة / المكونات** |
| `vouchers` | `frmInvInOutput.xaml` | الرقم، التاريخ، رقم المرجع، تاريخ المرجع، البيان، المستودع، اسم المورد، الرصيد + أفعال الأسطر (الباركود، بحث مادة، إضافة مادة، الوحدات، حذف السجل) |
| `transfers` | `frmInventoryTransfer.xaml` | الرقم، التاريخ، الوقت، المرجع، **من مستودع / إلى مستودع**، البيان |
| `production` | `frmProductionOrder.xaml` | المنتج، الوحدة، البيان + أعمدة المكوّنات (رمز الصنف، الصنف، الوحدة، الكمية الأساسية، الكمية، السعر، المجموع) |
| `expiry` | `frmItemsExpire.xaml` | صلاحية الصنف، رمز الصنف، الصنف، الكمية |
| `item-units` / الباركود | `frmMultiBarcode.xaml` | رقم الصنف، الصنف، الباركود، حذف |
| `below-minimum` | `frmItemsLimit.xaml` | الرقم، المجموعة، الصنف، سعر الشراء، سعر البيع، الرصيد، **حد الطلب** |
| مكوّنات الصنف | تبويب «المكونات» + `Class/ItemComponent.cs` | المكوّن، المستودع، الكمية، الوحدة، السعر، المجموع، مضاف/مطروح |

التقارير `Reports/` تحدّد محتوى العرض والطباعة: `rptItemDetails.repx` (بطاقة الصنف + إجمالي الكميات/المجموع)، `RptInvInOutput.repx` و`rptInventoryTransfer.repx` (أعمدة السند والمناقلة)، `rptItemsExpire.repx` (الصلاحية + سعر التكلفة)، `rptItemsDirectory.repx` (دليل الأصناف)، `rptProductionOrder.repx`، `Barcode.repx`، `rptInventoryReport.repx`.

الفئات `Class/` تحدّد السلوك: `Inventory.cs` (`UpdateItemStock`, `ItemsExpirationStock`, `TotalItemStock`, `Inventorybalance`, `InventoryCost`, `InventoryCostByType`)، `ItemOper.cs` س 744-760 (`purch, sale, barcode, perc` → `UnitEquality`)، `InvoiceOper.cs` س 5031 (`ItemPrimaryQnty = ItemQuantity * UnitEquality`)، `ListItemunit.cs`، `ItemComponent.cs`.

**قاعدة:** كل تسمية عمود أو تبويب أو زر تُؤخذ من هذه الملفات بنصها العربي؛ أي تسمية مخترعة تُذكر مع سببها.

### 8.4 Screens, in order

`vouchers` (تخطيط مقسوم + تبويبات + مسح باركود + عمود الوحدة الأساسية) ← `transfers` (`StatusTrack` + تقدّم الاستلام) ← `adjustments` (نموذج جرد بألوان + اعتماد) ← `item-card` (مؤشرات كمية وقيمة + إجماليات) ← `in-transit` (مؤشرات + دلاء عمر + إقفال) ← `items` (تبويباتها الأربعة) ← `item-units` ← `expiry` (دلاء) ← `below-minimum` ← `production` ← باقي الشاشات ← **جديد `/inventory/overview`** (لوحة المخزون).

### 8.5 Definition of done

لا `<table>` يدوية · ≥ 12 شاشة على `PageHeader` + `FilterBar` · `LoadingState` و`EmptyState` في كل قائمة · عنصر تصميمي جديد واحد على الأقل في كل شاشة · بطاقة الصنف بتبويباتها الأربعة · المؤشرات تعرض الكمية والقيمة معاً · **مطابقة تسميات `Desktop_ERP` ≥ 90٪** · خطوط الأساس في §9 تبقى خضراء.

### 8.6 Handoff

النص الجاهز للجلسة القادمة: `docs/desktop-parity/NEXT_SESSION_PROMPT_PHASE_05_UI.md`.

## 9. Verification

* `apps/api/test/inventory-documents.spec.ts` — 14 tests (part one).
* `apps/api/test/inventory-units-barcode.spec.ts` — 11 tests (part two): the base unit is
  always answerable, a box of 12 is refused a ratio of zero and the base unit cannot be
  re-scaled, 2 boxes move 24 pieces and are worth 240, an undefined unit is refused at
  posting, a scan answers item + unit + factor, one label cannot belong to two items,
  lots inside the horizon are reported and those beyond it are not, and a barcode from
  another tenant is invisible.
* `apps/api/test/inventory-closure.spec.ts` — 7 tests (part three): a partly received
  transfer still lists its outstanding 18, closing as a return brings them home and does
  **not** count as a receipt, a shortage writes the transit asset off without moving
  stock, closing twice is refused, a draft cannot be closed, the item card's
  opening + in − out agrees with the balance table, and the movement list honours a
  period.
* `node scripts/verify-inventory.mjs` — the same journey against a live stack, now
  eleven sections: opening → issue → count → transfer → negative → reorder → وحدات
  القياس → الباركود → تواريخ الصلاحية → **بضاعة في الطريق → بطاقة الصنف**, asserting the
  ledger at every step.
* Suite: API **78 files / 454 tests** green, `@erp/database` **17/17** green, staff build
  **99/99** static pages.

---

## 10. Deliberately deferred

* Production orders / item assembly (`frmProductionOrder*`) already have their own
  service; they are not re-modelled here.
* Unit-aware *pricing lists* — a unit carries its own sale/purchase price, but price lists
  (phase 08) do not yet choose a unit.
* Printing barcode labels and the stock documents themselves (phase 10).
