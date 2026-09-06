import { Body, Controller, Get, Param, Post } from '@nestjs/common';

import { getTenantContext } from '../platform/context/tenant-context.js';
import { RequiresPermission } from '../platform/decorators/requires-permission.decorator.js';

import { AccountingService, type AccountInput, type JournalLineInput } from './accounting.service.js';

@Controller()
export class AccountingController {
  constructor(private readonly accounting: AccountingService) {}

  @Get('accounts')
  @RequiresPermission('accounting.account.view')
  async listAccounts() {
    return { data: await this.accounting.listAccounts(getTenantContext().tenantId) };
  }

  @Post('accounts')
  @RequiresPermission('accounting.account.manage')
  async createAccount(@Body() body: AccountInput) {
    return { data: await this.accounting.createAccount(getTenantContext().tenantId, body) };
  }

  @Get('accounts/:id')
  @RequiresPermission('accounting.account.view')
  async readAccount(@Param('id') id: string) {
    return { data: await this.accounting.readAccount(getTenantContext().tenantId, id) };
  }

  @Post('journal-entries')
  @RequiresPermission('accounting.journal.post')
  async postJournal(@Body() body: { branchId: string; fiscalPeriodId: string; date: string; description?: string; lines: JournalLineInput[] }) {
    return { data: await this.accounting.postJournal(getTenantContext().tenantId, body) };
  }
}
