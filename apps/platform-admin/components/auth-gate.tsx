'use client';

import { useSession } from '../lib/session';

import { LoginScreen } from './login-screen';
import { PlatformGuard } from './platform-guard';

/**
 * Everything behind this component requires a platform session. There is no
 * self-service signup on the console: platform operators are created by hand and
 * granted roles from `/roles`.
 */
export function AuthGate({ children }: { children: React.ReactNode }) {
  const { status, error, signOut, me } = useSession();

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

  return (
    <div className="wrap">
      <header className="top">
        <span className="brand">
          <span className="logo">ERP</span>
          <span>لوحة تحكم المنصة</span>
        </span>
        <span className="muted">
          {me?.user.fullName} · {me?.user.email}
          <button className="btn" type="button" onClick={() => void signOut()} style={{ marginInlineStart: 12 }}>
            خروج
          </button>
        </span>
      </header>
      <main style={{ marginTop: 18 }}>
        <PlatformGuard>{children}</PlatformGuard>
      </main>
    </div>
  );
}
