import { Body, Controller, Get, Param, Post } from '@nestjs/common';

import { getTenantContext } from '../platform/context/tenant-context.js';
import { RequiresPermission } from '../platform/decorators/requires-permission.decorator.js';

import { TailoringService, type MeasurementInput } from './tailoring.service.js';
@Controller('tailoring')
export class TailoringController { constructor(private readonly tailoring: TailoringService) {} @Get('parties/:partyId/measurements') @RequiresPermission('tailoring.view') list(@Param('partyId') partyId: string) { return this.tailoring.list(getTenantContext().tenantId, partyId); } @Get('parties/:partyId/measurements/latest') @RequiresPermission('tailoring.view') latest(@Param('partyId') partyId: string) { return this.tailoring.latest(getTenantContext().tenantId, partyId); } @Post('measurements') @RequiresPermission('tailoring.manage') create(@Body() body: MeasurementInput) { return this.tailoring.create(getTenantContext().tenantId, body); } }
