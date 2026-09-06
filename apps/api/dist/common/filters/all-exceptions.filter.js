var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
import { Catch, HttpException, HttpStatus } from '@nestjs/common';
import { createProblemDetails, DomainError } from '@erp/contracts';
let AllExceptionsFilter = class AllExceptionsFilter {
    catch(exception, host) {
        const ctx = host.switchToHttp();
        const response = ctx.getResponse();
        const request = ctx.getRequest();
        const traceId = request.headers['x-request-id'] ?? 'unknown';
        if (exception instanceof HttpException) {
            const status = exception.getStatus();
            const payload = exception.getResponse();
            const problem = createProblemDetails(new DomainError('HTTP_EXCEPTION', typeof payload === 'string' ? payload : 'Request failed', status), String(traceId));
            response.status(status).json(problem);
            return;
        }
        const problem = createProblemDetails(new DomainError('INTERNAL_SERVER_ERROR', 'An unexpected error occurred', HttpStatus.INTERNAL_SERVER_ERROR), String(traceId));
        response.status(HttpStatus.INTERNAL_SERVER_ERROR).json(problem);
    }
};
AllExceptionsFilter = __decorate([
    Catch()
], AllExceptionsFilter);
export { AllExceptionsFilter };
