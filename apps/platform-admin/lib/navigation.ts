/**
 * شجرة تنقّل لوحة المنصة (P-C1 — الأساس والقشرة).
 *
 * The console has no `Desktop_ERP` counterpart to mirror: the desktop product serves one
 * company and has no tenants, licences or operators. So this tree is built from the
 * **existing console surface** (`docs/roadmap/PLATFORM_CONSOLE_PLAN.md` §2 measured eleven
 * pages and seventeen endpoints) plus the two screens P-C1 adds — إعدادات المنصة and the
 * cross-tenant التدقيق — and every label that already existed is re-used **verbatim**:
 * نظرة عامة · العملاء · التراخيص · الباقات · طلبات التفعيل · المستخدمون · أدوار المنصة ·
 * التدقيق · الصحة (`components/platform-guard.tsx`, pre-P-C1), plus the three screens P-C4
 * adds to the money group — الفواتير · المتابعة والتحصيل · الإيراد — whose labels the plan
 * already names («`/invoices`», «dunning screen», «revenue dashboard»). The invented labels
 * and their justification are listed in the part document (§«ما اخترعناه»).
 *
 * `status` is part of the data model for the same reason the staff tree has it: a screen
 * is never allowed to pretend. `ready` here means the page exists **and** the endpoint it
 * calls is a console route protected by a `console.*` code.
 *
 * `permission` is the console code the sidebar filters on. The API is the authority — the
 * sidebar only decides what to *offer* (`useSession().canConsole`), so a hidden link is a
 * convenience, never a control.
 */

export type ConsoleStatus = 'ready' | 'planned';

export type ConsoleItem = {
  key: string;
  labelAr: string;
  labelEn: string;
  href: string;
  /** `console.*` code required to open the page. */
  permission: string;
  status: ConsoleStatus;
  /** Where the page's data comes from — printed on the screen and checked by tests. */
  endpoint: string;
  icon?: string;
};

export type ConsoleGroup = {
  key: string;
  labelAr: string;
  labelEn: string;
  /** 🔴…🟢 — the priority the plan gives the unit this item belongs to. */
  icon: string;
  items: ConsoleItem[];
};

const item = (
  key: string,
  labelAr: string,
  labelEn: string,
  href: string,
  permission: string,
  endpoint: string,
  status: ConsoleStatus = 'ready',
): ConsoleItem => ({ key, labelAr, labelEn, href, permission, endpoint, status });

/**
 * The four sidebar groups named by the plan: العملاء · المال · التشغيل · المنصة.
 * Ordering follows the operator's day: who are my customers, what do they owe, is the
 * service healthy, and who am I on this platform.
 */
export const consoleGroups: readonly ConsoleGroup[] = [
  {
    key: 'customers',
    labelAr: 'العملاء',
    labelEn: 'Customers',
    icon: '🏢',
    items: [
      item('tenants', 'العملاء', 'Customers', '/tenants', 'console.tenants.view', 'GET /platform/tenants'),
      item(
        'tenant-new',
        'عميل جديد',
        'New customer',
        '/tenants/new',
        'console.tenants.manage',
        'POST /platform/tenants',
      ),
    ],
  },
  {
    key: 'money',
    labelAr: 'المال',
    labelEn: 'Money',
    icon: '💳',
    items: [
      item(
        'subscriptions',
        'التراخيص',
        'Licences',
        '/subscriptions',
        'console.subscriptions.manage',
        'GET /platform/subscriptions',
      ),
      item('plans', 'الباقات', 'Plans', '/plans', 'console.plans.manage', 'GET /platform/plans'),
      // P-C4 — three money screens the console did not have: the documents the platform
      // issues, the collection ladder that follows them, and the revenue board. All three are
      // behind `console.billing.manage` (declared in P-C1, first used here), which is why the
      // auditor — read-only over customers and the audit trail — does not see them.
      item('invoices', 'الفواتير', 'Invoices', '/invoices', 'console.billing.manage', 'GET /platform/invoices'),
      item(
        'dunning',
        'المتابعة والتحصيل',
        'Dunning',
        '/dunning',
        'console.billing.manage',
        'GET /platform/dunning',
      ),
      item('revenue', 'الإيراد', 'Revenue', '/revenue', 'console.billing.manage', 'GET /platform/revenue'),
      item(
        'activation-requests',
        'طلبات التفعيل',
        'Activation requests',
        '/activation-requests',
        'console.activation.review',
        'GET /platform/activation-requests',
      ),
    ],
  },
  {
    key: 'operations',
    labelAr: 'التشغيل',
    labelEn: 'Operations',
    icon: '🛠️',
    items: [
      item('audit', 'التدقيق', 'Audit', '/audit', 'console.audit.view', 'GET /platform/audit'),
      item('jobs', 'المهام والطوابير', 'Jobs', '/jobs', 'console.jobs.view', 'GET /platform/jobs/outbox'),
      item('health', 'الصحة', 'Health', '/health', 'console.health.view', 'GET /api/health/ready'),
    ],
  },
  {
    key: 'platform',
    labelAr: 'المنصة',
    labelEn: 'Platform',
    icon: '⚙️',
    items: [
      item('overview', 'نظرة عامة', 'Overview', '/', 'console.tenants.view', 'GET /platform/overview'),
      // P-C5 — «الاستخدام والحصص»: شبكةٌ تقول من اقترب من حدّه ومن تجاوزه، وتصديرٌ لمن
      // يمسك المال. الرؤية `console.tenants.view` (كل مشغّل يرى استهلاك عملائه)، والتصدير
      // `console.billing.manage` (بيانٌ يذهب إلى الفوترة) — والاثنان من P-C1.
      item('usage', 'الاستخدام', 'Usage', '/usage', 'console.tenants.view', 'GET /platform/usage'),
      item('users', 'المستخدمون', 'Users', '/users', 'console.users.view', 'GET /platform/users'),
      item('roles', 'أدوار المنصة', 'Platform roles', '/roles', 'console.users.view', 'GET /platform/roles'),
      item(
        'settings',
        'إعدادات المنصة',
        'Platform settings',
        '/settings',
        'console.tenants.view',
        'GET /platform/settings',
      ),
    ],
  },
] as const;

export const consoleItems: readonly ConsoleItem[] = consoleGroups.flatMap((group) => group.items);

/**
 * The groups an operator with `permissions` can actually open. Empty groups never render:
 * a heading with nothing under it is worse than an absent heading.
 */
export function visibleConsoleGroups(permissions: readonly string[]): ConsoleGroup[] {
  return consoleGroups
    .map((group) => ({
      ...group,
      items: group.items.filter((entry) => permissions.includes(entry.permission)),
    }))
    .filter((group) => group.items.length > 0);
}

/** The group a path belongs to — used by the shell's breadcrumb. */
export function groupForPath(pathname: string): ConsoleGroup | undefined {
  return consoleGroups.find((group) =>
    group.items.some((entry) => entry.href === pathname || (entry.href !== '/' && pathname.startsWith(entry.href))),
  );
}

export function findConsoleItem(href: string): ConsoleItem | undefined {
  return consoleItems.find((entry) => entry.href === href);
}
