import { describe, expect, it } from 'vitest';
import { serviceBookingFormSchema, serviceBookingSchema } from './types';

describe('service booking schemas', () => {
  it('parses a service booking', () => {
    const s = serviceBookingSchema.parse({
      id: crypto.randomUUID(),
      code: 'SB-1',
      type: 1,
      orderId: null,
      providerId: null,
      description: 'Khách sạn 4*',
      startDate: null,
      endDate: null,
      quantity: 2,
      unitPrice: 1000000,
      totalAmount: 2000000,
      status: 0,
      note: null,
    });
    expect(s.type).toBe(1);
    expect(s.totalAmount).toBe(2000000);
  });

  it('form requires description', () => {
    expect(
      serviceBookingFormSchema.safeParse({
        code: '',
        type: 1,
        orderId: '',
        providerId: '',
        description: '',
        startDate: null,
        endDate: null,
        quantity: 1,
        unitPrice: 0,
        status: 0,
        note: '',
      }).success,
    ).toBe(false);
  });
});
