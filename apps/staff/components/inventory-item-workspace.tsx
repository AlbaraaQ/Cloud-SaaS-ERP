'use client';

import { useState, type FormEvent } from 'react';

import { ApiError, apiData, apiDelete, apiList, apiPatch, apiPost } from '../lib/api';
import {
  arabicName,
  listCategories,
  listItems,
  listTaxGroups,
  listUnits,
  money,
  percent,
  quantity,
  type Category,
  type Item,
  type TaxGroup,
  type Unit,
} from '../lib/lookups';
import { useSession } from '../lib/session';
import { useQuery } from '../lib/use-query';

import { DataTable, Notice, QueryView } from './data-view';
import { Forbidden, Loading, Screen } from './screen';

type ItemTab = 'basic' | 'inventory' | 'units' | 'codes';
type ItemUnit = {
  unitId: string;
  ratio: string;
  barcode?: string | null;
  salePrice?: string | null;
  purchasePrice?: string | null;
  isDefaultPurchase?: boolean;
  isDefaultSale?: boolean;
  code?: string | null;
  nameAr?: string | null;
  nameEn?: string | null;
  isBase?: boolean;
};
type ItemBarcode = {
  barcode: string;
  unitId?: string | null;
  source: 'primary' | 'unit' | 'alternate';
  primary?: boolean;
};
type BarcodeRegistry = {
  primary: { barcode: string; unitId: string; primary: boolean } | null;
  barcodes: ItemBarcode[];
};
type AlternativeCode = { id: string; code: string; notes?: string | null };

type ItemForm = {
  sku: string;
  barcode: string;
  nameAr: string;
  nameEn: string;
  categoryId: string;
  baseUnitId: string;
  kind: 'stock' | 'service' | 'composite';
  salePrice: string;
  purchasePrice: string;
  taxGroupId: string;
  minQty: string;
  maxQty: string;
  maxDiscountPct: string;
  maxDiscountAmt: string;
  trackLot: boolean;
  trackSerial: boolean;
  weightedScale: boolean;
  showInPos: boolean;
};
type ItemUnitForm = {
  unitId: string;
  ratio: string;
  barcode: string;
  salePrice: string;
  purchasePrice: string;
  isDefaultPurchase: boolean;
  isDefaultSale: boolean;
};

const EMPTY_ITEM: ItemForm = {
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
  minQty: '0',
  maxQty: '',
  maxDiscountPct: '',
  maxDiscountAmt: '',
  trackLot: false,
  trackSerial: false,
  weightedScale: false,
  showInPos: true,
};
const EMPTY_ITEM_UNIT: ItemUnitForm = {
  unitId: '',
  ratio: '1',
  barcode: '',
  salePrice: '',
  purchasePrice: '',
  isDefaultPurchase: false,
  isDefaultSale: false,
};

const ITEM_TABS: Array<{ id: ItemTab; label: string; needsSavedItem?: boolean }> = [
  { id: 'basic', label: 'البيانات الأساسية' },
  { id: 'inventory', label: 'المخزون والتشغيل', needsSavedItem: true },
  { id: 'units', label: 'وحدات الشراء والبيع', needsSavedItem: true },
  { id: 'codes', label: 'الباركود والأكواد', needsSavedItem: true },
];

function optionalText(value: string): string | undefined {
  return value.trim() || undefined;
}

function nullableText(value: string): string | null {
  return value.trim() || null;
}

function stringValue(...values: Array<string | null | undefined>): string {
  return values.find((value) => value !== undefined && value !== null) ?? '';
}

function boolValue(...values: Array<boolean | undefined>): boolean {
  return values.find((value) => value !== undefined) ?? false;
}

function formFromItem(item?: Item): ItemForm {
  if (!item) return { ...EMPTY_ITEM };
  return {
    sku: item.sku,
    barcode: item.barcode ?? '',
    nameAr: item.nameAr ?? item.name_ar ?? '',
    nameEn: item.nameEn ?? '',
    categoryId: item.categoryId ?? item.category_id ?? '',
    baseUnitId: item.baseUnitId ?? item.base_unit_id ?? '',
    kind: item.kind === 'service' || item.kind === 'composite' ? item.kind : 'stock',
    salePrice: stringValue(item.salePrice, item.sale_price),
    purchasePrice: stringValue(item.purchasePrice, item.purchase_price),
    taxGroupId: item.taxGroupId ?? item.tax_group_id ?? '',
    minQty: stringValue(item.minQty, item.min_qty, '0'),
    maxQty: stringValue(item.maxQty, item.max_qty),
    maxDiscountPct: stringValue(item.maxDiscountPct, item.max_discount_pct),
    maxDiscountAmt: stringValue(item.maxDiscountAmt, item.max_discount_amt),
    trackLot: boolValue(item.trackLot, item.track_lot),
    trackSerial: boolValue(item.trackSerial, item.track_serial),
    weightedScale: boolValue(item.weightedScale, item.weighted_scale),
    showInPos: boolValue(item.showInPos, item.show_in_pos),
  };
}

function kindLabel(kind?: string): string {
  if (kind === 'service') return 'خدمة';
  if (kind === 'composite') return 'مركب';
  return 'مخزني';
}

function sourceLabel(source: ItemBarcode['source']): string {
  if (source === 'primary') return 'أساسي';
  if (source === 'unit') return 'باركود وحدة';
  return 'إضافي';
}

/**
 * A full item card, inspired by the desktop's item/unit screens rather than a one-shot
 * directory form. A saved item becomes the parent of its unit, scanner-code and stock
 * policy tabs, while a new item intentionally starts with the immutable base unit first.
 */
export function InventoryItemWorkspace() {
  const { can } = useSession();
  const canManage = can('catalog.item.manage');
  const [search, setSearch] = useState('');
  const [appliedSearch, setAppliedSearch] = useState('');
  const items = useQuery<Item[]>(() => listItems(appliedSearch || undefined), [appliedSearch]);
  const categories = useQuery<Category[]>(() => listCategories(), []);
  const units = useQuery<Unit[]>(() => listUnits(), []);
  const taxGroups = useQuery<TaxGroup[]>(() => listTaxGroups(), []);

  const [cardOpen, setCardOpen] = useState(false);
  const [selected, setSelected] = useState<Item | undefined>();
  const [form, setForm] = useState<ItemForm>({ ...EMPTY_ITEM });
  const [tab, setTab] = useState<ItemTab>('basic');
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger' | 'info' | 'warn'; text: string }>();

  const itemUnits = useQuery<ItemUnit[]>(
    () => (selected ? apiList<ItemUnit>(`/organization/catalog/items/${selected.id}/units`) : Promise.resolve([])),
    [selected?.id],
  );
  const barcodeRegistry = useQuery<BarcodeRegistry>(
    () =>
      selected
        ? apiData<BarcodeRegistry>(`/organization/catalog/items/${selected.id}/barcodes`)
        : Promise.resolve({ primary: null, barcodes: [] }),
    [selected?.id],
  );
  const alternativeCodes = useQuery<AlternativeCode[]>(
    () => (selected ? apiList<AlternativeCode>(`/organization/catalog/items/${selected.id}/alternative-codes`) : Promise.resolve([])),
    [selected?.id],
  );

  const [unitForm, setUnitForm] = useState<ItemUnitForm>({ ...EMPTY_ITEM_UNIT });
  const [editingUnitId, setEditingUnitId] = useState<string>();
  const [barcodeForm, setBarcodeForm] = useState({ barcode: '', unitId: '' });
  const [alternativeForm, setAlternativeForm] = useState({ code: '', notes: '' });
  const [editingAlternativeCode, setEditingAlternativeCode] = useState<string>();

  const categoryRows = categories.data ?? [];
  const allUnits = units.data ?? [];
  const taxRows = taxGroups.data ?? [];
  const saved = Boolean(selected);
  const missingReferences = categoryRows.length === 0 || allUnits.length === 0;
  const readonly = !canManage || busy;

  function showError(error: unknown) {
    setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
  }

  function resetCard() {
    setSelected(undefined);
    setForm({ ...EMPTY_ITEM });
    setTab('basic');
    setCardOpen(false);
    setUnitForm({ ...EMPTY_ITEM_UNIT });
    setEditingUnitId(undefined);
    setBarcodeForm({ barcode: '', unitId: '' });
    setAlternativeForm({ code: '', notes: '' });
    setEditingAlternativeCode(undefined);
  }

  function openNew() {
    setSelected(undefined);
    setForm({ ...EMPTY_ITEM });
    setTab('basic');
    setNotice(undefined);
    setUnitForm({ ...EMPTY_ITEM_UNIT });
    setEditingUnitId(undefined);
    setBarcodeForm({ barcode: '', unitId: '' });
    setAlternativeForm({ code: '', notes: '' });
    setEditingAlternativeCode(undefined);
    setCardOpen(true);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  function openItem(item: Item) {
    setSelected(item);
    setForm(formFromItem(item));
    setTab('basic');
    setNotice(undefined);
    setCardOpen(true);
    setUnitForm({ ...EMPTY_ITEM_UNIT });
    setEditingUnitId(undefined);
    setBarcodeForm({ barcode: '', unitId: item.baseUnitId ?? item.base_unit_id ?? '' });
    setAlternativeForm({ code: '', notes: '' });
    setEditingAlternativeCode(undefined);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  async function saveItem(event: FormEvent) {
    event.preventDefault();
    if (!canManage) return;
    if (!selected && missingReferences) {
      setNotice({ kind: 'warn', text: 'أنشئ مجموعة ووحدة قياس واحدة على الأقل قبل حفظ بطاقة الصنف.' });
      return;
    }
    setBusy(true);
    setNotice(undefined);
    try {
      const common = {
        sku: form.sku.trim(),
        barcode: nullableText(form.barcode),
        nameAr: form.nameAr.trim(),
        nameEn: nullableText(form.nameEn),
        categoryId: form.categoryId,
        kind: form.kind,
        salePrice: nullableText(form.salePrice),
        purchasePrice: nullableText(form.purchasePrice),
        taxGroupId: form.taxGroupId || null,
        minQty: form.minQty.trim() || '0',
        maxQty: nullableText(form.maxQty),
        maxDiscountPct: nullableText(form.maxDiscountPct),
        maxDiscountAmt: nullableText(form.maxDiscountAmt),
        trackLot: form.trackLot,
        trackSerial: form.trackSerial,
        weightedScale: form.weightedScale,
        showInPos: form.showInPos,
      };
      const savedItem = selected
        ? await apiPatch<Item>(`/organization/catalog/items/${selected.id}`, common)
        : await apiPost<Item>('/organization/catalog/items', {
            ...common,
            // Create accepts omitted optional prices rather than `null`.
            barcode: optionalText(form.barcode),
            nameEn: optionalText(form.nameEn),
            salePrice: optionalText(form.salePrice),
            purchasePrice: optionalText(form.purchasePrice),
            taxGroupId: form.taxGroupId || undefined,
            maxQty: optionalText(form.maxQty),
            maxDiscountPct: optionalText(form.maxDiscountPct),
            maxDiscountAmt: optionalText(form.maxDiscountAmt),
            baseUnitId: form.baseUnitId,
          });
      setSelected(savedItem);
      setForm(formFromItem(savedItem));
      setCardOpen(true);
      setNotice({ kind: 'ok', text: selected ? `تم حفظ بطاقة ${savedItem.sku}.` : `تم إنشاء بطاقة ${savedItem.sku}. يمكنك الآن إضافة الوحدات والأكواد.` });
      items.reload();
    } catch (error) {
      showError(error);
    } finally {
      setBusy(false);
    }
  }

  async function removeItem(item: Item) {
    if (!canManage || !window.confirm(`هل تريد حذف المادة ${item.sku}؟ إذا كانت لها حركات، سيؤرشفها النظام بدلاً من حذف تاريخها.`)) return;
    setBusy(true);
    setNotice(undefined);
    try {
      const result = await apiDelete<{ archived?: boolean }>(`/organization/catalog/items/${item.id}`);
      setNotice({ kind: 'ok', text: result.archived ? `أُرشفت ${item.sku} لأن لها تاريخ حركات.` : `تم حذف ${item.sku}.` });
      if (selected?.id === item.id) resetCard();
      items.reload();
    } catch (error) {
      showError(error);
    } finally {
      setBusy(false);
    }
  }

  function resetUnitForm() {
    setEditingUnitId(undefined);
    setUnitForm({ ...EMPTY_ITEM_UNIT });
  }

  function editUnit(row: ItemUnit) {
    if (row.isBase) return;
    setEditingUnitId(row.unitId);
    setUnitForm({
      unitId: row.unitId,
      ratio: row.ratio,
      barcode: row.barcode ?? '',
      salePrice: row.salePrice ?? '',
      purchasePrice: row.purchasePrice ?? '',
      isDefaultPurchase: Boolean(row.isDefaultPurchase),
      isDefaultSale: Boolean(row.isDefaultSale),
    });
  }

  async function saveUnit(event: FormEvent) {
    event.preventDefault();
    if (!selected || !canManage) return;
    setBusy(true);
    setNotice(undefined);
    try {
      await apiPost(`/organization/catalog/items/${selected.id}/units`, {
        unitId: unitForm.unitId,
        ratio: unitForm.ratio.trim(),
        // An empty value is intentional: the API clears a previously set unit barcode.
        barcode: unitForm.barcode.trim(),
        salePrice: nullableText(unitForm.salePrice),
        purchasePrice: nullableText(unitForm.purchasePrice),
        isDefaultPurchase: unitForm.isDefaultPurchase,
        isDefaultSale: unitForm.isDefaultSale,
      });
      setNotice({ kind: 'ok', text: editingUnitId ? 'تم حفظ إعدادات الوحدة البديلة.' : 'تمت إضافة الوحدة البديلة.' });
      resetUnitForm();
      itemUnits.reload();
      barcodeRegistry.reload();
    } catch (error) {
      showError(error);
    } finally {
      setBusy(false);
    }
  }

  async function removeUnit(row: ItemUnit) {
    if (!selected || row.isBase || !canManage || !window.confirm(`هل تريد إزالة وحدة ${row.code ?? ''} من بطاقة ${selected.sku}؟`)) return;
    setBusy(true);
    setNotice(undefined);
    try {
      await apiDelete(`/organization/catalog/items/${selected.id}/units/${row.unitId}`);
      setNotice({ kind: 'ok', text: 'تمت إزالة الوحدة البديلة.' });
      if (editingUnitId === row.unitId) resetUnitForm();
      itemUnits.reload();
      barcodeRegistry.reload();
    } catch (error) {
      showError(error);
    } finally {
      setBusy(false);
    }
  }

  async function addBarcode(event: FormEvent) {
    event.preventDefault();
    if (!selected || !canManage) return;
    setBusy(true);
    setNotice(undefined);
    try {
      await apiPost(`/organization/catalog/items/${selected.id}/barcodes`, {
        barcode: barcodeForm.barcode.trim(),
        unitId: barcodeForm.unitId || form.baseUnitId,
      });
      setBarcodeForm((current) => ({ ...current, barcode: '' }));
      setNotice({ kind: 'ok', text: 'تمت إضافة الباركود الإضافي وربطه بالوحدة المختارة.' });
      barcodeRegistry.reload();
    } catch (error) {
      showError(error);
    } finally {
      setBusy(false);
    }
  }

  async function removeBarcode(row: ItemBarcode) {
    if (!selected || row.source !== 'alternate' || !canManage || !window.confirm(`هل تريد إزالة الباركود ${row.barcode}؟`)) return;
    setBusy(true);
    setNotice(undefined);
    try {
      await apiDelete(`/organization/catalog/items/${selected.id}/barcodes/${encodeURIComponent(row.barcode)}`);
      setNotice({ kind: 'ok', text: 'تمت إزالة الباركود الإضافي.' });
      barcodeRegistry.reload();
    } catch (error) {
      showError(error);
    } finally {
      setBusy(false);
    }
  }

  async function saveAlternativeCode(event: FormEvent) {
    event.preventDefault();
    if (!selected || !canManage) return;
    setBusy(true);
    setNotice(undefined);
    try {
      await apiPost(`/organization/catalog/items/${selected.id}/alternative-codes`, {
        code: alternativeForm.code.trim(),
        notes: optionalText(alternativeForm.notes),
      });
      setNotice({ kind: 'ok', text: editingAlternativeCode ? 'تم تحديث بيان الكود البديل.' : 'تم حفظ الكود البديل. يمكن استخدامه الآن في قارئ الباركود والبحث.' });
      setAlternativeForm({ code: '', notes: '' });
      setEditingAlternativeCode(undefined);
      alternativeCodes.reload();
    } catch (error) {
      showError(error);
    } finally {
      setBusy(false);
    }
  }

  function editAlternativeCode(row: AlternativeCode) {
    setEditingAlternativeCode(row.code);
    setAlternativeForm({ code: row.code, notes: row.notes ?? '' });
  }

  async function removeAlternativeCode(row: AlternativeCode) {
    if (!selected || !canManage || !window.confirm(`هل تريد إيقاف الكود البديل ${row.code}؟`)) return;
    setBusy(true);
    setNotice(undefined);
    try {
      await apiDelete(`/organization/catalog/items/${selected.id}/alternative-codes/${encodeURIComponent(row.code)}`);
      setNotice({ kind: 'ok', text: 'تم إيقاف الكود البديل مع الاحتفاظ بسجل التدقيق.' });
      if (editingAlternativeCode === row.code) {
        setAlternativeForm({ code: '', notes: '' });
        setEditingAlternativeCode(undefined);
      }
      alternativeCodes.reload();
    } catch (error) {
      showError(error);
    } finally {
      setBusy(false);
    }
  }

  if (!can('catalog.item.view')) return <Forbidden />;

  const categoryLabel = categoryRows.find((row) => row.id === form.categoryId);
  const baseUnit = allUnits.find((row) => row.id === form.baseUnitId);
  const configuredUnits = itemUnits.data ?? [];
  const unitChoices = configuredUnits.length
    ? configuredUnits
    : form.baseUnitId
      ? [{ unitId: form.baseUnitId, code: baseUnit?.code ?? null, nameAr: baseUnit?.nameAr ?? null, isBase: true, ratio: '1' }]
      : [];
  // Unit identity is part of the item-unit key. New-unit mode intentionally excludes
  // already-configured choices; edit mode keeps just its own identity fixed.
  const selectableAlternativeUnits = allUnits.filter(
    (unit) =>
      unit.id !== form.baseUnitId &&
      (unit.id === editingUnitId || !configuredUnits.some((configured) => configured.unitId === unit.id)),
  );

  return (
    <Screen
      title="بطاقات الأصناف"
      subtitle="بطاقة تشغيلية موحدة للبيانات، سياسة المخزون، وحدات الشراء والبيع، والباركود والأكواد البديلة."
      crumbs={['المستودعات', 'التعاريف']}
      actions={
        <>
          <button className="btn" type="button" onClick={items.reload} disabled={busy}>
            تحديث الدليل
          </button>
          {canManage ? (
            <button className="btn primary" type="button" onClick={openNew} disabled={busy}>
              مادة جديدة
            </button>
          ) : null}
        </>
      }
    >
      {cardOpen && (
        <section className="card item-card">
          <div className="item-card-head">
            <div>
              <p className="crumbs">المخزون ← بطاقة الصنف</p>
              <h2>{selected ? `${selected.sku} — ${form.nameAr || 'بطاقة صنف'}` : 'بطاقة صنف جديدة'}</h2>
              <p className="muted small" style={{ margin: '4px 0 0' }}>
                {selected
                  ? 'التغييرات المخزنية الحساسة مقيدة بالخادم بعد أول حركة، لحماية معنى الأرصدة والسجل.'
                  : 'احفظ البيانات الأساسية ووحدة القياس الأساسية أولاً، ثم افتح تبويبات الوحدات والأكواد.'}
              </p>
            </div>
            <div className="row">
              {selected ? <span className="badge active">{kindLabel(form.kind)}</span> : <span className="badge draft">جديد</span>}
              <button className="btn" type="button" onClick={resetCard} disabled={busy}>
                إغلاق البطاقة
              </button>
            </div>
          </div>

          {selected && (
            <div className="item-summary" aria-label="ملخص الصنف">
              <span><b dir="ltr">{selected.sku}</b> رمز الصنف</span>
              <span>{categoryLabel ? arabicName(categoryLabel) : '—'} المجموعة</span>
              <span>{baseUnit ? arabicName(baseUnit) : '—'} الوحدة الأساسية</span>
              <span>{form.showInPos ? 'ظاهر في نقطة البيع' : 'مخفي من نقطة البيع'}</span>
            </div>
          )}

          <nav className="item-tabs" aria-label="أقسام بطاقة الصنف">
            {ITEM_TABS.map((entry) => (
              <button
                className={`item-tab${tab === entry.id ? ' active' : ''}`}
                type="button"
                key={entry.id}
                disabled={Boolean(entry.needsSavedItem && !saved)}
                onClick={() => setTab(entry.id)}
              >
                {entry.label}
                {entry.needsSavedItem && !saved ? ' (بعد الحفظ)' : ''}
              </button>
            ))}
          </nav>

          {tab === 'basic' && (
            <form className="form" onSubmit={saveItem}>
              {!selected && missingReferences ? (
                <Notice notice={{ kind: 'warn', text: 'يلزم وجود مجموعة ووحدة قياس واحدة على الأقل. أنشئهما من بطاقات التعاريف أولاً.' }} />
              ) : null}
              <div className="form-grid">
                <label className="field">
                  <span>رمز الصنف (SKU) *</span>
                  <input className="input" dir="ltr" value={form.sku} required disabled={readonly} onChange={(event) => setForm((current) => ({ ...current, sku: event.target.value }))} />
                </label>
                <label className="field">
                  <span>الباركود الأساسي</span>
                  <input className="input" dir="ltr" value={form.barcode} disabled={readonly} placeholder="يُربط تلقائياً بالوحدة الأساسية" onChange={(event) => setForm((current) => ({ ...current, barcode: event.target.value }))} />
                </label>
                <label className="field">
                  <span>الاسم العربي *</span>
                  <input className="input" value={form.nameAr} required disabled={readonly} onChange={(event) => setForm((current) => ({ ...current, nameAr: event.target.value }))} />
                </label>
                <label className="field">
                  <span>الاسم الإنجليزي</span>
                  <input className="input" dir="ltr" value={form.nameEn} disabled={readonly} onChange={(event) => setForm((current) => ({ ...current, nameEn: event.target.value }))} />
                </label>
                <label className="field">
                  <span>المجموعة *</span>
                  <select className="input" value={form.categoryId} required disabled={readonly} onChange={(event) => setForm((current) => ({ ...current, categoryId: event.target.value }))}>
                    <option value="">— اختر —</option>
                    {categoryRows.map((row) => <option key={row.id} value={row.id}>{row.code} — {arabicName(row)}</option>)}
                  </select>
                </label>
                <label className="field">
                  <span>الوحدة الأساسية *</span>
                  <select className="input" value={form.baseUnitId} required disabled={readonly || Boolean(selected)} onChange={(event) => setForm((current) => ({ ...current, baseUnitId: event.target.value }))}>
                    <option value="">— اختر —</option>
                    {allUnits.map((row) => <option key={row.id} value={row.id}>{row.code} — {arabicName(row)}</option>)}
                  </select>
                  {selected ? <span className="muted small">الوحدة الأساسية لا تتغير بعد الحفظ لأن جميع أرصدة المادة معبرة بها.</span> : null}
                </label>
                <label className="field">
                  <span>نوع الصنف</span>
                  <select className="input" value={form.kind} disabled={readonly} onChange={(event) => setForm((current) => ({ ...current, kind: event.target.value as ItemForm['kind'] }))}>
                    <option value="stock">مخزني</option>
                    <option value="service">خدمة</option>
                    <option value="composite">مركب</option>
                  </select>
                </label>
                <label className="field">
                  <span>المجموعة الضريبية</span>
                  <select className="input" value={form.taxGroupId} disabled={readonly} onChange={(event) => setForm((current) => ({ ...current, taxGroupId: event.target.value }))}>
                    <option value="">— بدون —</option>
                    {taxRows.map((row) => <option key={row.id} value={row.id}>{arabicName(row)} — {percent(row.rate)}</option>)}
                  </select>
                </label>
                <label className="field">
                  <span>سعر البيع الافتراضي</span>
                  <input className="input" dir="ltr" inputMode="decimal" value={form.salePrice} disabled={readonly} placeholder="0.0000" onChange={(event) => setForm((current) => ({ ...current, salePrice: event.target.value }))} />
                </label>
                <label className="field">
                  <span>سعر الشراء الافتراضي</span>
                  <input className="input" dir="ltr" inputMode="decimal" value={form.purchasePrice} disabled={readonly} placeholder="0.0000" onChange={(event) => setForm((current) => ({ ...current, purchasePrice: event.target.value }))} />
                </label>
              </div>
              <Notice notice={notice} />
              {canManage ? (
                <div className="row">
                  <button className="btn primary" type="submit" disabled={busy || (!selected && missingReferences)}>
                    {busy ? 'جارٍ الحفظ…' : selected ? 'حفظ البيانات الأساسية' : 'إنشاء البطاقة'}
                  </button>
                  {selected ? <button className="btn" type="button" onClick={() => setTab('inventory')} disabled={busy}>متابعة إعدادات المخزون</button> : null}
                </div>
              ) : <p className="alert info">لديك صلاحية العرض فقط. لا يمكن تعديل بطاقة الصنف.</p>}
            </form>
          )}

          {tab === 'inventory' && selected && (
            <form className="form" onSubmit={saveItem}>
              <div className="item-section-intro">
                <h3>سياسة المخزون والتشغيل</h3>
                <p className="muted small">حدود إعادة الطلب والخصم والتتبع تتحكم في العمليات اللاحقة. بعد الحركة يحمي الخادم SKU ونسبة الوحدة من أي تغيير يعيد تفسير السجل.</p>
              </div>
              <div className="form-grid">
                <label className="field">
                  <span>الحد الأدنى لإعادة الطلب</span>
                  <input className="input" dir="ltr" inputMode="decimal" value={form.minQty} disabled={readonly} onChange={(event) => setForm((current) => ({ ...current, minQty: event.target.value }))} />
                </label>
                <label className="field">
                  <span>الحد الأعلى للمخزون</span>
                  <input className="input" dir="ltr" inputMode="decimal" value={form.maxQty} disabled={readonly} placeholder="اختياري" onChange={(event) => setForm((current) => ({ ...current, maxQty: event.target.value }))} />
                </label>
                <label className="field">
                  <span>أقصى خصم نسبة %</span>
                  <input className="input" dir="ltr" inputMode="decimal" value={form.maxDiscountPct} disabled={readonly} placeholder="0 إلى 100" onChange={(event) => setForm((current) => ({ ...current, maxDiscountPct: event.target.value }))} />
                </label>
                <label className="field">
                  <span>أقصى خصم مبلغ</span>
                  <input className="input" dir="ltr" inputMode="decimal" value={form.maxDiscountAmt} disabled={readonly} placeholder="اختياري" onChange={(event) => setForm((current) => ({ ...current, maxDiscountAmt: event.target.value }))} />
                </label>
              </div>
              <div className="item-switches">
                <label className="item-switch">
                  <input type="checkbox" checked={form.trackLot} disabled={readonly} onChange={(event) => setForm((current) => ({ ...current, trackLot: event.target.checked }))} />
                  <span><b>تتبع الدفعات / الصلاحية</b><small>يربط الحركات برقم التشغيلة وتاريخ الانتهاء.</small></span>
                </label>
                <label className="item-switch">
                  <input type="checkbox" checked={form.trackSerial} disabled={readonly} onChange={(event) => setForm((current) => ({ ...current, trackSerial: event.target.checked }))} />
                  <span><b>تتبع السيريال</b><small>يلزم اختيار سيريال لكل وحدة عند الترحيل.</small></span>
                </label>
                <label className="item-switch">
                  <input type="checkbox" checked={form.weightedScale} disabled={readonly} onChange={(event) => setForm((current) => ({ ...current, weightedScale: event.target.checked }))} />
                  <span><b>يباع بالميزان</b><small>يسمح بكميات عشرية في نقاط البيع والمستودع.</small></span>
                </label>
                <label className="item-switch">
                  <input type="checkbox" checked={form.showInPos} disabled={readonly} onChange={(event) => setForm((current) => ({ ...current, showInPos: event.target.checked }))} />
                  <span><b>ظاهر في نقطة البيع</b><small>إيقافه يخفي الصنف من اختصارات وكاشير POS دون حذف تاريخه.</small></span>
                </label>
              </div>
              <Notice notice={notice} />
              {canManage ? <div className="row"><button className="btn primary" type="submit" disabled={busy}>{busy ? 'جارٍ الحفظ…' : 'حفظ سياسة المخزون'}</button></div> : null}
            </form>
          )}

          {tab === 'units' && selected && (
            <div className="grid">
              <div className="item-section-intro">
                <h3>وحدات الشراء والبيع</h3>
                <p className="muted small">النسبة تعني عدد الوحدات الأساسية داخل الوحدة البديلة. يثبت الخادم النسبة ولا يسمح بحذف الوحدة بعد أول حركة عليها.</p>
              </div>
              <QueryView query={itemUnits} empty="لا توجد وحدات" emptyDetail="الوحدة الأساسية ستظهر تلقائياً بعد إعادة التحميل.">
                {(rows) => (
                  <DataTable
                    rows={rows}
                    rowKey={(row) => row.unitId}
                    columns={[
                      { key: 'unit', header: 'الوحدة', cell: (row) => <>{row.code ?? '—'} — {row.nameAr ?? row.nameEn ?? '—'} {row.isBase ? <span className="badge">أساسية</span> : null}</> },
                      { key: 'ratio', header: 'التحويل للأساس', align: 'num', cell: (row) => quantity(row.ratio) },
                      { key: 'sale', header: 'سعر البيع', align: 'num', cell: (row) => money(row.salePrice) },
                      { key: 'purchase', header: 'سعر الشراء', align: 'num', cell: (row) => money(row.purchasePrice) },
                      { key: 'barcode', header: 'باركود الوحدة', align: 'ltr', cell: (row) => row.barcode ?? '—' },
                      { key: 'defaults', header: 'افتراضية', cell: (row) => <span className="chips">{row.isDefaultPurchase ? <span className="chip">شراء</span> : null}{row.isDefaultSale ? <span className="chip">بيع</span> : null}{!row.isDefaultPurchase && !row.isDefaultSale ? '—' : null}</span> },
                      ...(canManage ? [{ key: 'actions', header: '', cell: (row: ItemUnit) => row.isBase ? <span className="muted small">تدار من البيانات الأساسية</span> : <span className="row"><button className="btn sm" type="button" disabled={busy} onClick={() => editUnit(row)}>تعديل</button><button className="btn sm danger" type="button" disabled={busy} onClick={() => void removeUnit(row)}>إزالة</button></span> }] : []),
                    ]}
                  />
                )}
              </QueryView>

              {canManage && (
                <form className="card item-subform" onSubmit={saveUnit}>
                  <div className="section-title">
                    <div><h3>{editingUnitId ? 'تعديل وحدة بديلة' : 'إضافة وحدة بديلة'}</h3><p className="muted small" style={{ margin: '3px 0 0' }}>تعرض أسعار هذه الوحدة عند اختيارها في المستندات أو المسح.</p></div>
                    {editingUnitId ? <button className="btn sm" type="button" onClick={resetUnitForm} disabled={busy}>وحدة جديدة</button> : null}
                  </div>
                  <div className="form-grid">
                    <label className="field"><span>الوحدة *</span><select className="input" value={unitForm.unitId} required disabled={readonly || Boolean(editingUnitId)} onChange={(event) => setUnitForm((current) => ({ ...current, unitId: event.target.value }))}><option value="">— اختر —</option>{selectableAlternativeUnits.map((unit) => <option key={unit.id} value={unit.id}>{unit.code} — {arabicName(unit)}</option>)}</select>{editingUnitId ? <small className="muted">لا تتغير هوية الوحدة؛ أزلها ثم أضف وحدة أخرى عند الحاجة.</small> : null}</label>
                    <label className="field"><span>نسبة التحويل *</span><input className="input" dir="ltr" inputMode="decimal" value={unitForm.ratio} required disabled={readonly} onChange={(event) => setUnitForm((current) => ({ ...current, ratio: event.target.value }))} /></label>
                    <label className="field"><span>باركود الوحدة</span><input className="input" dir="ltr" value={unitForm.barcode} disabled={readonly} placeholder="إفراغه يمسح باركود الوحدة" onChange={(event) => setUnitForm((current) => ({ ...current, barcode: event.target.value }))} /></label>
                    <label className="field"><span>سعر بيع الوحدة</span><input className="input" dir="ltr" inputMode="decimal" value={unitForm.salePrice} disabled={readonly} placeholder="اختياري" onChange={(event) => setUnitForm((current) => ({ ...current, salePrice: event.target.value }))} /></label>
                    <label className="field"><span>سعر شراء الوحدة</span><input className="input" dir="ltr" inputMode="decimal" value={unitForm.purchasePrice} disabled={readonly} placeholder="اختياري" onChange={(event) => setUnitForm((current) => ({ ...current, purchasePrice: event.target.value }))} /></label>
                    <label className="item-switch compact"><input type="checkbox" checked={unitForm.isDefaultPurchase} disabled={readonly} onChange={(event) => setUnitForm((current) => ({ ...current, isDefaultPurchase: event.target.checked }))} /><span><b>وحدة الشراء الافتراضية</b></span></label>
                    <label className="item-switch compact"><input type="checkbox" checked={unitForm.isDefaultSale} disabled={readonly} onChange={(event) => setUnitForm((current) => ({ ...current, isDefaultSale: event.target.checked }))} /><span><b>وحدة البيع الافتراضية</b></span></label>
                  </div>
                  <Notice notice={notice} />
                  <div className="row"><button className="btn primary" type="submit" disabled={busy}>{busy ? 'جارٍ الحفظ…' : editingUnitId ? 'حفظ الوحدة' : 'إضافة الوحدة'}</button></div>
                </form>
              )}
            </div>
          )}

          {tab === 'codes' && selected && (
            <div className="grid">
              <div className="item-section-intro">
                <h3>الباركود والأكواد البديلة</h3>
                <p className="muted small">يتحقق الخادم من عدم تكرار الرمز عبر SKU والباركود الأساسي وباركود الوحدة والأكواد البديلة داخل المنشأة كاملة.</p>
              </div>
              {barcodeRegistry.status === 'loading' ? <Loading rows={2} /> : null}
              {barcodeRegistry.status === 'error' ? <Notice notice={{ kind: 'danger', text: barcodeRegistry.error ?? 'تعذر تحميل الباركودات.' }} /> : null}
              {barcodeRegistry.status === 'success' && (
                <DataTable
                  rows={barcodeRegistry.data?.barcodes ?? []}
                  rowKey={(row) => row.barcode}
                  columns={[
                    { key: 'barcode', header: 'الباركود', align: 'ltr', cell: (row) => row.barcode },
                    { key: 'source', header: 'المصدر', cell: (row) => sourceLabel(row.source) },
                    { key: 'unit', header: 'الوحدة', cell: (row) => unitChoices.find((unit) => unit.unitId === row.unitId)?.code ?? '—' },
                    ...(canManage ? [{ key: 'actions', header: '', cell: (row: ItemBarcode) => row.source === 'alternate' ? <button className="btn sm danger" type="button" disabled={busy} onClick={() => void removeBarcode(row)}>إزالة</button> : <span className="muted small">يُدار من {row.source === 'primary' ? 'البيانات الأساسية' : 'تبويب الوحدات'}</span> }] : []),
                  ]}
                />
              )}
              {barcodeRegistry.status === 'success' && (barcodeRegistry.data?.barcodes.length ?? 0) === 0 ? <p className="alert info">لا توجد باركودات مسجلة بعد؛ أضف الباركود الأساسي أو رمزاً إضافياً.</p> : null}

              {canManage && (
                <form className="card item-subform" onSubmit={addBarcode}>
                  <h3>باركود إضافي</h3>
                  <div className="form-grid">
                    <label className="field"><span>الباركود *</span><input className="input" dir="ltr" required value={barcodeForm.barcode} disabled={readonly} onChange={(event) => setBarcodeForm((current) => ({ ...current, barcode: event.target.value }))} /></label>
                    <label className="field"><span>يتبع الوحدة</span><select className="input" value={barcodeForm.unitId} disabled={readonly} onChange={(event) => setBarcodeForm((current) => ({ ...current, unitId: event.target.value }))}>{unitChoices.map((unit) => <option key={unit.unitId} value={unit.unitId}>{unit.code ?? '—'} — {unit.nameAr ?? unit.nameEn ?? ''}</option>)}</select></label>
                  </div>
                  <div className="row"><button className="btn primary" type="submit" disabled={busy}>{busy ? 'جارٍ الحفظ…' : 'إضافة الباركود'}</button></div>
                </form>
              )}

              <div className="card item-subform">
                <div className="section-title"><div><h3>أكواد بديلة ومورد</h3><p className="muted small" style={{ margin: '3px 0 0' }}>تصلح لرمز المورد أو رمز الديسكتوب السابق، وتظهر في lookup كـ«كود بديل».</p></div></div>
                <QueryView query={alternativeCodes} empty="لا توجد أكواد بديلة" emptyDetail="أضف رمز المورد أو رمزاً وراثياً ليتعرف عليه الماسح.">
                  {(rows) => <DataTable rows={rows} rowKey={(row) => row.id} columns={[
                    { key: 'code', header: 'الكود', align: 'ltr', cell: (row) => row.code },
                    { key: 'notes', header: 'البيان', cell: (row) => row.notes ?? '—' },
                    ...(canManage ? [{ key: 'actions', header: '', cell: (row: AlternativeCode) => <span className="row"><button className="btn sm" type="button" disabled={busy} onClick={() => editAlternativeCode(row)}>تعديل البيان</button><button className="btn sm danger" type="button" disabled={busy} onClick={() => void removeAlternativeCode(row)}>إيقاف</button></span> }] : []),
                  ]} />}
                </QueryView>
                {canManage && (
                  <form className="form" onSubmit={saveAlternativeCode} style={{ marginTop: 12 }}>
                    <div className="form-grid">
                      <label className="field"><span>الكود البديل *</span><input className="input" dir="ltr" required value={alternativeForm.code} disabled={readonly || Boolean(editingAlternativeCode)} onChange={(event) => setAlternativeForm((current) => ({ ...current, code: event.target.value }))} />{editingAlternativeCode ? <small className="muted">لا يغيّر التعديل الرمز نفسه؛ أوقفه ثم أضف رمزاً جديداً عند الحاجة.</small> : null}</label>
                      <label className="field"><span>بيان / مصدر الكود</span><input className="input" value={alternativeForm.notes} disabled={readonly} placeholder="مثال: رمز المورد" onChange={(event) => setAlternativeForm((current) => ({ ...current, notes: event.target.value }))} /></label>
                    </div>
                    <div className="row"><button className="btn primary" type="submit" disabled={busy}>{busy ? 'جارٍ الحفظ…' : editingAlternativeCode ? 'حفظ البيان' : 'حفظ الكود البديل'}</button>{editingAlternativeCode ? <button className="btn" type="button" disabled={busy} onClick={() => { setAlternativeForm({ code: '', notes: '' }); setEditingAlternativeCode(undefined); }}>إلغاء التعديل</button> : null}</div>
                  </form>
                )}
              </div>
              <Notice notice={notice} />
            </div>
          )}
        </section>
      )}

      <div className="card toolbar no-print">
        <input
          className="input"
          value={search}
          placeholder="ابحث بالاسم أو SKU أو الباركود الأساسي"
          onChange={(event) => setSearch(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter') setAppliedSearch(search.trim());
          }}
        />
        <button className="btn" type="button" onClick={() => setAppliedSearch(search.trim())}>بحث</button>
        {appliedSearch ? <button className="btn" type="button" onClick={() => { setSearch(''); setAppliedSearch(''); }}>إلغاء الفلتر</button> : null}
      </div>

      {!cardOpen ? <Notice notice={notice} /> : null}
      <QueryView query={items} empty="لا توجد أصناف" emptyDetail="ابدأ بإنشاء بطاقة صنف ثم أضف وحداتها وباركوداتها من داخل البطاقة.">
        {(rows) => (
          <DataTable
            rows={rows}
            rowKey={(row) => row.id}
            columns={[
              { key: 'sku', header: 'الرمز', align: 'ltr', cell: (row) => row.sku },
              { key: 'name', header: 'الصنف', cell: (row) => arabicName(row) },
              { key: 'barcode', header: 'الباركود الأساسي', align: 'ltr', cell: (row) => row.barcode ?? '—' },
              { key: 'kind', header: 'النوع', cell: (row) => kindLabel(row.kind) },
              { key: 'sale', header: 'سعر البيع', align: 'num', cell: (row) => money(row.salePrice ?? row.sale_price) },
              { key: 'stock', header: 'التشغيل', cell: (row) => <span className="chips">{row.trackLot ?? row.track_lot ? <span className="chip">دفعات</span> : null}{row.trackSerial ?? row.track_serial ? <span className="chip">سيريال</span> : null}{row.weightedScale ?? row.weighted_scale ? <span className="chip">ميزان</span> : null}{!(row.trackLot ?? row.track_lot) && !(row.trackSerial ?? row.track_serial) && !(row.weightedScale ?? row.weighted_scale) ? '—' : null}</span> },
              { key: 'open', header: '', cell: (row) => <span className="row"><button className="btn sm" type="button" disabled={busy} onClick={() => openItem(row)}>فتح البطاقة</button>{canManage ? <button className="btn sm danger" type="button" disabled={busy} onClick={() => void removeItem(row)}>حذف</button> : null}</span> },
            ]}
          />
        )}
      </QueryView>
    </Screen>
  );
}
