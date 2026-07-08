import { describe, expect, it } from 'vitest';
import { orderCostSchema } from './orderCostApi';

describe('orderCostSchema', () => {
  it('parses an order cost', () => {
    const c = orderCostSchema.parse({
      id: crypto.randomUUID(),
      orderId: crypto.randomUUID(),
      providerId: crypto.randomUUID(),
      serviceName: 'Xe 45 chỗ',
      dayIndex: 1,
      expectedAmount: 5000000,
      actualAmount: 4800000,
      deposit: 1000000,
      surcharge: 0,
      vat: 480000,
      status: 1,
    });
    expect(c.serviceName).toBe('Xe 45 chỗ');
    expect(c.status).toBe(1);
  });

  it('allows a null serviceName', () => {
    const c = orderCostSchema.parse({
      id: crypto.randomUUID(),
      orderId: crypto.randomUUID(),
      providerId: crypto.randomUUID(),
      serviceName: null,
      dayIndex: 2,
      expectedAmount: 0,
      actualAmount: 0,
      deposit: 0,
      surcharge: 0,
      vat: 0,
      status: 0,
    });
    expect(c.serviceName).toBeNull();
  });
});
