'use client';

import { useState } from 'react';

import { DataTable } from './data-table';

const reportKeys = ['sales-by-day','sales-by-category','sales-by-item','inventory-valuation','trial-balance','general-ledger','vat-return','cashier-shift'];

export function ReportRunner() {
  const [key, setKey] = useState(reportKeys[0] ?? 'sales-by-day');
  const rows = [{ id: key, key, status: 'ready', note: 'Run against /reports/{key} when API token is connected.' }];
  return <div className="grid"><div className="toolbar"><select className="input" value={key} onChange={(event) => setKey(event.target.value)}>{reportKeys.map((entry) => <option key={entry}>{entry}</option>)}</select><button className="btn primary" type="button">تشغيل التقرير</button><button className="btn" type="button">CSV/XLSX/PDF</button></div><DataTable id="report-runner" rows={rows} columns={[{ key: 'key', title: 'Report' }, { key: 'status', title: 'Status' }, { key: 'note', title: 'Note' }]} /></div>;
}
