'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useMemo, useState } from 'react';

import { useSession } from '../lib/session';
import { visibleModules, type ModuleNode } from '../lib/navigation';

function statusDot(status: string) {
  const title = status === 'ready' ? 'جاهز' : status === 'api' ? 'الواجهة البرمجية جاهزة' : 'قيد التطوير';
  return <span className={`dot ${status}`} title={title} aria-label={title} />;
}

function ModuleBlock({ module, pathname, filter }: { module: ModuleNode; pathname: string; filter: string }) {
  const matches = (text: string) => text.toLowerCase().includes(filter.toLowerCase());
  const groups = filter
    ? module.groups
        .map((group) => ({ ...group, items: group.items.filter((item) => matches(item.labelAr) || matches(item.labelEn)) }))
        .filter((group) => group.items.length > 0)
    : module.groups;

  const active = groups.some((group) => group.items.some((item) => pathname === item.href.split('?')[0]));
  const [open, setOpen] = useState(active || Boolean(filter));

  if (groups.length === 0) return null;
  const expanded = open || Boolean(filter);

  return (
    <div className={`nav-module ${expanded ? 'open' : ''}`}>
      <button type="button" className="nav-module-head" onClick={() => setOpen(!expanded)} aria-expanded={expanded}>
        <span className="nav-icon" aria-hidden>
          {module.icon}
        </span>
        <span className="nav-module-label">
          {module.labelAr}
          <small>{module.labelEn}</small>
        </span>
        <span className="chev" aria-hidden>
          {expanded ? '▾' : '◂'}
        </span>
      </button>
      {expanded && (
        <div className="nav-groups">
          {groups.map((group) => (
            <div className="nav-group" key={group.key}>
              <p className="nav-group-title">{group.labelAr}</p>
              {group.items.map((item) => (
                <Link
                  key={item.key}
                  href={item.href}
                  className={pathname === item.href.split('?')[0] ? 'nav-link active' : 'nav-link'}
                >
                  {statusDot(item.status)}
                  <span>{item.labelAr}</span>
                </Link>
              ))}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export function AppShell({ children }: { children: React.ReactNode }) {
  const { me, isPlatformAdmin, signOut } = useSession();
  const pathname = usePathname() ?? '/';
  const [filter, setFilter] = useState('');
  const [mobileOpen, setMobileOpen] = useState(false);

  const tree = useMemo(
    () => visibleModules(me?.permissions ?? [], isPlatformAdmin),
    [me?.permissions, isPlatformAdmin],
  );

  return (
    <div className="shell">
      <aside className={`side ${mobileOpen ? 'open' : ''}`}>
        <div className="brand">
          <span className="logo">ERP</span>
          <span>
            Cloud SaaS ERP
            <small>{me?.membership.tenantName ?? '—'}</small>
          </span>
        </div>

        <Link href="/" className={pathname === '/' ? 'nav-link home active' : 'nav-link home'}>
          🏠 <span>الرئيسية</span>
        </Link>

        <input
          className="nav-search"
          value={filter}
          onChange={(event) => setFilter(event.target.value)}
          placeholder="بحث في الشاشات…"
          aria-label="بحث في الشاشات"
        />

        <nav className="nav" aria-label="أقسام النظام">
          {tree.map((module) => (
            <ModuleBlock key={module.key} module={module} pathname={pathname} filter={filter} />
          ))}
        </nav>

        <div className="nav-legend">
          <span>
            <span className="dot ready" /> جاهز
          </span>
          <span>
            <span className="dot api" /> API
          </span>
          <span>
            <span className="dot planned" /> قيد التطوير
          </span>
        </div>
      </aside>

      <main className="main">
        <header className="topbar">
          <div className="row" style={{ alignItems: 'center' }}>
            <button className="btn only-mobile" type="button" onClick={() => setMobileOpen(!mobileOpen)} aria-label="القائمة">
              ☰
            </button>
            <div>
              <strong>{me?.membership.tenantName ?? 'غير متصل'}</strong>
              <p className="muted" style={{ margin: 0 }}>
                {me?.membership.tenantCode ? `رمز المنشأة: ${me.membership.tenantCode}` : '—'}
                {me?.membership.isOwner ? ' · مالك' : ''}
                {isPlatformAdmin ? ' · مدير منصة' : ''}
              </p>
            </div>
          </div>
          <div className="row" style={{ alignItems: 'center' }}>
            <span className="muted">{me?.user.fullName}</span>
            <Link className="btn" href="/settings/change-password">
              كلمة المرور
            </Link>
            <button className="btn danger" type="button" onClick={() => void signOut()}>
              خروج
            </button>
          </div>
        </header>
        {children}
      </main>
    </div>
  );
}
