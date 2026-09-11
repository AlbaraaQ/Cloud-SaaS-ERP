'use client';

import { InventoryDocumentWorkspace } from '../../../components/inventory-document-workspace';

/** Stand-alone issue for consumption/operations that are not a sales invoice. */
export default function InventoryIssuesPage() {
  return (
    <InventoryDocumentWorkspace
      kind="issue"
      title="فاتورة إخراج مخزني"
      subtitle="إخراج مواد للاستهلاك أو التشغيل؛ تعتمد تكلفة المخزون المتحركة عند الترحيل ويُنشأ القيد تلقائياً."
      newLabel="فاتورة إخراج جديدة"
    />
  );
}
