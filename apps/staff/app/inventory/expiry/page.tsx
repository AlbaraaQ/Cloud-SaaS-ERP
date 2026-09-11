'use client';

import { useState } from 'react';

import { DataTable, QueryView } from '../../../components/data-view';
import { Screen } from '../../../components/screen';
import { expiryReport, quantity, shortDate, type ExpiryRow } from '../../../lib/lookups';
import { useQuery } from '../../../lib/use-query';

const HORIZONS = [
  { days: 7, label: 'أسبوع' },
  { days: 30, label: 'شهر' },
  { days: 90, label: 'ثلاثة أشهر' },
  { days: 365, label: 'سنة' },
];

/**
 * تواريخ الصلاحية — what is on the shelf, and how long it has left.
 *
 * A lot you cannot see is a lot that quietly becomes waste: the desktop carried this
 * report next to the item card, and it is the reason a lot carries an expiry date at
 * all. Rows already past their date lead the list and are marked منتهية.
 */
export default function ExpiryPage() {
  const [days, setDays] = useState(30);
  const rows = useQuery<ExpiryRow[]>(() => expiryReport(days), [days]);

  const data = rows.data ?? [];
  const expired = data.filter((row) => row.expired).length;
  const atRisk = data.length - expired;

  return (
    <Screen
      title="تواريخ الصلاحية"
      subtitle="الدفعات التي تنتهي خلال المدة المحددة، وما انتهى منها فعلاً — مرتبة بالأقرب أولاً."
      crumbs={['المستودعات', 'التقارير']}
    >
      <div className="card toolbar">
        <label className="field">
          <span>خلال</span>
          <select
            className="input"
            value={String(days)}
            onChange={(event) => setDays(Number(event.target.value))}
          >
            {HORIZONS.map((horizon) => (
              <option key={horizon.days} value={horizon.days}>
                {horizon.label}
              </option>
            ))}
          </select>
        </label>
        <span className={`badge${expired > 0 ? ' failed' : ''}`}>{`منتهية: ${expired}`}</span>
        <span className="badge">{`قاربت: ${atRisk}`}</span>
      </div>

      {expired > 0 && (
        <p className="alert warn">
          {`يوجد ${expired} دفعة منتهية الصلاحية في الأرصدة. راجعها بجرد أو سند صرف قبل البيع.`}
        </p>
      )}

      <QueryView
        query={rows}
        empty="لا توجد دفعات قاربت على الانتهاء"
        emptyDetail="لم تُسجَّل أي دفعة بتاريخ صلاحية داخل هذه المدة. أنشئ الدفعات من شاشة الدفعات عند الاستلام."
      >
        {(list) => (
          <DataTable
            rows={list}
            rowKey={(row) => row.lotId}
            columns={[
              { key: 'lot', header: 'رقم الدفعة', align: 'ltr', cell: (row) => row.lotNo },
              { key: 'sku', header: 'الرمز', align: 'ltr', cell: (row) => row.sku },
              { key: 'item', header: 'المادة', cell: (row) => row.nameAr },
              {
                key: 'expiry',
                header: 'تاريخ الانتهاء',
                align: 'ltr',
                cell: (row) => shortDate(row.expiryDate),
              },
              {
                key: 'left',
                header: 'المتبقي',
                align: 'num',
                cell: (row) =>
                  row.expired ? (
                    <span
                      className="badge failed"
                      dir="ltr"
                    >{`انتهت منذ ${Math.abs(row.daysLeft)} يوم`}</span>
                  ) : (
                    <span dir="ltr">{`${row.daysLeft} يوم`}</span>
                  ),
              },
              { key: 'qty', header: 'الرصيد', align: 'num', cell: (row) => quantity(row.quantity) },
            ]}
          />
        )}
      </QueryView>
    </Screen>
  );
}
