# TESTING_STRATEGY

> Level C (reference) but its gates are mandatory per `PROJECT_CONTRACT` §11.

## 1. Pyramid & Tooling

```
        e2e (Playwright, P17/P18 only, critical flows)
       /      \
  integration (supertest + Testcontainers PG: API ↔ DB real)
 /                \
unit (Vitest: services, math, guards, mapping transforms)
```

- DB migrations tested by `drizzle-kit` + boot-time verify script.
- Factories/fixtures in `packages/testing` (tenant factories, COA fixtures, invoice
  builders, legacy-row samples pulled from anonymized dumps in `fixtures/legacy/`).

## 2. Mandatory Test Classes (per phase)

| Class | Applies to | Assertion essence |
|---|---|---|
| Unit | every service | business rules, rounding, guards |
| API integration | every endpoint | status codes, DTO contract, authN/Z, error codes |
| Tenant isolation | every new scoped resource | §6 harness — 4 proofs (read/write/fk/rls) |
| Accounting invariants | accounting-adjacent phases | T1..T10 (ACCOUNTING_ARCHITECTURE §7) |
| Money/rounding | money-touching code | decimal string, HALF_UP, no float |
| Migration | migration phases | transforms, dedupe, reconciliation maths on fixtures |

## 3. Coverage & Gates

- Changed-lines coverage ≥ 80%; accounting/inventory math ≥ 95% on engines.
- `npm run verify`: typegen → tsc → lint → unit → integration → build. All green.
- CI matrix: unit on every push; integration nightly + on PR to main; e2e per release.

## 4. Accounting & Inventory Determinism Tests

- Golden fixtures: seeded tenant with scripted docs (sales/purchases/returns/vouchers/
  payroll) → snapshot TB, GL, stock valuations, VAT report. Any change must justify a
  snapshot diff in the PR notes (Change Process).
- Property-based tests (fast-check) for: allocation splits never exceed, reversal
  mirrors, fx round-trip within tolerance.

## 5. Performance Smoke

- Hot paths benchmarked in CI (rates as budgets): invoice post ≤ 300 ms p95 fixture
  scale (100k lines), TB(1y) ≤ 800 ms, stock level ≤ 100 ms. Budgets live in
  `packages/testing/perf-budgets.json`; failures = warn until P23, then hard.

## 6. Tenant Isolation Harness (canon, implemented P03)

`packages/testing/isolation-suite.ts` exposes `expectIsolation(app, {a,b, resource})`
used by every phase:

1) read-by-id → 404 · 2) list/search contains none of B's rows ·
3) write with B's FKs (branch/warehouse/party/account/…) → 422/404 ·
4) RLS probe: repo query executed with GUC of A returns only A (and with GUC unset → 0 rows).
Plus negative JWT cases: forged `tid` claim rejected; suspended tenant 402/423 code.

## 7. Migration Testing

- Fixture legacy DB image (anonymized mini-Data16) in docker-compose for CI.
- Snapshot transforms per mapping file; dedupe/idempotency re-run test (second import
  changes nothing); rollback test; reconciliation JSON schema test + golden values.

## 8. E2E Scope (kept small deliberately)

Login→post sale→pay→shift close→TB check (admin). Login→view statement→download invoice
(customer). Migration happy-path UI once. Everything else: integration level.

## 9. Test Data Ethics

Never commit real customer data; legacy fixtures anonymized (names/emails scrubbed);
generator scripts committed instead of dumps for large volumes.
