# CUSTOMER_UI_MASTER_REQUIREMENTS

> Level C — aggregated customer-facing app (`apps/customer`, Next.js 15).
> Audience: (a) *portal* = tenant's own staff-light usage & their customers where
> enabled; (b) public marketing/onboarding pages. RTL-first; mobile-first.

## 0. Global

Marketing home (value prop, pricing placeholder, contact) · Auth pages (login,
forgot/reset, forced-reset for migrated users — no legacy passwords) · Tenant picker
when user has multiple memberships · Same shell kits as admin but simplified; full
empty/loading/error states; language toggle ar/en.

## 1. Self-Service Portal (per enabled features)

- **Dashboard**: my open balances, recent invoices, pending payments, quick links.
- **My Invoices** (customer-facing when `portal.customer_access=on`): list with status
  chips, detail page (lines, totals, VAT), PDF download, ZATCA QR display.
- **Statement of account**: date-range, running balance, export PDF/CSV
  (parity with legacy account statement).
- **Payments history**: vouchers affecting me, allocations view.
- **Profile**: contact data update request (creates approval task in admin, not direct
  write — keeps books integrity).
- **Notifications**: e-invoice shared, payment received confirmations.

## 2. Lightweight Operational Screens (tenant staff who don't need the admin panel)

- Mobile quick-sale (if `portal.quick_sale`): item search, qty, pay → posts invoice
  (uses same `/sales-invoices` API, permission-scoped `sales.invoice.create`).
- Stock lookup for salespeople (levels by allowed branches only).
- Task inbox: approvals generated elsewhere (price changes, credit overrides).

## 3. Onboarding & Migration-Aware Screens

- Tenant onboarding wizard: company data → COA template choice → first branch/
  warehouse/safe → admin invite → "import from legacy?" hook to admin migration
  console (link, not duplicate).
- Forced password setup for migrated users via email token.

## 4. Public Document Verification (optional flag)

`verify` page: enter invoice UUID/hash → shows issuer, date, total (supports ZATCA
fatoora flows); no auth, rate-limited, no extra data leakage.

## 5. States & UX Contracts

Empty portals (friendly "contact your administrator"), payment failure copy with
support channel, PDF fallbacks when artifact jobs pending, graceful 402 when tenant
suspended (read-only mode banner).

## 6. Non-goals (v1)

No public e-commerce storefront (Salla integration serves that), no password-free
magic links, no embedded PSP checkout (payment link integration is a future ADR).
