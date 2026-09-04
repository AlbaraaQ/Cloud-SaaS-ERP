import {
  ArgumentsHost,
  Catch,
  ExceptionFilter,
  HttpException,
  HttpStatus,
} from '@nestjs/common';
import { Response } from 'express';

import { createProblemDetails, DomainError } from '@erp/contracts';

@Catch()
export class AllExceptionsFilter implements ExceptionFilter {
  catch(exception: unknown, host: ArgumentsHost) {
    const ctx = host.switchToHttp();
    const response = ctx.getResponse<Response>();
    const request = ctx.getRequest();
    const traceId = request.headers['x-request-id'] ?? 'unknown';

    if (exception instanceof HttpException) {
      const status = exception.getStatus();
      const payload = exception.getResponse();
      const problem = createProblemDetails(
        new DomainError('HTTP_EXCEPTION', typeof payload === 'string' ? payload : 'Request failed', status),
        String(traceId),
      );
      response.status(status).json(problem);
      return;
    }

    const problem = createProblemDetails(
      new DomainError(
        'INTERNAL_SERVER_ERROR',
        'An unexpected error occurred',
        HttpStatus.INTERNAL_SERVER_ERROR,
      ),
      String(traceId),
    );
    response.status(HttpStatus.INTERNAL_SERVER_ERROR).json(problem);
  }
}
