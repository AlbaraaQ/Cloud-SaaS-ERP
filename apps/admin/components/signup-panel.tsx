'use client';

import { useEffect, useState } from 'react';

import { ApiError, apiData, apiPost } from '../lib/api';
import { useSession } from '../lib/session';

type Plan = { id: string; code: string; name: string; amount: string; currency: string; interval: string };

type SignupResult = {
  tenantCode: string;
  ownerEmail: string;
  subscriptionStatus: string;
};

/**
 * نافذة الاشتراك — self-service signup.
 *
 * Posts to the public `POST /api/v1/signup`, which creates the tenant, the owner account,
 * the baseline roles and the default branch/warehouse/safe, and files a pending activation
 * request. The owner can sign in immediately; the licence itself is granted by the platform
 * operator, so nobody can self-activate.
 */
export function SignupPanel({ onBackToLogin }: { onBackToLogin: () => void }) {
  const { signIn } = useSession();
  const [plans, setPlans] = useState<Plan[]>([]);
  const [companyName, setCompanyName] = useState('');
  const [ownerFullName, setOwnerFullName] = useState('');
  const [ownerEmail, setOwnerEmail] = useState('');
  const [ownerPassword, setOwnerPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [planId, setPlanId] = useState('');
  const [countryCode, setCountryCode] = useState('SA');
  const [baseCurrency, setBaseCurrency] = useState('SAR');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | undefined>();
  const [done, setDone] = useState<SignupResult | undefined>();

  useEffect(() => {
    apiData<Plan[]>('/signup/plans')
      .then(setPlans)
      .catch(() => setPlans([]));
  }, []);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    if (ownerPassword !== confirm) {
      setError('كلمتا المرور غير متطابقتين.');
      return;
    }
    setBusy(true);
    setError(undefined);
    try {
      const result = await apiPost<SignupResult>('/signup', {
        companyName: companyName.trim(),
        ownerFullName: ownerFullName.trim(),
        ownerEmail: ownerEmail.trim().toLowerCase(),
        ownerPassword,
        planId: planId || undefined,
        countryCode,
        baseCurrency,
      });
      setDone(result);
    } catch (caught) {
      if (caught instanceof ApiError) {
        if (caught.status === 409) setError('اسم المنشأة مستخدم مسبقاً. جرّب اسماً آخر.');
        else if (caught.status === 422) setError(caught.detail ?? 'تحقق من البيانات: كلمة المرور 12 حرفاً على الأقل.');
        else if (caught.status === 429) setError('محاولات كثيرة. انتظر دقيقة ثم أعد المحاولة.');
        else if (caught.status === 403) setError('التسجيل الذاتي معطّل على هذا الخادم. تواصل مع إدارة المنصة.');
        else setError(caught.message);
      } else {
        setError('تعذر الاتصال بالخادم. تأكد من تشغيل الـ API.');
      }
    } finally {
      setBusy(false);
    }
  }

  if (done) {
    return (
      <div className="auth-card">
        <h2>تم إنشاء منشأتك</h2>
        <p className="alert ok">
          احفظ رمز المنشأة — ستحتاجه في كل مرة تسجّل فيها الدخول: <b dir="ltr">{done.tenantCode}</b>
        </p>
        <dl className="kv">
          <dt>رمز المنشأة</dt>
          <dd dir="ltr">{done.tenantCode}</dd>
          <dt>البريد</dt>
          <dd dir="ltr">{done.ownerEmail}</dd>
          <dt>حالة الاشتراك</dt>
          <dd>بانتظار اعتماد إدارة المنصة</dd>
        </dl>
        <p className="muted small">
          جُهّزت لك الحسابات الافتراضية وفرع رئيسي ومستودع وخزنة. يمكنك الدخول الآن والبدء بإعداد دليل الحسابات؛
          تُفعَّل الميزات المدفوعة فور اعتماد طلب الاشتراك.
        </p>
        <button
          className="btn primary block"
          type="button"
          disabled={busy}
          onClick={() => {
            setBusy(true);
            void signIn(done.ownerEmail, ownerPassword, done.tenantCode).catch(() => {
              setBusy(false);
              onBackToLogin();
            });
          }}
        >
          الدخول إلى النظام
        </button>
      </div>
    );
  }

  return (
    <form className="auth-card" onSubmit={submit}>
      <h2>اشترك الآن</h2>
      <p className="muted">أنشئ منشأتك وحساب المالك في خطوة واحدة.</p>

      <label className="field">
        <span>اسم المنشأة *</span>
        <input
          className="input"
          value={companyName}
          onChange={(event) => setCompanyName(event.target.value)}
          placeholder="مؤسسة النور التجارية"
          required
        />
      </label>

      <label className="field">
        <span>اسمك الكامل *</span>
        <input className="input" value={ownerFullName} onChange={(event) => setOwnerFullName(event.target.value)} required />
      </label>

      <label className="field">
        <span>البريد الإلكتروني *</span>
        <input
          className="input"
          type="email"
          dir="ltr"
          value={ownerEmail}
          onChange={(event) => setOwnerEmail(event.target.value)}
          autoComplete="username"
          required
        />
      </label>

      <div className="row">
        <label className="field">
          <span>كلمة المرور * (12+)</span>
          <input
            className="input"
            type="password"
            dir="ltr"
            minLength={12}
            value={ownerPassword}
            onChange={(event) => setOwnerPassword(event.target.value)}
            autoComplete="new-password"
            required
          />
        </label>
        <label className="field">
          <span>تأكيد كلمة المرور *</span>
          <input
            className="input"
            type="password"
            dir="ltr"
            minLength={12}
            value={confirm}
            onChange={(event) => setConfirm(event.target.value)}
            autoComplete="new-password"
            required
          />
        </label>
      </div>

      <div className="row">
        <label className="field">
          <span>الدولة</span>
          <select className="input" value={countryCode} onChange={(event) => setCountryCode(event.target.value)}>
            <option value="SA">السعودية</option>
            <option value="YE">اليمن</option>
            <option value="AE">الإمارات</option>
            <option value="EG">مصر</option>
          </select>
        </label>
        <label className="field">
          <span>العملة</span>
          <select className="input" value={baseCurrency} onChange={(event) => setBaseCurrency(event.target.value)}>
            <option value="SAR">SAR</option>
            <option value="YER">YER</option>
            <option value="AED">AED</option>
            <option value="EGP">EGP</option>
            <option value="USD">USD</option>
          </select>
        </label>
      </div>

      {plans.length > 0 && (
        <label className="field">
          <span>الباقة المطلوبة</span>
          <select className="input" value={planId} onChange={(event) => setPlanId(event.target.value)}>
            <option value="">— أختارها لاحقاً</option>
            {plans.map((plan) => (
              <option key={plan.id} value={plan.id}>
                {plan.name} — {plan.amount} {plan.currency} / {plan.interval === 'year' ? 'سنة' : 'شهر'}
              </option>
            ))}
          </select>
        </label>
      )}

      {error && (
        <p className="alert danger" role="alert">
          {error}
        </p>
      )}

      <button className="btn primary block" type="submit" disabled={busy}>
        {busy ? 'جارٍ إنشاء المنشأة…' : 'إنشاء الحساب'}
      </button>

      <button className="btn block" type="button" onClick={onBackToLogin}>
        لدي حساب — تسجيل الدخول
      </button>
    </form>
  );
}
