# Phase 02 — Sales invoice engine (done 2026-09-10)

Desktop reference: `Class/InvoiceCalc.cs` (`SumTotal`/`SumCost`/`SumNet`), `SaveInvoice`
+ `BindToEntry` (`Sub_Code` 5-leg journal: customer Dr / sales Cr / discount-Dr contra /
VAT Cr / profit Dr–Cr), returns (`ReBindToEntry`), void (`DeleteInvTrans` flag-only).

Rule of the phase: **posting is one engine, not caller-supplied lines.** The draft
stays permissive; `POST /sales/invoices/:id/post` enforces the desktop gates and
writes stock + costs + journal in a single transaction.

## 2A. Invoice math — discount before VAT (desktop `SumTotal`)

`packages/contracts/src/invoice-math.ts` (`calculateInvoiceTotals`, shared by the API
and the staff screens, so both always agree):

- Line net = `qty × price − line discount`; header discount is spread over the
  lines pro-rata (`discountShare`) and **reduces the VAT base** (`taxable`), then
  `tax = taxable × rate / 100`.
- `subtotal` = Σ taxable (net of header discount); `total = subtotal + taxTotal +
  extraTax − withholding`. `priceIncludesVat` back-solves the taxable base.
- Reference case (locked in `sales-posting.spec.ts`): 2×500 − 20 @15% →
  subtotal `980.0000`, tax `147.0000`, total `1127.0000`.

## 2B. Posting engine — `post()` does SaveInvoice + BindToEntry

`apps/api/src/modules/sales/sales.service.ts`:

- **Gates (drafts may stay incomplete):** `SALES_QUOTATION_NOT_POSTABLE` (409),
  `SALES_WAREHOUSE_REQUIRED` (422, only when stocked `stock`-kind lines exist —
  service-only and description-only invoices post warehouse-free, incl. progress
  bills), `SALES_TOTAL_INVALID` (422, negative sale), `SALES_CUSTOMER_REQUIRED`
  (422), `SALES_FISCAL_PERIOD_REQUIRED` (422, legacy explicit-lines path only).
- **Numbering:** `SI-/SR-/CN-/DN-` + 6 digits from the branch sequence, allocated
  inside the posting transaction.
- **Stock (`recordAutoStock`, desktop `SumCost`):** sale lines relieve at average
  cost (`outAtAvg`); each line's `cost_total` is stamped from the movement's
  average; returns restore at the **source invoice's original unit cost**
  (`returnAtOriginalCost`, falling back to the current average when unknown) so
  returns stay value-neutral. Service items never touch the ledger.
- **Journal (`buildAutoJournal`, from the resolved posting profile — never from
  caller lines):** sale = Dr settlement / Cr sales gross / Dr discount-given /
  Cr VAT-output / Cr excise / Dr COGS–Cr inventory (zero legs skipped); returns
  and credit notes mirror every leg. Withholding invoices are refused with
  `SALES_WITHHOLDING_MANUAL_POSTING` (explicit lines required).
- **Settlement:** `PostingInput.settlement` (`credit`/`cash`/`bank`) +
  `settlementAccountId` (tenant-validated, `SALES_SETTLEMENT_ACCOUNT_*`).
  Cash/bank posts debit the till/bank with **no party subledger**, records the
  invoice's first payment row (`invoice_payments`, idempotent
  `sales-settle:<id>`), and lands the invoice `paid`. Returns always refund
  through the settlement account but never fabricate a payment row.
- **Legacy compatibility:** callers passing explicit `journalLines` /
  `inventoryLines` keep the old behaviour untouched (progress bills, POS
  back-office imports); `void()` keeps its ZATCA seal + paid guards.
- **Void:** reverses the journal (mirror entry linked by `reversalOf`), mirrors
  every stock movement (`sales_void`), then flips the status — the desktop only
  flagged the invoice and left the stock relieved. Guards:
  `SALES_VOID_REASON_REQUIRED`, `SALES_VOID_ZATCA_SEALED`, `SALES_VOID_HAS_PAYMENTS`.

## 2C. Tafqeet consolidation (amount in Arabic words)

The repo had two converters: a weak `amountToArabicWords` port and the superior
`apps/api/.../reporting/tafqeet.ts` (Decimal-safe, dual/plural scale grammar,
11 currencies, `… لا غير`). The weak port was **deleted**; the canon moved
verbatim to `packages/contracts/src/arabic-words.ts`
(`amountInArabicWords`/`integerInArabicWords`, exported from the contracts
index) with its spec; the API module is now a one-line re-export. 71/71
contracts specs green. **Do not re-port desktop `Number2Arabic.cs`.**

## 2D. Specs repaired honestly (no weakening)

Posting now requires a real ledger (chart + profile + fiscal year), so five
suites that posted on bare fixture tenants were upgraded to provision properly
(`provisionOrgDefaults` + fiscal year in `beforeAll`, reusing the provisioned
`MAIN` branch): `einvoicing`, `portal`, `printing-and-cards`,
`production-and-returns`, `warehouse-documents`. The phase-10 unit mock gained
the header fields the new gates require. Result: **73 files / 405 tests green**,
including the new `test/sales-posting.spec.ts` (8 tests: profile seeding,
discount-VAT totals, post engine, warehouse gate, negative-total gate, return
mirror, cash settlement, void reversal + paid-guard).

## 2E. Staff screens

- `sales/invoices/new`: salesman is now a real lookup (was a raw id input),
  warehouses are branch-scoped, a posting hint appears when stocked lines lack a
  warehouse, and the discount field notes that it reduces the VAT base. Totals
  come from the shared `calculateInvoiceTotals` — identical to the API.
- `sales/invoices/[id]`: the post action no longer builds journal/inventory
  lines in the browser (and no longer posts a second COGS journal). It sends
  `{ settlement, settlementAccountId, settlementCashLocationId }`; the engine
  does the rest. Cash/bank settlement shows a till/bank picker.

## 2F. Live verification (demo tenant, 2026-09-10)

2×500 − 20 @15% → `980/147/1127`, `SI-000001`, balanced journal
(`Dr 12310001 1127 / Cr 4100001 1000 / Dr 4100003 20 / Cr 2222001 147 /
Dr 3200004 80 / Cr 1270001 80`), stock 50→48 @40 avg, line cost `80.0000`;
cash sale landed `paid 115.0000` with a payment row; return posted `SR-000001`;
service invoice posted warehouse-free and voided cleanly; voiding a paid
invoice refused with `SALES_VOID_HAS_PAYMENTS`.

## Follow-ups (not this phase)

- Voiding an invoice that already has returns posted against it is allowed and
  leaves a dangling `referenceInvoiceId` (financially consistent — the return
  keeps its own journal + stock — but the link dangles). Consider
  `SALES_VOID_HAS_RETURNS`.
- Purchases `POST …/post` still takes caller-built lines (`purchases/invoices/[id]`
  page) — Phase 03 migrates it onto the same engine pattern.
- `apps/staff/lib/posting.ts` client-side builders (`salesJournalLines`,
  `inventoryLinesFor`, `cogsAmountFor`) are now unused by sales but still used
  by purchases/payroll — remove after Phase 03.
