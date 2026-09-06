import { describe, expect, it } from 'vitest';
import { RequestIdMiddleware } from './request-id.middleware.js';
describe('RequestIdMiddleware', () => {
    it('adds or preserves a trace id', () => {
        const req = { headers: {} };
        const res = { setHeader: () => undefined };
        let nextCalled = false;
        RequestIdMiddleware(req, res, () => {
            nextCalled = true;
        });
        expect(nextCalled).toBe(true);
        expect(req.headers['x-request-id']).toBeDefined();
    });
});
