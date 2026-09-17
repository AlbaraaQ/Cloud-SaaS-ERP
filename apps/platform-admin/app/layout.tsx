import './globals.css';

import type { Metadata, Viewport } from 'next';
import type { ReactNode } from 'react';

import { AuthGate } from '../components/auth-gate';
import { SessionProvider } from '../lib/session';

export const metadata: Metadata = {
  title: 'لوحة تحكم المنصة — Cloud ERP',
  description: 'إدارة المنشآت والاشتراكات والتراخيص لمشغّلي المنصة',
};

export const viewport: Viewport = { width: 'device-width', initialScale: 1 };

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="ar" dir="rtl">
      <body>
        <SessionProvider>
          <AuthGate>{children}</AuthGate>
        </SessionProvider>
      </body>
    </html>
  );
}
