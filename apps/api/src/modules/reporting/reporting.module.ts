import { Module } from '@nestjs/common';

import { DatabaseModule } from '../../database/database.module.js';

import { ReportLayoutsService } from './report-layouts.service.js';
import { ReportingController } from './reporting.controller.js';
import { ReportingService } from './reporting.service.js';

@Module({
  imports: [DatabaseModule],
  controllers: [ReportingController],
  providers: [ReportingService, ReportLayoutsService],
  exports: [ReportingService, ReportLayoutsService],
})
export class ReportingModule {}
