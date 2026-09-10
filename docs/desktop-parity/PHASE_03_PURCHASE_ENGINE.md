# Phase 03 — Purchase engine (done 2026-09-10)

Desktop reference: `Class/InvoiceOper.cs` `BindToEntry` purchase mirror
(supplier Cr / purchases Dr / discount-received Cr / VAT-input Dr / cash legs),
`ItemOper.ItemAdditonalCos` (additional cost pro-rata by line value:
`cost × (itemTotal / invTotal) / qty`), `ItemOper.AvgCost` (moving average).

Rule of the phase: same engine pattern as Phase 02 — posting is one engine, not
caller-supplied lines. The draft stays permissive; `POST
/purchase-invoices/:id/post` enforces the gates and writes stock + landed costs
+ journal in a single transaction.

## 3A. Posting engine — `post()` mirrors SaveInvoice + BindToEntry

`apps/api/src/modules/purchases/purchases.service.ts`:

- **Gates:** `PURCHASE_WAREHOUSE_REQUIRED` (422, only when stocked `stock`-kind
  lines exist — service purchases post warehouse-free), `PURCHASE_TOTAL_INVALID`
  (422, negative purchase), `PURCHASE_FISCAL_PERIOD_REQUIRED` (422, legacy
  explicit-lines path only). Supplier is already enforced at creation.
- **Numbering:** `PI-/PR-` + 6 digits, allocated inside the posting transaction
  (moved before the journal so the auto description carries the number; the
  legacy explicit path keeps its byte-identical description).
- **Stock (perpetual, desktop moving average):** stocked lines receive at landed
  unit cost (`inWithCost`, freight etc. via the existing `allocateLandedCost`,
  whose by-value default matches `ItemAdditonalCos`); each line keeps its
  `allocatedCost`/`landedTotal`/`unitCostAtPost` stamping. Returns relieve at the
  current average (`outAtAvg`) — the only correct perpetual behaviour. Service
  lines never touch the ledger (previously they raised `STOCK_INSUFFICIENT`).
- **Journal (`buildAutoJournal`, gross method, from the resolved profile):**
  purchase = Dr inventory (landed stocked + stocked discount share) / Dr
  purchases (non-stock nets + share) / Dr VAT-input / Dr excise / Dr each
  expense-cost account (with its cost center) / Cr discount-received / Cr
  settlement. The header-discount stocked/non-stock split reuses the desktop
  gross weights, so the earned discount keeps its contra leg and the entry
  balances by construction. Expense costs without an account are refused with
  `PURCHASE_COST_ACCOUNT_REQUIRED` (named cost); all non-profile accounts are
  tenant-validated (`PURCHASE_POSTING_ACCOUNT_INVALID`). Withholding purchases
  are refused with `PURCHASE_WITHHOLDING_MANUAL_POSTING`, like sales.
- **Returns:** stocked goods go straight against inventory at the relieved
  (average) value — no contra leg; only services use `purchaseReturnAccountId`;
  VAT/excise/expense legs mirror; the price-vs-average drift posts to COGS
  (gain = Cr, loss = Dr, labelled `فرق متوسط التكلفة — مردود مشتريات`),
  skipped when zero.
- **Settlement:** `credit`/`cash`/`bank` + tenant-validated `settlementAccountId`.
  Cash/bank purchases debit no party subledger on the till leg, record the first
  payment in `payment_allocations`, and land `paid`. Returns never fabricate a
  payment row.
- **Legacy compatibility:** explicit `journalLines` keep the old behaviour
  untouched (POS still posts this way until Phase 04).
- **Void:** was flag-only (journal + stock left behind). Now reverses the
  journal (mirror linked by `reversalOf`, original marked `void`), mirrors every
  stock movement (`purchase_void`), then flips the status. Guards:
  `PURCHASE_VOID_REASON_REQUIRED`, `PURCHASE_VOID_HAS_PAYMENTS`.

## 3B. Specs — `test/purchase-posting.spec.ts` (8 tests, green)

Profile purchase keys resolve; post writes the 5-leg journal
(Dr inventory 1100 / Dr VAT 147 / Dr expense 50 / Cr discount-received 20 /
Cr payable 1277), receives 10 units @108 landed, and stamps the line;
warehouse gate fires for stocked but not services (services Dr purchases, no
inventory leg); negative totals refused; account-less expense costs refused;
return relieves 4 units at 108 average with Dr COGS 32 (unrefunded freight) and
a balanced entry; cash purchase lands `paid 115.0000` against the till; void
restores the stock level exactly and links the reversal, while a paid purchase
is refused with `PURCHASE_VOID_HAS_PAYMENTS`. Full API suite: **74 files /
414 tests green**; no existing suite touched purchase posting, so no
provisioning upgrades were needed.

## 3C. Staff screens

- `purchases/invoices/[id]`: the post action no longer builds journal lines in
  the browser — it sends `{ settlement, settlementAccountId,
  settlementCashLocationId }` with a till/bank picker for cash/bank. The cost
  form gained an allocation-target selector (inventory vs direct expense) with
  an expense-account picker (postable expense accounts only); posting is
  pre-guarded when a direct expense lacks an account.
- `purchases/invoices/new`: warehouse is no longer forced at draft creation
  (services need none) with a posting hint for stocked lines, branch-scoped
  warehouses, and the discount-reduces-VAT note. Staff builds green.

## 3D. Live verification (demo tenant, 2026-09-10)

10×100 − 20 @15% + 100 freight → `PI-000001`, total `1227.0000`, balanced
journal (Dr 1270001 1100 / Dr 2222001 147 / Cr 3200003 20 / Cr 22111001 1227);
stock 48 @40 + 10 @108 → 58 @51.7241 (moving average confirmed); return of 2
posted `PR-000001`, back to 56 @51.7241.

## Follow-ups (not this phase)

- `apps/staff/app/sales/pos/page.tsx` still posts via explicit lines
  (`salesJournalLines` + `inventoryLinesFor` + a second COGS journal) — Phase 04
  migrates POS onto the engine (including cash-shift settlement).
- `apps/staff/lib/posting.ts`: `purchaseJournalLines` is now unused;
  `salesJournalLines`/`inventoryLinesFor`/`cogsAmountFor`/`requireAccount`/
  `loadPostingProfile` remain only for POS — remove after Phase 04.
- Voiding a purchase that already has returns posted against it is allowed
  (same documented caveat as sales `SALES_VOID_HAS_RETURNS`).
- `GET /purchase-invoices/:id` returns lines + costs but no payments; the cash
  settlement row is visible via paidTotal/paymentStatus only.
