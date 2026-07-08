import { describe, expect, it } from 'vitest';
import { orderSchema, seatSchema } from './seatTypes';

describe('order/seat schemas', () => {
  it('parses an order', () => {
    const o = orderSchema.parse({
      id: crypto.randomUUID(),
      code: 'ORD001',
      tourDepartureId: crypto.randomUUID(),
      customerId: crypto.randomUUID(),
      totalRevenue: 10000000,
      totalCost: 6000000,
      status: 2,
    });
    expect(o.code).toBe('ORD001');
  });

  it('parses a seat', () => {
    const s = seatSchema.parse({
      id: crypto.randomUUID(),
      orderId: crypto.randomUUID(),
      status: 1,
      upfrontAmount: 0,
      lineTotal: 2500000,
      holdExpiresAt: '2026-08-01T00:00:00Z',
      reservationCode: 'RSV001',
    });
    expect(s.status).toBe(1);
  });

  it('rejects an order missing required fields', () => {
    expect(orderSchema.safeParse({ id: crypto.randomUUID() }).success).toBe(false);
  });
});
