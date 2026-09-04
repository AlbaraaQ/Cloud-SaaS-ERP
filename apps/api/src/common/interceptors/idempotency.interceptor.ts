import { Injectable, type CallHandler, type ExecutionContext, type NestInterceptor } from '@nestjs/common';
import type { Request, Response } from 'express';
import { of, tap, type Observable } from 'rxjs';
import { IDEMPOTENCY_KEY_HEADER } from '@erp/contracts';

import { getRequestContext } from '../../request-context/request-context.js';

/**
 * `Idempotency-Key` skeleton — PHASE_02 §5.6.
 *
 * TODO(phase:04): persist to the `idempotency_keys` table (DATABASE_DESIGN §4) so replays
 * survive a restart and are shared across replicas. The wire contract is already the
 * final one: a replay returns the first response with `Idempotency-Replayed: true`.
 */

type CachedResponse = { status: number; body: unknown };

const TTL_MS = 24 * 60 * 60 * 1000;
const MUTATING = new Set(['POST', 'PUT', 'PATCH']);

@Injectable()
export class IdempotencyInterceptor implements NestInterceptor {
  private readonly store = new Map<string, { cachedAt: number; response: CachedResponse }>();

  intercept(context: ExecutionContext, next: CallHandler): Observable<unknown> {
    const request = context.switchToHttp().getRequest<Request>();
    const response = context.switchToHttp().getResponse<Response>();

    const key = request.headers[IDEMPOTENCY_KEY_HEADER];
    if (typeof key !== 'string' || key.length === 0 || !MUTATING.has(request.method)) {
      return next.handle();
    }

    const tenantId = getRequestContext().tenant?.tenantId ?? 'anonymous';
    const storeKey = `${tenantId}:${request.method}:${request.path}:${key}`;

    this.evictExpired();
    const existing = this.store.get(storeKey);
    if (existing) {
      response.setHeader('Idempotency-Replayed', 'true');
      response.status(existing.response.status);
      return of(existing.response.body);
    }

    return next.handle().pipe(
      tap((body) => {
        this.store.set(storeKey, {
          cachedAt: Date.now(),
          response: { status: response.statusCode, body },
        });
      }),
    );
  }

  private evictExpired(): void {
    const cutoff = Date.now() - TTL_MS;
    for (const [key, entry] of this.store) {
      if (entry.cachedAt < cutoff) this.store.delete(key);
    }
  }
}
