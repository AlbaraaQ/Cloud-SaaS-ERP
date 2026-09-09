'use client';

import { useState } from 'react';

import { Screen, Loading, ErrorBox } from '../../../components/screen';
import { ApiError, apiData, apiFetch } from '../../../lib/api';
import { useQuery } from '../../../lib/use-query';

type MfaStatus = { enabled: boolean; enrolled: boolean; recoveryCodesLeft: number };
type MfaEnroll = { secretBase32: string; otpauthUrl: string; issuer: string };

/**
 * التحقق بخطوتين — TOTP (RFC 6238) on the caller's own account.
 *
 * Flow mirrors the API's two-phase enrolment: `enroll` hands over a secret to scan or
 * type into an authenticator app; `enable` only takes effect after a valid code proves
 * the app was really configured — then eight one-time recovery codes appear once.
 */
export default function TwoFactorPage() {
  const status = useQuery<MfaStatus>(() => apiData('/auth/mfa'));

  const [phase, setPhase] = useState<'idle' | 'confirming' | 'codes'>('idle');
  const [enroll, setEnroll] = useState<MfaEnroll | undefined>(undefined);
  const [code, setCode] = useState('');
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);
  const [password, setPassword] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | undefined>(undefined);
  const [copied, setCopied] = useState<string | undefined>(undefined);

  const fail = (caught: unknown) =>
    setError(caught instanceof ApiError ? caught.message : 'تعذر تنفيذ الطلب. أعد المحاولة.');

  async function start() {
    setBusy(true);
    setError(undefined);
    try {
      const created = await apiData<MfaEnroll>('/auth/mfa/enroll', { method: 'POST', body: JSON.stringify({}) });
      setEnroll(created);
      setPhase('confirming');
    } catch (caught) {
      fail(caught);
    } finally {
      setBusy(false);
    }
  }

  async function confirm() {
    setBusy(true);
    setError(undefined);
    try {
      const enabled = await apiData<{ recoveryCodes: string[] }>('/auth/mfa/enable', {
        method: 'POST',
        body: JSON.stringify({ code: code.trim() }),
      });
      setRecoveryCodes(enabled.recoveryCodes);
      setPhase('codes');
      setCode('');
      status.reload();
    } catch (caught) {
      fail(caught);
    } finally {
      setBusy(false);
    }
  }

  async function disable() {
    setBusy(true);
    setError(undefined);
    try {
      await apiFetch('/auth/mfa/disable', { method: 'POST', body: JSON.stringify({ password }) });
      setPassword('');
      setPhase('idle');
      status.reload();
    } catch (caught) {
      fail(caught);
    } finally {
      setBusy(false);
    }
  }

  function copy(text: string, what: string) {
    void navigator.clipboard?.writeText(text).then(() => {
      setCopied(what);
      setTimeout(() => setCopied(undefined), 1500);
    });
  }

  function downloadCodes() {
    const blob = new Blob([`Cloud SaaS ERP — recovery codes\n${recoveryCodes.join('\n')}\n`], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = 'erp-recovery-codes.txt';
    anchor.click();
    URL.revokeObjectURL(url);
  }

  return (
    <Screen
      title="التحقق بخطوتين"
      subtitle="Two-factor authentication (TOTP)"
      crumbs={['الإعدادات', 'المستخدمون']}
    >
      {status.status === 'loading' && <Loading rows={3} />}
      {status.status === 'error' && <ErrorBox message={status.error} onRetry={status.reload} />}
      {status.status === 'forbidden' && <ErrorBox message={status.error} />}

      {status.status === 'success' && (
        <>
          <section className="card">
            <div className="row" style={{ alignItems: 'center', gap: 10 }}>
              <span className={`badge ${status.data!.enabled ? 'ready' : 'planned'}`}>
                {status.data!.enabled ? 'مفعَّل — الدخول يتطلب رمز التطبيق' : 'غير مفعَّل'}
              </span>
              {status.data!.enabled && (
                <span className="muted">رموز الاسترداد المتبقية: {status.data!.recoveryCodesLeft}</span>
              )}
            </div>
            <p className="muted" style={{ marginTop: 8 }}>
              يضيف التحقق بخطوتين طبقة حماية فوق كلمة المرور: رمز متغيَّر كل 30 ثانية من تطبيق مصادقة
              (Google Authenticator أو Microsoft Authenticator أو غيرهما)، مع رموز استرداد تُستخدم مرة واحدة
              إذا فقدت جهازك.
            </p>
          </section>

          {error && (
            <section className="card">
              <p className="alert danger">{error}</p>
            </section>
          )}

          {/* Phase: not enrolled — offer to start. */}
          {status.status === 'success' && !status.data!.enrolled && phase === 'idle' && (
            <section className="card">
              <button className="btn primary" type="button" onClick={() => void start()} disabled={busy}>
                {busy ? 'جارٍ الإنشاء…' : 'بدء الإعداد'}
              </button>
            </section>
          )}

          {/* Phase: secret issued — scan/type, then confirm with a live code. */}
          {phase === 'confirming' && enroll && (
            <section className="card">
              <strong>1) أضف الحساب إلى تطبيق المصادقة</strong>
              <p className="muted">
                امسح الرابط أدناه كرمز QR عبر أي مولّد رموز، أو أدخل المفتاح يدوياً في التطبيق.
              </p>
              <dl className="kv">
                <dt>رابط التهيئة (otpauth)</dt>
                <dd dir="ltr" style={{ wordBreak: 'break-all' }}>
                  {enroll.otpauthUrl}
                </dd>
                <dt>المفتاح (إدخال يدوي)</dt>
                <dd dir="ltr" style={{ letterSpacing: 2 }}>
                  {enroll.secretBase32}
                </dd>
              </dl>
              <div className="toolbar">
                <button className="btn" type="button" onClick={() => copy(enroll.otpauthUrl, 'otpauth')}>
                  {copied === 'otpauth' ? 'تم النسخ' : 'نسخ الرابط'}
                </button>
                <button className="btn" type="button" onClick={() => copy(enroll.secretBase32, 'secret')}>
                  {copied === 'secret' ? 'تم النسخ' : 'نسخ المفتاح'}
                </button>
              </div>

              <strong style={{ display: 'block', marginTop: 14 }}>2) أكِّد برمز التطبيق</strong>
              <div className="row" style={{ gap: 8, marginTop: 6 }}>
                <input
                  className="input"
                  style={{ maxWidth: 160 }}
                  value={code}
                  onChange={(event) => setCode(event.target.value)}
                  placeholder="000000"
                  inputMode="numeric"
                  dir="ltr"
                />
                <button className="btn primary" type="button" onClick={() => void confirm()} disabled={busy || code.trim().length !== 6}>
                  {busy ? 'جارٍ التأكيد…' : 'تفعيل'}
                </button>
              </div>
            </section>
          )}

          {/* Phase: just enabled — recovery codes are visible exactly once. */}
          {phase === 'codes' && (
            <section className="card">
              <p className="alert">
                تم تفعيل التحقق بخطوتين. هذه رموز الاسترداد — تُعرض مرة واحدة فقط ولن تظهر مجدداً.
                احفظها في مكان آمن؛ كل رمز يعمل مرة واحدة.
              </p>
              <pre dir="ltr" style={{ letterSpacing: 2, lineHeight: 2 }}>
                {recoveryCodes.join('\n')}
              </pre>
              <div className="toolbar">
                <button className="btn" type="button" onClick={() => copy(recoveryCodes.join('\n'), 'codes')}>
                  {copied === 'codes' ? 'تم النسخ' : 'نسخ الرموز'}
                </button>
                <button className="btn" type="button" onClick={downloadCodes}>
                  تنزيل ملف نصي
                </button>
              </div>
            </section>
          )}

          {/* Phase: enabled — disable requires the account password. */}
          {status.data!.enabled && phase !== 'codes' && (
            <section className="card">
              <strong>إيقاف التحقق بخطوتين</strong>
              <p className="muted">يتطلب كلمة مرور الحساب. لا يُنصح بالإيقاف إلا عند فقدان تطبيق المصادقة.</p>
              <div className="row" style={{ gap: 8, marginTop: 6 }}>
                <input
                  className="input"
                  style={{ maxWidth: 280 }}
                  type="password"
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  autoComplete="current-password"
                  placeholder="كلمة المرور"
                  dir="ltr"
                />
                <button className="btn danger" type="button" onClick={() => void disable()} disabled={busy || !password}>
                  {busy ? 'جارٍ الإيقاف…' : 'إيقاف التحقق بخطوتين'}
                </button>
              </div>
            </section>
          )}
        </>
      )}
    </Screen>
  );
}
