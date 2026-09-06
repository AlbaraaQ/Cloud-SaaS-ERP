import { AsyncLocalStorage } from 'node:async_hooks';

export interface RequestContext {
  traceId: string;
  startTime: number;
}

export const requestContextStorage = new AsyncLocalStorage<RequestContext>();

export function getTraceId(): string {
  return requestContextStorage.getStore()?.traceId ?? 'unknown';
}
