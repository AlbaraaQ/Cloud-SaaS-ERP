# PHASE_13_PROMPT — E-Invoicing (KSA ZATCA production; Egypt ETA adapter stub)

## 0. PROJECT CONTEXT (standalone)
Cloud multi-tenant SaaS ERP. Phase 13 of 23. Frozen rules active. Legacy proves the
domain: ZATCA Phase-II (`ZatcaCredential` CSR/CSID/secret, `CSRProperties`,
`ZatcaEncodedInvoice`, `ZatcaResponse`, invoice `QRCode/InvoiceHash/UUID/ZatcaSent`)
and Egypt ETA config (`EtaSetting`, GS1/EGS item codes, `Foundation.EgyEInvoice`).
New design: credentials encrypted at rest (AES-GCM, `DATA_ENC_KEY`), submissions
ledger with retry, async processing through BullMQ `einvoice` queue, invoice columns
already exist from P10. Direction: signing/submission must be NON-BLOCKING for
posting (invoice posts; e-invoice pipeline follows) with clear failure surfaces.

## 1. CURRENT PHASE
**#13 — E-Invoicing**: credentials vault, signing pipeline, submission orchestration,
status tracking, ZATCA production-grade adapter; ETA adapter behind interface (stub +
config validation, no certification).

## 2. REQUIRED INPUT DOCUMENTS
1. `docs/AI_DEVELOPMENT_PROTOCOL.md` 2. `docs/PROJECT_CONTRACT.md`
3. `docs/DATABASE_DESIGN.md` §13 4. `docs/API_CONTRACT.md` §10
5. `docs/SECURITY_ARCHITECTURE.md` §9 6. `docs/LEGACY_BUSINESS_LOGIC.md` BL-11
7. `docs/TARGET_ARCHITECTURE.md` §6 8. `docs/STATUS.md`.
(External spec links to fetch during implementation: ZATCA Fatoora developer portal —
sandbox endpoints & UBL 2.1 KSA profiles; keep usage behind an adapter so spec drift
is isolated.)

## 3. PREVIOUS PHASE OUTPUTS (verify; else stop)
Sales invoices (uuid/hash/qr/status columns + drafts/posting), outbox/jobs infra,
files, settings.

## 4. GOAL & SCOPE
### In scope
- Tables+RLS: `einvoice_credentials, einvoice_submissions` (+ audit hooks).
- Crypto vault service: encrypt/decrypt for credential fields; masked reads; key
  rotation note (re-encrypt job stub).
- ZATCA adapter: CSR onboarding storage, UBL 2.1 XML build for invoice/credit/debit
  notes (B2C simplified & B2B standard), hash chaining (previous hash per tenant
  sequence registry table `einvoice_chain` — ADD this table; update DATABASE_DESIGN
  via change process entry), QR TLV generation, signing (ECDSA secp256k1 via node
  crypto), compliance/report-submission endpoints, sandbox & production env config.
- Submission orchestrator: events `invoice.posted` → enqueue; retry policy
  (exponential, max N, manual retry endpoint); status reflected back to invoice
  (`einvoice_status` pending/cleared/reported/failed).
- Console endpoints per API_CONTRACT §10 + health probe.
- ETA adapter: interface + config schema validation + explicit `not_implemented`
  error path; disabled routes unless `authority=eta` feature flag per tenant.
### Out of scope (DO NOT DO)
Production certificate issuance against ZATCA for the owner (owner action; you
implement the flows), real ETA calls, WhatsApp/SMS delivery, e-invoice PDF/A-3
embedding (P14 print may embed QR image only).

## 5. EXACT TASKS
1. Migrations (+`einvoice_chain` via CR) + RLS; permissions `einvoice.view/manage/
   submit/credentials.manage` seeds.
2. Vault service + tests (round-trip, wrong-key fails closed, no plaintext in dumps).
3. UBL builder + golden-file tests vs static expected XML for fixtures (simplified
   B2C with QR TLV; standard B2B skeleton).
4. Signing + hash-chain service (sequential per tenant — chain row locked in tx;
   concurrency test).
5. Queue consumer with sandbox client (HTTP) + retry/marking + invoice status sync.
6. Endpoints incl. retry, list filters, health; admin-masked credential GET.
7. ETA stub + flag gating tests.
8. Failure surfaces: failed submissions produce notifications (P04 hook) — test.
9. STATUS.md; ADMIN master §10 alignment; DATABASE_DESIGN §13 append chain table.

## 6. DATABASE IMPACT
+3 tables RLS; invoice columns already exist (no change); chain registry new.

## 7. API IMPACT
API_CONTRACT §10 implemented; no deviation expected; `EINVOICE_REJECTED` reused.

## 8. SECURITY REQUIREMENTS
Private keys/CSID secrets encrypted; masked GET (show last 4 only); submission
payloads may include party tax data — logs redacted; rate limits on retry; sandbox vs
production clearly separated (env-bound base URLs allow-list).

## 9. TESTING REQUIREMENTS
Golden UBL/QR snapshots; vault tests; chain concurrency; queue retry tests with fake
client; integration endpoints; isolation proofs; feature-flag-off behavior tests.

## 10. DOCUMENTATION REQUIREMENTS
STATUS.md; module README (onboarding runbook steps for ZATCA sandbox→production);
SECURITY §9 tick confirmed; CR for the added table included in report.

## 11. ACCEPTANCE CRITERIA
- Fixture invoice produces deterministic UBL+hash+QR; sandbox-mode submission flow
  returns parsed status (mock client acceptable where sandbox unavailable offline —
  mock layer is contract-tested).
- Retry moves pending→failed with notification; manual retry works.
- Secrets never retrievable via API (masked) nor present in audit/log dumps (tests).

## 12. DEFINITION OF DONE
verify green · test classes · docs + CR note · protocol §8 report.

## 13. DELIVERABLES
E-invoicing module (ZATCA complete pipeline, ETA stub) + vault + docs.

## 14. DO NOT DO
Certificate procurement for owner accounts · ETA real integration · PDF/A-3 ·
any change to invoice totals logic (P10 surface untouched; you only read).
