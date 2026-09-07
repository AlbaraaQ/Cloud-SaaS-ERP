/* global RequestInit */
export type ApiState<T> = { status: 'idle' | 'loading' | 'success' | 'error' | 'forbidden'; data?: T; error?: string };

const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:3000/api/v1';

export async function apiFetch<T>(path: string, token?: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers: { 'content-type': 'application/json', ...(token ? { authorization: `Bearer ${token}` } : {}), ...(init?.headers ?? {}) },
  });
  if (response.status === 403) throw new Error('FORBIDDEN');
  if (response.status === 401) throw new Error('UNAUTHENTICATED');
  if (!response.ok) throw new Error(await response.text());
  return (await response.json()) as T;
}

export const endpoints = {
  login: '/auth/login',
  me: '/me',
  reports: '/reports',
  migrationRuns: '/migration/runs',
  compatDevices: '/compat/devices',
};
