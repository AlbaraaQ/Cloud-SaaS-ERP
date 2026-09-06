/**
 * `pnpm db:seed` — idempotent platform seed (PHASE_03 §5.7).
 *
 * Lives in the API app because hashing a password is a platform-module concern
 * (Argon2id, PROJECT_CONTRACT §9); `packages/database` only receives the finished hash.
 *
 * Usage:
 *   DATABASE_MIGRATOR_URL=postgres://… DEMO_OWNER_PASSWORD='…' pnpm db:seed
 */
import { env } from '@erp/config';
import { seedPlatform } from '@erp/database';

import { PasswordService } from '../src/modules/platform/auth/password.service.js';

async function main(): Promise<void> {
  const password = process.env.DEMO_OWNER_PASSWORD;
  const passwords = new PasswordService();

  let ownerPasswordHash: string | undefined;
  if (password) {
    passwords.assertPolicy(password, { email: process.env.DEMO_OWNER_EMAIL ?? 'owner@demo.test' });
    ownerPasswordHash = await passwords.hash(password);
  } else {
    console.log(
      'DEMO_OWNER_PASSWORD not set — the demo owner is created without a password ' +
        '(status=invited, must_change_password=true). No password is ever generated silently.',
    );
  }

  const report = await seedPlatform(env.DATABASE_MIGRATOR_URL ?? env.DATABASE_URL, {
    tenantCode: process.env.DEMO_TENANT_CODE ?? 'demo',
    tenantName: process.env.DEMO_TENANT_NAME,
    ownerEmail: process.env.DEMO_OWNER_EMAIL,
    ownerFullName: process.env.DEMO_OWNER_NAME,
    ownerPasswordHash,
    log: (message) => console.log(message),
  });

  console.log(
    `seed complete — tenant ${report.tenantId}, owner user ${report.userId}, ` +
      `membership ${report.membershipId}, roles [${report.roles.join(', ')}]`,
  );
}

main().catch((error: unknown) => {
  console.error(error instanceof Error ? error.message : String(error));
  process.exitCode = 1;
});
