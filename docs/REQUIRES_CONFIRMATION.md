# REQUIRES_CONFIRMATION

> Level C, but **blocking** for the items marked 🔴.
> Registry of everything that could NOT be proven from the two schema exports.
> Owner answers get recorded in the `Answer` column; decisions then flow into
> `ARCHITECTURE_DECISION_RECORDS.md`. Nothing here may be guessed during implementation.

**Legend:** 🔴 blocks migration/production correctness · 🟡 affects design quality · ⚪ nice to know.

| # | Sev | Topic | Question / Evidence needed | Proposed default if unanswered |
|---|---|---|---|---|
| RC-01 | 🔴 | `InvTypes` data | Export table rows: mapping of (InvType, ProcType) → name + `IsInput`. This defines every document class. | Treat inv_type 1=purchase, 2=POS sale, 3=dine-in sale, others frozen until known |
| RC-02 | 🔴 | `proc_type` values | Full value list (1,2,4 observed) + semantics per inv_type; is 4 "quotation/order"? | Keep type-driven direction mapping in posting profiles |
| RC-03 | 🔴 | `pay_type` map | Exact enum vs `SettingPayMethods` flags (1 cash, 2 card, 4 split, -1/5 credit classes?) | Map to `cash/card/bank/credit/split` with documented transforms |
| RC-04 | 🔴 | Stock direction consistency | Confirm rules used by C# app per doc type (TVFs are inconsistent for sales types) | New engine: explicit direction per doc type (BL-2) |
| RC-05 | 🔴 | `Inv.EntryID` join | Does it join `Entry.id` or `Entry.GlobalID`? Sample rows comparing both | Migration tries both; report orphans |
| RC-06 | 🔴 | Entry reversal | How are posted entries corrected today (state flip? negative re-entry?) | Target: reversal-only; import legacy corrections as-is |
| RC-07 | 🔴 | Invoice total formula | Exact composition of `tot_net` per type (discounts/additions/insurance/VAT-in-price/WHT) + rounding order | Recompute & reconcile per invoice during migration dry-run |
| RC-08 | 🔴 | Supplier linkage | Do purchases store supplier in `Inv.cust_id` (Customers table) or `Suppliers.id`? FK absent | Migrate both; flag mismatches |
| RC-09 | 🔴 | Invoice settlement | Any open-item/allocation logic in app? (no table found) | Use `payment_allocations`; migrate paid amounts as lump allocations by date order |
| RC-10 | 🟡 | Attendance evaluation | Any shift/overtime rules outside DB (C# code)? | Store punches; rules engine deferred |
| RC-11 | 🔴 | Opening balances | `Accounts_Index.IValue`/`Account_Value` semantics; are cached totals trusted? | Recompute from lines; compare; report variance |
| RC-12 | 🟡 | `Accounts_Index.ParentCode` join | ParentCode is nvarchar vs Code int — string-cast join? Leading zeros? | Cast safely; validate tree on import |
| RC-13 | 🟡 | `Type`/`Nature`/`FinalAcc` maps | Enum meanings (Type=2 leaf proven; rest?) | SEED lookups with owner review |
| RC-14 | 🟡 | `Safes` used as `store` | Join in `ItemsExpirationStock(store→Safes.id)` — are stores and safes one table in practice? Or bug? | Keep warehouses vs cash safes separate; verify with data sample |
| RC-15 | 🔴 | `Inv_Sub.val` vs `val1` | Which is canonical quantity (vs `UnitEquality`)? Reports disagree | Reconcile: stock uses `val`, money uses `val1*price`; confirm with samples |
| RC-16 | 🟡 | `Entry_sub.res_id` | Meaning? (receipt ref?) | Carry to `legacy_id`/meta if unresolved |
| RC-17 | 🟡 | `Inv.purch_rest_id` | Linked doc semantics (purchase remainder?) | Metadata carry |
| RC-18 | 🟡 | Receipts enums | `ReceiptType`, `PaymentType`, `CheckState`, `State` value lists | Extract distinct values from data before migration |
| RC-19 | 🔴 | Live voucher tables | Which of `SandQ/SandD/SandQD/SandSD/SandVAT` vs `Receipts` is authoritative today (per customer)? | Import both families into unified `vouchers` with doc-kind mapping |
| RC-20 | 🟡 | `ProductStocks` writer | Which process maintains the cache (no trigger/proc in export)? | Ignore cache; recompute balances from documents |
| RC-21 | ⚪ | `Other_Column` R1..L5 | Optics grid semantics | Park as JSONB metadata on optical module |
| RC-22 | ⚪ | `VATClients` | Where used? | Import as parties flagged `vat_client` |
| RC-23 | 🟡 | `Customers.act` | Lookup → `Acts`? | Map to `activity_id` nullable |
| RC-24 | 🟡 | Contracting enums | `ContrType`, `statusPk`, `RestractionPk`(retention?), `TermPk` value lists | Extract distinct values; map to lookup tables |
| RC-25 | 🟡 | Installment rules | `cont.period` unit (days/months), interest handling in `installval`? | Configurable schedule template |
| RC-26 | 🟡 | Marina pricing | `GroupMarine` hour/half-hour/offer interplay + `RentPeriodSub` | Encode in pricing tables P22 |
| RC-27 | 🔴 | Fiscal-year DBs | Is `Data16` year-isolated (only 2016 docs)? Multi-year migration scope per customer? | Engine accepts N source DBs per tenant, merged on fiscal years |
| RC-28 | 🔴 | Deleted rows | Should soft-deleted legacy rows migrate (as void docs) or be archived-only? | Default: archive + void-marked, excluded from balances |
| RC-29 | 🟡 | `Foundation` vs branches | One company row per DB? Multi-company per DB possible? | One `company_profile` per tenant |
| RC-30 | 🟡 | Employee↔User link | `Users.emp` NOT NULL — is every user an employee? Platform users? | Membership model covers; import mapping table |
| RC-31 | 🔴 | Rounding digits | `SettingGeneral.DigitsNo` default 2 — per tenant actual values? (affects reconciliation tolerance) | Capture per tenant in migration profile; tolerance report |

> Process: owner answers → update this table → if the answer changes architecture,
> open an ADR. Phase P15 (migration) cannot mark "Done" while any 🔴 is open.
