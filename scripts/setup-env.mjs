#!/usr/bin/env node
/* eslint-disable no-console */
/**
 * `pnpm env:setup` — create a working `.env` at the repository root.
 *
 * Generates the two secrets that cannot have a default (the RS256 JWT key pair and the
 * AES-256-GCM data-encryption key), copies every documented variable from
 * `infrastructure/env/.env.example`, and never overwrites an existing `.env` unless
 * `--force` is passed.
 */
import { generateKeyPairSync, randomBytes } from 'node:crypto';
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const root = join(here, '..');
const target = join(root, '.env');
const force = process.argv.includes('--force');

if (existsSync(target) && !force) {
  console.log(`.env already exists at ${target} — nothing to do (use --force to regenerate).`);
  process.exit(0);
}

const { privateKey, publicKey } = generateKeyPairSync('rsa', {
  modulusLength: 2048,
  privateKeyEncoding: { type: 'pkcs8', format: 'pem' },
  publicKeyEncoding: { type: 'spki', format: 'pem' },
});

const escape = (pem) => pem.trim().replace(/\r?\n/g, '\\n');
const dataEncKey = randomBytes(32).toString('base64');
const fileSigningSecret = randomBytes(32).toString('base64');

const templatePath = join(root, '.env.example');
let content = existsSync(templatePath) ? readFileSync(templatePath, 'utf8') : '';

const replacements = {
  JWT_PRIVATE_KEY: `"${escape(privateKey)}"`,
  JWT_PUBLIC_KEY: `"${escape(publicKey)}"`,
  DATA_ENC_KEY: dataEncKey,
  FILE_URL_SIGNING_SECRET: fileSigningSecret,
};

for (const [key, value] of Object.entries(replacements)) {
  const pattern = new RegExp(`^${key}=.*$`, 'm');
  content = pattern.test(content) ? content.replace(pattern, `${key}=${value}`) : `${content}\n${key}=${value}\n`;
}

writeFileSync(target, content, { mode: 0o600 });

console.log(`Wrote ${target}`);
console.log('  • generated a fresh RS256 key pair (JWT_PRIVATE_KEY / JWT_PUBLIC_KEY)');
console.log('  • generated DATA_ENC_KEY and FILE_URL_SIGNING_SECRET (32 random bytes each)');
console.log('');
console.log('Next steps:');
console.log('  1. pnpm db:up            # postgres + redis + minio via docker compose');
console.log('  2. pnpm db:roles         # create the erp_api / erp_migrator roles');
console.log('  3. pnpm db:migrate       # apply the SQL migrations');
console.log("  4. DEMO_OWNER_PASSWORD='ChangeMe!Strong123' pnpm db:seed");
console.log('  5. pnpm dev              # api :3000, admin :3001, customer :3002');
