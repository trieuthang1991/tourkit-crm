import { describe, expect, it } from 'vitest';
import { orderDebtRowSchema } from './reportApi';

describe('orderDebtRowSchema', () => {
  it('parses an order debt row', () => {
    const row = orderDebtRowSchema.parse({
      orderId: crypto.randomUUID(),
      orderCode: 'DH001',
      customerId: crypto.randomUUID(),
      total: 10000000,
      paid: 4000000,
      outstanding: 6000000,
    });
    expect(row.orderCode).toBe('DH001');
    expect(row.outstanding).toBe(6000000);
  });
});
