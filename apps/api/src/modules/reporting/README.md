# Reporting module

Phase 14 adds a registry-driven report read side. Report keys are centrally registered
in `REPORT_KEYS`; adding a report requires a key, parameter contract, query builder,
row shape, export support, and golden tests.

Reports read immutable journals, inventory ledgers, invoices, vouchers, and shift-close
facts. They do not mutate business state or cache totals on master records.

Async exports return a `reports-export` queue token; later worker/rendering phases can
attach generated CSV/XLSX/PDF artifacts to the files table.
