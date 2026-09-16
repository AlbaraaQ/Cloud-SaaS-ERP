'use client';

import { useState } from 'react';

const SUPPORT_EMAIL = process.env.NEXT_PUBLIC_SUPPORT_EMAIL ?? 'support@example.com';

export default function ContactPage() {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [body, setBody] = useState('');

  const href = `mailto:${SUPPORT_EMAIL}?subject=${encodeURIComponent(`استفسار من ${name || 'زائر الموقع'}`)}&body=${encodeURIComponent(`${body}\n\n— ${name}\n${email}`)}`;

  return <section className="card"><h1>تواصل معنا</h1>
    <p className="muted">اكتب رسالتك وستُفتح في بريدك لإرسالها إلى فريق المبيعات: <bdi>{SUPPORT_EMAIL}</bdi></p>
    <div className="form">
      <input className="input" placeholder="الاسم" aria-label="name" value={name} onChange={(event) => setName(event.target.value)} />
      <input className="input" placeholder="البريد" aria-label="email" value={email} onChange={(event) => setEmail(event.target.value)} />
      <textarea className="input" placeholder="كيف نساعدك؟" aria-label="message" value={body} onChange={(event) => setBody(event.target.value)} />
      <a className="btn primary" href={href}>إرسال عبر البريد</a>
    </div>
  </section>;
}
