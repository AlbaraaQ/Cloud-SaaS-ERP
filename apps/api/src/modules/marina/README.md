# Marina pack (Phase 22 + Phase 09 part six)

Feature flag: `pack.marina`. Covers vessel groups, hour/half-hour/offer pricing, vessels,
owner percentage links, bookings with insurance/companions metadata, booking additions,
rental invoice composition through the sales service, violations and lightweight daily
operation plans.

## Where the documents come from (Phase 09 part six)

| Window | File | What it holds |
|---|---|---|
| الحجوزات | `Form_WPF/frmBookingM.xaml` («الحجوزات») | «📋 بيانات الحجوزات» و«🔍 البحث» — 🔢 الرقم · 📅 التاريخ · 📋 الفئة · 🔖 حالة الحجز («مؤكد»/«غير مؤكد») · 🚢 نوع الحجز («حجز عادي»/«بحر مفتوح») · ⏱️ المدة ساعة/دقيقة · 💰 القيمة · 🎁 الإضافات (الكمية · السعر · الإجمالي) · مجاميع «إجمالي الإضافات · الإجمالي · ضريبة 15% · الصافي» |
| المخالفات | `Form_WPF/frmViolationM.xaml` («المخالفات») | 🔢 الرقم · ⛵ المركب · ⚠️ نوع المخالفة · ⏱️ مدة المخالفة (يوم) · 📝 ملاحظة — وثلاثة رفوض: «يجب اختيار المركب» · «يجب تحديد مدة المخالفة» · «يجب تحديد نوع المخالفة» |
| بحث الفواتير | `Form_WPF/frmInvoiceRentSrch.xaml` («بحث الفواتير») | «🔍 خيارات البحث»: العميل أو جواله · التاريخان · الصافي من/إلى، و«🧾 قائمة الفواتير»: الرقم · التاريخ · العميل · الصافي · الجوال |

The save is one transaction over three tables (`frmBookingM.xaml.cs` L590–L740):
`RentInvoice` (with `tot_Rent` · `tot_Additions` · `tax` · `tot_net` · `RentPeriod`), then
`Booking`, then `delete BookingAddition` and the additions again. The totals are
`CalcuAll` (L478) — `ضريبة = ROUND(الإجمالي × MainVAT ÷ 100, 2)` و`الصافي = الإجمالي +
الضريبة` — with `MainVAT` read from `SettingGeneral where Inv_Id=4`
(`marina.vatRate` here, 15 by default).

`booking-documents.service.ts` carries the two documents; `marina.service.ts` keeps the
definitions, the operations and the rental invoice. The **operations controller is
registered first** in `marina.module.ts`: its static `bookings/uninvoiced` must be mapped
before the documents' `bookings/:id`.

## Endpoints

| Method | Path | Permission | What it is |
|---|---|---|---|
| GET | `/marina/bookings` | `marina.view` | the list; `?number=` · `?customer=` (name or phone) · `?partyId=` · `?vesselId=` · `?status=` · `?from=`/`?to=` |
| GET/POST | `/marina/bookings`, `/marina/bookings/{id}` | view / manage | the card; «يجب تحديد مدة الحجز» before anything is written |
| PATCH/DELETE | `/marina/bookings/{id}` | `marina.manage` | تعديل (with `version`) · حذف ناعم |
| POST/DELETE | `/marina/bookings/{id}/additions`, `…/additions/{additionId}` | `marina.manage` | 🎁 الإضافات — العدد × السعر |
| POST | `/marina/bookings/{id}/rental-invoice` | `marina.invoice` | `RentInvoice` — القيمة · الإضافات · التأمين · الإجمالي · الضريبة · الصافي |
| GET/POST | `/marina/violations`, `/marina/violations/{id}` | view / manage | ⚠️ المخالفات، وثلاثة رفوض بترتيبها |
| PATCH/DELETE | `/marina/violations/{id}` | `marina.manage` | تعديل · حذف ناعم («اختر المخالفة ليتم حذفها») |
| GET | `/marina/rental-invoices` | `marina.view` | «🔍 خيارات البحث»: `?customer=` · `?from=`/`?to=` · `?minNet=`/`?maxNet=` |
| POST | `/marina/rental-invoices/link` | `marina.invoice` | إصدار فواتير لحجوزاتٍ بلا فاتورة |

## Tests

- `apps/api/test/marina-booking-documents.spec.ts` — 11 tests: the card and its four
  totals, the refusals, the additions, the version conflict, the search, the violation and
  its three refusals, the rental invoice and its search, the delete, the permission split
  and tenant isolation.
- `apps/api/test/marina-operations.spec.ts` — preparation, rota, invoice linking and the
  frozen day (a booking dated into a closed day is still refused).
- `scripts/verify-marina.mjs` — 45 live checks against a running stack; re-runnable and
  non-destructive (it deletes the bookings and violations it creates, in a `finally`).

## Staff screens

`/marina/bookings` (⛵ الحجوزات), `/marina/violations` (⚠️ المخالفات) and
`/marina/link-invoices` (🧾 بحث الفواتير — with «🔍 خيارات البحث» and the linking action).
 `/marina/vessels` keeps the definitions (groups · vessels · owners), and
📋 بطاقة الفئة (`frmGroupM`) is the next part.
