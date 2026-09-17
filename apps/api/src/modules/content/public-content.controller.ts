import { Controller, Get, Param, Query } from '@nestjs/common';
import { ApiOkResponse, ApiOperation, ApiParam, ApiQuery, ApiTags } from '@nestjs/swagger';
import {
  publicHelpQuerySchema,
  publicPostsQuerySchema,
  type ContentBanner,
  type ContentPageDetail,
  type ContentSitemapRow,
  type ListEnvelope,
  type PublicFaq,
  type PublicPost,
  type PublicSite,
} from '@erp/contracts';

import { ZodValidationPipe } from '../../common/pipes/zod-validation.pipe.js';
import { Public } from '../platform/decorators/public.decorator.js';

import { ContentService } from './content.service.js';

/**
 * P-M1 · P-M2 · P-M5 — الواجهة العامة للموقع التسويقي.
 *
 * سبعة مسارات بلا جلسة، وكلها **قراءة**: إعدادات الموقع، وقوائمه، ولافتته، وصفحةٌ منشورة،
 * وقائمة مقالات، ومركز مساعدة، وأسئلة شائعة، وخريطة الموقع. والزائر لا يكتب شيئاً هنا —
 * النماذج (تواصل · نشرة · عملاء متوقّعون) في P-M6 بمسارٍ آخر له حمايةُ مزعجٍ خاصة.
 *
 * والقاعدة الحاكمة: **`@Public()` لا تعني بلا عزل**. الخدمة تفتح معاملة بسياق المنصّة ثم
 * تُصفّي بـ`publishedWhere`، فالمسوّدة والمجدولة غير موجودتين من هنا بنيوياً — ويقيس ذلك
 * `apps/api/test/public-content.spec.ts` و`scripts/verify-content.mjs`.
 */
@ApiTags('public-content')
@Controller('public')
export class PublicContentController {
  constructor(private readonly content: ContentService) {}

  @Public()
  @Get('site')
  @ApiOperation({ summary: 'هوية الموقع وقوائمه ولافتته المعروضة (نداءٌ واحد للقشرة)' })
  @ApiOkResponse({ description: 'Site identity, menus and the live banner' })
  async site(): Promise<{ data: PublicSite }> {
    return { data: await this.content.publicSite() };
  }

  @Public()
  @Get('sitemap')
  @ApiOperation({ summary: 'خريطة الموقع: مسارات المنشور فقط بلغاتها' })
  async sitemap(): Promise<{ data: ContentSitemapRow[] }> {
    return { data: await this.content.publicSitemap() };
  }

  @Public()
  @Get('posts')
  @ApiQuery({ name: 'kind', required: false, description: 'post (default) · case_study · …' })
  @ApiQuery({ name: 'category', required: false })
  @ApiQuery({ name: 'limit', required: false })
  @ApiQuery({ name: 'offset', required: false })
  @ApiOperation({ summary: 'المحتوى المسرود المنشور: المدوّنة افتراضاً، ودراسات الحالة بترشيح النوع' })
  async posts(
    @Query(new ZodValidationPipe(publicPostsQuerySchema)) query: PublicPostsQueryShape,
  ): Promise<ListEnvelope<PublicPost>> {
    return this.content.publicPosts(query);
  }

  @Public()
  @Get('help')
  @ApiQuery({ name: 'category', required: false })
  @ApiQuery({ name: 'q', required: false })
  @ApiOperation({ summary: 'مقالات مركز المساعدة، مع بحثٍ في العنوان والملخّص' })
  async help(
    @Query(new ZodValidationPipe(publicHelpQuerySchema)) query: PublicHelpQueryShape,
  ): Promise<ListEnvelope<PublicPost>> {
    return this.content.publicHelp(query);
  }

  @Public()
  @Get('faq')
  @ApiOperation({ summary: 'الأسئلة الشائعة مسطَّحة من كتل الصفحات المنشورة (وJSON-LD منها)' })
  async faq(): Promise<{ data: PublicFaq[] }> {
    return { data: await this.content.publicFaq() };
  }

  @Public()
  @Get('banners')
  @ApiOperation({ summary: 'اللافتات المعروضة الآن' })
  async banners(): Promise<{ data: ContentBanner[] }> {
    return { data: await this.content.publicBanners() };
  }

  @Public()
  @Get('content/:slug')
  @ApiParam({ name: 'slug', description: 'رابط الصفحة (المسوّدة والمجدولة تُردّ 404)' })
  @ApiOperation({ summary: 'صفحةٌ منشورة بكتلها' })
  async page(@Param('slug') slug: string): Promise<{ data: ContentPageDetail }> {
    return { data: await this.content.publicPage(slug) };
  }
}

type PublicPostsQueryShape = Parameters<ContentService['publicPosts']>[0];
type PublicHelpQueryShape = Parameters<ContentService['publicHelp']>[0];
