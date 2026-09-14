import { sql } from 'drizzle-orm';
import { boolean, date, index, integer, jsonb, numeric, pgTable, primaryKey, text, timestamp, uniqueIndex, uuid } from 'drizzle-orm/pg-core';

import { baseAuditColumns, baseLegacyColumns, baseSoftDeleteColumns } from '../columns.js';

import { items } from './catalog.js';
import { branches, warehouses } from './organization.js';
import { parties } from './parties.js';
import { salesInvoiceLines, salesInvoices } from './sales.js';
import { vouchers } from './treasury.js';
import { tenants } from './platform.js';

const money = { precision: 20, scale: 4, mode: 'string' as const };
const pct = { precision: 7, scale: 4, mode: 'string' as const };

export const opticalPrescriptions = pgTable('optical_prescriptions', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), partyId: uuid('party_id').notNull().references(() => parties.id), invoiceLineId: uuid('invoice_line_id').references(() => salesInvoiceLines.id, { onDelete: 'set null' }), orientation: text('orientation').notNull().default('distance'), rightEye: jsonb('right_eye').$type<{ sph?: string; cyl?: string; axis?: string; add?: string; ipd?: string }>().notNull().default({}), leftEye: jsonb('left_eye').$type<{ sph?: string; cyl?: string; axis?: string; add?: string; ipd?: string }>().notNull().default({}), otherGrid: jsonb('other_grid').$type<Record<string, string>>().notNull().default({}), notes: text('notes'), ...baseAuditColumns(), ...baseSoftDeleteColumns(), ...baseLegacyColumns(),
}, (t) => ({ party: index('optical_prescriptions_party_idx').on(t.tenantId, t.partyId), line: index('optical_prescriptions_line_idx').on(t.tenantId, t.invoiceLineId) }));

/**
 * 📏 القياس — `CustomerMeasurements(MeasurementID, Cust_ID, MeasurementName,
 * MeasurementDate, Notes, IsActive)` (`Form_WPF/frmMeasurements.xaml` «إدارة قياسات
 * العملاء» + `frmMeasurementDetails.xaml` «📏 بيانات القياس»).
 *
 * `measurements` keeps its free-form keys: `frmCustomers.xaml` L1184 «📐 المقاسات»
 * stores fixed columns of its own (الطول · كتف · الرقبة · وسع اليد · وسع الخطوة · رقم
 * الصفحة) on the same document, and values written through the 📏 خصائص are keyed by
 * attribute **id** — so renaming a خاصية never orphans a number already taken.
 */
export const customerMeasurements = pgTable('customer_measurements', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), partyId: uuid('party_id').notNull().references(() => parties.id), kind: text('kind').notNull().default('tailoring'),
  /** 👤 اسم صاحب القياس — `MeasurementName`; «قياس بتاريخ …» stands in when it is null. */
  name: text('name'),
  /** 📅 التاريخ — `MeasurementDate`. */
  measurementDate: date('measurement_date'),
  measurements: jsonb('measurements').$type<Record<string, string>>().notNull().default({}), notes: text('notes'), active: boolean('active').notNull().default(true), ...baseAuditColumns(), ...baseSoftDeleteColumns(), ...baseLegacyColumns(),
}, (t) => ({ party: index('customer_measurements_party_idx').on(t.tenantId, t.partyId, t.createdAt), date: index('customer_measurements_tenant_date_idx').on(t.tenantId, t.measurementDate, t.createdAt) }));

/** 📏 خصائص القياسات — `MeasurementAttributes(AttributeID, AttributeName, DisplayOrder, IsActive)`. */
export const tailoringMeasurementAttributes = pgTable('tailoring_measurement_attributes', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), nameAr: text('name_ar').notNull(), displayOrder: integer('display_order').notNull().default(0), active: boolean('active').notNull().default(true), ...baseAuditColumns(), ...baseSoftDeleteColumns(),
}, (t) => ({ name: uniqueIndex('tailoring_measurement_attributes_tenant_name_key').on(t.tenantId, t.nameAr).where(sql`deleted_at IS NULL`), order: index('tailoring_measurement_attributes_tenant_idx').on(t.tenantId, t.displayOrder) }));

/**
 * 🧵 طلب التفصيل — `TailoringOrders` / `vw_OrdersComplete`
 * (`Form_WPF/frmOrders.xaml` «إدارة طلبات التفصيل» + `frmOrderDetails.xaml` «إضافة طلب تفصيل»).
 * `remainingAmount` is *not* stored: ⌛ المتبقي is 💰 السعر − 💵 المدفوع, as
 * `frmOrderDetails.CalculateRemaining` L290 prints it.
 */
export const tailoringOrders = pgTable('tailoring_orders', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), number: text('number').notNull(), partyId: uuid('party_id').notNull().references(() => parties.id, { onDelete: 'restrict' }), measurementId: uuid('measurement_id').references(() => customerMeasurements.id, { onDelete: 'set null' }), typeId: uuid('type_id').notNull().references(() => tailoringTypes.id, { onDelete: 'restrict' }), statusId: uuid('status_id').notNull().references(() => tailoringOrderStatuses.id, { onDelete: 'restrict' }), orderDate: date('order_date').notNull(), deliveryDate: date('delivery_date'), quantity: numeric('quantity', money).notNull().default('1'), price: numeric('price', money).notNull().default('0'), paidAmount: numeric('paid_amount', money).notNull().default('0'), fabricType: text('fabric_type'), fabricColor: text('fabric_color'), designNotes: text('design_notes'), generalNotes: text('general_notes'), ...baseAuditColumns(), ...baseSoftDeleteColumns(),
}, (t) => ({ number: uniqueIndex('tailoring_orders_tenant_number_key').on(t.tenantId, t.number).where(sql`deleted_at IS NULL`), date: index('tailoring_orders_tenant_date_idx').on(t.tenantId, t.orderDate), party: index('tailoring_orders_tenant_party_idx').on(t.tenantId, t.partyId), status: index('tailoring_orders_tenant_status_idx').on(t.tenantId, t.statusId) }));

/** 🔧 الخيارات المختارة للطلب — `OrderOptions(OrderID, CategoryID, ValueID)`. */
export const tailoringOrderOptions = pgTable('tailoring_order_options', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), orderId: uuid('order_id').notNull().references(() => tailoringOrders.id, { onDelete: 'cascade' }), categoryId: uuid('category_id').notNull().references(() => tailoringOptionCategories.id, { onDelete: 'cascade' }), valueId: uuid('value_id').notNull().references(() => tailoringOptionValues.id, { onDelete: 'restrict' }), ...baseAuditColumns(),
}, (t) => ({ key: uniqueIndex('tailoring_order_options_tenant_key').on(t.tenantId, t.orderId, t.categoryId) }));

/** ⚙️ الحالة — `OrderStatus(StatusID, StatusName, IsActive, DisplayOrder)`. */
export const tailoringOrderStatuses = pgTable('tailoring_order_statuses', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), code: text('code').notNull(), nameAr: text('name_ar').notNull(), displayOrder: integer('display_order').notNull().default(0), isFinal: boolean('is_final').notNull().default(false), active: boolean('active').notNull().default(true), ...baseAuditColumns(), ...baseSoftDeleteColumns(),
}, (t) => ({ code: uniqueIndex('tailoring_order_statuses_tenant_code_key').on(t.tenantId, t.code).where(sql`deleted_at IS NULL`), order: index('tailoring_order_statuses_tenant_idx').on(t.tenantId, t.displayOrder) }));

/** 🧵 نوع التفصيل — `TailoringTypes(TypeID, TypeName, DefaultPrice, IsActive)`. */
export const tailoringTypes = pgTable('tailoring_types', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), code: text('code'), nameAr: text('name_ar').notNull(), defaultPrice: numeric('default_price', money).notNull().default('0'), active: boolean('active').notNull().default(true), ...baseAuditColumns(), ...baseSoftDeleteColumns(),
}, (t) => ({ name: uniqueIndex('tailoring_types_tenant_name_key').on(t.tenantId, t.nameAr).where(sql`deleted_at IS NULL`), code: uniqueIndex('tailoring_types_tenant_code_key').on(t.tenantId, t.code).where(sql`deleted_at IS NULL AND code IS NOT NULL`) }));

/** 🔧 الخيارات — `OptionCategories(CategoryID, CategoryName, IsActive, DisplayOrder)`. */
export const tailoringOptionCategories = pgTable('tailoring_option_categories', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), nameAr: text('name_ar').notNull(), displayOrder: integer('display_order').notNull().default(0), active: boolean('active').notNull().default(true), ...baseAuditColumns(), ...baseSoftDeleteColumns(),
}, (t) => ({ name: uniqueIndex('tailoring_option_categories_tenant_name_key').on(t.tenantId, t.nameAr).where(sql`deleted_at IS NULL`), order: index('tailoring_option_categories_tenant_idx').on(t.tenantId, t.displayOrder) }));

/** 🔧 الخيارات — `OptionValues(ValueID, CategoryID, ValueName, IsDefault, IsActive, DisplayOrder)`. */
export const tailoringOptionValues = pgTable('tailoring_option_values', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), categoryId: uuid('category_id').notNull().references(() => tailoringOptionCategories.id, { onDelete: 'cascade' }), nameAr: text('name_ar').notNull(), isDefault: boolean('is_default').notNull().default(false), displayOrder: integer('display_order').notNull().default(0), active: boolean('active').notNull().default(true), ...baseAuditColumns(), ...baseSoftDeleteColumns(),
}, (t) => ({ name: uniqueIndex('tailoring_option_values_tenant_name_key').on(t.tenantId, t.categoryId, t.nameAr).where(sql`deleted_at IS NULL`), category: index('tailoring_option_values_category_idx').on(t.tenantId, t.categoryId, t.displayOrder) }));

/**
 * 🧾 فاتورة التفصيل — `Inv_Tailor` / `Inv_Sub_Tailor`
 * (`Form_WPF/frmViewOrders.xaml` «عرض الطلبات - ViewOrders» + `AddNewSizes.xaml`
 * «إضافة مقاس جديد»).
 *
 * ⌛ الباقي و💰 الإجمالي are *not* stored: `frmViewOrders` shows
 * `الإجمالي × 1.05` and `الباقي = (الإجمالي × 1.05) − المدفوع` (L88–L90).
 */
export const tailoringInvoices = pgTable('tailoring_invoices', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), number: text('number').notNull(), partyId: uuid('party_id').references(() => parties.id, { onDelete: 'restrict' }), customerName: text('customer_name').notNull(), phone: text('phone'), invoiceDate: date('invoice_date').notNull(), quantity: numeric('quantity', money).notNull().default('1'), unitPrice: numeric('unit_price', money).notNull().default('0'), total: numeric('total', money).notNull().default('0'), paidAmount: numeric('paid_amount', money).notNull().default('0'), statusId: uuid('status_id').notNull().references(() => tailoringOrderStatuses.id, { onDelete: 'restrict' }), garmentTypeId: uuid('garment_type_id').references(() => tailoringGarmentTypes.id, { onDelete: 'set null' }), billed: boolean('billed').notNull().default(false), measurements: jsonb('measurements').$type<Record<string, string>>().notNull().default({}), notes: text('notes'), ...baseAuditColumns(), ...baseSoftDeleteColumns(),
}, (t) => ({ number: uniqueIndex('tailoring_invoices_tenant_number_key').on(t.tenantId, t.number).where(sql`deleted_at IS NULL`), date: index('tailoring_invoices_tenant_date_idx').on(t.tenantId, t.invoiceDate), party: index('tailoring_invoices_tenant_party_idx').on(t.tenantId, t.partyId), name: index('tailoring_invoices_tenant_name_idx').on(t.tenantId, t.customerName), status: index('tailoring_invoices_tenant_status_idx').on(t.tenantId, t.statusId) }));

/** 💵 إستلام دفعة — the link between a فاتورة تفصيل and the سند قبض that paid it. */
/** A payment line is written once and never edited — so, as with `invoice_payments`, it carries no version. */
export const tailoringInvoicePayments = pgTable('tailoring_invoice_payments', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), invoiceId: uuid('invoice_id').notNull().references(() => tailoringInvoices.id, { onDelete: 'cascade' }), voucherId: uuid('voucher_id').references(() => vouchers.id, { onDelete: 'set null' }), amount: numeric('amount', money).notNull(), paidAt: date('paid_at').notNull(), note: text('note'), createdAt: timestamp('created_at', { withTimezone: true }).notNull().defaultNow(), createdBy: uuid('created_by'),
}, (t) => ({ invoice: index('tailoring_invoice_payments_invoice_idx').on(t.tenantId, t.invoiceId, t.paidAt) }));

/** 👔 نوع الثوب — `typeCB` in `AddNewSizes.xaml` L423 (سعودي · بحريني · اماراتي · كويتي). */
export const tailoringGarmentTypes = pgTable('tailoring_garment_types', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), code: text('code').notNull(), nameAr: text('name_ar').notNull(), displayOrder: integer('display_order').notNull().default(0), active: boolean('active').notNull().default(true), ...baseAuditColumns(), ...baseSoftDeleteColumns(),
}, (t) => ({ code: uniqueIndex('tailoring_garment_types_tenant_code_key').on(t.tenantId, t.code).where(sql`deleted_at IS NULL`), order: index('tailoring_garment_types_tenant_idx').on(t.tenantId, t.displayOrder) }));

export const vesselGroups = pgTable('vessel_groups', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), name: text('name').notNull(), code: text('code'), ...baseAuditColumns(), ...baseSoftDeleteColumns(), ...baseLegacyColumns(),
}, (t) => ({ code: uniqueIndex('vessel_groups_code_key').on(t.tenantId, t.code).where(sql`code IS NOT NULL AND deleted_at IS NULL`) }));
export const vessels = pgTable('vessels', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), groupId: uuid('group_id').references(() => vesselGroups.id, { onDelete: 'set null' }), code: text('code').notNull(), name: text('name').notNull(), status: text('status').notNull().default('available'), capacity: integer('capacity').notNull().default(0), metadata: jsonb('metadata').$type<Record<string, unknown>>().notNull().default({}), ...baseAuditColumns(), ...baseSoftDeleteColumns(), ...baseLegacyColumns(),
}, (t) => ({ code: uniqueIndex('vessels_code_key').on(t.tenantId, t.code).where(sql`deleted_at IS NULL`), group: index('vessels_group_idx').on(t.tenantId, t.groupId) }));
export const vesselGroupPricing = pgTable('vessel_group_pricing', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), groupId: uuid('group_id').notNull().references(() => vesselGroups.id, { onDelete: 'cascade' }), periodKind: text('period_kind').notNull(), price: numeric('price', money).notNull(), currency: text('currency').notNull().default('SAR'), ...baseAuditColumns(),
}, (t) => ({ kind: uniqueIndex('vessel_group_pricing_kind_key').on(t.tenantId, t.groupId, t.periodKind) }));
export const vesselOwners = pgTable('vessel_owners', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), vesselId: uuid('vessel_id').notNull().references(() => vessels.id, { onDelete: 'cascade' }), partyId: uuid('party_id').notNull().references(() => parties.id), percent: numeric('percent', pct).notNull(), ...baseAuditColumns(), ...baseSoftDeleteColumns(),
}, (t) => ({ owner: uniqueIndex('vessel_owners_party_key').on(t.tenantId, t.vesselId, t.partyId).where(sql`deleted_at IS NULL`) }));
export const marinaBookings = pgTable('marina_bookings', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), branchId: uuid('branch_id').notNull().references(() => branches.id), partyId: uuid('party_id').notNull().references(() => parties.id), vesselId: uuid('vessel_id').notNull().references(() => vessels.id), startsAt: timestamp('starts_at', { withTimezone: true }).notNull(), endsAt: timestamp('ends_at', { withTimezone: true }).notNull(), companions: integer('companions').notNull().default(0), insuranceAmount: numeric('insurance_amount', money).notNull().default('0'), status: text('status').notNull().default('booked'), metadata: jsonb('metadata').$type<Record<string, unknown>>().notNull().default({}), ...baseAuditColumns(), ...baseSoftDeleteColumns(), ...baseLegacyColumns(),
}, (t) => ({ vesselTime: index('marina_bookings_vessel_time_idx').on(t.tenantId, t.vesselId, t.startsAt, t.endsAt) }));
export const marinaBookingAdditions = pgTable('marina_booking_additions', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), bookingId: uuid('booking_id').notNull().references(() => marinaBookings.id, { onDelete: 'cascade' }), description: text('description').notNull(), amount: numeric('amount', money).notNull(), ...baseAuditColumns(),
});
export const rentalInvoices = pgTable('rental_invoices', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), bookingId: uuid('booking_id').notNull().references(() => marinaBookings.id), salesInvoiceId: uuid('sales_invoice_id').references(() => salesInvoices.id, { onDelete: 'set null' }), periodAmount: numeric('period_amount', money).notNull(), additionsAmount: numeric('additions_amount', money).notNull().default('0'), insuranceAmount: numeric('insurance_amount', money).notNull().default('0'), total: numeric('total', money).notNull(), status: text('status').notNull().default('draft'), metadata: jsonb('metadata').$type<Record<string, unknown>>().notNull().default({}), ...baseAuditColumns(), ...baseLegacyColumns(),
}, (t) => ({ booking: uniqueIndex('rental_invoices_booking_key').on(t.tenantId, t.bookingId) }));
export const marinaViolations = pgTable('marina_violations', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), vesselId: uuid('vessel_id').references(() => vessels.id), bookingId: uuid('booking_id').references(() => marinaBookings.id), partyId: uuid('party_id').references(() => parties.id), violationDate: date('violation_date').notNull(), amount: numeric('amount', money).notNull().default('0'), description: text('description').notNull(), status: text('status').notNull().default('open'), ...baseAuditColumns(), ...baseSoftDeleteColumns(), ...baseLegacyColumns(),
});
export const marinaOperationPlans = pgTable('marina_operation_plans', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), groupId: uuid('group_id').references(() => vesselGroups.id), planDate: date('plan_date').notNull(), name: text('name').notNull(), ...baseAuditColumns(), ...baseSoftDeleteColumns(), ...baseLegacyColumns(),
}, (t) => ({ day: index('marina_operation_plans_day_idx').on(t.tenantId, t.planDate) }));
export const marinaOperationPlanLines = pgTable('marina_operation_plan_lines', {
  planId: uuid('plan_id').notNull().references(() => marinaOperationPlans.id, { onDelete: 'cascade' }), lineNo: integer('line_no').notNull(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), vesselId: uuid('vessel_id').notNull().references(() => vessels.id), periodLabel: text('period_label'), metadata: jsonb('metadata').$type<Record<string, unknown>>().notNull().default({}),
}, (t) => ({ pk: primaryKey({ columns: [t.planId, t.lineNo] }) }));

/** تحضير المراكب — one preparation per booking, plus the return that closes it. */
export const marinaPreparations = pgTable('marina_preparations', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), bookingId: uuid('booking_id').notNull().references(() => marinaBookings.id, { onDelete: 'cascade' }), vesselId: uuid('vessel_id').notNull().references(() => vessels.id), preparedOn: date('prepared_on').notNull(), status: text('status').notNull().default('prepared'), fuelLevel: text('fuel_level'), lifeJackets: integer('life_jackets').notNull().default(0), checklist: jsonb('checklist').$type<Record<string, unknown>>().notNull().default({}), notes: text('notes'), preparedAt: timestamp('prepared_at', { withTimezone: true }).notNull().defaultNow(), returnedAt: timestamp('returned_at', { withTimezone: true }), returnNotes: text('return_notes'), ...baseAuditColumns(),
}, (t) => ({ booking: uniqueIndex('marina_preparations_booking_key').on(t.tenantId, t.bookingId), day: index('marina_preparations_day_idx').on(t.tenantId, t.preparedOn, t.status) }));

/** إغلاق اليومية — a closed harbour day refuses new bookings and rental invoices. */
export const marinaDayClosings = pgTable('marina_day_closings', {
  id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), branchId: uuid('branch_id').notNull().references(() => branches.id), closeDate: date('close_date').notNull(), bookingsCount: integer('bookings_count').notNull().default(0), rentalsCount: integer('rentals_count').notNull().default(0), rentalsTotal: numeric('rentals_total', money).notNull().default('0'), additionsTotal: numeric('additions_total', money).notNull().default('0'), insuranceTotal: numeric('insurance_total', money).notNull().default('0'), violationsTotal: numeric('violations_total', money).notNull().default('0'), notes: text('notes'), closedAt: timestamp('closed_at', { withTimezone: true }).notNull().defaultNow(), closedBy: uuid('closed_by'), ...baseAuditColumns(),
}, (t) => ({ day: uniqueIndex('marina_day_closings_day_key').on(t.tenantId, t.branchId, t.closeDate) }));

export const vehicleMakes = pgTable('vehicle_makes', { id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), name: text('name').notNull(), ...baseAuditColumns(), ...baseSoftDeleteColumns(), ...baseLegacyColumns() }, (t) => ({ name: uniqueIndex('vehicle_makes_name_key').on(t.tenantId, t.name).where(sql`deleted_at IS NULL`) }));
export const vehicleModels = pgTable('vehicle_models', { id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), makeId: uuid('make_id').notNull().references(() => vehicleMakes.id, { onDelete: 'cascade' }), name: text('name').notNull(), ...baseAuditColumns(), ...baseSoftDeleteColumns(), ...baseLegacyColumns() }, (t) => ({ name: uniqueIndex('vehicle_models_name_key').on(t.tenantId, t.makeId, t.name).where(sql`deleted_at IS NULL`) }));
export const itemVehicleFitment = pgTable('item_vehicle_fitment', { id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), itemId: uuid('item_id').notNull().references(() => items.id, { onDelete: 'cascade' }), makeId: uuid('make_id').notNull().references(() => vehicleMakes.id), modelId: uuid('model_id').references(() => vehicleModels.id), yearFrom: integer('year_from'), yearTo: integer('year_to'), notes: text('notes'), ...baseAuditColumns(), ...baseSoftDeleteColumns(), ...baseLegacyColumns() }, (t) => ({ lookup: index('item_vehicle_fitment_lookup_idx').on(t.tenantId, t.makeId, t.modelId, t.yearFrom, t.yearTo), item: index('item_vehicle_fitment_item_idx').on(t.tenantId, t.itemId) }));

export const sallaConnections = pgTable('salla_connections', { id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), storeId: text('store_id').notNull(), accessTokenEnc: text('access_token_enc').notNull(), refreshTokenEnc: text('refresh_token_enc'), webhookSecretEnc: text('webhook_secret_enc').notNull(), status: text('status').notNull().default('active'), scopes: jsonb('scopes').$type<string[]>().notNull().default([]), expiresAt: timestamp('expires_at', { withTimezone: true }), ...baseAuditColumns(), ...baseSoftDeleteColumns() }, (t) => ({ store: uniqueIndex('salla_connections_store_key').on(t.tenantId, t.storeId).where(sql`deleted_at IS NULL`) }));
export const sallaItemSync = pgTable('salla_item_sync', { id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), connectionId: uuid('connection_id').references(() => sallaConnections.id, { onDelete: 'cascade' }), itemId: uuid('item_id').notNull().references(() => items.id, { onDelete: 'cascade' }), remoteId: text('remote_id'), lastExportedSnapshot: jsonb('last_exported_snapshot').$type<Record<string, unknown>>().notNull().default({}), diffFlags: jsonb('diff_flags').$type<string[]>().notNull().default([]), status: text('status').notNull().default('pending'), nextRetryAt: timestamp('next_retry_at', { withTimezone: true }), attempts: integer('attempts').notNull().default(0), ...baseAuditColumns() }, (t) => ({ item: uniqueIndex('salla_item_sync_item_key').on(t.tenantId, t.connectionId, t.itemId) }));
export const sallaExportLog = pgTable('salla_export_log', { id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), connectionId: uuid('connection_id').references(() => sallaConnections.id, { onDelete: 'set null' }), itemId: uuid('item_id').references(() => items.id, { onDelete: 'set null' }), action: text('action').notNull(), status: text('status').notNull().default('queued'), requestPayload: jsonb('request_payload').$type<Record<string, unknown>>().notNull().default({}), responsePayload: jsonb('response_payload').$type<Record<string, unknown>>(), error: text('error'), createdAt: timestamp('created_at', { withTimezone: true }).notNull().defaultNow() }, (t) => ({ status: index('salla_export_log_status_idx').on(t.tenantId, t.status) }));
export const sallaBranchMappings = pgTable('salla_branch_mappings', { id: uuid('id').primaryKey(), tenantId: uuid('tenant_id').notNull().references(() => tenants.id, { onDelete: 'cascade' }), connectionId: uuid('connection_id').references(() => sallaConnections.id, { onDelete: 'cascade' }), branchId: uuid('branch_id').references(() => branches.id), warehouseId: uuid('warehouse_id').references(() => warehouses.id), cashLocationId: uuid('cash_location_id'), remoteBranchId: text('remote_branch_id'), ...baseAuditColumns(), ...baseSoftDeleteColumns() }, (t) => ({ remote: uniqueIndex('salla_branch_mappings_remote_key').on(t.tenantId, t.connectionId, t.remoteBranchId).where(sql`deleted_at IS NULL`) }));

export const nicheTables = { opticalPrescriptions, customerMeasurements, tailoringMeasurementAttributes, tailoringOrders, tailoringOrderOptions, tailoringOrderStatuses, tailoringTypes, tailoringOptionCategories, tailoringOptionValues, tailoringInvoices, tailoringInvoicePayments, tailoringGarmentTypes, vesselGroups, vessels, vesselGroupPricing, vesselOwners, marinaBookings, marinaBookingAdditions, rentalInvoices, marinaViolations, marinaOperationPlans, marinaOperationPlanLines, marinaPreparations, marinaDayClosings, vehicleMakes, vehicleModels, itemVehicleFitment, sallaConnections, sallaItemSync, sallaExportLog, sallaBranchMappings };
