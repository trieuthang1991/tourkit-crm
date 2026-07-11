import { describe, expect, it } from 'vitest';
import { languageTypeCreateSchema, languageTypeSchema } from './types';

describe('language type schemas', () => {
  it('parses a language type', () => {
    const t = languageTypeSchema.parse({ id: crypto.randomUUID(), name: 'Tiếng Anh', code: 'en', sortOrder: 1, status: 1 });
    expect(t.name).toBe('Tiếng Anh');
    expect(t.code).toBe('en');
  });

  it('create form requires name and coerces empty code to null', () => {
    const parsed = languageTypeCreateSchema.parse({ name: 'Tiếng Trung', code: '', sortOrder: 0 });
    expect(parsed.code).toBeNull();
    expect(languageTypeCreateSchema.safeParse({ name: '', code: null, sortOrder: 0 }).success).toBe(false);
  });
});
