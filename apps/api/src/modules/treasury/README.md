# Treasury module

Phase 12 unifies legacy receipts and Sand* documents into `vouchers`, adds cash transfers, expense types, cashier shift close, cheque transitions, and cash-location balance writers.

| Document | Debit | Credit |
| --- | --- | --- |
| Receipt voucher | cash/bank/card location | party receivable or counter account |
| Payment voucher | party payable, expense, VAT, salary, or counter account | cash/bank/card location |
| Cleared receipt cheque | cash/bank location | cheque clearing memo/profile account |
| Bounced cheque | reversal/memo per profile | reversal/memo per profile |
| Cash transfer send | in-transit/counter | source cash location |
| Cash transfer receive | destination cash location | in-transit/counter |

Cash-location balances are updated in the same transaction as voucher posting, voiding, transfer send, and transfer receive. Cheque vouchers affect balances only on terminal collection/clearance.

Shift close stores counted denomination lines, expected cash from posted cash vouchers inside the shift window, and `diff = counted - expected`. Reports remain structured JSON until Phase 14 rendering.
