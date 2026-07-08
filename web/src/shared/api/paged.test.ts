import { describe, expect, it } from 'vitest';
import { z } from 'zod';
import { pagedSchema } from './paged';

describe('pagedSchema', () => {
  it('parses a paged envelope', () => {
    const schema = pagedSchema(z.object({ id: z.string() }));
    const parsed = schema.parse({ items: [{ id: 'a' }], total: 1, page: 1, size: 20 });
    expect(parsed.items).toHaveLength(1);
    expect(parsed.total).toBe(1);
  });

  it('rejects a bare array', () => {
    const schema = pagedSchema(z.object({ id: z.string() }));
    expect(() => schema.parse([{ id: 'a' }])).toThrow();
  });
});
