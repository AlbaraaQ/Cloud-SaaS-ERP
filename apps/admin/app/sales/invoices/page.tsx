'use client';

import Link from 'next/link';
import { useState } from 'react';

import { DataTable, QueryView } from '../../../components/data-view';
import { Screen } from '../../../components/screen';
import { apiList } from '../../../lib/api';
import { listParties, money, partyLabel, shortDate, statusLabel, type Party } from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type Invoice = {
  id: string;
  number: string | null;
  kind: string;
  status: string;
  partyId: string | null;
  cashCustomerName: string | null;
  currency: string;
  subtotal: string;
  taxTotal: string;
  total: string;
  paidTotal: string;
  paymentStatus: string;
  createdAt: string;
};

const KIND_LABELS: Record<string, string> = {
  sale: 'مبيعات',
  sale_return: 'مردود مبيعات',
  credit_note: 'إشعار دائن',
  debit_note: 'إشعار مدين',
};

export default function SalesInvoicesPage() {
  const { can } = useSession();
  const [kind, setKind] = useState('');
  const invoices = useQuery<Invoice[]>(() => apiList<Invoice>('/sales/invoices'), []);
  const parties = useQuery<Party[]>(() => listParties('customer'), []);

  const rows = (invoices.data ?? []).filter((row) => !kind || row.kind === kind);
  const customerOf = (row: Invoice) => {
    if (row.cashCustomerName) return `${row.cashCustomerName} (نقدي)`;
    const party = (parties.data ?? []).find((entry) => entry.id === row.partyId);
    return party ? partyLabel(party) : '—';
  };

  return (
    <Screen
      title="فواتير المبيعات"
      subtitle="كل الفواتير: المسودات القابلة للتعديل والفواتير المرحّلة بأرقامها الرسمية."
      crumbs={['المبيعات', 'العمليات']}
      actions={
        can('sales.invoice.create') ? (
          <Link className="btn primary" href="/sales/invoices/new">
            فاتورة جديدة
          </Link>
        ) : null
      }
    >
      <div className="card toolbar">
        <label className="field">
          <span>النوع</span>
          <select className="input" value={kind} onChange={(event) => setKind(event.target.value)}>
            <option value="">الكل</option>
            {Object.entries(KIND_LABELS).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </label>
      </div>

      <QueryView query={invoices} isEmpty={() => rows.length === 0} empty="لا توجد فواتير" emptyDetail="ابدأ بإصدار فاتورة مبيعات جديدة.">
        {() => (
          <DataTable
            rows={rows}
            rowKey={(row) => row.id}
            columns={[
              {
                key: 'number',
                header: 'الرقم',
                align: 'ltr',
                cell: (row) => <Link href={`/sales/invoices/${row.id}`}>{row.number ?? 'مسودة'}</Link>,
              },
              { key: 'kind', header: 'النوع', cell: (row) => KIND_LABELS[row.kind] ?? row.kind },
              { key: 'customer', header: 'العميل', cell: (row) => customerOf(row) },
              { key: 'date', header: 'التاريخ', align: 'ltr', cell: (row) => shortDate(row.createdAt) },
              { key: 'net', header: 'قبل الضريبة', align: 'num', cell: (row) => money(row.subtotal) },
              { key: 'tax', header: 'الضريبة', align: 'num', cell: (row) => money(row.taxTotal) },
              { key: 'total', header: 'الإجمالي', align: 'num', cell: (row) => money(row.total) },
              { key: 'paid', header: 'المدفوع', align: 'num', cell: (row) => money(row.paidTotal) },
              { key: 'status', header: 'الحالة', cell: (row) => <span className="badge">{statusLabel(row.status)}</span> },
              {
                key: 'print',
                header: '',
                cell: (row) => (
                  <Link className="btn sm" href={`/print/sales-invoice/${row.id}`}>
                    طباعة
                  </Link>
                ),
              },
            ]}
          />
        )}
      </QueryView>
    </Screen>
  );
}
