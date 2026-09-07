'use client';

import { useState } from 'react';

export function ConfirmDialog({ action, diff }: { action: string; diff: Record<string, string> }) {
  const [open, setOpen] = useState(false);
  return <><button className="btn danger" onClick={() => setOpen(true)} type="button">{action}</button>{open ? <div className="card" role="dialog" aria-modal="true" aria-label={action}><h3>{action}</h3><pre>{JSON.stringify(diff, null, 2)}</pre><button className="btn primary" onClick={() => setOpen(false)} type="button">Confirm</button><button className="btn" onClick={() => setOpen(false)} type="button">Cancel</button></div> : null}</>;
}
