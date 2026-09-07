/* global RequestInit */
const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:3000/api/v1';
export async function portalFetch<T>(path: string, token?: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, { ...init, headers: { 'content-type': 'application/json', ...(token ? { authorization: `Bearer ${token}` } : {}), ...(init?.headers ?? {}) } });
  if (response.status === 404) throw new Error('NOT_FOUND');
  if (response.status === 429) throw new Error('RATE_LIMITED');
  if (!response.ok) throw new Error(await response.text());
  return await response.json() as T;
}
export const apiPaths = { login: '/auth/login', refresh: '/auth/refresh', me: '/me', invoices: '/sales-invoices', statement: '/parties/{id}/statement', notifications: '/notifications', stock: '/inventory/levels' };
