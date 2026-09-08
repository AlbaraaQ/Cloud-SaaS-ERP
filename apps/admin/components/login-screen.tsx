'use client';

import { useState } from 'react';

import { ApiError, apiBaseUrl } from '../lib/api';
import { useSession } from '../lib/session';

import { SignupPanel } from './signup-panel';

/** Sign-in screen for the back office. Arabic-first, RTL, keyboard friendly. */
export function LoginScreen({ initialError }: { initialError?: string }) {
  const { signIn } = useSession();
  const [mode, setMode] = useState<'login' | 'signup'>('login');
  const [tenantCode, setTenantCode] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | undefined>(initialError);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(undefined);
    try {
      await signIn(email.trim(), password, tenantCode.trim());
    } catch (caught) {
      if (caught instanceof ApiError) {
        if (caught.status === 401) setError('بيانات الدخول غير صحيحة. تحقق من رمز المنشأة والبريد وكلمة المرور.');
        else if (caught.status === 423) setError('الاشتراك موقوف. تواصل مع إدارة المنصة لتفعيل الحساب.');
        else if (caught.status === 429) setError('محاولات كثيرة. انتظر دقيقة ثم أعد المحاولة.');
        else setError(caught.message);
      } else {
        setError(
          `تعذر الاتصال بالخادم (${apiBaseUrl}). تأكد من تشغيل الـ API وضبط API_PROXY_TARGET في ملف .env`,
        );
      }
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-brand">
        <span className="logo big">ERP</span>
        <h1>نظام المحاسبة السحابي</h1>
        <p>
          محاسبة، مستودعات، مشتريات، مبيعات، رواتب، مراسٍ ومشاريع — بنظام متعدد المنشآت وفوترة إلكترونية
          متوافقة مع هيئة الزكاة والضريبة.
        </p>
        <ul>
          <li>عزل كامل لبيانات كل منشأة (RLS على مستوى قاعدة البيانات)</li>
          <li>صلاحيات دقيقة لكل شاشة وكل إجراء</li>
          <li>سجل تدقيق غير قابل للتعديل</li>
        </ul>
      </div>

      {mode === 'signup' ? (
        <SignupPanel onBackToLogin={() => setMode('login')} />
      ) : (
      <form className="auth-card" onSubmit={submit}>
        <h2>تسجيل الدخول</h2>
        <p className="muted">لوحة تحكم المنشأة ولوحة تحكم المنصة.</p>

        <label className="field">
          <span>رمز المنشأة</span>
          <input
            className="input"
            value={tenantCode}
            onChange={(event) => setTenantCode(event.target.value)}
            placeholder="demo"
            autoComplete="organization"
            required
            dir="ltr"
          />
        </label>

        <label className="field">
          <span>البريد الإلكتروني</span>
          <input
            className="input"
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            placeholder="owner@demo.test"
            autoComplete="username"
            required
            dir="ltr"
          />
        </label>

        <label className="field">
          <span>كلمة المرور</span>
          <input
            className="input"
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            autoComplete="current-password"
            required
            dir="ltr"
          />
        </label>

        {error && (
          <p className="alert danger" role="alert">
            {error}
          </p>
        )}

        <button className="btn primary block" type="submit" disabled={busy}>
          {busy ? 'جارٍ الدخول…' : 'دخول'}
        </button>

        <button className="btn block" type="button" onClick={() => setMode('signup')}>
          ليس لديك حساب؟ اشترك الآن
        </button>
      </form>
      )}
    </div>
  );
}
