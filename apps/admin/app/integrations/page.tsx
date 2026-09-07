import { ModulePage, type Feature } from '../../components/module-page';
const features: Feature[] = [
  { name: 'Salla OAuth connection', endpoint: '/integrations/salla/connections', action: 'encrypted OAuth tokens and webhook secret' },
  { name: 'Branch/warehouse mapping', endpoint: '/integrations/salla/branch-mappings', action: 'map Salla branch to ERP branch/warehouse/safe' },
  { name: 'Sync monitor', endpoint: '/integrations/salla/export-log', action: 'queue status, retries and request/response logs' },
  { name: 'Diff detector', endpoint: '/integrations/salla/export-queue', action: 'name/price/cost/qty changes since last export' },
  { name: 'Orders webhook', endpoint: '/integrations/salla/webhooks/{storeId}/orders', action: 'HMAC verified order ingestion to sales invoice' },
];
export default function Page() { return <ModulePage title="تكاملات المتاجر" subtitle="تكامل سلة: OAuth، المزامنة، الفروقات، والويب هوك." features={features} kind="integrations" />; }
