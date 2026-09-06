export class DomainError extends Error {
  public readonly code: string;
  public readonly status: number;
  public readonly details?: unknown;

  constructor(code: string, message: string, status = 500, details?: unknown) {
    super(message);
    this.name = 'DomainError';
    this.code = code;
    this.status = status;
    this.details = details;
  }
}

export type ProblemError = {
  field?: string;
  message?: string;
};

export type ProblemDetails = {
  type: string;
  title: string;
  status: number;
  code: string;
  detail: string;
  traceId: string;
  errors?: ProblemError[];
};

export function createProblemDetails(error: DomainError | Error, traceId: string): ProblemDetails {
  const domainError =
    error instanceof DomainError ? error : new DomainError('INTERNAL_SERVER_ERROR', error.message, 500);

  return {
    type: 'about:blank',
    title: 'Request failed',
    status: domainError.status,
    code: domainError.code,
    detail: domainError.message,
    traceId,
    errors:
      domainError.details && typeof domainError.details === 'object'
        ? Array.isArray(domainError.details)
          ? domainError.details as ProblemError[]
          : [domainError.details as ProblemError]
        : undefined,
  };
}

export const contractVersion = '0.1.0';
