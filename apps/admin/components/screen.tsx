'use client';

import Link from 'next/link';
import type { ReactNode } from 'react';

import { findScreenByHref } from '../lib/navigation';

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

/**
 * Placeholder for a screen that exists in the menu tree but is not built yet.
 * It never fakes data: it states the status and names the endpoint it will consume,
 * so the roadmap is visible instead of hidden behind a dummy table.
 */
export function ScreenScaffold({ href }: { href: string }) {
  const item = findScreenByHref(href);

  if (!item) {
    return (
      <Screen title="شاشة غير معروفة" subtitle={href}>
        <Empty title="هذه الشاشة غير مسجلة في شجرة النظام" detail="تحقق من الرابط أو ارجع للرئيسية." />
        <Link className="btn" href="/">
          الرئيسية
        </Link>
      </Screen>
    );
  }

  return (
    <Screen
      title={item.labelAr}
      subtitle={item.labelEn}
      crumbs={[item.moduleLabelAr, item.groupLabelAr]}
      actions={
        <Link className="btn" href="/">
          الرئيسية
        </Link>
      }
    >
      <div className="card">
        <div className="row" style={{ alignItems: 'center', marginBottom: 10 }}>
          <span className={`badge ${item.status}`}>
            {item.status === 'api' ? 'الواجهة البرمجية جاهزة — الشاشة قيد التنفيذ' : 'قيد التطوير'}
          </span>
        </div>
        {item.status === 'api' ? (
          <p>
            منطق هذه الشاشة موجود بالفعل في الخادم ويمكن استدعاؤه الآن عبر المسار أدناه؛ ما ينقص هو واجهة
            الإدخال والعرض.
          </p>
        ) : (
          <p>هذه الشاشة مدرجة في خطة النظام ولم تُنفذ بعد، لا في الواجهة ولا في الخادم.</p>
        )}
        {item.endpoint && (
          <dl className="kv" style={{ marginTop: 10 }}>
            <dt>المسار البرمجي</dt>
            <dd dir="ltr">{item.endpoint}</dd>
            {item.permission && (
              <>
                <dt>الصلاحية</dt>
                <dd dir="ltr">{item.permission}</dd>
              </>
            )}
          </dl>
        )}
      </div>
    </Screen>
  );
}
