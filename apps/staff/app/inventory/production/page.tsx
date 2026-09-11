'use client';

import { useState } from 'react';

import { DataTable, Notice, QueryView } from '../../../components/data-view';
import { Screen } from '../../../components/screen';
import { DocField, DocHead, StatTile, StatTiles, StatusTrack, Totals } from '../../../components/ui';
import { ApiError, apiList, apiPost } from '../../../lib/api';
import {
  arabicName,
  itemLabel,
  listItems,
  listWarehouses,
  money,
  quantity,
  shortDate,
  statusLabel,
  today,
  type Item,
  type Warehouse,
} from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

type Component = { lineNo: number; itemId: string; qty: string; unitCost: string; lineCost: string };
type ProductionOrder = {
  id: string;
  number: string;
  warehouseId: string;
  orderDate: string;
  status: string;
  outputItemId: string;
  outputQty: string;
  componentCost: string;
  unitCost: string;
  notes: string | null;
  components: Component[];
};

type ComponentDraft = { itemId: string; qty: string };

const STATUS_LABELS: Record<string, string> = { draft: 'مسودة', completed: 'منفَّذ', cancelled: 'ملغي' };
const emptyComponent = (): ComponentDraft => ({ itemId: '', qty: '' });

/**
 * أمر الإنتاج وتقريره. Cost is never typed here: components leave at the warehouse's
 * moving average and the finished item is valued at exactly what left, so the screen can
 * only show the result after the order runs.
 */
export default function ProductionOrdersPage() {
  const { can } = useSession();
  const warehouses = useQuery<Warehouse[]>(() => listWarehouses(), []);
  const items = useQuery<Item[]>(() => listItems(), []);
  const [statusFilter, setStatusFilter] = useState('');
  const orders = useQuery<ProductionOrder[]>(() => apiList<ProductionOrder>(`/inventory/production-orders${statusFilter ? `?status=${statusFilter}` : ''}`), [statusFilter]);

  const [warehouseId, setWarehouseId] = useState('');
  const [orderDate, setOrderDate] = useState(today());
  const [outputItemId, setOutputItemId] = useState('');
  const [outputQty, setOutputQty] = useState('1');
  const [notes, setNotes] = useState('');
  const [components, setComponents] = useState<ComponentDraft[]>([emptyComponent(), emptyComponent(), emptyComponent()]);
  const [expanded, setExpanded] = useState('');
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger' | 'info'; text: string } | undefined>();

  const itemName = (id: string) => {
    const item = (items.data ?? []).find((row) => row.id === id);
    return item ? itemLabel(item) : id;
  };
  const warehouseName = (id: string) => {
    const warehouse = (warehouses.data ?? []).find((row) => row.id === id);
    return warehouse ? arabicName(warehouse) : id;
  };
  const filled = components.filter((component) => component.itemId !== '' && component.qty.trim() !== '');

  function setComponent(index: number, patch: Partial<ComponentDraft>) {
    setComponents((current) => current.map((component, position) => (position === index ? { ...component, ...patch } : component)));
  }

  async function create() {
    setBusy(true);
    setNotice(undefined);
    try {
      if (!warehouseId) throw new ApiError(422, 'VALIDATION_FAILED', 'اختر المستودع.');
      if (!outputItemId) throw new ApiError(422, 'VALIDATION_FAILED', 'اختر المنتج الناتج.');
      if (filled.length === 0) throw new ApiError(422, 'VALIDATION_FAILED', 'أضف مكوناً واحداً على الأقل.');
      const created = await apiPost<ProductionOrder>('/inventory/production-orders', {
        warehouseId,
        orderDate,
        outputItemId,
        outputQty,
        notes: notes.trim() || undefined,
        components: filled.map((component) => ({ itemId: component.itemId, qty: component.qty })),
      });
      setNotice({ kind: 'ok', text: `تم إنشاء أمر الإنتاج ${created.number} كمسودة — لم يتحرك المخزون بعد.` });
      setComponents([emptyComponent(), emptyComponent(), emptyComponent()]);
      setNotes('');
      orders.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  async function act(order: ProductionOrder, action: 'complete' | 'cancel') {
    setBusy(true);
    setNotice(undefined);
    try {
      const result = await apiPost<ProductionOrder>(`/inventory/production-orders/${order.id}/${action}`, {});
      setNotice({
        kind: 'ok',
        text: action === 'complete'
          ? `نُفِّذ الأمر ${order.number}: استُهلكت مكونات بقيمة ${money(result.componentCost)}، ودخل المنتج بتكلفة ${money(result.unitCost)} للوحدة.`
          : `تم إلغاء الأمر ${order.number}.`,
      });
      orders.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  const open = (orders.data ?? []).find((row) => row.id === expanded) ?? null;
  const orderRows = orders.data ?? [];
  const completed = orderRows.filter((row) => row.status === 'completed');
  const drafts = orderRows.filter((row) => row.status === 'draft');
  const producedCost = completed.reduce((sum, row) => sum + Number(row.componentCost), 0);
  const producedQty = completed.reduce((sum, row) => sum + Number(row.outputQty), 0);
  const componentsCost = orderRows.reduce((sum, row) => sum + Number(row.componentCost), 0);

  return (
    <Screen
      title="أوامر الإنتاج"
      subtitle="تحويل مكونات إلى منتج: تخرج المكونات من المستودع بمتوسط تكلفتها ويدخل المنتج بمجموع ما خرج مقسوماً على الكمية المنتجة. الأمر محايد محاسبياً فلا ينشأ عنه قيد."
      crumbs={['المستودعات', 'تقارير مستودعية']}
      actions={
        <a className="btn" href="/reports/production-orders">
          تقرير أوامر الإنتاج
        </a>
      }
    >
      {notice && <Notice notice={notice} />}

      <StatTiles>
        <StatTile label="أوامر الإنتاج" value={orderRows.length} hint="مسودة ومكتملة" tone="brand" />
        <StatTile label="مسودات" value={drafts.length} hint="تنتظر التنفيذ" tone={drafts.length > 0 ? 'warn' : 'ok'} />
        <StatTile label="أوامر مكتملة" value={completed.length} hint="نُفّذت وحرّكت المخزون" tone="ok" />
        <StatTile label="الكمية المنتجة" value={quantity(producedQty)} hint="من الأوامر المكتملة" />
        <StatTile label="تكلفة المكونات" value={money(componentsCost)} hint="ما خرج من المستودع" />
      </StatTiles>

      {can('inventory.production.manage') && (
        <div className="card">
          <h3>أمر إنتاج جديد</h3>
          <div className="form-grid">
            <label className="field">
              <span>المستودع</span>
              <select className="input" value={warehouseId} onChange={(event) => setWarehouseId(event.target.value)}>
                <option value="">— اختر —</option>
                {(warehouses.data ?? []).map((row) => (
                  <option key={row.id} value={row.id}>{arabicName(row)}</option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>التاريخ</span>
              <input className="input" type="date" value={orderDate} onChange={(event) => setOrderDate(event.target.value)} />
            </label>
            <label className="field wide">
              <span>المنتج الناتج</span>
              <select className="input" value={outputItemId} onChange={(event) => setOutputItemId(event.target.value)}>
                <option value="">— اختر —</option>
                {(items.data ?? []).map((row) => (
                  <option key={row.id} value={row.id}>{itemLabel(row)}</option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>الكمية المنتجة</span>
              <input className="input" value={outputQty} onChange={(event) => setOutputQty(event.target.value)} inputMode="decimal" />
            </label>
            <label className="field wide">
              <span>ملاحظات</span>
              <input className="input" value={notes} onChange={(event) => setNotes(event.target.value)} />
            </label>
          </div>

          <h4>المكونات</h4>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>الصنف</th>
                  <th>الكمية المستهلكة</th>
                </tr>
              </thead>
              <tbody>
                {components.map((component, index) => (
                  <tr key={index}>
                    <td>
                      <select className="input" value={component.itemId} onChange={(event) => setComponent(index, { itemId: event.target.value })}>
                        <option value="">— اختر —</option>
                        {(items.data ?? []).map((row) => (
                          <option key={row.id} value={row.id}>{itemLabel(row)}</option>
                        ))}
                      </select>
                    </td>
                    <td>
                      <input className="input" value={component.qty} onChange={(event) => setComponent(index, { qty: event.target.value })} inputMode="decimal" />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="toolbar">
            <button className="btn sm" type="button" onClick={() => setComponents((current) => [...current, emptyComponent()])}>
              إضافة مكوّن
            </button>
            <span className="chip">{`عدد المكونات: ${filled.length}`}</span>
            <button className="btn primary" type="button" onClick={create} disabled={busy}>
              {busy ? 'جارٍ الحفظ…' : 'حفظ الأمر'}
            </button>
          </div>
          <p className="muted">التكلفة تُحسب لحظة التنفيذ من أرصدة المستودع، ولا تُدخَل يدوياً.</p>
        </div>
      )}

      <div className="card">
        <div className="toolbar">
          <label className="field">
            <span>الحالة</span>
            <select className="input" value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
              <option value="">الكل</option>
              <option value="draft">مسودة</option>
              <option value="completed">منفَّذ</option>
              <option value="cancelled">ملغي</option>
            </select>
          </label>
          <span className="chip">{`أوامر منفَّذة: ${completed.length}`}</span>
          <span className="chip">{`تكلفة الإنتاج: ${money(producedCost)}`}</span>
        </div>
        <QueryView query={orders} empty="لا توجد أوامر إنتاج" emptyDetail="أنشئ أمراً من النموذج أعلاه.">
          {(rows) => (
            <DataTable
              rows={rows}
              rowKey={(row) => row.id}
              columns={[
                { key: 'number', header: 'الرقم', align: 'ltr', cell: (row) => (
                  <button className="btn sm" type="button" onClick={() => setExpanded(row.id === expanded ? '' : row.id)}>{row.number}</button>
                ) },
                { key: 'date', header: 'التاريخ', align: 'ltr', cell: (row) => shortDate(row.orderDate) },
                { key: 'output', header: 'المنتج', cell: (row) => itemName(row.outputItemId) },
                { key: 'qty', header: 'الكمية', align: 'num', cell: (row) => quantity(row.outputQty) },
                { key: 'warehouse', header: 'المستودع', cell: (row) => warehouseName(row.warehouseId) },
                { key: 'cost', header: 'تكلفة المكونات', align: 'num', cell: (row) => money(row.componentCost) },
                { key: 'unit', header: 'تكلفة الوحدة', align: 'num', cell: (row) => money(row.unitCost) },
                { key: 'status', header: 'الحالة', cell: (row) => <span className="badge">{STATUS_LABELS[row.status] ?? statusLabel(row.status)}</span> },
                {
                  key: 'actions',
                  header: '',
                  cell: (row) => (
                    <div className="row">
                      {row.status === 'draft' && can('inventory.production.complete') && (
                        <button className="btn sm primary" type="button" disabled={busy} onClick={() => act(row, 'complete')}>
                          تنفيذ
                        </button>
                      )}
                      {row.status === 'draft' && can('inventory.production.manage') && (
                        <button className="btn sm danger" type="button" disabled={busy} onClick={() => act(row, 'cancel')}>
                          إلغاء
                        </button>
                      )}
                    </div>
                  ),
                },
              ]}
            />
          )}
        </QueryView>
      </div>

      {open && (
        <div className="card">
          <div className="section-title">
            <h3 dir="ltr">{open.number}</h3>
            <span className="badge">{STATUS_LABELS[open.status] ?? statusLabel(open.status)}</span>
          </div>

          <StatusTrack
            steps={['مسودة', 'مُنفَّذ', 'مُلغى']}
            current={open.status === 'draft' ? 0 : open.status === 'completed' ? 1 : 2}
            cancelled={open.status === 'cancelled'}
          />

          <DocHead>
            <DocField label="الرقم">
              <span dir="ltr">{open.number}</span>
            </DocField>
            <DocField label="التاريخ">{shortDate(open.orderDate)}</DocField>
            <DocField label="المنتج">{itemName(open.outputItemId)}</DocField>
            <DocField label="الكمية المنتجة">
              <span dir="ltr">{quantity(open.outputQty)}</span>
            </DocField>
            <DocField label="المستودع">{warehouseName(open.warehouseId)}</DocField>
            <DocField label="تكلفة الوحدة">
              <span dir="ltr">{money(open.unitCost)}</span>
            </DocField>
            <DocField label="البيان">{open.notes ?? '—'}</DocField>
          </DocHead>

          <DataTable
            rows={open.components}
            rowKey={(row) => String(row.lineNo)}
            footer={[
              <>المجموع</>,
              '',
              '',
              quantity(open.components.reduce((sum, row) => sum + Number(row.qty), 0)),
              '',
              money(open.components.reduce((sum, row) => sum + Number(row.lineCost), 0)),
            ]}
            columns={[
              { key: 'no', header: '#', align: 'num', cell: (row) => row.lineNo },
              {
                key: 'sku',
                header: 'رمز الصنف',
                align: 'ltr',
                cell: (row) => (items.data ?? []).find((item) => item.id === row.itemId)?.sku ?? '—',
              },
              { key: 'item', header: 'الصنف', cell: (row) => itemName(row.itemId) },
              {
                key: 'qty',
                header: 'الكمية',
                align: 'num',
                cell: (row) => quantity(row.qty),
              },
              { key: 'unit', header: 'سعر الوحدة', align: 'num', cell: (row) => money(row.unitCost) },
              { key: 'cost', header: 'المجموع', align: 'num', cell: (row) => money(row.lineCost) },
            ]}
          />

          <Totals
            items={[
              {
                label: 'إجمالي كمية المكونات',
                value: quantity(open.components.reduce((sum, row) => sum + Number(row.qty), 0)),
              },
              {
                label: 'إجمالي تكلفة المكونات',
                value: money(open.components.reduce((sum, row) => sum + Number(row.lineCost), 0)),
              },
              { label: 'تكلفة الوحدة المنتجة', value: money(open.unitCost) },
            ]}
          />

          {open.status === 'draft' && <p className="muted">التكاليف تظهر أصفاراً حتى يُنفَّذ الأمر.</p>}
        </div>
      )}
    </Screen>
  );
}
