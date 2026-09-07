import { describe, expect, it } from 'vitest';

import { sections } from '../lib/navigation.js';

describe('admin navigation coverage', () => {
  it('covers dashboard plus master requirement sections 1-12', () => {
    expect(sections.map((section) => section.key)).toEqual(['dashboard','platform','organization','catalog','accounting','parties','inventory','sales','purchases','treasury','einvoicing','reporting','migration']);
  });
  it('keeps every route permission gated', () => {
    expect(sections.every((section) => section.permission.includes('.'))).toBe(true);
  });
});
