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
  type Branch,
  type Item,
  type Party,
  type TaxGroup,
  type Warehouse,
} from '../../../../lib/lookups';
import { useSession } from '../../../../lib/session';
import { useQuery } from '../../../../lib/use-query';

export default function NewSalesInvoicePage() {
  const router = useRouter();
  const { can } = useSession();
  const branches = useQuery<Branch[]>(() => listBranches(), []);
  const warehouses = useQuery<Warehouse[]>(() => listWarehouses(), []);
  const parties = useQuery<Party[]>(() => listParties('customer'), []);
  const items = useQuery<Item[]>(() => listItems(), []);
  const taxGroups = useQuery<TaxGroup[]>(() => listTaxGroups(), []);

  const [branchId, setBranchId] = useState('');
  const [warehouseId, setWarehouseId] = useState('');
  const [partyId, setPartyId] = useState('');
  const [cashName, setCashName] = useState('');
  const [cashMobile, setCashMobile] = useState('');
  const [salesmanId, setSalesmanId] = useState('');
  const [includesVat, setIncludesVat] = useState(false);
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
      if (!partyId && !cashName.trim()) throw new ApiError(422, 'VALIDATION_FAILED', 'اختر عميلاً أو اكتب اسم العميل النقدي.');
      const invoice = await apiPost<{ id: string }>('/sales/invoices', {
        branchId: effectiveBranch,
        warehouseId: effectiveWarehouse || undefined,
        partyId: partyId || undefined,
        salesmanId: salesmanId || undefined,
        cashCustomerName: partyId ? undefined : cashName.trim(),
        cashCustomerMobile: partyId ? undefined : cashMobile.trim() || undefined,
        kind: 'sale',
        priceIncludesVat: includesVat,
        invoiceDiscount: invoiceDiscountText.trim() || undefined,
        lines: toApiLines(lines),
      });
      router.push(`/sales/invoices/${invoice.id}`);
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
      setBusy(false);
    }
  }

  if (!can('sales.invoice.create')) {
    return (
      <Screen title="فاتورة مبيعات جديدة" crumbs={['المبيعات', 'العمليات']}>
        <div className="card state">
          <strong>لا تملك صلاحية إنشاء فواتير المبيعات</strong>
        </div>
      </Screen>
    );
  }

  return (
    <Screen
      title="فاتورة مبيعات جديدة"
      subtitle="تُحفظ الفاتورة كمسودة أولاً، ثم تُرحَّل من شاشة الفاتورة فتأخذ رقمها الرسمي وتتحرك بها المخازن والقيود."
      crumbs={['المبيعات', 'العمليات']}
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
              {warehouseRows.map((row) => (
                <option key={row.id} value={row.id}>
                  {arabicName(row)}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>العميل</span>
            <select className="input" value={partyId} onChange={(event) => setPartyId(event.target.value)}>
              <option value="">— عميل نقدي —</option>
              {(parties.data ?? []).map((row) => (
                <option key={row.id} value={row.id}>
                  {partyLabel(row)}
                </option>
              ))}
            </select>
          </label>
          {!partyId && (
            <>
              <label className="field">
                <span>اسم العميل النقدي *</span>
                <input className="input" value={cashName} onChange={(event) => setCashName(event.target.value)} required />
              </label>
              <label className="field">
                <span>جوال العميل</span>
                <input className="input" dir="ltr" value={cashMobile} onChange={(event) => setCashMobile(event.target.value)} />
              </label>
            </>
          )}
          <label className="field">
            <span>المندوب</span>
            <input className="input" dir="ltr" placeholder="معرّف المندوب (اختياري)" value={salesmanId} onChange={(event) => setSalesmanId(event.target.value)} />
          </label>
          <label className="field">
            <span>خصم على الفاتورة</span>
            <input className="input" dir="ltr" inputMode="decimal" value={invoiceDiscountText} onChange={(event) => setInvoiceDiscountText(event.target.value)} />
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
        <InvoiceLines lines={lines} onChange={setLines} items={items.data ?? []} taxGroups={taxGroups.data ?? []} priceField="salePrice" />

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
