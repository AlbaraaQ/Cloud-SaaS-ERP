# Phase 04 — Point of sale, cashier shifts, day close

Date: 2026-09 · Status: ✅ done
Sources: `Desktop_ERP/SmartAuditERP/Form_WPF/frmPOS.xaml.cs`, `frmInvPOS.xaml.cs`,
`frmCloseShift.xaml.cs`, `frmCloseShiftInv.xaml.cs`, `ClosShiftAndroid.xaml.cs`,
`Class/CasherClosedDto.cs`, `Class/CasherClosedSubDto.cs`.

---

## 1. What the desktop did

| Desktop behaviour | Where | Cloud before Phase 04 | Cloud after Phase 04 |
|---|---|---|---|
| One Save writes the invoice, its stock movement, its entry and its payment | `frmPOS` / `frmInvPOS` | three browser round trips, not atomic | `POST /pos/checkout` — one transaction |
| The till takes cash, network (card), bank or postpones to the customer account | `Paytype` / `ProcType` | cash vs "card"→bank only | `cash` \| `card` \| `bank` \| `credit` |
| Change is returned to the customer | `frmPOSPay` | not modelled | `tendered` → `change` |
| Every till sale belongs to the open `CasherClosed` shift | `CasherClosed` | no link at all | `sales_invoices.shift_id` (migration 0033) |
| Closing counts the drawer and compares it to the day's takings (`CashTotal`, `SAfeNetVal`, `NetworkSum`, `ReturnSum`, `PostPoneSales`, `Diff`) | `frmCloseShift` + `ListCasherClosedSub` | cash **vouchers only** — POS sales invisible | vouchers + invoice payments, per method |
| "نأسف! لا يمكن حذف فاتورة بعد إغلاق اليومية" — no invoice changes after the day is closed | `frmPOS.DeleteInv` (L1828) | not enforced | `SALES_SHIFT_CLOSED` (409) |

## 2. The engine

### 2.1 `POST /pos/checkout` (new, `pos.operate` + `sales.invoice.post`)

```jsonc
{
  "branchId": "…", "warehouseId": "…",
  "priceIncludesVat": true,          // shelf prices are tax-inclusive, like the desktop
  "invoiceDiscount": "0",            // optional header discount
  "orderType": "pos",
  "shiftId": "…",                    // optional; defaults to the caller's open shift
  "partyId": "…",                    // required for `credit`
  "cashCustomerName": "عميل نقدي",   // walk-in default
  "lines": [{ "itemId": "…", "quantity": "2", "unitPrice": "100", "taxRate": "15", "discountRate": "0" }],
  "payment": { "method": "cash", "cashLocationId": "…", "tendered": "500" }
}
```

Response: `{ number, subtotal, taxTotal, total, paidTotal, paymentStatus, method,
cashLocationId, shiftId, change, tendered, invoiceId }`.

Order of operations (all inside one `withTenantTx`):

1. `ensureEnabled` — the `pack.pos` tenant flag.
2. Cart gates: `POS_CART_EMPTY`, `POS_LINE_ITEM_REQUIRED`, `POS_LINE_QUANTITY_INVALID`,
   `POS_PAYMENT_METHOD_INVALID`, `POS_CREDIT_CUSTOMER_REQUIRED` (a postponed sale needs
   a customer account, exactly like the desktop's `POSTPONED SALES Inv`).
3. Tender pre-check with the *same* `calculateInvoiceTotals` the API uses —
   `POS_TENDER_INVALID`, `POS_INSUFFICIENT_CASH` — so a short tender is refused
   **before** a row is written.
4. Drawer resolution: the named cash location must belong to the branch (RLS-scoped),
   otherwise the branch default of the tender's kind (`safe` for cash, `bank` for
   card/bank). Its `accountId` becomes the settlement account.
   → `POS_CASH_LOCATION_INVALID`, `POS_SETTLEMENT_ACCOUNT_REQUIRED`.
5. Shift resolution: an explicit `shiftId` must be open and at the same branch
   (`POS_SHIFT_INVALID` / `POS_SHIFT_CLOSED` / `POS_SHIFT_BRANCH_MISMATCH`); otherwise
   the caller's open shift at that branch is attached. With `pos.requireShift` set,
   cash cannot be taken outside a shift (`POS_SHIFT_REQUIRED`).
6. `SalesService.createAndPost` — numbering, gates, average-cost stock relief,
   profile-built journal (incl. COGS) and the settlement payment row.

### 2.2 `SalesService` — `createInTx` / `postInTx` / `createAndPost`

`create()` and `post()` were refactored into transaction-scoped halves so a till
checkout can run them in one transaction. Public behaviour is unchanged:

- `post()` now runs its gates *inside* the transaction (the period gate still fires
  first, before the transaction opens, so no inventory side effect can precede it).
- `get()` is now a single round trip (`getInTx`) instead of three.
- `settlement` accepts `card` alongside `cash`/`bank`: it settles into the bank
  account but keeps its own payment row, so the shift report can separate network
  takings from transfers.

### 2.3 Shift close (`POST /shift-closes/:id/close`)

`expectedCash` = cash vouchers (receipts − payments, as before)
**+** cash invoice payments on this shift's invoices (or, for invoices captured with
no shift, on this branch during the window) **−** cash payments on returns/credit notes.

The summary now carries the desktop's own columns:

```jsonc
{
  "vouchers": 0, "vouchersCash": "0.0000", "invoices": 2,
  "sales":   { "cash": "200.0000", "card": "100.0000", "bank": "0.0000", "credit": "0.0000" },
  "returns": { "cash": "0.0000", "card": "0.0000", "bank": "0.0000", "credit": "0.0000" },
  "expectedCash": "200.0000", "countedCash": "200.0000", "diff": "0.0000"
}
```

`GET /shift-closes/current` returns the same block as `live`, so a cashier sees what
the drawer should hold before counting it. `shift_close_lines` now records one line
per method (cash/card/bank/credit) plus the voucher subtotal, for the closing report.

### 2.4 Sealing a closed shift

`SalesService.void()` and `updateDraft()` refuse (`SALES_SHIFT_CLOSED`, 409) any
invoice whose `shift_id` points at a closed shift — the desktop's rule, now enforced
in the service instead of a dialog. Back-office invoices (`shift_id IS NULL`) are
unaffected, so nothing that worked before stops working.

### 2.5 Restaurant tables

`PosService.close()` now takes a settlement (`credit` by default — the previous
implicit behaviour) and settles the table's invoice in the same transaction.
`sendToInvoice()` resolves the branch's default warehouse, without which the
stock-moving sale could not be posted at all (a latent bug: no table could ever be
closed once the Phase 02 warehouse gate landed).

## 3. Database

`packages/database/migrations/0033_pos_shift_link.sql` — additive, idempotent, no
data loss:

```sql
ALTER TABLE sales_invoices
  ADD COLUMN IF NOT EXISTS shift_id uuid REFERENCES shift_closes (id) ON DELETE SET NULL;
CREATE INDEX IF NOT EXISTS sales_invoices_shift_idx ON sales_invoices (tenant_id, shift_id);
```

`ON DELETE SET NULL` keeps the sales history intact if a shift row is ever removed.

## 4. Screens (staff)

- **`/sales/pos`** — rewritten against the engine. Item tiles with search + category
  chips, cart with editable price/quantity/discount, tax-inclusive toggle, header
  discount, walk-in vs customer account, four tender methods with drawer picker,
  quick-cash buttons, live change, shift banner (open a shift inline), printable last
  receipt, and the last POS sales. One button → one call.
- **`/sales/shifts`** — the open shift now shows its live takings (cash / network /
  bank / postponed / vouchers / expected now), and the history table gained نقداً،
  شبكة، آجل columns from the frozen closing summary.

## 5. Tests

`apps/api/test/pos-checkout.spec.ts` (8 tests, all green):

1. cash checkout → posted + numbered + paid + stock relieved + balanced journal
   (cash leg + COGS) + payment row with the drawer
2. short tender refused, nothing written
3. empty cart rejected; another tenant's drawer rejected (`POS_CASH_LOCATION_INVALID`)
4. shift linkage + day close counts the till's cash (expected 100 / counted 120 /
   diff 20), live summary before closing, postponed sale excluded from cash
5. voiding a sale after its shift closed → `SALES_SHIFT_CLOSED`
6. card → bank account debited, payment row `card`
7. postponed sale needs a customer account; receivable debited, stays unpaid
8. table → send-to-invoice → close against cash, paid, table closed

Full suite: **75 files / 422 tests green** (was 74 / 414).

## 6. Live verification

`node scripts/verify-pos.mjs` against the seeded `demo` tenant (real HTTP):

```
✔ cash sale SI-000003: total 200.0000 · tendered 500.0000 · change 300.0000 · paid · shift …
✔ invoice SI-000003: status posted · lines 1 · payments 1 (cash 200.0000)
  line cost stamped: 120.0000
✔ stock movements for the sale: 1 (out 2.0000)
✔ journal Sales invoice SI-000003: 5 legs · Dr 320.0000 / Cr 320.0000 · balanced true
✔ card sale SI-000004: 100.0000 · paid · payment row card
✔ postponed sale SI-000005: 100.0000 · unpaid (no cash in the drawer)
✔ shift closed: expectedCash 200.0000 · countedCash 200.0000 · diff 0.0000
                 sales { cash: 200, card: 100 }
✔ sealed: 409 SALES_SHIFT_CLOSED
```

## 7. Follow-ups (not in this phase)

- **Hold / recall tickets** (`frmHoldM*`, 8 hold slots) and **shortcut items**
  (`frmShortCutInv*`) — the desktop's speed features for a busy till.
- **Multi-tender splitting** (`frmPOSPay*`: part cash, part card on one ticket) — the
  engine writes one payment row per settlement today.
- **Variance account** — today the counted-vs-expected difference is reported in the
  shift summary only; `EntryOper.BindCloseShiftToEntry` (L402) posts it to an account.
- **Cashier permissions** (`frmCasherSetting*`, `pos.priceoverride`) — price overrides
  at the till are not gated yet.
- `sales_invoices` has no `cashier_id`: the shift (`user_id`) is the closest link, as
  on the desktop.
