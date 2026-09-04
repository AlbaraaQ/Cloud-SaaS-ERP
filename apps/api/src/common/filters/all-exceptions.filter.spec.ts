import { BadRequestException } from '@nestjs/common';
import { Test } from '@nestjs/testing';
import { describe, expect, it } from 'vitest';

import { createProblemDetails, DomainError } from '@erp/contracts';

import { AllExceptionsFilter } from './all-exceptions.filter.js';

describe('AllExceptionsFilter', () => {
  it('maps bad request exceptions to problem+json payloads', () => {
    const filter = new AllExceptionsFilter();
    const problem = createProblemDetails(new DomainError('VALIDATION_FAILED', 'bad payload', 400), 'trace-1');

    expect(problem.code).toBe('VALIDATION_FAILED');
    expect(problem.status).toBe(400);
    expect(problem.traceId).toBe('trace-1');
    expect(filter).toBeInstanceOf(AllExceptionsFilter);
    expect(new BadRequestException('bad payload')).toBeInstanceOf(BadRequestException);
  });
});
