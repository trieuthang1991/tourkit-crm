import { describe, expect, it } from 'vitest';
import { customerTagCreateSchema, customerTagSchema } from './types';

describe('customer tag schemas', () => {
  it('parses a customer tag with color', () => {
    const t = customerTagSchema.parse({
      id: crypto.randomUUID(),
      name: 'VIP',
      color: 'gold',
      sortOrder: 1,
      status: 1,
    });
    expect(t.name).toBe('VIP');
    expect(t.color).toBe('gold');
  });

  it('create schema requires name, color optional', () => {
    expect(customerTagCreateSchema.safeParse({ name: '', color: null, sortOrder: 1 }).success).toBe(false);
    expect(customerTagCreateSchema.safeParse({ name: 'Mới', color: null, sortOrder: 1 }).success).toBe(true);
  });
});
