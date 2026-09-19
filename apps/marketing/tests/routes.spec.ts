import { existsSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

import { describe, expect, it } from 'vitest';

import { publicRoutes } from '../lib/navigation';

const appDir = fileURLToPath(new URL('../app', import.meta.url));

describe('marketing route groups', () => {
  it('advertises public pages only', () => {
    // P-M6 أضاف «اطلب عرضاً» إلى المسارات المعلَنة: الاستمارة صارت مساراً حقيقياً بمصدرٍ
    // موسوم (`demo`) في طابور العملاء المتوقّعين، لا زرّاً يُعيد الزائر إلى `/contact`.
    expect(publicRoutes.map((route) => route.key)).toEqual(['home', 'pricing', 'demo', 'contact', 'verify']);
    expect(publicRoutes.every((route) => !route.href.startsWith('/portal') && !route.href.startsWith('/auth'))).toBe(true);
  });

  /** A link in the header that leads to a 404 is worse than no link, so every route must have a page. */
  it('has a page file behind every advertised route', () => {
    for (const route of publicRoutes) {
      const relative = route.href === '/' ? 'page.tsx' : `${route.href.slice(1)}/page.tsx`;
      expect(existsSync(join(appDir, relative)), route.href).toBe(true);
    }
    expect(existsSync(join(appDir, 'onboarding/page.tsx')), '/onboarding').toBe(true);
    expect(existsSync(join(appDir, 'demo/page.tsx')), '/demo').toBe(true);
    // P-M7: صفحة إلغاء الاشتراك — يُفتح رابطها من كل رسالة حملة، فهي مسارٌ حقيقيّ يُقاس.
    expect(existsSync(join(appDir, 'unsubscribe/page.tsx')), '/unsubscribe').toBe(true);
    // P-M8: صفحة الثقة وفهرس القطاعات، وصفحةٌ لكل قطاع — ومسارٌ يُعلَن في الرأس ولا صفحةَ
    // تحته أسوأ من مسارٍ غائب، فيُقاس هنا لا في الإنتاج.
    expect(existsSync(join(appDir, 'trust/page.tsx')), '/trust').toBe(true);
    expect(existsSync(join(appDir, 'industries/page.tsx')), '/industries').toBe(true);
    expect(existsSync(join(appDir, 'industries/[slug]/page.tsx')), '/industries/[slug]').toBe(true);
    expect(existsSync(join(appDir, 'login/page.tsx')), '/login').toBe(true);
  });

  /** The portal and console must not exist on this surface. */
  it('hosts no portal or console page', () => {
    for (const relative of ['portal/page.tsx', 'auth/login/page.tsx', 'platform/page.tsx']) {
      expect(existsSync(join(appDir, relative)), relative).toBe(false);
    }
  });
});
