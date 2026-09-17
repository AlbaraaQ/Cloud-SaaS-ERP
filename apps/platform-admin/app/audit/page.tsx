'use client';

import { useState } from 'react';

import { Empty, ErrorBox, Forbidden, Loading, Screen } from '../../components/screen';
import { apiData } from '../../lib/api';
import { useQuery } from '../../lib/use-query';

/**
 * التدقيق — now actually cross-tenant (P-C1).
 *
 * Before this part the page called `GET /audit-log`: a tenant-scoped endpoint that answered
 * with the **platform tenant's own rows** — for an operator, an empty list that looked like
 * "nothing ever happened" (INCOMPLETE_INVENTORY §4.2 measured it). The page now reads
 * `GET /platform/audit`, which returns every customer's rows with the customer's code and
 * name beside them, and filters by customer, action, entity and date.
 */

type AuditRow = {
  id: string;
  tenantId: string | null;
  tenantCode: string | null;
  tenantName: string | null;
  occurredAt?: never;
  action: string;
  entity: string;
  entityId: string | null;
  actorUserId: string | null;
  actorLabel: string | null;
  createdAt: string;
};

type AuditPageResult = { items: AuditRow[]; total: number; limit: number; offset: number };

const PAGE_SIZE = 100;

export default function AuditPage() {
  const [filters, setFilters] = useState({ tenant: '', action: '', entity: '', from: '', to: '' });
  const [applied, setApplied] = useState(filters);

  const audit = useQuery<AuditPageResult>(() => {
    const params = new URLSearchParams({ limit: String(PAGE_SIZE) });
    if (applied.tenant) params.set('filter[tenantId]', applied.tenant);
    if (applied.action) params.set('filter[action]', applied.action);
    if (applied.entity) params.set('filter[entity]', applied.entity);
    if (applied.from) params.set('filter[from]', new Date(applied.from).toISOString());
    if (applied.to) params.set('filter[to]', new Date(applied.to).toISOString());
    return apiData<AuditPageResult>(`/platform/audit?${params.toString()}`);
  }, [applied]);

  const rows = audit.data?.items ?? [];

  return (
    <Screen
      title="سجل التدقيق"
      subtitle="سجل عابر للمستأجرين: كل عملية مؤثّرة في أي منشأة، بمَن فعلها ولمن. للقراءة فقط."
      crumbs={['المنصة', 'التشغيل']}
      actions={
        <button className="btn" type="button" onClick={audit.reload}>
          تحديث
        </button>
      }
    >
      <div className="card tight no-print">
        <div className="form-grid">
          <label className="field">
            <span>معرّف العميل (UUID)</span>
            <input
              className="input"
              dir="ltr"
              value={filters.tenant}
              placeholder="اتركه فارغاً لكل العملاء"
              onChange={(event) => setFilters({ ...filters, tenant: event.target.value })}
            />
          </label>
          <label className="field">
            <span>الإجراء</span>
            <input
              className="input"
              dir="ltr"
              value={filters.action}
              placeholder="create · update · auth.login"
              onChange={(event) => setFilters({ ...filters, action: event.target.value })}
            />
          </label>
          <label className="field">
            <span>الكيان</span>
            <input
              className="input"
              dir="ltr"
              value={filters.entity}
              placeholder="settings · sales_invoices"
              onChange={(event) => setFilters({ ...filters, entity: event.target.value })}
            />
          </label>
          <label className="field">
            <span>من وقت</span>
            <input
              className="input"
              type="datetime-local"
              value={filters.from}
              onChange={(event) => setFilters({ ...filters, from: event.target.value })}
            />
          </label>
          <label className="field">
            <span>إلى وقت</span>
            <input
              className="input"
              type="datetime-local"
              value={filters.to}
              onChange={(event) => setFilters({ ...filters, to: event.target.value })}
            />
          </label>
        </div>
        <div className="row">
          <button className="btn primary" type="button" onClick={() => setApplied(filters)}>
            تطبيق المرشّحات
          </button>
          <button
            className="btn"
            type="button"
            onClick={() => {
              const cleared = { tenant: '', action: '', entity: '', from: '', to: '' };
              setFilters(cleared);
              setApplied(cleared);
            }}
          >
            إزالة المرشّحات
          </button>
        </div>
      </div>

      {audit.status === 'loading' && <Loading />}
      {audit.status === 'forbidden' && <Forbidden />}
      {audit.status === 'error' && <ErrorBox message={audit.error} onRetry={audit.reload} />}
      {audit.status === 'success' &&
        (rows.length === 0 ? (
          <Empty
            title="لا توجد سجلات مطابقة"
            detail="جرّب توسيع الفترة أو إزالة المرشّحات — القراءة تشمل كل المنشآت."
          />
        ) : (
          <>
            <p className="muted small">
              {rows.length} من {audit.data?.total ?? rows.length} سجلاً
            </p>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>الوقت</th>
                    <th>العميل</th>
                    <th>الإجراء</th>
                    <th>الكيان</th>
                    <th>المعرّف</th>
                    <th>المستخدم</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((row) => (
                    <tr key={row.id}>
                      <td dir="ltr">{new Date(row.createdAt).toLocaleString('ar-SA')}</td>
                      <td>
                        {row.tenantName ? (
                          <>
                            {row.tenantName}
                            <br />
                            <span className="muted small" dir="ltr">
                              {row.tenantCode}
                            </span>
                          </>
                        ) : (
                          <span className="muted">المنصة</span>
                        )}
                      </td>
                      <td dir="ltr">{row.action}</td>
                      <td dir="ltr">{row.entity}</td>
                      <td dir="ltr" className="small">
                        {row.entityId ? row.entityId.slice(0, 12) : '—'}
                      </td>
                      <td dir="ltr" className="small">
                        {row.actorLabel ?? (row.actorUserId ? row.actorUserId.slice(0, 8) : '—')}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        ))}
    </Screen>
  );
}
