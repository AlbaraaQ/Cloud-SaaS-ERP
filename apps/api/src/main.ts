import 'reflect-metadata';

import { NestFactory } from '@nestjs/core';
import { Logger } from 'nestjs-pino';
import { assertRuntimeEnv, env } from '@erp/config';

import { AppModule } from './app.module.js';
import { applyHttpConfiguration, buildOpenApiDocument, registerOpenApi } from './bootstrap.js';
import { RequestIdMiddleware } from './common/middleware/request-id.middleware.js';

async function bootstrap(): Promise<void> {
  // PHASE_02 §8: fail fast on a missing runtime variable instead of degrading silently.
  assertRuntimeEnv();

  const app = await NestFactory.create(AppModule, { bufferLogs: true });

  app.use(RequestIdMiddleware);
  applyHttpConfiguration(app);
  app.useLogger(app.get(Logger));

  registerOpenApi(app, buildOpenApiDocument(app));

  await app.listen(env.PORT, env.API_HOST);

  console.log(`erp-api listening on http://${env.API_HOST}:${env.PORT} (${env.NODE_ENV})`);
}

void bootstrap();
