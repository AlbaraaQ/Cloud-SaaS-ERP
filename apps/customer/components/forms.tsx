'use client';

import { useState } from 'react';
import type { FormEvent } from 'react';

const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:3000/api/v1';

export function LoginForm() {
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  async function submit(event: FormEvent<globalThis.HTMLFormElement>) {
    event.preventDefault();
    setError('');
    setLoading(true);
    const form = new FormData(event.currentTarget);
    try {
      const response = await fetch(`${apiBaseUrl}/auth/login`, {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({
          tenantCode: String(form.get('tenantCode')),
          email: String(form.get('email')),
          password: String(form.get('password')),
        }),
      });
      if (!response.ok) throw new Error(response.status === 401 ? 'بيانات الدخول غير صحيحة' : 'تعذر تسجيل الدخول');
      const result = await response.json();
      if (result.data?.accessToken) globalThis.document.cookie = `erp_access_token=${encodeURIComponent(result.data.accessToken)}; path=/; SameSite=Lax`;
      globalThis.location.assign('/portal');
    } catch (loginError) {
      setError(loginError instanceof Error ? loginError.message : 'تعذر تسجيل الدخول');
    } finally {
      setLoading(false);
    }
  }

  return <form className="form" onSubmit={submit} noValidate>
    <input className="input" name="tenantCode" placeholder="رمز الشركة" aria-label="tenant" required />
    <input className="input" name="email" type="email" placeholder="البريد الإلكتروني" aria-label="email" required />
    <input className="input" name="password" type="password" placeholder="كلمة المرور" aria-label="password" required />
    {error ? <p role="alert" className="muted">{error}</p> : null}
    <button className="btn primary" type="submit" disabled={loading}>{loading ? 'جارٍ التحقق...' : 'تسجيل الدخول'}</button>
    <a className="muted" href="/auth/forced-reset">إعداد كلمة مرور مستخدم مرحّل</a>
  </form>;
}
export function ProfileRequestForm() { return <form className="form"><input className="input" placeholder="الاسم" /><input className="input" placeholder="الجوال" /><textarea className="input" placeholder="تفاصيل طلب التعديل" /><button className="btn primary" type="button">إرسال طلب موافقة</button></form>; }
export function VerifyForm() { const [result, setResult] = useState(''); return <div className="card"><form className="form"><input className="input" placeholder="Invoice UUID" /><input className="input" placeholder="Hash / QR value" /><button className="btn primary" type="button" onClick={() => setResult('issuer: شركة*** · date: 2026-09-07 · total: SAR 115.00')}>تحقق</button></form>{result ? <p className="muted">{result}</p> : <p className="muted">المخرجات مخفية ومحدودة البيانات ومحمية بمعدل طلبات منخفض.</p>}</div>; }
export function QuickSaleForm() { return <form className="form"><input className="input" placeholder="بحث عن صنف" /><input className="input" inputMode="decimal" placeholder="الكمية" /><select className="input"><option>cash</option><option>card</option><option>credit</option></select><button className="btn primary" type="button">إنشاء وترحيل فاتورة</button></form>; }
