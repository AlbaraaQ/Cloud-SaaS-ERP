import { Body, Controller, Get, Param, Post, Query } from '@nestjs/common';

import { getTenantContext } from '../platform/context/tenant-context.js';
import { RequiresPermission } from '../platform/decorators/requires-permission.decorator.js';

import { HrmService, type AdjustmentInput, type DepartmentInput, type EmployeeInput, type JobInput, type PayRunInput, type PostRunInput, type RunInput } from './hrm.service.js';

@Controller('hrm')
export class HrmController {
  constructor(private readonly hrm: HrmService) {}
  @Get('departments') @RequiresPermission('hrm.view') departments() { return this.hrm.listDepartments(getTenantContext().tenantId); }
  @Post('departments') @RequiresPermission('hrm.manage') createDepartment(@Body() body: DepartmentInput) { return this.hrm.createDepartment(getTenantContext().tenantId, body); }
  @Get('jobs') @RequiresPermission('hrm.view') jobs() { return this.hrm.listJobs(getTenantContext().tenantId); }
  @Post('jobs') @RequiresPermission('hrm.manage') createJob(@Body() body: JobInput) { return this.hrm.createJob(getTenantContext().tenantId, body); }
  @Get('employees') @RequiresPermission('hrm.view') employees() { return this.hrm.listEmployees(getTenantContext().tenantId); }
  @Post('employees') @RequiresPermission('hrm.manage') createEmployee(@Body() body: EmployeeInput) { return this.hrm.createEmployee(getTenantContext().tenantId, body); }
  @Post('attendance/import') @RequiresPermission('hrm.manage') importAttendance(@Body() body: { csv: string }) { return this.hrm.importAttendanceCsv(getTenantContext().tenantId, body.csv); }
  @Get('attendance/summary') @RequiresPermission('hrm.view') attendanceSummary(@Query('enroll') enroll: string, @Query('from') from: string, @Query('to') to: string) { return this.hrm.attendanceSummary(getTenantContext().tenantId, enroll, from, to); }
  @Post('adjustments') @RequiresPermission('hrm.manage') createAdjustment(@Body() body: AdjustmentInput) { return this.hrm.createAdjustment(getTenantContext().tenantId, body); }
  @Post('adjustments/:id/approve') @RequiresPermission('hrm.adjust.approve') approveAdjustment(@Param('id') id: string) { return this.hrm.approveAdjustment(getTenantContext().tenantId, id); }
  @Post('payroll/preview') @RequiresPermission('hrm.view') preview(@Body() body: RunInput) { return this.hrm.preview(getTenantContext().tenantId, body); }
  @Get('payroll/runs') @RequiresPermission('hrm.view') runs() { return this.hrm.listRuns(getTenantContext().tenantId); }
  @Post('payroll/runs') @RequiresPermission('hrm.manage') createRun(@Body() body: RunInput) { return this.hrm.createRun(getTenantContext().tenantId, body); }
  @Get('payroll/runs/:id') @RequiresPermission('hrm.view') readRun(@Param('id') id: string) { return this.hrm.readRun(getTenantContext().tenantId, id); }
  @Post('payroll/runs/:id/post') @RequiresPermission('hrm.payroll.post') postRun(@Param('id') id: string, @Body() body: PostRunInput) { return this.hrm.postRun(getTenantContext().tenantId, id, body); }
  @Post('payroll/runs/:id/pay') @RequiresPermission('hrm.payroll.post') payRun(@Param('id') id: string, @Body() body: PayRunInput) { return this.hrm.payRun(getTenantContext().tenantId, id, body); }
  @Post('payroll/runs/:id/reverse') @RequiresPermission('hrm.payroll.post') reverseRun(@Param('id') id: string, @Body() body: { reason: string }) { return this.hrm.reverseRun(getTenantContext().tenantId, id, body.reason); }
}
