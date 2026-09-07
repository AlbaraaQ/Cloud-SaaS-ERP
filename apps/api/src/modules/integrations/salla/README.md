# Salla integration (Phase 22)

Feature flag: `integration.salla`. OAuth access/refresh tokens and webhook secrets are encrypted with AES-256-GCM. Export queue/log endpoints use a mockable worker path; diff flags mirror legacy `vw_Items_Salla_Status` fields: name, price, cost and qty. Order webhooks require HMAC-SHA256 verification before creating sales invoices.
