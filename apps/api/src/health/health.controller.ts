import { Controller, Get } from '@nestjs/common';

import { DatabaseService } from '../database/database.service.js';

@Controller('health')
export class HealthController {
  private readonly databaseService: DatabaseService;

  constructor(databaseService: DatabaseService) {
    this.databaseService = databaseService;
  }

  @Get('live')
  live() {
    return { status: 'ok', service: 'api' };
  }

  @Get('ready')
  async ready() {
    const dbReady = await this.databaseService.checkConnection();
    return {
      status: dbReady ? 'ok' : 'degraded',
      db: dbReady ? 'connected' : 'unavailable',
    };
  }
}
