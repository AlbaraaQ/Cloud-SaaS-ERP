import { Body, Controller, Get, Post, Query } from '@nestjs/common';
import { z } from 'zod';

import { getTenantContext } from '../../platform/context/tenant-context.js';
import { RequiresPermission } from '../../platform/decorators/requires-permission.decorator.js';

import { CatalogService } from './catalog.service.js';

const itemSchema = z.object({ sku: z.string().min(1).max(80), barcode: z.string().trim().min(1).max(64).optional(), nameAr: z.string().min(1).max(200), nameEn: z.string().max(200).optional(), categoryId: z.string().uuid(), baseUnitId: z.string().uuid(), kind: z.enum(['stock', 'service', 'composite']).optional(), salePrice: z.string().optional(), purchasePrice: z.string().optional(), taxGroupId: z.string().uuid().optional() });
const categorySchema = z.object({ code: z.string().min(1).max(40), nameAr: z.string().min(1).max(200), nameEn: z.string().max(200).optional(), parentId: z.string().uuid().optional() });
const unitSchema = z.object({ code: z.string().min(1).max(20), nameAr: z.string().min(1).max(120), nameEn: z.string().max(120).optional() });
const taxGroupSchema = z.object({ nameAr: z.string().min(1).max(120), nameEn: z.string().max(120).optional(), rate: z.string().regex(/^\d+(\.\d{1,4})?$/), vatAccountId: z.string().uuid().optional(), isInclusiveDefault: z.boolean().optional() });

@Controller('organization/catalog')
export class CatalogController {
  constructor(private readonly catalog: CatalogService) {}

  @Get('items') @RequiresPermission('catalog.item.view') list(@Query('q') q?: string) { return this.catalog.listItems(getTenantContext().tenantId, q); }
  @Post('items') @RequiresPermission('catalog.item.manage') create(@Body() body: unknown) { return this.catalog.createItem(getTenantContext().tenantId, itemSchema.parse(body)); }
  @Get('categories') @RequiresPermission('catalog.category.view') categories() { return this.catalog.listCategories(getTenantContext().tenantId); }
  @Post('categories') @RequiresPermission('catalog.category.manage') createCategory(@Body() body: unknown) { return this.catalog.createCategory(getTenantContext().tenantId, categorySchema.parse(body)); }
  @Get('units') @RequiresPermission('catalog.unit.view') units() { return this.catalog.listUnits(getTenantContext().tenantId); }
  @Post('units') @RequiresPermission('catalog.unit.manage') createUnit(@Body() body: unknown) { return this.catalog.createUnit(getTenantContext().tenantId, unitSchema.parse(body)); }
  @Get('tax-groups') @RequiresPermission('catalog.taxgroup.view') taxGroups() { return this.catalog.listTaxGroups(getTenantContext().tenantId); }
  @Post('tax-groups') @RequiresPermission('catalog.taxgroup.manage') createTaxGroup(@Body() body: unknown) { return this.catalog.createTaxGroup(getTenantContext().tenantId, taxGroupSchema.parse(body)); }
}
