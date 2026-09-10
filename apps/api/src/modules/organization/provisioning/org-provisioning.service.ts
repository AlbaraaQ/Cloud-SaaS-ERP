import { Inject, Injectable, Logger } from '@nestjs/common';
import { and, eq, inArray, isNull } from 'drizzle-orm';
import {
  accounts,
  branches,
  branchPostingProfiles,
  cashLocationBalances,
  cashLocations,
  currencies,
  DEMO_CHART_OF_ACCOUNTS,
  DEMO_POSTING_PROFILE,
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
    const mainCashAccountId = await this.ensureChartOfAccounts(tx, tenantId, actorUserId, now);
    if (mainCashAccountId) created = true;
    if (await this.ensurePostingProfile(tx, tenantId, actorUserId, now)) created = true;

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
        // The desktop chart is seeded above, so the safe posts to 1211001 from day one.
        accountId: mainCashAccountId,
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
   * Seeds the desktop default chart of accounts (`Accounts_Index`, 112 accounts + the
   * cloud COGS extension) for a tenant that has none yet.
   *
   * Idempotent: a tenant with any live account is left untouched, so re-running
   * provisioning never duplicates or renames the accountant's chart. Parents precede
   * children in `DEMO_CHART_OF_ACCOUNTS`, so one ordered pass resolves every `parent_id`,
   * `level` and ltree `path` exactly the way `AccountingService.create` would.
   *
   * Returns the id of 1211001 (الصندوق الرئيسي) so the main safe can post to it —
   * or `null` when the chart already existed (the safe keeps whatever link it has).
   */
  private async ensureChartOfAccounts(
    tx: DrizzleTx,
    tenantId: string,
    actorUserId: string | null,
    now: Date,
  ): Promise<string | null> {
    const [existing] = await tx
      .select({ id: accounts.id })
      .from(accounts)
      .where(and(eq(accounts.tenantId, tenantId), isNull(accounts.deletedAt)))
      .limit(1);
    if (existing) return null;

    const idByCode = new Map<string, string>();
    const pathByCode = new Map<string, string>();
    const levelByCode = new Map<string, number>();
    for (const account of DEMO_CHART_OF_ACCOUNTS) {
      const id = newId();
      const parentId = account.parent ? idByCode.get(account.parent) : undefined;
      const parentPath = account.parent ? pathByCode.get(account.parent) : undefined;
      const level = account.parent ? (levelByCode.get(account.parent) ?? 0) + 1 : 0;
      const path = parentPath ? `${parentPath}.${id}` : id;
      await tx.insert(accounts).values({
        id,
        tenantId,
        code: account.code,
        nameAr: account.nameAr,
        nameEn: account.nameEn ?? null,
        parentId: parentId ?? null,
        level,
        path,
        type: account.type,
        normalBalance: account.normalBalance ?? (account.type === 'asset' || account.type === 'expense' ? 'debit' : 'credit'),
        isPostable: account.postable !== false,
        allowManual: true,
        createdAt: now,
        createdBy: actorUserId,
        legacySource: 'desktop-erp',
        legacyId: `Accounts_Index:${account.code}`,
      });
      idByCode.set(account.code, id);
      pathByCode.set(account.code, path);
      levelByCode.set(account.code, level);
    }
    this.logger.log({ tenantId, count: idByCode.size }, 'desktop chart of accounts seeded');
    return idByCode.get('1211001') ?? null;
  }

  /**
   * Seeds the tenant-wide posting profile (`branch NULL`, doc `*`) that every
   * auto-posting engine resolves through `PostingProfilesService` — the cloud heir
   * of the desktop `SettingGeneral.*Acc` defaults.
   *
   * Idempotent: an existing `*` profile is left untouched (the accountant may have
   * customised it). Codes that resolve to no account are omitted rather than
   * guessed — posting then fails with a named key error instead of corrupting the
   * ledger. Returns whether a profile was created.
   */
  private async ensurePostingProfile(
    tx: DrizzleTx,
    tenantId: string,
    actorUserId: string | null,
    now: Date,
  ): Promise<boolean> {
    const [existing] = await tx
      .select({ id: branchPostingProfiles.id })
      .from(branchPostingProfiles)
      .where(
        and(
          eq(branchPostingProfiles.tenantId, tenantId),
          isNull(branchPostingProfiles.branchId),
          eq(branchPostingProfiles.docType, '*'),
        ),
      )
      .limit(1);
    if (existing) return false;

    const codes = [...new Set(Object.values(DEMO_POSTING_PROFILE))];
    const rows = await tx
      .select({ id: accounts.id, code: accounts.code })
      .from(accounts)
      .where(and(eq(accounts.tenantId, tenantId), isNull(accounts.deletedAt), inArray(accounts.code, codes)));
    const byCode = new Map(rows.map((row) => [row.code, row.id]));
    const mapping: Record<string, string | number> = { version: 1 };
    for (const [key, code] of Object.entries(DEMO_POSTING_PROFILE)) {
      const accountId = byCode.get(code);
      if (accountId) mapping[key] = accountId;
    }
    await tx.insert(branchPostingProfiles).values({
      id: newId(),
      tenantId,
      branchId: null,
      docType: '*',
      mapping,
      createdAt: now,
      createdBy: actorUserId,
    });
    this.logger.log({ tenantId, keys: Object.keys(mapping).length - 1 }, 'tenant posting profile seeded');
    return true;
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
