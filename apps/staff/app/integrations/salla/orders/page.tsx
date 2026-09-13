'use client';

import Link from 'next/link';

import { DataTable, QueryView } from '../../../../components/data-view';
import { Screen } from '../../../../components/screen';
import { apiData } from '../../../../lib/api';
import { dateTime, money, statusLabel } from '../../../../lib/lookups';
import { useQuery } from '../../../../lib/use-query';

type Order = {
  id: string;
  number: string | null;
  status: string;
  customer: string | null;
  mobile: string | null;
  total: string;
  paymentStatus: string;
  createdAt: string;
};

/**
 * Orders that arrived from Salla.
 *
 * Each webhook creates a normal sales invoice tagged `salla`, so an order is managed
 * exactly like any other invoice — this screen is the entry point into that flow.
 */
export default function SallaOrdersPage() {
  const orders = useQuery<Order[]>(() => apiData<Order[]>('/integrations/salla/orders'), []);

  return (
    <Screen
      title="إدارة طلبات سلة"
      subtitle="كل طلب وارد من المتجر يصبح فاتورة مبيعات في النظام — رحّلها واقبض قيمتها من شاشة الفواتير."
      crumbs={['المستودعات', 'متجر سلة']}
    >
      <QueryView
        query={orders}
        empty="لا توجد طلبات واردة"
        emptyDetail="سجّل عنوان الويب هوك في لوحة سلة ليصل أول طلب إلى هنا."
      >
        {(rows) => (
          <div className="card">
            <DataTable
              columns={[
                { key: 'number', header: 'رقم الفاتورة', align: 'ltr', cell: (row: Order) => row.number ?? 'مسودة' },
                { key: 'created', header: 'وقت الطلب', cell: (row: Order) => dateTime(row.createdAt) },
                { key: 'customer', header: 'العميل', cell: (row: Order) => row.customer ?? '—' },
                { key: 'mobile', header: 'الجوال', align: 'ltr', cell: (row: Order) => row.mobile ?? '—' },
                { key: 'total', header: 'الإجمالي', align: 'num', cell: (row: Order) => money(row.total) },
                { key: 'status', header: 'حالة الفاتورة', cell: (row: Order) => statusLabel(row.status) },
                { key: 'payment', header: 'السداد', cell: (row: Order) => statusLabel(row.paymentStatus) },
                { key: 'open', header: '', cell: (row: Order) => <Link className="btn sm" href={`/sales/invoices/${row.id}`}>فتح الفاتورة</Link> },
              ]}
              rows={rows}
              rowKey={(row) => row.id}
            />
          </div>
        )}
      </QueryView>
    </Screen>
  );
}
