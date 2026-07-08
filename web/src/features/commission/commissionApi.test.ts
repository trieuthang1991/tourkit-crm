import { describe, expect, it } from 'vitest';
import { profitSchema, profitShareSchema } from './commissionApi';

describe('profitSchema', () => {
  it('parses a profit summary', () => {
    const p = profitSchema.parse({ revenue: 100000000, cost: 60000000, profit: 40000000 });
    expect(p.profit).toBe(40000000);
  });
});

describe('profitShareSchema', () => {
  it('parses a profit share', () => {
    const s = profitShareSchema.parse({
      id: crypto.randomUUID(),
      orderId: crypto.randomUUID(),
      userId: crypto.randomUUID(),
      percentage: 10,
      amount: 4000000,
      profitBase: 40000000,
    });
    expect(s.percentage).toBe(10);
    expect(s.amount).toBe(4000000);
  });
});
