import { randomUUID } from 'node:crypto';
import { requestContextStorage } from '../request-context/request-context.js';
export function RequestIdMiddleware(req, res, next) {
    const traceId = req.headers['x-request-id']?.toString() ?? randomUUID();
    req.headers['x-request-id'] = traceId;
    res.setHeader('x-request-id', traceId);
    requestContextStorage.run({ traceId, startTime: Date.now() }, () => next());
}
