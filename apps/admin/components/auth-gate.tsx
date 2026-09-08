'use client';

import { useSession } from '../lib/session';

import { AppShell } from './app-shell';
import { LoginScreen } from './login-screen';

/**
 * Everything behind this component requires a session. The admin panel used to be fully
 * public — anybody who could reach port 3001 saw the whole back office.
 */
export function AuthGate({ children }: { children: React.ReactNode }) {
  const { status, error } = useSession();

  if (status === 'loading') {
    return (
      <div className="boot">
        <div className="boot-card">
          <span className="logo">ERP</span>
          <p>جارٍ التحقق من الجلسة…</p>
        </div>
      </div>
    );
  }

  if (status === 'anonymous') return <LoginScreen initialError={error} />;

  return <AppShell>{children}</AppShell>;
}
