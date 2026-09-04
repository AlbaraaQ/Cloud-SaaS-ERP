import { Module } from '@nestjs/common';
import { ConfigModule } from '@nestjs/config';
import { LoggerModule } from 'nestjs-pino';

import { env } from '@erp/config';

import { DatabaseService } from './database/database.service.js';
import { HealthController } from './health/health.controller.js';
import { RequestContextInterceptor } from './common/interceptors/request-context.interceptor.js';

@Module({
  imports: [
    ConfigModule.forRoot({
      isGlobal: true,
      cache: true,
      envFilePath: ['.env', '.env.local'],
      validate: () => env,
    }),
    LoggerModule.forRoot({
      pinoHttp: {
        level: env.NODE_ENV === 'production' ? 'info' : 'debug',
        customProps: () => ({
          service: 'erp-api',
        }),
        redact: ['req.headers.authorization', 'req.headers.cookie', 'password', 'secret', 'token', 'key'],
      },
    }),
  ],
  controllers: [HealthController],
  providers: [DatabaseService, RequestContextInterceptor],
})
export class AppModule {}
