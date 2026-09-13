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
| 07 | Accounting (دليل الحسابات والشجرة بالأرصدة، كشف الحساب، إنشاء قيد يومية، مراكز التكلفة، الفترات والميزان) | 🟡 **parts 1–3 done** — 📂 دليل الحسابات بالأرصدة + بطاقة الحساب (migration 0045) · 📄 كشف الحساب بالرصيد السابق والمتحرّك وتجميعي/تفصيلي وكشف حساب رئيسي · 📒 إنشاء قيد يومية بـ⏰ الوقت و✅ قيد ضريبي والمندوب والفرق= (migration 0046; 21 tests + 48 live checks); parts 4–5 surveyed | `PHASE_07_ACCOUNTING.md` §1–§6 |
| 08 | HRM (employees, attendance, payroll, custody) | ⬜ | `ROADMAP_PHASES_02_11.md` |
| 09 | Verticals (contracting/projects, marina, optics, tailoring, Salla…) | ⬜ | `ROADMAP_PHASES_02_11.md` |
| 10 | Reports (95 `.repx` → cloud report engine) | ⬜ | `ROADMAP_PHASES_02_11.md` |
| 11 | Zatca e-invoicing + ETA + integrations hardening | ⬜ | `ROADMAP_PHASES_02_11.md` |

Deferred by the owner: marketing CMS, per-tenant mobile-shop module.
Done earlier, outside this programme: RBAC reorganisation (PR #4), desktop chart
seeding (112 accounts + COGS extension, `packages/database/src/desktop-coa.ts`).
Phase 05 added two more leaves (115 accounts) and three posting-profile keys; an
existing tenant is completed in place by `OrgProvisioningService`.
