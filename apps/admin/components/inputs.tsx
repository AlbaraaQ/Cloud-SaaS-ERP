'use client';

import { Decimal } from 'decimal.js';

export function MoneyInput({ value, onChange }: { value: string; onChange?: (valueText: string) => void }) {
  return <input className="input" inputMode="decimal" value={value} onChange={(event) => onChange?.(normalise(event.target.value, 2))} aria-label="money" />;
}
export function QtyInput({ value, onChange }: { value: string; onChange?: (valueText: string) => void }) {
  return <input className="input" inputMode="decimal" value={value} onChange={(event) => onChange?.(normalise(event.target.value, 4))} aria-label="quantity" />;
}
function normalise(raw: string, scale: number): string { try { return new Decimal(raw || '0').toFixed(scale); } catch { return raw; } }
