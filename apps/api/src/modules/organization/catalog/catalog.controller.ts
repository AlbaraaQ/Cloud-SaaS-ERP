import { Body, Controller, Delete, Get, Param, Patch, Post, Query } from '@nestjs/common';
import { z } from 'zod';

import { getTenantContext } from '../../platform/context/tenant-context.js';
import { RequiresPermission } from '../../platform/decorators/requires-permission.decorator.js';

import { CatalogService } from './catalog.service.js';

const quantitySchema = z.string().regex(/^\d+(\.\d{1,6})?$/);
const itemSchema = z.object({ sku: z.string().min(1).max(80), barcode: z.string().trim().min(1).max(64).optional(), nameAr: z.string().min(1).max(200), nameEn: z.string().max(200).optional(), categoryId: z.string().uuid(), baseUnitId: z.string().uuid(), kind: z.enum(['stock', 'service', 'composite']).optional(), salePrice: quantitySchema.optional(), purchasePrice: quantitySchema.optional(), taxGroupId: z.string().uuid().optional(), minQty: quantitySchema.optional(), maxQty: quantitySchema.optional(), maxDiscountPct: quantitySchema.optional(), maxDiscountAmt: quantitySchema.optional(), trackLot: z.boolean().optional(), trackSerial: z.boolean().optional(), weightedScale: z.boolean().optional(), showInPos: z.boolean().optional() });
const categorySchema = z.object({ code: z.string().min(1).max(40), nameAr: z.string().min(1).max(200), nameEn: z.string().max(200).optional(), parentId: z.string().uuid().optional() });
const unitSchema = z.object({ code: z.string().min(1).max(20), nameAr: z.string().min(1).max(120), nameEn: z.string().max(120).optional() });
const itemPatchSchema = z.object({ sku: z.string().min(1).max(80).optional(), barcode: z.string().trim().max(64).nullish(), nameAr: z.string().min(1).max(200).optional(), nameEn: z.string().max(200).nullish(), categoryId: z.string().uuid().optional(), kind: z.enum(['stock', 'service', 'composite']).optional(), salePrice: quantitySchema.nullish(), purchasePrice: quantitySchema.nullish(), taxGroupId: z.string().uuid().nullish(), minQty: quantitySchema.optional(), maxQty: quantitySchema.nullish(), maxDiscountPct: quantitySchema.nullish(), maxDiscountAmt: quantitySchema.nullish(), trackLot: z.boolean().optional(), trackSerial: z.boolean().optional(), weightedScale: z.boolean().optional(), showInPos: z.boolean().optional() });
const itemUnitSchema = z.object({ unitId: z.string().uuid(), ratio: quantitySchema, barcode: z.string().trim().max(64).optional(), salePrice: quantitySchema.nullish(), purchasePrice: quantitySchema.nullish(), isDefaultPurchase: z.boolean().optional(), isDefaultSale: z.boolean().optional() });
const itemBarcodeSchema = z.object({ barcode: z.string().trim().min(1).max(64), unitId: z.string().uuid().optional() });
const itemAlternativeCodeSchema = z.object({ code: z.string().trim().min(1).max(80), notes: z.string().trim().max(500).optional() });
const categoryPatchSchema = z.object({ code: z.string().min(1).max(40).optional(), nameAr: z.string().min(1).max(200).optional(), nameEn: z.string().max(200).nullish(), parentId: z.string().uuid().nullish() });
const unitPatchSchema = z.object({ code: z.string().min(1).max(20).optional(), nameAr: z.string().min(1).max(120).optional(), nameEn: z.string().max(120).nullish() });
const taxGroupPatchSchema = z.object({ nameAr: z.string().min(1).max(120).optional(), nameEn: z.string().max(120).nullish(), rate: z.string().regex(/^\d+(\.\d{1,4})?$/).optional(), vatAccountId: z.string().uuid().nullish(), isInclusiveDefault: z.boolean().optional() });

const taxGroupSchema = z.object({ nameAr: z.string().min(1).max(120), nameEn: z.string().max(120).optional(), rate: z.string().regex(/^\d+(\.\d{1,4})?$/), vatAccountId: z.string().uuid().optional(), isInclusiveDefault: z.boolean().optional() });

@Controller('organization/catalog')
export class CatalogController {
  constructor(private readonly catalog: CatalogService) {}

  @Get('items') @RequiresPermission('catalog.item.view') list(@Query('q') q?: string) { return this.catalog.listItems(getTenantContext().tenantId, q); }
  /** Scanner lookup resolves SKU, primary/alternate barcode and the selected unit. */
  @Get('items/lookup') @RequiresPermission('catalog.item.view') lookup(@Query('code') code: string) { return this.catalog.lookupItem(getTenantContext().tenantId, code); }
  @Get('items/:id/units') @RequiresPermission('catalog.item.view') itemUnits(@Param('id') id: string) { return this.catalog.listItemUnits(getTenantContext().tenantId, id); }
  @Post('items/:id/units') @RequiresPermission('catalog.item.manage') upsertItemUnit(@Param('id') id: string, @Body() body: unknown) { return this.catalog.upsertItemUnit(getTenantContext().tenantId, id, itemUnitSchema.parse(body)); }
  @Delete('items/:id/units/:unitId') @RequiresPermission('catalog.item.manage') removeItemUnit(@Param('id') id: string, @Param('unitId') unitId: string) { return this.catalog.removeItemUnit(getTenantContext().tenantId, id, unitId); }
  @Get('items/:id/barcodes') @RequiresPermission('catalog.item.view') itemBarcodes(@Param('id') id: string) { return this.catalog.listItemBarcodes(getTenantContext().tenantId, id); }
  @Post('items/:id/barcodes') @RequiresPermission('catalog.item.manage') addItemBarcode(@Param('id') id: string, @Body() body: unknown) { return this.catalog.addItemBarcode(getTenantContext().tenantId, id, itemBarcodeSchema.parse(body)); }
  @Delete('items/:id/barcodes/:barcode') @RequiresPermission('catalog.item.manage') removeItemBarcode(@Param('id') id: string, @Param('barcode') barcode: string) { return this.catalog.removeItemBarcode(getTenantContext().tenantId, id, barcode); }
  @Get('items/:id/alternative-codes') @RequiresPermission('catalog.item.view') itemAlternativeCodes(@Param('id') id: string) { return this.catalog.listItemAlternativeCodes(getTenantContext().tenantId, id); }
  @Post('items/:id/alternative-codes') @RequiresPermission('catalog.item.manage') upsertItemAlternativeCode(@Param('id') id: string, @Body() body: unknown) { return this.catalog.upsertItemAlternativeCode(getTenantContext().tenantId, id, itemAlternativeCodeSchema.parse(body)); }
  @Delete('items/:id/alternative-codes/:code') @RequiresPermission('catalog.item.manage') removeItemAlternativeCode(@Param('id') id: string, @Param('code') code: string) { return this.catalog.removeItemAlternativeCode(getTenantContext().tenantId, id, code); }
  @Post('items') @RequiresPermission('catalog.item.manage') create(@Body() body: unknown) { return this.catalog.createItem(getTenantContext().tenantId, itemSchema.parse(body)); }
  @Patch('items/:id') @RequiresPermission('catalog.item.manage') update(@Param('id') id: string, @Body() body: unknown) { return this.catalog.updateItem(getTenantContext().tenantId, id, itemPatchSchema.parse(body)); }
  @Delete('items/:id') @RequiresPermission('catalog.item.manage') remove(@Param('id') id: string) { return this.catalog.removeItem(getTenantContext().tenantId, id); }
  @Get('categories') @RequiresPermission('catalog.category.view') categories() { return this.catalog.listCategories(getTenantContext().tenantId); }
  @Post('categories') @RequiresPermission('catalog.category.manage') createCategory(@Body() body: unknown) { return this.catalog.createCategory(getTenantContext().tenantId, categorySchema.parse(body)); }
  @Patch('categories/:id') @RequiresPermission('catalog.category.manage') updateCategory(@Param('id') id: string, @Body() body: unknown) { return this.catalog.updateCategory(getTenantContext().tenantId, id, categoryPatchSchema.parse(body)); }
  @Delete('categories/:id') @RequiresPermission('catalog.category.manage') removeCategory(@Param('id') id: string) { return this.catalog.removeCategory(getTenantContext().tenantId, id); }
  @Get('units') @RequiresPermission('catalog.unit.view') units() { return this.catalog.listUnits(getTenantContext().tenantId); }
  @Post('units') @RequiresPermission('catalog.unit.manage') createUnit(@Body() body: unknown) { return this.catalog.createUnit(getTenantContext().tenantId, unitSchema.parse(body)); }
  @Patch('units/:id') @RequiresPermission('catalog.unit.manage') updateUnit(@Param('id') id: string, @Body() body: unknown) { return this.catalog.updateUnit(getTenantContext().tenantId, id, unitPatchSchema.parse(body)); }
  @Delete('units/:id') @RequiresPermission('catalog.unit.manage') removeUnit(@Param('id') id: string) { return this.catalog.removeUnit(getTenantContext().tenantId, id); }
  @Get('tax-groups') @RequiresPermission('catalog.taxgroup.view') taxGroups() { return this.catalog.listTaxGroups(getTenantContext().tenantId); }
  @Post('tax-groups') @RequiresPermission('catalog.taxgroup.manage') createTaxGroup(@Body() body: unknown) { return this.catalog.createTaxGroup(getTenantContext().tenantId, taxGroupSchema.parse(body)); }
  @Patch('tax-groups/:id') @RequiresPermission('catalog.taxgroup.manage') updateTaxGroup(@Param('id') id: string, @Body() body: unknown) { return this.catalog.updateTaxGroup(getTenantContext().tenantId, id, taxGroupPatchSchema.parse(body)); }
}
