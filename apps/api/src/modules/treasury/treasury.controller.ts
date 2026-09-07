import { Body, Controller, Get, Param, Patch, Post, Query } from '@nestjs/common';

import { getTenantContext } from '../platform/context/tenant-context.js';
import { RequiresPermission } from '../platform/decorators/requires-permission.decorator.js';

import { TreasuryService, type TransferInput, type VoucherInput, type VoucherPostInput, type ShiftCount } from './treasury.service.js';

@Controller()
export class TreasuryController {
  constructor(private readonly treasury: TreasuryService) {}
  @Get('vouchers') @RequiresPermission('treasury.view') vouchers() { return this.treasury.vouchers(getTenantContext().tenantId); }
  @Get('vouchers/:id') @RequiresPermission('treasury.view') getVoucher(@Param('id') id: string) { return this.treasury.getVoucher(getTenantContext().tenantId, id); }
  @Post('vouchers') @RequiresPermission('treasury.voucher.create') createVoucher(@Body() body: VoucherInput) { return this.treasury.createVoucher(getTenantContext().tenantId, body); }
  @Patch('vouchers/:id') @RequiresPermission('treasury.voucher.create') updateVoucher(@Param('id') id: string, @Body() body: Partial<VoucherInput>) { return this.treasury.updateDraftVoucher(getTenantContext().tenantId, id, body); }
  @Post('vouchers/:id/post') @RequiresPermission('treasury.voucher.post') postVoucher(@Param('id') id: string, @Body() body: VoucherPostInput) { return this.treasury.postVoucher(getTenantContext().tenantId, id, body); }
  @Post('vouchers/:id/void') @RequiresPermission('treasury.voucher.void') voidVoucher(@Param('id') id: string, @Body() body: { reason: string }) { return this.treasury.voidVoucher(getTenantContext().tenantId, id, body.reason); }
  @Post('vouchers/:id/cheque') @RequiresPermission('treasury.cheque.clear') cheque(@Param('id') id: string, @Body() body: { action: 'clear' | 'bounce' | 'collect' }) { return this.treasury.transitionCheque(getTenantContext().tenantId, id, body.action); }
  @Get('cash-transfers') @RequiresPermission('treasury.view') transfers() { return this.treasury.transfers(getTenantContext().tenantId); }
  @Post('cash-transfers') @RequiresPermission('treasury.transfer.manage') createTransfer(@Body() body: TransferInput) { return this.treasury.createTransfer(getTenantContext().tenantId, body); }
  @Post('cash-transfers/:id/send') @RequiresPermission('treasury.transfer.manage') sendTransfer(@Param('id') id: string) { return this.treasury.sendTransfer(getTenantContext().tenantId, id); }
  @Post('cash-transfers/:id/receive') @RequiresPermission('treasury.transfer.manage') receiveTransfer(@Param('id') id: string) { return this.treasury.receiveTransfer(getTenantContext().tenantId, id); }
  @Get('expense-types') @RequiresPermission('treasury.view') expenseTypes() { return this.treasury.listExpenseTypes(getTenantContext().tenantId); }
  @Post('expense-types') @RequiresPermission('treasury.expensetype.manage') createExpenseType(@Body() body: { nameAr: string; nameEn?: string; accountId: string; costCenterId?: string }) { return this.treasury.createExpenseType(getTenantContext().tenantId, body); }
  @Post('shift-closes/open') @RequiresPermission('treasury.shift.close') openShift(@Body() body: { branchId: string; userId?: string }) { const ctx = getTenantContext(); return this.treasury.openShift(ctx.tenantId, body.branchId, body.userId ?? ctx.userId); }
  @Get('shift-closes/current') @RequiresPermission('treasury.view') currentShift(@Query('branch_id') branchId: string, @Query('user_id') userId?: string) { const ctx = getTenantContext(); return this.treasury.currentShift(ctx.tenantId, branchId, userId ?? ctx.userId); }
  @Get('shift-closes') @RequiresPermission('treasury.view') history(@Query('branch_id') branchId?: string) { return this.treasury.history(getTenantContext().tenantId, branchId); }
  @Post('shift-closes/:id/close') @RequiresPermission('treasury.shift.close') closeShift(@Param('id') id: string, @Body() body: { counts: ShiftCount[] }) { return this.treasury.closeShift(getTenantContext().tenantId, id, body.counts); }
  @Get('shift-closes/:id/print-data') @RequiresPermission('treasury.view') printShift(@Param('id') id: string) { return this.treasury.printShiftData(getTenantContext().tenantId, id); }
  @Get('cash-locations/:id/balance') @RequiresPermission('treasury.view') cashLocationBalance(@Param('id') id: string, @Query('currency') currency?: string) { return this.treasury.getCashBalance(getTenantContext().tenantId, id, currency); }
  @Post('cash-locations/:id/recalc-balance') @RequiresPermission('treasury.transfer.manage') recalc(@Param('id') id: string, @Query('currency') currency?: string) { return this.treasury.recalcCashBalance(getTenantContext().tenantId, id, currency); }
}
