import { describe, expect, it } from 'vitest';
import { quoteFormSchema, quoteSchema } from './types';

describe('quote schemas', () => {
  it('parses a quote with pricing fields and lines', () => {
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
      adults: 10,
      children: 2,
      infants: 1,
      childPercent: 75,
      infantPercent: 50,
      totalCost: 4000000,
      totalProfit: 1000000,
      adultPrice: 2900000,
      childPrice: 2175000,
      infantPrice: 1450000,
      convertedOrderId: null,
      lines: [
        {
          id: crypto.randomUUID(),
          description: 'Phòng khách sạn',
          quantity: 3,
          unitPrice: 600000,
          amount: 1800000,
          serviceType: 1,
          scope: 1,
          providerServiceId: null,
          unitCost: 500000,
          marginPercent: 20,
        },
      ],
    });
    expect(q.lines).toHaveLength(1);
    expect(q.totalProfit).toBe(1000000);
    expect(q.lines[0]?.unitCost).toBe(500000);
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
        adults: 0,
        children: 0,
        infants: 0,
        childPercent: 75,
        infantPercent: 50,
        lines: [],
      }).success,
    ).toBe(false);
  });

  it('form rejects margin over 500 percent', () => {
    expect(
      quoteFormSchema.safeParse({
        code: 'BG-1',
        customerName: '',
        title: 'Tour',
        validUntil: null,
        status: 0,
        note: '',
        adults: 1,
        children: 0,
        infants: 0,
        childPercent: 75,
        infantPercent: 50,
        lines: [
          {
            description: 'Phòng',
            quantity: 1,
            unitPrice: 0,
            serviceType: 1,
            scope: 1,
            providerServiceId: null,
            unitCost: 100000,
            marginPercent: 900,
          },
        ],
      }).success,
    ).toBe(false);
  });
});
