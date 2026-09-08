'use client';

import { useMemo, useState } from 'react';

import { Empty, ErrorBox, Forbidden, Loading, Screen } from '../../../../components/screen';
import { apiData } from '../../../../lib/api';
import { useQuery } from '../../../../lib/use-query';
import { nameOf, parentOf, postableOf, typeOf, type Account } from '../../../../lib/accounts';

type TreeNode = Account & { children: TreeNode[] };

function buildTree(accounts: Account[]): TreeNode[] {
  const byId = new Map<string, TreeNode>();
  for (const account of accounts) byId.set(account.id, { ...account, children: [] });

  const roots: TreeNode[] = [];
  for (const node of byId.values()) {
    const parentId = parentOf(node);
    const parent = parentId ? byId.get(parentId) : undefined;
    if (parent) parent.children.push(node);
    else roots.push(node);
  }
  const sort = (nodes: TreeNode[]) => {
    nodes.sort((left, right) => left.code.localeCompare(right.code));
    for (const node of nodes) sort(node.children);
  };
  sort(roots);
  return roots;
}

function Node({ node, depth, expanded, toggle }: { node: TreeNode; depth: number; expanded: Set<string>; toggle: (id: string) => void }) {
  const open = expanded.has(node.id);
  const hasChildren = node.children.length > 0;
  return (
    <>
      <tr>
        <td style={{ paddingInlineStart: 10 + depth * 20 }}>
          {hasChildren ? (
            <button className="btn sm" type="button" onClick={() => toggle(node.id)} aria-expanded={open}>
              {open ? '−' : '+'}
            </button>
          ) : (
            <span style={{ display: 'inline-block', width: 22 }} />
          )}{' '}
          <span dir="ltr">{node.code}</span>
        </td>
        <td>{nameOf(node)}</td>
        <td>{typeOf(node)}</td>
        <td>{postableOf(node) ? <span className="badge active">ترحيل</span> : <span className="badge">تجميعي</span>}</td>
        <td className="num">{node.children.length || ''}</td>
      </tr>
      {open && node.children.map((child) => <Node key={child.id} node={child} depth={depth + 1} expanded={expanded} toggle={toggle} />)}
    </>
  );
}

export default function AccountTreePage() {
  const accounts = useQuery<Account[]>(() => apiData<Account[]>('/accounts'), []);
  const [expanded, setExpanded] = useState<Set<string>>(new Set());

  const tree = useMemo(() => buildTree(accounts.data ?? []), [accounts.data]);

  const toggle = (id: string) =>
    setExpanded((current) => {
      const next = new Set(current);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  const expandAll = () => setExpanded(new Set((accounts.data ?? []).map((account) => account.id)));

  return (
    <Screen
      title="شجرة الحسابات"
      subtitle="عرض هرمي لدليل الحسابات بالمستويات."
      crumbs={['المحاسبة', 'تعاريف']}
      actions={
        <>
          <button className="btn" type="button" onClick={expandAll}>
            توسيع الكل
          </button>
          <button className="btn" type="button" onClick={() => setExpanded(new Set())}>
            طي الكل
          </button>
          <button className="btn" type="button" onClick={() => window.print()}>
            طباعة
          </button>
        </>
      }
    >
      {accounts.status === 'loading' && <Loading />}
      {accounts.status === 'forbidden' && <Forbidden />}
      {accounts.status === 'error' && <ErrorBox message={accounts.error} onRetry={accounts.reload} />}
      {accounts.status === 'success' &&
        (tree.length === 0 ? (
          <Empty title="لا توجد حسابات بعد" />
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>الرمز</th>
                  <th>الاسم</th>
                  <th>النوع</th>
                  <th>الحالة</th>
                  <th className="num">فروع</th>
                </tr>
              </thead>
              <tbody>
                {tree.map((node) => (
                  <Node key={node.id} node={node} depth={0} expanded={expanded} toggle={toggle} />
                ))}
              </tbody>
            </table>
          </div>
        ))}
    </Screen>
  );
}
