import { Inject, Injectable, Logger } from '@nestjs/common';
import { and, eq, isNull } from 'drizzle-orm';
import {
  branches,
  cashLocationBalances,
  cashLocations,
  currencies,
  newId,
  priceLists,
  tenants,
  warehouses,
  withTenantTx,
  type DatabaseHandle,
  type DrizzleTx,
} from '@erp/database';

import { DATABASE_HANDLE } from '../../../database/database.module.js';

/**
 * `provisionOrgDefaults(tenantId)` — PHASE_05 §5.7.
 *
 * A tenant that exists but has no branch cannot be used: documents cannot be numbered
 * (`document_sequences` is scoped by branch), stock has nowhere to live and money has
 * nowhere to sit. This service creates that minimum — base currency, main branch, main
 * warehouse, main safe with a zero balance, default price list — and is the seam the
 * PHASE_03 tenant factory and the PHASE_15 migrator both call.
 *
 * It is **idempotent**: every step is skipped when its row already exists, so calling it
 * twice (a retried provisioning job, a re-run import) is safe and returns the same ids.
 * It deliberately writes through the tables rather than through the CRUD services: it
 * runs outside any HTTP request, so there is no branch scope, no actor and no audit
 * interceptor to satisfy.
 */

export type OrgDefaults = {
  tenantId: string;
  branchId: string;
  warehouseId: string;
  cashLocationId: string;
  priceListId: string;
  currencyCode: string;
  /** False when everything already existed — the call was a no-op. */
  created: boolean;
};

export type ProvisionOptions = {
  actorUserId?: string | null;
  /** Defaults to `MAIN`; the migrator overrides it to match the legacy code. */
  code?: string;
  nameAr?: string;
  nameEn?: string;
};

const DEFAULT_CODE = 'MAIN';

@Injectable()
export class OrgProvisioningService {
  private readonly logger = new Logger(OrgProvisioningService.name);

  constructor(@Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle) {}

  async provisionOrgDefaults(tenantId: string, options: ProvisionOptions = {}): Promise<OrgDefaults> {
    return withTenantTx(this.database.db, tenantId, (tx) =>
      this.provisionInTx(tx, tenantId, options),
    );
  }

  /** Same work inside a caller's transaction — the tenant factory creates both at once. */
  async provisionInTx(
    tx: DrizzleTx,
    tenantId: string,
    options: ProvisionOptions = {},
  ): Promise<OrgDefaults> {
    const actorUserId = options.actorUserId ?? null;
    const now = new Date();
    const code = (options.code ?? DEFAULT_CODE).toUpperCase();
    const nameAr = options.nameAr ?? 'الفرع الرئيسي';
    const nameEn = options.nameEn ?? 'Main branch';
    let created = false;

    const currencyCode = await this.ensureBaseCurrency(tx, tenantId, actorUserId, now);

    let branchId = await firstId(
      tx
        .select({ id: branches.id })
        .from(branches)
        .where(and(eq(branches.tenantId, tenantId), eq(branches.isDefault, true), isNull(branches.deletedAt)))
        .limit(1),
    );
    if (!branchId) {
      branchId = newId();
      await tx.insert(branches).values({
        id: branchId,
        tenantId,
        code,
        nameAr,
        nameEn,
        isDefault: true,
        isActive: true,
        createdAt: now,
        createdBy: actorUserId,
      });
      created = true;
    }

    let warehouseId = await firstId(
      tx
        .select({ id: warehouses.id })
        .from(warehouses)
        .where(
          and(
            eq(warehouses.tenantId, tenantId),
            eq(warehouses.isDefault, true),
            isNull(warehouses.deletedAt),
          ),
        )
        .limit(1),
    );
    if (!warehouseId) {
      warehouseId = newId();
      await tx.insert(warehouses).values({
        id: warehouseId,
        tenantId,
        branchId,
        code,
        name: nameEn,
        isDefault: true,
        isActive: true,
        createdAt: now,
        createdBy: actorUserId,
      });
      created = true;
    }

    let cashLocationId = await firstId(
      tx
        .select({ id: cashLocations.id })
        .from(cashLocations)
        .where(
          and(
            eq(cashLocations.tenantId, tenantId),
            eq(cashLocations.kind, 'safe'),
            eq(cashLocations.isDefault, true),
            isNull(cashLocations.deletedAt),
          ),
        )
        .limit(1),
    );
    if (!cashLocationId) {
      cashLocationId = newId();
      await tx.insert(cashLocations).values({
        id: cashLocationId,
        tenantId,
        branchId,
        kind: 'safe',
        name: 'Main safe',
        // account_id stays NULL until PHASE_07 creates the chart of accounts (CR-006).
        accountId: null,
        currencyCode: null,
        isDefault: true,
        isActive: true,
        createdAt: now,
        createdBy: actorUserId,
      });
      created = true;
    }

    // PHASE_05 §11: a provisioned tenant's safe reports a balance, and it is zero.
    await tx
      .insert(cashLocationBalances)
      .values({ tenantId, cashLocationId, currencyCode, balance: '0' })
      .onConflictDoNothing();

    let priceListId = await firstId(
      tx
        .select({ id: priceLists.id })
        .from(priceLists)
        .where(
          and(
            eq(priceLists.tenantId, tenantId),
            eq(priceLists.isDefault, true),
            isNull(priceLists.deletedAt),
          ),
        )
        .limit(1),
    );
    if (!priceListId) {
      priceListId = newId();
      await tx.insert(priceLists).values({
        id: priceListId,
        tenantId,
        name: 'Default price list',
        currencyCode,
        isDefault: true,
        isActive: true,
        createdAt: now,
        createdBy: actorUserId,
      });
      created = true;
    }

    if (created) {
      this.logger.log({ tenantId, branchId, warehouseId, cashLocationId }, 'organization defaults provisioned');
    }

    return { tenantId, branchId, warehouseId, cashLocationId, priceListId, currencyCode, created };
  }

  /**
   * The tenant record already names a base currency (PHASE_01 `tenants.base_currency`);
   * provisioning turns that string into an actual `currencies` row so FX and price lists
   * have something to reference.
   */
  private async ensureBaseCurrency(
    tx: DrizzleTx,
    tenantId: string,
    actorUserId: string | null,
    now: Date,
  ): Promise<string> {
    const [existingBase] = await tx
      .select({ code: currencies.code })
      .from(currencies)
      .where(and(eq(currencies.tenantId, tenantId), eq(currencies.isBase, true)))
      .limit(1);
    if (existingBase) return existingBase.code.trim();

    const [tenantRow] = await tx
      .select({ baseCurrency: tenants.baseCurrency })
      .from(tenants)
      .where(eq(tenants.id, tenantId))
      .limit(1);
    const code = (tenantRow?.baseCurrency ?? 'SAR').trim().toUpperCase();

    await tx
      .insert(currencies)
      .values({
        tenantId,
        code,
        nameAr: code,
        nameEn: code,
        minorUnits: code === 'KWD' || code === 'BHD' || code === 'OMR' ? 3 : 2,
        isBase: true,
        isActive: true,
        createdAt: now,
        createdBy: actorUserId,
      })
      .onConflictDoNothing();

    // A tenant may already have the currency enabled without it being base.
    await tx
      .update(currencies)
      .set({ isBase: true })
      .where(and(eq(currencies.tenantId, tenantId), eq(currencies.code, code)));

    return code;
  }
}

async function firstId(query: PromiseLike<Array<{ id: string }>>): Promise<string | undefined> {
  const rows = await query;
  return rows[0]?.id;
}
