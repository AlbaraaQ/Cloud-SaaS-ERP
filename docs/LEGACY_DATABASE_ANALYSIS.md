# LEGACY_DATABASE_ANALYSIS

> Level C. Deep analysis of legacy SQL Server DB **`Data16`** (two script sections received
> 15/08 schema export). Badges: `CONFIRMED` / `INFERRED` / `UNKNOWN` / `REQUIRES_CONFIRMATION`.

## 0. Global Findings (apply to the whole DB)

| # | Finding | Badge | Impact on the new system |
|---|---|---|---|
| G1 | ~180 tables, only **4 FK constraints** in total | CONFIRMED | Migration must validate orphans; new DB enforces FKs everywhere |
| G2 | Money columns are `float` (≈15 sig. digits, binary) | CONFIRMED | Rounding drift expected; migration reconciles to `numeric(20,4)` with documented variance |
| G3 | `image` columns (deprecated type) for logos/scans | CONFIRMED | Migrate to object storage (`files`), not bytea |
| G4 | `IS_Deleted bit NULL` everywhere, sometimes missing | CONFIRMED | Normalize to `deleted_at/by`; treat NULL as not-deleted |
| G5 | Almost no DB audit columns (only few tables have CreatedBy/Date) | CONFIRMED | New schema adds audit columns universally |
| G6 | No tenant concept; one company per DB; **DB-per-fiscal-year** (`Data16`, `Year_Previews.Dbname`) | CONFIRMED | New: `fiscal_years` rows inside one tenant DB |
| G7 | `GlobalID varchar(20)/nvarchar` PKs on documents (`Inv`, `Entry`, `Receipts`) | CONFIRMED | Keep as `legacy_id`; new PKs are UUID v7 |
| G8 | Sequences per (branch, type) via counters tables (`InvCounters`, `DailyOrderCounter`) | CONFIRMED | Re-implemented by `document_sequences` |
| G9 | Business rules duplicated across TVFs/SPs with subtle differences | CONFIRMED | New rule engine single-sourced in services (see BL docs) |
| G10 | Plaintext/weak secrets (`Users.pwd nvarchar(50)`, `HR_InOutAct_Tbl.password`, `SettingEmail.SendPWd`) | CONFIRMED | Never migrated; force reset; security flag |
| G11 | Arabic + English dual-name columns (`name`, `nameEN`, `AName`) | CONFIRMED | New schema has `name_ar`/`name_en` consistently |
| G12 | `Sync`, `CloudID`, `CloudSerial`, MQTT settings | CONFIRMED | Legacy has offline/branch sync; replaced by API gateway (P16) |
| G13 | Several junk/import tables (`Paste Errors`, `Switchboard Items*`, `try`, `intel`, `intel1`, `sett`, `Table_2`, `[21346]`, `commentsTable`) | CONFIRMED | Excluded from target; archived raw during migration |

---

## 1. Core Document Engine: `Inv` / `Inv_Sub` (+ contract & rent clones)

### 1.1 `dbo.Inv` — universal invoice header (PK `InvGlobalID varchar(20)`)

| Column | Type | Reading | Badge |
|---|---|---|---|
| `InvGlobalID` | varchar(20) PK | Global doc key (string; likely branch/type composite or generated) | CONFIRMED key; format REQUIRES_CONFIRMATION |
| `proc_id` | int identity | Per-doc auto id | CONFIRMED |
| `proc_type` | int | Document direction/family discriminator (1/2/≥4 observed) | CONFIRMED values exist; semantics §BL-2 |
| `id` | int | Human number; unique per `(branch, inv_type, proc_type, id)` via `UQ_InvNo` | CONFIRMED |
| `date`, `IssueDate` | datetime | Doc date / issue datetime | CONFIRMED |
| `inv_type` | int | Document kind (sales 2/3 confirmed in SPs; see `InvTypes`) | CONFIRMED enumeration unknown → `InvTypes` data needed |
| `OrderType` | int | Restaurant order mode (local/takeaway/family/car/table/hosting per `SettingOrderMethods`) | INFERRED |
| `safe`, `stock` | int | Cash safe / warehouse refs | CONFIRMED(names) FK absent |
| `cust_id` | int | Party ref (`Customers.id`; used for supplier docs too?) | REQUIRES_CONFIRMATION (who supplies purchases) |
| `sales_emp`, `salesman` | int | Employee / `salesmen` refs | CONFIRMED |
| `InvTotal, tot_purch, AdditionsTot, Insurance, tot_net, minus(discount), paid, tax, ExtraVAT, ItemsDiscount, InvCost, FreeVATSales, InvSum, VATPercent, AdditionalCost, TotalWithholdingTax, InvProfit` | float | Full money breakdown incl. VAT-in/out, extras, WHT, profit | CONFIRMED columns; exact formula per type REQUIRES_CONFIRMATION |
| `EntryID` | int | Link to journal `Entry` (likely `Entry.id`) | REQUIRES_CONFIRMATION (join column) |
| `purch_rest_id` | int | Linked purchase / related doc id | UNKNOWN exact semantics |
| `Reff_No/Reff_date` | | Supplier ref / return ref | CONFIRMED |
| `branch` | int | Branch ref | CONFIRMED |
| `IS_Buy` | bit | direction flag (INFERRED: 1=purchase-side) | INFERRED |
| `pay_type` | int | Payment method enum (1 net-total, 2 visa, 4 split, -1/5 deferred-ish in SPs) | CONFIRMED values; full map REQUIRES_CONFIRMATION vs `SettingPayMethods` |
| `bank, cash, visa` | | Bank ref + tender split amounts | CONFIRMED |
| `PriceIncVAT` | bit | Prices include VAT (global default in `SettingGeneral.PriceIncVAT`) | CONFIRMED |
| `PaymentStatus`, `InvoiceStatus` | int | Status enums | CONFIRMED names; value maps REQUIRES_CONFIRMATION |
| `InvCombinedId`, `TableNo`, `CloudID`, `CashCustomerName/Mobile` | | Restaurant/cloud/POS extras | CONFIRMED |
| `QRCode, InvoiceHash, UUID, ZatcaSent` | nvarchar(max)/bit | **ZATCA Phase-II fields** | CONFIRMED |
| `Balance_previews`, `PeriodID` | decimal/int | Prior-year balance carryover, fiscal period link (`AccountingPeriods`) | CONFIRMED |
| `CurrencyCode` default `'SAR'` | | Multi-currency support | CONFIRMED |

### 1.2 `dbo.Inv_Sub` — invoice lines (PK identity `id`; links by `InvGlobalID`, no FK)

Key fields: `ItemId`, `unit`, `UnitEquality` (unit conversion), `val` (qty in some
denominator), `val1` (qty used by sales/cost SPs as effective qty), `exchange_price`,
`expire_date`, `taxperc/taxval`, `discount`, `AvrgCost`, `CurrentQnty`, `ItemAddedCost`,
`ItemPriceWithoutVAT`, `WithholdingTax(Perc)`, `ItemCostCenter`, `ItemAdditionalTax(Perc)`,
`Description`, `ProductId` (=0 filter in reports — flags real items vs others?), weights
per line. CONFIRMED columns; `val` vs `val1` exact roles REQUIRES_CONFIRMATION
(most read procs use `val1` for money and `val` for stock in/out; one view uses `val1` for "quantity").

### 1.3 Clones
- `InvContratct` + `_Sub`: copy of Inv plus **contracting columns** (`contract_total_value`,
  `original_work_amount`, `special_discount`, `vat_amount`, `total_with_vat`,
  `work_guarantee` (retention), `net_due_this_payment`, `previously_paid_amount`,
  `remaining_contract_balance`) → **progress billing (مستخلصات)** CONFIRMED.
- `RentInvoice`: marina rental invoice (`MarineId`, `GroupId`, `tot_Rent`, `RentPeriod`,
  `Companions`, insurance) CONFIRMED vertical.

### 1.4 Counters & restaurant helpers
`InvCounters(Branch,InvType,ProcType,LastID)` PK composite CONFIRMED; `DailyOrderCounter`,
`OrderNumbers` (unique per day) CONFIRMED restaurant queue numbers; `Tables`, `Cat_Table`,
`Table_Order` CONFIRMED restaurant tables; `Check_Close` links invoices to shift close.

---

## 2. Accounting

| Object | Analysis | Badge |
|---|---|---|
| `Accounts_Index` | COA: `Code int PK`, `AName`, `Nature int`, `Type int` (2 = leaf/postable per `GetAccountStatement`), `ParentCode nvarchar(50)` (**type mismatch to Code**), `IValue`(opening?), `FinalAcc`, cached `Total_Debts/Total_Credits/Account_Value`, `Acc_branch`, `CostCenter`, `IsDeleted` | CONFIRMED; ParentCode join semantics REQUIRES_CONFIRMATION |
| `Entry` | Journal header: `GlobalID` PK, number `id`, `doc_no`, `type`→`EntryTypes`, **`state` (state=1 = posted**, filter used by balance functions), `branch`, `EmpID`, `IsVAT` | CONFIRMED |
| `Entry_sub` | Lines: `EntryGlobalID` NOT NULL (no FK, **no PK**), `dept`(debit), `credit`, `acc_no`, `CCcode` (cost center), `branch`, `salesman`, `res_id` | CONFIRMED; `res_id` meaning UNKNOWN |
| `His_Entry(_sub)` | Snapshot/history copies of entries | CONFIRMED purpose INFERRED (audit of edited entries) |
| `EntryTypes` | Id/Name/Name_En lookup | CONFIRMED |
| `AccountingPeriods` + `PeriodLocks` | Fiscal periods with close metadata + **per-module locks** (`ModuleName`, `IsLocked`) | CONFIRMED (recently added feature) |
| `Cost_Center` + `costs` | Cost centers (hierarchy `ParentCode`), `costs` links expense names→account+costCenter | CONFIRMED |
| TVFs `AccountTotalBalance`, `AccountsTotalBalance` | Balances from posted (`state=1`), non-deleted entries ≤ date, branch-scoped | CONFIRMED |
| SP `GetAccountStatement` | Recursive account-tree statement with previous-balance row; filters dates/branch | CONFIRMED GL/account-statement logic |
| `Inv.Balance_previews`, `Year_Previews` | Carried balances from prior-year DATABASES | CONFIRMED |

---

## 3. Treasury, Cash & Cheques

| Object | Analysis | Badge |
|---|---|---|
| `Safes` | Cash safes per branch (`IS_Default`); **also joined as `store` (warehouse) in `ItemsExpirationStock`** — naming collision or shared lookup | CONFIRMED anomaly, REQUIRES_CONFIRMATION |
| `Banks` (+country/city/area/tel) | Bank definitions, `Acc_Code` COA link, `DisPre`(discount %?), `ChangeInPOS` | CONFIRMED |
| `treasury` | tiny lookup (id,name,type) — likely safe types | INFERRED |
| `Receipts` | **Unified voucher**: `GlobalID`, `ReceiptNo`, `ReceiptType` (in/out?), `PaymentType`, `ClientID`, `DebitAcc`/`CreditAcc` (COA codes), `Payment/VAT/NetVal`, cheque block (`CheckNo/CheckDate/Checkbank/CheckState`), `EntryGlobalID` link, `Cccode`, `Recipient`, `MobPOSID` | CONFIRMED structurally; enum maps REQUIRES_CONFIRMATION |
| `SandQ`, `SandD`, `SandQD`, `SandSD`, `SandVAT` | Older voucher family (سند قبض/صرف variants): date, cust/emp/acc, `val`, `safe_bank_id`, cheque block, `Cccode`; `SandVAT` adds Supplier+VAT split | CONFIRMED legacy; which are live REQUIRES_CONFIRMATION |
| `Currency_SafeBalance`, `Currency_Lastprice(_Sub)` | Multi-currency balances per safe; FX rate journal with buy/sell bands | CONFIRMED |
| `Rekaba(curr_id, chk, val)` | Currency denominations for cash counting (فئات العملة) | INFERRED |
| `CasherClosed(+_Sub)`, `CahierClosedAndroid`, `SettingCloseShift`, `CloseShiftCustomer` | **Cashier shift-close (Z-report)**: cash/network/returns/expenses/purchases/hosting/insurance totals, diff vs counted cash, html report, per-customer close amounts | CONFIRMED |

---

## 4. Inventory & Catalog

| Object | Analysis | Badge |
|---|---|---|
| `Items` (manual `id`, not identity) | names AR/EN, `code`, `barcode`, `Grpcode`, `group_id`→category, `unit`, `ShowInPOS`, `Wscale`(weight-scale item), `purch_price`/`sale_price`, `limit`(min qty), `MaxQtyLimit`, `tax`, `tax_group`, `ItemType`, `ItemProperty` (=8 → expiry-tracked in TVF), `FillValue`, WHT %, Egypt `EgyItemCode/EgyCodeType` (GS1/EGS), max discount %/amount, audit cols | CONFIRMED |
| `ItemsCategory` | hierarchy via `ParentCode`, POS flags, kitchen `printer`, `PrintAllItems/PrintItemsSeparately`, branch scoping, **both `Id` and `CategoryId` columns** (used inconsistently: joins use both!) | CONFIRMED anomaly |
| `units` | **No PK, no identity**; `Id` vs `UnitId` duplication | CONFIRMED anomaly |
| `ItemUnits` | per-item unit conversion (`perc`), per-unit purch/sale price + barcode | CONFIRMED |
| `Itembarcodes`, `ItemAlternativeCodes` | multi-barcode + alternative SKUs | CONFIRMED |
| `ItemPrices` | price history (purch/sale bands, competitor/consumer/wholesale) | CONFIRMED |
| `ItemsType`, `tax_groups`, `priceTypes` | lookups | CONFIRMED |
| `ItemComponents` | kit/BOM composition (`type bit`, `store`) | CONFIRMED |
| `ItemDetails` | import data: supplier ref, importer, net weight, lead time | CONFIRMED |
| `ItemSerialNo`, `InvoiceItemDetail` | serials & detailed line attributes: batch, production/expiry dates, H/W, color, size, `InvertoryImpact` (1=in/2=out per `funCalculateSerialNoSummary`) | CONFIRMED |
| `ItemBatchDeliveries` | batch deliveries to recipients (تسليم دفعات) w/ custody employee | CONFIRMED; target module REQUIRES_CONFIRMATION |
| `EquipMakes/EquipModels`, `ItemVehicleFitment` | **vehicle spare-parts fitment** (make/model/year) — only formal FKs in DB | CONFIRMED |
| `Glasses`, `Other_Column` | optics prescriptions (SPH/CYL/AX/ADD/IPD) + R1..R5/L1..L5 grid | CONFIRMED vertical; `Other_Column` semantics UNKNOWN |
| `CustomerMeasurements` | tailoring measurements per customer | CONFIRMED vertical |
| `ProductStocks` | denormalized stock cache `(ProductId, BranchId, InventoryId, UnitId, Quantity real, AvrgCost real)`; read by `CalcProductStocks` | CONFIRMED; who maintains it REQUIRES_CONFIRMATION (no trigger in export) |
| `SafesAdjust(_Sub)` | **stock-taking/adjustment** (itemized `InQuant/OutQuant/CurrentStock/RealStock`, approval flag) — misnamed "Safes" | CONFIRMED |
| `SafesTransfer(_Sub)` | **stock transfer** between stores w/ send/receive confirmations and avg cost — again item-based | CONFIRMED |
| Stock TVFs | `TotalItemStock*`, `Inventorybalance`, `ItemsStockByBranch`, `ItemStockLimits`, `ItemsExpirationStock`, `ItemBalanceWithAvrgCost` | CONFIRMED logic; see G9/BL-3 |
| SPs | `CalcItemStock(AllBranches)`, `ItemAvrgCost`, `InventoryCost(ForBalanceSheet)`, `InventoryCostByType` | CONFIRMED; weighted-average valuation incl. discount allocation formula |

---

## 5. Parties

| Object | Analysis | Badge |
|---|---|---|
| `Customers` | full party: geo (country/city/area + free-text city2/area2), `act`→`Acts`?, `national_id`, `tax_no`, `maxdepit` (credit limit), `type`, **`AccountCode` → COA link**, `IdScan` image, ZATCA address fields (Plot/Building/Street/District/Postal), `CrNo`, `ISCredit`, `Pricing` (price list) | CONFIRMED; `act` lookup INFERRED |
| `Suppliers` | minimal (name, taxNo) — clearly newer/parallel | CONFIRMED; purchase linkage REQUIRES_CONFIRMATION |
| `Owners` | marina vessel owners w/ % (`Marine.OwnerPercent`) | CONFIRMED |
| `PM_Contractor` | contractors + bank block (IBAN/SWIFT) | CONFIRMED |
| `VATClients` | name+taxNo mini-table | UNKNOWN usage |
| `DealPersons` | generic contacts | CONFIRMED |
| `salesmen` | sales reps + 3 commission types (`comm`, `Profit_Comm`, `Colle_Comm`) | CONFIRMED |

---

## 6. HR & Payroll

`Employees` (full profile + salary components incl. housing/food/travel/medical +
banking), `Managements/Departments/jobs/Marital_status/Religions/States/Countries/Cities/
Areas/Acts` lookups, `EmpBranches`, `EmpSalaryAddSub` (additions/deductions w/ SubFromSalary
+ EntryGlobalId + Cash/Bank), `Salary_Pay(_Sub)`, `SalaryPay`, `Salary_Res(_Details)`
(monthly runs w/ EntryGlobalID), `SalaryAddSubTypes`, `Attendance` (biometric raw punch),
`HR_InOutAct_Tbl` (device users; **password plaintext**), `SettingFingurePrint` (device
IP/port). Payroll CONFIRMED end-to-end w/ journal link; attendance is raw-capture only,
no shift rules evident (REQUIRES_CONFIRMATION).

---

## 7. Vertical & Aux Modules (must not be lost — rule §24)

| Module | Tables | Status |
|---|---|---|
| Restaurant/café | `Tables`, `Cat_Table`, `Table_Order`, `Additions`, `CategoryNotes`, `SettingOrderMethods`, `DailyOrderCounter`, `OrderNumbers`, kitchen printing in ItemsCategory/SettingPrint | Mapped → P19 |
| Marina rental | `Marine`, `GroupMarine`, `Booking`, `BookingAddition`, `RentInvoice`, `RentPeriod(Sub)`, `OperationPlan`, `OperPlanSub`, `MarineOperPeriod`, `Violation`, `Owners` | Mapped → P22 |
| Contracting/PM | `PM_Projects`, `PM_GroupStages`, `PM_ProjStages`, `PM_Stages`, `PM_Status`, `PM_Terms`, `PM_FileType`, `PM_Requirement(Sub)`, `PM_ContractInv(Sub)`, `PM_Contractor`, `InvContratct(_Sub)` | Mapped → P21 |
| Installments | `cont`, `cont_installments` | Mapped → P21 |
| Optics | `Glasses`, `Other_Column` | Mapped → P22 |
| Tailoring | `CustomerMeasurements` (+ `SettingGeneral.AtiveCustMeasur`) | Mapped → P22 |
| Spare parts | `EquipMakes`, `EquipModels`, `ItemVehicleFitment`, `ItemAlternativeCodes`, `ItemDetails` (+ `SettingGeneral.spareparts`) | Mapped → P22 (+P06 core) |
| ZATCA (KSA) | `SettingZatca`, `ZatcaCredential`, `CSRProperties`, `ZatcaEncodedInvoice`, `ZatcaResponse`, `Inv.{QRCode,InvoiceHash,UUID,ZatcaSent}` | Mapped → P13 |
| ETA (Egypt) | `EtaSetting`, `Items.EgyItemCode/EgyCodeType`, `Foundation.EgyEInvoice` | Adapter design P13 |
| Salla e-commerce | `SallaSettings`(OAuth), `Items_Salla_Sync`, `Salla_Export_Log`, `Branch_safes_salla`, `vw_Items_Salla_Status`, `CloudPriceSetting`, `CloudPaytypeSetting`, `CloudPayment` | Mapped → P22 |
| Sync/offline | `SettingSync`, `SyncEntities`, `Settingmqtt`, `SettingNotify`, `*.Sync`, `*.CloudID` | Mapped → P16 |
| Attachments | `Documents(FileName,FileUrl,GlobalId,type)` | Mapped → P04 files |
| Offers/promotions | `Offer`, `OfferItems`, `OfferForClient` | Mapped → P10 (basic) |
| Backup/ops | `BackupHistory`, `BackupSetting`, `DbVersions`, `Log4NetLog`, `PrinterSettings`, `Setting*` (print/barcode/scale/display/email/POS) | Platform equivalents; desktop-only settings noted |
| Licensing-ish | `Offer` PK named `PK_License`, `SettingNeoleap.license` | UNKNOWN — no license enforcement tables beyond flags |
| Junk | `[21346]`, `Table_2`, `Paste Errors`, `Switchboard Items(11)`, `intel(1)`, `try`, `sett`, `commentsTable`, `Cat_Table` (dup? no: table categories) | Archive-only |

---

## 8. `SettingGeneral` — the behavior switchboard (CONFIRMED columns)

Controls per invoice-type (`Inv_Id` = inv_type): VAT inclusion, `MainVAT %`, sell-below-zero
(`SaleByMinus`), `CostType`, default store/treasury/unit/customer, auto journal (`HasEntry`),
sync flags, COA mapping codes (`ItemsAcc`, `ReturnItemsAcc`, `DiscountAcc`, `InsureAcc`,
`DeliveryAcc`, `StoreAcc`, `VATCode`), linked/unlinked returns, `AdditionalTax %`,
decimal digits, order flow, payment status, invoice code prefix, salesman required,
cloud serials, cash-customer/mobile required, detailed item entry (serials/dimensions),
`Bypassinvposting`, `spareparts`, customer measurements, default pay type.

→ This table proves **posting rules are configuration-driven**; the new posting engine
must use posting profiles per document type (`ACCOUNTING_ARCHITECTURE.md` §6).

## 9. E-Invoice & Integration Security Observations

- `ZatcaCredential` stores CSR/private key/CSID/secret **plaintext** (CONFIRMED).
- `EtaSetting` stores ETA client secrets + token PIN plaintext (CONFIRMED).
- `SallaSettings` stores OAuth client secret + token refresh plaintext (CONFIRMED).
All → encrypted-at-rest secrets store in target. Migration moves values encrypted, never re-logs.

## 10. Index/constraints reality check

- No secondary indexes beyond PK/unique in export (CONFIRMED — none scripted).
- Composite unique: `Inv(branch,inv_type,proc_type,id)`, `OrderNumbers(OrderDate,OrderNo)`, `InvCounters` PK.
- Several tables without PK: `units`, `Entry_sub`, `Itembarcodes`, `ItemUnits`, `ItemPrices`,
  `sett`, `Paste Errors`, jazz tables. CONFIRMED.

---

## 11. Cross-Reference: where each legacy capability lands

See `DOMAIN_MODEL.md` §3 mapping matrix (table-by-table) and `REQUIRES_CONFIRMATION.md`
for the 30+ open questions (proc_type/inv_type enumeration, pay_type map, receipt enums,
Safes-as-store anomaly, Accounts_Index.ParentCode join, `res_id`, `purch_rest_id`,
val vs val1, live Sand* tables, ProductStocks maintainer, `Other_Column`, VATClients…).
