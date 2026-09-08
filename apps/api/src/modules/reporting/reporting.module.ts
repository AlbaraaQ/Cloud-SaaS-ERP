import { Module } from '@nestjs/common';

import { DatabaseModule } from '../../database/database.module.js';

import { PrintTemplatesService } from './print-templates.service.js';
import { ReportLayoutsService } from './report-layouts.service.js';
import { ReportingController } from './reporting.controller.js';
import { ReportingService } from './reporting.service.js';

@Module({
  imports: [DatabaseModule],
  controllers: [ReportingController],
  providers: [ReportingService, ReportLayoutsService, PrintTemplatesService],
  exports: [ReportingService, ReportLayoutsService, PrintTemplatesService],
})
export class ReportingModule {}
