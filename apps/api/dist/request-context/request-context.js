import { AsyncLocalStorage } from 'node:async_hooks';
export const requestContextStorage = new AsyncLocalStorage();
export function getRequestContext() {
    return (requestContextStorage.getStore() ?? {
        traceId: 'local-request',
        startTime: Date.now(),
    });
}
export function getTraceId() {
    return getRequestContext().traceId;
}
