import { Inject, Injectable } from '@nestjs/common';
import { sql } from 'drizzle-orm';
import type { DatabaseHandle } from '@erp/database';

import { DATABASE_HANDLE } from '../../../database/database.tokens.js';
import { getTenantContext } from '../context/tenant-context.js';
import { getAuthContext } from '../../../request-context/request-context.js';

export type BillingPlan = {
  id: string;
  code: string;
  name: string;
  interval: 'month' | 'year';
  amount: string;
  currency: string;
  stripePriceId: string | null;
};

@Injectable()
export class BillingService {
  constructor(@Inject(DATABASE_HANDLE) private readonly database: DatabaseHandle) {}

  async listActivePlans(): Promise<BillingPlan[]> {
    const result = await this.database.db.execute(sql`
      SELECT id, code, name, interval, amount::text, currency, stripe_price_id
      FROM billing_plans WHERE active = true
      ORDER BY amount ASC, interval ASC, code ASC
    `);
    return result.rows.map((row) => ({
      id: String(row.id), code: String(row.code), name: String(row.name),
      interval: row.interval === 'year' ? 'year' : 'month', amount: String(row.amount),
      currency: String(row.currency), stripePriceId: row.stripe_price_id ? String(row.stripe_price_id) : null,
    }));
  }

  async requestManualActivation(planId: string, notes?: string) {
    const tenant = getTenantContext();
    const auth = getAuthContext();
    const result = await this.database.db.execute(sql`
      INSERT INTO activation_requests (id, tenant_id, requested_by, plan_id, notes)
      SELECT gen_random_uuid(), ${tenant.tenantId}::uuid, ${auth.userId}::uuid, id, ${notes ?? null}
      FROM billing_plans WHERE id = ${planId}::uuid AND active = true
      RETURNING id, tenant_id, plan_id, status, notes, created_at
    `);
    if (!result.rows[0]) throw new Error('Active billing plan was not found');
    return result.rows[0];
  }

  async mySubscription() {
    const tenant = getTenantContext();
    const result = await this.database.db.execute(sql`
      SELECT s.id, s.status, s.provider, s.current_period_start, s.current_period_end,
             s.activated_at, p.code AS plan_code, p.name AS plan_name,
             p.amount::text, p.currency
      FROM tenant_subscriptions s JOIN billing_plans p ON p.id = s.plan_id
      WHERE s.tenant_id = ${tenant.tenantId}::uuid
      ORDER BY s.created_at DESC LIMIT 1
    `);
    return result.rows[0] ?? null;
  }

  async listActivationRequests() {
    if (!getAuthContext().isPlatformAdmin) throw new Error('Platform administrator access required');
    const result = await this.database.db.execute(sql`
      SELECT r.id, r.tenant_id, r.plan_id, r.status, r.notes, r.created_at,
             t.code AS tenant_code, t.name AS tenant_name, p.name AS plan_name, p.amount::text
      FROM activation_requests r JOIN tenants t ON t.id = r.tenant_id
      LEFT JOIN billing_plans p ON p.id = r.plan_id
      WHERE r.status = 'pending' ORDER BY r.created_at ASC
    `);
    return result.rows;
  }

  async reviewActivation(requestId: string, approve: boolean, notes?: string) {
    const auth = getAuthContext();
    if (!auth.isPlatformAdmin) throw new Error('Platform administrator access required');
    const result = await this.database.db.execute(sql`
      WITH reviewed AS (
        UPDATE activation_requests SET status = ${approve ? 'approved' : 'rejected'},
          reviewed_by = ${auth.userId}::uuid, reviewed_at = now(), notes = COALESCE(${notes ?? null}, notes), updated_at = now()
        WHERE id = ${requestId}::uuid AND status = 'pending'
        RETURNING id, tenant_id, plan_id, status
      )
      SELECT * FROM reviewed
    `);
    const request = result.rows[0];
    if (!request) throw new Error('Pending activation request was not found');
    if (approve) {
      await this.database.db.execute(sql`
        INSERT INTO tenant_subscriptions (id, tenant_id, plan_id, status, provider, activated_at, current_period_start)
        VALUES (gen_random_uuid(), ${request.tenant_id}::uuid, ${request.plan_id}::uuid, 'active', 'manual', now(), now())
      `);
    }
    return request;
  }
}
