# PROJECT_CONTRACT

> **Level A — FROZEN.** Any change here requires an approved ADR
> (`ARCHITECTURE_DECISION_RECORDS.md`) before implementation continues.
> This file is the constitution: every phase prompt inherits it by reference.

---

## 1. Identity & Naming Conventions

| Thing | Convention | Example |
|---|---|---|
| DB tables | `snake_case`, plural | `sales_invoices`, `journal_entry_lines` |
| DB columns | `snake_case` | `unit_price`, `deleted_at` |
| DB enums | separate lookup table OR `text` + CHECK; never PG `ENUM` type (migration pain) | `status text CHECK (status IN (...))` |
| TypeScript files | `kebab-case` | `posting-engine.service.ts` |
| TypeScript classes | `PascalCase` | `PostingEngineService` |
| TS vars/functions | `camelCase` | `postJournalEntry()` |
| API paths | `/api/v{n}/kebab-plural` | `/api/v1/sales-invoices` |
| Permission codes | `module.entity.action` | `sales.invoice.post`, `accounting.period.close` |
| CSS classes (frontends) | Tailwind utilities only; no custom CSS unless justified |
| Env vars | `SCREAMING_SNAKE` | `DATABASE_URL`, `S3_BUCKET` |
| Doc types (domain) | `snake_case` codes | `sales_invoice`, `receipt_voucher`, `stock_transfer` |
| Legacy references | column `legacy_id text` + `legacy_source text` | `legacy_source='data16'` |

## 2. Primary Keys & IDs

- **All PKs: `uuid` generated as UUID v7** (time-ordered, index-friendly) via
  application-side generation (`uuidv7` lib) — never DB `gen_random_uuid()` (not sortable).
- Human-readable numbers come from `document_sequences` (see `DATABASE_DESIGN.md` §4.1)
  allocated with `SELECT … FOR UPDATE` inside the same transaction as the insert.
  Sequence scope: `(tenant_id, branch_id, doc_type, fiscal_year_id NULL-able)`.
  Sequences are **gap-tolerant** unless a module explicitly configures strict mode.
- `legacy_id` keeps the legacy PK verbatim (`'INV-12345'`, `'42'`…) per migrated row,
  unique per `(tenant_id, legacy_source, entity)`.

## 3. Money, Quantities, Rates

- Currency amounts: `numeric(20,4)`. Unit prices: `numeric(20,4)`.
  Quantities: `numeric(20,4)` base-unit denominated. FX rates: `numeric(20,10)`.
- TS representation: `decimal.js` (string-serialized over the wire). **IEEE `number`
  is forbidden for money.** ESLint rule `no-restricted-syntax` guards it.
- Rounding: `ROUND_HALF_UP` at (a) each invoice line net/tax, (b) document totals.
  Rounding grain = currency minor units from `currencies.minor_units` (SAR=2).
- Multi-currency: every monetary document stores `currency_code`, `fx_rate`,
  and base amounts; journal lines carry both document & base currency amounts.

## 4. Time

- Storage: `timestamptz` UTC everywhere; business/posting dates additionally as
  `date` columns resolved in the **tenant timezone** (`tenants.timezone`).
- APIs accept/emit ISO-8601 with offset. Frontends render in tenant/user timezone.
- Fiscal boundaries are defined on tenant-local dates, stored as `date`.

## 5. Soft Delete & Immutability

- Master data: `deleted_at timestamptz NULL`, `deleted_by uuid NULL`.
  Unique constraints use partial indexes `WHERE deleted_at IS NULL`.
- **Posted financial documents & journal entries are NEVER updated/deleted**
  (DB trigger + REVOKE). Lifecycle: `draft → posted → void(via reversal)`.
- Inventory ledger (`inventory_transactions`) is append-only as well.

## 6. Tenancy

- Every business table has `tenant_id uuid NOT NULL REFERENCES tenants(id)`.
- Isolation: application guard + **Postgres RLS** (`app.tenant_id` GUC set per
  transaction by the DataSource middleware). Defense in depth, not either/or.
- Composite unique keys always include `tenant_id`.
- Platform tables without `tenant_id`: `tenants`, `users`, `permissions`,
  `migrations_log`. Everything else is tenant-scoped. No exceptions.
- Cross-tenant access exists ONLY in platform-admin plane (separate Nest module,
  separate guard, separate audit channel).

## 7. Audit

- Server-managed columns on all business tables: `created_at`, `created_by`,
  `updated_at`, `updated_by` (+ soft-delete pair).
- Change history: append-only `audit_log` (before/after JSONB) written by an
  interceptor for every mutating endpoint + selected service calls.
- Financial posting actions additionally write `source_type/source_id` on journals.

## 8. API Rules (summary; normative = `API_CONTRACT.md`)

- Versioning: path prefix `/api/v1`. Breaking changes → `/api/v2` (new major).
- Errors: RFC 9457 `application/problem+json` with `code`, `traceId`.
- Pagination: `?limit` (≤200, default 50) + `&offset`, response envelope
  `{ data, meta: { total, limit, offset } }`.
- Filtering/sorting/search: `filter[field]=`, `sort=-date,total`, `q=`.
- Mutating money endpoints accept `Idempotency-Key` header (24h dedupe).
- Tenant context comes from the **access token**; it cannot be supplied by the client.
  Optional `X-Branch-Id` scopes branch data when the membership allows it.

## 9. Authentication & Authorization

- Access token: JWT RS256, 15 min, claims `{ sub, tid, mid, scope }`.
- Refresh: rotating refresh tokens (30 d, hashed at rest, reuse-detection → family revoke).
- Passwords: Argon2id (memory 64 MiB, lanes 4, time 3). Legacy passwords are migrated
  as **force-reset**: no password hashes are imported from legacy.
- Authorization: RBAC permissions + optional branch scoping on membership.
- Sensitive plaintext fields in legacy (`Users.pwd`, `HR_InOutAct_Tbl.password`,
  `SettingEmail.SendPWd`, MQTT/Eta/Salla secrets) are **never** migrated as-is;
  secrets move to encrypted storage (`SECURITY_ARCHITECTURE.md` §9).

## 10. Error & Logging Discipline

- pino JSON logs, `traceId` propagated (AsyncLocalStorage). No logs of PII/secrets.
- Every 4xx/5xx response carries a stable `code` from `packages/contracts/errors.ts`.

## 11. Testing Gates (per phase — details in `TESTING_STRATEGY.md`)

- Unit tests for every domain service (≥80% line coverage on changed code).
- API integration tests (Testcontainers Postgres) for every endpoint added.
- **Tenant isolation test** for every new tenant-scoped resource.
- Accounting changes must pass the **invariant suite** (double-entry, immutability,
  period-lock, FIFO of reversals).
- `npm run verify` = typegen + tsc --noEmit + build + tests must pass before merge.

## 12. Documentation Duties

- New/changed table → update `DATABASE_DESIGN.md` (+ ADR if structural).
- New/changed endpoint → update `API_CONTRACT.md` (OpenAPI as code artifact too).
- New/changed permission → update `SECURITY_ARCHITECTURE.md` §5 matrix.
- New module → add sections to both UI master requirement files.
- Every phase ends by updating `docs/STATUS.md` (progress ledger, created in P01).

## 13. Non-Negotiable "Never" List

1. Never let cloud code read/write the legacy SQL Server at runtime (migration tool excepted, offline).
2. Never store money in `float`/`number`.
3. Never mutate posted journals/inventory ledger — reverse instead.
4. Never bypass RLS except the migration role inside the migrator, or platform-admin plane.
5. Never import legacy plaintext passwords/secrets.
6. Never extend a phase beyond its `SCOPE` — split work via change requests instead.
7. Never use `SELECT *`, string-concatenated SQL, or tenant-unscoped queries.
