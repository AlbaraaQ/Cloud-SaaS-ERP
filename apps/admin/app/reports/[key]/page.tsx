'use client';

import { use, useEffect, useMemo, useState } from 'react';

import { DataTable, Notice, QueryView } from '../../../components/data-view';
import { Screen } from '../../../components/screen';
import { downloadCsv } from '../../../lib/accounts';
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
  fetchReportCatalog,
  formatCell,
  initialFilters,
  isNumericColumn,
  runReport,
  type ReportEntry,
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

  return (
    <Screen
      title={entry?.titleAr ?? 'تقرير'}
      subtitle={entry?.hintAr ?? undefined}
      crumbs={['التقارير', REPORT_GROUP_LABELS[entry?.group ?? ''] ?? '']}
      actions={
        <>
          <button
            type="button"
            className="btn"
            disabled={!result || result.rows.length === 0}
            onClick={() => {
              if (!result) return;
              downloadCsv(
                `${reportKey}.csv`,
                result.columns.map((column) => column.labelAr),
                result.rows.map((row) => result.columns.map((column) => row[column.key] ?? '')),
              );
            }}
          >
            تصدير CSV
          </button>
          <button type="button" className="btn no-print" onClick={() => window.print()}>
            طباعة
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
