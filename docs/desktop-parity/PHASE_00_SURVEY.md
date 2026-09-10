# Phase 00 — Survey (done 2026-09-10)

## Desktop backend shape

`Desktop_ERP/SmartAuditERP/` after the owner's full upload:

| Area | Path | Size |
|---|---|---|
| Business logic | `Class/*.cs` | 143 files, ~39K LOC |
| WPF row/view models | `Class_WPF/*.cs` | 39 files |
| Screen code-behind | `Form_WPF/*.xaml.cs` | 273 files |
| Screen markup | `Form_WPF/*.xaml` | 271 files |
| Reports | `Reports/*.repx` | 95 files |
| Seed scripts | `CrystalLiteDB.txt`, `AlterDb.txt` | chart + entry/invoice types |

Style: converted VB (verbose `Operators.*`, `#region`s, `static` helpers),
business layer calls `SqlClient` directly and pops WPF `MessageBox`es — port the
**rules**, never the shape.

## Key engine files (read these first in every domain phase)

| File | LOC | What it owns |
|---|---|---|
| `Class/InvoiceOper.cs` | 5,519 | `SaveInvoice`, `InvoiceCalc`, `BindToEntry`, payments, Zatca, offers |
| `Class/ItemOper.cs` + `ItemOpercontract.cs` | 2,949 + 2,675 | items, units, barcodes, prices, stock |
| `Class/Common.cs` | 2,405 | shared helpers, formatting, lookups |
| `Class/InvoiceOperContract.cs` | 2,244 | contracting-side invoice ops |
| `Class/EntityOperations.cs` | 1,989 | generic CRUD/audit plumbing |
| `Class/EntryOper.cs` | 1,475 | `BindInvoiceToEntry`, `BindReceiptToEntry`, `SaveEntry` |
| `Class/ReceiptOper.cs` | 844 | receipts/payments |
| `Class/Print.cs` | 1,237 | printing (bill, barcode, A4) |
| `Class/MainClass.cs` | 914 | globals: language, branch, user, settings handles |
| `Class/ZatcaService.cs` | 546 | Zatca send/poll (see also `EtaService.cs`) |
| `Class/Number2Arabic.cs` | — | amount-to-words (تفقيط) — every printed doc needs it |

## Invoice-type matrix (`InvoiceOper.GetInvoiceType`, invType × ProcType)

| invType | ProcType 1 | ProcType 2 | ProcType 3 |
|---|---|---|---|
| 1 | مشتريات (purchase) | مردود مشتريات | — |
| 2 | فاتورة ضريبية/مبسطة/مبيعات (TaxType 2/1/3) | إشعار دائن (credit note) | — |
| 3 | مبيعات (POS channel) | إشعار دائن | فاتورة مؤقتة |
| 20 | مبيعات (2nd channel) | إشعار دائن | — |
| 23 | مبيعات | إشعار دائن | — |
| 21 / 22 | إشعار مدين / دائن | إشعار دائن / مدين | — |
| 4 / 5 | إدخال / إخراج مخزني | — | — |
| 8 | مناقلة مرسلة / مستلمة (by direction flag) | — | — |
| 9 | بضاعة أول المدة | — | — |
| 14 | طلب بضاعة | — | — |

Cloud mapping note: the cloud has no `entry_types`/`invoice_types` tables — document
kinds live as API enums + `branch_posting_profiles.mapping`. Each domain phase must
declare its enum values and their desktop `(invType, ProcType)` equivalent.

## Permission model

Desktop: per-form flags — `FormPermission { Editable, Addable, Delete, Search, Print }`
(`Class/FormPermission.cs`, applied to WPF buttons). Users in `Class/User.cs`,
operation permissions in `ListOperationPermission.cs`.

Cloud: richer — 154-permission registry (`tenant.*` namespace), roles, memberships,
per-membership scopes. The desktop's 5 flags map to cloud verbs
(view/create/edit/delete/print) when porting each screen's gating.

## Domain → forms → cloud (condensed; full list in `ROADMAP_PHASES_02_11.md`)

~24 sales/POS forms, 9 purchase, 35 stock/item, 33 treasury, 34 accounting/tax,
11 HRM, 12 customer, 13 project/contract, 35 reports, 9 users/auth, plus verticals
(optics `frmGlasses`, tailoring `frmMeasurement*`, marina `frmBookingM`…).
Staff surface today: 183 nav screens (see `PHASE_01_USERS_NAV.md` for the dedup).
