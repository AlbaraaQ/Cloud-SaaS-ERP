'use client';

import { useState } from 'react';

import { ApiError } from '../lib/api';
import { useLang } from '../lib/i18n';
import { useSession } from '../lib/session';

import { SignupPanel } from './signup-panel';

/** Sign-in screen for the back office. Bilingual, keyboard friendly, with a TOTP step. */
export function LoginScreen({ initialError }: { initialError?: string }) {
  const { signIn } = useSession();
  const { t } = useLang();
  const [mode, setMode] = useState<'login' | 'signup'>('login');
  const [step, setStep] = useState<'credentials' | 'mfa'>('credentials');
  const [tenantCode, setTenantCode] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [mfaCode, setMfaCode] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | undefined>(initialError);

  async function attempt(code?: string) {
    setBusy(true);
    setError(undefined);
    try {
      await signIn(email.trim(), password, tenantCode.trim(), code);
    } catch (caught) {
      if (caught instanceof ApiError) {
        if (caught.code === 'MFA_REQUIRED') {
          // Credentials already checked out — only the authenticator code is missing.
          setStep('mfa');
        } else if (caught.status === 401) setError(t(step === 'mfa' ? 'mfa.error.invalid' : 'login.error.invalid'));
        else if (caught.status === 423) setError(t('login.error.suspended'));
        else if (caught.status === 429) setError(t('login.error.rateLimited'));
        else setError(caught.message);
      } else {
        setError(t('login.error.unreachable'));
      }
    } finally {
      setBusy(false);
    }
  }

  function submit(event: React.FormEvent) {
    event.preventDefault();
    if (step === 'mfa') void attempt(mfaCode.trim());
    else void attempt();
  }

  return (
    <div className="auth-page">
      <div className="auth-brand">
        <span className="logo big">ERP</span>
        <h1>{t('app.name')}</h1>
        <p>{t('app.tagline')}</p>
        <ul>
          <li>{t('app.feature.isolation')}</li>
          <li>{t('app.feature.permissions')}</li>
          <li>{t('app.feature.audit')}</li>
        </ul>
      </div>

      {mode === 'signup' ? (
        <SignupPanel onBackToLogin={() => setMode('login')} />
      ) : (
      <form className="auth-card" onSubmit={submit}>
        <h2>{step === 'mfa' ? t('mfa.title') : t('login.title')}</h2>
        <p className="muted">{step === 'mfa' ? t('mfa.hint') : t('login.subtitle')}</p>

        {step === 'credentials' ? (
          <>
            <label className="field">
              <span>{t('login.tenantCode')}</span>
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
              <span>{t('login.email')}</span>
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
              <span>{t('login.password')}</span>
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
          </>
        ) : (
          <label className="field">
            <span>{t('mfa.code')}</span>
            <input
              className="input"
              value={mfaCode}
              onChange={(event) => setMfaCode(event.target.value)}
              placeholder="000000"
              autoComplete="one-time-code"
              inputMode="numeric"
              autoFocus
              required
              dir="ltr"
            />
          </label>
        )}

        {error && (
          <p className="alert danger" role="alert">
            {error}
          </p>
        )}

        <button className="btn primary block" type="submit" disabled={busy}>
          {busy ? (step === 'mfa' ? t('mfa.verifying') : t('login.busy')) : step === 'mfa' ? t('mfa.verify') : t('login.submit')}
        </button>

        {step === 'mfa' ? (
          <button
            className="btn block"
            type="button"
            onClick={() => {
              setStep('credentials');
              setMfaCode('');
              setError(undefined);
            }}
          >
            {t('mfa.back')}
          </button>
        ) : (
          <button className="btn block" type="button" onClick={() => setMode('signup')}>
            {t('login.signup')}
          </button>
        )}
      </form>
      )}
    </div>
  );
}
