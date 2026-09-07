import { z } from 'zod';

/* eslint-disable no-restricted-syntax */

const decimalString = z.string().regex(/^-?\d+(?:\.\d+)?$/);

export const invoiceLineInputSchema = z.object({
  quantity: decimalString,
  unitPrice: decimalString,
  discountRate: decimalString.default('0'),
  discountAmount: decimalString.default('0'),
  taxRate: decimalString.default('0'),
});

export type InvoiceLineInput = z.input<typeof invoiceLineInputSchema>;
export type InvoiceLineTotal = {
  gross: string;
  discount: string;
  net: string;
  tax: string;
  total: string;
};

export type InvoiceTotalsInput = {
  lines: InvoiceLineInput[];
  priceIncludesVat?: boolean;
  invoiceDiscount?: string;
  extraTax?: string;
  withholding?: string;
  scale?: number;
};

export type InvoiceTotals = {
  lines: InvoiceLineTotal[];
  subtotal: string;
  discount: string;
  taxable: string;
  tax: string;
  extraTax: string;
  withholding: string;
  total: string;
};

type Int = bigint;
const ten = (scale: number): Int => 10n ** BigInt(scale);

function parse(value: string, scale: number): Int {
  const normalized = value.trim();
  const [wholePart = '0', fraction = ''] = normalized.replace(/^\+/, '').split('.');
  const negative = wholePart.startsWith('-');
  const wholeValue = BigInt(negative ? wholePart.slice(1) || '0' : wholePart || '0');
  const kept = fraction.padEnd(scale, '0').slice(0, scale);
  const base = wholeValue * ten(scale) + BigInt(kept || '0');
  const rounded = fraction.length > scale && Number(fraction[scale]) >= 5 ? base + 1n : base;
  return negative ? -rounded : rounded;
}

function format(value: Int, scale: number): string {
  const negative = value < 0n;
  const absolute = negative ? -value : value;
  const raw = absolute.toString().padStart(scale + 1, '0');
  const fraction = raw.slice(-scale).replace(/0+$/, '');
  return `${negative ? '-' : ''}${raw.slice(0, -scale)}${fraction ? `.${fraction}` : ''}`;
}

function divRound(numerator: Int, denominator: Int): Int {
  if (denominator === 0n) throw new Error('Cannot divide by zero');
  const sign = numerator < 0n === denominator < 0n ? 1n : -1n;
  const n = numerator < 0n ? -numerator : numerator;
  const d = denominator < 0n ? -denominator : denominator;
  return sign * ((n + d / 2n) / d);
}

export function calculateInvoiceTotals(input: InvoiceTotalsInput): InvoiceTotals {
  const scale = input.scale ?? 4;
  const unit = ten(scale);
  const lines = input.lines.map((line) => {
    const quantity = parse(line.quantity, scale);
    const unitPrice = parse(line.unitPrice, scale);
    const gross = divRound(quantity * unitPrice, unit);
    const rate = parse(line.discountRate ?? '0', scale);
    const rateDiscount = divRound(gross * rate, 100n * unit);
    const discount = rateDiscount + parse(line.discountAmount ?? '0', scale);
    const net = gross - discount;
    const taxRate = parse(line.taxRate ?? '0', scale);
    const tax = input.priceIncludesVat
      ? divRound(net * taxRate, 100n * unit + taxRate)
      : divRound(net * taxRate, 100n * unit);
    const taxableNet = input.priceIncludesVat ? net - tax : net;
    const total = input.priceIncludesVat ? net : net + tax;
    return { gross: format(gross, scale), discount: format(discount, scale), net: format(taxableNet, scale), tax: format(tax, scale), total: format(total, scale) };
  });
  const subtotal = lines.reduce((sum, line) => sum + parse(line.net, scale), 0n);
  const discount = parse(input.invoiceDiscount ?? '0', scale);
  const taxable = subtotal - discount;
  const tax = lines.reduce((sum, line) => sum + parse(line.tax, scale), 0n);
  const extraTax = parse(input.extraTax ?? '0', scale);
  const withholding = parse(input.withholding ?? '0', scale);
  const total = taxable + tax + extraTax - withholding;
  return { lines, subtotal: format(subtotal, scale), discount: format(discount, scale), taxable: format(taxable, scale), tax: format(tax, scale), extraTax: format(extraTax, scale), withholding: format(withholding, scale), total: format(total, scale) };
}

export type LandedCostLineInput = { lineId?: string; itemId?: string; quantity: string; net: string; unitCost?: string };
export type LandedCostInput = { lines: LandedCostLineInput[]; costs: Array<{ amount: string }>; method: 'qty' | 'value'; scale?: number };
export type LandedCostLineAllocation = {
  lineId?: string;
  itemId?: string;
  base: string;
  allocatedCost: string;
  net: string;
  landedTotal: string;
  effectiveUnitCost: string;
};
export type LandedCostAllocation = { method: 'qty' | 'value'; totalCost: string; lines: LandedCostLineAllocation[] };

/**
 * Allocates landed costs pro-rata by quantity or value, HALF_UP at the selected scale,
 * then assigns any rounding remainder to the line with the largest allocation base.
 */
export function allocateLandedCost(input: LandedCostInput): LandedCostAllocation {
  const scale = input.scale ?? 4;
  const totalCost = input.costs.reduce((sum, cost) => sum + parse(cost.amount, scale), 0n);
  const prepared = input.lines.map((line, index) => {
    const quantity = parse(line.quantity, scale);
    const net = parse(line.net, scale);
    const base = input.method === 'qty' ? quantity : net;
    return { line, index, quantity, net, base };
  });
  const totalBase = prepared.reduce((sum, line) => sum + line.base, 0n);
  if (prepared.length === 0 || totalCost < 0n || totalBase <= 0n) {
    throw new Error('Cannot allocate landed cost without positive lines, costs and allocation base');
  }

  const allocations = prepared.map((line) => divRound(totalCost * line.base, totalBase));
  const allocated = allocations.reduce((sum, value) => sum + value, 0n);
  const remainder = totalCost - allocated;
  if (remainder !== 0n) {
    let target = 0;
    for (let index = 1; index < prepared.length; index += 1) {
      if (prepared[index]!.base > prepared[target]!.base) target = index;
    }
    allocations[target] = (allocations[target] ?? 0n) + remainder;
  }

  return {
    method: input.method,
    totalCost: format(totalCost, scale),
    lines: prepared.map(({ line, quantity, net, base }, index) => {
      const allocatedCost = allocations[index] ?? 0n;
      const landedTotal = net + allocatedCost;
      const effectiveUnitCost = divRound(landedTotal * ten(scale), quantity);
      return {
        lineId: line.lineId,
        itemId: line.itemId,
        base: format(base, scale),
        allocatedCost: format(allocatedCost, scale),
        net: format(net, scale),
        landedTotal: format(landedTotal, scale),
        effectiveUnitCost: format(effectiveUnitCost, scale),
      };
    }),
  };
}

export const invoiceMath = { calculateInvoiceTotals, allocateLandedCost, invoiceLineInputSchema };
