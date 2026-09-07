import { Module } from '@nestjs/common';

import { DatabaseModule } from '../../database/database.module.js';
import { SalesModule } from '../sales/sales.module.js';

import { MarinaController } from './marina.controller.js';
import { MarinaService } from './marina.service.js';
@Module({ imports: [DatabaseModule, SalesModule], controllers: [MarinaController], providers: [MarinaService] })
export class MarinaModule {}
