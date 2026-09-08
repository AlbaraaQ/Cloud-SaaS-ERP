'use client';

import { useEffect, useState } from 'react';

import { StatusBadge } from '../../components/status-badge';
import { apiFetch } from '../../lib/api';

type RequestRow = { id: string; tenant_code: string; tenant_name: string; plan_name: string; amount: string; created_at: string; status: string };

function token() {
  const value = globalThis.document.cookie.split('; ').find((part) => part.startsWith('erp_access_token='));
  return value ? decodeURIComponent(value.split('=')[1] ?? '') : undefined;
}

export default function BillingPage() {
  const [requests, setRequests] = useState<RequestRow[]>([]);
  const [message, setMessage] = useState('');
  const [loading, setLoading] = useState(true);

  async function load() {
    try {
      const result = await apiFetch<{ data: RequestRow[] }>('/billing/activation-requests', token());
      setRequests(result.data);
    } catch {
      setMessage('تعذر تحميل الطلبات. تأكد من تسجيل الدخول بصلاحيات مدير المنصة.');
    } finally { setLoading(false); }
  }

  useEffect(() => { void load(); }, []);

  async function review(id: string, approve: boolean) {
    setMessage('');
    try {
      await apiFetch(`/billing/activation-requests/${id}/review`, token(), { method: 'POST', body: JSON.stringify({ approve }) });
      setRequests((current) => current.filter((request) => request.id !== id));
      setMessage(approve ? 'تم تفعيل الاشتراك بنجاح.' : 'تم رفض طلب التفعيل.');
    } catch { setMessage('تعذر تنفيذ القرار.'); }
  }

  return <div className="grid">
    <section className="section-title"><div><h1>اشتراكات العملاء</h1><p className="muted">مراجعة طلبات التفعيل اليدوي وإدارة الوصول للنظام المحاسبي.</p></div></section>
    {message && <p className="muted" role="status">{message}</p>}
    {loading ? <p className="muted">جاري تحميل الطلبات...</p> : requests.length === 0 ? <section className="card"><h2>لا توجد طلبات معلقة</h2><p className="muted">ستظهر هنا طلبات العملاء من صفحة الاشتراك.</p></section> : <section className="card" style={{ overflowX: 'auto' }}><table><thead><tr><th>الشركة</th><th>الباقة</th><th>القيمة</th><th>التاريخ</th><th>الحالة</th><th>الإجراء</th></tr></thead><tbody>{requests.map((request) => <tr key={request.id}><td>{request.tenant_name} ({request.tenant_code})</td><td>{request.plan_name}</td><td>{request.amount}</td><td>{new Date(request.created_at).toLocaleDateString('ar-SA')}</td><td><StatusBadge value={request.status} /></td><td><div className="row"><button className="btn primary" type="button" onClick={() => review(request.id, true)}>تفعيل</button><button className="btn" type="button" onClick={() => review(request.id, false)}>رفض</button></div></td></tr>)}</tbody></table></section>}
  </div>;
}
