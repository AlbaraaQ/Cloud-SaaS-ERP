'use client';

import { useState, type ReactNode } from 'react';

import { ApiError } from '../lib/api';
import type { QueryState } from '../lib/use-query';

import { DataTable, Notice, QueryView, type Column } from './data-view';
import { Screen } from './screen';


/**
 * A directory screen = a table plus an inline "new record" form.
 *
 * Two thirds of the menu tree (warehouses, categories, units, parties, departments,
 * jobs, employees, vessels…) are exactly that. Describing them declaratively keeps the
 * pages short enough to read in one screen and makes every one of them behave the same:
 * same states, same validation feedback, same reload-after-save.
 */
export type FieldSpec = {
  name: string;
  label: string;
  type?: 'text' | 'number' | 'date' | 'select' | 'checkbox' | 'textarea';
  options?: Array<{ id: string; label: string }>;
  required?: boolean;
  ltr?: boolean;
  placeholder?: string;
  hint?: string;
  /** Full-width row in the form grid. */
  wide?: boolean;
};

export type FormValues = Record<string, string | boolean>;

export function Directory<T>({
  title,
  subtitle,
  crumbs,
  query,
  columns,
  rowKey,
  empty,
  emptyDetail,
  canCreate = true,
  createLabel = 'إضافة جديد',
  formTitle,
  fields,
  initial,
  onCreate,
  successText,
  blocked,
  toolbar,
  children,
}: {
  title: string;
  subtitle?: string;
  crumbs?: string[];
  query: QueryState<T[]>;
  columns: Array<Column<T>>;
  rowKey: (row: T, index: number) => string;
  empty?: string;
  emptyDetail?: string;
  canCreate?: boolean;
  createLabel?: string;
  formTitle?: string;
  fields: FieldSpec[];
  initial?: FormValues;
  onCreate: (values: FormValues) => Promise<unknown>;
  successText?: (values: FormValues) => string;
  /** Rendered instead of the submit button when a prerequisite is missing. */
  blocked?: string;
  toolbar?: ReactNode;
  /** Extra content rendered between the form and the table. */
  children?: ReactNode;
}) {
  const blank: FormValues = Object.fromEntries(fields.map((field) => [field.name, field.type === 'checkbox' ? false : '']));
  const [open, setOpen] = useState(false);
  const [values, setValues] = useState<FormValues>({ ...blank, ...initial });
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: 'ok' | 'danger'; text: string } | undefined>();

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setNotice(undefined);
    try {
      await onCreate(values);
      setNotice({ kind: 'ok', text: successText ? successText(values) : 'تم الحفظ.' });
      setValues({ ...blank, ...initial });
      query.reload();
    } catch (error) {
      setNotice({ kind: 'danger', text: error instanceof ApiError ? error.message : String(error) });
    } finally {
      setBusy(false);
    }
  }

  return (
    <Screen
      title={title}
      subtitle={subtitle}
      crumbs={crumbs}
      actions={
        canCreate ? (
          <button className="btn primary" type="button" onClick={() => setOpen(!open)}>
            {open ? 'إغلاق' : createLabel}
          </button>
        ) : null
      }
    >
      {open && (
        <form className="card" onSubmit={submit}>
          <h2>{formTitle ?? createLabel}</h2>
          {blocked && <p className="alert warn">{blocked}</p>}
          <FormFields fields={fields} values={values} onChange={setValues} />
          <Notice notice={notice} />
          <button className="btn primary" type="submit" disabled={busy || Boolean(blocked)}>
            {busy ? 'جارٍ الحفظ…' : 'حفظ'}
          </button>
        </form>
      )}

      {toolbar && <div className="card toolbar">{toolbar}</div>}
      {children}

      <QueryView query={query} empty={empty} emptyDetail={emptyDetail}>
        {(rows) => <DataTable rows={rows} rowKey={rowKey} columns={columns} />}
      </QueryView>
    </Screen>
  );
}

/** The same field renderer, also usable by hand-written document screens. */
export function FormFields({
  fields,
  values,
  onChange,
}: {
  fields: FieldSpec[];
  values: FormValues;
  onChange: (next: FormValues) => void;
}) {
  const update = (name: string, value: string | boolean) => onChange({ ...values, [name]: value });

  return (
    <div className="form-grid">
      {fields.map((field) => {
        const value = values[field.name];
        if (field.type === 'checkbox') {
          return (
            <label className="field" key={field.name}>
              <span>{field.label}</span>
              <span className="row">
                <input type="checkbox" checked={Boolean(value)} onChange={(event) => update(field.name, event.target.checked)} />
                {field.hint && <span className="muted small">{field.hint}</span>}
              </span>
            </label>
          );
        }
        return (
          <label className="field" key={field.name} style={field.wide ? { gridColumn: '1 / -1' } : undefined}>
            <span>
              {field.label}
              {field.required ? ' *' : ''}
            </span>
            {field.type === 'select' ? (
              <select
                className="input"
                value={String(value ?? '')}
                required={field.required}
                onChange={(event) => update(field.name, event.target.value)}
              >
                <option value="">— اختر —</option>
                {(field.options ?? []).map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.label}
                  </option>
                ))}
              </select>
            ) : field.type === 'textarea' ? (
              <textarea
                className="input"
                rows={3}
                value={String(value ?? '')}
                required={field.required}
                placeholder={field.placeholder}
                onChange={(event) => update(field.name, event.target.value)}
              />
            ) : (
              <input
                className="input"
                type={field.type === 'date' ? 'date' : 'text'}
                inputMode={field.type === 'number' ? 'decimal' : undefined}
                dir={field.ltr || field.type === 'number' || field.type === 'date' ? 'ltr' : undefined}
                value={String(value ?? '')}
                required={field.required}
                placeholder={field.placeholder}
                onChange={(event) => update(field.name, event.target.value)}
              />
            )}
            {field.hint && <span className="muted small">{field.hint}</span>}
          </label>
        );
      })}
    </div>
  );
}
