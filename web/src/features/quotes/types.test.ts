import { describe, expect, it } from 'vitest';
import { quoteFormSchema, quoteSchema } from './types';

describe('quote schemas', () => {
  it('parses a quote with lines', () => {
    const q = quoteSchema.parse({
      id: crypto.randomUUID(),
      code: 'BG-1',
      customerId: null,
      customerName: 'Công ty ABC',
      title: 'Báo giá tour',
      validUntil: null,
      status: 0,
      note: null,
      totalAmount: 5000000,
      lines: [{ id: crypto.randomUUID(), description: 'Người lớn', quantity: 2, unitPrice: 2500000, amount: 5000000 }],
    });
    expect(q.lines).toHaveLength(1);
    expect(q.totalAmount).toBe(5000000);
  });

  it('form requires code, title and at least 1 line', () => {
    expect(
      quoteFormSchema.safeParse({
        code: '',
        customerName: '',
        title: '',
        validUntil: null,
        status: 0,
        note: '',
        lines: [],
      }).success,
    ).toBe(false);
  });
});
