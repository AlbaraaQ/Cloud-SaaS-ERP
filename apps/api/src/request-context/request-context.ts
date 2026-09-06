import { AsyncLocalStorage } from 'node:async_hooks';

import { DomainError, errorCodes } from '@erp/contracts';

/**
 * Per-request context propagated with `AsyncLocalStorage` (API_ARCHITECTURE §2 first
 * pipeline stage, PROJECT_CONTRACT §10 `traceId` propagation).
 *
 * The store object is created once by `RequestIdMiddleware` and then *mutated* by the
 * guards further down the pipeline, which is why `AuthGuard`/`TenantGuard` can publish
 * their results without owning the ALS scope themselves.
 */

export type AuthContextValue = {
  /** `sub` — user id. */
  userId: string;
  /** `tid` — tenant id claimed by the token (verified by TenantGuard). */
  claimedTenantId: string;
  /** `mid` — membership id. */
  membershipId: string;
  /** `scope` — token scopes. */
  scope: string[];
  /** `jti` — token id, used for logout. */
  tokenId: string;
  isPlatformAdmin: boolean;
};

export type TenantContextValue = {
  tenantId: string;
  tenantCode: string;
  tenantStatus: string;
  membershipId: string;
  userId: string;
  /** Effective permission set = UNION(roles) (DATABASE_DESIGN §2). `*` = owner. */
  permissions: string[];
  /** NULL = all branches (MULTI_TENANCY §2). */
  branchScope: string[] | null;
  isOwner: boolean;
};

export type RequestContextValue = {
  traceId: string;
  startTime: number;
  auth?: AuthContextValue;
  tenant?: TenantContextValue;
  /** Validated `X-Branch-Id` request scope. */
  branchId?: string;
  /** Client address, captured for `audit_log.meta` (SECURITY_ARCHITECTURE §10). */
  clientIp?: string;
  /** Truncated `User-Agent`, captured for `audit_log.meta`. */
  userAgent?: string;
  /**
   * Set by a service that has already written a richer `audit_log` row (with a real
   * `before`/`after` diff) for this request, so `AuditInterceptor` does not add a
   * second, poorer one.
   */
  audited?: boolean;
};

export const requestContextStorage = new AsyncLocalStorage<RequestContextValue>();

const FALLBACK: RequestContextValue = {
  traceId: 'local-request',
  startTime: 0,
};

export function getRequestContext(): RequestContextValue {
  return requestContextStorage.getStore() ?? FALLBACK;
}

export function getTraceId(): string {
  return getRequestContext().traceId;
}

export function tryGetAuthContext(): AuthContextValue | undefined {
  return getRequestContext().auth;
}

/** Throws `UNAUTHENTICATED` when AuthGuard did not run (a public route, or a wiring bug). */
export function getAuthContext(): AuthContextValue {
  const auth = getRequestContext().auth;
  if (!auth) {
    throw new DomainError(errorCodes.UNAUTHENTICATED, 'Authentication required', 401);
  }
  return auth;
}

/** Mutates the active store; safe to call from guards running inside the ALS scope. */
export function setAuthContext(value: AuthContextValue): void {
  const store = requestContextStorage.getStore();
  if (store) store.auth = value;
}

export function setTenantContextValue(value: TenantContextValue): void {
  const store = requestContextStorage.getStore();
  if (store) store.tenant = value;
}

export function setBranchId(branchId: string): void {
  const store = requestContextStorage.getStore();
  if (store) store.branchId = branchId;
}

/** PHASE_04 — `audit_log.meta` provenance, published by `RequestIdMiddleware`. */
export function setClientMetadata(value: { clientIp?: string; userAgent?: string }): void {
  const store = requestContextStorage.getStore();
  if (!store) return;
  if (value.clientIp) store.clientIp = value.clientIp;
  if (value.userAgent) store.userAgent = value.userAgent;
}

/**
 * Marks the current request as already audited by a service that produced a real
 * before/after diff — `AuditInterceptor` then skips its generic row.
 */
export function markRequestAudited(): void {
  const store = requestContextStorage.getStore();
  if (store) store.audited = true;
}

export function isRequestAudited(): boolean {
  return getRequestContext().audited === true;
}
