/* eslint-disable no-restricted-syntax */
import { Body, Controller, Get, Param, Post, Query } from '@nestjs/common';

import { getTenantContext } from '../platform/context/tenant-context.js';
import { RequiresPermission } from '../platform/decorators/requires-permission.decorator.js';

import { MarinaService } from './marina.service.js';
@Controller('marina')
export class MarinaController { constructor(private readonly marina: MarinaService) {} @Get() @RequiresPermission('marina.view') list() { return this.marina.list(getTenantContext().tenantId); } @Get('bookings') @RequiresPermission('marina.view') bookings() { return this.marina.listBookings(getTenantContext().tenantId); } @Get('violations') @RequiresPermission('marina.view') violations() { return this.marina.listViolations(getTenantContext().tenantId); } @Post('groups') @RequiresPermission('marina.manage') group(@Body() b: { name: string; code?: string }) { return this.marina.createGroup(getTenantContext().tenantId, b); } @Post('groups/:id/pricing') @RequiresPermission('marina.manage') price(@Param('id') id: string, @Body() b: { periodKind: string; price: string; currency?: string }) { return this.marina.price(getTenantContext().tenantId, id, b); } @Post('vessels') @RequiresPermission('marina.manage') vessel(@Body() b: { groupId?: string; code: string; name: string; capacity?: number; metadata?: Record<string, unknown> }) { return this.marina.createVessel(getTenantContext().tenantId, b); } @Post('vessels/:id/owners') @RequiresPermission('marina.manage') owner(@Param('id') id: string, @Body() b: { partyId: string; percent: string }) { return this.marina.addOwner(getTenantContext().tenantId, id, b); } @Post('bookings') @RequiresPermission('marina.manage') booking(@Body() b: { branchId: string; partyId: string; vesselId: string; startsAt: string; endsAt: string; companions?: number; insuranceAmount?: string; metadata?: Record<string, unknown> }) { return this.marina.createBooking(getTenantContext().tenantId, b); } @Post('bookings/:id/additions') @RequiresPermission('marina.manage') addition(@Param('id') id: string, @Body() b: { description: string; amount: string }) { return this.marina.addBookingAddition(getTenantContext().tenantId, id, b); } @Post('bookings/:id/rental-invoice') @RequiresPermission('marina.invoice') invoice(@Param('id') id: string) { return this.marina.createRentalInvoice(getTenantContext().tenantId, id); } @Post('violations') @RequiresPermission('marina.manage') violation(@Body() b: { vesselId?: string; bookingId?: string; partyId?: string; violationDate: string; amount?: string; description: string }) { return this.marina.violation(getTenantContext().tenantId, b); } @Post('operation-plans') @RequiresPermission('marina.manage') plan(@Body() b: { groupId?: string; planDate: string; name: string; lines?: Array<{ vesselId: string; periodLabel?: string; metadata?: Record<string, unknown> }> }) { return this.marina.plan(getTenantContext().tenantId, b); } }

/**
 * Marina operations — preparation, rota, invoice linking and the day close (0025).
 *
 * They sit in a second controller so the original CRUD surface stays readable; Nest maps
 * both onto the same `/marina` prefix.
 */
@Controller('marina')
export class MarinaOperationsController {
  constructor(private readonly marina: MarinaService) {}

  @Get('operation-plans')
  @RequiresPermission('marina.view')
  plans(@Query('date') planDate?: string) {
    return this.marina.listPlans(getTenantContext().tenantId, planDate);
  }

  @Get('preparations')
  @RequiresPermission('marina.view')
  preparations(@Query('date') date?: string, @Query('status') status?: string) {
    return this.marina.listPreparations(getTenantContext().tenantId, { date, status });
  }

  @Post('bookings/:id/preparation')
  @RequiresPermission('marina.manage')
  prepare(@Param('id') id: string, @Body() body: { preparedOn?: string; fuelLevel?: string; lifeJackets?: number; checklist?: Record<string, unknown>; notes?: string }) {
    return this.marina.prepareBooking(getTenantContext().tenantId, id, body ?? {});
  }

  @Post('preparations/:id/return')
  @RequiresPermission('marina.manage')
  returnVessel(@Param('id') id: string, @Body() body: { returnNotes?: string }) {
    return this.marina.returnPreparation(getTenantContext().tenantId, id, body ?? {});
  }

  @Get('rental-invoices')
  @RequiresPermission('marina.view')
  rentals() {
    return this.marina.listRentalInvoices(getTenantContext().tenantId);
  }

  @Get('bookings/uninvoiced')
  @RequiresPermission('marina.view')
  uninvoiced() {
    return this.marina.listUninvoicedBookings(getTenantContext().tenantId);
  }

  @Post('rental-invoices/link')
  @RequiresPermission('marina.invoice')
  link(@Body() body: { bookingIds: string[] }) {
    return this.marina.linkInvoices(getTenantContext().tenantId, body?.bookingIds ?? []);
  }

  @Get('day-close')
  @RequiresPermission('marina.view')
  daySummary(@Query('branch_id') branchId: string, @Query('date') date: string) {
    return this.marina.dayCloseSummary(getTenantContext().tenantId, branchId, date);
  }

  @Get('day-closings')
  @RequiresPermission('marina.view')
  dayClosings(@Query('branch_id') branchId?: string) {
    return this.marina.listDayClosings(getTenantContext().tenantId, branchId);
  }

  @Post('day-close')
  @RequiresPermission('marina.manage')
  closeDay(@Body() body: { branchId: string; closeDate: string; notes?: string; force?: boolean }) {
    return this.marina.closeDay(getTenantContext().tenantId, body);
  }
}
