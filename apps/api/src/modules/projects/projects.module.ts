import { Module } from '@nestjs/common';

import { DatabaseModule } from '../../database/database.module.js';
import { PlatformServicesModule } from '../platform-services/index.js';
import { SalesModule } from '../sales/sales.module.js';
import { TreasuryModule } from '../treasury/treasury.module.js';

import { ContractingController } from './contracting.controller.js';
import { ContractingService } from './contracting.service.js';
import { ProjectsController } from './projects.controller.js';
import { ProjectsService } from './projects.service.js';

@Module({ imports: [DatabaseModule, PlatformServicesModule, SalesModule, TreasuryModule], controllers: [ProjectsController, ContractingController], providers: [ProjectsService, ContractingService], exports: [ProjectsService, ContractingService] })
export class ProjectsModule {}
