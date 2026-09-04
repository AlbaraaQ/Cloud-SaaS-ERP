# TARGET_ARCHITECTURE

> Level B — CANONICAL. Change via ADR only.

## 1. Style Decision: Modular Monolith (not microservices)

A **modular monolith** is the target: one deployable API, internally hard-partitioned.
ERP domains are intensely transactional (a sale touches inventory + AR + VAT + journal
atomically). Microservices would force distributed transactions (sagas/outbox chains)
before product-market fit; the monolith keeps ACID, deploy simplicity, and refactors
cheap. Module seams (below) keep a later extraction path open (e.g., Reporting or
E-Invoicing can leave the process if load demands). See ADR-001.

## 2. Technology Stack (choices + reason)

| Layer | Choice | Why (and rejected alternative) |
|---|---|---|
| Runtime | Node.js 22 LTS, TypeScript 5 strict | Team skill, ecosystem, JSON-native; (C#/.NET: re-platforming goal) |
| Framework | **NestJS 11** | DI, modules, guards/interceptors/pipes = ERP enforcement points; decorators → OpenAPI; huge hiring pool. (Fastify-express raw: too little structure → entropy risk in 20+ modules) |
| DB | **PostgreSQL 16** | RLS for tenancy, `numeric`, partial indexes, JSONB, ltree, mature tooling. (MySQL: weaker money/types & RLS absent) |
| ORM | **Drizzle ORM + drizzle-kit** | SQL-faithful, type-safe, zero magic for accounting queries, migrations as SQL. (Prisma: heavier runtime & migration black-boxing; TypeORM: metadata traps) |
| Validation | **zod** + `nestjs-zod` pipes | Shared schemas API↔contracts package |
| Auth | JWT RS256 access + rotating refresh (opaque, hashed) | `SECURITY_ARCHITECTURE.md` |
| Passwords | Argon2id (`@node-rs/argon2`) | OWASP 2024 baseline |
| Queue | **BullMQ** (Redis) | E-invoice submission, notifications, reports exports, migration jobs |
| Cache | Redis (read-through for lookups; explicit invalidation) | Not for money reads of record |
| Storage | S3-compatible (MinIO dev) + pre-signed URLs | Logos, scans, exports |
| Docs | `@nestjs/swagger` OpenAPI generated from DTOs | Contract-first-ish, single artifact |
| Logging | pino (`nestjs-pino`), AsyncLocalStorage trace | JSON, redaction paths |
| Tests | Vitest (unit) + supertest + Testcontainers (integration) | Fast, DB-real tests |
| Monorepo | **pnpm workspaces** | Simple; no Turborepo dependency in v1 |
| Frontends | **Next.js 15** (App Router) ×2 (admin, customer), Tailwind + shadcn/ui, TanStack Query, react-hook-form + zod | Type-shared via `@erp/contracts` |

## 3. Repository Layout (normative)

```
erp-saas/
├── package.json / pnpm-workspace.yaml / tsconfig.base.json
├── docs/                        ← this SSOT
├── apps/
│   ├── api/                     ← NestJS modular monolith
│   │   └── src/
│   │       ├── main.ts / app.module.ts / config/
│   │       └── modules/
│   │           ├── platform/      # tenancy, users, auth, audit, files, notifications, jobs, sequences
│   │           ├── organization/  # company profile, branches, warehouses, cash locations, currencies
│   │           ├── catalog/       # items, categories, units, prices, taxes
│   │           ├── accounting/    # COA, journals, periods, posting engine, statements
│   │           ├── parties/       # customers/suppliers, AR/AP, allocations
│   │           ├── inventory/     # ledger, balances, adjustments, transfers, lots/serials
│   │           ├── sales/ purchases/ treasury/ e-invoicing/ reporting/
│   │           ├── hrm/ projects/ pos/ niche/ integrations/
│   │           └── migration/     # import application services (engine lives in apps/migrator)
│   ├── admin/                   ← Next.js admin panel (P17)
│   ├── customer/                ← Next.js customer portal (P18)
│   └── migrator/                ← Node ETL CLI (P15): extract/map/validate/load
├── packages/
│   ├── database/                ← Drizzle schema + client + RLS helpers + migrations/
│   ├── contracts/               ← zod DTOs, error codes, permission codes (API↔UI shared)
│   ├── config/                  ← env schema, constants, feature flags
│   └── testing/                 ← fixtures, factories, testcontainers helpers
├── infrastructure/
│   ├── docker-compose.yml       # postgres, redis, minio, mailhog
│   └── env/.env.example
└── tests/                       # cross-app e2e (optional phase-level)
```

## 4. Module Boundary Rules

1. A module exports a **public API** (`index.ts`): services/DTOs others may use.
   Everything else is private (enforced by `eslint-plugin-boundaries`).
2. Cross-module calls are synchronous service calls inside the same transaction when
   invariants require (sale post → inventory + journal); otherwise via **domain events**
   (in-process emitter now; outbox table exists for future async).
3. No module reaches into another module's tables; all persistence via owning module
   services. Reporting reads through dedicated read-repositories (CQRS-lite).
4. Dependency direction: `platform` ← everyone; `organization` ← business modules;
   `accounting` & `inventory` never depend on `sales`/`purchases` (only on their
   posting DTOs). Verticals (`pos`, `projects`, `hrm`, `niche`) depend on core, never vice versa.

## 5. Runtime Topology & Deployment

- Docker images: `api` (stateless, N replicas), `worker` (BullMQ consumers), `admin`,
  `customer`, `migrator` (job-run). Postgres managed; Redis; S3.
- Stateless API: sessions via tokens; sticky sessions not required.
- Config via env (12-factor); `packages/config` zod-validates at boot (fail-fast).
- Health: `/health/live`, `/health/ready` (db+redis). Metrics endpoint (Prometheus)
  added in P23.

## 6. Background Processing

BullMQ queues: `einvoice`, `notifications`, `reports-export`, `migration`, `maintenance`.
Idempotent consumers; jobs carry `tenant_id` and re-apply RLS context inside the worker.

## 7. Caching Policy

Lookups (units, tax rates, permission sets) cacheable 60 s; **no caching** of journal,
balances-as-of-record, or stock balances — always computed from transactional state.
Report results cacheable keyed by params hash (5 min) except "as-of-now" stock.

## 8. File & Notification Strategy

Files: S3 pre-signed upload → `files` row → entity attach; antivirus hook interface
(deferred: behind `VirusScanner` port). Notifications: in-app table + email (SMTP/SES
port); templates per tenant later.

## 9. Legacy Compatibility Direction (frozen)

Desktop → API gateway (`/api/v1/compat/…`, device API keys, sync cursors) → cloud DB.
Cloud **never** connects to legacy SQL Server at runtime (offline migrator excepted).
Details: phase P16 + `MIGRATION_ARCHITECTURE.md` §9.

## 10. Observability

pino + OpenTelemetry SDK (traces → OTLP endpoint optional P23); error taxonomy from
`@erp/contracts`; audit_log for business auditing; metrics: req rate, latency p95,
queue depth, einvoice failures, migration throughput.
