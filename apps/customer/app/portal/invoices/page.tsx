'use client';

import { useEffect, useState } from 'react';

import { portalFetch } from '../../../lib/api';
import { PortalShell } from '../../../components/portal-shell';
import { SimpleTable, type Row } from '../../../components/table';

type Invoice = { id: string; invoiceNumber?: string; number?: string; issuedAt?: string; date?: string; total?: string | number; totalAmount?: string | number; status: string };

function accessToken() {
  const value = globalThis.document.cookie.split('; ').find((part) => part.startsWith('erp_access_token='));
  return value ? decodeURIComponent(value.split('=')[1] ?? '') : undefined;
}

export default function InvoicesPage() {
  const [rows, setRows] = useState<Row[]>([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    portalFetch<{ data: Invoice[] }>('/sales-invoices', accessToken())
      .then((result) => setRows(result.data.map((invoice) => ({
        id: invoice.id,
        number: invoice.invoiceNumber ?? invoice.number ?? invoice.id,
        date: invoice.issuedAt ?? invoice.date ?? '-',
        total: String(invoice.totalAmount ?? invoice.total ?? '-'),
        status: invoice.status,
      }))))
      .catch(() => setError('تعذر تحميل الفواتير من النظام'))
      .finally(() => setLoading(false));
  }, []);

  return <PortalShell><h1>فواتيري</h1><div className="toolbar"><input className="input" placeholder="بحث" aria-label="بحث في الفواتير" /></div>{loading ? <p className="muted">جارٍ تحميل الفواتير...</p> : error ? <p role="alert" className="muted">{error}</p> : rows.length ? <SimpleTable rows={rows} columns={['number', 'date', 'total', 'status']} /> : <p className="muted">لا توجد فواتير حقيقية لهذا الحساب.</p>}</PortalShell>;
}
