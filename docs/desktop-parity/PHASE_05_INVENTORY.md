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

## 8. Part four — إعادة تصميم شاشات المخزون (منفَّذ)

Presentation layer only: not one endpoint changed, not one permission widened.

### 8.1 Why — قياسات ما قبل التغيير

الخلفية كانت سليمة ومُختبَرة، لكن الشكل لم يتطور:

- كل شاشة ترث نفس القالب `components/screen.tsx` (مسار + عنوان + وصف + أزرار) ثم بطاقات وجداول `DataTable`؛ لا لوحة مؤشرات ولا خطوات ولا تخطيط مقسوم ولا تبويبات.
- أصناف معرّفة في `globals.css` ولا استخدام لها في `app/inventory/`: `.kpi` **0**، `.state` **0**، `.skeleton` **0**، `.section-title` **0**، `.grid.cols` **1**.
- **8 شاشات** تكتب `<table>` يدوياً بدل `DataTable`.

### 8.2 طبقة المكوّنات المشتركة — `apps/staff/components/ui.tsx`

`StatTiles` و`StatTile` (قيمة + كمية + تلميح + نغمة + شريط تقدّم) · `StatusTrack` (خطوات دورة المستند، مع حالة الإلغاء) · `Tabs` · `FilterBar` (فلاتر + أفعال) · `ActionBar` (يُخفى عند الطباعة) · `DocHead` و`DocField` (ترويسة المستند على شكل واجهات الديسكتوب) · `StateBox` (حالة فراغ/توضيح مفسِّرة) · `Totals` (شريط المجاميع).

### 8.3 نظام التصميم في `globals.css`

قسم جديد (≈320 سطراً) يضيف: `.tiles` و`.tile` بخمس نغمات، `.bar` لشرائط التقدّم والتغطية، `.stepper` و`.step`، `.tabs` و`.tab`، `.filters`، `.split` و`.split-list` و`.list-row` (تخطيط مقسوم: قائمة + تفاصيل)، `.doc-head` و`.doc-field`، `table.zebra` و`table.compact` و`tfoot` و`tr.row-active`، `.totals`، `.state-box`، و`@media print` يخفي الفلاتر والأفعال والقوائم الجانبية ويفتح الجداول للطباعة.

### 8.4 `DataTable` مُطوَّر

خصائص اختيارية جديدة: `zebra`، `compact`، `onRowClick` + `activeKey`، `footer` (صف مجاميع)، `expanded` (صف تفاصيل قابل للتوسّع). الشاشات الثلاث عشرة التي كانت تستدعيه استفادت منه دون تغيير سطر واحد.

### 8.5 `Directory` مُطوَّر

`tiles` (مؤشرات) و`footer` (مجاميع) — فاستفادت منه قوائم الدفعات والأرقام التسلسلية والمستودعات والوحدات والمجموعات دفعة واحدة.

### 8.6 الشاشات المُعاد بناؤها — كل شاشة ومرجعها من `Desktop_ERP`

| الشاشة | ما تغيّر | المرجع |
|---|---|---|
| `vouchers` | تبويبات الأنواع (إدخال/إخراج/بضاعة أول المدة) + تخطيط مقسوم (قائمة السندات ← بطاقة السند) + خطوات الحالة + ترويسة المستند + **الكمية بالوحدة الأساسية** + مجاميع + تأكيد قبل الترحيل/الإلغاء | `frmInvInOutput.xaml`، `RptInvInOutput.repx`، `InvoiceOper.cs` س 5031 |
| `transfers` | مؤشرات (في الطريق / مستلم جزئياً / الكمية والقيمة المعلّقة) + تبويبات الحالة + خطوات (مسودة→في الطريق→مُستلمة→مُغلقة) + ترويسة «من/إلى مستودع» + شريط تقدّم الاستلام لكل سطر + مجاميع | `frmInventoryTransfer.xaml`، `rptInventoryTransfer.repx` |
| `adjustments` | مؤشرات (مسودات/عجز/زيادة/صافي) + تبويبات + تخطيط مقسوم + **جدول فروق ملوّن** (مطابق/زيادة/عجز) + مجاميع + تأكيد الاعتماد قبل الترحيل | `frmItemInvertory`/`frmInvInOutput`، `Inventory.cs` |
| `item-card` | بطاقات (افتتاحي/وارد/صادر/ختامي) بالكمية **والقيمة** + ترويسة المستند + فلاتر + جدول موحّد بمجاميع + صف تفاصيل + **إجمالي الكميات وإجمالي المجموع** + طباعة | `rptItemDetails.repx`، `Inventorybalance()` |
| `in-transit` | بطاقات (معلّقة/قيمة/متأخرة/أقدم مدة) + **دلاء العمر** (٠–٢ / ٣–٧ / +٧) + أعمدة «من/إلى مستودع» + مجاميع + صف تفاصيل | `frmInventoryTransfer.xaml` |
| `items` | **تبويبات بطاقة الصنف الأربعة**: عام / وحدات / بضاعة أول المدة / المكونات + مؤشرات + تخطيط مقسوم + معاينة «1 علبة = 12 حبة» | `frmItems.xaml`، `ListItemunit.cs` |
| `item-units` | بطاقات الوحدات بمعاينة التحويل + مؤشرات (أساسية/تعبئة/باركودات/بلا باركود) + شريط فلاتر | `ItemOper.cs` س 744-760، `frmMultiBarcode.xaml` |
| `expiry` | بطاقات (منتهية/أسبوع/شهر/إجمالي) + **دلاء** (منتهية/أسبوع/شهر/أبعد) + مجاميع | `rptItemsExpire.repx`، `frmItemsExpire.xaml` |
| `below-minimum` | بطاقات (أصناف/عجز/حرجة/أكبر عجز) + **شريط تغطية** لكل صنف + مجاميع + مسار إلى سند التغطية | `frmItemsLimit.xaml` |
| `production` | بطاقات + خطوات + ترويسة (المنتج/الوحدة/البيان) + أعمدة المكوّنات (رمز الصنف، الصنف، الكمية، السعر، المجموع) + مجاميع | `frmProductionOrder.xaml`، `rptProductionOrder.repx` |
| `movements` | بطاقات (وارد/صادر/صافي/قيمة) + شريط فلاتر + اتجاه ملوّن + مجاميع | `Inventory.cs` |
| `levels` | بطاقات (قيمة المخزون/الكمية/متوسط التكلفة/أرصدة صفرية) + فلاتر + مجاميع | `InventoryCost(branch,date)` |
| `requests` | بطاقات لكل حالة + تبويبات الحالة بدل الشرائح | — |
| `deliveries` | بطاقات (فواتير معلقة/كمية متبقية/قيمة/مسلَّم) | — |
| `lots`, `serials`, `warehouses`, `units`, `categories` | مؤشرات مشتقة من الصفوف (منتهية/قاربت، متاح/محجوز/مُباع، مستودعات بلا فرع…) عبر `Directory` | — |

### 8.7 لوحة المخزون — `/inventory/overview` (جديدة)

بطاقات: قيمة المخزون، أصناف الأرصدة، تحت حد الطلب، دفعات قاربت الانتهاء، بضاعة في الطريق، مسودات تنتظر الترحيل — كل واحدة منها رابط إلى الشاشة التي تسوّيها، مع إجراءات سريعة وقائمتي «أكبر الأرصدة قيمةً» و«أكبر العجز». أُضيفت إلى `lib/navigation.ts` كمجموعة «نظرة عامة».

### 8.8 ما لم يُبنَ بعد (مرجعه جاهز)

- (الجزء الخامس بنى تبويب «المكونات» — انظر §11.)
- طباعة ملصق الباركود (`Barcode.repx`) مؤجَّلة للمرحلة 10.

### 8.9 التحقق

- `apps/api`: **78 ملفاً / 454 اختباراً** خضراء (لا تغيير في الخلفية).
- `apps/staff`: **36/36** — وأصلح الاختبار مفتاحاً مكرراً كان قائماً (`expiry` لشاشتين)، فصار مفتاح الدفعات `lots`.
- `pnpm --filter @erp/staff run build`: **100/100** صفحة ثابتة (99 + لوحة المخزون).
- `scripts/verify-inventory.mjs`: 11 قسماً كلها ✓.
- كل مسارات `/inventory/*` (21 مساراً) ترجع 200، وأصناف التصميم الجديدة موجودة في CSS المبني.

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
  twelve sections: opening → issue → count → transfer → negative → reorder → وحدات
  القياس → الباركود → تواريخ الصلاحية → **بضاعة في الطريق → بطاقة الصنف → مكوّنات الصنف**,
  asserting the ledger at every step.
* Suite: API **79 files / 462 tests** green (was 78/454 — the eight BOM tests of §11.8),
  `@erp/database` **17/17** green, staff build **100/100** static pages.

---

## 10. Part five — مكوّنات الصنف (BOM)

### 10.1 What the desktop did, file by file

| Behaviour | `Desktop_ERP` source | What it did | What we built |
|---|---|---|---|
| تبويب المكونات في بطاقة الصنف | `Form_WPF/frmItems.xaml` — `TabItem x:Name="TabPage1" Header="  المكونات  "` (L1149), group header `🔧 مكونات الصنف` (L1167), `pnlComponent` / `dgvComponent` (L1220-1346) | a grid of the item's components, edited in place | the fourth tab of `/inventory/items`, same header and same grid |
| أعمدة الشبكة | `frmItems.xaml` L1225-1346 | `#`, `رمز الصنف`, `رقم الصنف`, `الصنف`, `الوحدة`, `الكمية`, `السعر`, `المجموع`, `المستودع`, `مادة مضافة` (`Binding IsAdded`), `حذف` | `رمز الصنف`, `الصنف`, `الكمية`, `الوحدة`, `المستودع`, `مادة مضافة`, `حذف` (see §10.7 for the three columns we deliberately left out) |
| إعدادات الإنتاج على البطاقة | `frmItems.xaml` L1180-1225 — `Panel3` | `مستودع المنتج التام`, `تكلفة المادة`, `كمية المنتج` | `مستودع الإنتاج` and `الكمية المنتجة` live on the order (`frmProductionOrder.xaml`), which is where the desktop asks for them too; `تكلفة المادة` is *computed*, never typed |
| قراءة التركيبة | `frmItems.xaml.cs` `LoadItemComponents()` L697-733 — `SELECT … FROM ItemComponents LEFT JOIN items ON ItemComponents.ComponentId = items.id` | loaded the recipe for the open item | `GET /organization/catalog/items/:id/components`, joined to items, units and warehouses so the screen prints names instead of uuids |
| حفظ التركيبة | `frmItems.xaml.cs` L1085-1118 | `delete from ItemComponents where itemId=…` then re-insert every row; refuses an empty recipe with **«ادخل مكونات المادة»** (L1092) | `POST …/components` is an upsert on `(item_id, component_item_id)` — no delete-and-rewrite, so concurrent edits to other rows of the same recipe survive |
| أمر الإنتاج يملأ مكوّناته من البطاقة | `frmProductionOrder.xaml.cs` `LoadComponent()` L299-380 | for the chosen product, read `ItemComponents` and fill the grid (`ItemCode`, `Item_name`, `unit_id`, `store`, `quantity`, `type`) | `POST /inventory/production-orders` with **no** `components` reads `catalog.componentsFor()` inside the same transaction |
| معامل الوحدة | `frmProductionOrder.xaml.cs` L345-355 — `SELECT perc FROM ItemUnits WHERE ItemId=… AND unit=…` | `UnitEquality` per row | `componentUnitRatio()` — the same `item_units.perc`, defaulting to 1 for the base unit |
| ضرب الكمية في الكمية المنتجة | `frmProductionOrder.xaml.cs` L567-570 — `row.Qty = Math.Round(qty * row.BaseQty * row.UnitEquality, 2)` | the consumed quantity is the recipe **per unit** × the produced quantity × the unit ratio | `componentsFor(tx, tenantId, itemId, outputQty)` returns `qty × outputQty`, and `InventoryService.recordInTx` applies the unit's factor — identical arithmetic, done where the ledger is written |
| شاشة أمر الإنتاج | `frmProductionOrder.xaml` L246-546 | `🏭 أمر الإنتاج`, `📦 المنتج`, `📐 الوحدة`, `🏭 مستودع الإنتاج`, `🔢 الكمية المنتجة`, `📊 الكمية المتوفرة`, `📝 البيان`, `🧾 مكونات الإنتاج`, grid columns `#`, `رمز الصنف`, `الصنف`, `الوحدة`, `الكمية الأساسية`, `الكمية`, `السعر`, `المجموع`, `المستودع`, `المتوفرة`, `حذف` | `/inventory/production`: the same field labels, and a live preview of `الكمية الأساسية` → `الكمية` for every component of the chosen product |

### 10.2 Migration `0037_item_components.sql`

Additive, like every migration before it — nothing is dropped, nothing is rewritten:

1. `item_components.warehouse_id → warehouses(id)`, nullable, `ON DELETE SET NULL`, plus the
   partial index `item_components_warehouse_idx (tenant_id, warehouse_id) WHERE warehouse_id IS
   NOT NULL` — the desktop's `store` column, which decides where a component is drawn from;
2. `production_order_components.unit_id → units_of_measure(id)`, nullable — the unit a line was
   planned in, so an order can be re-read and costed without guessing;
3. the down migration (`migrations/down/0037_item_components.down.sql`) drops the index, then the
   two columns.

`item_components` itself was unusable before 0035 repaired its RLS and tenant column (§6.1);
0037 is the first migration that gives it a *consumer*.

### 10.3 The rule the service enforces

`CatalogService.componentsFor(tx, tenantId, itemId, outputQty)` is the single reader of the recipe:

* only `kind = 'component'` lines are consumed — `additive` is the desktop's `IsAdded` bit, a
  by-product that comes *out* of the build, not an ingredient, and is recorded but never drawn
  from stock;
* every quantity is scaled by the produced quantity and returns the component's own `unitId`, so
  `0.5 × 2` cartons is stored as `1 CTN` and consumed as 12 pieces;
* a component's unit must be its base unit or a unit defined on its own card
  (`CATALOG_COMPONENT_UNIT_INVALID`) — otherwise the ratio has no source;
* an item cannot be its own component (`CATALOG_COMPONENT_SELF`), and a recipe may not form a
  loop: `assertNoCycle` walks the graph depth-first to a bound of 10 and answers
  `CATALOG_COMPONENT_CYCLE` (409), because a cycle in a BOM is not a bad row, it is a bad
  *structure* that would hang an explosion;
* the quantity must be positive — the zod schema rejects it at the boundary
  (`VALIDATION_FAILED`) and the service repeats the check for direct callers such as the
  defaulting path (`CATALOG_COMPONENT_QTY_INVALID`).

### 10.4 Endpoints

| Method | Path | Permission | Notes |
|---|---|---|---|
| `GET` | `/organization/catalog/items/:id/components` | `catalog.item.view` | the recipe, joined to item/unit/warehouse names, ordered by the component's SKU |
| `POST` | `/organization/catalog/items/:id/components` | `catalog.item.manage` | upsert on `(item_id, component_item_id)` |
| `DELETE` | `/organization/catalog/items/:id/components/:componentItemId` | `catalog.item.manage` | one row |

Tenant isolation is the existing `tenant_isolation` policy on `item_components` (0035) — no new
policy was needed, and none of the three endpoints filters by tenant in application code.

### 10.5 Production orders

`components` in `POST /inventory/production-orders` is now **optional**:

* **typed** → validated exactly as before (quantity > 0, not the output item, no duplicates), and
  each line keeps its `unitId`;
* **omitted** → filled from the item card inside the same `withTenantTx`, scaled by `outputQty`;
* **empty in both places** → `422 PRODUCTION_COMPONENTS_REQUIRED` — the desktop's
  «ادخل مكونات المادة», raised by the API instead of a message box.

`complete()` passes each line's `unitId` to `recordInTx`, so the movement carries both what the
planner typed and what the ledger stored (`base_qty = qty × factor`). `InventoryModule` now
imports `CatalogModule`; that is not a cycle — the catalog module imports only the database.

### 10.6 Screens

* `/inventory/items` — تبويب **المكونات**: the recipe as a table with a totals row, an add row
  beneath it (الصنف · الكمية · الوحدة · المستودع · مادة مضافة), unit options coming from the
  selected component's own card, and **حذف** per row. The old `StateBox` that explained the tab
  was unimplemented is gone.
* `/inventory/production` — **مكونات الإنتاج** shows the card's recipe the moment a product is
  chosen: `رمز الصنف · الصنف · الكمية الأساسية · الوحدة · الكمية · المستودع`, where `الكمية` is
  `الكمية الأساسية × الكمية المنتجة`. «تعبئة تلقائية من بطاقة الصنف» is on by default — unchecking
  it brings back the manual grid, which is the desktop's own escape hatch for a one-off build
  that does not match the card. An item with no recipe says so where the components would be,
  instead of failing on save.

### 10.7 Label match against `Desktop_ERP`

Every visible label is taken from the files in §10.1:

| Desktop | Ours | |
|---|---|---|
| `المكونات` | `المكونات` | verbatim |
| `🔧 مكونات الصنف` | `🔧 مكونات الصنف` | verbatim |
| `رمز الصنف` · `الصنف` · `الوحدة` · `الكمية` · `المستودع` · `مادة مضافة` · `حذف` | the same | verbatim |
| `مستودع الإنتاج` · `الكمية المنتجة` · `المنتج` · `البيان` · `مكونات الإنتاج` | the same | verbatim |
| `الكمية الأساسية` · `الكمية` (order grid) | the same | verbatim |
| `رقم الصنف` | — | not shown: the desktop's internal `items.id`; `رمز الصنف` is the key a user types, and printing a uuid in an RTL grid buys nothing |
| `السعر` · `المجموع` | — | not stored on the recipe: the desktop keeps a *price snapshot* that goes stale, while the order is costed at completion from the warehouse's moving average — which is what `ItemOper.Cost` returns there anyway |
| `تكلفة المادة` | computed, not typed | the order shows the cost after completion; typing it would let a user disagree with the ledger |

Three invented labels, all of them switches the desktop does not need because it has only one
behaviour: **«تعبئة تلقائية من بطاقة الصنف»** (the desktop always auto-loads; the web form also
allows typed components, so it needs the choice), **«مكوّنات البطاقة: n»** (a chip counting the
recipe rows), and the empty-state sentence «بطاقة هذا الصنف لا تحمل مكوّنات بعد».

### 10.8 Verification

* `apps/api/test/inventory-bom.spec.ts` — 8 tests: add/read/update/delete a component, a
  non-positive quantity and a foreign unit are refused, a cycle is refused, a reader with only
  `catalog.item.view` cannot write, a production order fills itself from the card, an item with
  neither a recipe nor typed components is refused `PRODUCTION_COMPONENTS_REQUIRED`, completing
  the order moves both halves of the stock, and a component planned in cartons is consumed in
  pieces (0.5 × 2 cartons = 12 pieces).
* Suite: API **79 files / 462 tests** green, `apps/staff` **36/36** green, staff build
  **100/100** static pages.
* `node scripts/verify-inventory.mjs` — now **twelve** sections; the new one walks the whole
  journey against the live stack: store a recipe → read it back → refuse a self-reference and a
  cycle → create an order with no components → watch it fill with `3 × 4 = 12` → complete it and
  assert the part fell from 40 to 28, the assembly came in at 4, and its unit cost is exactly
  `60 / 4 = 15` → refuse an order for an item with no recipe.
* `/inventory/items` and `/inventory/production` both render (200) through the tunneled host.

---

## 11. Deliberately deferred

* Production orders / item assembly (`frmProductionOrder*`) already have their own service;
  §10 wired the item card's recipe into it, and the order itself is deliberately still
  one-level — a component that is itself an assembly is exploded per order, not recursively.
* Unit-aware *pricing lists* — a unit carries its own sale/purchase price, but price lists
  (phase 08) do not yet choose a unit.
* Printing barcode labels and the stock documents themselves (phase 10).
