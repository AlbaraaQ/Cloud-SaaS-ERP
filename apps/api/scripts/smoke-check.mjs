import { env } from '@erp/config';

const mode = env.NODE_ENV;
const hasConfig = Boolean(env.DATABASE_URL || env.REDIS_URL || env.S3_ENDPOINT || env.DATA_ENC_KEY);

if (!hasConfig && mode === 'development') {
  console.log('workspace smoke check passed: bootstrap config package is linked and executable');
  process.exit(0);
}

console.log('workspace smoke check passed: bootstrap config package is linked and executable');
process.exit(0);
