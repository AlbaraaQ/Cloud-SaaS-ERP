'use client';

import { Directory } from '../../../components/directory';
import { apiData, apiList, apiPost } from '../../../lib/api';
import { listParties, money, partyLabel, shortDate, statusLabel, today, type Party } from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type Marina = { groups: Array<{ id: string; name: string }>; vessels: Array<{ id: string; code: string; name: string }> };
type Violation = {
  id: string;
  vesselId: string | null;
  partyId: string | null;
  violationDate: string;
  amount: string;
  description: string;
  status: string;
};

export default function MarinaViolationsPage() {
  const { can } = useSession();
  const violations = useQuery<Violation[]>(() => apiList<Violation>('/marina/violations'), []);
  const marina = useQuery<Marina>(() => apiData<Marina>('/marina'), []);
  const parties = useQuery<Party[]>(() => listParties(), []);
  const vessels = marina.data?.vessels ?? [];

  return (
    <Directory<Violation>
      title="المخالفات"
      subtitle="مخالفات المراكب والملّاك مع قيمتها، وهي أساس المطالبة المالية لاحقاً."
      crumbs={['إدارة المراسي', 'العمليات']}
      query={violations}
      canCreate={can('marina.manage')}
      createLabel="مخالفة جديدة"
      fields={[
        { name: 'vesselId', label: 'المركب', type: 'select', options: vessels.map((row) => ({ id: row.id, label: `${row.code} — ${row.name}` })) },
        { name: 'partyId', label: 'الطرف', type: 'select', options: (parties.data ?? []).map((row) => ({ id: row.id, label: partyLabel(row) })) },
        { name: 'violationDate', label: 'التاريخ', type: 'date', required: true },
        { name: 'amount', label: 'قيمة المخالفة', type: 'number' },
        { name: 'description', label: 'الوصف', required: true, wide: true },
      ]}
      initial={{ violationDate: today() }}
      onCreate={(values) =>
        apiPost('/marina/violations', {
          vesselId: String(values.vesselId) || undefined,
          partyId: String(values.partyId) || undefined,
          violationDate: String(values.violationDate),
          amount: String(values.amount).trim() || undefined,
          description: String(values.description).trim(),
        })
      }
      successText={() => 'تم تسجيل المخالفة.'}
      rowKey={(row) => row.id}
      empty="لا توجد مخالفات"
      columns={[
        { key: 'date', header: 'التاريخ', align: 'ltr', cell: (row) => shortDate(row.violationDate) },
        { key: 'vessel', header: 'المركب', cell: (row) => vessels.find((entry) => entry.id === row.vesselId)?.name ?? '—' },
        {
          key: 'party',
          header: 'الطرف',
          cell: (row) => {
            const party = (parties.data ?? []).find((entry) => entry.id === row.partyId);
            return party ? partyLabel(party) : '—';
          },
        },
        { key: 'description', header: 'الوصف', cell: (row) => row.description },
        { key: 'amount', header: 'القيمة', align: 'num', cell: (row) => money(row.amount) },
        { key: 'status', header: 'الحالة', cell: (row) => <span className="badge">{statusLabel(row.status)}</span> },
      ]}
    />
  );
}
