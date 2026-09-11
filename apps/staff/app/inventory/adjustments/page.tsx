'use client';

import { InventoryDocumentWorkspace } from '../../../components/inventory-document-workspace';

/**
 * The old browser-built adjustment + manual journal sequence was intentionally replaced.
 * A draft adjustment now carries every line's in/out direction and posts its immutable
 * inventory movements and generated counter-entry together on the server.
 */
export default function StockAdjustmentsPage() {
  return (
    <InventoryDocumentWorkspace
      kind="adjustment"
      title="تسوية وجرد مخزني"
      subtitle="سجل فروقات الجرد كسطور زيادة أو نقص، راجعها كمسودة ثم رحّل الحركة والقيد المحاسبي في عملية واحدة."
      newLabel="تسوية مخزنية جديدة"
    />
  );
}
