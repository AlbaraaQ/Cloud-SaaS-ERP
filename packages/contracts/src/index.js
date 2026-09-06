export class DomainError extends Error {
    code;
    status;
    details;
    constructor(code, message, status = 500, details) {
        super(message);
        this.name = 'DomainError';
        this.code = code;
        this.status = status;
        this.details = details;
    }
}
export function createProblemDetails(error, traceId) {
    const domainError = error instanceof DomainError ? error : new DomainError('INTERNAL_SERVER_ERROR', error.message, 500);
    return {
        type: 'about:blank',
        title: 'Request failed',
        status: domainError.status,
        code: domainError.code,
        detail: domainError.message,
        traceId,
        errors: domainError.details && typeof domainError.details === 'object'
            ? Array.isArray(domainError.details)
                ? domainError.details
                : [domainError.details]
            : undefined,
    };
}
export const contractVersion = '0.1.0';
