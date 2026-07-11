import { describe, expect, it } from 'vitest';
import { customerSourceCreateSchema, customerSourceSchema } from './types';

describe('customer source schemas', () => {
  it('parses a customer source', () => {
    const s = customerSourceSchema.parse({
      id: crypto.randomUUID(),
      name: 'Facebook',
      sortOrder: 1,
      status: 1,
    });
    expect(s.name).toBe('Facebook');
  });

  it('create schema requires name', () => {
    expect(customerSourceCreateSchema.safeParse({ name: '', sortOrder: 1 }).success).toBe(false);
    expect(customerSourceCreateSchema.safeParse({ name: 'Zalo', sortOrder: 1 }).success).toBe(true);
  });
});
