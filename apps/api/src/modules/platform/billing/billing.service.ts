import { Inject, Injectable } from '@nestjs/common';
import { sql } from 'drizzle-orm';
import type { DatabaseHandle } from '@erp/database';

import { DATABASE_HANDLE } from '../../../database/database.tokens.js';

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
      FROM billing_plans
      WHERE active = true
      ORDER BY amount ASC, interval ASC, code ASC
    `);
    return result.rows.map((row) => ({
      id: String(row.id),
      code: String(row.code),
      name: String(row.name),
      interval: row.interval === 'year' ? 'year' : 'month',
      amount: String(row.amount),
      currency: String(row.currency),
      stripePriceId: row.stripe_price_id ? String(row.stripe_price_id) : null,
    }));
  }
}
