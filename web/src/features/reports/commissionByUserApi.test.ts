import { describe, expect, it } from 'vitest';
import { commissionByUserRowSchema } from './commissionByUserApi';

describe('commissionByUserRowSchema', () => {
  it('parses a commission-by-user row', () => {
    const row = commissionByUserRowSchema.parse({
      userId: crypto.randomUUID(),
      turnover: 10000000,
      cost: 3000000,
      profit: 7000000,
      commissionRate: 10,
      commissionAmount: 700000,
    });
    expect(row.commissionAmount).toBe(700000);
  });
});
