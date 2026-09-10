'use client';

import { useState } from 'react';

import { DataTable, Notice, QueryView } from '../../../components/data-view';
import { FormFields, type FormValues } from '../../../components/directory';
import { Screen } from '../../../components/screen';
import { ApiError, apiList, apiPost } from '../../../lib/api';
import { itemLabel, listItems, money, quantity, type Item } from '../../../lib/lookups';
import { useQuery } from '../../../lib/use-query';

type PriceList = { id: string; name: string; currencyCode: string; isDefault: boolean; isActive: boolean };
type PriceListItem = { id: string; itemId: string | null; unitPrice: string; minQty: string | null };

/**
 * Price lists.
 *
 * The list header carries the currency; the rows carry the price per item, with an
 * optional quantity break so wholesale tiers live in the same list.
 */
export default function PriceListsPage() {
  const lists = useQuery<PriceList[]>(() => apiList<PriceList>('/price-lists?limit=100'), []);
  const items = useQuery<Item[]>(() => listItems(), []);
  const [selected, setSelected] = useState<string | null>(null);
  const rows = useQuery<PriceListItem[]>(async () => (selected ? apiList<PriceListItem>(`/price-lists/${selected}/items?limit=200`) : []), [selected]);

  const [listForm, setListForm] = useState<FormValues>({ name: '', currencyCode: 'SAR', isDefault: false, isActive: true });
  const [rowForm, setRowForm] = useState<FormValues>({ itemId: '', unitPrice: '', minQty: '' });
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger' | 'info'; text: string } | undefined>();

  async function run(action: () => Promise<unknown>, text: string) {
    setBusy(true);
    setNotice(undefined);
    try {
      await action();
      setNotice({ kind: 'ok', text });
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  const currentList = (lists.data ?? []).find((row) => row.id === selected);

  return (
    <Screen title="مزامنة الأسعار" subtitle="قوائم الأسعار المعتمدة وأسعار الأصناف داخل كل قائمة." crumbs={['الإعدادات', 'المزامنة']}>
      <div className="grid cols-2">
        <form
          className="card"
          onSubmit={(event) => {
            event.preventDefault();
            void run(
              () =>
                apiPost('/price-lists', {
                  name: String(listForm.name),
                  currencyCode: String(listForm.currencyCode || 'SAR').toUpperCase(),
                  isDefault: Boolean(listForm.isDefault),
                  isActive: Boolean(listForm.isActive),
                }).then(() => {
                  setListForm({ name: '', currencyCode: 'SAR', isDefault: false, isActive: true });
                  lists.reload();
                }),
              'تم إنشاء القائمة.',
            );
          }}
        >
          <h3>قائمة أسعار جديدة</h3>
          <FormFields
            fields={[
              { name: 'name', label: 'اسم القائمة', required: true },
              { name: 'currencyCode', label: 'العملة', required: true, ltr: true, placeholder: 'SAR' },
              { name: 'isDefault', label: 'القائمة الافتراضية', type: 'checkbox' },
              { name: 'isActive', label: 'مفعّلة', type: 'checkbox' },
            ]}
            values={listForm}
            onChange={setListForm}
          />
          <button className="btn primary" type="submit" disabled={busy}>
            حفظ القائمة
          </button>
        </form>

        <div className="card">
          <h3>القوائم</h3>
          <QueryView query={lists} empty="لا توجد قوائم أسعار" emptyDetail="أنشئ قائمة واحدة على الأقل لتسعير الأصناف.">
            {(data) => (
              <DataTable
                columns={[
                  { key: 'name', header: 'القائمة', cell: (row: PriceList) => row.name },
                  { key: 'currency', header: 'العملة', align: 'ltr', cell: (row: PriceList) => row.currencyCode },
                  { key: 'flags', header: 'الحالة', cell: (row: PriceList) => `${row.isActive ? 'مفعّلة' : 'موقوفة'}${row.isDefault ? ' • افتراضية' : ''}` },
                  {
                    key: 'actions',
                    header: '',
                    cell: (row: PriceList) => (
                      <button type="button" className="btn sm" onClick={() => setSelected(row.id)}>
                        الأسعار
                      </button>
                    ),
                  },
                ]}
                rows={data}
                rowKey={(row) => row.id}
              />
            )}
          </QueryView>
        </div>
      </div>

      <Notice notice={notice} />

      {currentList ? (
        <div className="card">
          <h3>أسعار «{currentList.name}»</h3>
          <form
            className="toolbar"
            onSubmit={(event) => {
              event.preventDefault();
              void run(
                () =>
                  apiPost(`/price-lists/${currentList.id}/items`, {
                    itemId: String(rowForm.itemId),
                    unitPrice: String(rowForm.unitPrice),
                    minQty: rowForm.minQty ? String(rowForm.minQty) : undefined,
                  }).then(() => {
                    setRowForm({ itemId: '', unitPrice: '', minQty: '' });
                    rows.reload();
                  }),
                'تم حفظ السعر.',
              );
            }}
          >
            <label className="field">
              <span>الصنف</span>
              <select className="input" required value={String(rowForm.itemId)} onChange={(event) => setRowForm({ ...rowForm, itemId: event.target.value })}>
                <option value="">—</option>
                {(items.data ?? []).map((row) => (
                  <option key={row.id} value={row.id}>
                    {itemLabel(row)}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>السعر</span>
              <input className="input" dir="ltr" required value={String(rowForm.unitPrice)} onChange={(event) => setRowForm({ ...rowForm, unitPrice: event.target.value })} />
            </label>
            <label className="field">
              <span>من كمية</span>
              <input className="input" dir="ltr" value={String(rowForm.minQty)} placeholder="0" onChange={(event) => setRowForm({ ...rowForm, minQty: event.target.value })} />
            </label>
            <button className="btn" type="submit" disabled={busy}>
              إضافة سعر
            </button>
          </form>

          <QueryView query={rows} empty="لا توجد أسعار في هذه القائمة">
            {(data) => (
              <DataTable
                columns={[
                  {
                    key: 'item',
                    header: 'الصنف',
                    cell: (row: PriceListItem) => {
                      const item = (items.data ?? []).find((candidate) => candidate.id === row.itemId);
                      return item ? itemLabel(item) : '—';
                    },
                  },
                  { key: 'price', header: 'السعر', align: 'num', cell: (row: PriceListItem) => money(row.unitPrice) },
                  { key: 'minQty', header: 'من كمية', align: 'num', cell: (row: PriceListItem) => quantity(row.minQty ?? '0') },
                ]}
                rows={data}
                rowKey={(row) => row.id}
              />
            )}
          </QueryView>
        </div>
      ) : null}
    </Screen>
  );
}
