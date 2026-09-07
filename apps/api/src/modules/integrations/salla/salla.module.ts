import { Module } from '@nestjs/common';

import { DatabaseModule } from '../../../database/database.module.js';
import { SalesModule } from '../../sales/sales.module.js';

import { SallaController } from './salla.controller.js';
import { SallaService } from './salla.service.js';
@Module({ imports: [DatabaseModule, SalesModule], controllers: [SallaController], providers: [SallaService] })
export class SallaModule {}
