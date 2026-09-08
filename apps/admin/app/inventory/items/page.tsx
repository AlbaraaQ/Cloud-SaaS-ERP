'use client';

import { useState } from 'react';

import { DataTable, Notice, QueryView } from '../../../components/data-view';
import { Screen } from '../../../components/screen';
import { ApiError, apiPost } from '../../../lib/api';
import {
  arabicName,
  listCategories,
  listItems,
  listTaxGroups,
  listUnits,
  money,
  type Category,
  type Item,
  type TaxGroup,
  type Unit,
} from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

export default function ItemsPage() {
  const { can } = useSession();
  const [search, setSearch] = useState('');
  const [applied, setApplied] = useState('');
  const items = useQuery<Item[]>(() => listItems(applied || undefined), [applied]);
  const categories = useQuery<Category[]>(() => listCategories(), []);
  const units = useQuery<Unit[]>(() => listUnits(), []);
  const taxGroups = useQuery<TaxGroup[]>(() => listTaxGroups(), []);

  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({ sku: '', nameAr: '', nameEn: '', categoryId: '', baseUnitId: '', kind: 'stock', salePrice: '', purchasePrice: '', taxGroupId: '' });
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();

  const set = (key: keyof typeof form) => (event: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
    setForm((current) => ({ ...current, [key]: event.target.value }));

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setNotice(undefined);
    try {
      await apiPost('/organization/catalog/items', {
        sku: form.sku.trim(),
        nameAr: form.nameAr.trim(),
        nameEn: form.nameEn.trim() || undefined,
        categoryId: form.categoryId,
        baseUnitId: form.baseUnitId,
        kind: form.kind,
        salePrice: form.salePrice.trim() || undefined,
        purchasePrice: form.purchasePrice.trim() || undefined,
        taxGroupId: form.taxGroupId || undefined,
      });
      setNotice({ kind: 'ok', text: `تمت إضافة المادة ${form.sku}.` });
      setForm({ sku: '', nameAr: '', nameEn: '', categoryId: form.categoryId, baseUnitId: form.baseUnitId, kind: 'stock', salePrice: '', purchasePrice: '', taxGroupId: form.taxGroupId });
      items.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  const categoryRows = categories.data ?? [];
  const unitRows = units.data ?? [];
  const missingRefs = categoryRows.length === 0 || unitRows.length === 0;

  return (
    <Screen
      title="دليل المواد"
      subtitle="بطاقات الأصناف: الرمز، المجموعة، وحدة القياس، الأسعار والضريبة."
      crumbs={['المستودعات', 'التعاريف']}
      actions={
        can('catalog.item.manage') ? (
          <button className="btn primary" type="button" onClick={() => setOpen(!open)}>
            {open ? 'إغلاق' : 'مادة جديدة'}
          </button>
        ) : null
      }
    >
      {open && (
        <form className="card" onSubmit={submit}>
          <h2>بطاقة مادة جديدة</h2>
          {missingRefs && (
            <p className="alert warn">
              يلزم وجود مجموعة واحدة ووحدة قياس واحدة على الأقل. أنشئها من «بطاقة مجموعة» و«بطاقة وحدة».
            </p>
          )}
          <div className="form-grid">
            <label className="field">
              <span>الرمز (SKU) *</span>
              <input className="input" dir="ltr" value={form.sku} onChange={set('sku')} required />
            </label>
            <label className="field">
              <span>الاسم العربي *</span>
              <input className="input" value={form.nameAr} onChange={set('nameAr')} required />
            </label>
            <label className="field">
              <span>الاسم الإنجليزي</span>
              <input className="input" dir="ltr" value={form.nameEn} onChange={set('nameEn')} />
            </label>
            <label className="field">
              <span>المجموعة *</span>
              <select className="input" value={form.categoryId} onChange={set('categoryId')} required>
                <option value="">— اختر —</option>
                {categoryRows.map((row) => (
                  <option key={row.id} value={row.id}>{`${row.code} — ${arabicName(row)}`}</option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>وحدة القياس *</span>
              <select className="input" value={form.baseUnitId} onChange={set('baseUnitId')} required>
                <option value="">— اختر —</option>
                {unitRows.map((row) => (
                  <option key={row.id} value={row.id}>{`${row.code} — ${arabicName(row)}`}</option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>النوع</span>
              <select className="input" value={form.kind} onChange={set('kind')}>
                <option value="stock">مخزنية</option>
                <option value="service">خدمة</option>
                <option value="composite">مركبة</option>
              </select>
            </label>
            <label className="field">
              <span>سعر البيع</span>
              <input className="input" dir="ltr" inputMode="decimal" value={form.salePrice} onChange={set('salePrice')} />
            </label>
            <label className="field">
              <span>سعر الشراء</span>
              <input className="input" dir="ltr" inputMode="decimal" value={form.purchasePrice} onChange={set('purchasePrice')} />
            </label>
            <label className="field">
              <span>المجموعة الضريبية</span>
              <select className="input" value={form.taxGroupId} onChange={set('taxGroupId')}>
                <option value="">— بدون —</option>
                {(taxGroups.data ?? []).map((row) => (
                  <option key={row.id} value={row.id}>{arabicName(row)}</option>
                ))}
              </select>
            </label>
          </div>
          <Notice notice={notice} />
          <button className="btn primary" type="submit" disabled={busy || missingRefs}>
            {busy ? 'جارٍ الحفظ…' : 'حفظ المادة'}
          </button>
        </form>
      )}

      <div className="card toolbar">
        <input
          className="input"
          placeholder="بحث بالاسم العربي…"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter') setApplied(search.trim());
          }}
        />
        <button className="btn" type="button" onClick={() => setApplied(search.trim())}>
          بحث
        </button>
        {applied && (
          <button
            className="btn"
            type="button"
            onClick={() => {
              setSearch('');
              setApplied('');
            }}
          >
            إلغاء الفلتر
          </button>
        )}
      </div>

      <QueryView query={items} empty="لا توجد مواد" emptyDetail="ابدأ بإضافة بطاقة مادة جديدة.">
        {(rows) => (
          <DataTable
            rows={rows}
            rowKey={(row) => row.id}
            columns={[
              { key: 'sku', header: 'الرمز', align: 'ltr', cell: (row) => row.sku },
              { key: 'name', header: 'الاسم', cell: (row) => arabicName(row) },
              { key: 'kind', header: 'النوع', cell: (row) => (row.kind === 'service' ? 'خدمة' : row.kind === 'composite' ? 'مركبة' : 'مخزنية') },
              { key: 'sale', header: 'سعر البيع', align: 'num', cell: (row) => money(row.salePrice ?? row.sale_price) },
              { key: 'purchase', header: 'سعر الشراء', align: 'num', cell: (row) => money(row.purchasePrice ?? row.purchase_price) },
            ]}
          />
        )}
      </QueryView>
    </Screen>
  );
}
