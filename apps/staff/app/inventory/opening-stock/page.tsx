'use client';

import { InventoryDocumentWorkspace } from '../../../components/inventory-document-workspace';

/** بضاعة أول المدة is a dedicated document, never a redirected stock adjustment. */
export default function OpeningStockPage() {
  return (
    <InventoryDocumentWorkspace
      kind="opening"
      title="بضاعة أول المدة"
      subtitle="إثبات أرصدة المواد الافتتاحية بوحدة الإدخال وتكلفتها، ثم ترحيل حركة المخزون والقيد الافتتاحي معاً."
      newLabel="رصيد افتتاحي جديد"
    />
  );
}
