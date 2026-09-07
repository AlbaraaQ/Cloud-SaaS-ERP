# E-invoicing module

Phase 13 stores encrypted ZATCA credentials, builds deterministic UBL XML payloads,
maintains a tenant/environment hash chain, produces QR TLV payloads, records submission
ledger rows, and syncs accepted submission metadata back to posted sales invoices.

## ZATCA runbook

1. Store sandbox credentials with `PUT /einvoice/credentials`.
2. Post sales invoices normally. Posting is not blocked by e-invoicing.
3. Submit asynchronously with `POST /einvoice/sales-invoices/{id}/submit` or queue this call from invoice-posted events.
4. Monitor `GET /einvoice/submissions` and retry failed submissions with `POST /einvoice/submissions/{id}/retry`.
5. Switch `environment=production` only after owner-procured certificates are available.

Egypt ETA is represented by the same adapter boundary but intentionally returns an
explicit not-implemented status until certification scope is approved.
