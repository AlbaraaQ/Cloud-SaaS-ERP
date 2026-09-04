import { describe, expect, it } from 'vitest';
import { BadRequestException, NotFoundException } from '@nestjs/common';
import type { ZodError } from 'zod';
import { z } from 'zod';
import { DomainError, errorCodes } from '@erp/contracts';

import { ZodValidationException } from '../pipes/zod-validation.pipe.js';

import { AllExceptionsFilter, codeForStatus } from './all-exceptions.filter.js';

type SentResponse = { status: number; contentType: string; body: string };

function runFilter(exception: unknown): SentResponse {
  const filter = new AllExceptionsFilter();
  let sent: SentResponse | undefined;

  const response = {
    status(code: number) {
      sent = { status: code, contentType: '', body: '' };
      return {
        type(value: string) {
          if (sent) sent.contentType = value;
          return {
            send(payload: string) {
              if (sent) sent.body = payload;
              return this;
            },
          };
        },
      };
    },
  };

  const host = {
    switchToHttp: () => ({
      getResponse: () => response,
      getRequest: () => ({ headers: { 'x-request-id': 'trace-abc' } }),
    }),
  };

  filter.catch(exception, host as never);
  if (!sent) throw new Error('filter did not respond');
  return sent;
}

describe('AllExceptionsFilter', () => {
  it('emits application/problem+json with the stable code and traceId', () => {
    const sent = runFilter(
      new DomainError(errorCodes.FORBIDDEN, 'permission sales.invoice.post required', 403),
    );
    const body = JSON.parse(sent.body) as Record<string, unknown>;

    expect(sent.contentType).toBe('application/problem+json');
    expect(sent.status).toBe(403);
    expect(body).toMatchObject({
      type: 'about:blank',
      title: 'Forbidden',
      status: 403,
      code: 'FORBIDDEN',
      detail: 'permission sales.invoice.post required',
      traceId: 'trace-abc',
    });
  });

  it('maps zod failures to VALIDATION_FAILED with a field list', () => {
    const result = z.object({ email: z.string().email() }).safeParse({ email: 'nope' });
    expect(result.success).toBe(false);
    const sent = runFilter(new ZodValidationException(result.error as ZodError));
    const body = JSON.parse(sent.body) as { code: string; status: number; errors: unknown[] };

    expect(body.code).toBe('VALIDATION_FAILED');
    expect(body.status).toBe(400);
    expect(body.errors).toEqual([{ field: 'email', message: expect.any(String) }]);
  });

  it('maps Nest HTTP exceptions onto the registry and never leaks internals on 5xx', () => {
    const notFound = JSON.parse(runFilter(new NotFoundException('nope')).body) as Record<string, unknown>;
    expect(notFound).toMatchObject({ status: 404, code: 'NOT_FOUND' });

    const badRequest = JSON.parse(runFilter(new BadRequestException('bad')).body) as Record<string, unknown>;
    expect(badRequest).toMatchObject({ status: 400, code: 'VALIDATION_FAILED' });

    const boom = JSON.parse(runFilter(new Error('connection string leaked')).body) as Record<string, unknown>;
    expect(boom).toMatchObject({ status: 500, code: 'INTERNAL', detail: 'An unexpected error occurred' });
    expect(JSON.stringify(boom)).not.toContain('connection string leaked');
  });

  it('maps statuses to stable codes', () => {
    expect(codeForStatus(401)).toBe('UNAUTHENTICATED');
    expect(codeForStatus(403)).toBe('FORBIDDEN');
    expect(codeForStatus(404)).toBe('NOT_FOUND');
    expect(codeForStatus(409)).toBe('VERSION_CONFLICT');
    expect(codeForStatus(429)).toBe('RATE_LIMITED');
    expect(codeForStatus(422)).toBe('VALIDATION_FAILED');
    expect(codeForStatus(503)).toBe('INTERNAL');
  });

  it('never puts a stack trace into the response body', () => {
    const error = new Error('secret detail');
    const sent = runFilter(error);
    expect(sent.body).not.toContain('stack');
    expect(sent.body).not.toContain('secret detail');
  });
});
