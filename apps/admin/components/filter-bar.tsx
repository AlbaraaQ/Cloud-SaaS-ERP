'use client';

export function FilterBar({ onSearch }: { onSearch?: (value: string) => void }) {
  return <div className="toolbar"><input className="input" aria-label="search" placeholder="بحث / Search" onChange={(event) => onSearch?.(event.currentTarget.value)} /><select className="input" aria-label="status"><option>all</option><option>draft</option><option>posted</option><option>voided</option></select><button className="btn" type="button">تطبيق</button></div>;
}
