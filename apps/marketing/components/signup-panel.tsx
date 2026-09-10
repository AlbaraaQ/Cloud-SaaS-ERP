'use client';

import { useEffect, useState } from 'react';

import { PortalError, portalFetch } from '../lib/api';
import { surfaceHref } from '../lib/surfaces';

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
 *
 * Moved here from the old staff login screen during the 2026-09 surface separation:
 * registration belongs to the public marketing site, never to an authenticated console.
 */
export function SignupPanel() {
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
    portalFetch<Plan[]>('/signup/plans')
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
      const result = await portalFetch<SignupResult>('/signup', {
        method: 'POST',
        body: JSON.stringify({
          companyName: companyName.trim(),
          ownerFullName: ownerFullName.trim(),
          ownerEmail: ownerEmail.trim().toLowerCase(),
          ownerPassword,
          planId: planId || undefined,
          countryCode,
          baseCurrency,
        }),
      });
      setDone(result);
    } catch (caught) {
      if (caught instanceof PortalError) {
        if (caught.status === 409) setError('اسم المنشأة مستخدم مسبقاً. جرّب اسماً آخر.');
        else if (caught.status === 422) setError('تحقق من البيانات: كلمة المرور 12 حرفاً على الأقل.');
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
    const staffLogin = `${surfaceHref('staff', '/')}?tenant=${encodeURIComponent(done.tenantCode)}&email=${encodeURIComponent(done.ownerEmail)}&joined=1`;
    return (
      <div className="card">
        <h2>تم إنشاء منشأتك</h2>
        <p>
          احفظ رمز المنشأة — ستحتاجه في كل مرة تسجّل فيها الدخول: <b dir="ltr">{done.tenantCode}</b>
        </p>
        <p className="muted">
          البريد: <bdi>{done.ownerEmail}</bdi> · حالة الاشتراك: بانتظار اعتماد إدارة المنصة
        </p>
        <p className="muted">
          جُهّزت لك الحسابات الافتراضية وفرع رئيسي ومستودع وخزنة. يمكنك الدخول الآن والبدء بإعداد دليل الحسابات؛
          تُفعَّل الميزات المدفوعة فور اعتماد طلب الاشتراك.
        </p>
        <div className="toolbar">
          <a className="btn primary" href={staffLogin}>
            الدخول إلى لوحة الإدارة
          </a>
        </div>
      </div>
    );
  }

  return (
    <form className="form" onSubmit={submit}>
      <input
        className="input"
        aria-label="company"
        value={companyName}
        onChange={(event) => setCompanyName(event.target.value)}
        placeholder="اسم المنشأة * — مثال: مؤسسة النور التجارية"
        required
      />
      <input
        className="input"
        aria-label="full name"
        value={ownerFullName}
        onChange={(event) => setOwnerFullName(event.target.value)}
        placeholder="اسمك الكامل *"
        required
      />
      <input
        className="input"
        type="email"
        dir="ltr"
        aria-label="email"
        value={ownerEmail}
        onChange={(event) => setOwnerEmail(event.target.value)}
        autoComplete="username"
        placeholder="البريد الإلكتروني *"
        required
      />
      <div className="row">
        <input
          className="input"
          type="password"
          dir="ltr"
          aria-label="password"
          minLength={12}
          value={ownerPassword}
          onChange={(event) => setOwnerPassword(event.target.value)}
          autoComplete="new-password"
          placeholder="كلمة المرور * (12 حرفاً على الأقل)"
          required
        />
        <input
          className="input"
          type="password"
          dir="ltr"
          aria-label="confirm password"
          minLength={12}
          value={confirm}
          onChange={(event) => setConfirm(event.target.value)}
          autoComplete="new-password"
          placeholder="تأكيد كلمة المرور *"
          required
        />
      </div>
      <div className="row">
        <select className="input" aria-label="country" value={countryCode} onChange={(event) => setCountryCode(event.target.value)}>
          <option value="SA">السعودية</option>
          <option value="YE">اليمن</option>
          <option value="AE">الإمارات</option>
          <option value="EG">مصر</option>
        </select>
        <select className="input" aria-label="currency" value={baseCurrency} onChange={(event) => setBaseCurrency(event.target.value)}>
          <option value="SAR">SAR</option>
          <option value="YER">YER</option>
          <option value="AED">AED</option>
          <option value="EGP">EGP</option>
          <option value="USD">USD</option>
        </select>
      </div>
      {plans.length > 0 && (
        <select className="input" aria-label="plan" value={planId} onChange={(event) => setPlanId(event.target.value)}>
          <option value="">الباقة المطلوبة — أختارها لاحقاً</option>
          {plans.map((plan) => (
            <option key={plan.id} value={plan.id}>
              {plan.name} — {plan.amount} {plan.currency} / {plan.interval === 'year' ? 'سنة' : 'شهر'}
            </option>
          ))}
        </select>
      )}
      {error && (
        <p role="alert" className="muted">
          {error}
        </p>
      )}
      <button className="btn primary" type="submit" disabled={busy}>
        {busy ? 'جارٍ إنشاء المنشأة…' : 'إنشاء الحساب'}
      </button>
    </form>
  );
}
