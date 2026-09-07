export function KpiCard({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return <article className="card"><p className="muted">{label}</p><div className="kpi">{value}</div>{hint ? <small className="muted">{hint}</small> : null}</article>;
}
