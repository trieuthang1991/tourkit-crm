import { describe, expect, it } from 'vitest';
import { marketTypeCreateSchema, marketTypeSchema } from './types';

describe('market type schemas', () => {
  it('parses a market type', () => {
    const m = marketTypeSchema.parse({
      id: crypto.randomUUID(),
      name: 'Nội địa',
      parentId: null,
      sortOrder: 1,
      status: 1,
    });
    expect(m.name).toBe('Nội địa');
  });

  it('create schema requires name', () => {
    expect(marketTypeCreateSchema.safeParse({ name: '', parentId: null, sortOrder: 1 }).success).toBe(false);
  });
});
