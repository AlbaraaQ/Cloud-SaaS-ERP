# Desktop parity programme — من الدسكتوب إلى السحابة

The desktop product (`Desktop_ERP/`, SmartAuditERP — WPF + SQL Server, ~39K lines of
business logic in `Class/`, 273 screens in `Form_WPF/`, 95 DevExpress `.repx` reports)
is the functional specification. The cloud (this repo) is a clean-room rebuild:
**same behaviours and Arabic data, new architecture** (NestJS API + Next.js surfaces +
Postgres, multi-tenant, RBAC).

## Rules of the programme

1. Desktop code is **read for rules, never copied for architecture** — no `SqlClient`,
   no `MessageBox` in services, no static globals. Every ported rule lands behind a
   real API endpoint with tests.
2. Arabic names/codes stay verbatim (chart of accounts, invoice-type labels, units).
3. No mock UIs: a screen ships only when its endpoint exists; otherwise `status: 'api'`
   in `apps/staff/lib/navigation.ts`.
4. API compatibility is kept — porting adds endpoints/fields, never renames existing ones.
5. Each phase ends with: code + specs green + this README's status table updated.

## Status

| Phase | Scope | Status | Doc |
|---|---|---|---|
| 00 | Survey: architecture map, invoice matrix, file index | ✅ done | `PHASE_00_SURVEY.md` |
| 01 | Staff users/permissions UI + navigation dedup | ✅ done | `PHASE_01_USERS_NAV.md` |
| 02 | Sales invoice engine (calc, save, post, returns, notes) | ✅ done | `PHASE_02_SALES_ENGINE.md` |
| 03 | Purchase engine (invoices, returns, landed cost) | ✅ done | `PHASE_03_PURCHASE_ENGINE.md` |
| 04 | POS checkout + cashier shifts + day close | ✅ done | `PHASE_04_POS_SHIFTS.md` |
| 05 | Inventory (vouchers, transfers, counts, **serial/lot lifecycle**, **الرقم التسلسلي على سطر المستند**, reorder, multi-unit, barcodes, expiry, in-transit, item card, BOM/مكوّنات الصنف, production header, inventory reports) | ✅ parts 1–7 done (deferred: the batch half of the document line, §13) | `PHASE_05_INVENTORY.md` §10–§12 · `NEXT_SESSION_PROMPT_AFTER_PHASE_06.md` |
| 06 | Treasury (سند القبض والصرف بوثيقة كاملة + القيد، **تعريف الخزن والبنوك**، حركة الصندوق، **إغلاقات اليومية**، **التحويل البنكي والعميل النقدي**، **مناقلة الخزن**، **قيد الإغلاق** المؤجَّل من الجزأين 4 و5) | 🟡 parts 1–6 + the deferred close entry done (`سند القبض وسند الصرف`، `تعريف الخزن والبنوك`، `حركة الصندوق`، `إغلاقات اليومية`، `التحويل البنكي والعميل النقدي`، `مناقلة الخزن`، `قيد الإغلاق`) | `PHASE_06_TREASURY.md` §1–§15 · `NEXT_SESSION_PROMPT_AFTER_PHASE_06.md` |
| 07 | Accounting (دليل الحسابات والشجرة بالأرصدة، كشف الحساب، إنشاء قيد يومية، مراكز التكلفة، الفترات والميزان) | ✅ **done** — 📂 دليل الحسابات بالأرصدة + بطاقة الحساب (0045) · 📄 كشف الحساب بالرصيد السابق والمتحرّك وتجميعي/تفصيلي وكشف حساب رئيسي · 📒 إنشاء قيد يومية بـ⏰ الوقت و✅ قيد ضريبي والمندوب والفرق= (0046) · 🌳 مراكز التكلفة: شجرة بالأرصدة + 📊 كشف مركز الكلفة، و0047 يُصلح إبطال القيود · 🗂️ الفترات (0048: بطاقة الفترة، ➕/✏️/⚡/🔒/🔓/🗑️ بعبارات الديسكتوب) + ⚖️ ميزان المراجعة بالأعمدة العشرة + 📊 أرباح وخسائر حسابات رئيسية (23 tests + 83 live checks) | `PHASE_07_ACCOUNTING.md` §1–§8 |
| 08 | HRM (بطاقة الموظف، الإدارات والأقسام، الوظائف، الحوافز والجزاءات، دفع الرواتب، كشف حساب موظف، تقرير الرواتب) | 🟡 **part 5 done** — 👤 بطاقة الموظف + 🏢 الإدارات/الأقسام + 💼 الوظائف (0049) · 🎁 الحوافز والجزاءات (0050) · 💵 دفع الرواتب (0051) · 📄 كشف حساب موظف · 📈 حركات الموظف + 📊 تقرير الرواتب (بلا ترحيل: `GET /hrm/employee-movements` يقرأ مبيعات الموظف سطراً بسطر من `salesman_id`، و`GET /hrm/reports/salary` يقرأ إذونات الصرف بـ«كل الفترة»/الشهر والسنة؛ وتصحيحٌ لإذنٍ مرفوض كان يبقى في الدفاتر؛ 68 tests + 105 live checks) | `PHASE_08_HRM.md` §1–§8 |
| 09 | Verticals (contracting/projects, salesmen & commissions, tailoring orders, tailoring invoices, measurements, marina, optics, Salla) | 🟡 **parts 1–4 done** — (1) 🧑‍💼 المندوبون والعمولات: بطاقة المندوب بالثلاث نسب (`comm` · `Colle_Comm` · `Profit_Comm`) + «📋 طباعة فواتير مندوب وعمولاتهم» (ترحيل 0052؛ 14 tests + 21 live checks). (2) 🧵 طلب التفصيل: `frmOrders` + `frmOrderDetails` + `frmOptions` — الطلب وحالاته الأربع وخياراته وقماشه، و⌛ المتبقي = السعر − المدفوع، و⌛ متأخّر حتى «تم التسليم» (ترحيل 0053 يضيف ستة جداول؛ `GET/POST/PATCH/DELETE /tailoring/orders` و`POST …/status` و`/tailoring/types` و`/tailoring/option-categories` و`/tailoring/option-values`؛ 11 tests + 38 live checks). (3) 🧾 فاتورة التفصيل: `frmViewOrders` + `AddNewSizes` — `Inv_Tailor` برقمها `TI-000001` وإجماليها ×1.05 وباقيها، و39 قياساً في `measurements jsonb` تُخدم بسجلّ `GET /tailoring/measurement-fields`، و«إستلام دفعة» بربطها بسند قبض (ترحيل 0054 يضيف ثلاثة جداول؛ `GET/POST/PATCH/DELETE /tailoring/invoices` و`POST …/status` و`POST …/payments` و`/tailoring/garment-types`؛ 11 tests + 41 live checks). (4) 📏 القياسات: `frmMeasurements` + `frmMeasurementDetails` + `frmMeasurementAttributes` — قياس كل عميل باسمه وتاريخه وقيمه، وبطاقة تُبنى وقت التشغيل من «خصائص القياسات» (`ISNULL(MAX(DisplayOrder),0)+1` · «🔕 تعطيل» لا حذفاً · ▲▼ مبادلة الترتيب؛ ترحيل 0055 يضيف `tailoring_measurement_attributes` وعمودي `name`/`measurement_date`؛ `GET/POST/PATCH/DELETE /tailoring/measurements` و`/tailoring/measurement-attributes` و`…/move` و`…/deactivate`؛ 11 tests + 37 live checks) | `PHASE_09_VERTICALS.md` §1–§7 |
| 10 | Reports (95 `.repx` → cloud report engine) | ⬜ | `ROADMAP_PHASES_02_11.md` |
| 11 | Zatca e-invoicing + ETA + integrations hardening | ⬜ | `ROADMAP_PHASES_02_11.md` |

Deferred by the owner: marketing CMS, per-tenant mobile-shop module.
Done earlier, outside this programme: RBAC reorganisation (PR #4), desktop chart
seeding (112 accounts + COGS extension, `packages/database/src/desktop-coa.ts`).
Phase 05 added two more leaves (115 accounts) and three posting-profile keys; an
existing tenant is completed in place by `OrgProvisioningService`.
