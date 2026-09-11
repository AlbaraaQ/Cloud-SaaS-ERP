'use client';

import { InventoryDocumentWorkspace } from '../../../components/inventory-document-workspace';

/** Stand-alone receipt for goods that are not being received through a purchase invoice. */
export default function InventoryReceiptsPage() {
  return (
    <InventoryDocumentWorkspace
      kind="receipt"
      title="فاتورة إدخال مخزني"
      subtitle="استلام مواد إلى المستودع كوحدة ودفعة/سيريال عند الحاجة، مع قيد تلقائي عند الترحيل."
      newLabel="فاتورة إدخال جديدة"
    />
  );
}
