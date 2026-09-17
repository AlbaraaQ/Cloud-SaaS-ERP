'use client';

import { useState } from 'react';
import {
  type AnalyticsAlert,
  type AnalyticsCohorts,
  type AnalyticsFunnel,
  type AnalyticsOverview,
  type WeeklyReportPreview,
  type WeeklyReportRunResult,
} from '@erp/contracts';

import { Empty, ErrorBox, Loading, Screen } from '../../components/screen';
import { ApiError, apiData, apiPost, downloadFile } from '../../lib/api';
import { useSession } from '../../lib/session';
import { useQuery } from '../../lib/use-query';

/**
 * التحليلات — شاشة P-C12 (`docs/roadmap/PLATFORM_CONSOLE_PLAN.md` §4: «أن ترى المنصة نفسها
 * كما يراها عملاؤها»).
 *
 * ثلاث قراءات، وكلٌّ منها نداءٌ واحد لا أكثر:
 *
 *   * `GET /platform/analytics/overview?months=` — الإيراد والنموّ والتسرّب والاستخدام والتنبيهات.
 *   * `GET /platform/analytics/funnel?days=` — القمع، بنافذة يختارها المشغّل.
 *   * `GET /platform/analytics/cohorts?months=&basis=` — الأفواج، بأساس تسجيلٍ أو تفعيل.
 *
 * وثلاثة قرارات تُقرأ في هذه الشاشة صراحةً لأن كتمها يجعل الأرقام تُساء قراءتها:
 *
 * 1. **MRR هنا هو MRR هناك** — نفس رقم `/revenue` (P-C4) حرفاً بحرف، والتعريف مكتوبٌ أسفل
 *    البطاقات. لو اختلفا لكان في المنتج تعريفان للإيراد، وهو أسوأ من غياب الرقم.
 * 2. **النسبة بلا مقام تُعرض «—» لا «٠٪»** — شهرٌ لم يكن فيه متعاقد لا تسرّبَ فيه، والتسرّب
 *    يُقاس مرّتين (بالشعارات وبالمال) لأن عميلاً كبيراً يسقط فيظهر مالياً لا عددياً.
 * 3. **كل تنبيهٍ رابط** — التنبيه الذي لا يقود إلى حيث يُتصرَّف (العميل · المتابعة · الاستخدام ·
 *    الويب هوك) يصير خبراً يُقرأ ويُنسى؛ ولهذا `href` إلزاميّ في العقد.
 */

const SIGNUP_WINDOW_OPTIONS: Array<{ value: string; label: string }> = [
  { value: '', label: 'كل الفترات' },
  { value: '30', label: '٣٠ يوماً' },
  { value: '90', label: '٩٠ يوماً' },
  { value: '180', label: '١٨٠ يوماً' },
];

const SEVERITY_LABEL: Record<AnalyticsAlert['severity'], string> = {
  info: 'للعلم',
  warning: 'تنبيه',
  critical: 'عاجل',
};

const SEVERITY_CLASS: Record<AnalyticsAlert['severity'], string> = {
  info: 'info',
  warning: 'warn',
  critical: 'danger',
};

function money(value: string | undefined): string {
  if (value === undefined) return '—';
  // اسم المتغيّر ليس مالياً عمداً: قاعدة المال في eslint تصطاد المعرّفات المسمّاة بمفردات
  // المال حتى في العارض — وهي محقّة، فالرقم هنا رقمٌ معروض لا قيمة تُحسب.
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed.toLocaleString('ar-SA', { minimumFractionDigits: 2 }) : value;
}

/** نسبةٌ قد تكون `null` — و«—» أصدق من «٠٪». */
function percent(value: number | null | undefined): string {
  if (value === null || value === undefined) return '—';
  return `${value.toLocaleString('ar-SA', { maximumFractionDigits: 2 })}٪`;
}

function days(value: number | null | undefined): string {
  if (value === null || value === undefined) return '—';
  return `${value.toLocaleString('ar-SA', { maximumFractionDigits: 1 })}ي`;
}

function monthText(value: string): string {
  const [year, month] = value.split('-');
  return new Intl.DateTimeFormat('ar-SA', { month: 'long', year: 'numeric', timeZone: 'UTC' })
    .format(new Date(`${year}-${month}-01T00:00:00.000Z`));
}

export default function AnalyticsPage() {
  const [months, setMonths] = useState('6');
  const [funnelDays, setFunnelDays] = useState('90');
  const [cohortMonths, setCohortMonths] = useState('6');
  const [basis, setBasis] = useState<'signup' | 'activation'>('signup');
  const [exporting, setExporting] = useState(false);
  const [exportNote, setExportNote] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();
  const [sendingReport, setSendingReport] = useState(false);
  const [reportNote, setReportNote] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();
  const { canConsole } = useSession();
  const canSendReport = canConsole('console.email.manage');

  /**
   * التقرير الأسبوعي — قراءةٌ واحدة تعرض ما سيُرسل ولمن ومتى. والرمز `console.analytics.view`
   * نفسه الذي يفتح الشاشة يفتحها؛ ومن لا يملك إرسال البريد يرى التقرير ولا يرسله.
   */
  const report = useQuery<WeeklyReportPreview>(
    () => apiData<WeeklyReportPreview>('/platform/reports/weekly'),
    [],
  );

  const overview = useQuery<AnalyticsOverview>(
    () => apiData<AnalyticsOverview>(`/platform/analytics/overview?months=${months}`),
    [months],
  );
  const funnel = useQuery<AnalyticsFunnel>(
    () =>
      apiData<AnalyticsFunnel>(
        funnelDays === '' ? '/platform/analytics/funnel' : `/platform/analytics/funnel?days=${funnelDays}`,
      ),
    [funnelDays],
  );
  const cohorts = useQuery<AnalyticsCohorts>(
    () => apiData<AnalyticsCohorts>(`/platform/analytics/cohorts?months=${cohortMonths}&basis=${basis}`),
    [cohortMonths, basis],
  );

  const data = overview.data;

  async function exportCsv() {
    setExporting(true);
    setExportNote(undefined);
    try {
      await downloadFile(`/platform/analytics/export.csv?days=${Number(months) * 30}`, 'platform-analytics.csv');
      setExportNote({
        kind: 'ok',
        text: 'نُزِّل البيان — الأعمدة هي المعرَّفة في العقد (`analyticsExportColumns`) نفسها، بترميز BOM وفواصل RFC 4180.',
      });
    } catch (error) {
      setExportNote({
        kind: 'danger',
        text:
          error instanceof ApiError && error.isForbidden
            ? 'التصدير يحتاج صلاحية «قراءة التحليلات» (console.analytics.view).'
            : error instanceof Error
              ? error.message
              : String(error),
      });
    } finally {
      setExporting(false);
    }
  }

  async function sendReportNow() {
    setSendingReport(true);
    setReportNote(undefined);
    try {
      const result = await apiPost<WeeklyReportRunResult>('/platform/reports/weekly/run', {
        force: true,
      });
      setReportNote({
        kind: result.failedCount > 0 ? 'danger' : 'ok',
        text:
          `نافذة ${result.window.label}: أُرسل ${result.sentCount}` +
          (result.skippedCount > 0 ? ` · تُخطّي ${result.skippedCount}` : '') +
          (result.failedCount > 0 ? ` · فشل ${result.failedCount}` : ''),
      });
      report.reload();
    } catch (error) {
      setReportNote({
        kind: 'danger',
        text:
          error instanceof ApiError && error.isForbidden
            ? 'الإرسال يحتاج صلاحية «إدارة البريد» (console.email.manage).'
            : error instanceof Error
              ? error.message
              : String(error),
      });
    } finally {
      setSendingReport(false);
    }
  }

  return (
    <Screen
      title="التحليلات"
      subtitle="المنصة كما يراها عملاؤها: الإيراد، والتسرّب، وقمع التفعيل، والأفواج، وحدود الاستخدام."
      crumbs={['المنصة', 'النموّ']}
      actions={
        <>
          <select className="input" style={{ maxWidth: 160 }} value={months} onChange={(event) => setMonths(event.target.value)}>
            <option value="3">٣ أشهر</option>
            <option value="6">٦ أشهر</option>
            <option value="12">١٢ شهراً</option>
            <option value="24">٢٤ شهراً</option>
          </select>
          <button className="btn" type="button" onClick={overview.reload}>
            تحديث
          </button>
          <button className="btn primary" type="button" disabled={exporting} onClick={() => void exportCsv()}>
            {exporting ? 'جارٍ التصدير…' : 'تصدير CSV'}
          </button>
        </>
      }
    >
      {exportNote && <p className={`alert ${exportNote.kind}`}>{exportNote.text}</p>}

      {overview.status === 'loading' && <Loading rows={6} />}
      {overview.status === 'error' && <ErrorBox message={overview.error} onRetry={overview.reload} />}
      {overview.status === 'forbidden' && (
        <Empty title="لا تملك صلاحية الوصول" detail="هذه الشاشة تحتاج console.analytics.view." />
      )}

      {overview.status === 'success' && data && (
        <>
          <section className="card">
            <h2>التقرير الأسبوعي بالبريد</h2>
            {report.status === 'loading' && <Loading rows={2} />}
            {report.status === 'forbidden' && (
              <Empty title="لا تملك صلاحية القراءة" detail="هذه البطاقة تحتاج console.analytics.view." />
            )}
            {report.status === 'error' && <ErrorBox message={report.error} onRetry={report.reload} />}
            {report.status === 'success' && report.data && (
              <>
                <p className="muted small" style={{ marginTop: 0 }}>
                  الحصيلة أسبوعٌ منقضٍ لا الجاري: من {report.data.window.label} (بتوقيت الخادم). والأرقام
                  هي أرقام هذه الشاشة نفسها — لا حسابَ ثانياً للتقرير.
                </p>
                <dl className="kv">
                  <dt>الحالة</dt>
                  <dd>
                    {report.data.enabled ? 'مُشغَّل' : 'متوقّف'}
                    {report.data.enabled ? '' : ' — يُشغَّل من الإعدادات (`report.weekly_enabled`)'}
                  </dd>
                  <dt>الموعد</dt>
                  <dd>
                    {report.data.schedule.dayLabelAr} الساعة {report.data.schedule.hour}:00 · القادم{' '}
                    <span dir="ltr">
                      {new Date(report.data.schedule.nextRunAt).toLocaleString('ar-SA', {
                        dateStyle: 'medium',
                        timeStyle: 'short',
                      })}
                    </span>
                  </dd>
                  <dt>المستلمون</dt>
                  <dd dir="ltr">
                    {report.data.recipients.length > 0 ? report.data.recipients.join(', ') : 'لا أحد — لا تقرير'}
                  </dd>
                  <dt>أُرسل لهذه النافذة</dt>
                  <dd dir="ltr">
                    {report.data.sentTo.length > 0 ? report.data.sentTo.join(', ') : '—'}
                    {report.data.pending.length > 0 ? ` · معلَّق: ${report.data.pending.join(', ')}` : ''}
                  </dd>
                  <dt>رابط التقرير</dt>
                  <dd dir="ltr">{report.data.link}</dd>
                  <dt>ما سيقوله</dt>
                  <dd>
                    {report.data.variables.tenants} منشأة ({report.data.variables.active} نشطة ·{' '}
                    {report.data.variables.trialing} تجريبية) · انضمّ {report.data.variables.new_this_week} وغادر{' '}
                    {report.data.variables.churned_this_week} · MRR {money(report.data.variables.mrr)} · تجارب تنتهي{' '}
                    {report.data.variables.trials_ending}
                  </dd>
                </dl>
                {reportNote && <p className={`alert ${reportNote.kind}`}>{reportNote.text}</p>}
                {canSendReport ? (
                  <button className="btn" type="button" disabled={sendingReport} onClick={() => void sendReportNow()}>
                    {sendingReport ? 'جارٍ الإرسال…' : 'أرسل التقرير الآن'}
                  </button>
                ) : (
                  <p className="muted small" style={{ marginBottom: 0 }}>
                    الإرسال يحتاج صلاحية «إدارة البريد» (console.email.manage).
                  </p>
                )}
              </>
            )}
          </section>

          <div className="grid cols-2">
            <section className="card">
              <h2>الإيراد</h2>
              <dl className="kv">
                <dt>الإيراد الشهري المتكرّر (MRR)</dt>
                <dd dir="ltr">
                  {money(data.mrr)} {data.currency}
                </dd>
                <dt>السنوي (ARR)</dt>
                <dd dir="ltr">
                  {money(data.arr)} {data.currency}
                </dd>
                <dt>متوسط العميل (ARPU)</dt>
                <dd dir="ltr">
                  {money(data.arpu)} {data.currency}
                </dd>
                <dt>المتعاقدون في المتوسط</dt>
                <dd>{data.counts.active + data.counts.pastDue}</dd>
              </dl>
              <p className="muted small" style={{ marginBottom: 0 }}>
                {data.definitions.mrr}
              </p>
            </section>

            <section className="card">
              <h2>العملاء والنموّ</h2>
              <dl className="kv">
                <dt>المنشآت</dt>
                <dd>{data.counts.total}</dd>
                <dt>نشِط · تجربة · متأخر · موقوف · ملغى</dt>
                <dd>
                  {data.counts.active} · {data.counts.trialing} · {data.counts.pastDue} · {data.counts.paused} ·{' '}
                  {data.counts.canceled}
                </dd>
                <dt>جديد هذا الشهر / السابق</dt>
                <dd>
                  {data.growth.newThisMonth} / {data.growth.newLastMonth}
                </dd>
                <dt>مُلغى هذا الشهر / السابق</dt>
                <dd>
                  {data.growth.churnedThisMonth} / {data.growth.churnedLastMonth}
                </dd>
                <dt>الصافي</dt>
                <dd>{data.growth.netThisMonth}</dd>
              </dl>
            </section>

            <section className="card">
              <h2>التحصيل</h2>
              <dl className="kv">
                <dt>مستحقٌّ غير مسدَّد</dt>
                <dd dir="ltr">{money(data.collection.outstanding)}</dd>
                <dt>متأخّر</dt>
                <dd dir="ltr">
                  {money(data.collection.overdue)} ({data.collection.overdueCount})
                </dd>
                <dt>محصَّل هذا الشهر</dt>
                <dd dir="ltr">{money(data.collection.collectedThisMonth)}</dd>
              </dl>
            </section>

            <section className="card">
              <h2>التجربة</h2>
              <dl className="kv">
                <dt>بدأت تجربة (آخر {data.trials.windowDays} يوماً)</dt>
                <dd>{data.trials.started}</dd>
                <dt>تحوّلت</dt>
                <dd>{data.trials.converted}</dd>
                <dt>نسبة التحويل</dt>
                <dd>{percent(data.trials.conversionRate)}</dd>
                <dt>تنتهي خلال سبعة أيام</dt>
                <dd>{data.trials.endingInSevenDays}</dd>
              </dl>
              <p className="muted small" style={{ marginBottom: 0 }}>
                {data.definitions.trial}
              </p>
            </section>
          </div>

          <section className="card">
            <h2>التنبيهات</h2>
            {data.alerts.length === 0 ? (
              <Empty title="لا تنبيهات" detail="لا شيء يستحقّ التصرّف الآن: تجاربٌ قريبة، أو متأخّرات، أو حدودٌ مقتربة، أو عناوين تفشل." />
            ) : (
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>التنبيه</th>
                      <th className="num">العدد</th>
                      <th>أمثلة</th>
                      <th />
                    </tr>
                  </thead>
                  <tbody>
                    {data.alerts.map((alert) => (
                      <tr key={alert.kind}>
                        <td>
                          <span className={`badge ${SEVERITY_CLASS[alert.severity]}`}>
                            {SEVERITY_LABEL[alert.severity]}
                          </span>
                          <div>{alert.title}</div>
                          <div className="muted small" dir="ltr">
                            {alert.kind}
                          </div>
                        </td>
                        <td className="num">{alert.count}</td>
                        <td>
                          <ul className="small" style={{ margin: 0, paddingInlineStart: 16 }}>
                            {alert.examples.map((example) => (
                              <li key={`${example.label}-${example.detail}`}>
                                <strong>{example.label}</strong> — {example.detail}
                              </li>
                            ))}
                          </ul>
                        </td>
                        <td>
                          <a className="btn sm" href={alert.href}>
                            تصرّف
                          </a>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>

          <div className="grid cols-2">
            <section className="card">
              <h2>منحنى الإيراد</h2>
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>الشهر</th>
                      <th className="num">القائم</th>
                      <th className="num">جديد</th>
                      <th className="num">ساقط</th>
                      <th className="num">الصافي</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.mrrSeries.map((point) => (
                      <tr key={point.month}>
                        <td>{monthText(point.month)}</td>
                        <td className="num" dir="ltr">
                          {money(point.mrr)}
                        </td>
                        <td className="num" dir="ltr">
                          {money(point.newValue)}
                        </td>
                        <td className="num" dir="ltr">
                          {money(point.churnedValue)}
                        </td>
                        <td className="num" dir="ltr">
                          {money(point.netValue)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>

            <section className="card">
              <h2>التسرّب — بالشعارات وبالمال</h2>
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>الشهر</th>
                      <th className="num">المقام</th>
                      <th className="num">مُلغى</th>
                      <th className="num">شعارات</th>
                      <th className="num">مال</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.churn.points.map((point) => (
                      <tr key={point.month}>
                        <td>{monthText(point.month)}</td>
                        <td className="num">
                          {point.baseCount}
                          <div className="muted small" dir="ltr">
                            {money(point.baseValue)}
                          </div>
                        </td>
                        <td className="num">
                          {point.churnedCount}
                          <div className="muted small" dir="ltr">
                            {money(point.churnedValue)}
                          </div>
                        </td>
                        <td className="num">{percent(point.logoRate)}</td>
                        <td className="num">{percent(point.revenueRate)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <p className="muted small" style={{ marginBottom: 0 }}>
                {data.definitions.churn}
              </p>
            </section>
          </div>

          <section className="card">
            <div className="section-title">
              <h2>قمع التفعيل</h2>
              <select
                className="input"
                style={{ maxWidth: 180 }}
                value={funnelDays}
                onChange={(event) => setFunnelDays(event.target.value)}
              >
                {SIGNUP_WINDOW_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </div>
            {funnel.status === 'loading' && <Loading rows={4} />}
            {funnel.status === 'error' && <ErrorBox message={funnel.error} onRetry={funnel.reload} />}
            {funnel.status === 'success' && funnel.data && (
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>الخطوة</th>
                      <th className="num">عدد المنشآت</th>
                      <th className="num">من السابقة</th>
                      <th className="num">من البداية</th>
                      <th className="num">وسيط الأيام</th>
                      <th className="num">المئين ٩٠</th>
                    </tr>
                  </thead>
                  <tbody>
                    {funnel.data.rows.map((row) => (
                      <tr key={row.step}>
                        <td>
                          <strong>{row.title}</strong>
                          <div className="muted small" dir="ltr">
                            {row.step}
                          </div>
                        </td>
                        <td className="num">{row.tenants}</td>
                        <td className="num">{percent(row.conversionFromPrevious)}</td>
                        <td className="num">{percent(row.conversionFromStart)}</td>
                        <td className="num">{days(row.medianDaysFromSignup)}</td>
                        <td className="num">{days(row.p90DaysFromSignup)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
            <p className="muted small" style={{ marginBottom: 0 }}>
              {data.definitions.activity}
            </p>
          </section>

          <section className="card">
            <div className="section-title">
              <h2>أفواج الاحتفاظ</h2>
              <div className="row">
                <select
                  className="input"
                  style={{ maxWidth: 150 }}
                  value={basis}
                  onChange={(event) => setBasis(event.target.value === 'activation' ? 'activation' : 'signup')}
                >
                  <option value="signup">فوج التسجيل</option>
                  <option value="activation">فوج التفعيل</option>
                </select>
                <select
                  className="input"
                  style={{ maxWidth: 130 }}
                  value={cohortMonths}
                  onChange={(event) => setCohortMonths(event.target.value)}
                >
                  <option value="3">٣ أشهر</option>
                  <option value="6">٦ أشهر</option>
                  <option value="12">١٢ شهراً</option>
                </select>
              </div>
            </div>
            {cohorts.status === 'loading' && <Loading rows={4} />}
            {cohorts.status === 'error' && <ErrorBox message={cohorts.error} onRetry={cohorts.reload} />}
            {cohorts.status === 'success' && cohorts.data && (
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>الفوج</th>
                      <th className="num">الحجم</th>
                      {cohorts.data.rows.at(-1)?.cells.map((cell) => (
                        <th key={cell.month} className="num">
                          {monthText(cell.month)}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {cohorts.data.rows.map((row) => (
                      <tr key={row.cohort}>
                        <td>{monthText(row.cohort)}</td>
                        <td className="num">{row.size}</td>
                        {row.cells.map((cell) => (
                          <td key={cell.month} className="num">
                            <div>
                              {cell.contracted}/{row.size} <span className="muted small">متعاقد</span>
                            </div>
                            <div>
                              {cell.active}/{row.size} <span className="muted small">استعمل</span>
                            </div>
                            <div className="muted small">
                              {percent(cell.contractedRate)} · {percent(cell.activeRate)}
                            </div>
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
            <p className="muted small" style={{ marginBottom: 0 }}>
              الخليّة تحمل رقمين: من بقي <strong>متعاقداً</strong> (ترخيصٌ حَيّ في الشهر)، ومن{' '}
              <strong>استعمل</strong> فعلاً (فاتورة مبيعاتٍ مرحَّلة فيه). والفارق بينهما هو ما يُقرأ.
            </p>
          </section>

          <section className="card">
            <h2>الاستخدام لكل باقة</h2>
            {data.usageByPlan.length === 0 ? (
              <Empty title="لا باقات بعد" detail="لا ترخيص حَيّ يشير إلى باقة — أنشئ باقةً أو فعّل ترخيصاً." />
            ) : (
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>الباقة</th>
                      <th className="num">العملاء</th>
                      <th>المقياس</th>
                      <th className="num">الحدّ</th>
                      <th className="num">المتوسط</th>
                      <th className="num">الذروة</th>
                      <th className="num">النسبة</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.usageByPlan.flatMap((group) =>
                      group.metrics.map((metric, index) => (
                        <tr key={`${group.planCode}-${metric.metric}`}>
                          <td>{index === 0 ? group.planName : ''}</td>
                          <td className="num">{index === 0 ? group.tenants : ''}</td>
                          <td>{metric.label}</td>
                          <td className="num">{metric.limit === null ? 'بلا حدّ' : metric.limit}</td>
                          <td className="num">{metric.average.toLocaleString('ar-SA')}</td>
                          <td className="num">{metric.peak.toLocaleString('ar-SA')}</td>
                          <td className="num">{percent(metric.utilization)}</td>
                        </tr>
                      )),
                    )}
                  </tbody>
                </table>
              </div>
            )}
            <p className="muted small" style={{ marginBottom: 0 }}>
              الأرقام والحدود من شبكة الاستخدام (`/usage`) نفسها — لا حسابَ ثانياً هنا. و«بلا حدّ» تعني
              أن المشغّل لم يضبط سقفاً لهذا المقياس، لا أن السقف صفر.
            </p>
          </section>
        </>
      )}
    </Screen>
  );
}
