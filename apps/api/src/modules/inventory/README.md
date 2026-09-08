# Inventory ledger

`InventoryService.record()` is the append-only inventory boundary used by sales and purchasing documents. Every movement is recorded in `inventory_transactions` and the same transaction updates `stock_balances`.

## Costing hints

- `inWithCost`: incoming quantity increases the pool at the supplied unit cost.
- `outAtAvg`: outgoing quantity leaves the pool at the current moving average cost.
- `returnAtOriginalCost`: callers may provide the original cost for a return; the ledger records the hint and supplied cost for auditability.

For example, 10 units at 100 followed by 5 at 110 produces `(1000 + 550) / 15 = 103.3333`. Issuing 3 units leaves 12 units and the same average. Invoice discounts are allocated by line value: values 800 and 200 with a discount of 50 receive 40 and 10 respectively.

The ledger is immutable at the database trigger level. `valuationAsOf()` replays transactions up to a timestamp, while `recomputeBalances()` repairs the cache from the ledger.

Adjustment approval, transfer receive workflows, and lot/serial lifecycle APIs remain explicitly tracked as follow-up work until their integration tests are present.

## Production orders (`/inventory/production-orders`, migration 0027)

أمر الإنتاج: components out, one finished item in, in a single transaction. Components
leave at the warehouse's moving average; the output is valued at exactly the total that
left divided by the produced quantity, so inventory value is conserved and the document
raises **no journal entry**. Guards: the output cannot be its own component, a component
cannot repeat, and completion fails with `STOCK_INSUFFICIENT` instead of driving stock
negative. Only a draft order can be completed or cancelled — a completed order is reversed
with a stock adjustment, not by rewriting it.

Permissions: `inventory.view`, `inventory.production.manage` (create/cancel),
`inventory.production.complete` (move the stock). Report key: `production-orders`.
