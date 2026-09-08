# E-invoicing module

Saudi (ZATCA) e-invoicing, plus the placeholder boundary for Egypt's ETA.

## What actually happens when an invoice is submitted

`POST /sales-invoices/:id/einvoice/submit` (permission `einvoice.submit`) on a **posted**
invoice:

1. **Builds the document.** `zatca/ubl.ts` produces a UBL 2.1 `Invoice` from the real
   invoice: seller party from `company_profiles` (VAT number, CR number, national address),
   buyer party from `parties`, one `cac:InvoiceLine` per line with its own tax category, one
   `cac:TaxSubtotal` per VAT rate, and a `cac:LegalMonetaryTotal` whose arithmetic closes.
   `cbc:InvoiceTypeCode` is `388` for a sale and `381` for a credit note; its `name` attribute
   is `0100000` when the buyer has a VAT number (standard/B2B) and `0200000` otherwise
   (simplified/B2C).
2. **Chains it.** `einvoice_chain` hands out the previous invoice's hash (PIH — the genesis
   value is base64 SHA-256 of `"0"`) and the next invoice counter (ICV) under `FOR UPDATE`,
   so two invoices filed at the same instant cannot share a counter. Both are embedded in the
   document as `cac:AdditionalDocumentReference` entries.
3. **Hashes it.** `hashInvoiceXml` — base64 of the SHA-256 digest of the XML.
4. **Signs it, if it can.** When the tenant has uploaded an EC private key
   (`PUT /einvoice/credentials`), the hash is signed with ECDSA/SHA-256 and the public key is
   exported in DER. A key that is missing, malformed or not EC produces no signature and a
   logged warning — never a fake one.
5. **Builds the QR.** TLV, base64: tags 1–5 (seller name, VAT number, timestamp, total with
   VAT, VAT amount) always; tags 6–8 (hash, signature, public key) only when step 4 succeeded.
6. **Files it, if a gateway is configured.** With `ZATCA_API_BASE_URL` plus a stored CSID and
   secret, the document is POSTed to `/invoices/reporting/single` (or `/clearance/single` for
   standard invoices) with `Accept-Version: V2` and basic auth, and the real HTTP response is
   stored on the submission row.

## The submission states, and why they are not all "reported"

| status | meaning |
|---|---|
| `prepared` | The compliant document exists, is hashed and chained, and carries a valid phase-1 QR. Nothing was sent, because the tenant has not uploaded credentials. |
| `signed` | Same, plus a real signature and phase-2 QR tags. Waiting for a gateway URL. |
| `reported` / `cleared` | The authority answered `2xx`. The response body is stored. |
| `failed` | The authority answered an error, or the call could not be made. `error` says which. |

An invoice is never marked as accepted by an authority the system never spoke to. This is the
one thing a compliance feature must not lie about, and it is why `prepared` exists at all.

## What is still missing for full phase-2 certification

- **The XAdES enveloped signature and `UBLExtensions` block.** The signature bytes are
  produced, but wrapping them in the `ds:Signature`/`xades:QualifyingProperties` structure and
  canonicalising (C14N 1.1, excluding the signature, QR and extension nodes) is not
  implemented. Our generator never writes those three nodes, so the document as produced *is*
  its own hashed form; the canonicaliser has to strip them once they exist.
- **Onboarding.** CSR generation against ZATCA's template, the compliance CSID call, the
  compliance checks and the production CSID exchange are done by the customer today and the
  results pasted into `PUT /einvoice/credentials`.
- **Certificate tag 9** (the CA's signature over the certificate public key) is only available
  from the issued certificate, so it is emitted once the certificate itself is stored.

Everything above is credential-bound, not code-bound: no ZATCA sandbox account can be created
from inside this repository's CI.

## Runbook

1. Fill in بطاقة المنشأة — a missing VAT number makes every document invalid.
2. Generate the CSR on the customer's own device, complete ZATCA onboarding, then store the
   private key, CSID and secret with `PUT /einvoice/credentials` (encrypted at rest with
   AES-256-GCM, returned masked, never readable again).
3. Set `ZATCA_API_BASE_URL` for the environment (simulation first).
4. Post invoices normally; posting is never blocked by e-invoicing.
5. Watch `GET /einvoice/submissions` — the admin screen is الإعدادات ← المزامنة ← Zatca — and
   retry failures there. Retry re-files the stored document; it never rebuilds it, because the
   hash may not move.

Egypt ETA keeps the same adapter boundary and returns an explicit `501 ETA_NOT_IMPLEMENTED`
until certification scope is approved.
