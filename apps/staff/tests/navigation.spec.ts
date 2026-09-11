import { existsSync, readdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

import { describe, expect, it } from 'vitest';

import { allScreens, findScreenByHref, modules, screenCounts, visibleModules } from '../lib/navigation.js';

const appDir = join(dirname(fileURLToPath(import.meta.url)), '..', 'app');

/**
 * '/sales/invoices?kind=x' -> 'app/sales/invoices/page.tsx'.
 *
 * A segment may be served by a dynamic route (`app/reports/[key]/page.tsx`), so each
 * step falls back to the single `[param]` directory at that level when no literal
 * directory exists.
 */
function pageFileFor(href: string): string {
  const segments = (href.split('?')[0] ?? '').split('#')[0]?.replace(/^\//, '').split('/').filter(Boolean) ?? [];
  let current = appDir;
  for (const segment of segments) {
    if (existsSync(join(current, segment))) {
      current = join(current, segment);
      continue;
    }
    const dynamic = readdirSync(current, { withFileTypes: true }).find((entry) => entry.isDirectory() && entry.name.startsWith('['));
    if (!dynamic) return join(current, segment, 'page.tsx');
    current = join(current, dynamic.name);
  }
  return join(current, 'page.tsx');
}

describe('staff navigation tree', () => {
  it('mirrors the desktop product modules', () => {
    // Phase 06 gave the treasury its own module: the voucher documents, the safes and
    // the banks are one product area in `Desktop_ERP` (`frmSandQ`, `frmTreasury`,
    // `frmBanks`), and they are one module here.
    expect(modules.map((module) => module.key)).toEqual([
      'accounting',
      'treasury',
      'inventory',
      'purchases',
      'sales',
      'hrm',
      'marina',
      'projects',
      'settings',
      'support',
    ]);
  });

  it('gives every screen a unique key and a route', () => {
    const keys = allScreens.map((screen) => screen.key);
    expect(new Set(keys).size).toBe(keys.length);
    expect(allScreens.every((screen) => screen.href.startsWith('/'))).toBe(true);
  });

  it('routes every unimplemented screen through the scaffold namespace', () => {
    const planned = allScreens.filter((screen) => screen.status !== 'ready');
    expect(planned.every((screen) => screen.href.startsWith('/s/'))).toBe(true);
  });

  it('points every ready screen at a page that actually exists', () => {
    const missing = allScreens
      .filter((screen) => screen.status === 'ready')
      .filter((screen) => !existsSync(pageFileFor(screen.href)))
      .map((screen) => `${screen.key} -> ${screen.href}`);
    expect(missing).toEqual([]);
  });

  it('keeps every ready screen out of the scaffold namespace', () => {
    const ready = allScreens.filter((screen) => screen.status === 'ready');
    expect(ready.some((screen) => screen.href.startsWith('/s/'))).toBe(false);
    expect(ready.length).toBeGreaterThan(60);
  });

  it('routes every report menu item through the report engine', () => {
    const reports = allScreens.filter((item) => item.href.startsWith('/reports/'));
    expect(reports.length).toBeGreaterThan(40);
    expect(reports.every((item) => item.status === 'ready')).toBe(true);
    expect(reports.every((item) => item.permission === 'reporting.view')).toBe(true);
  });

  it('implements the Salla and synchronisation screens', () => {
    for (const key of ['salla-products', 'salla-orders', 'salla-warehouses', 'salla-settings', 'data-sync', 'sync-manage', 'sync-invoices', 'sync-journals', 'sync-vouchers', 'sync-stock', 'android-devices']) {
      const item = allScreens.find((screen) => screen.key === key);
      expect(item, key).toBeDefined();
      expect(item?.status, key).toBe('ready');
      expect(item?.href.startsWith('/s/'), key).toBe(false);
    }
  });

  it('implements the operational screens promised by the desktop menu', () => {
    for (const key of ['expense-card', 'barcode', 'sn-credit', 'sales-debit-note', 'zatca-settings', 'sync-zatca', 'sync-prices', 'import-export', 'offers']) {
      const item = allScreens.find((screen) => screen.key === key);
      expect(item, key).toBeDefined();
      expect(item?.status, key).toBe('ready');
      expect(item?.href.startsWith('/s/'), key).toBe(false);
    }
  });

  it('implements both sides of the credit/debit note story', () => {
    for (const key of ['sn-credit', 'pn-debit', 'sales-debit-note', 'purchase-credit-note', 'quotation', 'payment-methods']) {
      const item = allScreens.find((screen) => screen.key === key);
      expect(item, key).toBeDefined();
      expect(item?.status, key).toBe('ready');
    }
    expect(allScreens.find((screen) => screen.key === 'pn-report')?.href).toBe('/reports/purchase-notes');
  });

  it('wires the contracting return and the production order screens', () => {
    const wired: Record<string, string> = {
      'contracting-return': '/projects/contracting-return',
      'production-order': '/inventory/production',
    };
    for (const [key, href] of Object.entries(wired)) {
      const item = allScreens.find((screen) => screen.key === key);
      expect(item, key).toBeDefined();
      expect(item?.status, key).toBe('ready');
      expect(item?.href, key).toBe(href);
    }
  });

  it('wires the contracting screens: contractor contract, payment certificate and offers', () => {
    const wired: Record<string, string> = {
      'contractor-contract': '/projects/contractor-contract',
      'contractor-payment': '/projects/contractor-payment',
      'project-offers': '/projects/offers',
    };
    for (const [key, href] of Object.entries(wired)) {
      const item = allScreens.find((screen) => screen.key === key);
      expect(item, key).toBeDefined();
      expect(item?.status, key).toBe('ready');
      expect(item?.href, key).toBe(href);
    }
  });

  it('wires the marina operations and the project follow-up board', () => {
    const wired: Record<string, string> = {
      'marina-prep': '/marina/preparation',
      'marina-rota': '/marina/rota',
      'marina-link': '/marina/link-invoices',
      'marina-day-close': '/marina/day-close',
      'project-followup': '/projects/followup',
    };
    for (const [key, href] of Object.entries(wired)) {
      const item = allScreens.find((screen) => screen.key === key);
      expect(item, key).toBeDefined();
      expect(item?.status, key).toBe('ready');
      expect(item?.href, key).toBe(href);
    }
  });

  it('wires the warehouse documents that bracket a transfer', () => {
    const wired: Record<string, string> = {
      'goods-request': '/inventory/requests',
      'stock-delivery': '/inventory/deliveries',
      transfer: '/inventory/transfers',
    };
    for (const [key, href] of Object.entries(wired)) {
      const item = allScreens.find((screen) => screen.key === key);
      expect(item, key).toBeDefined();
      expect(item?.status, key).toBe('ready');
      expect(item?.href, key).toBe(href);
    }
  });

  it('serves the by-employee and POS breakdowns from the report engine', () => {
    const wired: Record<string, string> = {
      'employee-sales': '/reports/sales-by-employee',
      'employee-purchases': '/reports/purchases-by-employee',
      'pos-item-detail': '/reports/pos-item-detail',
      'pos-group': '/reports/pos-by-category',
      'sn-report': '/reports/sales-notes',
    };
    for (const [key, href] of Object.entries(wired)) {
      expect(allScreens.find((screen) => screen.key === key)?.href, key).toBe(href);
    }
  });

  it('resolves a screen from its href, ignoring the query string', () => {
    expect(findScreenByHref('/accounting/accounts')?.key).toBe('coa');
    expect(findScreenByHref('/accounting/journal-entries/new?kind=opening')?.key).toBe('opening-entry');
  });

  it('advertises no platform-console route on the staff surface', () => {
    expect(allScreens.some((screen) => screen.href === '/platform' || screen.href.startsWith('/platform/'))).toBe(false);
    expect(modules.some((module) => module.key === 'console')).toBe(false);
    const asOperator = visibleModules(['*'], true);
    expect(asOperator.some((module) => module.href.startsWith('/platform'))).toBe(false);
  });

  it('filters items the user has no permission for', () => {
    const readOnly = visibleModules(['accounting.account.view'], false);
    const accounting = readOnly.find((module) => module.key === 'accounting');
    expect(accounting).toBeDefined();
    const keys = accounting?.groups.flatMap((group) => group.items.map((item) => item.key)) ?? [];
    expect(keys).toContain('coa');
    expect(keys).not.toContain('journal-voucher');
  });

  it('wires the file-level operations and the report designer to real screens', () => {
    const wired: Record<string, string> = {
      backup: '/settings/backup',
      restore: '/settings/restore',
      'data-rotation': '/settings/data-rotation',
      'invoice-maintenance': '/settings/invoice-maintenance',
      'new-file': '/settings/new-file',
      'report-designer': '/support/report-designer',
    };
    for (const [key, href] of Object.entries(wired)) {
      const item = allScreens.find((screen) => screen.key === key);
      expect(item?.href, key).toBe(href);
      expect(item?.status, key).toBe('ready');
      expect(item?.permission, key).toBeTruthy();
    }
  });

  it('gives every href exactly one home — no duplicate menu entries', () => {
    const seen = new Map<string, string>();
    const dupes: string[] = [];
    for (const screen of allScreens) {
      const first = seen.get(screen.href);
      if (first) dupes.push(`${screen.href} (keys: ${first}, ${screen.key})`);
      else seen.set(screen.href, screen.key);
    }
    expect(dupes).toEqual([]);
  });

  it('reports honest implementation counts', () => {
    const counts = screenCounts();
    expect(counts.total).toBe(counts.ready + counts.api + counts.planned);
    expect(counts.ready).toBeGreaterThan(0);
  });
});
