import { AsyncLocalStorage } from 'node:async_hooks';

export type RequestContextValue = {
  traceId: string;
  tenantId?: string;
  startTime: number;
};

export const requestContextStorage = new AsyncLocalStorage<RequestContextValue>();

export function getRequestContext(): RequestContextValue {
  return (
    requestContextStorage.getStore() ?? {
      traceId: 'local-request',
      startTime: Date.now(),
    }
  );
}

export function getTraceId(): string {
  return getRequestContext().traceId;
}
