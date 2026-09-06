var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
import { Module } from '@nestjs/common';
import { ConfigModule } from '@nestjs/config';
import { LoggerModule } from 'nestjs-pino';
import { env } from '@erp/config';
import { DatabaseService } from './database/database.service.js';
import { HealthController } from './health/health.controller.js';
import { RequestContextInterceptor } from './common/interceptors/request-context.interceptor.js';
let AppModule = class AppModule {
};
AppModule = __decorate([
    Module({
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
], AppModule);
export { AppModule };
