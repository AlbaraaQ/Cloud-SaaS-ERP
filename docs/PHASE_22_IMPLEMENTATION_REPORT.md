# Phase 22 Implementation Report — Niche Verticals & Salla Integration

Date: 2026-09-07
State: COMPLETE

## Delivered

- Added niche schema/migration `0018_niche_verticals_salla.sql` with FORCE RLS on 19 tenant-scoped tables.
- Optics pack: prescriptions attached to party and invoice line, typed right/left eye JSON and legacy `Other_Column` JSON grid, plus invoice print-section endpoint.
- Tailoring pack: versioned `customer_measurements` with latest-card lookup for party/invoice context.
- Marina pack: vessel groups, pricing rows, vessels, owner percentages, bookings, additions, rental invoice composition through Sales, violations and operation plans.
- Fitment pack: vehicle makes/models, item fitment rows and compatibility lookup endpoints.
- Salla integration: OAuth authorization URL, encrypted connection storage, branch mappings, item diff flags, export queue/log mock worker and HMAC-secured order webhook ingestion.
- Added permissions, admin pages for all five vertical areas, READMEs, unit tests, API docs, legacy/RC markers and OpenAPI regeneration.

## Security and scope notes

- Salla tokens and webhook secrets are AES-256-GCM encrypted using the same local data-key pattern as e-invoicing secrets.
- Webhook ingestion validates HMAC before invoice creation.
- Real Salla network calls, rental payment gateways, kiosk workflows and marketing/CRM scope are intentionally excluded.
