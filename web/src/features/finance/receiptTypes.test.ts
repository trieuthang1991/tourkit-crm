import { describe, expect, it } from 'vitest';
import { balanceSchema, receiptSchema } from './receiptTypes';

describe('receiptSchema', () => {
  it('parses a receipt', () => {
    const r = receiptSchema.parse({
      id: crypto.randomUUID(),
      code: 'PT001',
      orderId: crypto.randomUUID(),
      amount: 1000000,
      paymentMethod: 'cash',
      issuedAt: new Date().toISOString(),
      partner: null,
      note: null,
      status: 1,
      isRecognized: false,
    });
    expect(r.code).toBe('PT001');
    expect(r.isRecognized).toBe(false);
  });

  it('allows a partner and note', () => {
    const r = receiptSchema.parse({
      id: crypto.randomUUID(),
      code: 'PT002',
      orderId: crypto.randomUUID(),
      amount: 500000,
      paymentMethod: 'bank_transfer',
      issuedAt: new Date().toISOString(),
      partner: 'Công ty ABC',
      note: 'Thu đợt 1',
      status: 2,
      isRecognized: true,
    });
    expect(r.partner).toBe('Công ty ABC');
    expect(r.isRecognized).toBe(true);
  });
});

describe('balanceSchema', () => {
  it('parses a balance', () => {
    const b = balanceSchema.parse({ orderId: crypto.randomUUID(), total: 100, paid: 50, outstanding: 50 });
    expect(b.outstanding).toBe(50);
  });
});
