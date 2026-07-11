import { describe, expect, it } from 'vitest';
import { invoiceFormSchema, invoiceSchema } from './types';

describe('invoice schemas', () => {
  it('parses an invoice with lines', () => {
    const inv = invoiceSchema.parse({
      id: crypto.randomUUID(),
      series: '1C25TAA',
      number: '0000123',
      invoiceDate: new Date().toISOString(),
      orderId: null,
      buyerName: 'Công ty ABC',
      buyerTaxCode: '0101234567',
      buyerAddress: null,
      subtotal: 10000000,
      vatAmount: 1000000,
      totalAmount: 11000000,
      status: 0,
      note: null,
      lines: [
        { id: crypto.randomUUID(), description: 'Tour', quantity: 1, unitPrice: 10000000, vatRate: 10, lineAmount: 10000000, lineVat: 1000000 },
      ],
    });
    expect(inv.totalAmount).toBe(11000000);
    expect(inv.lines[0]?.vatRate).toBe(10);
  });

  it('form requires date, buyer and at least 1 line', () => {
    expect(
      invoiceFormSchema.safeParse({
        series: '',
        number: '',
        invoiceDate: '',
        buyerName: '',
        buyerTaxCode: '',
        buyerAddress: '',
        status: 0,
        note: '',
        lines: [],
      }).success,
    ).toBe(false);
  });
});
