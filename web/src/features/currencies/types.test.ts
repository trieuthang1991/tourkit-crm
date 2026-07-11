import { describe, expect, it } from 'vitest';
import { currencyCreateSchema, currencySchema } from './types';

describe('currency schemas', () => {
  it('parses a currency', () => {
    const c = currencySchema.parse({ id: crypto.randomUUID(), code: 'USD', name: 'Đô la Mỹ', rateToVnd: 25000, sortOrder: 1, status: 1 });
    expect(c.code).toBe('USD');
    expect(c.rateToVnd).toBe(25000);
  });

  it('create form requires code, name and positive rate', () => {
    expect(currencyCreateSchema.safeParse({ code: '', name: 'X', rateToVnd: 1, sortOrder: 0 }).success).toBe(false);
    expect(currencyCreateSchema.safeParse({ code: 'USD', name: 'X', rateToVnd: 0, sortOrder: 0 }).success).toBe(false);
    expect(currencyCreateSchema.safeParse({ code: 'USD', name: 'Đô la', rateToVnd: 25000, sortOrder: 0 }).success).toBe(true);
  });
});
