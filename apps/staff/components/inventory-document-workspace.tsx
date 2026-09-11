'use client';

import Link from 'next/link';
import { useEffect, useMemo, useState } from 'react';

import { accountLabel, listAccounts, postableOf, type Account } from '../lib/accounts';
import { ApiError, apiData, apiList, apiPatch, apiPost } from '../lib/api';
import {
  arabicName,
  defaultOf,
  itemLabel,
  listBranches,
  listItems,
  listWarehouses,
  money,
  quantity,
  shortDate,
  statusLabel,
  today,
  type Branch,
  type Item,
  type Warehouse,
} from '../lib/lookups';
import { useSession } from '../lib/session';
import { useQuery } from '../lib/use-query';

import { DataTable, Notice, QueryView } from './data-view';
import { Forbidden, Screen } from './screen';

export type InventoryDocumentKind = 'opening' | 'receipt' | 'issue' | 'adjustment';

type ItemUnit = {
  unitId: string;
  ratio: string;
  code?: string | null;
  nameAr?: string | null;
  nameEn?: string | null;
  isBase?: boolean;
};
type InventoryLot = { id: string; itemId: string; lotNo: string; expiryDate?: string | null; onHand?: string };
type InventorySerial = { id: string; itemId: string; serialNo: string; warehouseId?: string | null; status: string };
type DocumentLine = {
  id: string;
  lineNo: number;
  itemId: string;
  unitId: string;
  quantity: string;
  baseQuantity: string;
  unitCost: string;
  lotId?: string | null;
  serialIds: string[];
  adjustmentDirection?: 'in' | 'out' | null;
  note?: string | null;
};
type InventoryDocument = {
  id: string;
  number: string;
  kind: InventoryDocumentKind;
  status: 'draft' | 'posted' | 'voided' | 'cancelled';
  branchId: string;
  warehouseId: string;
  documentDate: string;
  reason?: string | null;
  counterAccountId?: string | null;
  notes?: string | null;
  journalEntryId?: string | null;
  postedAt?: string | null;
  lines: DocumentLine[];
};
type ScannerLookup = { item: Item; unitId: string; ratio: string; matchedAs: string };
type DraftLine = {
  key: string;
  itemId: string;
  unitId: string;
  quantity: string;
  unitCost: string;
  lotId: string;
  serialIds: string[];
  adjustmentDirection: '' | 'in' | 'out';
  note: string;
};

type WorkspaceProps = {
  kind: InventoryDocumentKind;
  title: string;
  subtitle: string;
  newLabel: string;
};

const KIND_LABEL: Record<InventoryDocumentKind, string> = {
  opening: 'بضاعة أول المدة',
  receipt: 'فاتورة إدخال مخزني',
  issue: 'فاتورة إخراج مخزني',
  adjustment: 'تسوية مخزنية',
};

const KIND_PREFIX: Record<InventoryDocumentKind, string> = {
  opening: 'OS',
  receipt: 'IR',
  issue: 'IO',
  adjustment: 'ADJ',
};

const scanLabel: Record<string, string> = {
  sku: 'SKU',
  primary_barcode: 'باركود أساسي',
  unit_barcode: 'باركود وحدة',
  barcode: 'باركود إضافي',
  alternative_code: 'كود بديل',
};

function idForLine(): string {
  return typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function'
    ? crypto.randomUUID()
    : `${Date.now()}-${Math.random().toString(36).slice(2)}`;
}

function blankLine(): DraftLine {
  return {
    key: idForLine(),
    itemId: '',
    unitId: '',
    quantity: '',
    unitCost: '',
    lotId: '',
    serialIds: [],
    adjustmentDirection: '',
    note: '',
  };
}

function branchOf(warehouse: Warehouse): string | undefined {
  return warehouse.branchId ?? warehouse.branch_id;
}

function baseUnitOf(item: Item | undefined): string | undefined {
  return item?.baseUnitId ?? item?.base_unit_id;
}

function tracksLot(item: Item | undefined): boolean {
  return Boolean(item?.trackLot ?? item?.track_lot);
}

function tracksSerial(item: Item | undefined): boolean {
  return Boolean(item?.trackSerial ?? item?.track_serial);
}

function numeric(value: string): number {
  const result = Number(value);
  return Number.isFinite(result) ? result : 0;
}

/**
 * Shared desktop-style workspace for opening, receipt, issue and adjustment documents.
 * It deliberately keeps the operator's document separate from the immutable inventory
 * ledger: save/edit a draft first, then use the explicit posting action to atomically
 * create the stock movements and journal entry.
 */
export function InventoryDocumentWorkspace({ kind, title, subtitle, newLabel }: WorkspaceProps) {
  const { can } = useSession();
  const branches = useQuery<Branch[]>(() => listBranches(), []);
  const warehouses = useQuery<Warehouse[]>(() => listWarehouses(), []);
  const items = useQuery<Item[]>(() => listItems(), []);
  const accounts = useQuery<Account[]>(() => listAccounts(), []);

  const [statusFilter, setStatusFilter] = useState('');
  const [warehouseFilter, setWarehouseFilter] = useState('');
  const documents = useQuery<InventoryDocument[]>(() => {
    const params = new URLSearchParams({ kind });
    if (statusFilter) params.set('status', statusFilter);
    if (warehouseFilter) params.set('warehouse_id', warehouseFilter);
    return apiList<InventoryDocument>(`/inventory/documents?${params.toString()}`);
  }, [kind, statusFilter, warehouseFilter]);

  const [editorOpen, setEditorOpen] = useState(false);
  const [editingId, setEditingId] = useState<string>();
  const [selected, setSelected] = useState<InventoryDocument>();
  const [branchId, setBranchId] = useState('');
  const [warehouseId, setWarehouseId] = useState('');
  const [documentDate, setDocumentDate] = useState(today());
  const [reason, setReason] = useState('');
  const [counterAccountId, setCounterAccountId] = useState('');
  const [notes, setNotes] = useState('');
  const [lines, setLines] = useState<DraftLine[]>([blankLine()]);
  const [scanCode, setScanCode] = useState('');
  const [unitMap, setUnitMap] = useState<Record<string, ItemUnit[]>>({});
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger' | 'info' | 'warn'; text: string }>();
  const [busy, setBusy] = useState(false);

  const branchRows = branches.data ?? [];
  const warehouseRows = warehouses.data ?? [];
  const itemRows = (items.data ?? []).filter((item) => !item.kind || item.kind === 'stock');
  const postableAccounts = (accounts.data ?? []).filter((account) => postableOf(account));
  const availableWarehouses = useMemo(
    () => warehouseRows.filter((warehouse) => !branchId || !branchOf(warehouse) || branchOf(warehouse) === branchId),
    [warehouseRows, branchId],
  );

  useEffect(() => {
    if (!branchId && branchRows.length) setBranchId(defaultOf(branchRows)?.id ?? '');
  }, [branchId, branchRows]);

  useEffect(() => {
    if (!warehouseId || !availableWarehouses.some((warehouse) => warehouse.id === warehouseId)) {
      setWarehouseId(defaultOf(availableWarehouses)?.id ?? '');
    }
  }, [availableWarehouses, warehouseId]);

  const itemIdsKey = useMemo(
    () => [...new Set(lines.map((line) => line.itemId).filter(Boolean))].sort().join(','),
    [lines],
  );
  useEffect(() => {
    const itemIds = itemIdsKey ? itemIdsKey.split(',') : [];
    if (!itemIds.length) return;
    let active = true;
    Promise.all(
      itemIds.map(async (itemId) => ({
        itemId,
        units: await apiList<ItemUnit>(`/organization/catalog/items/${itemId}/units`),
      })),
    )
      .then((entries) => {
        if (!active) return;
        setUnitMap((current) => ({ ...current, ...Object.fromEntries(entries.map((entry) => [entry.itemId, entry.units])) }));
      })
      .catch(() => {
        // The final save retains server-side validation. A transient unit lookup must not
        // erase the operator's line or turn the whole document view into an error state.
      });
    return () => {
      active = false;
    };
  }, [itemIdsKey]);

  const needsLots = lines.some((line) => tracksLot(itemRows.find((item) => item.id === line.itemId)));
  const needsSerials = lines.some((line) => tracksSerial(itemRows.find((item) => item.id === line.itemId)));
  const lots = useQuery<InventoryLot[]>(() => (needsLots ? apiList<InventoryLot>('/inventory/lots') : Promise.resolve([])), [needsLots]);
  const serials = useQuery<InventorySerial[]>(() => (needsSerials ? apiList<InventorySerial>('/inventory/serials') : Promise.resolve([])), [needsSerials]);

  const incoming = (line: DraftLine): boolean => kind !== 'issue' && (kind !== 'adjustment' || line.adjustmentDirection !== 'out');

  function resetEditor() {
    setEditingId(undefined);
    setDocumentDate(today());
    setReason('');
    setCounterAccountId('');
    setNotes('');
    setLines([blankLine()]);
    setScanCode('');
    setEditorOpen(true);
    setSelected(undefined);
    setNotice(undefined);
  }

  function openDraft(document: InventoryDocument) {
    setEditingId(document.id);
    setBranchId(document.branchId);
    setWarehouseId(document.warehouseId);
    setDocumentDate(document.documentDate);
    setReason(document.reason ?? '');
    setCounterAccountId(document.counterAccountId ?? '');
    setNotes(document.notes ?? '');
    setLines(
      document.lines.map((line) => ({
        key: line.id,
        itemId: line.itemId,
        unitId: line.unitId,
        quantity: line.quantity,
        unitCost: line.unitCost,
        lotId: line.lotId ?? '',
        serialIds: line.serialIds ?? [],
        adjustmentDirection: line.adjustmentDirection ?? '',
        note: line.note ?? '',
      })),
    );
    setEditorOpen(true);
    setSelected(document);
    setNotice(undefined);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  function updateLine(key: string, patch: Partial<DraftLine>) {
    setLines((current) => current.map((line) => (line.key === key ? { ...line, ...patch } : line)));
  }

  function chooseItem(key: string, itemId: string) {
    const item = itemRows.find((candidate) => candidate.id === itemId);
    updateLine(key, {
      itemId,
      unitId: baseUnitOf(item) ?? '',
      lotId: '',
      serialIds: [],
    });
  }

  function unitsFor(line: DraftLine): ItemUnit[] {
    const configured = unitMap[line.itemId];
    if (configured?.length) return configured;
    const item = itemRows.find((candidate) => candidate.id === line.itemId);
    const baseUnit = baseUnitOf(item);
    return baseUnit ? [{ unitId: baseUnit, ratio: '1', code: 'BASE', nameAr: 'الوحدة الأساسية', isBase: true }] : [];
  }

  function ratioFor(line: DraftLine): number {
    return numeric(unitsFor(line).find((unit) => unit.unitId === line.unitId)?.ratio ?? '1');
  }

  function addScannedLine(result: ScannerLookup) {
    const scanned: DraftLine = {
      ...blankLine(),
      itemId: result.item.id,
      unitId: result.unitId,
      quantity: '1',
    };
    setLines((current) => {
      const firstIsEmpty = current.length === 1 && !current[0]?.itemId && !current[0]?.quantity;
      return firstIsEmpty ? [scanned] : [...current, scanned];
    });
  }

  async function scan() {
    const code = scanCode.trim();
    if (!code) return;
    setBusy(true);
    setNotice(undefined);
    try {
      const result = await apiData<ScannerLookup>(`/organization/catalog/items/lookup?code=${encodeURIComponent(code)}`);
      addScannedLine(result);
      setScanCode('');
      setNotice({ kind: 'ok', text: `أُضيفت ${itemLabel(result.item)} بوحدة المسح (${scanLabel[result.matchedAs] ?? result.matchedAs}).` });
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  function payload() {
    const filled = lines.filter((line) => line.itemId || line.quantity || line.unitCost || line.lotId || line.serialIds.length);
    if (!branchId || !warehouseId) {
      throw new ApiError(422, 'INVENTORY_DOCUMENT_TARGET_REQUIRED', 'اختر الفرع والمستودع قبل الحفظ.');
    }
    if (!counterAccountId) {
      throw new ApiError(422, 'INVENTORY_COUNTER_ACCOUNT_REQUIRED', 'اختر الحساب المقابل قبل حفظ المستند.');
    }
    if (!filled.length) {
      throw new ApiError(422, 'INVENTORY_DOCUMENT_LINES_REQUIRED', 'أضف صنفاً واحداً على الأقل إلى المستند.');
    }
    for (const line of filled) {
      if (!line.itemId || numeric(line.quantity) <= 0) {
        throw new ApiError(422, 'INVENTORY_DOCUMENT_QTY_INVALID', 'لكل سطر صنف وكمية موجبة مطلوبة.');
      }
      if (kind === 'adjustment' && !line.adjustmentDirection) {
        throw new ApiError(422, 'INVENTORY_ADJUSTMENT_DIRECTION_REQUIRED', 'حدد إضافة أو إخراجاً لكل سطر تسوية.');
      }
      if (incoming(line) && (line.unitCost.trim() === '' || numeric(line.unitCost) < 0)) {
        throw new ApiError(422, 'INVENTORY_RECEIPT_COST_REQUIRED', 'أدخل تكلفة الوحدة لكل سطر وارد.');
      }
    }
    return {
      kind,
      branchId,
      warehouseId,
      documentDate,
      reason: reason.trim() || undefined,
      counterAccountId,
      notes: notes.trim() || undefined,
      lines: filled.map((line) => ({
        itemId: line.itemId,
        unitId: line.unitId || undefined,
        quantity: line.quantity,
        unitCost: line.unitCost.trim() || undefined,
        lotId: line.lotId || undefined,
        serialIds: line.serialIds,
        adjustmentDirection: kind === 'adjustment' ? line.adjustmentDirection || undefined : undefined,
        note: line.note.trim() || undefined,
      })),
    };
  }

  async function save(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setNotice(undefined);
    try {
      const body = payload();
      const document = editingId
        ? await apiPatch<InventoryDocument>(`/inventory/documents/${editingId}`, body)
        : await apiPost<InventoryDocument>('/inventory/documents', body);
      setSelected(document);
      setEditorOpen(false);
      setNotice({
        kind: 'ok',
        text: editingId ? `تم تحديث ${KIND_LABEL[kind]} ${document.number} كمسودة.` : `تم حفظ ${KIND_LABEL[kind]} ${document.number} كمسودة. راجعه ثم رحّله.`,
      });
      documents.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  async function action(
    document: InventoryDocument,
    verb: 'post' | 'cancel' | 'void',
  ) {
    const prompts: Record<typeof verb, string> = {
      post: `ترحيل ${document.number} سينشئ حركة مخزون وقيداً محاسبياً ولا يمكن تعديله بعد ذلك. هل تريد المتابعة؟`,
      cancel: `إلغاء المسودة ${document.number}؟ لن تُنشأ أي حركة مخزون أو قيد.`,
      void: `عكس ${document.number}؟ سيتم إنشاء حركة وقيد عكسيين قابلين للتدقيق.`,
    };
    if (!window.confirm(prompts[verb])) return;
    let reasonForVoid: string | undefined;
    if (verb === 'void') {
      reasonForVoid = window.prompt('سبب الإلغاء والعكس *')?.trim();
      if (!reasonForVoid) {
        setNotice({ kind: 'warn', text: 'لم يتم العكس: سبب الإلغاء مطلوب للتدقيق.' });
        return;
      }
    }
    setBusy(true);
    setNotice(undefined);
    try {
      const next = await apiPost<InventoryDocument>(`/inventory/documents/${document.id}/${verb}`, verb === 'void' ? { reason: reasonForVoid } : {});
      setSelected(next);
      setEditorOpen(false);
      setNotice({
        kind: 'ok',
        text: verb === 'post' ? `تم ترحيل ${next.number} مع الحركة والقيد.` : verb === 'void' ? `تم عكس ${next.number} بحركة وقيد عكسيين.` : `تم إلغاء المسودة ${next.number}.`,
      });
      documents.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  const warehouseName = (id: string) => {
    const warehouse = warehouseRows.find((row) => row.id === id);
    return warehouse ? arabicName(warehouse) : id;
  };
  const branchName = (id: string) => {
    const branch = branchRows.find((row) => row.id === id);
    return branch ? arabicName(branch) : id;
  };
  const itemName = (id: string) => {
    const item = itemRows.find((row) => row.id === id);
    return item ? itemLabel(item) : id;
  };
  const unitName = (itemId: string, unitId: string) => {
    const unit = (unitMap[itemId] ?? []).find((row) => row.unitId === unitId);
    return unit ? `${unit.code ? `${unit.code} — ` : ''}${unit.nameAr ?? unit.nameEn ?? 'وحدة'}` : 'الوحدة الأساسية';
  };

  if (!can('inventory.view')) return <Forbidden />;

  return (
    <Screen
      title={title}
      subtitle={subtitle}
      crumbs={['المستودعات', 'العمليات']}
      actions={
        <>
          {selected ? (
            <Link className="btn" href={`/print/inventory-document/${selected.id}`}>
              طباعة المستند
            </Link>
          ) : (
            <button className="btn" type="button" disabled>
              طباعة المستند
            </button>
          )}
          {can('inventory.adjust') && (
            <button className="btn primary" type="button" onClick={resetEditor} disabled={busy}>
              {newLabel}
            </button>
          )}
        </>
      }
    >
      <div className="card tight no-print">
        <div className="row" style={{ alignItems: 'center', justifyContent: 'space-between' }}>
          <div>
            <strong>سير المستند</strong>
            <p className="muted small" style={{ margin: '4px 0 0' }}>
              مسودة قابلة للتعديل ← ترحيل ذري للمخزون والقيد ← عكس موثق عند الحاجة
            </p>
          </div>
          <span className="badge draft">{KIND_PREFIX[kind]} — {KIND_LABEL[kind]}</span>
        </div>
      </div>

      {editorOpen && (
        <form className="card form" onSubmit={save}>
          <div className="section-title">
            <div>
              <h2>{editingId ? `تعديل مسودة ${KIND_LABEL[kind]}` : `مسودة ${KIND_LABEL[kind]} جديدة`}</h2>
              <p className="muted small" style={{ margin: 0 }}>
                {kind === 'issue'
                  ? 'تكلفة الإخراج تُحسب من متوسط تكلفة المخزون عند الترحيل.'
                  : 'التكلفة المدخلة هي تكلفة وحدة الإدخال؛ يحولها النظام تلقائياً إلى تكلفة الوحدة الأساسية.'}
              </p>
            </div>
            <button className="btn" type="button" onClick={() => setEditorOpen(false)} disabled={busy}>إغلاق</button>
          </div>

          <div className="form-grid">
            <label className="field">
              <span>الفرع *</span>
              <select className="input" value={branchId} onChange={(event) => setBranchId(event.target.value)} required>
                <option value="">— اختر —</option>
                {branchRows.map((branch) => <option key={branch.id} value={branch.id}>{branch.code ? `${branch.code} — ` : ''}{arabicName(branch)}</option>)}
              </select>
            </label>
            <label className="field">
              <span>المستودع *</span>
              <select className="input" value={warehouseId} onChange={(event) => setWarehouseId(event.target.value)} required>
                <option value="">— اختر —</option>
                {availableWarehouses.map((warehouse) => <option key={warehouse.id} value={warehouse.id}>{warehouse.code ? `${warehouse.code} — ` : ''}{arabicName(warehouse)}</option>)}
              </select>
            </label>
            <label className="field">
              <span>تاريخ المستند *</span>
              <input className="input" dir="ltr" type="date" value={documentDate} onChange={(event) => setDocumentDate(event.target.value)} required />
            </label>
            <label className="field">
              <span>الحساب المقابل *</span>
              <select className="input" value={counterAccountId} onChange={(event) => setCounterAccountId(event.target.value)} required>
                <option value="">— اختر حساباً قابلاً للترحيل —</option>
                {postableAccounts.map((account) => <option key={account.id} value={account.id}>{accountLabel(account)}</option>)}
              </select>
              <span className="muted small">يختار النظام حساب المخزون من ملف ترحيل الفرع ولا يقبل إدخاله من المتصفح.</span>
            </label>
            <label className="field">
              <span>السبب / المرجع</span>
              <input className="input" value={reason} onChange={(event) => setReason(event.target.value)} placeholder="مثال: جرد أول المدة أو إذن استلام" />
            </label>
            <label className="field">
              <span>ملاحظات داخلية</span>
              <input className="input" value={notes} onChange={(event) => setNotes(event.target.value)} />
            </label>
          </div>

          <div className="card tight" style={{ background: '#f8fafc' }}>
            <div className="row" style={{ alignItems: 'end' }}>
              <label className="field" style={{ flex: '1 1 260px', marginBottom: 0 }}>
                <span>مسح باركود / SKU / كود بديل</span>
                <input
                  className="input"
                  dir="ltr"
                  value={scanCode}
                  onChange={(event) => setScanCode(event.target.value)}
                  onKeyDown={(event) => {
                    if (event.key === 'Enter') {
                      event.preventDefault();
                      void scan();
                    }
                  }}
                  placeholder="امسح الرمز ثم Enter"
                />
              </label>
              <button className="btn primary" type="button" onClick={() => void scan()} disabled={busy || !scanCode.trim()}>
                إضافة بالمسح
              </button>
            </div>
          </div>

          <div>
            <div className="section-title" style={{ marginBottom: 8 }}>
              <div>
                <h3>أسطر الأصناف</h3>
                <p className="muted small" style={{ margin: 0 }}>الوحدة والدفعة والسيريال المثبتة هنا تنتقل إلى حركة المخزون ولا يمكن تغييرها بعد الترحيل.</p>
              </div>
              <button className="btn sm" type="button" onClick={() => setLines((current) => [...current, blankLine()])} disabled={busy}>+ سطر</button>
            </div>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>الصنف</th>
                    <th>الوحدة</th>
                    {kind === 'adjustment' && <th>الأثر</th>}
                    <th className="num">الكمية</th>
                    <th className="num">الأساس</th>
                    <th className="num">تكلفة وحدة الإدخال{kind === 'issue' ? '' : ' *'}</th>
                    <th>دفعة / سيريال</th>
                    <th>ملاحظة</th>
                    <th className="no-print" />
                  </tr>
                </thead>
                <tbody>
                  {lines.map((line) => {
                    const item = itemRows.find((row) => row.id === line.itemId);
                    const lineUnits = unitsFor(line);
                    const isIncoming = incoming(line);
                    const selectedRatio = ratioFor(line);
                    const matchingLots = (lots.data ?? []).filter((lot) => lot.itemId === line.itemId);
                    const allowedSerials = (serials.data ?? []).filter((serial) => {
                      if (serial.itemId !== line.itemId) return false;
                      if (isIncoming) return serial.status === 'pending_receipt';
                      return ['available', 'reserved'].includes(serial.status) && serial.warehouseId === warehouseId;
                    });
                    return (
                      <tr key={line.key}>
                        <td style={{ minWidth: 220 }}>
                          <select className="input" value={line.itemId} onChange={(event) => chooseItem(line.key, event.target.value)} disabled={busy}>
                            <option value="">— اختر مادة مخزنية —</option>
                            {itemRows.map((row) => <option key={row.id} value={row.id}>{itemLabel(row)}</option>)}
                          </select>
                        </td>
                        <td style={{ minWidth: 150 }}>
                          <select className="input" value={line.unitId} onChange={(event) => updateLine(line.key, { unitId: event.target.value })} disabled={busy || !line.itemId}>
                            <option value="">— الأساسية —</option>
                            {lineUnits.map((unit) => <option key={unit.unitId} value={unit.unitId}>{unit.code ? `${unit.code} — ` : ''}{unit.nameAr ?? unit.nameEn ?? 'وحدة'} × {quantity(unit.ratio)}</option>)}
                          </select>
                        </td>
                        {kind === 'adjustment' && (
                          <td style={{ minWidth: 120 }}>
                            <select className="input" value={line.adjustmentDirection} onChange={(event) => updateLine(line.key, { adjustmentDirection: event.target.value as '' | 'in' | 'out' })} disabled={busy}>
                              <option value="">— اختر —</option>
                              <option value="in">زيادة</option>
                              <option value="out">نقص</option>
                            </select>
                          </td>
                        )}
                        <td className="num" style={{ minWidth: 100 }}>
                          <input className="input" dir="ltr" inputMode="decimal" value={line.quantity} onChange={(event) => updateLine(line.key, { quantity: event.target.value })} disabled={busy} />
                        </td>
                        <td className="num">{line.quantity ? quantity(numeric(line.quantity) * selectedRatio) : '—'}</td>
                        <td className="num" style={{ minWidth: 130 }}>
                          <input
                            className="input"
                            dir="ltr"
                            inputMode="decimal"
                            value={line.unitCost}
                            onChange={(event) => updateLine(line.key, { unitCost: event.target.value })}
                            placeholder={isIncoming ? '0.00' : 'متوسط تلقائي'}
                            required={isIncoming && Boolean(line.itemId)}
                            disabled={busy}
                          />
                        </td>
                        <td style={{ minWidth: 180 }}>
                          {tracksLot(item) && (
                            <select className="input" value={line.lotId} onChange={(event) => updateLine(line.key, { lotId: event.target.value })} disabled={busy}>
                              <option value="">— الدفعة مطلوبة —</option>
                              {matchingLots.map((lot) => <option key={lot.id} value={lot.id}>{lot.lotNo}{lot.expiryDate ? ` — ${lot.expiryDate}` : ''}{lot.onHand ? ` (${quantity(lot.onHand)})` : ''}</option>)}
                            </select>
                          )}
                          {tracksSerial(item) && (
                            <select
                              className="input"
                              multiple
                              value={line.serialIds}
                              onChange={(event) => updateLine(line.key, { serialIds: Array.from(event.currentTarget.selectedOptions, (option) => option.value) })}
                              disabled={busy}
                              aria-label="الأرقام التسلسلية"
                            >
                              {allowedSerials.map((serial) => <option key={serial.id} value={serial.id}>{serial.serialNo}</option>)}
                            </select>
                          )}
                          {!tracksLot(item) && !tracksSerial(item) && <span className="muted small">لا تتبع</span>}
                        </td>
                        <td style={{ minWidth: 130 }}>
                          <input className="input" value={line.note} onChange={(event) => updateLine(line.key, { note: event.target.value })} disabled={busy} />
                        </td>
                        <td className="no-print">
                          <button className="btn sm danger" type="button" disabled={busy || lines.length === 1} onClick={() => setLines((current) => current.filter((candidate) => candidate.key !== line.key))}>حذف</button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </div>

          <Notice notice={notice} />
          <div className="row">
            <button className="btn primary" type="submit" disabled={busy || !can('inventory.adjust')}>
              {busy ? 'جارٍ الحفظ…' : editingId ? 'حفظ تعديل المسودة' : 'حفظ كمسودة'}
            </button>
            <button className="btn" type="button" onClick={() => setEditorOpen(false)} disabled={busy}>إلغاء</button>
          </div>
        </form>
      )}

      {!editorOpen && <Notice notice={notice} />}

      {selected && (
        <section className="card" aria-label="تفاصيل المستند المحدد">
          <div className="section-title">
            <div>
              <div className="row" style={{ alignItems: 'center' }}>
                <h2 dir="ltr">{selected.number}</h2>
                <span className={`badge ${selected.status}`}>{statusLabel(selected.status)}</span>
              </div>
              <p className="muted small" style={{ margin: '5px 0 0' }}>
                {KIND_LABEL[selected.kind]} · {branchName(selected.branchId)} · {warehouseName(selected.warehouseId)} · {shortDate(selected.documentDate)}
              </p>
            </div>
            <div className="toolbar no-print">
              {selected.status === 'draft' && can('inventory.adjust') && <button className="btn" type="button" onClick={() => openDraft(selected)} disabled={busy}>تعديل المسودة</button>}
              {selected.status === 'draft' && can('inventory.adjust.approve') && <button className="btn primary" type="button" onClick={() => void action(selected, 'post')} disabled={busy}>ترحيل</button>}
              {selected.status === 'draft' && can('inventory.adjust') && <button className="btn danger" type="button" onClick={() => void action(selected, 'cancel')} disabled={busy}>إلغاء المسودة</button>}
              {selected.status === 'posted' && can('inventory.adjust.approve') && <button className="btn danger" type="button" onClick={() => void action(selected, 'void')} disabled={busy}>عكس / إلغاء</button>}
            </div>
          </div>
          {(selected.reason || selected.notes || selected.journalEntryId) && (
            <dl className="kv" style={{ marginTop: 12 }}>
              {selected.reason && <><dt>السبب / المرجع</dt><dd>{selected.reason}</dd></>}
              {selected.notes && <><dt>ملاحظات</dt><dd>{selected.notes}</dd></>}
              {selected.journalEntryId && <><dt>القيد الناتج</dt><dd dir="ltr">{selected.journalEntryId}</dd></>}
            </dl>
          )}
          <DataTable
            rows={selected.lines}
            rowKey={(line) => line.id}
            columns={[
              { key: 'line', header: '#', align: 'num', cell: (line) => line.lineNo },
              { key: 'item', header: 'المادة', cell: (line) => itemName(line.itemId) },
              { key: 'unit', header: 'الوحدة', cell: (line) => unitName(line.itemId, line.unitId) },
              { key: 'direction', header: 'الأثر', cell: (line) => line.adjustmentDirection === 'in' ? 'زيادة' : line.adjustmentDirection === 'out' ? 'نقص' : selected.kind === 'issue' ? 'إخراج' : 'إدخال' },
              { key: 'qty', header: 'الكمية', align: 'num', cell: (line) => quantity(line.quantity) },
              { key: 'base', header: 'الكمية الأساسية', align: 'num', cell: (line) => quantity(line.baseQuantity) },
              { key: 'cost', header: 'تكلفة الوحدة', align: 'num', cell: (line) => money(line.unitCost) },
              { key: 'note', header: 'ملاحظة', cell: (line) => line.note ?? '—' },
            ]}
          />
        </section>
      )}

      <section className="card no-print">
        <div className="section-title">
          <div>
            <h2>سجل {KIND_LABEL[kind]}</h2>
            <p className="muted small" style={{ margin: 0 }}>ابحث بالفرع/المستودع والحالة، ثم افتح المستند لمراجعته أو متابعة سيره.</p>
          </div>
          <div className="row">
            <select className="input" value={warehouseFilter} onChange={(event) => setWarehouseFilter(event.target.value)} style={{ minWidth: 190 }}>
              <option value="">كل المستودعات</option>
              {warehouseRows.map((warehouse) => <option key={warehouse.id} value={warehouse.id}>{arabicName(warehouse)}</option>)}
            </select>
            <select className="input" value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)} style={{ minWidth: 145 }}>
              <option value="">كل الحالات</option>
              <option value="draft">مسودة</option>
              <option value="posted">مرحّل</option>
              <option value="voided">ملغي بعكس</option>
              <option value="cancelled">ملغي</option>
            </select>
          </div>
        </div>
      </section>

      <QueryView query={documents} empty={`لا توجد ${KIND_LABEL[kind]} حتى الآن`} emptyDetail="ابدأ بمسودة جديدة؛ لن تتأثر الأرصدة قبل الترحيل.">
        {(rows) => (
          <DataTable
            rows={rows}
            rowKey={(document) => document.id}
            columns={[
              { key: 'number', header: 'الرقم', align: 'ltr', cell: (document) => document.number },
              { key: 'date', header: 'التاريخ', align: 'ltr', cell: (document) => shortDate(document.documentDate) },
              { key: 'branch', header: 'الفرع', cell: (document) => branchName(document.branchId) },
              { key: 'warehouse', header: 'المستودع', cell: (document) => warehouseName(document.warehouseId) },
              { key: 'lines', header: 'الأسطر', align: 'num', cell: (document) => document.lines.length },
              { key: 'status', header: 'الحالة', cell: (document) => <span className={`badge ${document.status}`}>{statusLabel(document.status)}</span> },
              {
                key: 'actions',
                header: '',
                cell: (document) => (
                  <div className="row">
                    <button className="btn sm" type="button" onClick={() => setSelected(document)}>عرض</button>
                    {document.status === 'draft' && can('inventory.adjust') && <button className="btn sm" type="button" onClick={() => openDraft(document)}>تعديل</button>}
                    {document.status === 'draft' && can('inventory.adjust.approve') && <button className="btn sm primary" type="button" onClick={() => void action(document, 'post')} disabled={busy}>ترحيل</button>}
                  </div>
                ),
              },
            ]}
          />
        )}
      </QueryView>

    </Screen>
  );
}
