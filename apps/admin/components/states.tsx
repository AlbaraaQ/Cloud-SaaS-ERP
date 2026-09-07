export function EmptyState({ title, detail }: { title: string; detail?: string }) { return <div className="card"><h3>{title}</h3><p className="muted">{detail ?? 'No rows yet.'}</p></div>; }
export function ErrorState({ detail }: { detail: string }) { return <div className="card"><h3>تعذر التحميل</h3><p className="muted">{detail}</p></div>; }
export function Skeleton() { return <div className="card" aria-busy="true"><p className="muted">Loading…</p></div>; }
export function ForbiddenState({ detail }: { detail: string }) { return <div className="card"><h3>403</h3><p className="muted">{detail}</p></div>; }
