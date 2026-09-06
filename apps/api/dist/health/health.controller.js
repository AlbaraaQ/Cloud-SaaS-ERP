var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
import { Controller, Get } from '@nestjs/common';
import { DatabaseService } from '../database/database.service.js';
let HealthController = class HealthController {
    databaseService;
    constructor(databaseService) {
        this.databaseService = databaseService;
    }
    live() {
        return { status: 'ok', service: 'api' };
    }
    async ready() {
        const dbReady = await this.databaseService.checkConnection();
        return {
            status: dbReady ? 'ok' : 'degraded',
            db: dbReady ? 'connected' : 'unavailable',
        };
    }
};
__decorate([
    Get('live'),
    __metadata("design:type", Function),
    __metadata("design:paramtypes", []),
    __metadata("design:returntype", void 0)
], HealthController.prototype, "live", null);
__decorate([
    Get('ready'),
    __metadata("design:type", Function),
    __metadata("design:paramtypes", []),
    __metadata("design:returntype", Promise)
], HealthController.prototype, "ready", null);
HealthController = __decorate([
    Controller('health'),
    __metadata("design:paramtypes", [DatabaseService])
], HealthController);
export { HealthController };
