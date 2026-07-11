import { describe, expect, it } from 'vitest';
import { paymentTermCreateSchema, paymentTermSchema } from './types';

describe('payment term schemas', () => {
  it('parses a payment term', () => {
    const t = paymentTermSchema.parse({ id: crypto.randomUUID(), name: 'Cọc 30%', description: 'Trước 7 ngày', sortOrder: 1, status: 1 });
    expect(t.name).toBe('Cọc 30%');
  });

  it('create form requires name; coerces empty description to null', () => {
    expect(paymentTermCreateSchema.parse({ name: 'A', description: '', sortOrder: 0 }).description).toBeNull();
    expect(paymentTermCreateSchema.safeParse({ name: '', description: null, sortOrder: 0 }).success).toBe(false);
  });
});
