import { Body, Controller, Get, Param, Post, Query } from '@nestjs/common';

import { getTenantContext } from '../platform/context/tenant-context.js';
import { RequiresPermission } from '../platform/decorators/requires-permission.decorator.js';

import {
  PosService,
  type PosHeldTicketInput,
  type PosShortcutInput,
  type DiningTableInput,
  type OrderItemInput,
  type PosCheckoutInput,
  type TableCategoryInput,
} from './pos.service.js';

@Controller('pos')
export class PosController {
  constructor(private readonly pos: PosService) {}

  @Get('held-tickets')
  @RequiresPermission('pos.operate')
  heldTickets(@Query('branch_id') branchId?: string, @Query('history') history?: string) {
    const ctx = getTenantContext();
    return this.pos.listHeldTickets(ctx.tenantId, ctx.userId, branchId, history === 'true');
  }

  @Post('held-tickets')
  @RequiresPermission('pos.operate')
  holdTicket(@Body() body: PosHeldTicketInput) {
    const ctx = getTenantContext();
    return this.pos.holdTicket(ctx.tenantId, ctx.userId, body);
  }

  @Post('held-tickets/:id/recall')
  @RequiresPermission('pos.operate')
  recallHeldTicket(@Param('id') id: string) {
    const ctx = getTenantContext();
    return this.pos.recallHeldTicket(ctx.tenantId, ctx.userId, id);
  }

  @Post('held-tickets/:id/cancel')
  @RequiresPermission('pos.operate')
  cancelHeldTicket(@Param('id') id: string) {
    const ctx = getTenantContext();
    return this.pos.cancelHeldTicket(ctx.tenantId, ctx.userId, id);
  }

  @Get('shortcuts')
  @RequiresPermission('pos.view')
  shortcuts(@Query('branch_id') branchId: string) {
    return this.pos.listShortcuts(getTenantContext().tenantId, branchId);
  }

  @Post('shortcuts')
  @RequiresPermission('pos.config.manage')
  setShortcut(@Body() body: PosShortcutInput) {
    const ctx = getTenantContext();
    return this.pos.setShortcut(ctx.tenantId, ctx.userId, body);
  }

  @Post('shortcuts/:slot/remove')
  @RequiresPermission('pos.config.manage')
  removeShortcut(@Param('slot') slot: string, @Body() body: { branchId: string }) {
    return this.pos.removeShortcut(getTenantContext().tenantId, body.branchId, Number(slot));
  }

  @Get('categories') @RequiresPermission('pos.view') categories(@Query('branchId') branchId?: string) {
    return this.pos.categories(getTenantContext().tenantId, branchId);
  }
  @Post('categories') @RequiresPermission('pos.config.manage') createCategory(
    @Body() body: TableCategoryInput,
  ) {
    return this.pos.createCategory(getTenantContext().tenantId, body);
  }
  @Get('tables') @RequiresPermission('pos.view') tables(@Query('branchId') branchId?: string) {
    return this.pos.tables(getTenantContext().tenantId, branchId);
  }
  @Post('tables') @RequiresPermission('pos.tables.manage') createTable(@Body() body: DiningTableInput) {
    return this.pos.createTable(getTenantContext().tenantId, body);
  }
  @Post('tables/:id/open') @RequiresPermission('pos.operate') open(
    @Param('id') id: string,
    @Body() body: { businessDay?: string },
  ) {
    return this.pos.openTable(getTenantContext().tenantId, id, body.businessDay);
  }
  @Post('tables/:id/items') @RequiresPermission('pos.operate') addItem(
    @Param('id') id: string,
    @Body() body: OrderItemInput & { businessDay?: string },
  ) {
    return this.pos.addItem(getTenantContext().tenantId, id, body, body.businessDay);
  }
  @Post('events/:id/void') @RequiresPermission('pos.operate') voidItem(
    @Param('id') id: string,
    @Body() body: { reason: string },
  ) {
    return this.pos.voidItem(getTenantContext().tenantId, id, body.reason);
  }
  @Post('tables/:id/send-to-invoice') @RequiresPermission('pos.operate') send(
    @Param('id') id: string,
    @Body() body: { cashCustomerName?: string; orderType?: string; businessDay?: string },
  ) {
    return this.pos.sendToInvoice(
      getTenantContext().tenantId,
      id,
      body.cashCustomerName,
      body.orderType,
      body.businessDay,
    );
  }
  @Post('tables/:id/close') @RequiresPermission('pos.operate') close(
    @Param('id') id: string,
    @Body()
    body: {
      settlement?: 'credit' | 'cash' | 'card' | 'bank';
      cashLocationId?: string;
      settlementAccountId?: string;
    },
  ) {
    return this.pos.close(getTenantContext().tenantId, id, body);
  }
  /**
   * The till checkout. One call creates, posts, relieves stock and settles the
   * sale — and links it to the cashier's open shift so the day-close report can
   * count the drawer. Requires `sales.invoice.post` as well as `pos.operate`
   * because it writes a posted invoice and a journal entry.
   */
  @Post('checkout') @RequiresPermission('pos.operate', 'sales.invoice.post') checkout(
    @Body() body: PosCheckoutInput,
  ) {
    const ctx = getTenantContext();
    return this.pos.checkout(ctx.tenantId, ctx.userId, body);
  }
  @Post('tables/:sourceId/merge/:targetId') @RequiresPermission('pos.operate') merge(
    @Param('sourceId') sourceId: string,
    @Param('targetId') targetId: string,
  ) {
    return this.pos.merge(getTenantContext().tenantId, sourceId, targetId);
  }
  @Post('tables/:id/split') @RequiresPermission('pos.operate') split(@Param('id') id: string) {
    return this.pos.split(getTenantContext().tenantId, id);
  }
}
