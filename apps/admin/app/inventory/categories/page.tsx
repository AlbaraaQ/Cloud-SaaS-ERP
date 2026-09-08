'use client';

import { Directory } from '../../../components/directory';
import { apiPost } from '../../../lib/api';
import { arabicName, listCategories, type Category } from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

export default function CategoriesPage() {
  const { can } = useSession();
  const categories = useQuery<Category[]>(() => listCategories(), []);

  return (
    <Directory<Category>
      title="بطاقة مجموعة"
      subtitle="مجموعات الأصناف — تُستخدم في التقارير وفي عرض المواد داخل نقطة البيع."
      crumbs={['المستودعات', 'التعاريف']}
      query={categories}
      canCreate={can('catalog.category.manage')}
      createLabel="مجموعة جديدة"
      fields={[
        { name: 'code', label: 'الرمز', required: true, ltr: true },
        { name: 'nameAr', label: 'الاسم العربي', required: true },
        { name: 'nameEn', label: 'الاسم الإنجليزي', ltr: true },
        {
          name: 'parentId',
          label: 'المجموعة الأب',
          type: 'select',
          options: (categories.data ?? []).map((row) => ({ id: row.id, label: `${row.code} — ${arabicName(row)}` })),
        },
      ]}
      onCreate={(values) =>
        apiPost('/organization/catalog/categories', {
          code: String(values.code).trim(),
          nameAr: String(values.nameAr).trim(),
          nameEn: String(values.nameEn).trim() || undefined,
          parentId: String(values.parentId) || undefined,
        })
      }
      successText={(values) => `تمت إضافة المجموعة ${String(values.code)}.`}
      rowKey={(row) => row.id}
      empty="لا توجد مجموعات"
      columns={[
        { key: 'code', header: 'الرمز', align: 'ltr', cell: (row) => row.code },
        { key: 'name', header: 'الاسم', cell: (row) => arabicName(row) },
      ]}
    />
  );
}
