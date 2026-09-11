'use client';

import { useState } from 'react';

import { DataTable, Notice, QueryView } from '../../../components/data-view';
import { WarehousePicker } from '../../../components/inventory-filters';
import { Screen } from '../../../components/screen';
import { ApiError } from '../../../lib/api';
import {
  closeTransfer,
  inTransitReport,
  money,
  quantity,
  shortDate,
  type InTransitRow,
} from '../../../lib/lookups';
import { useSession } from '../../../lib/session';
import { useQuery } from '../../../lib/use-query';

/**
 * بضاعة في الطريق — goods that left the source warehouse and never fully arrived.
 *
 * A transfer that sits here is not a statistic: the source has already lost the stock,
 * the destination never gained it, and بضاعة تحت التحويل keeps a balance nobody can
 * explain. This screen is where it is settled — either the remainder comes home
 * (`return`) or it is written off as a shortage — and in both cases the transit account
 * is cleared by a balanced entry.
 */
export default function InTransitPage() {
  const { can } = useSession();
  const manage = can('inventory.adjust');
  const [warehouseId, setWarehouseId] = useState('');
  const [busy, setBusy] = useState('');
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();

  const rows = useQuery<InTransitRow[]>(() => inTransitReport(warehouseId || undefined), [warehouseId]);

  async function settle(row: InTransitRow, mode: 'return' | 'shortage') {
    const question =
      mode === 'return'
        ? `إعادة ${row.qty} من ${row.sku ?? row.nameAr ?? ''} إلى مستودع المصدر؟`
        : `اعتبار ${row.qty} من ${row.sku ?? row.nameAr ?? ''} عجزاً وتحميل قيمته؟`;
    if (!window.confirm(question)) return;
    const reason = window.prompt(mode === 'return' ? 'سبب العودة' : 'سبب العجز') ?? undefined;
    setBusy(`${row.transferId}:${row.lineNo}`);
    setNotice(undefined);
    try {
      const result = await closeTransfer(row.transferId, { mode, reason: reason || undefined });
      setNotice({
        kind: 'ok',
        text:
          mode === 'return'
            ? `أُغلقت المناقلة ${result.number} بعودة البضاعة إلى المصدر — بقيمة ${money(result.value)}.`
            : `أُغلقت المناقلة ${result.number} كعجز — بقيمة ${money(result.value)}.`,
      });
      rows.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy('');
    }
  }

  const data = rows.data ?? [];
  const totalValue = data.reduce((sum, row) => sum + Number(row.value), 0);
  const stale = data.filter((row) => (row.daysInTransit ?? 0) >= 3).length;

  return (
    <Screen
      title="بضاعة في الطريق"
      subtitle="مناقلات أُرسلت ولم تُستلم كاملة — ما زال في حساب بضاعة تحت التحويل حتى تُعاد أو تُقفل."
      crumbs={['المستودعات', 'العمليات']}
    >
      <div className="card toolbar">
        <WarehousePicker value={warehouseId} onChange={setWarehouseId} includeAll />
        <span className="badge">{`قيمة المعلّق: ${money(totalValue)}`}</span>
        {stale > 0 && <span className="badge failed">{`متأخرة ٣ أيام أو أكثر: ${stale}`}</span>}
      </div>

      {stale > 0 && (
        <p className="alert warn">
          توجد أصناف في الطريق منذ ثلاثة أيام أو أكثر. راجعها: إما أن تُستلم، أو تُعاد إلى مصدرها، أو تُقفل
          كعجز.
        </p>
      )}

      <Notice notice={notice} />

      <QueryView
        query={rows}
        empty="لا توجد بضاعة في الطريق"
        emptyDetail="كل المناقلات إما استُلمت كاملة أو أُلغيت."
      >
        {(list) => (
          <DataTable
            rows={list}
            rowKey={(row) => `${row.transferId}:${row.lineNo}`}
            columns={[
              { key: 'number', header: 'المناقلة', align: 'ltr', cell: (row) => row.number },
              { key: 'sent', header: 'تاريخ الإرسال', align: 'ltr', cell: (row) => shortDate(row.sentAt) },
              {
                key: 'age',
                header: 'أيام في الطريق',
                align: 'num',
                cell: (row) =>
                  (row.daysInTransit ?? 0) >= 3 ? (
                    <span className="badge failed" dir="ltr">{`${row.daysInTransit} يوم`}</span>
                  ) : (
                    <span dir="ltr">{`${row.daysInTransit ?? 0} يوم`}</span>
                  ),
              },
              { key: 'item', header: 'الصنف', cell: (row) => `${row.sku ?? ''} — ${row.nameAr ?? ''}` },
              { key: 'qty', header: 'الكمية المعلّقة', align: 'num', cell: (row) => quantity(row.qty) },
              { key: 'base', header: 'بالوحدة الأساسية', align: 'num', cell: (row) => quantity(row.baseQty) },
              { key: 'value', header: 'القيمة', align: 'num', cell: (row) => money(row.value) },
              ...(manage
                ? [
                    {
                      key: 'actions',
                      header: '',
                      cell: (row: InTransitRow) => (
                        <span className="row">
                          <button
                            className="btn sm"
                            type="button"
                            disabled={busy === `${row.transferId}:${row.lineNo}`}
                            onClick={() => void settle(row, 'return')}
                          >
                            إعادة للمصدر
                          </button>
                          <button
                            className="btn sm danger"
                            type="button"
                            disabled={busy === `${row.transferId}:${row.lineNo}`}
                            onClick={() => void settle(row, 'shortage')}
                          >
                            إقفال كعجز
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
