'use client';

import { useState } from 'react';

import { Directory } from '../../../components/directory';
import { ApiError, apiList, apiPost } from '../../../lib/api';
import { money, shortDate, statusLabel, today } from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type Employee = { id: string; employeeNo: string; name: string };
type Adjustment = {
  id: string;
  employeeId: string;
  kind: 'addition' | 'deduction';
  componentCode: string;
  valueText: string;
  startsOn: string;
  endsOn: string | null;
  recurring: boolean;
  status: string;
  reason: string | null;
};

export default function AdjustmentsPage() {
  const { can } = useSession();
  const adjustments = useQuery<Adjustment[]>(() => apiList<Adjustment>('/hrm/adjustments'), []);
  const employees = useQuery<Employee[]>(() => apiList<Employee>('/hrm/employees'), []);
  const [error, setError] = useState('');

  async function approve(id: string) {
    setError('');
    try {
      await apiPost(`/hrm/adjustments/${id}/approve`, {});
      adjustments.reload();
    } catch (problem) {
      setError(problem instanceof ApiError ? problem.message : String(problem));
    }
  }

  return (
    <Directory<Adjustment>
      title="الحوافز والجزاءات"
      subtitle="إضافات وخصومات على الراتب؛ لا تدخل في الاستحقاق إلا بعد الاعتماد."
      crumbs={['الموظفين والرواتب', 'العمليات']}
      query={adjustments}
      canCreate={can('hrm.manage')}
      createLabel="حركة جديدة"
      blocked={(employees.data ?? []).length === 0 ? 'أضف موظفاً واحداً على الأقل أولاً.' : undefined}
      fields={[
        {
          name: 'employeeId',
          label: 'الموظف',
          type: 'select',
          required: true,
          options: (employees.data ?? []).map((row) => ({ id: row.id, label: `${row.employeeNo} — ${row.name}` })),
        },
        {
          name: 'kind',
          label: 'النوع',
          type: 'select',
          required: true,
          options: [
            { id: 'addition', label: 'حافز / إضافة' },
            { id: 'deduction', label: 'جزاء / خصم' },
          ],
        },
        { name: 'componentCode', label: 'البند', required: true, ltr: true, placeholder: 'bonus / penalty / advance' },
        { name: 'valueText', label: 'المبلغ', type: 'number', required: true },
        { name: 'startsOn', label: 'يبدأ في', type: 'date', required: true },
        { name: 'endsOn', label: 'ينتهي في', type: 'date' },
        { name: 'recurring', label: 'متكرر شهرياً', type: 'checkbox' },
        { name: 'reason', label: 'السبب', wide: true },
      ]}
      initial={{ startsOn: today(), kind: 'addition' }}
      onCreate={(values) =>
        apiPost('/hrm/adjustments', {
          employeeId: String(values.employeeId),
          kind: String(values.kind),
          componentCode: String(values.componentCode).trim(),
          valueText: String(values.valueText).trim(),
          startsOn: String(values.startsOn),
          endsOn: String(values.endsOn) || undefined,
          recurring: Boolean(values.recurring),
          reason: String(values.reason).trim() || undefined,
        })
      }
      successText={() => 'تم تسجيل الحركة كمسودة — اعتمدها لتدخل في الاستحقاق.'}
      rowKey={(row) => row.id}
      empty="لا توجد حوافز أو جزاءات"
      toolbar={error ? <span className="alert danger">{error}</span> : undefined}
      columns={[
        {
          key: 'employee',
          header: 'الموظف',
          cell: (row) => (employees.data ?? []).find((entry) => entry.id === row.employeeId)?.name ?? row.employeeId,
        },
        { key: 'kind', header: 'النوع', cell: (row) => (row.kind === 'addition' ? 'حافز' : 'جزاء') },
        { key: 'component', header: 'البند', align: 'ltr', cell: (row) => row.componentCode },
        { key: 'value', header: 'المبلغ', align: 'num', cell: (row) => money(row.valueText) },
        { key: 'from', header: 'من', align: 'ltr', cell: (row) => shortDate(row.startsOn) },
        { key: 'to', header: 'إلى', align: 'ltr', cell: (row) => shortDate(row.endsOn) },
        { key: 'recurring', header: 'متكرر', cell: (row) => (row.recurring ? 'نعم' : '—') },
        { key: 'status', header: 'الحالة', cell: (row) => <span className="badge">{statusLabel(row.status)}</span> },
        {
          key: 'actions',
          header: '',
          cell: (row) =>
            row.status === 'draft' && can('hrm.adjust.approve') ? (
              <button className="btn sm primary" type="button" onClick={() => approve(row.id)}>
                اعتماد
              </button>
            ) : null,
        },
      ]}
    />
  );
}
