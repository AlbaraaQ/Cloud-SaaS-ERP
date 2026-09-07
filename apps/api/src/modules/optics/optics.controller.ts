import { Body, Controller, Get, Param, Post, Query } from '@nestjs/common';

import { getTenantContext } from '../platform/context/tenant-context.js';
import { RequiresPermission } from '../platform/decorators/requires-permission.decorator.js';

import { OpticsService, type OpticalPrescriptionInput } from './optics.service.js';
@Controller('optics')
export class OpticsController { constructor(private readonly optics: OpticsService) {} @Get('prescriptions') @RequiresPermission('optics.view') list(@Query('partyId') partyId?: string) { return this.optics.list(getTenantContext().tenantId, partyId); } @Post('prescriptions') @RequiresPermission('optics.manage') create(@Body() body: OpticalPrescriptionInput) { return this.optics.create(getTenantContext().tenantId, body); } @Get('invoice-lines/:lineId/print-section') @RequiresPermission('optics.view') print(@Param('lineId') lineId: string) { return this.optics.invoicePrintSection(getTenantContext().tenantId, lineId); } }
