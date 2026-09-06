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

  @Get('fiscal-periods')
  @RequiresPermission('accounting.period.view')
  async listPeriods() {
    return { data: await this.accounting.listPeriods(getTenantContext().tenantId) };
  }

  @Post('fiscal-periods/:id/close')
  @RequiresPermission('accounting.period.close')
  async closePeriod(@Param('id') id: string) {
    await this.accounting.closePeriod(getTenantContext().tenantId, id);
    return { data: { id, status: 'closed' } };
  }

  @Post('fiscal-periods/:id/reopen')
  @RequiresPermission('accounting.period.reopen')
  async reopenPeriod(@Param('id') id: string, @Body() body: { reason: string }) {
    await this.accounting.reopenPeriod(getTenantContext().tenantId, id, body.reason);
    return { data: { id, status: 'open' } };
  }

  @Get('statements/trial-balance')
  @RequiresPermission('accounting.reports.view')
  async trialBalance() {
    return { data: await this.accounting.trialBalance(getTenantContext().tenantId) };
  }

  @Get('statements/general-ledger/:accountId')
  @RequiresPermission('accounting.reports.view')
  async generalLedger(@Param('accountId') accountId: string) {
    return { data: await this.accounting.generalLedger(getTenantContext().tenantId, accountId) };
  }

  @Post('journal-entries')
  @RequiresPermission('accounting.journal.post')
  async postJournal(@Body() body: { branchId: string; fiscalPeriodId: string; date: string; description?: string; lines: JournalLineInput[] }) {
    return { data: await this.accounting.postJournal(getTenantContext().tenantId, body) };
  }

  @Post('journal-entries/:id/reverse')
  @RequiresPermission('accounting.journal.reverse')
  async reverseJournal(@Param('id') id: string, @Body() body: { branchId: string; fiscalPeriodId: string; date: string; reason: string }) {
    return { data: await this.accounting.reverseJournal(getTenantContext().tenantId, id, body) };
  }
}
