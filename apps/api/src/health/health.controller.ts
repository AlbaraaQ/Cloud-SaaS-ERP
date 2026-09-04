import { Controller, Get } from '@nestjs/common';

import { DatabaseService } from '../database/database.service.js';

@Controller('health')
export class HealthController {
  constructor(private readonly databaseService: DatabaseService) {}

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
