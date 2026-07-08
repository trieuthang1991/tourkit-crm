import { describe, expect, it } from 'vitest';
import { paymentSchema } from './paymentTypes';

describe('paymentSchema', () => {
  it('parses a payment', () => {
    const p = paymentSchema.parse({
      id: crypto.randomUUID(),
      code: 'PAY001',
      orderId: crypto.randomUUID(),
      providerId: null,
      orderCostId: null,
      amount: 1000000,
      paymentMethod: 'cash',
      issuedAt: new Date().toISOString(),
      partner: null,
      receiverName: null,
      note: null,
      status: 0,
      isRecognized: false,
    });
    expect(p.code).toBe('PAY001');
    expect(p.isRecognized).toBe(false);
  });

  it('allows a provider, orderCost, receiver and note', () => {
    const p = paymentSchema.parse({
      id: crypto.randomUUID(),
      code: 'PAY002',
      orderId: crypto.randomUUID(),
      providerId: crypto.randomUUID(),
      orderCostId: crypto.randomUUID(),
      amount: 500000,
      paymentMethod: 'bank_transfer',
      issuedAt: new Date().toISOString(),
      partner: 'Công ty ABC',
      receiverName: 'Nguyễn Văn A',
      note: 'Chi đợt 1',
      status: 1,
      isRecognized: true,
    });
    expect(p.receiverName).toBe('Nguyễn Văn A');
    expect(p.isRecognized).toBe(true);
  });
});
