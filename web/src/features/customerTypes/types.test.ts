import { describe, expect, it } from 'vitest';
import { customerTypeCreateSchema, customerTypeSchema } from './types';

describe('customer type schemas', () => {
  it('parses a customer type', () => {
    const c = customerTypeSchema.parse({
      id: crypto.randomUUID(),
      code: 1,
      name: 'Khách lẻ',
      sortOrder: 1,
      status: 1,
    });
    expect(c.name).toBe('Khách lẻ');
    expect(c.code).toBe(1);
  });

  it('create schema requires name and positive code', () => {
    expect(customerTypeCreateSchema.safeParse({ code: 1, name: '', sortOrder: 1 }).success).toBe(false);
    expect(customerTypeCreateSchema.safeParse({ code: 0, name: 'Khách lẻ', sortOrder: 1 }).success).toBe(false);
    expect(customerTypeCreateSchema.safeParse({ code: 2, name: 'Khách đoàn', sortOrder: 1 }).success).toBe(true);
  });
});
