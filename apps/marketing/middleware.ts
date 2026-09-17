import { NextResponse, type NextRequest } from 'next/server';

/**
 * P-M1 — وسيطٌ يفعل شيئين صغيرين، وكلاهما لحاجةٍ حقيقية:
 *
 *   1. **ترويسة المسار**: التخطيط الجذري لا يعرف المسار، واللغة في هذا الموقع **في المسار**
 *      (`/en/...`) لا في كوكي — فيُنسخ المسار إلى `x-pathname` ليعرف التخطيط `lang` و`dir`.
 *   2. **حالة 404 الصحيحة** لصفحات المحتوى الديناميكية: يُسأل الـAPI عن الـslug قبل التصيير،
 *      وما لم يُنشر يُعاد كتابة الطلب إلى `/not-found-view` **بحالة 404**. بدونها يعيد
 *      Next حالة 200 لصفحةٍ غير موجودة (التصيير يبدأ قبل قرار الصفحة) — خطأٌ ناعم.
 *
 * الثمن: نداءٌ واحد إضافي على مسارات المقالات وحدها (لا الصفحة الرئيسية ولا القوائم). وهذا
 * مقبول: هو الثمن الذي يشتري حالة HTTP صحيحة، والخادم داخليّ (بلا شبكة).
 */
const apiBase = (
  process.env.API_INTERNAL_BASE ??
  process.env.API_PROXY_TARGET ??
  `http://127.0.0.1:${process.env.PORT ?? 3000}`
).replace(/\/+$/, '');

/** مسارات المحتوى الديناميكية وحدها: `/blog/x` و`/en/help/y` … */
const CONTENT_DETAIL = /^\/(?:en\/)?(?:blog|help|cases|legal)\/([^/]+)$/;

export async function middleware(request: NextRequest) {
  const headers = new Headers(request.headers);
  headers.set('x-pathname', request.nextUrl.pathname);

  const match = CONTENT_DETAIL.exec(request.nextUrl.pathname);
  if (match && (request.method === 'GET' || request.method === 'HEAD')) {
    const slug = decodeURIComponent(match[1] ?? '');
    let published = true;
    try {
      const response = await fetch(`${apiBase}/api/v1/public/content/${encodeURIComponent(slug)}`, {
        headers: { accept: 'application/json' },
      });
      published = response.ok;
    } catch {
      // الـAPI لا يجيب: لا نُخفي خطأ خدمةٍ عن الزائر بحالة 404 — الصفحة تُصيَّر وتقول ما عندها.
      published = true;
    }
    if (!published) {
      const url = request.nextUrl.clone();
      const english = /^\/en(?:\/|$)/.test(request.nextUrl.pathname);
      url.pathname = `/not-found-view/${english ? 'en' : 'ar'}`;
      return NextResponse.rewrite(url, { status: 404, request: { headers } });
    }
  }

  return NextResponse.next({ request: { headers } });
}

/** كل المسارات ما عدا أصول Next والملفات الثابتة والـAPI (الـAPI يُمرَّر بالـrewrite). */
export const config = {
  matcher: ['/((?!_next/static|_next/image|favicon.ico|api/).*)'],
};
