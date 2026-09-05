import { Inject, Injectable } from '@nestjs/common';
import { and, asc, count, eq, ilike, inArray, isNull, sql, type SQL } from 'drizzle-orm';
import {
  CASH_LOCATION_FILTERS,
  CASH_LOCATION_SORT_COLUMNS,
  bankDetailsSchema,
  buildMeta,
  maskIban,
  parseBooleanFilter,
  parseFilters,
  parseSort,
  type CashLocationBalanceDto,
  type CashLocationCreate,
  type CashLocationDto,
  type CashLocationUpdate,
  type ListEnvelope,
  type OrgListQuery,
} from '@erp/contracts';
import {
  cashLocationBalances,
  cashLocations,
  currencies,
  newId,
  tenants,
  withTenantTx,
  type CashLocation,
  type DatabaseHandle,
  type DrizzleTx,
} from '@erp/database';

import { DATABASE_HANDLE } from '../../../database/database.module.js';
import { getRequestContext, markRequestAudited } from '../../../request-context/request-context.js';
import { AuditService } from '../../platform-services/index.js';
import { assertBranchUsable } from '../warehouses/warehouses.service.js';
import {
  actorStamp,
  assertVersion,
  isoOf,
  isoOrNull,
  lockDefaultSwitch,
  notFound,
  validationFailed,
  visibleBranchIds,
} from '../shared/org-support.js';

/**
 * Cash locations — API_CONTRACT §3, DATABASE_DESIGN §5.
 *
 * `Safes` + `Banks` + `treasury` unified behind `kind` (DOMAIN_MODEL §3). Two rules go
 * beyond the usual master-data CRUD:
 *
 * - **Bank data is sensitive** (SECURITY_ARCHITECTURE §5). The IBAN is masked in list
 *   responses and only the single-row read returns it in full.
 * - **Every change is audited with a real before/after** (PHASE_05 §8), written inside
 *   the same transaction as the change itself, so "changed" and "audited" cannot come
 *   apart. The interceptor is told to stand down for the request.
 */
@Injectable()
export class CashLocationsService {
  constructor(
    @Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle,
    private readonly audit: AuditService,
  ) {}

  async list(tenantId: string, query: OrgListQuery): Promise<ListEnvelope<CashLocationDto>> {
    const filters = parseFilters(query.filter, CASH_LOCATION_FILTERS);
    const sort = parseSort(query.sort, CASH_LOCATION_SORT_COLUMNS);
    const scoped = visibleBranchIds();

    return withTenantTx(this.database.db, tenantId, async (tx) => {
      const conditions: SQL[] = [eq(cashLocations.tenantId, tenantId), isNull(cashLocations.deletedAt)];

      const isActive = parseBooleanFilter(filters.isActive);
      if (isActive !== undefined) conditions.push(eq(cashLocations.isActive, isActive));
      const isDefault = parseBooleanFilter(filters.isDefault);
      if (isDefault !== undefined) conditions.push(eq(cashLocations.isDefault, isDefault));
      if (filters.branchId) conditions.push(eq(cashLocations.branchId, filters.branchId));
      if (filters.kind) conditions.push(eq(cashLocations.kind, filters.kind));
      if (scoped) {
        conditions.push(scoped.length > 0 ? inArray(cashLocations.branchId, scoped) : sql`false`);
      }
      if (query.q) conditions.push(ilike(cashLocations.name, `%${query.q}%`));

      const where = and(...conditions);
      const [totalRow] = await tx.select({ value: count() }).from(cashLocations).where(where);

      const order =
        sort.length > 0
          ? sort.map((clause) => {
              const column =
                clause.column === 'kind'
                  ? cashLocations.kind
                  : clause.column === 'name'
                    ? cashLocations.name
                    : cashLocations.createdAt;
              return clause.direction === 'desc' ? sql`${column} DESC` : sql`${column} ASC`;
            })
          : [sql`${cashLocations.name} ASC`];

      const rows = await tx
        .select()
        .from(cashLocations)
        .where(where)
        .orderBy(...order)
        .limit(query.limit)
        .offset(query.offset);

      return {
        data: rows.map((row) => toCashLocationDto(row, { maskBankDetails: true })),
        meta: buildMeta(totalRow?.value ?? 0, query),
      };
    });
  }

  async read(tenantId: string, cashLocationId: string): Promise<CashLocationDto> {
    return withTenantTx(this.database.db, tenantId, async (tx) =>
      toCashLocationDto(await this.mustFind(tx, tenantId, cashLocationId)),
    );
  }

  async listBalances(tenantId: string, cashLocationId: string): Promise<CashLocationBalanceDto[]> {
    return withTenantTx(this.database.db, tenantId, async (tx) => {
      await this.mustFind(tx, tenantId, cashLocationId);
      const rows = await tx
        .select()
        .from(cashLocationBalances)
        .where(
          and(
            eq(cashLocationBalances.tenantId, tenantId),
            eq(cashLocationBalances.cashLocationId, cashLocationId),
          ),
        )
        .orderBy(asc(cashLocationBalances.currencyCode));

      return rows.map((row) => ({
        cashLocationId: row.cashLocationId,
        currencyCode: row.currencyCode.trim(),
        balance: row.balance,
        updatedAt: isoOrNull(row.updatedAt),
      }));
    });
  }

  async create(tenantId: string, input: CashLocationCreate): Promise<CashLocationDto> {
    const { actorUserId, now } = actorStamp();
    assertBankBlock(input.kind, input.bank ?? null);

    const created = await withTenantTx(this.database.db, tenantId, async (tx) => {
      await assertBranchUsable(tx, tenantId, input.branchId);
      const currencyCode = await resolveCurrency(tx, tenantId, input.currencyCode ?? null);

      const [existing] = await tx
        .select({ value: count() })
        .from(cashLocations)
        .where(
          and(
            eq(cashLocations.tenantId, tenantId),
            eq(cashLocations.kind, input.kind),
            isNull(cashLocations.deletedAt),
          ),
        );

      const wantsDefault = input.isDefault === true || (existing?.value ?? 0) === 0;
      if (wantsDefault) await this.clearDefault(tx, tenantId, input.kind);

      const cashLocationId = newId();
      await tx.insert(cashLocations).values({
        id: cashLocationId,
        tenantId,
        branchId: input.branchId,
        kind: input.kind,
        name: input.name,
        accountId: input.accountId ?? null,
        currencyCode: input.currencyCode ?? null,
        isDefault: wantsDefault,
        bank: input.bank ?? null,
        changeInPos: input.changeInPos ?? false,
        isActive: input.isActive ?? true,
        createdAt: now,
        createdBy: actorUserId,
      });

      // Seeds the balance row at zero — PHASE_12 writes it, a report reconciles it.
      await tx
        .insert(cashLocationBalances)
        .values({ tenantId, cashLocationId, currencyCode, balance: '0' })
        .onConflictDoNothing();

      const row = await this.mustFind(tx, tenantId, cashLocationId);
      await this.recordAudit(tx, tenantId, 'create', row, null);
      return row;
    });

    markRequestAudited();
    return toCashLocationDto(created);
  }

  async update(
    tenantId: string,
    cashLocationId: string,
    input: CashLocationUpdate,
  ): Promise<CashLocationDto> {
    const { actorUserId, now } = actorStamp();

    const updated = await withTenantTx(this.database.db, tenantId, async (tx) => {
      const existing = await this.mustFind(tx, tenantId, cashLocationId);
      assertVersion(existing.version, input.version);
      if (input.bank !== undefined) assertBankBlock(existing.kind, input.bank);

      if (input.branchId !== undefined && input.branchId !== existing.branchId) {
        await assertBranchUsable(tx, tenantId, input.branchId);
      }
      if (input.isDefault === true && !existing.isDefault) {
        await this.clearDefault(tx, tenantId, existing.kind);
      }
      if (input.isDefault === false && existing.isDefault) {
        throw validationFailed(
          'Promote another cash location to default instead of clearing the flag',
          'isDefault',
        );
      }
      if (input.isActive === false && existing.isDefault) {
        throw validationFailed('The default cash location cannot be deactivated', 'isActive');
      }

      const updates: Record<string, unknown> = {
        updatedAt: now,
        updatedBy: actorUserId,
        version: sql`${cashLocations.version} + 1`,
      };
      if (input.branchId !== undefined) updates.branchId = input.branchId;
      if (input.name !== undefined) updates.name = input.name;
      if (input.accountId !== undefined) updates.accountId = input.accountId;
      if (input.currencyCode !== undefined) updates.currencyCode = input.currencyCode;
      if (input.bank !== undefined) updates.bank = input.bank;
      if (input.changeInPos !== undefined) updates.changeInPos = input.changeInPos;
      if (input.isDefault !== undefined) updates.isDefault = input.isDefault;
      if (input.isActive !== undefined) updates.isActive = input.isActive;

      await tx.update(cashLocations).set(updates).where(eq(cashLocations.id, cashLocationId));

      if (input.currencyCode !== undefined) {
        const currencyCode = await resolveCurrency(tx, tenantId, input.currencyCode);
        await tx
          .insert(cashLocationBalances)
          .values({ tenantId, cashLocationId, currencyCode, balance: '0' })
          .onConflictDoNothing();
      }

      const row = await this.mustFind(tx, tenantId, cashLocationId);
      await this.recordAudit(tx, tenantId, 'update', row, existing);
      return row;
    });

    markRequestAudited();
    return toCashLocationDto(updated);
  }

  async remove(tenantId: string, cashLocationId: string): Promise<void> {
    const { actorUserId, now } = actorStamp();

    await withTenantTx(this.database.db, tenantId, async (tx) => {
      const existing = await this.mustFind(tx, tenantId, cashLocationId);
      if (existing.isDefault) {
        throw validationFailed('The default cash location cannot be deleted', 'id');
      }

      await tx
        .update(cashLocations)
        .set({
          deletedAt: now,
          deletedBy: actorUserId,
          isActive: false,
          updatedAt: now,
          updatedBy: actorUserId,
          version: sql`${cashLocations.version} + 1`,
        })
        .where(eq(cashLocations.id, cashLocationId));

      const row = await tx
        .select()
        .from(cashLocations)
        .where(eq(cashLocations.id, cashLocationId))
        .limit(1);
      await this.recordAudit(tx, tenantId, 'delete', row[0] ?? existing, existing);
    });

    markRequestAudited();
  }

  // --- internals ---------------------------------------------------------------

  private async mustFind(tx: DrizzleTx, tenantId: string, cashLocationId: string): Promise<CashLocation> {
    const [row] = await tx
      .select()
      .from(cashLocations)
      .where(
        and(
          eq(cashLocations.id, cashLocationId),
          eq(cashLocations.tenantId, tenantId),
          isNull(cashLocations.deletedAt),
        ),
      )
      .limit(1);
    if (!row) throw notFound('Cash location');

    const scoped = visibleBranchIds();
    if (scoped && !scoped.includes(row.branchId)) throw notFound('Cash location');
    return row;
  }

  private async clearDefault(tx: DrizzleTx, tenantId: string, kind: string): Promise<void> {
    await lockDefaultSwitch(tx, tenantId, `cash_locations:${kind}`);
    await tx
      .update(cashLocations)
      .set({ isDefault: false })
      .where(
        and(
          eq(cashLocations.tenantId, tenantId),
          eq(cashLocations.kind, kind),
          eq(cashLocations.isDefault, true),
          isNull(cashLocations.deletedAt),
        ),
      );
  }

  private async recordAudit(
    tx: DrizzleTx,
    tenantId: string,
    action: string,
    after: CashLocation,
    before: CashLocation | null,
  ): Promise<void> {
    const context = getRequestContext();
    await this.audit.recordInTx(tx, {
      tenantId,
      actorUserId: context.auth?.userId ?? null,
      membershipId: context.auth?.membershipId ?? null,
      action,
      entity: 'cash_location',
      entityId: after.id,
      before: before ? auditView(before) : null,
      after: action === 'delete' ? null : auditView(after),
      meta: { traceId: context.traceId ?? null },
    });
  }
}

/**
 * The audit projection of a cash location. The bank block is reduced to its masked IBAN:
 * an audit row is read by more people than the record itself, and SECURITY_ARCHITECTURE
 * §5 does not stop applying because the data moved into `audit_log`.
 */
function auditView(row: CashLocation): Record<string, unknown> {
  const bank = row.bank as { iban?: string; bankName?: string } | null;
  return {
    branchId: row.branchId,
    kind: row.kind,
    name: row.name,
    accountId: row.accountId,
    currencyCode: row.currencyCode?.trim() ?? null,
    isDefault: row.isDefault,
    changeInPos: row.changeInPos,
    isActive: row.isActive,
    bank: bank ? { bankName: bank.bankName ?? null, iban: bank.iban ? maskIban(bank.iban) : null } : null,
  };
}

function assertBankBlock(kind: string, bank: unknown): void {
  if (kind === 'safe') {
    if (bank !== null && bank !== undefined) {
      throw validationFailed('A safe cannot carry bank details', 'bank');
    }
    return;
  }
  if (bank === null || bank === undefined) {
    throw validationFailed('A bank cash location requires the bank block', 'bank');
  }
  const parsed = bankDetailsSchema.safeParse(bank);
  if (!parsed.success) {
    throw validationFailed(parsed.error.issues[0]?.message ?? 'Invalid bank details', 'bank');
  }
}

/** NULL currency means "the tenant's base currency" (DATABASE_DESIGN §5). */
async function resolveCurrency(
  tx: DrizzleTx,
  tenantId: string,
  currencyCode: string | null,
): Promise<string> {
  if (currencyCode) {
    const [row] = await tx
      .select({ code: currencies.code })
      .from(currencies)
      .where(and(eq(currencies.tenantId, tenantId), eq(currencies.code, currencyCode)))
      .limit(1);
    if (!row) throw validationFailed(`Currency '${currencyCode}' is not enabled for this tenant`, 'currencyCode');
    return row.code.trim();
  }

  const [tenantRow] = await tx
    .select({ baseCurrency: tenants.baseCurrency })
    .from(tenants)
    .where(eq(tenants.id, tenantId))
    .limit(1);
  return (tenantRow?.baseCurrency ?? 'SAR').trim();
}

export function toCashLocationDto(
  row: CashLocation,
  options: { maskBankDetails?: boolean } = {},
): CashLocationDto {
  const bank = (row.bank ?? null) as CashLocationDto['bank'];
  const projected =
    bank && options.maskBankDetails && bank.iban ? { ...bank, iban: maskIban(bank.iban) } : bank;

  return {
    id: row.id,
    branchId: row.branchId,
    kind: row.kind as CashLocationDto['kind'],
    name: row.name,
    accountId: row.accountId,
    currencyCode: row.currencyCode?.trim() ?? null,
    isDefault: row.isDefault,
    bank: projected,
    changeInPos: row.changeInPos,
    isActive: row.isActive,
    version: row.version,
    createdAt: isoOf(row.createdAt),
    updatedAt: isoOrNull(row.updatedAt),
  };
}
