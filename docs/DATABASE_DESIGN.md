# DATABASE_DESIGN (PostgreSQL 16)

> **Level B — CANONICAL.** The schema contract every phase implements with Drizzle.
> Global conventions apply to ALL tables unless stated: `id uuid PK` (v7, app-generated),
> `tenant_id uuid NOT NULL REFERENCES tenants(id)` on every business table,
> `created_at/created_by/updated_at/updated_by` (audit), `deleted_at/deleted_by`
> (master data only), `version int NOT NULL DEFAULT 1` (optimistic concurrency),
> `legacy_source text`, `legacy_id text` (+ unique partial index
> `(tenant_id, legacy_source, entity_hint, legacy_id) WHERE legacy_id IS NOT NULL`
> on migratable tables). Money `numeric(20,4)`; qty `numeric(20,4)`; fx `numeric(20,10)`;
> timestamps `timestamptz`; business dates `date`. RLS enabled on every business table.

Legend for per-table notes: **UQ** unique (implicitly composite incl. `tenant_id`),
**IDX** secondary index, **SD** soft-deletable, b-rules = enforced invariants.

---

## 1. Platform (no tenant_id)

**tenants** — `code text UQ-global`, `name`, `status text CHECK(active,suspended,archived) DEF 'active'`,
`base_currency char(3) DEF 'SAR'`, `timezone DEF 'Asia/Riyadh'`, `locale DEF 'ar'`,
`country_code char(2) DEF 'SA'`, `meta jsonb DEF '{}'`.
**users** — `email citext UQ-global`, `phone`, `password_hash text`, `full_name`,
`status`, `is_platform_admin bool DEF false`, `mfa_secret_enc bytea NULL`, `last_login_at`.
**permissions** — `code text PK` (`module.entity.action`), `description`, `module`.
**refresh_tokens** — `user_id FK`, `token_hash text UQ`, `family uuid`, `expires_at`,
`revoked_at`, `replaced_by uuid NULL`, `ip inet`, `user_agent text`, `tenant_id uuid NULL`.
**migrations_log** — drizzle-kit internal.

## 2. Tenancy & Access

**memberships** — `tenant_id FK`, `user_id FK`, UQ(tenant,user), `display_name`,
`branch_scope jsonb NULL`(null=all), `status`, `is_owner bool DEF false`.
**roles** — `tenant_id FK`, `name`, UQ(tenant,name), `is_system bool`, `description`.
**role_permissions** — PK(role_id, permission_code) FKs.
**membership_roles** — PK(membership_id, role_id).
Rule: effective permission set = UNION(roles). Branch scope enforced by guard.

## 3. Tenant Settings & Sequences

**tenant_settings** — PK(tenant_id,key), `value jsonb`. Typed keys registry in code.
**document_sequences** — PK(tenant_id, branch_id, doc_type, fiscal_year_id NULL),
`prefix text DEF ''`, `current_value bigint DEF 0`, `padding int DEF 6`. Allocation:
`INSERT … ON CONFLICT DO UPDATE SET current_value = … RETURNING` under tx (BL-1).
**currencies** — PK(tenant_id, code), `name_ar/en`, `minor_units smallint DEF 2`,
`is_base bool`; one base per tenant (partial UQ).
**fx_rates** — id, `tenant_id`, `from_code`, `to_code`, `rate numeric(20,10)`UQ(tenant,pair,from_date),
`effective_from date`. IDX(tenant,pair,from).

## 4. Platform Services

**audit_log** — append-only: `tenant_id NULL` (platform events), `actor_user_id`,
`actor_label`, `action`, `entity`, `entity_id`, `before jsonb`, `after jsonb`,
`meta jsonb`(ip,ua,trace), `created_at`. IDX(tenant,entity,entity_id), IDX(tenant,created_at).
DB: revoke UPDATE/DELETE from api role.
**files** — `tenant_id`, `bucket`, `object_key`, `name`, `mime`, `size_bytes`,
`checksum`, `entity text NULL`, `entity_id uuid NULL`, `uploaded_by`. SD. (replaces `Documents`,
`Items.image`, `Foundation.Logo`, `Customers.IdScan` → object storage.)
**notifications** — `tenant_id`, `membership_id`, `type`, `payload jsonb`, `read_at NULL`.
**outbox_jobs** — `tenant_id`, `queue`, `type`, `payload jsonb`, `status`, `attempts DEF 0`,
`run_at`, `processed_at NULL`. (Feeds BullMQ; einvoice/notifications/exports.)
**idempotency_keys** — PK(tenant_id, key), `endpoint`, `response`, `created_at`, expires 24h.

> PHASE_04 implementation notes (additive; no frozen column was removed):
> - `files` also carries `status ('pending'|'ready'|'deleted')` — a presigned row exists
>   before the bytes do, so "reserved" and "usable" must be distinguishable — plus the
>   `baseAuditColumns` used by every other table.
> - `outbox_jobs` also carries `last_error text` (the dead-letter reason) and
>   `updated_at`; `status` is `pending|published|dead`.
> - `idempotency_keys.response` is **text, not jsonb**: `API_CONTRACT §0` promises a
>   byte-identical replay and jsonb normalises key order. It also carries
>   `request_hash` (same key + different payload → 409 instead of a wrong replay),
>   `status_code`, `completed_at` and `expires_at`.
> - `document_sequences` uses a surrogate `id` plus a unique index on
>   `(tenant_id, coalesce(branch_id, nil-uuid), doc_type, coalesce(fiscal_year_id, nil-uuid))`,
>   because a PK containing NULLs cannot enforce "one row per scope" in PostgreSQL.
> - All six tables carry `ENABLE`+`FORCE` RLS. `audit_log` uses a variant policy
>   (`WITH CHECK (tenant_id IS NOT DISTINCT FROM <guc>)`) so platform-plane rows with a
>   NULL tenant can be written but never read back through a tenant session.

## 5. Organization

**company_profiles** — PK(tenant_id) FK. `name_ar`, `name_en`, `tax_no`, `cr_no`,
`logo_file_id FK files NULL`, `address jsonb` (national-address fields: plot, building,
street, add_street, district, postal — from `Foundation`), `phones jsonb`, `email`,
`einvoice_flags jsonb` (`zatca bool`, `eta bool`). ← `Foundation`. SD no.
**branches** — `tenant_id`, `code` UQ(tenant,code), `name_ar/en`, `address jsonb`,
`phone/mobile/email`, `is_default bool`, `is_active bool DEF true`, SD. ← `Branches`.
**branch_posting_profiles** — PK(branch_id, doc_type): account mapping JSONB
(sales/purchases/discount/vat/inventory/cash/bank accounts + cost_center) ← legacy
`SettingGeneral.*Acc` + `Branches.*Acc` merged. b-rule: accounts must exist & be postable.
**warehouses** — `tenant_id`, `branch_id FK`, `code UQ(tenant,code)`, `name`,
`inventory_account_id FK NULL`, `is_default`, `is_active`, SD. ← `Stocks`.
**cash_locations** — id, `tenant_id`, `branch_id FK`, `kind text CHECK(safe,bank)`,
`name`, `account_id FK accounts NOT NULL`, `currency_code NULL`(null=base), `is_default`,
`bank jsonb NULL`(bank_name, iban, swift, account_no), `change_in_pos bool`, `is_active`, SD.
← `Safes` + `Banks` + `treasury` unified.
**cash_location_balances** — PK(cash_location_id, currency_code), `balance numeric(20,4) DEF 0`
(denormalized; truth = journal + vouchers; reconciled by report) ← `Currency_SafeBalance`.
**price_lists** — `tenant_id`, `name`, `currency_code`, `is_default`, SD. ← `priceTypes`/`Pricing`.

## 6. Catalog

**item_categories** — `tenant_id`, `parent_id FK NULL`, `code UQ(tenant,code)`, `name_ar`, `name_en`,
`sort_order int DEF 0`, `show_in_pos/sale/purch bools`, `kitchen_printer text NULL`,
`print_separately bool`, `branch_id FK NULL`(null=all), SD. ← `ItemsCategory` (single `id`).
**units_of_measure** — `tenant_id`, `code UQ(tenant,code)`, `name_ar/en`. (fixed legacy `units` anomaly)
**tax_groups** — `tenant_id`, `name_ar/en`, `rate numeric(7,4)`, `vat_account_id FK`,
`is_inclusive_default bool`, SD. ← `tax_groups`.
**items** — `tenant_id`, `sku UQ(tenant,sku)`, `barcode NULL` (IDX), `name_ar`, `name_en NULL`,
`category_id FK`, `base_unit_id FK`, `kind text CHECK(stock,service,composite) DEF 'stock'`,
`sale_price`, `purchase_price`, `tax_group_id FK`, `min_qty DEF 0`, `max_qty NULL`,
`max_discount_pct/amt NULL`, `track_lot bool DEF false`(← ItemProperty=8), `track_serial bool`,
`weighted_scale bool`(← Wscale), `show_in_pos bool DEF true`, `egy_item_code/egy_code_type NULL`,
`withholding_rate NULL`, `image_file_id NULL`, `kind_flags jsonb`(sparepart etc.), SD,
IDX(tenant,category), FTS index on names. ← `Items`.
**item_units** — PK(item_id, unit_id): `ratio numeric(20,6)`(← perc/UnitEquality), `barcode UQ NULL`,
`sale_price/purchase_price NULL`, `is_default_purchase/sale`. ← `ItemUnits`+`Itembarcodes` primary.
**item_barcodes** — PK(tenant, barcode) → item_id (+unit NULL). ← `Itembarcodes` rest.
**item_alternative_codes** — `item_id FK`, `code UQ(tenant,code)`, `notes`, SD. ← `ItemAlternativeCodes`.
**item_components** — PK(item_id, component_item_id): `qty`, `unit_id`, `kind text`. ← `ItemComponents`(BOM/kits).
**item_price_history** — append-only: `item_id`, `unit_id NULL`, `prices jsonb`(purch,sale,bands,
wholesale,consumer,competitor), `recorded_by`, `recorded_at`. ← `ItemPrices`.
**item_details** — 1:1 items: `supplier_ref`, `importer`, `net_weight`, `lead_time`, `notes`, SD. ← `ItemDetails`.

## 7. Accounting Core (details + rationale in ACCOUNTING_ARCHITECTURE.md)

**accounts** — `tenant_id`, `code text UQ(tenant,code)`, `name_ar`, `name_en NULL`,
`parent_id FK NULL`, `level int`, `path ltree`(IDX gist), `type text CHECK(asset,liability,
equity,revenue,expense)`, `subtype text NULL`(cash,bank,receivable,payable,inventory,vat_output,
vat_input,sales,purchase_discount…), `normal_balance text CHECK(debit,credit)`, `is_postable bool DEF true`,
`allow_manual bool DEF true`, `branch_id NULL`(null=shared), `currency_code NULL`, SD.
← `Accounts_Index` (no cached totals — balances are computed). UQ path integrity trigger.
**fiscal_years** — `tenant_id`, `name`, `start_date`, `end_date`, `status CHECK(open,closed)`,
UQ non-overlapping (exclusion constraint on daterange).
**fiscal_periods** — `fiscal_year_id FK`, `name`, `start/end`, `status`, `closed_by/at`, SD no.
← `AccountingPeriods`. **period_module_locks** — PK(period_id, module), `locked bool DEF false`,
`locked_by/at`. ← `PeriodLocks`.
**journal_entries** — `tenant_id`, `branch_id FK`, `fiscal_period_id FK NOT NULL`, `date date NOT NULL`,
`number text`(sequence `journal_entry`), `kind text CHECK(manual,auto,reversal)`, `status text
CHECK(draft,posted,void) DEF 'draft'`, `description`, `source_type text NULL`(+`source_id uuid NULL`,
IDX), `reversal_of uuid NULL`, `idempotency_key NULL`, `posted_at/by NULL`, `created…`.
b-rules: posted ⇒ Σlines.debit=Σlines.credit>0; posted rows immutable (trigger);
`fiscal_period.status='open'` + module unlocked at post time; one reversal per entry.
← `Entry` (state=1⇒posted). `His_Entry*` obsolete (audit_log covers).
**journal_entry_lines** — `entry_id FK ON DELETE RESTRICT`, `line_no int`, `account_id FK NOT NULL`,
`debit numeric(20,4) DEF 0 CHECK >=0`, `credit … CHECK >=0`, CHECK (debit>0) XOR (credit>0),
`currency_code NULL + currency_amount NULL + fx_rate NULL`, `cost_center_id NULL`,
`party_id NULL`(subledger tag), `branch_id NULL`, `description`. IDX(account_id),IDX(party_id).
PK(entry_id,line_no). ← `Entry_sub` (res_id→meta).
**cost_centers** — `tenant_id`, `code UQ(tenant,code)`, `name_ar/en`, `parent_id NULL`,
`branch_id NULL`, SD. ← `Cost_Center`; expense labels from `costs` fold into accounts/CC.
**opening_balances** — `tenant_id`, `fiscal_year_id`, `account_id`, `party_id NULL`,
`item_id NULL`, `warehouse_id NULL`, `debit/credit`, `qty NULL`, `unit_cost NULL`, `note`;
imported as draft batch then posted into first open period (single balancing journal).
Unique(tenant,year,account,party,item,warehouse).

## 8. Parties & AR/AP

**parties** — `tenant_id`, `kind text CHECK(customer,supplier)` (+ `is_owner/is_contractor
bool` vertical flags), `code UQ(tenant,code)`(sequence), `name_ar`, `name_en NULL`,
`tax_no NULL`, `national_id NULL`, `cr_no NULL`, `phone/mobile/email`, `address jsonb`
(ZATCA national address), `country/city/area text NULL`, `activity text NULL`,
`credit_limit numeric NULL`(← maxdepit), `is_credit bool`, `price_list_id NULL`,
`account_id FK accounts NULL`(party sub-account, ← AccountCode), `bank jsonb NULL`
(contractor block), `is_active`, SD. IDX(name trigram), IDX(tax_no).
← `Customers`+`Suppliers`+`Owners`+`PM_Contractor`+`VATClients(flags)`.
**party_contacts** — `party_id FK`, `name`, `job`, `tel`, `mobile`, `notes`, SD. ← `DealPersons`.
**payment_allocations** — `tenant_id`, `voucher_id FK NULL`, `invoice_kind text`, `invoice_id uuid`,
`amount numeric(20,4)>0`, `allocated_by/at`. IDX(invoice), IDX(voucher). b-rule: Σ allocations
≤ voucher net and ≤ invoice balance (service-enforced). (New — fixes RC-09.)

## 9. Inventory (engine details in DOMAIN_MODEL §6)

**inventory_transactions** — APPEND-ONLY: `tenant_id`, `branch_id`, `warehouse_id`,
`item_id`, `doc_type text`, `doc_id uuid`, `doc_line_id uuid NULL`, `qty_base numeric(20,4)`
(signed = direction; unit = item base unit), `unit_cost numeric(20,4) NOT NULL`,
`lot_id NULL`, `serial_ids uuid[] NULL`, `occurred_at`, `created_by/at`. IDX(item,warehouse,occurred_at).
← replaces all on-the-fly stock TVFs (BL-3). No UPDATE/DELETE.
**stock_balances** — PK(warehouse_id, item_id): `qty numeric(20,4) DEF 0`, `avg_cost DEF 0`,
`value numeric DEF 0`(maintained in same tx ← `ProductStocks` done right).
**stock_adjustments / _lines** — header(branch, warehouse, date, status draft/approved/void,
`approved_by`, journal link) + lines(item, unit, counted_qty, system_qty, diff_qty IN/OUT,
unit_cost, note). ← `SafesAdjust(_Sub)` corrected.
**stock_transfers / _lines** — from/to warehouse+branch, status sent/received/void,
lines(item, qty, unit, received_qty, avg_cost, lot), confirmations. ← `SafesTransfer(_Sub)`.
**item_lots** — PK(item_id, batch_no): `expiry_date`, `production_date`, SD.
**item_serials** — UQ(tenant,item,serial): `lot_id NULL`, `status CHECK(in,reserved,sold,returned)`,
`last_doc_type/id`. ← `ItemSerialNo`+`InvoiceItemDetail` serial part.
**invoice_item_attributes** — per invoice line detailed entries: dims (H/W), color, size,
fill value/ratio, batch/expiry link, serial link. ← `InvoiceItemDetail` normalized.

## 10. Sales

**sales_invoices** — `tenant_id`, `branch_id`, `warehouse_id`, `kind text CHECK(sale,sale_return,
credit_note,debit_note)`, `number text UQ`(sequence), `date`, `due_date NULL`, `party_id NULL`
(null=cash walk-in), `cash_customer jsonb NULL`(name,mobile), `price_list_id NULL`,
`sales_emp_id NULL`, `salesman_id NULL`, `currency_code`, `fx_rate DEF 1`,
`price_includes_vat bool`, `status CHECK(draft,posted,void)`, `payment_status CHECK(unpaid,
partial,paid)`, `pay_method text NULL`(cash,card,bank,credit,split), `amounts jsonb`**→ typed
columns**: `subtotal`, `items_discount`, `invoice_discount`, `additions_total`, `insurance`,
`additional_costs`, `tax_total`, `extra_tax_total`, `withholding_total`, `total`,
`paid_amount DEF 0`, `balance numeric`, `cost_total`, `profit numeric` (snapshots like legacy),
`reference_no/date NULL`(returns link + original_invoice_id NULL), `notes`,
`einvoice_status text NULL`, `einvoice_uuid/hash/qr NULL`, `fiscal_period_id`,
`journal_entry_id FK NULL`, `posted_at/by`, `idempotency_key UQ NULL`, `order_type NULL,
table_no NULL`(POS pack), `combined_into uuid NULL`, `version`, SD=no (void instead).
IDX(date), IDX(party), IDX(status,date). ← `Inv` (sales-side slices).
**sales_invoice_lines** — PK(invoice_id,line_no): `item_id`, `description NULL`, `unit_id`,
`qty`, `base_qty`, `unit_price`, `discount_amount`, `tax_rate`, `tax_amount`, `additions_amount`,
`withholding_rate/amount`, `extra_tax_rate/amount`, `unit_cost_at_post NULL`, `line_total`,
`cost_center_id NULL`. ← `Inv_Sub` (sales).
**invoice_payments** — `invoice_kind`, `invoice_id`, `method`, `cash_location_id FK NULL`,
`amount`, `paid_at`, `reference NULL`, `voucher_id NULL`. ← `InvoicePayments` (+tender splits).
**sales_adjustment_notes** — credit/debit notes: party, linked invoice, discount/net split,
status, journal link. ← `CreditDeptNotes`.
**offers / offer_items / offer_parties** — promotions (targets, %/value, validity, account
for posting). ← `Offer*` condensed.

## 11. Purchases

**purchase_invoices / purchase_invoice_lines** — mirror sales kinds `purchase,purchase_return`;
supplier `party_id NOT NULL`; `landed_cost_alloc text CHECK(qty,value) NULL`.
**purchase_invoice_costs** — `invoice_id`, `cost_name`, `amount`, `cost_center_id`,
`account_id`, allocation target. ← `InvoiceCost` (drives `unit_cost` into avg pool, BL-3/BL-6).

## 12. Treasury

**vouchers** — unified: `tenant_id`, `branch_id`, `kind CHECK(receipt,payment)`, `subtype text`
(customer,supplier,expense,account,salary,vat,other), `number UQ`(seq), `date`, `party_id NULL`,
`counter_account_id FK NULL`, `cash_location_id FK`, `method CHECK(cash,cheque,bank_transfer,card)`,
`amount`, `vat_amount DEF 0`, `net_amount`, cheque block (`cheque_no/date/bank_name`,
`cheque_state CHECK(pending,cleared,bounced,collected) NULL`), `cost_center_id`,
`reference_no/date`, `recipient text`(← Recipient), `status draft/posted/void`,
`journal_entry_id FK`, `fiscal_period_id`, posted_at/by, idempotency UQ. ← `Receipts`+`Sand*`
families unified (RC-19 mapping table in MIGRATION doc).
**expense_types** — `tenant_id`, `name_ar/en`, `account_id`, `cost_center_id NULL`, SD. ← `costs`.
**cash_transfers** — from/to `cash_location`, `amount`, `currency`, status sent/received,
journal links (in/out). (New; legacy emulated via vouchers.)
**shift_closes / shift_close_lines** — cashier Z-close: user, period, expected/counted cash,
diff, per-method totals(cash, network, returns, postpones, insurance, expenses, purchases,
hosting, extras), per-party deferred totals, `report_html NULL`, status. ← `CasherClosed*`+
`CahierClosedAndroid`+`CloseShiftCustomer`+`Check_Close`.
**cash_count_lines** — `shift_close_id`, `currency_code`, `denomination numeric`, `count int`,
`total`. ← `Rekaba`.

## 13. E-Invoicing

**einvoice_credentials** — 1 per (tenant, authority): `authority CHECK(zatca,eta)`,
`environment CHECK(simulation,production)`, `csr text`, `private_key_enc bytea`,
`csid`, `secret_enc bytea`, `request_ids jsonb`, `org jsonb`(CSRProperties fields),
`valid_from/to`, `is_active`. Secrets encrypted at rest (app-layer AES-GCM).
**einvoice_submissions** — `invoice_id`, `authority`, `action CHECK(sign,submit,clear,report)`,
`status`, `uuid`, `hash`, `qr_payload`, `request_payload jsonb`, `response jsonb`,
`error text NULL`, `attempts`, `submitted_at/by`. IDX(invoice). ← ZatcaResponse/Encoded/ETA.

## 14. Migration (engine support, apps/migrator writes here)

**migration_runs** — `tenant_id`, `source_label`(`sqlserver:Data16`…), `mode CHECK(dry_run,import)`,
`status`, `started_by/at`, `finished_at`, `summary jsonb`.
**legacy_id_mappings** — UQ(tenant, entity, legacy_source, legacy_pk): `new_id uuid`, `run_id`.
**migration_issues** — `run_id`, `entity`, `legacy_pk`, `severity`, `code`, `message`, `payload jsonb`.
(Detailed contract in MIGRATION_ARCHITECTURE.md §6.)

## 15. Vertical Packs (owned by later phases; shapes frozen here)

**HR (P20)**: `departments`, `jobs`, `employees`(profile+salary components jsonb+bank),
`attendance_logs`(raw punches ← Attendance), `salary_adjustments`(← EmpSalaryAddSub),
`payroll_runs / payroll_run_lines`(← Salary_Res/SalaryPay), journals linked.
**Installments (P21)**: `installment_contracts`(party, item/stock, total, down, count,
period unit, first_date, status, ← `cont`), `installment_schedule`(num, due_date, amount,
paid, voucher_id ← `cont_installments`).
**Projects/Contracting (P21)**: `projects`(party, contractor_party, location, terms links,
dates, status ← PM_Projects), `project_stage_templates`, `project_stages`(order,
accreditation), `boq_terms`(← PM_Terms), `progress_bills / _lines`(retention
`work_guarantee`, previously_paid, remaining — from InvContratct), `project_requirements`.
**Restaurant POS (P19)**: `dining_tables`(cat, status, current invoice ← Tables),
`table_categories`, `order_items_events`(← Table_Order), configs in tenant_settings
(SettingOrderMethods/PayMethods/Print…).
**Niche (P22)**: `optical_prescriptions`(← Glasses+Other_Column jsonb), `customer_measurements`,
`vessels`+`vessel_groups`+`bookings`+`rental_invoices`+`violations`+`vessel_owners`(
party link, percent) ← Marine family, `vehicle_makes/models`, `item_vehicle_fitment`,
`salla_connections/items_sync/export_log`(OAuth tokens enc ← Salla*).

## 16. Denormalization & Reports

No balance caches on master rows (drop legacy `Total_Debts/…`). Reports compute from
ledger/journals; heavy ones may use **materialized views** refreshed on demand:
`mv_account_balances`, `mv_item_stock`, `mv_party_balances` (P14, per-tenant filtered).
Strategy documented, created only if profiling proves need (ADR-007 placeholder).

## 17. Default Seeds (per new tenant)

COA template (AR/AP/cash/bank/stock/sales/purchases/VAT in-out/discounts), 3 roles
(owner/accountant/cashier), SAR+USD currencies, Pcs unit, 15%/0%/exempt tax groups,
payment methods, posting profiles, main branch+warehouse+safe. Seed lists live in
`packages/config/seeds/*.ts` (P03/P05/P07).
