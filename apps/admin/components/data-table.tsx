'use client';

import type { ReactNode } from 'react';
import { useMemo, useState } from 'react';

import { readColumns, persistColumns } from '../lib/format';

export type Column<T> = { key: string; title: string; render?: (row: T) => ReactNode };

export function DataTable<T extends Record<string, unknown>>({ id, rows, columns }: { id: string; rows: T[]; columns: Column<T>[] }) {
  const [visible, setVisible] = useState(() => readColumns(id, columns.map((column) => column.key)));
  const visibleColumns = useMemo(() => columns.filter((column) => visible.includes(column.key)), [columns, visible]);
  function toggle(key: string) {
    const next = visible.includes(key) ? visible.filter((entry) => entry !== key) : [...visible, key];
    setVisible(next);
    persistColumns(id, next);
  }
  return <div className="card"><div className="toolbar" aria-label="column chooser">{columns.map((column) => <button className="btn" key={column.key} onClick={() => toggle(column.key)} type="button">{visible.includes(column.key) ? '✓ ' : ''}{column.title}</button>)}</div><div style={{ overflowX: 'auto' }}><table><thead><tr>{visibleColumns.map((column) => <th key={column.key}>{column.title}</th>)}</tr></thead><tbody>{rows.map((row, index) => <tr key={String(row.id ?? index)}>{visibleColumns.map((column) => <td key={column.key}>{column.render ? column.render(row) : String(row[column.key] ?? '')}</td>)}</tr>)}</tbody></table></div></div>;
}
