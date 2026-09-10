'use client';

import { useState } from 'react';

import { DataTable, Notice, QueryView } from '../../../components/data-view';
import { Screen } from '../../../components/screen';
import { ApiError, apiDelete, apiPatch, apiPost } from '../../../lib/api';
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
  /**
   * حد الطلب / حد الطلب الأقصى وتتبع الدفعات والأرقام التسلسلية: the fields that turn a
   * catalogue card into something the stock engine can act on — a reorder report needs
   * `minQty`, and a lot-controlled item cannot be received without naming its lot.
   */
  const blank = {
    sku: '',
    barcode: '',
    nameAr: '',
    nameEn: '',
    categoryId: '',
    baseUnitId: '',
    kind: 'stock',
    salePrice: '',
    purchasePrice: '',
    taxGroupId: '',
    minQty: '',
    maxQty: '',
    trackLot: false,
    trackSerial: false,
  };
  const [form, setForm] = useState(blank);
  const [editing, setEditing] = useState<Item | undefined>();
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();

  const set = (key: keyof typeof form) => (event: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
    setForm((current) => ({ ...current, [key]: event.target.value }));

  function startEdit(row: Item) {
    setEditing(row);
    setForm({
      sku: row.sku,
      barcode: row.barcode ?? '',
      nameAr: row.nameAr ?? row.name_ar ?? '',
      nameEn: row.nameEn ?? '',
      categoryId: row.categoryId ?? row.category_id ?? '',
      baseUnitId: row.baseUnitId ?? row.base_unit_id ?? '',
      kind: row.kind ?? 'stock',
      salePrice: row.salePrice ?? row.sale_price ?? '',
      purchasePrice: row.purchasePrice ?? row.purchase_price ?? '',
      taxGroupId: row.taxGroupId ?? row.tax_group_id ?? '',
      minQty: row.minQty ?? row.min_qty ?? '',
      maxQty: row.maxQty ?? row.max_qty ?? '',
      trackLot: Boolean(row.trackLot ?? row.track_lot),
      trackSerial: Boolean(row.trackSerial ?? row.track_serial),
    });
    setNotice(undefined);
    setOpen(true);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  function cancelEdit() {
    setEditing(undefined);
    setForm(blank);
  }

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setNotice(undefined);
    try {
      if (editing) {
        // The base unit is intentionally absent from the patch: every stored quantity and
        // moving-average cost of this item is expressed in it, so it stays as issued.
        await apiPatch(`/organization/catalog/items/${editing.id}`, {
          sku: form.sku.trim(),
          barcode: form.barcode.trim() || null,
          nameAr: form.nameAr.trim(),
          nameEn: form.nameEn.trim() || null,
          categoryId: form.categoryId,
          kind: form.kind,
          salePrice: form.salePrice.trim() || undefined,
          purchasePrice: form.purchasePrice.trim() || undefined,
          taxGroupId: form.taxGroupId || null,
          minQty: form.minQty.trim() || undefined,
          maxQty: form.maxQty.trim() || null,
          trackLot: form.trackLot,
          trackSerial: form.trackSerial,
        });
        setNotice({ kind: 'ok', text: `تم حفظ تعديل المادة ${form.sku}.` });
        cancelEdit();
      } else {
        await apiPost('/organization/catalog/items', {
          sku: form.sku.trim(),
          barcode: form.barcode.trim() || undefined,
          nameAr: form.nameAr.trim(),
          nameEn: form.nameEn.trim() || undefined,
          categoryId: form.categoryId,
          baseUnitId: form.baseUnitId,
          kind: form.kind,
          salePrice: form.salePrice.trim() || undefined,
          purchasePrice: form.purchasePrice.trim() || undefined,
          taxGroupId: form.taxGroupId || undefined,
          minQty: form.minQty.trim() || undefined,
          maxQty: form.maxQty.trim() || undefined,
          trackLot: form.trackLot,
          trackSerial: form.trackSerial,
        });
        setNotice({ kind: 'ok', text: `تمت إضافة المادة ${form.sku}.` });
        setForm({
          ...blank,
          categoryId: form.categoryId,
          baseUnitId: form.baseUnitId,
          taxGroupId: form.taxGroupId,
        });
      }
      items.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  async function remove(row: Item) {
    if (!window.confirm(`هل تريد حذف المادة ${row.sku}؟ إذا كانت لها حركات فسيتم أرشفتها فقط.`)) return;
    setBusy(true);
    setNotice(undefined);
    try {
      const result = await apiDelete<{ archived?: boolean }>(`/organization/catalog/items/${row.id}`);
      setNotice({
        kind: 'ok',
        text: result?.archived
          ? `للمادة ${row.sku} حركات سابقة، فتمت أرشفتها بدل حذفها.`
          : `تم حذف المادة ${row.sku}.`,
      });
      if (editing?.id === row.id) cancelEdit();
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
          <button
            className="btn primary"
            type="button"
            onClick={() => {
              if (open) cancelEdit();
              setOpen(!open);
            }}
          >
            {open ? 'إغلاق' : 'مادة جديدة'}
          </button>
        ) : null
      }
    >
      {open && (
        <form className="card" onSubmit={submit}>
          <h2>{editing ? `تعديل بطاقة المادة ${editing.sku}` : 'بطاقة مادة جديدة'}</h2>
          {missingRefs && !editing && (
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
              <span>الباركود</span>
              <input
                className="input"
                dir="ltr"
                value={form.barcode}
                onChange={set('barcode')}
                placeholder="يُستخدم الرمز (SKU) عند تركه فارغاً"
              />
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
              <select
                className="input"
                value={form.baseUnitId}
                onChange={set('baseUnitId')}
                required
                disabled={Boolean(editing)}
              >
                <option value="">— اختر —</option>
                {unitRows.map((row) => (
                  <option key={row.id} value={row.id}>{`${row.code} — ${arabicName(row)}`}</option>
                ))}
              </select>
              {editing && <span className="muted small">وحدة القياس الأساسية ثابتة بعد إنشاء المادة.</span>}
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
              <input
                className="input"
                dir="ltr"
                inputMode="decimal"
                value={form.salePrice}
                onChange={set('salePrice')}
              />
            </label>
            <label className="field">
              <span>سعر الشراء</span>
              <input
                className="input"
                dir="ltr"
                inputMode="decimal"
                value={form.purchasePrice}
                onChange={set('purchasePrice')}
              />
            </label>
            <label className="field">
              <span>حد الطلب (أدنى رصيد)</span>
              <input
                className="input"
                dir="ltr"
                inputMode="decimal"
                value={form.minQty}
                onChange={set('minQty')}
                placeholder="0"
              />
            </label>
            <label className="field">
              <span>الحد الأقصى</span>
              <input
                className="input"
                dir="ltr"
                inputMode="decimal"
                value={form.maxQty}
                onChange={set('maxQty')}
                placeholder="—"
              />
            </label>
            <label className="field">
              <span>تتبع بدفعات / تواريخ صلاحية</span>
              <select
                className="input"
                value={form.trackLot ? 'yes' : 'no'}
                onChange={(event) =>
                  setForm((current) => ({ ...current, trackLot: event.target.value === 'yes' }))
                }
              >
                <option value="no">لا</option>
                <option value="yes">نعم</option>
              </select>
            </label>
            <label className="field">
              <span>تتبع بأرقام تسلسلية</span>
              <select
                className="input"
                value={form.trackSerial ? 'yes' : 'no'}
                onChange={(event) =>
                  setForm((current) => ({ ...current, trackSerial: event.target.value === 'yes' }))
                }
              >
                <option value="no">لا</option>
                <option value="yes">نعم</option>
              </select>
            </label>
            <label className="field">
              <span>المجموعة الضريبية</span>
              <select className="input" value={form.taxGroupId} onChange={set('taxGroupId')}>
                <option value="">— بدون —</option>
                {(taxGroups.data ?? []).map((row) => (
                  <option key={row.id} value={row.id}>
                    {arabicName(row)}
                  </option>
                ))}
              </select>
            </label>
          </div>
          <Notice notice={notice} />
          <div className="row">
            <button className="btn primary" type="submit" disabled={busy || (missingRefs && !editing)}>
              {busy ? 'جارٍ الحفظ…' : editing ? 'حفظ التعديل' : 'حفظ المادة'}
            </button>
            {editing && (
              <button className="btn" type="button" onClick={cancelEdit} disabled={busy}>
                إلغاء التعديل
              </button>
            )}
          </div>
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

      {!open && notice && <Notice notice={notice} />}

      <QueryView query={items} empty="لا توجد مواد" emptyDetail="ابدأ بإضافة بطاقة مادة جديدة.">
        {(rows) => (
          <DataTable
            rows={rows}
            rowKey={(row) => row.id}
            columns={[
              { key: 'sku', header: 'الرمز', align: 'ltr', cell: (row) => row.sku },
              { key: 'barcode', header: 'الباركود', align: 'ltr', cell: (row) => row.barcode ?? '—' },
              { key: 'name', header: 'الاسم', cell: (row) => arabicName(row) },
              {
                key: 'kind',
                header: 'النوع',
                cell: (row) =>
                  row.kind === 'service' ? 'خدمة' : row.kind === 'composite' ? 'مركبة' : 'مخزنية',
              },
              {
                key: 'sale',
                header: 'سعر البيع',
                align: 'num',
                cell: (row) => money(row.salePrice ?? row.sale_price),
              },
              {
                key: 'purchase',
                header: 'سعر الشراء',
                align: 'num',
                cell: (row) => money(row.purchasePrice ?? row.purchase_price),
              },
              ...(can('catalog.item.manage')
                ? [
                    {
                      key: 'actions',
                      header: '',
                      cell: (row: Item) => (
                        <span className="row">
                          <button
                            className="btn sm"
                            type="button"
                            onClick={() => startEdit(row)}
                            disabled={busy}
                          >
                            تعديل
                          </button>
                          <button
                            className="btn sm danger"
                            type="button"
                            onClick={() => void remove(row)}
                            disabled={busy}
                          >
                            حذف
                          </button>
                        </span>
                      ),
                    },
                  ]
                : []),
            ]}
          />
        )}
      </QueryView>
    </Screen>
  );
}
