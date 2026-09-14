import { Inject, Injectable } from '@nestjs/common';
import { and, asc, count, desc, eq, gte, ilike, isNull, lte, or, sql, type SQL } from 'drizzle-orm';
import { DomainError, newId } from '@erp/contracts';
import {
  customerMeasurements,
  parties,
  tailoringOptionCategories,
  tailoringOptionValues,
  tailoringOrderOptions,
  tailoringOrders,
  tailoringOrderStatuses,
  tailoringTypes,
  tenantSettings,
  withTenantTx,
  type DatabaseHandle,
  type DrizzleTx,
} from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';
import { isUniqueViolation } from '../organization/shared/org-support.js';
import { SequencesService } from '../platform-services/index.js';

/**
 * 🧵 طلب التفصيل — the port of `Form_WPF/frmOrders.xaml` («إدارة طلبات التفصيل»),
 * `Form_WPF/frmOrderDetails.xaml` («إضافة طلب تفصيل») and `Form_WPF/frmOptions.xaml`
 * («⚙️ إدارة الخيارات الجاهزة»).
 *
 * The desktop's four tables are just names in queries — no DDL for `TailoringOrders`,
 * `OrderStatus`, `TailoringTypes`, `OptionCategories`, `OptionValues` or `OrderOptions`
 * ships with this repository — so the column shape below is read off the SELECTs and
 * INSERTs that use them:
 *
 *   • `frmOrders.xaml.cs` `LoadOrders` L73 — `vw_OrdersComplete` with `OrderID`,
 *     `OrderNumber`, `CustomerName`, `CustomerPhone`, `MeasurementName`, `TypeName`,
 *     `StatusName`, `OrderDate`, `DeliveryDate`, `Price`, `PaidAmount`,
 *     `RemainingAmount`, `IsDelayed`, filtered by status / من / إلى / search and ordered
 *     `OrderDate DESC`.
 *   • `frmOrderDetails.xaml.cs` — `TailoringOrders(Cust_ID, MeasurementID, TypeID,
 *     DeliveryDate, Quantity, Price, PaidAmount, FabricType, FabricColor, DesignNotes,
 *     GeneralNotes, CreatedBy)` plus `OrderOptions(OrderID, CategoryID, ValueID)`.
 *   • `frmOptions.xaml.cs` — `OptionCategories(CategoryName, DisplayOrder, IsActive)` and
 *     `OptionValues(CategoryID, ValueName, DisplayOrder, IsDefault, IsActive)`.
 *
 * The rules that are ported, all of them from the code-behind:
 *
 *   • «الرجاء اختيار عميل» / «الرجاء اختيار نوع التفصيل» / «الرجاء إدخال السعر»
 *     (`btnSave_Click` L320–341) — a customer, a type and a price greater than zero are
 *     what makes an order savable.
 *   • ⌛ المتبقي = 💰 السعر − 💵 المدفوع (`CalculateRemaining` L290). The desktop never
 *     refuses a negative remaining, it paints it green instead of red, so neither do we.
 *   • «الكل» in `cmbStatus` is `StatusID = 0`, i.e. no filter at all (L79).
 *   • the search box matches رقم الطلب or اسم العميل (L100), never الجوال.
 */
export type MeasurementInput = { partyId: string; kind?: string; measurements: Record<string, string>; notes?: string; active?: boolean };

export type StatusRow = {
  id: string;
  code: string;
  nameAr: string;
  displayOrder: number;
  isFinal: boolean;
  active: boolean;
};

export type TypeInput = { code?: string | null; nameAr: string; defaultPrice?: number | string };
export type TypePatch = { code?: string | null; nameAr?: string; defaultPrice?: number | string; active?: boolean };
export type TypeRow = { id: string; code: string | null; nameAr: string; defaultPrice: string; active: boolean };

export type CategoryInput = { nameAr: string; displayOrder?: number };
export type ValueInput = { categoryId: string; nameAr: string; displayOrder?: number; isDefault?: boolean };

export type CategoryRow = {
  id: string;
  nameAr: string;
  displayOrder: number;
  active: boolean;
  values: Array<{ id: string; nameAr: string; displayOrder: number; isDefault: boolean; active: boolean }>;
};

export type OrderOptionInput = { categoryId: string; valueId: string };

export type OrderInput = {
  partyId: string;
  typeId: string;
  statusId?: string;
  measurementId?: string | null;
  orderDate?: string;
  deliveryDate?: string | null;
  quantity?: number | string;
  price: number | string;
  paidAmount?: number | string;
  fabricType?: string | null;
  fabricColor?: string | null;
  designNotes?: string | null;
  generalNotes?: string | null;
  options?: OrderOptionInput[];
};

export type OrderPatch = Partial<OrderInput> & { version?: number };

export type OrderQuery = {
  statusId?: string;
  partyId?: string;
  from?: string;
  to?: string;
  search?: string;
  typeId?: string;
  limit?: number;
  offset?: number;
};

export type OrderRow = {
  id: string;
  number: string;
  partyId: string;
  customerName: string;
  customerPhone: string;
  measurementId: string | null;
  measurementName: string | null;
  typeId: string;
  typeName: string;
  statusId: string;
  statusName: string;
  orderDate: string;
  deliveryDate: string | null;
  quantity: string;
  price: string;
  paidAmount: string;
  remainingAmount: string;
  isDelayed: boolean;
  fabricType: string | null;
  fabricColor: string | null;
  designNotes: string | null;
  generalNotes: string | null;
  version: number;
  options: Array<{ categoryId: string; categoryName: string; valueId: string; valueName: string }>;
};

const ORDER_SEQUENCE = { prefix: 'TO-', padding: 6 } as const;

function todayISO(): string {
  return new Date().toISOString().slice(0, 10);
}

function isISODate(value: string): boolean {
  return /^\d{4}-\d{2}-\d{2}$/.test(value);
}

/** `frmOrderDetails` reads every number with `decimal.TryParse(…, out x)` — blank is zero. */
function decimal(value: number | string | null | undefined, fallback = 0): number {
  if (value === null || value === undefined || value === '') return fallback;
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function four(value: number): string {
  return value.toFixed(4);
}

function isDelayedOn(deliveryDate: string | null, isFinal: boolean): boolean {
  if (!deliveryDate || isFinal) return false;
  return deliveryDate < todayISO();
}

@Injectable()
export class TailoringService {
  constructor(
    @Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle,
    private readonly sequences: SequencesService,
  ) {}

  async ensureEnabled(tenantId: string) {
    const [flag] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(tenantSettings)
        .where(and(eq(tenantSettings.tenantId, tenantId), eq(tenantSettings.key, 'pack.tailoring')))
        .limit(1),
    );
    if (flag && flag.value !== true && flag.value !== 'true')
      throw new DomainError('NOT_FOUND', 'Tailoring pack is disabled', 404);
  }

  // ─────────────────────────────── 📏 القياسات (unchanged) ───────────────────────────────

  async list(tenantId: string, partyId: string) {
    await this.ensureEnabled(tenantId);
    return {
      data: await withTenantTx(this.database.db, tenantId, (tx) =>
        tx
          .select()
          .from(customerMeasurements)
          .where(
            and(
              eq(customerMeasurements.tenantId, tenantId),
              eq(customerMeasurements.partyId, partyId),
              isNull(customerMeasurements.deletedAt),
            ),
          )
          .orderBy(desc(customerMeasurements.createdAt)),
      ),
    };
  }

  async latest(tenantId: string, partyId: string) {
    const rows = await this.list(tenantId, partyId);
    return { data: rows.data[0] ?? null };
  }

  async create(tenantId: string, input: MeasurementInput) {
    await this.ensureEnabled(tenantId);
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .insert(customerMeasurements)
        .values({
          id: newId(),
          tenantId,
          partyId: input.partyId,
          kind: input.kind ?? 'tailoring',
          measurements: input.measurements,
          notes: input.notes,
          active: input.active ?? true,
        })
        .returning(),
    );
    return row;
  }

  // ─────────────────────────────── ⚙️ الحالات ───────────────────────────────

  /**
   * `frmOrders.LoadStatusFilter` L54 —
   * `SELECT StatusID, StatusName FROM OrderStatus WHERE IsActive=1 ORDER BY DisplayOrder`,
   * with «الكل» (StatusID 0) prepended in the UI rather than in the data.
   */
  async listStatuses(tenantId: string, options: { activeOnly?: boolean } = {}): Promise<{ data: StatusRow[] }> {
    await this.ensureEnabled(tenantId);
    const activeOnly = options.activeOnly ?? true;
    const rows = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(tailoringOrderStatuses)
        .where(
          and(
            eq(tailoringOrderStatuses.tenantId, tenantId),
            isNull(tailoringOrderStatuses.deletedAt),
            ...(activeOnly ? [eq(tailoringOrderStatuses.active, true)] : []),
          ),
        )
        .orderBy(asc(tailoringOrderStatuses.displayOrder), asc(tailoringOrderStatuses.nameAr)),
    );
    return {
      data: rows.map((row) => ({
        id: row.id,
        code: row.code,
        nameAr: row.nameAr,
        displayOrder: row.displayOrder,
        isFinal: row.isFinal,
        active: row.active,
      })),
    };
  }

  private async statusOrThrow(tenantId: string, statusId: string | undefined | null): Promise<StatusRow> {
    if (!statusId) {
      const statuses = await this.listStatuses(tenantId);
      const [first] = statuses.data;
      if (!first) throw new DomainError('TAILORING_STATUS_NOT_FOUND', 'لا توجد حالات للطلبات', 404);
      return first;
    }
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(tailoringOrderStatuses)
        .where(
          and(
            eq(tailoringOrderStatuses.tenantId, tenantId),
            eq(tailoringOrderStatuses.id, statusId),
            isNull(tailoringOrderStatuses.deletedAt),
          ),
        )
        .limit(1),
    );
    if (!row) throw new DomainError('TAILORING_STATUS_NOT_FOUND', 'حالة الطلب غير موجودة', 404);
    return {
      id: row.id,
      code: row.code,
      nameAr: row.nameAr,
      displayOrder: row.displayOrder,
      isFinal: row.isFinal,
      active: row.active,
    };
  }

  // ─────────────────────────────── 🧵 أنواع التفصيل ───────────────────────────────

  /**
   * `frmOrderDetails.LoadTypes` L60 —
   * `SELECT TypeID, TypeName, DefaultPrice FROM TailoringTypes WHERE IsActive=1 ORDER BY TypeName`.
   * No desktop screen edits أنواع التفصيل (the table is seeded by the DBA), but a طلب
   * cannot be saved without one, so the cloud exposes the CRUD the desktop leaves to SQL.
   */
  async listTypes(tenantId: string, options: { activeOnly?: boolean } = {}): Promise<{ data: TypeRow[] }> {
    await this.ensureEnabled(tenantId);
    const activeOnly = options.activeOnly ?? true;
    const rows = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(tailoringTypes)
        .where(
          and(
            eq(tailoringTypes.tenantId, tenantId),
            isNull(tailoringTypes.deletedAt),
            ...(activeOnly ? [eq(tailoringTypes.active, true)] : []),
          ),
        )
        .orderBy(asc(tailoringTypes.nameAr)),
    );
    return { data: rows.map((row) => this.toTypeRow(row)) };
  }

  private toTypeRow(row: typeof tailoringTypes.$inferSelect): TypeRow {
    return { id: row.id, code: row.code, nameAr: row.nameAr, defaultPrice: row.defaultPrice, active: row.active };
  }

  async createType(tenantId: string, input: TypeInput, userId?: string): Promise<TypeRow> {
    await this.ensureEnabled(tenantId);
    const nameAr = (input.nameAr ?? '').trim();
    if (!nameAr) throw new DomainError('TAILORING_TYPE_NAME_REQUIRED', 'الرجاء إدخال نوع التفصيل', 422);
    try {
      const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
        tx
          .insert(tailoringTypes)
          .values({
            id: newId(),
            tenantId,
            code: input.code?.trim() || null,
            nameAr,
            defaultPrice: four(decimal(input.defaultPrice)),
            createdBy: userId ?? null,
          })
          .returning(),
      );
      return this.toTypeRow(row!);
    } catch (error) {
      throw this.typeConflict(error);
    }
  }

  async updateType(tenantId: string, id: string, patch: TypePatch, userId?: string): Promise<TypeRow> {
    await this.ensureEnabled(tenantId);
    const values: Partial<typeof tailoringTypes.$inferInsert> = { updatedAt: new Date(), updatedBy: userId ?? null };
    if (patch.nameAr !== undefined) {
      const nameAr = patch.nameAr.trim();
      if (!nameAr) throw new DomainError('TAILORING_TYPE_NAME_REQUIRED', 'الرجاء إدخال نوع التفصيل', 422);
      values.nameAr = nameAr;
    }
    if (patch.code !== undefined) values.code = patch.code?.trim() || null;
    if (patch.defaultPrice !== undefined) values.defaultPrice = four(decimal(patch.defaultPrice));
    if (patch.active !== undefined) values.active = patch.active;
    try {
      const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
        tx
          .update(tailoringTypes)
          .set(values)
          .where(and(eq(tailoringTypes.tenantId, tenantId), eq(tailoringTypes.id, id), isNull(tailoringTypes.deletedAt)))
          .returning(),
      );
      if (!row) throw new DomainError('TAILORING_TYPE_NOT_FOUND', 'نوع التفصيل غير موجود', 404);
      return this.toTypeRow(row!);
    } catch (error) {
      throw this.typeConflict(error);
    }
  }

  async deleteType(tenantId: string, id: string, userId?: string): Promise<{ deleted: true }> {
    await this.ensureEnabled(tenantId);
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .update(tailoringTypes)
        .set({ deletedAt: new Date(), deletedBy: userId ?? null, active: false })
        .where(and(eq(tailoringTypes.tenantId, tenantId), eq(tailoringTypes.id, id), isNull(tailoringTypes.deletedAt)))
        .returning({ id: tailoringTypes.id }),
    );
    if (!row) throw new DomainError('TAILORING_TYPE_NOT_FOUND', 'نوع التفصيل غير موجود', 404);
    return { deleted: true };
  }

  /**
   * The index is the authority — the pre-check can pass under concurrency and the insert
   * still lose, and the operator deserves the same sentence either way.
   */
  private typeConflict(error: unknown): unknown {
    if (isUniqueViolation(error, 'tailoring_types_tenant_name_key'))
      return new DomainError('TAILORING_TYPE_NAME_TAKEN', 'نوع التفصيل موجود مسبقاً', 409);
    if (isUniqueViolation(error, 'tailoring_types_tenant_code_key'))
      return new DomainError('TAILORING_TYPE_CODE_TAKEN', 'رمز نوع التفصيل موجود مسبقاً', 409);
    return error;
  }

  // ─────────────────────────────── 🔧 الخيارات الجاهزة ───────────────────────────────

  /** `frmOptions.xaml.cs` L49 and L81 — categories with their values, both `ORDER BY DisplayOrder`. */
  async listOptionCategories(tenantId: string, options: { activeOnly?: boolean } = {}): Promise<{ data: CategoryRow[] }> {
    await this.ensureEnabled(tenantId);
    const activeOnly = options.activeOnly ?? true;
    return {
      data: await withTenantTx(this.database.db, tenantId, async (tx) => {
        const categories = await tx
          .select()
          .from(tailoringOptionCategories)
          .where(
            and(
              eq(tailoringOptionCategories.tenantId, tenantId),
              isNull(tailoringOptionCategories.deletedAt),
              ...(activeOnly ? [eq(tailoringOptionCategories.active, true)] : []),
            ),
          )
          .orderBy(asc(tailoringOptionCategories.displayOrder), asc(tailoringOptionCategories.nameAr));
        const values = await tx
          .select()
          .from(tailoringOptionValues)
          .where(
            and(
              eq(tailoringOptionValues.tenantId, tenantId),
              isNull(tailoringOptionValues.deletedAt),
              ...(activeOnly ? [eq(tailoringOptionValues.active, true)] : []),
            ),
          )
          .orderBy(asc(tailoringOptionValues.displayOrder), asc(tailoringOptionValues.nameAr));
        return categories.map((category) => ({
          id: category.id,
          nameAr: category.nameAr,
          displayOrder: category.displayOrder,
          active: category.active,
          values: values
            .filter((value) => value.categoryId === category.id)
            .map((value) => ({
              id: value.id,
              nameAr: value.nameAr,
              displayOrder: value.displayOrder,
              isDefault: value.isDefault,
              active: value.active,
            })),
        }));
      }),
    };
  }

  async createOptionCategory(tenantId: string, input: CategoryInput, userId?: string): Promise<{ id: string; nameAr: string; displayOrder: number }> {
    await this.ensureEnabled(tenantId);
    const nameAr = (input.nameAr ?? '').trim();
    if (!nameAr) throw new DomainError('TAILORING_CATEGORY_NAME_REQUIRED', 'الرجاء إدخال اسم التصنيف', 422);
    // `frmOptions.xaml.cs` L136 — `ISNULL(MAX(DisplayOrder),0)+1`
    const [last] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select({ displayOrder: tailoringOptionCategories.displayOrder })
        .from(tailoringOptionCategories)
        .where(eq(tailoringOptionCategories.tenantId, tenantId))
        .orderBy(desc(tailoringOptionCategories.displayOrder))
        .limit(1),
    );
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .insert(tailoringOptionCategories)
        .values({
          id: newId(),
          tenantId,
          nameAr,
          displayOrder: input.displayOrder ?? (last?.displayOrder ?? 0) + 1,
          createdBy: userId ?? null,
        })
        .returning(),
    );
    return { id: row!.id, nameAr: row!.nameAr, displayOrder: row!.displayOrder };
  }

  async updateOptionCategory(
    tenantId: string,
    id: string,
    patch: { nameAr?: string; displayOrder?: number; active?: boolean },
    userId?: string,
  ): Promise<{ id: string; nameAr: string; displayOrder: number }> {
    await this.ensureEnabled(tenantId);
    if (patch.nameAr !== undefined && !patch.nameAr.trim())
      throw new DomainError('TAILORING_CATEGORY_NAME_REQUIRED', 'الرجاء إدخال اسم التصنيف', 422);
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .update(tailoringOptionCategories)
        .set({
          ...(patch.nameAr === undefined ? {} : { nameAr: patch.nameAr.trim() }),
          ...(patch.displayOrder === undefined ? {} : { displayOrder: patch.displayOrder }),
          ...(patch.active === undefined ? {} : { active: patch.active }),
          updatedAt: new Date(),
          updatedBy: userId ?? null,
        })
        .where(
          and(
            eq(tailoringOptionCategories.tenantId, tenantId),
            eq(tailoringOptionCategories.id, id),
            isNull(tailoringOptionCategories.deletedAt),
          ),
        )
        .returning(),
    );
    if (!row) throw new DomainError('TAILORING_CATEGORY_NOT_FOUND', 'التصنيف غير موجود', 404);
    return { id: row.id, nameAr: row.nameAr, displayOrder: row.displayOrder };
  }

  /** `frmOptions.xaml.cs` L198 — `UPDATE OptionCategories SET IsActive=0`, after «هل أنت متأكد من حذف هذا التصنيف وجميع خياراته؟». */
  async deleteOptionCategory(tenantId: string, id: string, userId?: string): Promise<{ deleted: true }> {
    await this.ensureEnabled(tenantId);
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .update(tailoringOptionCategories)
        .set({ deletedAt: new Date(), deletedBy: userId ?? null, active: false })
        .where(
          and(
            eq(tailoringOptionCategories.tenantId, tenantId),
            eq(tailoringOptionCategories.id, id),
            isNull(tailoringOptionCategories.deletedAt),
          ),
        )
        .returning({ id: tailoringOptionCategories.id }),
    );
    if (!row) throw new DomainError('TAILORING_CATEGORY_NOT_FOUND', 'التصنيف غير موجود', 404);
    return { deleted: true };
  }

  async createOptionValue(tenantId: string, input: ValueInput, userId?: string): Promise<{ id: string; nameAr: string; isDefault: boolean }> {
    await this.ensureEnabled(tenantId);
    const nameAr = (input.nameAr ?? '').trim();
    if (!nameAr) throw new DomainError('TAILORING_VALUE_NAME_REQUIRED', 'الرجاء إدخال اسم الخيار', 422);
    await this.categoryOrThrow(tenantId, input.categoryId);
    const [last] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select({ displayOrder: tailoringOptionValues.displayOrder })
        .from(tailoringOptionValues)
        .where(and(eq(tailoringOptionValues.tenantId, tenantId), eq(tailoringOptionValues.categoryId, input.categoryId)))
        .orderBy(desc(tailoringOptionValues.displayOrder))
        .limit(1),
    );
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .insert(tailoringOptionValues)
        .values({
          id: newId(),
          tenantId,
          categoryId: input.categoryId,
          nameAr,
          isDefault: input.isDefault ?? false,
          displayOrder: input.displayOrder ?? (last?.displayOrder ?? 0) + 1,
          createdBy: userId ?? null,
        })
        .returning(),
    );
    return { id: row!.id, nameAr: row!.nameAr, isDefault: row!.isDefault };
  }

  async updateOptionValue(
    tenantId: string,
    id: string,
    patch: { nameAr?: string; displayOrder?: number; isDefault?: boolean; active?: boolean },
    userId?: string,
  ): Promise<{ id: string; nameAr: string; isDefault: boolean }> {
    await this.ensureEnabled(tenantId);
    if (patch.nameAr !== undefined && !patch.nameAr.trim())
      throw new DomainError('TAILORING_VALUE_NAME_REQUIRED', 'الرجاء إدخال اسم الخيار', 422);
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .update(tailoringOptionValues)
        .set({
          ...(patch.nameAr === undefined ? {} : { nameAr: patch.nameAr.trim() }),
          ...(patch.displayOrder === undefined ? {} : { displayOrder: patch.displayOrder }),
          ...(patch.isDefault === undefined ? {} : { isDefault: patch.isDefault }),
          ...(patch.active === undefined ? {} : { active: patch.active }),
          updatedAt: new Date(),
          updatedBy: userId ?? null,
        })
        .where(and(eq(tailoringOptionValues.tenantId, tenantId), eq(tailoringOptionValues.id, id), isNull(tailoringOptionValues.deletedAt)))
        .returning(),
    );
    if (!row) throw new DomainError('TAILORING_VALUE_NOT_FOUND', 'الخيار غير موجود', 404);
    return { id: row.id, nameAr: row.nameAr, isDefault: row.isDefault };
  }

  /** «هل أنت متأكد من حذف هذا الخيار؟» — `UPDATE OptionValues SET IsActive=0` (`frmOptions.xaml.cs` L298). */
  async deleteOptionValue(tenantId: string, id: string, userId?: string): Promise<{ deleted: true }> {
    await this.ensureEnabled(tenantId);
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .update(tailoringOptionValues)
        .set({ deletedAt: new Date(), deletedBy: userId ?? null, active: false })
        .where(and(eq(tailoringOptionValues.tenantId, tenantId), eq(tailoringOptionValues.id, id), isNull(tailoringOptionValues.deletedAt)))
        .returning({ id: tailoringOptionValues.id }),
    );
    if (!row) throw new DomainError('TAILORING_VALUE_NOT_FOUND', 'الخيار غير موجود', 404);
    return { deleted: true };
  }

  /** ⭐ تعيين افتراضي — `UPDATE OptionValues SET IsDefault=0 WHERE CategoryID=@CatID` then `SET IsDefault=1` (L323/L330). */
  async setDefaultOptionValue(tenantId: string, id: string, userId?: string): Promise<{ id: string; isDefault: boolean }> {
    await this.ensureEnabled(tenantId);
    const [current] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(tailoringOptionValues)
        .where(and(eq(tailoringOptionValues.tenantId, tenantId), eq(tailoringOptionValues.id, id), isNull(tailoringOptionValues.deletedAt)))
        .limit(1),
    );
    if (!current) throw new DomainError('TAILORING_VALUE_NOT_FOUND', 'الخيار غير موجود', 404);
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      await tx
        .update(tailoringOptionValues)
        .set({ isDefault: false, updatedAt: new Date(), updatedBy: userId ?? null })
        .where(
          and(
            eq(tailoringOptionValues.tenantId, tenantId),
            eq(tailoringOptionValues.categoryId, current.categoryId),
            isNull(tailoringOptionValues.deletedAt),
          ),
        );
      await tx
        .update(tailoringOptionValues)
        .set({ isDefault: true, updatedAt: new Date(), updatedBy: userId ?? null })
        .where(and(eq(tailoringOptionValues.tenantId, tenantId), eq(tailoringOptionValues.id, id)));
    });
    return { id, isDefault: true };
  }

  private async categoryOrThrow(tenantId: string, categoryId: string | undefined | null) {
    if (!categoryId) throw new DomainError('TAILORING_CATEGORY_NOT_FOUND', 'التصنيف غير موجود', 404);
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(tailoringOptionCategories)
        .where(
          and(
            eq(tailoringOptionCategories.tenantId, tenantId),
            eq(tailoringOptionCategories.id, categoryId),
            isNull(tailoringOptionCategories.deletedAt),
          ),
        )
        .limit(1),
    );
    if (!row) throw new DomainError('TAILORING_CATEGORY_NOT_FOUND', 'التصنيف غير موجود', 404);
    return row;
  }

  // ─────────────────────────────── 🧾 الطلبات ───────────────────────────────

  /**
   * `frmOrders.LoadOrders` L73 — the grid «📋 قائمة الطلبات» with
   * `رقم الطلب · 👤 العميل · 📞 الجوال · القياس · نوع التفصيل · ⚙️ الحالة ·
   * 📅 تاريخ الطلب · 📅 موعد التسليم · 💰 السعر · 💵 المدفوع · ⌛ المتبقي`.
   */
  async listOrders(tenantId: string, query: OrderQuery = {}): Promise<{ data: OrderRow[]; meta: { total: number } }> {
    await this.ensureEnabled(tenantId);
    const limit = Math.min(Math.max(query.limit ?? 200, 1), 500);
    const offset = Math.max(query.offset ?? 0, 0);
    const filters: SQL[] = [eq(tailoringOrders.tenantId, tenantId), isNull(tailoringOrders.deletedAt)];
    if (query.statusId) filters.push(eq(tailoringOrders.statusId, query.statusId));
    if (query.partyId) filters.push(eq(tailoringOrders.partyId, query.partyId));
    if (query.typeId) filters.push(eq(tailoringOrders.typeId, query.typeId));
    if (query.from) filters.push(gte(tailoringOrders.orderDate, query.from));
    if (query.to) filters.push(lte(tailoringOrders.orderDate, query.to));
    const search = query.search?.trim();
    const searchFilter = search
      ? or(ilike(tailoringOrders.number, `%${search}%`), ilike(parties.name, `%${search}%`))
      : undefined;
    if (searchFilter) filters.push(searchFilter);
    const where = and(...filters);
    const rows = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select({
          order: tailoringOrders,
          customerName: parties.name,
          customerPhone: parties.phone,
          typeName: tailoringTypes.nameAr,
          statusName: tailoringOrderStatuses.nameAr,
          statusFinal: tailoringOrderStatuses.isFinal,
        })
        .from(tailoringOrders)
        .innerJoin(parties, eq(parties.id, tailoringOrders.partyId))
        .innerJoin(tailoringTypes, eq(tailoringTypes.id, tailoringOrders.typeId))
        .innerJoin(tailoringOrderStatuses, eq(tailoringOrderStatuses.id, tailoringOrders.statusId))
        .where(where)
        .orderBy(desc(tailoringOrders.orderDate), desc(tailoringOrders.number))
        .limit(limit)
        .offset(offset),
    );
    const data = await this.withMeasurementNames(tenantId, rows);
    const [counted] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select({ total: count() })
        .from(tailoringOrders)
        .innerJoin(parties, eq(parties.id, tailoringOrders.partyId))
        .where(where),
    );
    return { data, meta: { total: Number(counted?.total ?? 0) } };
  }

  private async withMeasurementNames(
    tenantId: string,
    rows: Array<{
      order: typeof tailoringOrders.$inferSelect;
      customerName: string;
      customerPhone: string | null;
      typeName: string;
      statusName: string;
      statusFinal: boolean;
    }>,
  ): Promise<OrderRow[]> {
    const measurementIds = rows.map((row) => row.order.measurementId).filter((id): id is string => Boolean(id));
    const names = new Map<string, string>();
    if (measurementIds.length) {
      const measurements = await withTenantTx(this.database.db, tenantId, (tx) =>
        tx
          .select({ id: customerMeasurements.id, createdAt: customerMeasurements.createdAt, kind: customerMeasurements.kind })
          .from(customerMeasurements)
          .where(and(eq(customerMeasurements.tenantId, tenantId), isNull(customerMeasurements.deletedAt))),
      );
      for (const measurement of measurements) {
        if (!measurementIds.includes(measurement.id)) continue;
        // `frmOrderDetails.LoadCustomerMeasurements` L259 — the display name falls back to
        // «قياس بتاريخ …» when `MeasurementName` is null.
        names.set(measurement.id, `قياس بتاريخ ${measurement.createdAt.toISOString().slice(0, 10)}`);
      }
    }
    return rows.map((row) => ({
      id: row.order.id,
      number: row.order.number,
      partyId: row.order.partyId,
      customerName: row.customerName,
      customerPhone: row.customerPhone ?? '',
      measurementId: row.order.measurementId,
      measurementName: names.get(row.order.measurementId ?? '') ?? null,
      typeId: row.order.typeId,
      typeName: row.typeName,
      statusId: row.order.statusId,
      statusName: row.statusName,
      orderDate: row.order.orderDate,
      deliveryDate: row.order.deliveryDate,
      quantity: row.order.quantity,
      price: row.order.price,
      paidAmount: row.order.paidAmount,
      remainingAmount: four(decimal(row.order.price) - decimal(row.order.paidAmount)),
      isDelayed: isDelayedOn(row.order.deliveryDate, row.statusFinal),
      fabricType: row.order.fabricType,
      fabricColor: row.order.fabricColor,
      designNotes: row.order.designNotes,
      generalNotes: row.order.generalNotes,
      version: row.order.version,
      options: [],
    }));
  }

  async getOrder(tenantId: string, id: string): Promise<OrderRow> {
    await this.ensureEnabled(tenantId);
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select({
          order: tailoringOrders,
          customerName: parties.name,
          customerPhone: parties.phone,
          typeName: tailoringTypes.nameAr,
          statusName: tailoringOrderStatuses.nameAr,
          statusFinal: tailoringOrderStatuses.isFinal,
        })
        .from(tailoringOrders)
        .innerJoin(parties, eq(parties.id, tailoringOrders.partyId))
        .innerJoin(tailoringTypes, eq(tailoringTypes.id, tailoringOrders.typeId))
        .innerJoin(tailoringOrderStatuses, eq(tailoringOrderStatuses.id, tailoringOrders.statusId))
        .where(and(eq(tailoringOrders.tenantId, tenantId), eq(tailoringOrders.id, id), isNull(tailoringOrders.deletedAt)))
        .limit(1),
    );
    if (!row) throw new DomainError('TAILORING_ORDER_NOT_FOUND', 'الطلب غير موجود', 404);
    const [shaped] = await this.withMeasurementNames(tenantId, [row]);
    return { ...shaped!, options: await this.readOptions(tenantId, id) };
  }

  private async readOptions(tenantId: string, orderId: string) {
    const rows = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select({
          categoryId: tailoringOrderOptions.categoryId,
          categoryName: tailoringOptionCategories.nameAr,
          valueId: tailoringOrderOptions.valueId,
          valueName: tailoringOptionValues.nameAr,
        })
        .from(tailoringOrderOptions)
        .innerJoin(tailoringOptionCategories, eq(tailoringOptionCategories.id, tailoringOrderOptions.categoryId))
        .innerJoin(tailoringOptionValues, eq(tailoringOptionValues.id, tailoringOrderOptions.valueId))
        .where(and(eq(tailoringOrderOptions.tenantId, tenantId), eq(tailoringOrderOptions.orderId, orderId)))
        .orderBy(asc(tailoringOptionCategories.displayOrder)),
    );
    return rows;
  }

  /** «💾 حفظ الطلب» — `frmOrderDetails.btnSave_Click` L318. */
  async createOrder(tenantId: string, input: OrderInput, userId?: string): Promise<OrderRow> {
    await this.ensureEnabled(tenantId);
    const { partyId, typeId, price: orderPrice, options } = this.validateOrderInput(tenantId, input);
    const status = await this.statusOrThrow(tenantId, input.statusId);
    await this.assertParty(tenantId, partyId);
    await this.assertType(tenantId, typeId);
    await this.assertMeasurement(tenantId, input.measurementId, partyId);
    const orderDate = this.dateOrToday(input.orderDate);
    const id = newId();
    const number = await withTenantTx(this.database.db, tenantId, (tx) =>
      this.sequences.next({ tenantId, docType: 'tailoring_order' }, tx, ORDER_SEQUENCE).then((allocated) => allocated.display),
    );
    await withTenantTx(this.database.db, tenantId, async (tx) => {
      await tx.insert(tailoringOrders).values({
        id,
        tenantId,
        number,
        partyId,
        measurementId: input.measurementId ?? null,
        typeId,
        statusId: status.id,
        orderDate,
        deliveryDate: this.dateOrNull(input.deliveryDate),
        quantity: four(decimal(input.quantity, 1)),
        price: four(orderPrice),
        paidAmount: four(decimal(input.paidAmount)),
        fabricType: input.fabricType?.trim() || null,
        fabricColor: input.fabricColor?.trim() || null,
        designNotes: input.designNotes?.trim() || null,
        generalNotes: input.generalNotes?.trim() || null,
        createdBy: userId ?? null,
      });
      await this.writeOptions(tx, tenantId, id, options, userId);
    });
    return this.getOrder(tenantId, id);
  }

  /**
   * `frmOrders.btnEdit_Click` L176 opens `frmOrderDetails`, whose `LoadOrderData` L417 is
   * empty («يمكن تطويرها لاحقًا») and whose save always INSERTs — editing an order on the
   * desktop really does create a second one. The cloud updates the order the user
   * selected instead: that is what «✏️ تعديل» means to the person clicking it.
   */
  async updateOrder(tenantId: string, id: string, patch: OrderPatch, userId?: string): Promise<OrderRow> {
    await this.ensureEnabled(tenantId);
    const [current] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select()
        .from(tailoringOrders)
        .where(and(eq(tailoringOrders.tenantId, tenantId), eq(tailoringOrders.id, id), isNull(tailoringOrders.deletedAt)))
        .limit(1),
    );
    if (!current) throw new DomainError('TAILORING_ORDER_NOT_FOUND', 'الطلب غير موجود', 404);
    if (patch.version !== undefined && patch.version !== current.version)
      throw new DomainError('VERSION_CONFLICT', 'تم تعديل الطلب من جهة أخرى', 409);

    const partyId = current.partyId;
    const typeId = patch.typeId ?? current.typeId;
    const orderPrice = patch.price === undefined ? decimal(current.price) : this.priceOrThrow(patch.price);
    if (patch.partyId !== undefined && patch.partyId !== partyId)
      throw new DomainError('TAILORING_CUSTOMER_IMMUTABLE', 'لا يمكن تغيير عميل الطلب', 422);
    await this.assertType(tenantId, typeId);
    const measurementId = patch.measurementId === undefined ? current.measurementId : patch.measurementId;
    await this.assertMeasurement(tenantId, measurementId, partyId);
    const statusId = patch.statusId === undefined ? current.statusId : (await this.statusOrThrow(tenantId, patch.statusId)).id;

    await withTenantTx(this.database.db, tenantId, async (tx) => {
      await tx
        .update(tailoringOrders)
        .set({
          typeId,
          statusId,
          measurementId: measurementId ?? null,
          orderDate: patch.orderDate === undefined ? current.orderDate : this.dateOrToday(patch.orderDate),
          deliveryDate: patch.deliveryDate === undefined ? current.deliveryDate : this.dateOrNull(patch.deliveryDate),
          quantity: four(decimal(patch.quantity ?? current.quantity, 1)),
          price: four(orderPrice),
          paidAmount: four(decimal(patch.paidAmount ?? current.paidAmount)),
          fabricType: patch.fabricType === undefined ? current.fabricType : patch.fabricType?.trim() || null,
          fabricColor: patch.fabricColor === undefined ? current.fabricColor : patch.fabricColor?.trim() || null,
          designNotes: patch.designNotes === undefined ? current.designNotes : patch.designNotes?.trim() || null,
          generalNotes: patch.generalNotes === undefined ? current.generalNotes : patch.generalNotes?.trim() || null,
          updatedAt: new Date(),
          updatedBy: userId ?? null,
          version: current.version + 1,
        })
        .where(and(eq(tailoringOrders.tenantId, tenantId), eq(tailoringOrders.id, id)));
      if (patch.options) await this.writeOptions(tx, tenantId, id, patch.options, userId);
    });
    return this.getOrder(tenantId, id);
  }

  /**
   * `frmOrders.btnChangeStatus_Click` L281 — the «تغيير حالة الطلب» window calls
   * `sp_UpdateOrderStatus @OrderID, @NewStatusID, @ChangedBy`. The procedure's body is not
   * in the repository; what is observable is the reload that follows, so the status is
   * written on the order itself and nothing else is touched.
   */
  async changeStatus(tenantId: string, id: string, statusId: string, userId?: string): Promise<OrderRow> {
    await this.ensureEnabled(tenantId);
    const status = await this.statusOrThrow(tenantId, statusId);
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .update(tailoringOrders)
        .set({
          statusId: status.id,
          updatedAt: new Date(),
          updatedBy: userId ?? null,
          version: sql`${tailoringOrders.version} + 1`,
        })
        .where(and(eq(tailoringOrders.tenantId, tenantId), eq(tailoringOrders.id, id), isNull(tailoringOrders.deletedAt)))
        .returning({ id: tailoringOrders.id }),
    );
    if (!row) throw new DomainError('TAILORING_ORDER_NOT_FOUND', 'الطلب غير موجود', 404);
    return this.getOrder(tenantId, id);
  }

  /** «🗑️ حذف» — `DELETE FROM TailoringOrders WHERE OrderID=@ID` (`frmOrders.xaml.cs` L218). */
  async deleteOrder(tenantId: string, id: string, userId?: string): Promise<{ deleted: true }> {
    await this.ensureEnabled(tenantId);
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .update(tailoringOrders)
        .set({ deletedAt: new Date(), deletedBy: userId ?? null })
        .where(and(eq(tailoringOrders.tenantId, tenantId), eq(tailoringOrders.id, id), isNull(tailoringOrders.deletedAt)))
        .returning({ id: tailoringOrders.id }),
    );
    if (!row) throw new DomainError('TAILORING_ORDER_NOT_FOUND', 'الطلب غير موجود', 404);
    return { deleted: true };
  }

  // ─────────────────────────────── helpers ───────────────────────────────

  private validateOrderInput(tenantId: string, input: OrderInput) {
    const partyId = input.partyId?.trim();
    if (!partyId) throw new DomainError('TAILORING_CUSTOMER_REQUIRED', 'الرجاء اختيار عميل', 422);
    const typeId = input.typeId?.trim();
    if (!typeId) throw new DomainError('TAILORING_TYPE_REQUIRED', 'الرجاء اختيار نوع التفصيل', 422);
    if (input.orderDate && !isISODate(input.orderDate))
      throw new DomainError('TAILORING_DATE_INVALID', 'تاريخ الطلب غير صحيح', 422);
    if (input.deliveryDate && !isISODate(input.deliveryDate))
      throw new DomainError('TAILORING_DATE_INVALID', 'موعد التسليم غير صحيح', 422);
    if (input.quantity !== undefined && decimal(input.quantity, 1) < 0)
      throw new DomainError('TAILORING_QUANTITY_INVALID', 'الكمية غير صحيحة', 422);
    return { partyId, typeId, price: this.priceOrThrow(input.price), options: input.options ?? [] };
  }

  /** «الرجاء إدخال السعر» — the desktop refuses `price <= 0` (`btnSave_Click` L339). */
  private priceOrThrow(value: number | string | undefined): number {
    const parsed = decimal(value);
    if (parsed <= 0) throw new DomainError('TAILORING_PRICE_REQUIRED', 'الرجاء إدخال السعر', 422);
    return parsed;
  }

  private dateOrToday(value: string | undefined): string {
    if (!value) return todayISO();
    return value;
  }

  private dateOrNull(value: string | null | undefined): string | null {
    if (!value) return null;
    return value;
  }

  private async assertParty(tenantId: string, partyId: string) {
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select({ id: parties.id })
        .from(parties)
        .where(and(eq(parties.tenantId, tenantId), eq(parties.id, partyId), isNull(parties.deletedAt)))
        .limit(1),
    );
    if (!row) throw new DomainError('TAILORING_CUSTOMER_NOT_FOUND', 'العميل غير موجود', 404);
  }

  private async assertType(tenantId: string, typeId: string) {
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select({ id: tailoringTypes.id })
        .from(tailoringTypes)
        .where(and(eq(tailoringTypes.tenantId, tenantId), eq(tailoringTypes.id, typeId), isNull(tailoringTypes.deletedAt)))
        .limit(1),
    );
    if (!row) throw new DomainError('TAILORING_TYPE_NOT_FOUND', 'نوع التفصيل غير موجود', 404);
  }

  /** The القياس must belong to the same عميل — `LoadCustomerMeasurements` L259 filters by `Cust_ID`. */
  private async assertMeasurement(tenantId: string, measurementId: string | null | undefined, partyId: string) {
    if (!measurementId) return;
    const [row] = await withTenantTx(this.database.db, tenantId, (tx) =>
      tx
        .select({ id: customerMeasurements.id, partyId: customerMeasurements.partyId })
        .from(customerMeasurements)
        .where(
          and(
            eq(customerMeasurements.tenantId, tenantId),
            eq(customerMeasurements.id, measurementId),
            isNull(customerMeasurements.deletedAt),
          ),
        )
        .limit(1),
    );
    if (!row) throw new DomainError('TAILORING_MEASUREMENT_NOT_FOUND', 'القياس غير موجود', 404);
    if (row.partyId !== partyId)
      throw new DomainError('TAILORING_MEASUREMENT_PARTY_MISMATCH', 'القياس لا يتبع هذا العميل', 422);
  }

  private async writeOptions(
    tx: DrizzleTx,
    tenantId: string,
    orderId: string,
    options: OrderOptionInput[],
    userId?: string,
  ) {
    const seen = new Map<string, string>();
    for (const option of options) {
      if (!option?.categoryId || !option?.valueId)
        throw new DomainError('TAILORING_OPTION_INVALID', 'الخيار غير صحيح', 422);
      // `selectedOptions[catId] = valId` — one value per category (`OptionButton_Click` L176).
      seen.set(option.categoryId, option.valueId);
    }
    for (const [categoryId, valueId] of seen) {
      const [value] = await tx
        .select({ id: tailoringOptionValues.id })
        .from(tailoringOptionValues)
        .where(
          and(
            eq(tailoringOptionValues.tenantId, tenantId),
            eq(tailoringOptionValues.id, valueId),
            eq(tailoringOptionValues.categoryId, categoryId),
            isNull(tailoringOptionValues.deletedAt),
          ),
        )
        .limit(1);
      if (!value) throw new DomainError('TAILORING_VALUE_NOT_FOUND', 'الخيار غير موجود', 404);
    }
    await tx.delete(tailoringOrderOptions).where(and(eq(tailoringOrderOptions.tenantId, tenantId), eq(tailoringOrderOptions.orderId, orderId)));
    for (const [categoryId, valueId] of seen) {
      await tx.insert(tailoringOrderOptions).values({
        id: newId(),
        tenantId,
        orderId,
        categoryId,
        valueId,
        createdBy: userId ?? null,
      });
    }
  }
}
