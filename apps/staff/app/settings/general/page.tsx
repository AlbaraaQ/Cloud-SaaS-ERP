'use client';

import { ErrorBox, Forbidden, Loading, Screen } from '../../../components/screen';
import { apiData } from '../../../lib/api';
import { useQuery } from '../../../lib/use-query';

type Settings = Record<string, unknown>;

export default function GeneralSettingsPage() {
  const settings = useQuery<Settings>(() => apiData<Settings>('/settings'), []);

  const entries = Object.entries(settings.data ?? {});

  return (
    <Screen
      title="إعدادات عامة"
      subtitle="الإعدادات المطبقة على هذه المنشأة (العملة، الضريبة، سياسة الترقيم، …)."
      crumbs={['الإعدادات', 'إعدادات عامة']}
      actions={
        <button className="btn" type="button" onClick={settings.reload}>
          تحديث
        </button>
      }
    >
      {settings.status === 'loading' && <Loading />}
      {settings.status === 'forbidden' && <Forbidden />}
      {settings.status === 'error' && <ErrorBox message={settings.error} onRetry={settings.reload} />}
      {settings.status === 'success' && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>المفتاح</th>
                <th>القيمة</th>
              </tr>
            </thead>
            <tbody>
              {entries.map(([key, value]) => (
                <tr key={key}>
                  <td dir="ltr">{key}</td>
                  <td dir="ltr" className="small">{typeof value === 'object' ? JSON.stringify(value) : String(value)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Screen>
  );
}
