'use client';

import type { ReactNode } from 'react';


/** Standard page chrome: breadcrumb, title, actions. */
export function Screen({
  title,
  subtitle,
  crumbs,
  actions,
  children,
}: {
  title: string;
  subtitle?: string;
  crumbs?: string[];
  actions?: ReactNode;
  children: ReactNode;
}) {
  return (
    <div className="grid">
      <section className="section-title">
        <div>
          {crumbs && crumbs.length > 0 && <p className="crumbs">{crumbs.join(' ← ')}</p>}
          <h1>{title}</h1>
          {subtitle && <p className="muted" style={{ margin: '4px 0 0' }}>{subtitle}</p>}
        </div>
        {actions && <div className="toolbar no-print">{actions}</div>}
      </section>
      {children}
    </div>
  );
}

export function Loading({ rows = 4 }: { rows?: number }) {
  return (
    <div className="card grid" aria-busy="true">
      {Array.from({ length: rows }).map((_, index) => (
        <div className="skeleton" key={index} style={{ width: `${100 - index * 8}%` }} />
      ))}
    </div>
  );
}

export function ErrorBox({ message, onRetry }: { message?: string; onRetry?: () => void }) {
  return (
    <div className="card">
      <p className="alert danger">تعذر تحميل البيانات: {message ?? 'خطأ غير معروف'}</p>
      {onRetry && (
        <button className="btn" type="button" onClick={onRetry} style={{ marginTop: 10 }}>
          إعادة المحاولة
        </button>
      )}
    </div>
  );
}

export function Forbidden() {
  return (
    <div className="card state">
      <strong>لا تملك صلاحية الوصول</strong>
      <p className="muted">اطلب من مالك الحساب منحك الصلاحية المطلوبة من «صلاحيات المستخدمين».</p>
    </div>
  );
}

export function Empty({ title, detail }: { title: string; detail?: string }) {
  return (
    <div className="card state">
      <strong>{title}</strong>
      {detail && <p className="muted">{detail}</p>}
    </div>
  );
}
