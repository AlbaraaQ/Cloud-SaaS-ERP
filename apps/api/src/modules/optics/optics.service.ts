import { Inject, Injectable } from '@nestjs/common';
import { and, desc, eq, isNull } from 'drizzle-orm';
import { DomainError, newId } from '@erp/contracts';
import { opticalPrescriptions, tenantSettings, withTenantTx, type DatabaseHandle } from '@erp/database';

import { DATABASE_HANDLE } from '../../database/database.module.js';

export type OpticalPrescriptionInput = { partyId: string; invoiceLineId?: string; orientation?: string; rightEye?: Record<string, string>; leftEye?: Record<string, string>; otherGrid?: Record<string, string>; notes?: string };
@Injectable()
export class OpticsService {
  constructor(@Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle) {}
  async ensureEnabled(tenantId: string) { const [flag] = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(tenantSettings).where(and(eq(tenantSettings.tenantId, tenantId), eq(tenantSettings.key, 'pack.optics'))).limit(1)); if (flag && flag.value !== true && flag.value !== 'true') throw new DomainError('NOT_FOUND', 'Optics pack is disabled', 404); }
  async list(tenantId: string, partyId?: string) { await this.ensureEnabled(tenantId); return { data: await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(opticalPrescriptions).where(and(eq(opticalPrescriptions.tenantId, tenantId), partyId ? eq(opticalPrescriptions.partyId, partyId) : undefined, isNull(opticalPrescriptions.deletedAt))).orderBy(desc(opticalPrescriptions.createdAt))) }; }
  async create(tenantId: string, input: OpticalPrescriptionInput) { await this.ensureEnabled(tenantId); const [row] = await withTenantTx(this.database.db, tenantId, (tx) => tx.insert(opticalPrescriptions).values({ id: newId(), tenantId, partyId: input.partyId, invoiceLineId: input.invoiceLineId, orientation: input.orientation ?? 'distance', rightEye: input.rightEye ?? {}, leftEye: input.leftEye ?? {}, otherGrid: input.otherGrid ?? {}, notes: input.notes }).returning()); return row; }
  async invoicePrintSection(tenantId: string, invoiceLineId: string) { await this.ensureEnabled(tenantId); const rows = await withTenantTx(this.database.db, tenantId, (tx) => tx.select().from(opticalPrescriptions).where(and(eq(opticalPrescriptions.tenantId, tenantId), eq(opticalPrescriptions.invoiceLineId, invoiceLineId), isNull(opticalPrescriptions.deletedAt)))); return { title: 'Optical prescription', rows }; }
}
