'use client';

import { useRouter } from 'next/navigation';
import { useState } from 'react';

import { Notice } from '../../../../components/data-view';
import { InvoiceLines, TotalsPanel, computeTotals, emptyLine, filledLines, toApiLines, type LineDraft } from '../../../../components/invoice-editor';
import { Screen } from '../../../../components/screen';
import { ApiError, apiPost } from '../../../../lib/api';
import {
  arabicName,
  branchOptions,
  defaultOf,
  listBranches,
  listItems,
  listParties,
  listTaxGroups,
  listWarehouses,
  partyLabel,
  today,
  type Branch,
  type Item,
  type Party,
  type TaxGroup,
  type Warehouse,
} from '../../../../lib/lookups';
import { useSession } from '../../../../lib/session';
import { useQuery } from '../../../../lib/use-query';

export default function NewPurchaseInvoicePage() {
  const router = useRouter();
  const { can } = useSession();
  const branches = useQuery<Branch[]>(() => listBranches(), []);
  const warehouses = useQuery<Warehouse[]>(() => listWarehouses(), []);
  const suppliers = useQuery<Party[]>(() => listParties('supplier'), []);
  const items = useQuery<Item[]>(() => listItems(), []);
  const taxGroups = useQuery<TaxGroup[]>(() => listTaxGroups(), []);

  const [branchId, setBranchId] = useState('');
  const [warehouseId, setWarehouseId] = useState('');
  const [partyId, setPartyId] = useState('');
  const [kind, setKind] = useState<'purchase' | 'purchase_return'>('purchase');
  const [supplierRef, setSupplierRef] = useState('');
  const [supplierRefDate, setSupplierRefDate] = useState(today());
  const [includesVat, setIncludesVat] = useState(false);
  const [allocation, setAllocation] = useState<'value' | 'qty'>('value');
  const [invoiceDiscountText, setInvoiceDiscountText] = useState('');
  const [lines, setLines] = useState<LineDraft[]>([emptyLine()]);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();

  const branchRows = branches.data ?? [];
  const warehouseRows = warehouses.data ?? [];
  const effectiveBranch = branchId || defaultOf(branchRows)?.id || '';
  const effectiveWarehouse = warehouseId || defaultOf(warehouseRows.filter((row) => !effectiveBranch || row.branchId === effectiveBranch))?.id || '';
  const totals = computeTotals(lines, { priceIncludesVat: includesVat, invoiceDiscount: invoiceDiscountText });

  async function save(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setNotice(undefined);
    try {
      if (filledLines(lines).length === 0) throw new ApiError(422, 'VALIDATION_FAILED', 'أضف سطراً واحداً على الأقل.');
      const invoice = await apiPost<{ id: string }>('/purchase-invoices', {
        branchId: effectiveBranch,
        warehouseId: effectiveWarehouse || undefined,
        partyId,
        kind,
        supplierReferenceNo: supplierRef.trim() || undefined,
        supplierReferenceDate: supplierRefDate || undefined,
        priceIncludesVat: includesVat,
        invoiceDiscount: invoiceDiscountText.trim() || undefined,
        landedCostAlloc: allocation,
        lines: toApiLines(lines).map((line) => ({ ...line, itemId: line.itemId })),
      });
      router.push(`/purchases/invoices/${invoice.id}`);
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
      setBusy(false);
    }
  }

  if (!can('purchase.invoice.create')) {
    return (
      <Screen title="فاتورة مشتريات جديدة" crumbs={['المشتريات', 'العمليات']}>
        <div className="card state">
          <strong>لا تملك صلاحية إنشاء فواتير المشتريات</strong>
        </div>
      </Screen>
    );
  }

  return (
    <Screen
      title="فاتورة مشتريات جديدة"
      subtitle="تُحفظ كمسودة يمكن إضافة مصاريف الشحن والتخليص إليها، ثم تُرحَّل فتُحمَّل تلك المصاريف على تكلفة الأصناف."
      crumbs={['المشتريات', 'العمليات']}
    >
      <form className="card" onSubmit={save}>
        <div className="form-grid">
          <label className="field">
            <span>الفرع *</span>
            <select className="input" value={effectiveBranch} onChange={(event) => setBranchId(event.target.value)} required>
              {branchOptions(branchRows).map((option) => (
                <option key={option.id} value={option.id}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>المستودع</span>
            <select className="input" value={effectiveWarehouse} onChange={(event) => setWarehouseId(event.target.value)}>
              <option value="">— بدون حركة مخزنية —</option>
              {warehouseRows
                .filter((row) => !effectiveBranch || row.branchId === effectiveBranch)
                .map((row) => (
                  <option key={row.id} value={row.id}>
                    {arabicName(row)}
                  </option>
                ))}
            </select>
            {filledLines(lines).some((line) => line.itemId) && !effectiveWarehouse && (
              <span className="muted small">ترحيل فاتورة فيها أصناف مخزنية يتطلب اختيار مستودع.</span>
            )}
          </label>
          <label className="field">
            <span>المورد *</span>
            <select className="input" value={partyId} onChange={(event) => setPartyId(event.target.value)} required>
              <option value="">— اختر —</option>
              {(suppliers.data ?? []).map((row) => (
                <option key={row.id} value={row.id}>
                  {partyLabel(row)}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>النوع</span>
            <select className="input" value={kind} onChange={(event) => setKind(event.target.value as 'purchase' | 'purchase_return')}>
              <option value="purchase">فاتورة مشتريات</option>
              <option value="purchase_return">مردود مشتريات</option>
            </select>
          </label>
          <label className="field">
            <span>رقم فاتورة المورد</span>
            <input className="input" dir="ltr" value={supplierRef} onChange={(event) => setSupplierRef(event.target.value)} />
          </label>
          <label className="field">
            <span>تاريخ فاتورة المورد</span>
            <input className="input" type="date" dir="ltr" value={supplierRefDate} onChange={(event) => setSupplierRefDate(event.target.value)} />
          </label>
          <label className="field">
            <span>خصم على الفاتورة</span>
            <input className="input" dir="ltr" inputMode="decimal" value={invoiceDiscountText} onChange={(event) => setInvoiceDiscountText(event.target.value)} />
            <span className="muted small">يُخفّض وعاء الضريبة قبل احتسابها.</span>
          </label>
          <label className="field">
            <span>توزيع المصاريف</span>
            <select className="input" value={allocation} onChange={(event) => setAllocation(event.target.value as 'value' | 'qty')}>
              <option value="value">بحسب القيمة</option>
              <option value="qty">بحسب الكمية</option>
            </select>
          </label>
          <label className="field">
            <span>الأسعار شاملة الضريبة</span>
            <span className="row">
              <input type="checkbox" checked={includesVat} onChange={(event) => setIncludesVat(event.target.checked)} />
              <span className="muted small">تُستخرج الضريبة من السعر بدل إضافتها إليه.</span>
            </span>
          </label>
        </div>

        <h2>الأصناف</h2>
        <InvoiceLines lines={lines} onChange={setLines} items={items.data ?? []} taxGroups={taxGroups.data ?? []} priceField="purchasePrice" />

        <h2>الإجماليات</h2>
        <TotalsPanel totals={totals} />

        <Notice notice={notice} />
        <button className="btn primary" type="submit" disabled={busy}>
          {busy ? 'جارٍ الحفظ…' : 'حفظ كمسودة'}
        </button>
      </form>
    </Screen>
  );
}
