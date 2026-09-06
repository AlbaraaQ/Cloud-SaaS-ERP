import { AsyncLocalStorage } from 'node:async_hooks';
export const requestContextStorage = new AsyncLocalStorage();
export function getTraceId() {
    return requestContextStorage.getStore()?.traceId ?? 'unknown';
}
