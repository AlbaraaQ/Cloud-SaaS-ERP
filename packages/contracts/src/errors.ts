/**
 * Stable error-code registry — API_CONTRACT §0 ("Stable error codes (seed registry,
 * extend only)"). Every 4xx/5xx response carries one of these codes
 * (PROJECT_CONTRACT §10). Add codes at the end; never rename or remove one.
 */
export const errorCodes = {
  UNAUTHENTICATED: 'UNAUTHENTICATED',
  FORBIDDEN: 'FORBIDDEN',
  TENANT_SUSPENDED: 'TENANT_SUSPENDED',
  TENANT_CONTEXT_MISSING: 'TENANT_CONTEXT_MISSING',
  VALIDATION_FAILED: 'VALIDATION_FAILED',
  FILTER_NOT_ALLOWED: 'FILTER_NOT_ALLOWED',
  NOT_FOUND: 'NOT_FOUND',
  VERSION_CONFLICT: 'VERSION_CONFLICT',
  IDEMPOTENCY_REPLAY: 'IDEMPOTENCY_REPLAY',
  ACCOUNT_NOT_POSTABLE: 'ACCOUNT_NOT_POSTABLE',
  JOURNAL_UNBALANCED: 'JOURNAL_UNBALANCED',
  ACCOUNTING_PERIOD_CLOSED: 'ACCOUNTING_PERIOD_CLOSED',
  ACCOUNTING_PERIOD_LOCKED_MODULE: 'ACCOUNTING_PERIOD_LOCKED_MODULE',
  DOCUMENT_ALREADY_POSTED: 'DOCUMENT_ALREADY_POSTED',
  DOCUMENT_NOT_DRAFT: 'DOCUMENT_NOT_DRAFT',
  PARTY_CREDIT_LIMIT_EXCEEDED: 'PARTY_CREDIT_LIMIT_EXCEEDED',
  STOCK_INSUFFICIENT: 'STOCK_INSUFFICIENT',
  SEQUENCE_EXHAUSTED: 'SEQUENCE_EXHAUSTED',
  EINVOICE_REJECTED: 'EINVOICE_REJECTED',
  MIGRATION_CONFLICT: 'MIGRATION_CONFLICT',
  RATE_LIMITED: 'RATE_LIMITED',
  INTERNAL: 'INTERNAL',
  // PHASE_05 §7 — no posting profile answers a (branch, doc_type) lookup.
  ACCOUNT_PROFILE_MISSING: 'ACCOUNT_PROFILE_MISSING',
} as const;

export type ErrorCode = (typeof errorCodes)[keyof typeof errorCodes];

export function isErrorCode(value: string): value is ErrorCode {
  return Object.prototype.hasOwnProperty.call(errorCodes, value);
}

/** HTTP status each stable code maps to. RFC 9457 `status` member. */
export const errorStatus: Record<ErrorCode, number> = {
  UNAUTHENTICATED: 401,
  FORBIDDEN: 403,
  TENANT_SUSPENDED: 423,
  TENANT_CONTEXT_MISSING: 400,
  VALIDATION_FAILED: 400,
  FILTER_NOT_ALLOWED: 400,
  NOT_FOUND: 404,
  VERSION_CONFLICT: 409,
  IDEMPOTENCY_REPLAY: 409,
  ACCOUNT_NOT_POSTABLE: 422,
  JOURNAL_UNBALANCED: 422,
  ACCOUNTING_PERIOD_CLOSED: 423,
  ACCOUNTING_PERIOD_LOCKED_MODULE: 423,
  DOCUMENT_ALREADY_POSTED: 409,
  DOCUMENT_NOT_DRAFT: 409,
  PARTY_CREDIT_LIMIT_EXCEEDED: 422,
  STOCK_INSUFFICIENT: 422,
  SEQUENCE_EXHAUSTED: 422,
  EINVOICE_REJECTED: 422,
  MIGRATION_CONFLICT: 409,
  RATE_LIMITED: 429,
  INTERNAL: 500,
  ACCOUNT_PROFILE_MISSING: 422,
};

/** RFC 9457 `title` member for each stable code. */
export const errorTitle: Record<ErrorCode, string> = {
  UNAUTHENTICATED: 'Unauthenticated',
  FORBIDDEN: 'Forbidden',
  TENANT_SUSPENDED: 'Tenant suspended',
  TENANT_CONTEXT_MISSING: 'Tenant context missing',
  VALIDATION_FAILED: 'Validation failed',
  FILTER_NOT_ALLOWED: 'Filter not allowed',
  NOT_FOUND: 'Not found',
  VERSION_CONFLICT: 'Version conflict',
  IDEMPOTENCY_REPLAY: 'Idempotency replay',
  ACCOUNT_NOT_POSTABLE: 'Account not postable',
  JOURNAL_UNBALANCED: 'Journal unbalanced',
  ACCOUNTING_PERIOD_CLOSED: 'Accounting period closed',
  ACCOUNTING_PERIOD_LOCKED_MODULE: 'Accounting period locked for module',
  DOCUMENT_ALREADY_POSTED: 'Document already posted',
  DOCUMENT_NOT_DRAFT: 'Document not draft',
  PARTY_CREDIT_LIMIT_EXCEEDED: 'Party credit limit exceeded',
  STOCK_INSUFFICIENT: 'Insufficient stock',
  SEQUENCE_EXHAUSTED: 'Sequence exhausted',
  EINVOICE_REJECTED: 'E-invoice rejected',
  MIGRATION_CONFLICT: 'Migration conflict',
  RATE_LIMITED: 'Rate limited',
  INTERNAL: 'Internal error',
  ACCOUNT_PROFILE_MISSING: 'Posting profile missing',
};

export function statusForCode(code: string): number {
  return isErrorCode(code) ? errorStatus[code] : 500;
}

export function titleForCode(code: string): string {
  return isErrorCode(code) ? errorTitle[code] : 'Request failed';
}
