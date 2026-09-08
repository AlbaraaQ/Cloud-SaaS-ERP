'use client';

import { Directory } from '../../../components/directory';
import { apiList, apiPost } from '../../../lib/api';
import { accountLabel, listAccounts, postableOf, typeOf, type Account } from '../../../lib/accounts';
import { arabicName, listCostCenters, type CostCenter } from '../../../lib/lookups';
import { useQuery } from '../../../lib/use-query';

type ExpenseType = { id: string; nameAr: string; nameEn: string | null; accountId: string; costCenterId: string | null };

/**
 * Expense cards.
 *
 * Each card binds an expense name to the account it posts to, so a payment voucher can
 * pick "electricity" instead of asking the cashier to know account 5107.
 */
export default function ExpenseTypesPage() {
  const expenses = useQuery<ExpenseType[]>(() => apiList<ExpenseType>('/expense-types'), []);
  const accounts = useQuery<Account[]>(() => listAccounts(), []);
  const costCenters = useQuery<CostCenter[]>(() => listCostCenters(), []);

  const accountRows = (accounts.data ?? []).filter((account) => postableOf(account) && typeOf(account) === 'expense');

  return (
    <Directory<ExpenseType>
      title="بطاقة المصاريف"
      subtitle="عرّف كل نوع مصروف مرة واحدة واربطه بحساب المصروف ومركز التكلفة."
      crumbs={['المحاسبة', 'تعاريف']}
      query={expenses}
      createLabel="مصروف جديد"
      formTitle="بطاقة مصروف"
      empty="لا توجد أنواع مصاريف"
      emptyDetail="عرّف مصروفاتك المتكررة (إيجار، كهرباء، صيانة…) لتظهر في سندات الصرف."
      blocked={accountRows.length === 0 ? 'لا يوجد حساب مصروفات قابل للترحيل في دليل الحسابات.' : undefined}
      fields={[
        { name: 'nameAr', label: 'الاسم بالعربية', required: true },
        { name: 'nameEn', label: 'الاسم بالإنجليزية', ltr: true },
        { name: 'accountId', label: 'حساب المصروف', type: 'select', required: true, wide: true, options: accountRows.map((account) => ({ id: account.id, label: accountLabel(account) })) },
        { name: 'costCenterId', label: 'مركز التكلفة', type: 'select', options: (costCenters.data ?? []).map((row) => ({ id: row.id, label: `${row.code} — ${arabicName(row)}` })) },
      ]}
      onCreate={(values) =>
        apiPost('/expense-types', {
          nameAr: String(values.nameAr),
          nameEn: values.nameEn ? String(values.nameEn) : undefined,
          accountId: String(values.accountId),
          costCenterId: values.costCenterId ? String(values.costCenterId) : undefined,
        })
      }
      successText={(values) => `تم حفظ «${String(values.nameAr)}».`}
      columns={[
        { key: 'name', header: 'المصروف', cell: (row) => row.nameAr },
        { key: 'nameEn', header: 'Name', align: 'ltr', cell: (row) => row.nameEn ?? '—' },
        {
          key: 'account',
          header: 'حساب المصروف',
          cell: (row) => {
            const account = (accounts.data ?? []).find((candidate) => candidate.id === row.accountId);
            return account ? accountLabel(account) : '—';
          },
        },
        {
          key: 'costCenter',
          header: 'مركز التكلفة',
          cell: (row) => {
            const center = (costCenters.data ?? []).find((candidate) => candidate.id === row.costCenterId);
            return center ? arabicName(center) : '—';
          },
        },
      ]}
      rowKey={(row) => row.id}
    />
  );
}
