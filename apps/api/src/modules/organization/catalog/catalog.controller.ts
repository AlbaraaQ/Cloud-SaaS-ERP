import { Body, Controller, Get, Post, Query } from '@nestjs/common';
import { z } from 'zod';

import { getTenantContext } from '../../platform/context/tenant-context.js';

import { CatalogService } from './catalog.service.js';

const itemSchema = z.object({ sku: z.string().min(1).max(80), nameAr: z.string().min(1).max(200), nameEn: z.string().max(200).optional(), categoryId: z.string().uuid(), baseUnitId: z.string().uuid(), kind: z.enum(['stock', 'service', 'composite']).optional(), salePrice: z.string().optional(), purchasePrice: z.string().optional(), taxGroupId: z.string().uuid().optional() });

@Controller('organization/catalog')
export class CatalogController {
  constructor(private readonly catalog: CatalogService) {}

  @Get('items') list(@Query('q') q?: string) { return this.catalog.listItems(getTenantContext().tenantId, q); }
  @Post('items') create(@Body() body: unknown) { return this.catalog.createItem(getTenantContext().tenantId, itemSchema.parse(body)); }
  @Get('categories') categories() { return this.catalog.listCategories(getTenantContext().tenantId); }
}
