'use client';

import { use, useEffect, useMemo, useState } from 'react';

import { DataTable, Notice, QueryView } from '../../../components/data-view';
import { Screen } from '../../../components/screen';
import {
  arabicName,
  itemLabel,
  listBranches,
  listCategories,
  listCostCenters,
  listItems,
  listParties,
  listSalesmen,
  listWarehouses,
  partyLabel,
  type Branch,
  type Category,
  type CostCenter,
  type Item,
  type Party,
  type Salesman,
  type Warehouse,
} from '../../../lib/lookups';
import {
  REPORT_GROUP_LABELS,
  exportReport,
  fetchReportCatalog,
  fetchReportLayouts,
  formatCell,
  initialFilters,
  isNumericColumn,
  openPrintable,
  runReport,
  saveExport,
  type ExportFormat,
  type ReportEntry,
  type ReportLayout,
  type ReportParam,
  type ReportResult,
} from '../../../lib/reports';
import { useQuery } from '../../../lib/use-query';


export default function ReportRunnerPage({ params }: { params: Promise<{ key: string }> }) {
  const reportKey = use(params).key;
  const catalog = useQuery<ReportEntry[]>(() => fetchReportCatalog(), []);
  const entry = (catalog.data ?? []).find((row) => row.key === reportKey);

  const [filters, setFilters] = useState<Record<string, string>>({});
  const [applied, setApplied] = useState<Record<string, string>>({});
  const [ready, setReady] = useState(false);
  const [busy, setBusy] = useState<ExportFormat | null>(null);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger' | 'warn'; text: string } | null>(null);
  // Saved layouts (مصمم التقارير) ride along as an ordinary filter: the server resolves
  // the layout, applies its defaults and returns the columns already shaped.
  const layouts = useQuery<ReportLayout[]>(() => fetchReportLayouts(reportKey), [reportKey]);

  // The filter shape is only known once the catalog answers.
  useEffect(() => {
    if (!entry) return;
    const initial = initialFilters(entry.params);
    setFilters(initial);
    setApplied(initial);
    setReady(true);
  }, [entry]);

  const appliedKey = JSON.stringify(applied);
  const report = useQuery<ReportResult | null>(async () => (ready && entry ? runReport(reportKey, applied) : null), [reportKey, appliedKey, ready, Boolean(entry)]);

  const kinds = new Set((entry?.params ?? []).map((param) => param.kind));
  const branches = useQuery<Branch[]>(async () => (kinds.has('branch') ? listBranches() : []), [reportKey, kinds.has('branch')]);
  const warehouses = useQuery<Warehouse[]>(async () => (kinds.has('warehouse') ? listWarehouses() : []), [reportKey, kinds.has('warehouse')]);
  const parties = useQuery<Party[]>(async () => (kinds.has('party') ? listParties() : []), [reportKey, kinds.has('party')]);
  const items = useQuery<Item[]>(async () => (kinds.has('item') ? listItems() : []), [reportKey, kinds.has('item')]);
  const categories = useQuery<Category[]>(async () => (kinds.has('category') ? listCategories() : []), [reportKey, kinds.has('category')]);
  const costCenters = useQuery<CostCenter[]>(async () => (kinds.has('costCenter') ? listCostCenters() : []), [reportKey, kinds.has('costCenter')]);
  const salesmen = useQuery<Salesman[]>(async () => (kinds.has('salesman') ? listSalesmen() : []), [reportKey, kinds.has('salesman')]);

  const optionsFor = useMemo(
    () => ({
      branch: (branches.data ?? []).map((row) => ({ value: row.id, label: arabicName(row) })),
      warehouse: (warehouses.data ?? []).map((row) => ({ value: row.id, label: arabicName(row) })),
      party: (parties.data ?? []).map((row) => ({ value: row.id, label: partyLabel(row) })),
      item: (items.data ?? []).map((row) => ({ value: row.id, label: itemLabel(row) })),
      category: (categories.data ?? []).map((row) => ({ value: row.id, label: arabicName(row) })),
      costCenter: (costCenters.data ?? []).map((row) => ({ value: row.id, label: `${row.code} — ${arabicName(row)}` })),
      salesman: (salesmen.data ?? []).map((row) => ({ value: row.id, label: row.name })),
    }),
    [branches.data, warehouses.data, parties.data, items.data, categories.data, costCenters.data, salesmen.data],
  );

  if (catalog.status === 'success' && !entry) {
    return (
      <Screen title="تقرير غير معروف" crumbs={['التقارير']}>
        <Notice notice={{ kind: 'danger', text: `لا يوجد تقرير بالمفتاح «${reportKey}». راجع مركز التقارير.` }} />
      </Screen>
    );
  }

  const result = report.data ?? null;
  const columns = entry?.columns ?? [];

  // Exports re-run the report on the server: the file always matches the filters and the
  // saved layout, never just the rows that happen to be rendered.
  async function download(format: ExportFormat) {
    setBusy(format);
    setNotice(null);
    try {
      const produced = await exportReport(reportKey, applied, format);
      if (format === 'pdf') {
        if (!openPrintable(produced.content)) {
          setNotice({ kind: 'warn', text: 'تعذّر فتح نافذة الطباعة — اسمح بالنوافذ المنبثقة لهذا الموقع ثم أعد المحاولة.' });
        }
      } else {
        saveExport(produced);
        setNotice({ kind: 'ok', text: `تم تصدير ${produced.rows} سجلاً إلى الملف ${produced.filename}` });
      }
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof Error ? error.message : 'تعذّر التصدير' });
    } finally {
      setBusy(null);
    }
  }

  return (
    <Screen
      title={entry?.titleAr ?? 'تقرير'}
      subtitle={entry?.hintAr ?? undefined}
      crumbs={['التقارير', REPORT_GROUP_LABELS[entry?.group ?? ''] ?? '']}
      actions={
        <>
          <button type="button" className="btn" disabled={!result || busy !== null} onClick={() => void download('xlsx')}>
            {busy === 'xlsx' ? 'جارٍ التصدير…' : 'تصدير Excel'}
          </button>
          <button type="button" className="btn" disabled={!result || busy !== null} onClick={() => void download('csv')}>
            {busy === 'csv' ? 'جارٍ التصدير…' : 'تصدير CSV'}
          </button>
          <button type="button" className="btn no-print" disabled={!result || busy !== null} onClick={() => void download('pdf')}>
            {busy === 'pdf' ? 'جارٍ التجهيز…' : 'طباعة / PDF'}
          </button>
        </>
      }
    >
      {entry ? (
        <form
          className="card toolbar no-print"
          onSubmit={(event) => {
            event.preventDefault();
            setApplied({ ...filters });
          }}
        >
          {(layouts.data ?? []).length > 0 && (
            <label className="field">
              <span>التصميم</span>
              <select
                className="input"
                value={filters.layout ?? ''}
                onChange={(event) => setFilters((current) => ({ ...current, layout: event.target.value }))}
              >
                <option value="">الافتراضي</option>
                {(layouts.data ?? []).map((layout) => (
                  <option key={layout.id} value={layout.id}>
                    {layout.name}
                  </option>
                ))}
                <option value="none">كل الأعمدة</option>
              </select>
            </label>
          )}
          {entry.params.map((param) => (
            <FilterField
              key={param.name}
              param={param}
              value={filters[param.name] ?? ''}
              options={param.kind === 'select' ? (param.options ?? []).map((option) => ({ value: option.value, label: option.labelAr })) : optionsFor[param.kind as keyof typeof optionsFor] ?? []}
              onChange={(value) => setFilters((current) => ({ ...current, [param.name]: value }))}
            />
          ))}
          <button type="submit" className="btn primary">
            عرض التقرير
          </button>
        </form>
      ) : null}

      <Notice notice={notice ?? undefined} />

      <QueryView query={report} isEmpty={(data) => data !== null && data.rows.length === 0} empty="لا توجد بيانات ضمن هذه الفترة" emptyDetail="جرّب توسيع الفترة أو إزالة المرشحات — التقارير تعرض المستندات المرحّلة فقط.">
        {(data) =>
          data === null ? null : (
            <>
              <div className="card">
                <div className="row">
                  <span className="chip">عدد السطور: {data.rowCount}</span>
                  <span className="chip">وقت الاستخراج: {data.generatedAt.slice(0, 16).replace('T', ' ')}</span>
                  {Object.entries(data.totals).map(([key, value]) => (
                    <span key={key} className="chip">
                      {columns.find((column) => column.key === key)?.labelAr ?? key}: {formatCell(value, columns.find((column) => column.key === key)?.type ?? 'money')}
                    </span>
                  ))}
                </div>
              </div>
              <div className="card">
                <DataTable
                  columns={data.columns.map((column) => ({
                    key: column.key,
                    header: column.labelAr,
                    align: isNumericColumn(column) ? ('num' as const) : undefined,
                    cell: (row: Record<string, string>) => formatCell(row[column.key] ?? '', column.type),
                  }))}
                  rows={data.rows}
                  rowKey={(_row, index) => `${reportKey}-${index}`}
                />
              </div>
            </>
          )
        }
      </QueryView>
    </Screen>
  );
}

function FilterField({ param, value, options, onChange }: { param: ReportParam; value: string; options: Array<{ value: string; label: string }>; onChange: (value: string) => void }) {
  if (param.kind === 'date') {
    return (
      <label className="field">
        <span>{param.labelAr}</span>
        <input className="input" type="date" value={value} onChange={(event) => onChange(event.target.value)} />
      </label>
    );
  }
  return (
    <label className="field">
      <span>{param.labelAr}</span>
      <select className="input" value={value} onChange={(event) => onChange(event.target.value)}>
        <option value="">الكل</option>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  );
}
