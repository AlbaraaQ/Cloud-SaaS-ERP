'use client';

import { calculateInvoiceTotals, type InvoiceTotals } from '@erp/contracts';

import { itemLabel, money, type Item, type TaxGroup } from '../lib/lookups';

/**
 * The line grid shared by the sales invoice, the purchase invoice and the POS ticket.
 *
 * Totals are computed with `calculateInvoiceTotals` from `@erp/contracts` — the very
 * function the API uses when it stores the invoice — so what the user reads before saving
 * is what the server persists, down to the last of the four decimals.
 */
export type LineDraft = {
  itemId: string;
  description: string;
  quantityText: string;
  unitPriceText: string;
  discountRateText: string;
  taxRateText: string;
  taxGroupId: string;
};

export function emptyLine(taxRateText = '15'): LineDraft {
  return { itemId: '', description: '', quantityText: '1', unitPriceText: '', discountRateText: '', taxRateText, taxGroupId: '' };
}

const numeric = (value: string) => (value.trim() === '' || Number.isNaN(Number(value)) ? '0' : value.trim());

export function filledLines(lines: LineDraft[]): LineDraft[] {
  return lines.filter((line) => (line.itemId || line.description.trim()) && Number(line.quantityText) > 0);
}

export function computeTotals(lines: LineDraft[], options: { priceIncludesVat?: boolean; invoiceDiscount?: string } = {}): InvoiceTotals {
  return calculateInvoiceTotals({
    lines: filledLines(lines).map((line) => ({
      quantity: numeric(line.quantityText),
      unitPrice: numeric(line.unitPriceText),
      discountRate: numeric(line.discountRateText),
      discountAmount: '0',
      taxRate: numeric(line.taxRateText),
    })),
    priceIncludesVat: options.priceIncludesVat,
    invoiceDiscount: options.invoiceDiscount ? numeric(options.invoiceDiscount) : '0',
  });
}

/** Maps the draft grid onto the `lines` payload both invoice services accept. */
export function toApiLines(lines: LineDraft[]) {
  return filledLines(lines).map((line) => ({
    itemId: line.itemId || undefined,
    description: line.description.trim() || undefined,
    quantity: numeric(line.quantityText),
    unitPrice: numeric(line.unitPriceText),
    discountRate: numeric(line.discountRateText),
    taxRate: numeric(line.taxRateText),
    taxGroupId: line.taxGroupId || undefined,
  }));
}

export function InvoiceLines({
  lines,
  onChange,
  items,
  taxGroups,
  priceField = 'salePrice',
}: {
  lines: LineDraft[];
  onChange: (next: LineDraft[]) => void;
  items: Item[];
  taxGroups: TaxGroup[];
  /** Which catalogue price pre-fills a new line. */
  priceField?: 'salePrice' | 'purchasePrice';
}) {
  const totals = computeTotals(lines);

  function update(index: number, patch: Partial<LineDraft>) {
    onChange(lines.map((line, position) => (position === index ? { ...line, ...patch } : line)));
  }

  function pickItem(index: number, itemId: string) {
    const item = items.find((row) => row.id === itemId);
    const catalogue = item ? ((priceField === 'salePrice' ? item.salePrice ?? item.sale_price : item.purchasePrice ?? item.purchase_price) ?? '') : '';
    const group = item?.taxGroupId ? taxGroups.find((row) => row.id === item.taxGroupId) : undefined;
    update(index, {
      itemId,
      unitPriceText: lines[index]?.unitPriceText || String(catalogue ?? ''),
      taxGroupId: group?.id ?? lines[index]?.taxGroupId ?? '',
      taxRateText: group ? String(Number(group.rate) * 100) : (lines[index]?.taxRateText ?? '15'),
    });
  }

  return (
    <>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th style={{ minWidth: '14rem' }}>المادة / الوصف</th>
              <th>الكمية</th>
              <th>السعر</th>
              <th>خصم %</th>
              <th>ضريبة %</th>
              <th>الصافي</th>
              <th>الإجمالي</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {lines.map((line, index) => {
              const computed = totals.lines[filledLines(lines).indexOf(line)];
              return (
                <tr key={index}>
                  <td>
                    <select className="input" value={line.itemId} onChange={(event) => pickItem(index, event.target.value)}>
                      <option value="">— بند حر —</option>
                      {items.map((row) => (
                        <option key={row.id} value={row.id}>
                          {itemLabel(row)}
                        </option>
                      ))}
                    </select>
                    {!line.itemId && (
                      <input
                        className="input"
                        placeholder="وصف البند"
                        value={line.description}
                        onChange={(event) => update(index, { description: event.target.value })}
                      />
                    )}
                  </td>
                  <td>
                    <input className="input" dir="ltr" inputMode="decimal" value={line.quantityText} onChange={(event) => update(index, { quantityText: event.target.value })} />
                  </td>
                  <td>
                    <input className="input" dir="ltr" inputMode="decimal" value={line.unitPriceText} onChange={(event) => update(index, { unitPriceText: event.target.value })} />
                  </td>
                  <td>
                    <input className="input" dir="ltr" inputMode="decimal" value={line.discountRateText} onChange={(event) => update(index, { discountRateText: event.target.value })} />
                  </td>
                  <td>
                    <input className="input" dir="ltr" inputMode="decimal" value={line.taxRateText} onChange={(event) => update(index, { taxRateText: event.target.value })} />
                  </td>
                  <td className="num">{computed ? money(computed.net) : '—'}</td>
                  <td className="num">{computed ? money(computed.total) : '—'}</td>
                  <td>
                    <button className="btn sm" type="button" onClick={() => onChange(lines.filter((_, position) => position !== index))}>
                      حذف
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      <button className="btn sm" type="button" onClick={() => onChange([...lines, emptyLine()])}>
        + سطر
      </button>
    </>
  );
}

export function TotalsPanel({ totals, currency = 'SAR' }: { totals: InvoiceTotals; currency?: string }) {
  return (
    <dl className="kv">
      <dt>الإجمالي قبل الضريبة</dt>
      <dd>{money(totals.subtotal, currency)}</dd>
      <dt>الخصم</dt>
      <dd>{money(totals.discount, currency)}</dd>
      <dt>الوعاء الخاضع</dt>
      <dd>{money(totals.taxable, currency)}</dd>
      <dt>ضريبة القيمة المضافة</dt>
      <dd>{money(totals.tax, currency)}</dd>
      <dt>الإجمالي المستحق</dt>
      <dd>
        <strong>{money(totals.total, currency)}</strong>
      </dd>
    </dl>
  );
}
