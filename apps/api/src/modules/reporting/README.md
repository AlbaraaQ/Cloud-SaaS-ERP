# Reporting module

Phase 14 adds a registry-driven report read side. Report keys are centrally registered
in `REPORT_KEYS`; adding a report requires a key, parameter contract, query builder,
row shape, export support, and golden tests.

Reports read immutable journals, inventory ledgers, invoices, vouchers, and shift-close
facts. They do not mutate business state or cache totals on master records.

Async exports return a `reports-export` queue token; later worker/rendering phases can
attach generated CSV/XLSX/PDF artifacts to the files table.

## Saved layouts — مصمم التقارير

`report_layouts` (migration `0028`) is the whole persistence of the report designer, and it
stores **presentation only**: which of a report's own columns are visible, in what order,
under what heading, plus default filters and an `is_default` flag per report.

| Route | Permission |
|---|---|
| `GET /reports/layouts?report_key=` | `reporting.view` |
| `POST /reports/layouts` | `reporting.layout.manage` |
| `PATCH /reports/layouts/:id` | `reporting.layout.manage` |
| `DELETE /reports/layouts/:id` | `reporting.layout.manage` |

The report's SQL is never user-authored. `normalizeColumns` drops any key the registered
report does not produce and refuses a layout with nothing left visible
(`REPORT_LAYOUT_EMPTY`), and an unknown `reportKey` is a 404 on save. That is what keeps a
"designer" from becoming a query editor pointed at other tenants' data — and it means a
layout keeps working when the report behind it is rewritten.

`GET /reports/:key` accepts `?layout=<id|name>`; omitting it applies the report's default
layout, and `?layout=none` forces the raw column set. Layout filters are merged *under* the
caller's own filters, so a saved default is a starting point rather than a cage.

These layout routes are declared **before** `@Get(':key')` in the controller — otherwise
`/reports/layouts` would be parsed as a report named "layouts".

Both `ReportingService` and `ReportLayoutsService` take the injected `DATABASE_HANDLE`
rather than the module-level `getDatabase()` singleton, so a test (or any second pool) runs
reports against the database it was actually given.

## Printed documents

`PrintTemplatesService` renders the six papers a company hands out: the sales invoice, the
purchase invoice, the receipt/payment voucher, the journal voucher and the daily shift
close. Each route returns `{ html }` — one self-contained A4 page with its own stylesheet,
no external font, image or script — because the admin shows it inside a sandboxed iframe
and the same string is what gets saved to disk or sent to a printer.

| Route | Document |
|---|---|
| `GET /reports/print/invoices/:id` | فاتورة مبيعات / مردود / عرض سعر |
| `GET /reports/print/purchase-invoices/:id` | فاتورة مشتريات / مردود مشتريات |
| `GET /reports/print/vouchers/:id` | سند قبض / سند صرف |
| `GET /reports/print/journal-entries/:id` | سند قيد |
| `GET /reports/print/shifts/:id` | إغلاق اليومية |

All five require `reporting.view` and are tenant-scoped through `withTenantTx`, so a
document id from another tenant is a 404 rather than a leak.

Two details are deliberate:

- **The amount in words** (`tafqeet.ts`) is printed on every money document. It implements
  the conventional accounting form — unit before ten (`واحد وعشرون`), the dual
  (`مائتان`, `ألفان`), the 3–10 plural (`ثلاثة آلاف`) and the accusative singular after
  11–99 (`خمسة عشر ريالاً`) — and names the currency and its fraction from an ISO code.
- **The ZATCA QR** is drawn only when the invoice actually carries a reported TLV payload.
  Until then the slot prints a sentence saying so, because a decorative square that no
  scanner can read is worse than an honest gap.
