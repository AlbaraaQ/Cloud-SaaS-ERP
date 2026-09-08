'use client';

import { useState } from 'react';

import { decodeZatcaQr, type ZatcaQr } from '../../lib/qr';

export default function VerifyPage() {
  const [value, setValue] = useState('');
  const [result, setResult] = useState<ZatcaQr | null>(null);
  const [error, setError] = useState('');

  function decode() {
    setError('');
    setResult(null);
    try {
      setResult(decodeZatcaQr(value));
    } catch (decodeError) {
      setError(decodeError instanceof Error ? decodeError.message : 'تعذّرت قراءة الرمز');
    }
  }

  return (
    <div className="grid">
      <section className="hero">
        <h1>قراءة رمز QR للفاتورة الإلكترونية</h1>
        <p>الصق محتوى رمز QR المطبوع على الفاتورة (نص Base64) لعرض اسم البائع ورقمه الضريبي وتاريخ الفاتورة والإجمالي والضريبة. القراءة تتم داخل متصفحك ولا تُرسل لأي خادم.</p>
      </section>

      <section className="card">
        <div className="form">
          <textarea className="input" rows={4} value={value} onChange={(event) => setValue(event.target.value)} placeholder="AQ4... (محتوى رمز QR)" aria-label="qr payload" />
          <div className="toolbar">
            <button className="btn primary" type="button" onClick={decode} disabled={!value.trim()}>
              قراءة الرمز
            </button>
            <button className="btn" type="button" onClick={() => { setValue(''); setResult(null); setError(''); }}>
              مسح
            </button>
          </div>
        </div>
        {error ? <p className="muted" role="alert">{error}</p> : null}
        {result ? (
          <table>
            <tbody>
              <tr><td className="muted">اسم البائع</td><td>{result.sellerName || '—'}</td></tr>
              <tr><td className="muted">الرقم الضريبي</td><td>{result.vatNumber || '—'}</td></tr>
              <tr><td className="muted">تاريخ الفاتورة</td><td>{result.timestamp || '—'}</td></tr>
              <tr><td className="muted">الإجمالي شامل الضريبة</td><td>{result.total || '—'}</td></tr>
              <tr><td className="muted">ضريبة القيمة المضافة</td><td>{result.vatTotal || '—'}</td></tr>
              <tr><td className="muted">حالة الختم</td><td><span className={`badge ${result.signed ? 'paid' : 'pending'}`}>{result.signed ? 'يحتوي على ختم وتوقيع إلكتروني' : 'مبسّطة بدون ختم (وسوم 6-8 غير موجودة)'}</span></td></tr>
            </tbody>
          </table>
        ) : null}
        <p className="muted">
          <small>للتحقق الرسمي من صحة الفاتورة استخدم تطبيق «فاتورة» من هيئة الزكاة والضريبة والجمارك؛ هذه الأداة تقرأ محتوى الرمز فقط.</small>
        </p>
      </section>
    </div>
  );
}
