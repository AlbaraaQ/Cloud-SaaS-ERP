import { readdirSync, readFileSync, statSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

import { permissionRegistry } from '@erp/contracts';
import { describe, expect, it } from 'vitest';

/**
 * Guard rail for `@RequiresPermission`.
 *
 * The guard denies any code the registry does not know, so a typo (or a code that was
 * never registered) turns into a permanent 403 that no role can grant. This test fails
 * at build time instead, where the mistake is cheap.
 */

const srcDir = dirname(fileURLToPath(import.meta.url));

function sourceFiles(dir: string): string[] {
  return readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) return sourceFiles(full);
    return entry.isFile() && full.endsWith('.ts') && !full.endsWith('.spec.ts') ? [full] : [];
  });
}

describe('permission codes used by controllers', () => {
  it('are all declared in the shared registry', () => {
    const declared = new Set(permissionRegistry.map((permission) => permission.code));
    const used = new Map<string, string>();

    for (const file of sourceFiles(srcDir)) {
      if (!statSync(file).isFile()) continue;
      const source = readFileSync(file, 'utf8');
      for (const match of source.matchAll(/RequiresPermission\('([^']+)'\)/g)) {
        const code = match[1];
        if (code) used.set(code, file.slice(srcDir.length + 1));
      }
    }

    expect(used.size).toBeGreaterThan(50);
    const unknown = [...used.entries()]
      .filter(([code]) => !declared.has(code))
      .map(([code, file]) => `${code} (${file})`);
    expect(unknown).toEqual([]);
  });
});
