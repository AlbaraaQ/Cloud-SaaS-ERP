import { Body, Controller, Delete, Get, Param, Patch, Post, Query } from '@nestjs/common';
import { ApiOperation, ApiTags } from '@nestjs/swagger';

import { getTenantContext } from '../platform/context/tenant-context.js';
import { RequiresPermission } from '../platform/decorators/requires-permission.decorator.js';

import {
  AccountingService,
  type AccountInput,
  type AccountPatch,
  type CostCenterInput,
  type CostCenterPatch,
  type FiscalYearInput,
  type JournalLineInput,
} from './accounting.service.js';

@ApiTags('accounting')
@Controller()
export class AccountingController {
  constructor(private readonly accounting: AccountingService) {}

  /**
   * 📂 شجرة الحسابات و 📋 تفاصيل الحسابات — `frmAccountsDirectory.xaml`. `q` searches
   * كود/اسم الحساب (the window's 🔍 → `frmAccountSrch`), `type` and `branchId` narrow the
   * directory, and `withBalances=1` adds the الرصيد each node shows (`trBalance` in the
   * desktop's tree) — rolled up from **posted** entries only, because a balance that
   * counts drafts is a number that disappears.
   */
  @Get('accounts')
  @RequiresPermission('accounting.account.view')
  async listAccounts(
    @Query('q') q?: string,
    @Query('type') type?: string,
    @Query('branch_id') branchId?: string,
    @Query('with_balances') withBalances?: string,
  ) {
    return {
      data: await this.accounting.listAccounts(getTenantContext().tenantId, {
        q,
        type,
        branchId,
        withBalances: withBalances === '1' || withBalances === 'true',
      }),
    };
  }

  @Post('accounts')
  @RequiresPermission('accounting.account.manage')
  async createAccount(@Body() body: AccountInput) {
    return { data: await this.accounting.createAccount(getTenantContext().tenantId, body) };
  }

  @Patch('accounts/:id')
  @RequiresPermission('accounting.account.manage')
  @ApiOperation({ summary: 'Edit an account card' })
  async updateAccount(@Param('id') id: string, @Body() body: AccountPatch) {
    return { data: await this.accounting.updateAccount(getTenantContext().tenantId, id, body) };
  }

  @Delete('accounts/:id')
  @RequiresPermission('accounting.account.manage')
  @ApiOperation({ summary: 'Delete an unused account' })
  async deleteAccount(@Param('id') id: string) {
    return { data: await this.accounting.deleteAccount(getTenantContext().tenantId, id) };
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

  @Post('fiscal-periods/:id/modules/:module/lock')
  @RequiresPermission('accounting.period.close')
  async lockModule(@Param('id') id: string, @Param('module') module: string) {
    return { data: await this.accounting.lockModule(getTenantContext().tenantId, id, module) };
  }

  @Post('fiscal-periods/:id/modules/:module/unlock')
  @RequiresPermission('accounting.period.reopen')
  async unlockModule(@Param('id') id: string, @Param('module') module: string) {
    return { data: await this.accounting.unlockModule(getTenantContext().tenantId, id, module) };
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

  // ------------------------------------------------------------- cost centres

  @Get('cost-centers')
  @RequiresPermission('accounting.account.view')
  @ApiOperation({ summary: 'List cost centres' })
  async listCostCenters() {
    return { data: await this.accounting.listCostCenters(getTenantContext().tenantId) };
  }

  @Post('cost-centers')
  @RequiresPermission('accounting.account.manage')
  @ApiOperation({ summary: 'Create a cost centre' })
  async createCostCenter(@Body() body: CostCenterInput) {
    return { data: await this.accounting.createCostCenter(getTenantContext().tenantId, body) };
  }

  @Patch('cost-centers/:id')
  @RequiresPermission('accounting.account.manage')
  @ApiOperation({ summary: 'Edit a cost centre' })
  async updateCostCenter(@Param('id') id: string, @Body() body: CostCenterPatch) {
    return { data: await this.accounting.updateCostCenter(getTenantContext().tenantId, id, body) };
  }

  @Delete('cost-centers/:id')
  @RequiresPermission('accounting.account.manage')
  @ApiOperation({ summary: 'Delete an unused cost centre' })
  async deleteCostCenter(@Param('id') id: string) {
    return { data: await this.accounting.deleteCostCenter(getTenantContext().tenantId, id) };
  }

  // ------------------------------------------------------------- fiscal calendar

  @Get('fiscal-years')
  @RequiresPermission('accounting.period.view')
  @ApiOperation({ summary: 'List fiscal years' })
  async listFiscalYears() {
    return { data: await this.accounting.listFiscalYears(getTenantContext().tenantId) };
  }

  @Post('fiscal-years')
  @RequiresPermission('accounting.period.close')
  @ApiOperation({ summary: 'Open a fiscal year and generate its monthly periods' })
  async createFiscalYear(@Body() body: FiscalYearInput) {
    return { data: await this.accounting.createFiscalYear(getTenantContext().tenantId, body) };
  }

  // ------------------------------------------------------------- journal register

  @Get('journal-entries')
  @RequiresPermission('accounting.reports.view')
  @ApiOperation({ summary: 'Journal register (القيود اليومية) with per-entry totals' })
  async listJournalEntries(
    @Query('from') from?: string,
    @Query('to') to?: string,
    @Query('branchId') branchId?: string,
    @Query('fiscalPeriodId') fiscalPeriodId?: string,
    @Query('status') status?: string,
    @Query('limit') limit?: string,
  ) {
    return {
      data: await this.accounting.listJournalEntries(getTenantContext().tenantId, {
        from,
        to,
        branchId,
        fiscalPeriodId,
        status,
        limit: limit ? Number(limit) : undefined,
      }),
    };
  }

  @Get('journal-entries/:id')
  @RequiresPermission('accounting.reports.view')
  @ApiOperation({ summary: 'Read one journal entry with its lines' })
  async readJournalEntry(@Param('id') id: string) {
    return { data: await this.accounting.readJournalEntry(getTenantContext().tenantId, id) };
  }

  @Post('journal-entries')
  @RequiresPermission('accounting.journal.post')
  @ApiOperation({
    summary: 'Post a balanced journal entry (branch and fiscal period are derived from the date when omitted)',
  })
  async postJournal(
    @Body()
    body: {
      branchId?: string;
      fiscalPeriodId?: string;
      date: string;
      description?: string;
      lines: JournalLineInput[];
    },
  ) {
    return { data: await this.accounting.postJournal(getTenantContext().tenantId, body) };
  }

  @Post('journal-entries/:id/reverse')
  @RequiresPermission('accounting.journal.reverse')
  async reverseJournal(@Param('id') id: string, @Body() body: { branchId: string; fiscalPeriodId: string; date: string; reason: string }) {
    return { data: await this.accounting.reverseJournal(getTenantContext().tenantId, id, body) };
  }
}
