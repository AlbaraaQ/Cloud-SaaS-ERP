/**
 * `@erp/contracts` — the API↔UI shared contract package (TARGET_ARCHITECTURE §3):
 * zod DTOs, the stable error-code registry, permission codes, pagination and id rules.
 */
export { errorCodes, errorStatus, errorTitle, isErrorCode, statusForCode, titleForCode } from './errors.js';
export type { ErrorCode } from './errors.js';

export { DomainError, createProblemDetails, problemFromZodError } from './problem.js';
export type { ProblemDetails, ProblemError } from './problem.js';

export {
  DEFAULT_PAGE_LIMIT,
  MAX_PAGE_LIMIT,
  buildMeta,
  listEnvelope,
  paginationQuerySchema,
  parseFilters,
  parseSort,
} from './pagination.js';
export type { ListEnvelope, ListMeta, PaginationQuery, SortClause, SortDirection } from './pagination.js';

export { idParamSchema, isUuid, newId, uuidSchema } from './ids.js';
export type { IdParam } from './ids.js';

export {
  AUTHORIZATION_HEADER,
  BRANCH_ID_HEADER,
  IDEMPOTENCY_KEY_HEADER,
  REQUEST_ID_HEADER,
  newRequestId,
} from './request-id.js';
export type { RequestId } from './request-id.js';

export {
  ALL_PERMISSIONS,
  findPermission,
  isKnownPermissionCode,
  permissionModules,
  permissionRegistry,
  permissionsForModule,
} from './permissions.js';
export type { PermissionDefinition } from './permissions.js';

export * from './platform/index.js';

export const contractVersion = '0.3.0';
